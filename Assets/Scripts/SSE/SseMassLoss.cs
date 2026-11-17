// SseMassLoss.cs —— 质量损失与观测换算的 C# 机械翻译
// 严格对应 UPB-CLI src 下的 massloss.c / massloss.h
// （对应原 Python 项目中的 sse/massloss.py）。
//
// Hurley, Pols & Tout (2000) 第 7 节给出了与拟合公式配套使用的完整质量损失
// 算法。详细模型本身是无质量损失的，所以这里在演化轨道之上叠加质量损失。
//
// 三个处方叠加使用：
//   - Nieuwenhuijzen & de Jager (1990): 全赫罗图的高光度星风
//   - Reimers (1975):                  巨星 (kw 2..9)
//   - Vassiliadis & Wood (1993):       脉动 AGB (kw 5, 6)
// 再加上小包层被剥离恒星的减弱 Wolf-Rayet 星风。
//
// 注意: 论文第 7 节的处方都以 Msun/yr 为单位，本程序内部时间用 Myr，
//       所以最后乘 1.0e6 转换成 Msun/Myr。
//
// 翻译约定：指针/结构体不涉及；math.h -> System.Math；函数名保留 C 原名；
// 常量宏 -> private const。

using System;
using System.Collections.Generic;

namespace UPB.SSE {

    /// <summary>质量损失率与观测换算，见 massloss.c。</summary>
    public static class SseMassLoss {

        /* --- 与 Python 版一致的常量 (massloss.h) --- */
        private const double ML_L0     = 7.0e4;
        private const double ML_KAP    = -0.5;
        private const double ML_A_HE   = 4.0;
        private const double ML_A_CO   = 15.0;
        private const double ML_A_ONe  = 17.0;
        private const double ML_B_WIND = 0.0;
        private const double ML_NETA   = 0.5;

        /* 1 Myr = 1.0e6 yr，用于把 Msun/yr 转成 Msun/Myr */
        private const double ML_YR_PER_MYR = 1.0e6;

        /* ---------------------------------------------------------- 质量损失 */

        /// <summary>
        /// 总质量损失率，单位 Msun/Myr。对应 C 版 mass_loss_rate()。
        /// 参数: kw 恒星类型; lum/r 光度(Lsun)/半径(Rsun);
        ///       mt/mc 当前总质量、核心质量(Msun); z 金属度;
        ///       rl Roche-lobe 半径 (只在潮汐增强风里用，单星时传 0)。
        /// </summary>
        public static double mass_loss_rate(int kw, double lum, double r,
                                            double mt, double mc, double z, double rl)
        {
            double zfac, dms, dml, mew;
            double p0, dmt;

            if (mt <= 0.0 || r <= 0.0 || lum <= 0.0)
                return 0.0;

            zfac = Math.Sqrt(z / 0.02);

            /* --- Nieuwenhuijzen & de Jager (1990)，全赫罗图 --- */
            if (lum > 4000.0) {
                double x = Math.Min(1.0, (lum - 4000.0) / 500.0);
                dms = 9.6e-15 * x * Math.Pow(r, 0.81) * Math.Pow(lum, 1.24) * Math.Pow(mt, 0.16);
                dms *= zfac;
            } else {
                dms = 0.0;
            }

            if (kw >= 2 && kw <= 9) {
                /* --- Reimers (1975) --- */
                dml = ML_NETA * 4.0e-13 * r * lum / mt;
                if (rl > 0.0) {
                    double rr = Math.Min(0.5, r / rl);
                    dml *= 1.0 + ML_B_WIND * Math.Pow(rr, 6.0);
                }

                /* --- Vassiliadis & Wood (1993): 长周期 AGB 脉动 --- */
                if (kw == 5 || kw == 6) {
                    p0 = -2.07 - 0.9 * Math.Log10(mt) + 1.94 * Math.Log10(r);
                    p0 = Math.Min(Math.Pow(10.0, p0), 2000.0);
                    dmt = -11.4 + 0.0125 * (p0 - 100.0 * Math.Max(mt - 2.5, 0.0));
                    dmt = Math.Min(Math.Pow(10.0, dmt), 1.36e-9 * lum);
                    dml = Math.Max(dml, dmt);
                }

                if (kw > 6) {
                    dms = Math.Max(dml, 1.0e-13 * Math.Pow(lum, 1.5));
                } else {
                    dms = Math.Max(dml, dms);
                    mew = ((mt - mc) / mt)
                          * Math.Min(5.0, Math.Max(1.2, Math.Pow(lum / ML_L0, ML_KAP)));
                    if (mew < 1.0) {
                        /* 小氢包层的减弱 Wolf-Rayet 星风 */
                        dml = 1.0e-13 * Math.Pow(lum, 1.5) * (1.0 - mew) * zfac;
                        dms = Math.Max(dml, dms);
                    }
                }
            }

            /* 从 Msun/yr 转成 Msun/Myr */
            return dms * ML_YR_PER_MYR;
        }

        /* ---------------------------------------------------------- 观测换算 */

        /// <summary>从 L (Lsun) 和 R (Rsun) 求有效温度 (K)。</summary>
        public static double temperature_from_lr(double lum, double r)
        {
            if (lum <= 0.0 || r <= 0.0)
                return 0.0;
            return 5773.15 * Math.Pow(lum / (r * r), 0.25);
        }

        /* C 中的 SPECTRAL_BOUNDS[] 表 { limit, name }；
         * 这里拆成两个平行的静态数组，逐项一一对应，遍历顺序完全相同。 */
        private static readonly double[] SPECTRAL_BOUNDS_LIMIT = {
            30000.0,
            10000.0,
             7500.0,
             6000.0,
             5200.0,
             3700.0,
        };

        private static readonly string[] SPECTRAL_BOUNDS_NAME = {
            "O",
            "B",
            "A",
            "F",
            "G",
            "K",
        };

        /// <summary>从有效温度判断光谱型 (单字符字符串，如 "G")。</summary>
        public static string spectral_type_from_temperature(double t_eff)
        {
            int i;
            int n = SPECTRAL_BOUNDS_LIMIT.Length;
            for (i = 0; i < n; i++) {
                if (t_eff >= SPECTRAL_BOUNDS_LIMIT[i])
                    return SPECTRAL_BOUNDS_NAME[i];
            }
            return "M";
        }

        /// <summary>近似热改正 BC (等值)，仅用于报告，不影响演化。</summary>
        public static double bolometric_correction(double t_eff)
        {
            double x;
            if (t_eff <= 0.0)
                return 0.0;
            x = Math.Log10(t_eff);
            if (x >= 3.90)
                return -5.0 + 2.5 * (x - 3.90) * 10.0;
            if (x >= 3.70)
                return -0.10 + 4.0 * (x - 3.70);
            return 0.9 + 3.0 * (x - 3.60);
        }
    }
}
