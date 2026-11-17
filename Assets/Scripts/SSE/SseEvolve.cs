// SseEvolve.cs —— 单星演化驱动的 C# 机械翻译
// 严格对应 UPB-CLI src 下的 evolve.c / evolve.h
// （对应原 Python 项目中的 sse/evolve.py）。
//
// 让一颗恒星按照 Hurley, Pols & Tout (2000) 的阶段推进：从 ZAMS 经过主序、
// 赫氏空隙、第一巨星支、核心氦燃烧、渐近巨星支，到残骸阶段，期间按照论文
// 第 7 节叠加质量损失。
//
// 默认步长为主序寿命的 1/1000，所有阶段统一使用；在需要时缩短以避免跨过
// 阶段边界。按照论文第 7.1 节，时标、光度和核心质量用的序列参数取自有效
// 初始质量 M0；M0 只在主序和赫氏空隙上重新锚定到当前质量，以及恒星被剥离
// 成裸氦星时。半径公式和质量损失率使用当前质量。
//
// 翻译约定：
//   - Track / TrackPoint 是类，故 C 里的 `Track tr` 局部变量 + `*out = tr`
//     在此直接作用于传入的 tr（引用语义，赋值是自赋值）。
//   - C 的 malloc/realloc 动态数组 -> Track.points (List<TrackPoint>)。
//   - 函数名、分支顺序、步长逻辑、质量损失调用位置均与 C 完全一致。

using System;
using System.Collections.Generic;

namespace UPB.SSE {

    /// <summary>单星演化驱动，见 evolve.c。</summary>
    public static class SseEvolve {

        /* --- 残骸类型 (不再进行核燃烧)，evolve.h --- */
        private const int EV_REMNANT_MIN = 10;
        private const int EV_REMNANT_MAX = 15;

        /* ----------------------------------------------------------- Track 管理 */

        /// <summary>
        /// 初始化 Track（清零、分配默认值）。对应 C 版 track_init()。
        /// C 里是 memset(tr, 0, sizeof(*tr))；这里逐字段清零。
        /// </summary>
        public static void track_init(Track tr, double mass, double metallicity)
        {
            if (tr == null) return;

            /* memset 部分 */
            tr.mass = 0.0;
            tr.metallicity = 0.0;
            if (tr.points == null) {
                tr.points = new List<TrackPoint>();
            } else {
                tr.points.Clear();
            }
            tr.t_ms   = 0.0;
            tr.t_bgb  = 0.0;
            tr.t_he_i = 0.0;
            tr.t_he   = 0.0;
            tr.t_bagb = 0.0;
            tr.t_tp   = 0.0;
            tr.t_nuc  = 0.0;
            tr.l_zams = 0.0;
            tr.r_zams = 0.0;
            tr.final_mass = 0.0;
            tr.success = 0;
            tr.error = "";
            tr.steps = 0;
            tr.stagnated = 0;
            /* C: last 被 memset 成全零结构体，track_final 因而永远不会返回 NULL */
            tr.last = new TrackPoint();
            tr.last.spectral_type = "";

            /* memset 之后的显式赋值 */
            tr.mass = mass;
            tr.metallicity = metallicity;
            tr.success = 1;
        }

        /// <summary>
        /// 释放 Track 内部的 points 数组。C# 里 List 由 GC 管理，只需清空。
        /// </summary>
        public static void track_free(Track tr)
        {
            if (tr == null) return;
            if (tr.points != null) {
                tr.points.Clear();
            }
        }

        /// <summary>取最后一个记录点；track 为空时返回 null。</summary>
        public static TrackPoint track_final(Track tr)
        {
            if (tr == null) return null;
            if (tr.points != null && tr.points.Count > 0)
                return tr.points[tr.points.Count - 1];
            return tr.last;
        }

        /// <summary>
        /// 追加一个记录点到 Track。C 里 cap_points 满时按 2 倍 realloc；
        /// C# 的 List&lt;TrackPoint&gt; 自动增长，因此只需 Add。
        /// </summary>
        private static int track_append(Track tr, TrackPoint pt)
        {
            tr.points.Add(pt);
            return 0;
        }

        /* --------------------------------------------------------------- 边界 */

        /// <summary>
        /// 阶段边界时间，按升序排列。对裸氦星 (kw 7..9) 用氦星主序寿命。
        /// C 版原型: static int boundaries(const Sequence *sp, int kw, double mt, double *out)
        /// （out 是数组输出，故这里用 double[] output，名字避开 C# 关键字 out。）
        /// </summary>
        private static int boundaries(Sequence sp, int kw, double mt, double[] output)
        {
            int n = 0;
            double[] ts = new double[6];
            int i, j;

            if (kw >= 7 && kw <= 9) {
                double tm_he = (mt > 0.0) ? SseTimescales.seq_t_hems(sp, mt) : 0.0;
                if (tm_he > 0.0) { output[n++] = tm_he; }
                return n;
            }

            ts[0] = sp.tm;
            ts[1] = sp.tscls[1];
            ts[2] = sp.tscls[2];
            ts[3] = sp.tscls[2] + sp.tscls[3];
            ts[4] = sp.tscls[13];
            ts[5] = sp.tscls[14];

            /* 去掉非正值 */
            for (i = 0; i < 6; i++) {
                if (ts[i] > 0.0) {
                    output[n++] = ts[i];
                }
            }
            /* 冒泡排序 */
            for (i = 0; i < n - 1; i++) {
                for (j = 0; j < n - 1 - i; j++) {
                    if (output[j] > output[j + 1]) {
                        double t = output[j];
                        output[j] = output[j + 1];
                        output[j + 1] = t;
                    }
                }
            }
            return n;
        }

        /* --------------------------------------------------------------- evolve */

        /// <summary>
        /// 主演化函数。返回 0 成功；-1 参数错误（含 sequence_init 失败）。
        /// </summary>
        public static int evolve(double mass, double metallicity, EvolveOptions opts, Track tr)
        {
            EvolveOptions opt;
            Sequence sp, sp_cur;
            double aj, mt, mc, mass0, prev_kw;
            /* C 里的局部变量名 base 是 C# 关键字，故改名为 base_dt（其余完全对应） */
            double end_time, base_dt, dt;
            int kw, n_steps;
            int phase_start_aj, phase_steps, prev_phase;
            int i, nb;
            double[] bd = new double[6];

            if (tr == null) return -1;

            /* 默认选项 (C: opt = *opts 是结构体拷贝；这里同样拷贝一份，
             * 不改动调用者传入的 opts.max_steps 等字段) */
            if (opts != null) {
                opt = new EvolveOptions();
                opt.end_time = opts.end_time;
                opt.step_size = opts.step_size;
                opt.max_steps = opts.max_steps;
                opt.record = opts.record;
                opt.mass_loss = opts.mass_loss;
                opt.stop_at_remnant = opts.stop_at_remnant;
            } else {
                opt = new EvolveOptions();
                opt.end_time = 0.0;
                opt.step_size = 0.0;
                opt.max_steps = 100000;
                opt.record = 1;
                opt.mass_loss = 1;
                opt.stop_at_remnant = 1;
            }
            if (opt.max_steps <= 0) opt.max_steps = 100000;

            track_init(tr, mass, metallicity);

            sp = new Sequence();
            if (SseTimescales.sequence_init(sp, mass, metallicity) != 0) {
                tr.success = 0;
                /* C: snprintf(..., "sequence_init failed for M=%g Z=%g", ...) */
                tr.error = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                         "sequence_init failed for M={0:G6} Z={1:G6}",
                                         mass, metallicity);
                /* C: *out = tr; —— 引用语义下 tr 即 out，无需赋值 */
                return -1;
            }

            tr.t_ms   = sp.tm;
            tr.t_bgb  = sp.tscls[1];
            tr.t_he_i = sp.tscls[2];
            tr.t_he   = sp.tscls[3];
            tr.t_bagb = sp.t_bagb;
            tr.t_tp   = sp.t_tp;
            tr.t_nuc  = sp.tn;
            tr.l_zams = sp.lums[1];
            tr.r_zams = SseTimescales.seq_rzams(sp, sp.mass);

            end_time = (opt.end_time > 0.0) ? opt.end_time : 1.0e300;

            /* 初始状态 */
            aj = 0.0;
            mt = mass;
            mc = 0.0;
            kw = (mass < sp.mhook - 0.3) ? 0 : 1;
            sp_cur = sp;
            mass0 = sp.mass_in;
            prev_kw = (double)kw;

            if (opt.step_size > 0.0) {
                base_dt = opt.step_size;
            } else {
                base_dt = Math.Max(sp.tm / 1000.0, 1.0e-9);
            }

            n_steps = 0;
            phase_start_aj = (int)aj;
            phase_steps = 0;
            prev_phase = kw;

            while (n_steps < opt.max_steps) {
                Point pt;

                /* --- 有效初始质量 M0 (论文 Section 7.1) --- */
                if (kw >= 7 && kw <= 9 && prev_kw <= 6.0) {
                    SseTimescales.sequence_init(sp_cur, mt, metallicity);
                    mass0 = sp_cur.mass_in;
                } else if (kw <= 2 && Math.Abs(mt - mass0) > 1.0e-4 * Math.Max(mass0, 1.0)) {
                    Sequence sp_new = new Sequence();
                    SseTimescales.sequence_init(sp_new, mt, metallicity);
                    if (kw <= 1) {
                        if (sp_cur.tm > 0.0)
                            aj *= sp_new.tm / sp_cur.tm;
                    } else if (sp_cur.tscls[1] > sp_cur.tm) {
                        double frac = (aj - sp_cur.tm)
                                      / (sp_cur.tscls[1] - sp_cur.tm);
                        if (frac < 0.0) frac = 0.0;
                        if (frac > 1.0) frac = 1.0;
                        aj = sp_new.tm + frac * (sp_new.tscls[1] - sp_new.tm);
                    }
                    sp_cur = sp_new;
                    mass0 = sp_cur.mass_in;
                }
                prev_kw = (double)kw;

                /* --- 一步 hrdiag --- */
                pt = SsePhases.hrdiag(sp_cur, kw, aj, mt, mc, mass0);
                kw = pt.kw;
                mc = pt.mc;
                if (pt.kw == 15 || pt.mt > 0.0) {
                    mt = pt.mt;
                }

                if (kw != prev_phase) {
                    prev_phase = kw;
                    phase_start_aj = (int)aj;   /* C 中同样只赋值、不读取 */
                    phase_steps = 0;
                } else {
                    phase_steps++;
                }

                /* --- 记录 --- */
                {
                    TrackPoint rec = new TrackPoint();
                    double temp = SseMassLoss.temperature_from_lr(pt.lum, pt.r);
                    rec.time = aj;
                    rec.luminosity = pt.lum;
                    rec.radius = pt.r;
                    rec.temperature = temp;
                    rec.mass = mt;
                    rec.core_mass = mc;
                    rec.phase = kw;
                    /* C: spectral_type[0] = name[0]; spectral_type[1] = '\0';
                     * massloss 返回的始终是单字符串，Substring(0,1) 等价于取 [0]。 */
                    rec.spectral_type = SseMassLoss.spectral_type_from_temperature(temp).Substring(0, 1);
                    rec.core_temperature = 0.0;
                    rec.menv = pt.menv;
                    rec.renv = pt.renv;

                    if (opt.record != 0) {
                        if (track_append(tr, rec) != 0) {
                            tr.success = 0;
                            tr.error = "out of memory appending track point";
                            return -1;
                        }
                    }
                    tr.last = rec;
                }

                tr.steps = n_steps + 1;

                if (aj >= end_time) break;

                /* --- 到达残骸 --- */
                if (opt.stop_at_remnant != 0
                    && (kw >= EV_REMNANT_MIN && kw <= EV_REMNANT_MAX)) {
                    break;
                }
                if (opt.stop_at_remnant != 0 && mt <= 0.0) break;

                /* --- 选择下一步步长 --- */
                dt = base_dt;

                if (opt.step_size <= 0.0) {
                    /* 停滞保护 */
                    if (phase_steps > 60000) {
                        tr.stagnated = 1;
                        break;
                    }

                    /* 不跨过阶段边界 */
                    if (kw >= 3) {
                        nb = boundaries(sp_cur, kw, mt, bd);
                        for (i = 0; i < nb; i++) {
                            if (aj < bd[i] && bd[i] < aj + dt) {
                                dt = bd[i] - aj;
                                break;
                            }
                        }
                    }
                }

                {
                    double t_next = aj + dt;
                    if (end_time != 1.0e300 && t_next > end_time) {
                        t_next = end_time;
                    }

                    /* --- 应用质量损失 --- */
                    if (opt.mass_loss != 0 && mt > 0.0) {
                        double rate = SseMassLoss.mass_loss_rate(kw, pt.lum, pt.r, mt, mc,
                                                                 metallicity, 0.0);
                        mt = mt - rate * (t_next - aj);
                        if (kw >= 2 && kw <= 9 && mc > 0.0) {
                            if (mt < mc) mt = mc;
                        }
                        if (mt < 0.0) mt = 0.0;
                    }

                    aj = t_next;
                }

                n_steps++;
            }

            tr.final_mass = mt;
            /* C: *out = tr; —— 引用语义下 tr 即 out，无需赋值 */
            return 0;
        }
    }
}
