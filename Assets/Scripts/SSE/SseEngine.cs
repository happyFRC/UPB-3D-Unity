// SseEngine.cs —— 面向用户的单星演化引擎的 C# 机械翻译
// 严格对应 UPB-CLI src 下的 engine.c / engine.h
// （engine.c 里 _to_state 调用的 temperature_from_lr 来自 massloss.h，这里直接用
//   SseMassLoss.temperature_from_lr）。
//
// 翻译约定（与工程里其它 SSE 移植文件一致）：
//   - C 的结构体指针 -> C# 类引用，`p->x` -> `p.x`
//   - C 的 `double *out` -> 返回 PhaseTimes 实例（失败返回 null，对应 C 的 -1 且 out 被清零）
//   - C 的 char[] -> string；snprintf 的缓冲区截断长度照样保留
//   - 文件级 static 函数 -> private static，且保留 C 的标识符便于逐行比对
//   - math.h -> System.Math（isfinite 用本文件的 _is_finite）
//
// 共享类型 Track / TrackPoint（SseTypes.cs）不允许修改，因此 C 版
// EvolutionResult / StellarState 中在 C# 侧没有对应成员的字段无法落地，
// 每处都在代码里就地注释：
//   EvolutionResult.end_time / step_size / computation_time_ms  -> Track 无对应字段
//   EvolutionResult.final_state                                -> 映射为 Track.last
//   StellarState.core_h_fraction / core_he_fraction / phase_name -> TrackPoint 无对应字段
//     （core_temperature 与光谱型照旧落盘；阶段名请调用 SseEngine.phase_name_cn(phase)）

using System;
using System.Collections.Generic;

namespace UPB.SSE {

    /// <summary>阶段边界时间与特征光度，见 engine.h 的 PhaseTimes（字段一一对应）。</summary>
    public class PhaseTimes {
        public double t_ms, t_bgb, t_he_i, t_he, t_bagb, t_tp, t_nuclear;
        public double l_zams, l_tms, l_bgb, l_he_i, l_min_he, l_bagb;
        public double r_zams, r_tms;
        public double m_hook, m_he_f, m_fgb;
    }

    /// <summary>面向用户的单星演化引擎，见 engine.c。</summary>
    public static class SseEngine {

        /* ---------------------------------------------------- 支持的范围 (engine.h) */
        public const double ENG_MASS_MIN = 0.077;
        public const double ENG_MASS_MAX = 100.0;
        public const double ENG_Z_MIN    = 0.0001;
        public const double ENG_Z_MAX    = 0.03;

        /* engine.h 的 SpectralType 枚举。C# 侧只在 _spectral_boundary_to_value 里
         * 需要这些常量（该函数在 C 里同样没有被调用，见下面的注释）。 */
        private const int SPECTRAL_O = 0;
        private const int SPECTRAL_B = 1;
        private const int SPECTRAL_A = 2;
        private const int SPECTRAL_F = 3;
        private const int SPECTRAL_G = 4;
        private const int SPECTRAL_K = 5;
        private const int SPECTRAL_M = 6;

        /* ------------------------------------------------------------ 阶段名 */

        /* 英文阶段名，对应 engine.c 的 PHASE_LABELS_EN */
        private static readonly string[] PHASE_LABELS_EN = {
            "MS (convective)",
            "Main sequence",
            "Hertzsprung gap",
            "First giant branch",
            "Core helium burning",
            "Early AGB",
            "Thermally pulsing AGB",
            "Naked helium star MS",
            "Naked helium star HG",
            "Naked helium star GB",
            "Helium white dwarf",
            "Carbon/oxygen white dwarf",
            "Oxygen/neon white dwarf",
            "Neutron star",
            "Black hole",
            "Massless remnant"
        };

        /* 中文阶段名，对应 engine.c 的 PHASE_LABELS_CN */
        private static readonly string[] PHASE_LABELS_CN = {
            "主序(全对流)",
            "主序",
            "赫氏空隙",
            "第一巨星支",
            "核心氦燃烧",
            "早期AGB",
            "热脉动AGB",
            "氦星主序",
            "氦星赫氏空隙",
            "氦星巨星支",
            "氦白矮星",
            "碳氧白矮星",
            "氧氖白矮星",
            "中子星",
            "黑洞",
            "无质量残骸"
        };

        /// <summary>英文阶段名；phase 越界返回 "unknown"。</summary>
        public static string phase_name_en(int phase)
        {
            if (phase < 0 || phase > 15) return "unknown";
            return PHASE_LABELS_EN[phase];
        }

        /// <summary>中文阶段名；phase 越界返回 "未知"。</summary>
        public static string phase_name_cn(int phase)
        {
            if (phase < 0 || phase > 15) return "未知";
            return PHASE_LABELS_CN[phase];
        }

        /* 残骸阶段（不再是核燃烧恒星） */
        private static bool is_remnant_phase(int phase)
        {
            return (phase >= 10 && phase <= 15);
        }

        /* ---------------------------------------------------------------- 辅助 */

        /* 光谱型枚举 -> 温度边界。
         * 与 C 版一样，这个函数在 engine.c 里并没有被调用（死代码），
         * 这里照抄保留，便于与 C 版逐行比对。 */
        private static double _spectral_boundary_to_value(int sp)
        {
            switch (sp) {
                case SPECTRAL_O: return 30000.0;
                case SPECTRAL_B: return 10000.0;
                case SPECTRAL_A: return  7500.0;
                case SPECTRAL_F: return  6000.0;
                case SPECTRAL_G: return  5200.0;
                case SPECTRAL_K: return  3700.0;
                default:         return     0.0;
            }
        }

        /* 与 phases.py / engine.py 里 _spectral_type 相同的判断（阈值与顺序照抄） */
        private static string _spectral_type_str(double t_eff)
        {
            string s;
            if      (t_eff >= 30000.0) { s = "O"; }
            else if (t_eff >= 10000.0) { s = "B"; }
            else if (t_eff >=  7500.0) { s = "A"; }
            else if (t_eff >=  6000.0) { s = "F"; }
            else if (t_eff >=  5200.0) { s = "G"; }
            else if (t_eff >=  3700.0) { s = "K"; }
            else                       { s = "M"; }
            return s;
        }

        /* 近似中心温度 (K)。仅为诊断，不影响演化。
         *
         * 病毒定理估计 T_c ~ G M mu m_u / (k R)，无量纲系数校准到今日太阳
         * 为 1.57e7 K。退化残骸不适用病毒定理，返回 0。
         */
        private static double _core_temperature(double mass, double radius,
                                               double x, double y, double z, int phase)
        {
            double denom, mu;
            if (phase >= 7 || mass <= 0.0 || radius <= 0.0) return 0.0;
            denom = 2.0 * x + 0.75 * y + 0.5 * z;
            mu = (denom > 0.0) ? (1.0 / denom) : 0.6;
            return 1.57e7 * (mass / 1.0) * (mu / 0.62) * (1.0 / radius);
        }

        /* 把 TrackPoint 转成“StellarState”（C# 侧同样是 TrackPoint） */
        private static TrackPoint _to_state(TrackPoint pt)
        {
            double t_eff;
            TrackPoint st = new TrackPoint();          /* memset(st, 0, sizeof(*st)) */
            t_eff = pt.temperature;
            if (t_eff <= 0.0) t_eff = SseMassLoss.temperature_from_lr(pt.luminosity, pt.radius);
            st.time        = pt.time;
            st.luminosity  = pt.luminosity;
            st.radius      = pt.radius;
            st.temperature = t_eff;
            st.spectral_type = _spectral_type_str(t_eff);
            /* C 版下面还有：
             *   st->core_h_fraction  = 0.0;
             *   st->core_he_fraction = 0.0;
             *   st->core_temperature = 0.0;   （随后由 engine_evolve 覆写）
             *   st->phase_name = phase_name_cn(pt->phase)（截断到 39 字符）
             * 共享类型 TrackPoint 只有 core_temperature，没有前两个成分字段和
             * phase_name（SseTypes.cs 不允许修改）。阶段名请用
             * SseEngine.phase_name_cn(st.phase) 取得。 */
            st.core_temperature = 0.0;
            st.mass             = pt.mass;
            st.core_mass        = pt.core_mass;
            st.phase            = pt.phase;
            st.menv             = pt.menv;
            st.renv             = pt.renv;
            return st;
        }

        /* ---------------------------------------------------------- 输入检查 */

        /* isfinite() 的等价物（Unity 的 .NET Standard 2.0 里没有 double.IsFinite） */
        private static bool _is_finite(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }

        /* C 的 %g（6 位有效数字，去掉尾零），仅用于错误信息 */
        private static string _g(double v)
        {
            return v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
        }

        /* snprintf(r->error_message, sizeof(r->error_message), "%s", msg) 的等价：
         * char[256] 最多放 255 个字符 + '\0' */
        private static void _set_error(Track r, string msg)
        {
            if (r == null) return;
            if (msg == null) msg = "";
            r.error = (msg.Length > 255) ? msg.Substring(0, 255) : msg;
        }

        /* 校验 (mass, metallicity)。返回 null 表示通过，否则返回错误信息。 */
        private static string _validate(double mass, double metallicity)
        {
            if (!_is_finite(mass) || !_is_finite(metallicity)) {
                return "mass and metallicity must be finite numbers";
            }
            if (mass < ENG_MASS_MIN || mass > ENG_MASS_MAX) {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                     "mass {0} is outside the supported range [{1}, {2}] Msun",
                                     _g(mass), _g(ENG_MASS_MIN), _g(ENG_MASS_MAX));
            }
            if (metallicity < ENG_Z_MIN || metallicity > ENG_Z_MAX) {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                     "metallicity {0} is outside the supported range [{1}, {2}]",
                                     _g(metallicity), _g(ENG_Z_MIN), _g(ENG_Z_MAX));
            }
            return null;
        }

        /* ------------------------------------------------------------ 生命周期 */

        /// <summary>主序寿命 t_MS (Myr)。参数越界返回 0。</summary>
        public static double engine_get_main_sequence_lifetime(double mass, double metallicity)
        {
            Sequence sp = new Sequence();
            if (mass < ENG_MASS_MIN || mass > ENG_MASS_MAX) return 0.0;
            if (metallicity < ENG_Z_MIN || metallicity > ENG_Z_MAX) return 0.0;
            if (SseTimescales.sequence_init(sp, mass, metallicity) != 0) return 0.0;
            return sp.tm;
        }

        /// <summary>BGB 寿命 t_BGB (Myr)。参数越界返回 0。</summary>
        public static double engine_get_bgb_lifetime(double mass, double metallicity)
        {
            Sequence sp = new Sequence();
            if (mass < ENG_MASS_MIN || mass > ENG_MASS_MAX) return 0.0;
            if (metallicity < ENG_Z_MIN || metallicity > ENG_Z_MAX) return 0.0;
            if (SseTimescales.sequence_init(sp, mass, metallicity) != 0) return 0.0;
            return sp.tscls[1];
        }

        /// <summary>
        /// 所有阶段边界时间与特征光度。参数越界 / sequence_init 失败返回 null
        /// （对应 C 版返回 -1 且 *out 保持 memset 后的全零）。
        /// </summary>
        public static PhaseTimes engine_get_phase_times(double mass, double metallicity)
        {
            Sequence sp;
            PhaseTimes pt = new PhaseTimes();          /* memset(out, 0, sizeof(*out)) */
            if (mass < ENG_MASS_MIN || mass > ENG_MASS_MAX) return null;
            if (metallicity < ENG_Z_MIN || metallicity > ENG_Z_MAX) return null;

            sp = new Sequence();
            if (SseTimescales.sequence_init(sp, mass, metallicity) != 0) return null;

            pt.t_ms       = sp.tm;
            pt.t_bgb      = sp.tscls[1];
            pt.t_he_i     = sp.tscls[2];
            pt.t_he       = sp.tscls[3];
            pt.t_bagb     = sp.t_bagb;
            pt.t_tp       = sp.t_tp;
            pt.t_nuclear  = sp.tn;

            pt.l_zams     = sp.lums[1];
            pt.l_tms      = sp.lums[2];
            pt.l_bgb      = sp.lums[3];
            pt.l_he_i     = sp.lums[4];
            pt.l_min_he   = sp.lums[5];
            pt.l_bagb     = sp.lums[7];

            pt.r_zams     = SseTimescales.seq_rzams(sp, sp.mass);
            pt.r_tms      = SseTimescales.seq_r_tms(sp, sp.mass);

            pt.m_hook     = sp.mhook;
            pt.m_he_f     = sp.mhef;
            pt.m_fgb      = sp.mfgb;
            return pt;
        }

        /* -------------------------------------------------------------- evolve */

        /* memset(result, 0, sizeof(*result)) 的等价物。
         * C 版会把 evolution_track 指针一起清零；C# 的 Track.points 是 List，
         * 清空即等价于“空的轨道”。 */
        private static void _result_clear(Track r)
        {
            r.mass = 0.0;
            r.metallicity = 0.0;
            if (r.points == null) r.points = new List<TrackPoint>();
            else r.points.Clear();
            r.t_ms = 0.0;
            r.t_bgb = 0.0;
            r.t_he_i = 0.0;
            r.t_he = 0.0;
            r.t_bagb = 0.0;
            r.t_tp = 0.0;
            r.t_nuc = 0.0;
            r.l_zams = 0.0;
            r.r_zams = 0.0;
            r.final_mass = 0.0;
            r.success = 0;
            r.error = "";
            r.steps = 0;
            r.stagnated = 0;
            r.last = null;
        }

        /* 追加 TrackPoint 到结果。
         * C 版用 realloc 扩容、失败返回 -1；C# 的 List 自行管理容量，
         * 这里只剩 Add（唯一失败情形是内存耗尽，会抛 OutOfMemoryException）。 */
        private static int result_append(Track r, TrackPoint st)
        {
            r.points.Add(st);
            return 0;
        }

        /* 时钟：返回毫秒。
         * C 版用 clock()（进程 CPU 时间）；这里取 .NET 的单调高精度计数器。
         * 注意 Track 没有 computation_time_ms 字段，所以耗时算出来无处存放，
         * 仅保留 engine.c 的调用点以便对照。 */
        private static double now_ms()
        {
            return (double)System.Diagnostics.Stopwatch.GetTimestamp() * 1000.0
                   / (double)System.Diagnostics.Stopwatch.Frequency;
        }

        /// <summary>
        /// 单星演化。
        /// track_history != 0 时把每个记录点追加到 result.points；
        /// 无论是否保留轨道，result.last 都会是有效的最终状态（对应 C 的
        /// get_final_state()，engine.c 里用 track_final(&tr) 取得，空轨道时回落到
        /// Track.last）。成功返回 0，失败返回 -1 且 result.error 有说明。
        /// </summary>
        public static int engine_evolve(double mass, double metallicity,
                                        double end_time, double step_size,
                                        int track_history, int mass_loss,
                                        Track result)
        {
            EvolveOptions opt;
            Track tr;
            string problem;
            double t0;
            double x, y, z;
            int i, rc;

            if (result == null) return -1;
            _result_clear(result);                     /* memset(result, 0, sizeof(*result)) */

            result.mass = mass;
            result.metallicity = metallicity;
            /* C 版还写入 result->end_time = end_time; result->step_size = step_size;
             * Track 没有这两个字段（SseTypes.cs 不可修改）。 */

            /* 参数校验 */
            problem = _validate(mass, metallicity);
            if (problem != null) {
                result.success = 0;
                _set_error(result, problem);
                return -1;
            }
            if (end_time < 0.0) {
                result.success = 0;
                _set_error(result, "end_time must not be negative");
                return -1;
            }

            t0 = now_ms();

            /* 调用底层 evolve。
             * end_time <= 0 表示演化到残骸、step_size <= 0 表示默认 t_MS/1000，
             * 这两个“默认值”由 evolve() 自己解释，engine.c 里是原样透传。 */
            opt = new EvolveOptions();
            opt.end_time = end_time;
            opt.step_size = step_size;
            opt.max_steps = 100000;
            opt.record = (track_history != 0) ? 1 : 0;
            opt.mass_loss = (mass_loss != 0) ? 1 : 0;
            opt.stop_at_remnant = 1;

            tr = new Track();
            /* C 版这里是未初始化的栈上 `Track tr;`，由 evolve() 内部 track_init()。
             * C# 里显式初始化一次，保证传进去的 Track 一定有效（track_init 幂等）。 */
            SseEvolve.track_init(tr, mass, metallicity);
            rc = SseEvolve.evolve(mass, metallicity, opt, tr);
            if (rc != 0 || tr.success == 0) {
                result.success = 0;
                _set_error(result, tr.error);
                /* C 版此处还写 result->computation_time_ms = now_ms() - t0;
                 * Track 无该字段。 */
                SseEvolve.track_free(tr);
                return -1;
            }

            result.steps = tr.steps;
            result.stagnated = tr.stagnated;
            result.t_ms   = tr.t_ms;
            result.t_bgb  = tr.t_bgb;
            result.t_he_i = tr.t_he_i;
            result.t_he   = tr.t_he;
            result.t_bagb = tr.t_bagb;
            result.t_tp   = tr.t_tp;
            /* evolution.h 的 Track 还有 t_nuc / l_zams / r_zams / final_mass，
             * 但 engine.c 的 EvolutionResult 没有这些字段，这里照 C 只搬上面 6 个，
             * 与 C 版对外可见行为一致（result.t_nuc 留 0）。 */

            /* 包层成分: Y = 0.24 + 2Z, X = 1 - Y - Z */
            y = 0.24 + 2.0 * metallicity;
            x = 1.0 - y - metallicity;
            if (x < 0.0) x = 0.0;
            z = metallicity;

            /* 轨道 */
            if (track_history != 0) {
                for (i = 0; i < tr.n_points; i++) {
                    TrackPoint st;
                    st = _to_state(tr.points[i]);
                    /* C 版还写 st.core_h_fraction = x; st.core_he_fraction = y;
                     * TrackPoint 无这两个字段。 */
                    st.core_temperature = _core_temperature(st.mass, st.radius,
                                                            x, y, z, st.phase);
                    if (result_append(result, st) != 0) {
                        result.success = 0;
                        _set_error(result, "out of memory building track");
                        SseEvolve.track_free(tr);
                        return -1;
                    }
                }
            }

            /* 最终状态 */
            {
                TrackPoint fp = SseEvolve.track_final(tr);
                if (fp != null) {
                    TrackPoint fs;
                    fs = _to_state(fp);
                    /* C 版还写 fs.core_h_fraction = x; fs.core_he_fraction = y; */
                    fs.core_temperature = _core_temperature(fs.mass, fs.radius,
                                                            x, y, z, fs.phase);
                    result.last = fs;                  /* C: result->final_state = fs; */

                    /* 保持存储的最后一个点与最终状态一致 */
                    if (result.n_points > 0) {
                        result.points[result.n_points - 1] = fs;
                    }
                }
            }

            /* C 版：result->computation_time_ms = now_ms() - t0; 然后 success = 1。
             * Track 没有 computation_time_ms 字段（SseTypes.cs 不允许修改），
             * 所以这里仍照 C 计算一次耗时，只是无处保存。 */
            double computation_time_ms = now_ms() - t0;
            if (computation_time_ms < 0.0) computation_time_ms = 0.0;

            result.success = 1;
            SseEvolve.track_free(tr);                   /* C: track_free(&tr) */
            return 0;
        }

        /* engine.c 的 track_final(&tr) 在 evolve.h 里实现，C# 版见
         * SseEvolve.track_final：轨道为空时回落到 Track.last，永不为 null。 */

        /* ------------------------------------------------------------- 便捷函数 */

        /// <summary>便捷函数：演化为摘要结构体（不保留轨道）。</summary>
        public static int evolve_star(double mass, double metallicity, double end_time,
                                      double step_size, Track result)
        {
            return engine_evolve(mass, metallicity, end_time, step_size,
                                 0, 1, result);
        }

        /// <summary>便捷函数：演化为完整结果（保留轨道）。</summary>
        public static int evolve_star_detailed(double mass, double metallicity, double end_time,
                                               double step_size, Track result)
        {
            return engine_evolve(mass, metallicity, end_time, step_size,
                                 1, 1, result);
        }

        /* engine.c 里还有 engine_evolve_batch() 与 evolution_result_free()：
         *   - batch 需要 double[] 参数组 + Track[] 结果数组，本题未要求，且
         *     数组长度与 max_steps 的默认值需另行约定，故未移植；
         *   - evolution_result_free 对 List 型 Track 没有意义（无裸指针可释放）。 */
    }
}
