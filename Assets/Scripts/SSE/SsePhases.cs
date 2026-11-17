// SsePhases.cs —— 恒星属性随年龄变化的 C# 机械翻译
// 严格对应 UPB-CLI src 下的 phases.c / phases.h
// （Hurley, Pols & Tout 2000 的 hrdiag 例程 + mrenv 包层估计）。
//
// 翻译约定：
//   - const Sequence *sp  -> Sequence sp；sp->x -> sp.x
//   - double *x 输出参数   -> out double x（mrenv 按约定；helium_star 见下方说明）
//   - math.h              -> System.Math
//   - 静态文件级函数       -> private static
// 数值、分支顺序、钳位（fmin/fmax）与 C 版逐行对应，未做任何化简或优化。

using System;
using System.Collections.Generic;

namespace UPB.SSE {

    /// <summary>恒星属性随年龄变化（论文第 5 节）与对流包层（第 7.2 节），见 phases.c。</summary>
    public static class SsePhases {

        /* ---------------- phases.h 常量 ---------------- */
        private const double PH_LUM0   = 7.0e4;
        private const double PH_KAP    = -0.5;
        private const double PH_AHE    = 4.0;
        private const double PH_ACO    = 15.0;
        private const double PH_AONE   = 17.0;
        private const double PH_MXNS   = 3.0;
        private const double PH_RNS    = 1.4e-5;
        private const double PH_TAUMIN = 5.0e-8;

        /* ---------------- timescales.h 常量 ---------------- */
        private const double MCH_  = 1.44;
        private const double MLP_  = 12.0;
        private const double TINY_ = 1.0e-14;

        /* ------------------------------------------------------------------ 小工具 */

        /// <summary>base**exponent，base&lt;=0 时返回 0（phases.c: pow_safe）。</summary>
        private static double pow_safe(double base_v, double exponent)
        {
            if (base_v <= 0.0) return 0.0;
            return Math.Pow(base_v, exponent);
        }

        /// <summary>Fortran 风格的实数幂（phases.c: real_pow）。</summary>
        private static double real_pow(double base_v, double exponent)
        {
            if (base_v < 0.0) return -Math.Pow(-base_v, exponent);
            return Math.Pow(base_v, exponent);
        }

        /* --------------------------------------------------- MS 亮度/半径系数 (5.1) */

        /// <summary>MS 光度系数 alpha（phases.c: lalph）。</summary>
        private static double lalph(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double mcut = 2.0;
            double b_val;
            if (m >= mcut) {
                return (a.a45 + a.a46 * Math.Pow(m, a.a48))
                       / (Math.Pow(m, 0.4) + a.a47 * Math.Pow(m, 1.9));
            }
            if (m <= 0.5) return a.a49;
            if (m <= 0.7) return a.a49 + ((0.3 - a.a49) / 0.2) * (m - 0.5);
            if (m <= a.a52)
                return 0.3 + ((a.a50 - 0.3) / (a.a52 - 0.7)) * (m - 0.7);
            if (m <= a.a53)
                return a.a50 + ((a.a51 - a.a50) / (a.a53 - a.a52))
                       * (m - a.a52);
            b_val = (a.a45 + a.a46 * Math.Pow(mcut, a.a48))
                    / (Math.Pow(mcut, 0.4) + a.a47 * Math.Pow(mcut, 1.9));
            return a.a51 + ((b_val - a.a51) / (mcut - a.a53)) * (m - a.a53);
        }

        /// <summary>MS 光度系数 beta（phases.c: lbet）。</summary>
        private static double lbet(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double beta = Math.Max(0.0, a.a54 - a.a55 * Math.Pow(m, a.a56));
            double a57 = a.a57;
            if (m > a57 && beta > 0.0) {
                double b_val = Math.Max(0.0, a.a54 - a.a55 * Math.Pow(a57, a.a56));
                beta = Math.Max(0.0, b_val - 10.0 * (m - a57) * b_val);
            }
            return beta;
        }

        /// <summary>MS 光度系数 eta（phases.c: lneta）。</summary>
        private static double lneta(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double neta;
            if (m <= 1.0)       neta = 10.0;
            else if (m >= 1.1)  neta = 20.0;
            else                neta = 10.0 + 100.0 * (m - 1.0);
            return Math.Min(neta, a.eta_max);
        }

        /// <summary>MS 半径系数 alpha（phases.c: ralph）。</summary>
        private static double ralph(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double b_val, c_val;
            if (m <= 0.5) return a.a62;
            if (m <= 0.65)
                return a.a62 + ((a.a63 - a.a62) / 0.15) * (m - 0.5);
            if (m <= a.a68)
                return a.a63 + ((a.a64 - a.a63) / (a.a68 - 0.65)) * (m - 0.65);
            if (m <= a.a66) {
                b_val = (a.a58 * Math.Pow(a.a66, a.a60))
                        / (a.a59 * Math.Pow(a.a66, a.a61));
                return a.a64 + ((b_val - a.a64) / (a.a66 - a.a68))
                       * (m - a.a68);
            }
            if (m <= a.a67)
                return (a.a58 * Math.Pow(m, a.a60)) / (a.a59 * Math.Pow(m, a.a61));
            c_val = (a.a58 * Math.Pow(a.a67, a.a60))
                    / (a.a59 * Math.Pow(a.a67, a.a61));
            return c_val + a.a65 * (m - a.a67);
        }

        /// <summary>MS 半径系数 beta（phases.c: rbet）。</summary>
        private static double rbet(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double m2 = 2.0, m3 = 16.0;
            double v, b2, b3;
            if (m <= 1.0)          v = 1.06;
            else if (m <= a.a74)   v = 1.06 + ((a.a72 - 1.06) / (a.a74 - 1.0))
                                            * (m - 1.0);
            else if (m <= m2) {
                b2 = (a.a69 * Math.Pow(m2, 3.5)) / (a.a70 + Math.Pow(m2, a.a71));
                v = a.a72 + ((b2 - a.a72) / (m2 - a.a74)) * (m - a.a74);
            } else if (m <= m3) {
                v = (a.a69 * Math.Pow(m, 3.5)) / (a.a70 + Math.Pow(m, a.a71));
            } else {
                b3 = (a.a69 * Math.Pow(m3, 3.5)) / (a.a70 + Math.Pow(m3, a.a71));
                v = b3 + a.a73 * (m - m3);
            }
            return v - 1.0;
        }

        /// <summary>MS 半径系数 gamma（phases.c: rgamm）。</summary>
        private static double rgamm(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double m1 = 1.0;
            double b1, v, c_val;
            if (m > a.a75 + 0.1) return 0.0;
            b1 = Math.Max(0.0, a.a76 + a.a77 * Math.Pow(Math.Abs(m1 - a.a78), a.a79));
            if (m <= m1) {
                v = a.a76 + a.a77 * Math.Pow(Math.Abs(m - a.a78), a.a79);
            } else if (m <= a.a75) {
                v = b1 + (a.a80 - b1)
                    * Math.Pow((m - m1) / (a.a75 - m1), a.a81);
            } else {
                c_val = a.a80;
                if (a.a75 <= m1) c_val = b1;
                v = c_val - 10.0 * (m - a.a75) * c_val;
            }
            return Math.Max(v, 0.0);
        }

        /// <summary>钩子（hook）光度扰动，eq. 5.2（phases.c: lhook）。</summary>
        private static double lhook(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double mhook = sp.mhook;
            double b_val;
            if (m <= mhook) return 0.0;
            if (m >= a.a33)
                return Math.Min(a.a34 / Math.Pow(m, a.a35), a.a36 / Math.Pow(m, a.a37));
            b_val = Math.Min(a.a34 / Math.Pow(a.a33, a.a35),
                             a.a36 / Math.Pow(a.a33, a.a37));
            return b_val * Math.Pow((m - mhook) / (a.a33 - mhook), 0.4);
        }

        /// <summary>钩子（hook）半径扰动（phases.c: rhook）。</summary>
        private static double rhook(Sequence sp, double m)
        {
            ZCoeffs a = sp.c;
            double mhook = sp.mhook;
            double m2, b2;
            if (m <= mhook) return 0.0;
            if (m <= a.a42)
                return a.a43 * Math.Sqrt((m - mhook) / (a.a42 - mhook));
            if (m <= 2.0) {
                m2 = 2.0;
                b2 = (a.a38 + a.a39 * Math.Pow(m2, 3.5))
                     / (a.a40 * Math.Pow(m2, 3.0) + Math.Pow(m2, a.a41)) - 1.0;
                return a.a43 + (b2 - a.a43)
                       * Math.Pow((m - a.a42) / (m2 - a.a42), a.a44);
            }
            return (a.a38 + a.a39 * Math.Pow(m, 3.5))
                   / (a.a40 * Math.Pow(m, 3.0) + Math.Pow(m, a.a41)) - 1.0;
        }

        /// <summary>MS 末核心质量系数（phases.c: mctms，eq. 5.x）。</summary>
        private static double mctms(Sequence sp, double m)
        {
            double m525 = Math.Pow(m, 5.25);
            return (1.586 + m525) / (2.434 + 1.02 * m525);
        }

        /* ------------------------------------------------- 小包层扰动 (eq. 83-84) */

        /// <summary>小包层光度扰动（phases.c: lpert）。</summary>
        private static double lpert(double m, double mew)
        {
            double b = 0.002 * Math.Max(1.0, 2.5 / m);
            double c = 3.0;
            return ((1.0 + Math.Pow(b, c)) * Math.Pow(mew / b, c))
                   / (1.0 + Math.Pow(mew / b, c));
        }

        /// <summary>小包层半径扰动（phases.c: rpert）。</summary>
        private static double rpert(double m, double mew, double r, double rc)
        {
            double a = 0.1;
            double b = 0.006 * Math.Max(1.0, 2.5 / m);
            double c = 3.0;
            double q = Math.Log(r / rc);
            return ((1.0 + Math.Pow(b, c)) * Math.Pow(mew / b, c) * Math.Pow(mew, a / q))
                   / (1.0 + Math.Pow(mew / b, c));
        }

        /* ------------------------------------------------------------ 白矮星半径 */

        /// <summary>白矮星/中子星半径（phases.c: wd_radius）。</summary>
        private static double wd_radius(double mt)
        {
            double arg;
            if (mt < 5.0e-6) return 0.009;
            if (mt < 5.0e-4) return 0.09;
            arg = Math.Pow(MCH_ / mt, 2.0 / 3.0) - Math.Pow(mt / MCH_, 2.0 / 3.0);
            return Math.Max(PH_RNS, 0.0115 * Math.Sqrt(Math.Max(1.48204e-6, arg)));
        }

        /* ------------------------------------------------------------ 裸氦星处理 */

        /// <summary>
        /// C 版 HE_MC_FROM_L 宏（phases.c 中 helium_star 内的 #define）。
        /// 宏体只依赖 lum 与 gb[3..7]，故抽成带 gb 参数的静态函数。
        /// </summary>
        private static double he_mc_from_l(double lum, double[] gb)
        {
            return (lum <= gb[4] * Math.Pow(Math.Pow(gb[3] / gb[4], 1.0 / (gb[5] - gb[6])), gb[5]))
                ? ((lum > 0.0) ? Math.Pow(lum / gb[4], 1.0 / gb[5]) : 0.0)
                : ((lum > 0.0) ? Math.Pow(lum / gb[3], 1.0 / gb[6]) : 0.0);
        }

        /// <summary>
        /// 裸氦星（phases.c: helium_star）。返回 r, lum, kw, mc, rc, menv, renv, k2。
        /// 注意：C 版是「通过指针写出」，且存在只写部分输出的路径（aj >= tm 且未
        /// 触发残骸判据时 lum 不会被写回，调用者原有的 lum 保留）。为保持这一语义，
        /// 这里用 ref 而不是 out；调用点处的实参在调用前都已赋值。
        /// 形参 kw / mc 在 C 版函数体内未被读取，为保持签名一致而保留。
        /// </summary>
        private static void helium_star(Sequence sp, int kw, double aj, double mt,
                                        double mc,
                                        ref double r_out, ref double lum_out, ref int kw_out,
                                        ref double mc_out, ref double rc_out,
                                        ref double menv_out, ref double renv_out, ref double k2_out)
        {
            double[] tscls = new double[16];
            double[] lums = new double[9];
            double[] gb = new double[10];
            double tm, rzams, rx, rc, menv, renv, k2;
            double mtc, mcmax;
            int i;

            for (i = 0; i < 16; i++) tscls[i] = sp.tscls[i];
            for (i = 0; i < 9; i++)  lums[i]  = sp.lums[i];
            for (i = 0; i < 10; i++) gb[i]    = sp.gb[i];

            tm = SseTimescales.seq_t_hems(sp, mt);
            tscls[1] = tm;
            lums[1] = SseTimescales.seq_l_zhe(sp, mt);
            {
                double am = Math.Max(0.0, 0.85 - 0.08 * mt);
                lums[2] = lums[1] * (1.0 + 0.45 + am);
            }
            gb[8] = 8.0e-5;
            gb[3] = 4.1e4;
            gb[4] = 5.5e4 / (1.0 + 0.4 * Math.Pow(mt, 4.0));
            gb[5] = 5.0;
            gb[6] = 3.0;
            gb[7] = Math.Pow(gb[3] / gb[4], 1.0 / (gb[5] - gb[6]));
            lums[6] = gb[4] * Math.Pow(gb[7], gb[5]);

            {
                double mc1 = he_mc_from_l(lums[2], gb);
                tscls[4] = tm + 1.0 / ((gb[5] - 1.0) * gb[8] * gb[4])
                           * Math.Pow(mc1, 1.0 - gb[5]);
                tscls[6] = tscls[4] - (tscls[4] - tm)
                           * Math.Pow(gb[7] / mc1, 1.0 - gb[5]);
                tscls[5] = tscls[6] + 1.0 / ((gb[6] - 1.0) * gb[8] * gb[3])
                           * Math.Pow(gb[7], 1.0 - gb[6]);
            }

            mtc = Math.Min(mt, 1.45 * mt - 0.31);
            if (mtc <= 0.0) mtc = mt;
            mcmax = Math.Min(mtc, Math.Max(MCH_, 0.773 * mt - 0.35));
            if (mcmax <= gb[7])
                tscls[14] = tscls[4] - 1.0 / ((gb[5] - 1.0) * gb[8] * gb[4])
                            * Math.Pow(mcmax, 1.0 - gb[5]);
            else
                tscls[14] = tscls[5] - 1.0 / ((gb[6] - 1.0) * gb[8] * gb[3])
                            * Math.Pow(mcmax, 1.0 - gb[6]);
            tscls[14] = Math.Max(tscls[14], tm);

            rzams = SseTimescales.seq_r_zhe(sp, mt);
            rx = rzams;
            rc = 0.0;
            menv = 1.0e-10;
            renv = 1.0e-10;
            k2 = 0.21;

            if (aj < tm) {
                double tau = aj / tm;
                double am, l_tms_he, mc_end;
                kw_out = 7;
                am = Math.Max(0.0, 0.85 - 0.08 * sp.mass);
                lum_out = lums[1] * (1.0 + 0.45 * tau + am * tau * tau);
                am = Math.Max(0.0, 0.4 - 0.22 * Math.Log10(mt));
                r_out = rx * (1.0 + am * (tau - Math.Pow(tau, 6.0)));
                l_tms_he = lums[2];
                if (l_tms_he <= gb[4] * Math.Pow(Math.Pow(gb[3] / gb[4],
                                                         1.0 / (gb[5] - gb[6])), gb[5]))
                    mc_end = (l_tms_he > 0.0) ? Math.Pow(l_tms_he / gb[4], 1.0 / gb[5]) : 0.0;
                else
                    mc_end = (l_tms_he > 0.0) ? Math.Pow(l_tms_he / gb[3], 1.0 / gb[6]) : 0.0;
                mc = Math.Min(mcmax, mc_end);
                if (mt < sp.zpars[10]) kw_out = 10;
            } else {
                double lum, rg;
                kw_out = 8;
                lum = SseTimescales.seq_l_from_t(sp, aj, gb[8], tscls[4], tscls[5], tscls[6], mt);
                r_out = SseTimescales.seq_r_he_hg(sp, mt, lum, rx, lums[2]);
                rg = SseTimescales.seq_r_he_gb(sp, lum);
                mc = he_mc_from_l(lum, gb);
                mtc = Math.Min(mt, 1.45 * mt - 0.31);
                if (mtc <= 0.0) mtc = mt;
                mcmax = Math.Min(mtc, Math.Max(MCH_, 0.773 * sp.mass - 0.35));
                mc = Math.Max(mc, mcmax);
                if (mc >= mcmax) {
                    double mcx;
                    aj = 0.0;
                    mc = Math.Max(mcmax, 1.0e-10);
                    if (mc < MCH_) {
                        if (sp.mass < 1.6) {
                            mt = Math.Max(mc, (mc + 0.31) / 1.45);
                            kw_out = 11;
                        } else {
                            mt = mc;
                            kw_out = 12;
                        }
                    } else {
                        if (mc < 2.5)       mcx = 0.161767 * mc + 1.067055;
                        else                mcx = 0.314154 * mc + 0.686088;
                        if (mc <= 5.0)      mt = mcx;
                        else if (mc < 7.6)  mt = mcx + (mc - 5.0) * (mt - mcx) / 2.6;
                        mc = mt;
                        kw_out = (mt <= PH_MXNS) ? 13 : 14;
                    }
                    lum_out = lum;
                    mc_out = mc;
                    rc_out = r_out;
                    menv_out = 1.0e-10;
                    renv_out = 1.0e-10;
                    k2_out = 0.21;
                    return;
                }
                if (r_out >= rg) {
                    kw_out = 9;
                    r_out = rg;
                }
            }

            if (kw_out >= 10) {
                double mcx;
                if (kw_out >= 10 && kw_out <= 12) {
                    mc = mt;
                    lum_out = 635.0 * mt * Math.Pow(sp.z, 0.4)
                              / Math.Pow((kw_out == 10) ? PH_AHE
                                         : ((kw_out == 11) ? PH_ACO : PH_AONE),
                                         1.4);
                    r_out = wd_radius(mt);
                    mc_out = mc;
                    rc_out = r_out;
                    menv_out = 1.0e-10;
                    renv_out = 1.0e-10;
                    k2_out = 0.21;
                    return;
                }
                if (kw_out == 13) {
                    mc = mt;
                    lum_out = 0.02 * Math.Pow(mt, 2.0 / 3.0) / Math.Pow(Math.Max(aj, 0.1), 2.0);
                    r_out = PH_RNS;
                    mc_out = mc;
                    rc_out = r_out;
                    menv_out = 1.0e-10;
                    renv_out = 1.0e-10;
                    k2_out = 0.21;
                    return;
                }
                if (kw_out == 14) {
                    mc = mt;
                    lum_out = 1.0e-10;
                    r_out = 4.24e-6 * mt;
                    mc_out = mc;
                    rc_out = r_out;
                    menv_out = 1.0e-10;
                    renv_out = 1.0e-10;
                    k2_out = 0.21;
                    return;
                }
                mcx = 0.0;
                _ = mcx;   /* C: (void)mcx; */
            }

            mc_out = mc;
            rc_out = rc;
            menv_out = menv;
            renv_out = renv;
            k2_out = k2;
        }

        /* ------------------------------------------------------------------ mrenv */

        /// <summary>
        /// 对流包层质量/半径与回转半径（论文第 7.2 节，phases.c: mrenv）。
        /// </summary>
        public static void mrenv(int kw, double mass, double mt, double mc,
                                 double lum, double rad, double rc,
                                 double aj, double tm, double ltms, double lbgb, double lhei,
                                 double rzams, double rtms, double rg,
                                 out double menv_out, out double renv_out, out double k2_out)
        {
            double logm, a, c, d, e;
            double k2z, k2bgb, k2g, menvg, renvg;
            double menv, renv, k2e;
            double teff = 0.0, tebgb = 0.0;   /* kw<=2 分支用，外层需要 */

            if (mass <= 0.0 || mt <= 0.0 || rad <= 0.0 || lum <= 0.0) {
                menv_out = 1.0e-10;
                renv_out = 1.0e-10;
                k2_out   = 0.21;
                return;
            }

            logm = Math.Log10(mass);
            a = Math.Min(0.81, Math.Max(0.68, 0.68 + 0.4 * logm));
            c = Math.Max(-2.5, Math.Min(-1.5, -2.5 + 5.0 * logm));
            d = -0.1;
            e = 0.025;

            k2z = Math.Min(0.21, Math.Max(0.09 - 0.27 * logm, 0.037 + 0.033 * logm));
            if (logm > 1.3)
                k2z = k2z - 0.055 * Math.Pow(logm - 1.3, 2.0);
            k2bgb = Math.Min(0.15, Math.Min(0.147 + 0.03 * logm, 0.162 - 0.04 * logm));

            if (kw >= 3 && kw <= 6) {
                double logmt = Math.Log10(mt);
                double f = 0.208 + 0.125 * logmt - 0.035 * logmt * logmt;
                double b = 1.0e4 * Math.Pow(mt, 1.5) / (1.0 + 0.1 * Math.Pow(mt, 1.5));
                double x = Math.Pow((lum - lbgb) / b, 2.0);
                double y = (f - 0.033 * Math.Log10(lbgb)) / k2bgb - 1.0;
                k2g = (f - 0.033 * Math.Log10(lum) + 0.4 * x)
                      / (1.0 + y * (lbgb / lum) + x);
            } else if (kw == 9) {
                double b = 3.0e4 * Math.Pow(mt, 1.5);
                double x = Math.Pow(Math.Max(0.0, lum / b - 0.5), 2.0);
                k2g = (k2bgb + 0.4 * x) / (1.0 + 0.4 * x);
            } else {
                k2g = k2bgb;
            }

            if (kw <= 2) {
                menvg = 0.5; renvg = 0.65;
            } else if (kw == 3 && lum < 3.0 * lbgb) {
                double x = Math.Min(3.0, lhei / lbgb);
                double tau = Math.Max(0.0, Math.Min(1.0, (x - lum / lbgb) / (x - 1.0)));
                menvg = 1.0 - 0.5 * tau * tau;
                renvg = 1.0 - 0.35 * tau * tau;
            } else {
                menvg = 1.0; renvg = 1.0;
            }

            if (rad < rg) {
                if (kw <= 6) {
                    double rzr = Math.Max(1.0, rad / rzams);
                    k2e = (k2z - e) * Math.Pow(rzr, c) + e * Math.Pow(rzr, d);
                } else if (kw == 7) {
                    double tau = (tm != 0.0) ? aj / tm : 0.0;
                    k2e = 0.08 - 0.03 * tau;
                } else {
                    k2e = 0.08 * rzams / rad;
                }

                {
                    double tauenv;
                    if (kw <= 2) {
                        teff = Math.Pow(lum, 0.25) / Math.Sqrt(rad);
                        tebgb = Math.Pow(lbgb, 0.25) / Math.Sqrt(rg);
                        tauenv = Math.Max(0.0, Math.Min(1.0, (tebgb / teff - a) / (1.0 - a)));
                    } else {
                        tauenv = Math.Max(0.0, Math.Min(1.0,
                                      (Math.Sqrt(rad / rg) - a) / (1.0 - a)));
                    }

                    if (tauenv > 0.0) {
                        menv = menvg * Math.Pow(tauenv, 5.0);
                        renv = renvg * Math.Pow(tauenv, 1.25);
                        if (kw <= 1) {
                            double x = Math.Max(0.0, Math.Min(1.0, (0.1 - logm) / 0.55));
                            double menvz = 0.18 * x + 0.82 * Math.Pow(x, 5.0);
                            double renvz = 0.4 * Math.Pow(x, 0.25) + 0.6 * Math.Pow(x, 10.0);
                            double y = 2.0 + 8.0 * x;
                            double tetms = Math.Pow(ltms, 0.25) / Math.Sqrt(rtms);
                            double tautms = Math.Max(0.0, Math.Min(1.0,
                                             (tebgb / tetms - a) / (1.0 - a)));
                            double menvt = menvg * Math.Pow(tautms, 5.0);
                            double renvt = renvg * Math.Pow(tautms, 1.25);
                            double tau = (tm != 0.0) ? aj / tm : 0.0;
                            if (tautms > 0.0 && menvt > 0.0 && renvt > 0.0) {
                                menv = menvz + Math.Pow(tau, y) * menv
                                       * (menvt - menvz) / menvt;
                                renv = renvz + Math.Pow(tau, y) * renv
                                       * (renvt - renvz) / renvt;
                            } else {
                                menv = 0.0;
                                renv = 0.0;
                            }
                            k2e = k2e + Math.Pow(tau, y) * Math.Pow(tauenv, 3.0)
                                  * (k2g - k2e);
                        } else {
                            k2e = k2e + Math.Pow(tauenv, 3.0) * (k2g - k2e);
                        }
                    } else {
                        menv = 0.0;
                        renv = 0.0;
                    }
                }
            } else {
                menv = menvg;
                renv = renvg;
                k2e = k2g;
            }

            menv = menv * (mt - mc);
            renv = renv * (rad - rc);
            menv_out = Math.Max(menv, 1.0e-10);
            renv_out = Math.Max(renv, 1.0e-10);
            k2_out   = k2e;
        }

        /* ------------------------------------------------------------------ 阈值 */

        /// <summary>深对流 MS 星的质量阈值（phases.c: mA_ms_threshold）。</summary>
        public static double mA_ms_threshold(Sequence sp)
        {
            return sp.mhook - 0.3;
        }

        /* ------------------------------------------------------------------ hrdiag */

        /// <summary>
        /// 核心例程：给定 (M, Z)、恒星类型 kw、年龄 aj、当前质量 mt、上一时刻核心
        /// 质量 mc，返回光度/半径/核心质量/核心半径/恒星类型与对流包层属性
        /// （phases.c: hrdiag）。
        /// mass 传 &lt;0 表示使用 sp.mass_in。
        /// </summary>
        public static Point hrdiag(Sequence sp, int kw, double aj, double mt,
                                   double mc, double mass)
        {
            Point p = new Point();
            double mass0, mass_f, mt0;
            double mc_prev, mcx_prev;
            double r, lum, rc, menv, renv, k2;
            double mtc;
            double[] tscls, lums, gb;
            double[] zp;
            double[] tscls_local = new double[16];
            double[] lums_local = new double[9];
            double[] gb_local = new double[10];
            double rg = 0.0;            /* 让 mrenv 调用处可见 */
            int i;

            /* 清零：C 版 memset(&out, 0, sizeof(out)) 对应 new Point() 的零初始化 */
            mass0 = (mass < 0.0) ? sp.mass_in : mass;
            mass_f = Math.Min(mass0, 100.0);
            mt0 = mt;
            _ = mt0;                    /* C: (void)mt0; */
            mt = Math.Min(mt, 100.0);
            mc_prev = mc;
            _ = mc_prev;                /* 与 C 一致：mc_prev 只赋值不读取 */
            mcx_prev = (kw == 6) ? Math.Max(mc, sp.mc_du) : mc;

            for (i = 0; i < 16; i++) tscls_local[i] = sp.tscls[i];
            for (i = 0; i < 9; i++)  lums_local[i]  = sp.lums[i];
            for (i = 0; i < 10; i++) gb_local[i]    = sp.gb[i];
            tscls = tscls_local;
            lums  = lums_local;
            gb    = gb_local;
            zp    = sp.zpars;

            p.mass = mass_f;
            p.mt   = mt;

            r = lum = 0.0;
            rc = 0.0;
            menv = renv = 1.0e-10;
            k2 = 0.21;
            mtc = Math.Min(mt, 1.45 * mt - 0.31);

            if (kw <= 6) {
                double tbagb_local = tscls[2] + tscls[3];
                double thg = tscls[1] - sp.tm;
                double rzams = SseTimescales.seq_rzams(sp, mass_f);
                double rtms = SseTimescales.seq_r_tms(sp, mass_f);
                rg = SseTimescales.seq_r_gb(sp, lums[3], mt);

                if (aj < tscls[1]) {
                    /* --------------------------------------- MS 或 HG */
                    if (aj < sp.tm) {
                        /* MS */
                        double tau = aj / sp.tm;
                        double thook = SseTimescales.seq_t_hook(sp, mass_f) * tscls[1];
                        double zeta = 0.01;
                        double tau1 = Math.Min(1.0, aj / thook);
                        double tau2 = Math.Max(0.0, Math.Min(1.0,
                                       (aj - (1.0 - zeta) * thook) / (zeta * thook)));
                        double dell = lhook(sp, mass_f);
                        double dtau = tau1 * tau1 - tau2 * tau2;
                        double alpha = lalph(sp, mass_f);
                        double beta = lbet(sp, mass_f);
                        double neta = lneta(sp, mass_f);
                        double lx = Math.Log10(lums[2] / lums[1]);
                        double xx;
                        double delr, ralpha, rbeta, rgamma, rx;
                        mc = 0.0;
                        if (tau > PH_TAUMIN)
                            xx = alpha * tau + beta * Math.Pow(tau, neta)
                                 + (lx - alpha - beta) * tau * tau - dell * dtau;
                        else
                            xx = alpha * tau + (lx - alpha) * tau * tau - dell * dtau;
                        lum = lums[1] * Math.Pow(10.0, xx);

                        delr = rhook(sp, mass_f);
                        dtau = tau1 * tau1 * tau1 - tau2 * tau2 * tau2;
                        ralpha = ralph(sp, mass_f);
                        rbeta  = rbet(sp, mass_f);
                        rgamma = rgamm(sp, mass_f);
                        rx = Math.Log10(rtms / rzams);
                        if (tau > PH_TAUMIN)
                            xx = ralpha * tau + rbeta * Math.Pow(tau, 10.0)
                                 + rgamma * Math.Pow(tau, 40.0)
                                 + (rx - ralpha - rbeta - rgamma)
                                   * tau * tau * tau - delr * dtau;
                        else
                            xx = ralpha * tau + (rx - ralpha) * tau * tau * tau
                                 - delr * dtau;
                        r = rzams * Math.Pow(10.0, xx);

                        if (mass_f < mA_ms_threshold(sp)) {
                            double rdgen = 0.0258 * Math.Pow(1.0 + sp.x_h, 5.0 / 3.0)
                                           * Math.Pow(mass_f, -1.0 / 3.0);
                            kw = 0;
                            r = Math.Max(rdgen, r);
                        } else {
                            kw = 1;
                        }
                    } else {
                        /* ----------------------------------- 赫氏空隙 */
                        double mcx = mc;
                        double neta, tau;
                        if (mass_f <= sp.mhef)
                            mc = SseTimescales.seq_mc_from_l(sp, lums[3], mass_f);
                        else if (mass_f <= sp.mfgb)
                            mc = SseTimescales.seq_mc_he_if(sp, mass_f, zp[9]);
                        else
                            mc = SseTimescales.seq_mc_he_if(sp, mass_f, zp[10]);
                        neta = mctms(sp, mass_f);
                        tau = (aj - sp.tm) / thg;
                        mc = ((1.0 - tau) * neta + tau) * mc;
                        mc = Math.Max(mc, mcx);

                        if (mc >= mt) {
                            aj = 0.0;
                            if (mass_f > sp.mhef) {
                                /* 零龄裸氦星 */
                                Sequence sp2 = new Sequence();
                                mc = 0.0;
                                SseTimescales.sequence_init(sp2, mt, sp.z);
                                helium_star(sp2, 7, 0.0, mt, mc,
                                            ref r, ref lum, ref kw, ref mc, ref rc,
                                            ref menv, ref renv, ref k2);
                                p.mass = mt;
                                p.mt = mt;
                                p.r = r; p.lum = lum; p.kw = kw;
                                p.mc = mc; p.rc = rc;
                                p.menv = menv; p.renv = renv; p.k2 = k2;
                                return p;
                            } else {
                                /* 零龄氦白矮星 */
                                mc = mt;
                                p.mass = mt;
                                kw = 10;
                            }
                        } else {
                            /* C 版此处写作 `double rx, tau;`：内层 tau 遮蔽了外层
                               已算好的 tau = (aj - tm)/thg 并且未初始化（原版 C 二进制
                               的 UB，表现为 HG 段 L/R 冻结在 TAMS 值）。按修正后的参考
                               实现 `double rx, tau = (aj - tm)/thg;`（即原意），这里
                               直接复用外层 tau，不重新声明。 */
                            double rx;
                            lum = lums[2] * Math.Pow(lums[3] / lums[2], tau);
                            if (mass_f <= sp.mfgb) {
                                rx = rg;
                            } else {
                                double rmin = SseTimescales.seq_r_min_he(sp, mass_f);
                                double ry = SseTimescales.seq_ragbf(sp, lums[4], mt);
                                double texp, tau2;
                                rx = Math.Min(rmin, ry);
                                if (mass_f <= MLP_) {
                                    texp = Math.Log(mass_f / MLP_) / Math.Log(sp.mfgb / MLP_);
                                    rx = rg;
                                    rx = rmin * Math.Pow(rx / rmin, texp);
                                }
                                tau2 = SseTimescales.seq_tau_bl(sp, mass_f);
                                if (tau2 < TINY_) rx = ry;
                            }
                            r = rtms * Math.Pow(rx / rtms, tau);
                            kw = 2;
                        }
                    }
                } else if (aj < tscls[2]) {
                    /* ----------------------------------- 第一巨星支 */
                    kw = 3;
                    lum = SseTimescales.seq_l_from_t(sp, aj, gb[1], tscls[4], tscls[5],
                                                     tscls[6], mass_f);
                    if (mass_f <= sp.mhef) {
                        mc = SseTimescales.seq_mc_from_l(sp, lum, mass_f);
                    } else {
                        double tau = (aj - tscls[1]) / (tscls[2] - tscls[1]);
                        double mcx = SseTimescales.seq_mc_he_if(sp, mass_f, zp[9]);
                        double mcy = SseTimescales.seq_mc_he_if(sp, mass_f, zp[10]);
                        mc = mcx + (mcy - mcx) * tau;
                    }
                    r = SseTimescales.seq_r_gb(sp, lum, mt);
                    rg = r;
                    if (mc >= mt) {
                        aj = 0.0;
                        if (mass_f > sp.mhef) {
                            Sequence sp2 = new Sequence();
                            mc = 0.0;
                            p.mass = mt;
                            SseTimescales.sequence_init(sp2, mt, sp.z);
                            helium_star(sp2, 7, 0.0, mt, mc,
                                        ref r, ref lum, ref kw, ref mc, ref rc,
                                        ref menv, ref renv, ref k2);
                            p.r = r; p.lum = lum; p.kw = kw;
                            p.mc = mc; p.rc = rc;
                            p.menv = menv; p.renv = renv; p.k2 = k2;
                            p.mt = mt;
                            return p;
                        } else {
                            mc = mt;
                            p.mass = mt;
                            kw = 10;
                        }
                    }
                } else if (aj < tbagb_local) {
                    /* ----------------------------------- 核心氦燃烧 */
                    double mcx, tau, texp, taul, tauh, tau2, lx, ly, rx, ry, rmin;
                    if (kw == 3 && mass_f <= sp.mhef) {
                        /* LM 星: 氦闪瞬时，重置时钟 */
                        aj = tscls[2];
                    }
                    if (mass_f <= sp.mhef)
                        mcx = SseTimescales.seq_mc_from_l(sp, lums[4], mass_f);
                    else
                        mcx = SseTimescales.seq_mc_he_if(sp, mass_f, zp[10]);
                    tau = (aj - tscls[2]) / tscls[3];
                    mc = mcx + (SseTimescales.seq_mc_bagb(sp, mass_f) - mcx) * tau;

                    if (mass_f <= sp.mhef) {
                        /* LM: 整个 CHeB 都是蓝的 */
                        lx = lums[5];
                        ly = lums[7];
                        rx = SseTimescales.seq_r_zahb(sp, mc, mt);
                        rg = SseTimescales.seq_r_gb(sp, lx, mt);
                        rmin = rg * Math.Pow(sp.rmin_ratio, mass_f / sp.mhef);
                        texp = Math.Min(Math.Max(0.4, rmin / rx), 2.5);
                        ry = SseTimescales.seq_ragbf(sp, ly, mt);
                        if (rmin < rx)
                            taul = Math.Pow(Math.Log(rx / rmin), 1.0 / 3.0);
                        else {
                            rmin = rx;
                            taul = 0.0;
                        }
                        tauh = Math.Pow(Math.Log(ry / rmin), 1.0 / 3.0);
                        tau2 = taul * (tau - 1.0) + tauh * tau;
                        r = rmin * Math.Exp(Math.Pow(Math.Abs(tau2), 3.0));
                        rg = rg + tau * (ry - rg);
                        lum = lx * Math.Pow(ly / lx, Math.Pow(tau, texp));
                    } else if (mass_f > sp.mfgb) {
                        /* HM: 先蓝后红 (超) 巨星 */
                        tau2 = SseTimescales.seq_tau_bl(sp, mass_f);
                        {
                            double tloop = tscls[2] + tau2 * tscls[3];
                            rmin = SseTimescales.seq_r_min_he(sp, mass_f);
                            rg = SseTimescales.seq_r_gb(sp, lums[4], mt);
                            rx = SseTimescales.seq_ragbf(sp, lums[4], mt);
                            rmin = Math.Min(rmin, rx);
                            if (mass_f <= MLP_) {
                                texp = Math.Log(mass_f / MLP_) / Math.Log(sp.mfgb / MLP_);
                                rx = rg;
                                rx = rmin * Math.Pow(rx / rmin, texp);
                            } else {
                                rx = rmin;
                            }
                            texp = Math.Min(Math.Max(0.4, rmin / rx), 2.5);
                            lum = lums[4] * Math.Pow(lums[7] / lums[4], Math.Pow(tau, texp));
                            if (aj < tloop) {
                                double ly2 = lums[4] * Math.Pow(lums[7] / lums[4],
                                                                Math.Pow(tau2, texp));
                                double ry2 = SseTimescales.seq_ragbf(sp, ly2, mt);
                                double taul2 = 0.0, tauh2 = 0.0;
                                if (Math.Abs(rmin - rx) > TINY_)
                                    taul2 = Math.Pow(Math.Log(rx / rmin), 1.0 / 3.0);
                                if (ry2 > rmin)
                                    tauh2 = Math.Pow(Math.Log(ry2 / rmin), 1.0 / 3.0);
                                tau = (aj - tscls[2]) / (tau2 * tscls[3]);
                                tau2 = taul2 * (tau - 1.0) + tauh2 * tau;
                                r = rmin * Math.Exp(Math.Pow(Math.Abs(tau2), 3.0));
                                rg = rg + tau * (ry2 - rg);
                            } else {
                                r = SseTimescales.seq_ragbf(sp, lum, mt);
                                rg = r;
                            }
                        }
                    } else {
                        /* IM: 先红巨星后蓝回圈 */
                        tau2 = 1.0 - SseTimescales.seq_tau_bl(sp, mass_f);
                        {
                            double tloop = tscls[2] + tau2 * tscls[3];
                            if (aj < tloop) {
                                tau = (tloop - aj) / (tau2 * tscls[3]);
                                lum = lums[5] * Math.Pow(lums[4] / lums[5],
                                                          Math.Pow(tau, 3.0));
                                r = SseTimescales.seq_r_gb(sp, lum, mt);
                                rg = r;
                            } else {
                                lx = lums[5];
                                ly = lums[7];
                                rx = SseTimescales.seq_r_gb(sp, lx, mt);
                                rmin = SseTimescales.seq_r_min_he(sp, mt);
                                texp = Math.Min(Math.Max(0.4, rmin / rx), 2.5);
                                ry = SseTimescales.seq_ragbf(sp, ly, mt);
                                if (rmin < rx)
                                    taul = Math.Pow(Math.Log(rx / rmin), 1.0 / 3.0);
                                else {
                                    rmin = rx;
                                    taul = 0.0;
                                }
                                tauh = Math.Pow(Math.Log(ry / rmin), 1.0 / 3.0);
                                tau = (aj - tloop)
                                      / (tscls[3] - (tloop - tscls[2]));
                                tau2 = taul * (tau - 1.0) + tauh * tau;
                                r = rmin * Math.Exp(Math.Pow(Math.Abs(tau2), 3.0));
                                rg = rx + tau * (ry - rx);
                                lum = lx * Math.Pow(ly / lx, Math.Pow(tau, texp));
                            }
                        }
                    }

                    if (mc >= mt) {
                        /* 演化后的 MS 裸氦星 */
                        double xx;
                        Sequence sp2 = new Sequence();
                        kw = 7;
                        xx = (aj - tscls[2]) / tscls[3];
                        SseTimescales.sequence_init(sp2, mt, sp.z);
                        p.mass = mt;
                        aj = xx * SseTimescales.seq_t_hems(sp2, mt);
                        helium_star(sp2, kw, aj, mt, mc,
                                    ref r, ref lum, ref kw, ref mc, ref rc,
                                    ref menv, ref renv, ref k2);
                        p.r = r; p.lum = lum; p.kw = kw;
                        p.mc = mc; p.rc = rc;
                        p.menv = menv; p.renv = renv; p.k2 = k2;
                        p.mt = mt;
                        return p;
                    }
                    kw = 4;
                } else {
                    /* ----------------------------------- AGB */
                    double mcbagb = SseTimescales.seq_mc_bagb(sp, mass_f);
                    double mcx = SseTimescales.seq_mc_from_t(sp, tbagb_local, gb[8], tscls[7],
                                                             tscls[8], tscls[9], mass_f);
                    double mcmax = Math.Max(Math.Max(MCH_, 0.773 * mcbagb - 0.35),
                                            1.05 * mcx);

                    if (aj < tscls[13]) {
                        /* 早期 AGB: H-耗尽核心质量恒定 */
                        mcx = SseTimescales.seq_mc_from_t(sp, aj, gb[8], tscls[7], tscls[8],
                                                          tscls[9], mass_f);
                        mc = mcbagb;
                        lum = SseTimescales.seq_l_from_mc(sp, mcx, mass_f);
                        if (mt <= mc) {
                            /* 演化后的裸氦星 */
                            Sequence sp2 = new Sequence();
                            kw = 9;
                            mt = mc;
                            p.mass = mt;
                            mc = mcx;
                            SseTimescales.sequence_init(sp2, mt, sp.z);
                            if (mc <= gb[7])
                                aj = tscls[4] - 1.0
                                     / ((gb[5] - 1.0) * gb[8] * gb[4])
                                     * Math.Pow(mc, 1.0 - gb[5]);
                            else
                                aj = tscls[5] - 1.0
                                     / ((gb[6] - 1.0) * gb[8] * gb[3])
                                     * Math.Pow(mc, 1.0 - gb[6]);
                            aj = Math.Max(aj, sp2.tm);
                            helium_star(sp2, 9, aj, mt, mc,
                                        ref r, ref lum, ref kw, ref mc, ref rc,
                                        ref menv, ref renv, ref k2);
                            p.r = r; p.lum = lum; p.kw = kw;
                            p.mc = mc; p.rc = rc;
                            p.menv = menv; p.renv = renv; p.k2 = k2;
                            p.mt = mt;
                            return p;
                        }
                        kw = 5;
                    } else {
                        /* TPAGB */
                        double lam, tau, mcx_du;
                        kw = 6;
                        mcx = SseTimescales.seq_mc_from_t(sp, aj, gb[2], tscls[10], tscls[11],
                                                          tscls[12], mass_f);
                        if (mcx <= 0.0) mcx = mcmax;
                        mcx = Math.Max(mcx, mcx_prev);
                        lum = SseTimescales.seq_l_from_mc(sp, mcx, mass_f);
                        lam = Math.Min(0.9, 0.3 + 0.001 * Math.Pow(mass_f, 5.0));
                        tau = tscls[13];
                        mcx_du = SseTimescales.seq_mc_from_t(sp, tau, gb[2], tscls[10],
                                                             tscls[11], tscls[12], mass_f);
                        mc = mcx - lam * (mcx - mcx_du);
                        mcx_prev = mcx;
                        mcmax = Math.Min(mt, mcmax);
                    }

                    r = SseTimescales.seq_ragbf(sp, lum, mt);
                    rg = r;

                    if (mcmax - mcx < TINY_) {
                        aj = 0.0;
                        mc = mcmax;
                        if (mc < MCH_) {
                            mt = mc;
                            kw = (mcbagb < 1.6) ? 11 : 12;
                            p.mass = mt;
                        } else if (mcbagb < 1.6) {
                            kw = 15;
                            aj = 0.0;
                            mt = 0.0;
                            lum = 1.0e-10;
                            r = 1.0e-10;
                            p.mass = 0.0;
                        } else {
                            double mcx2;
                            if (mc < 6.0)
                                mcx2 = 1.44;
                            else
                                mcx2 = 1.4512017 * mc - 6.5913737e-3 * mc * mc
                                       - 6.1073371;
                            mt = mcx2;
                            mc = mt;
                            kw = (mt <= PH_MXNS) ? 13 : 14;
                            p.mass = mt;
                        }
                    }
                }
            }

            p.mt = mt;

            /* ------------------------------------------------ 裸氦星 */
            if (kw >= 7 && kw <= 9) {
                helium_star(sp, kw, aj, mt, mc,
                            ref r, ref lum, ref kw, ref mc, ref rc,
                            ref menv, ref renv, ref k2);
                p.r = r; p.lum = lum; p.kw = kw;
                p.mc = mc; p.rc = rc;
                p.menv = menv; p.renv = renv; p.k2 = k2;
                return p;
            }

            /* ------------------------------------------------ 白矮星 */
            if (kw >= 10 && kw <= 12) {
                double xx;
                mc = mt;
                if (mc >= MCH_ && kw == 12) {
                    kw = 13;
                    mt = 1.3;
                } else if (mc >= MCH_ - 0.06 && kw <= 11) {
                    kw = 15;
                    aj = 0.0;
                    mt = 0.0;
                    lum = 1.0e-10;
                    r = 1.0e-10;
                } else {
                    xx = (kw == 10) ? PH_AHE : ((kw == 11) ? PH_ACO : PH_AONE);
                    lum = 635.0 * mt * Math.Pow(sp.z, 0.4)
                          / Math.Pow(xx * (aj + 0.1), 1.4);
                    r = wd_radius(mt);
                }
                p.mass = mt; p.mt = mt;
                p.r = r; p.lum = lum; p.kw = kw; p.mc = mc;
                return p;
            }

            /* ------------------------------------------------ 中子星 */
            if (kw == 13) {
                mc = mt;
                if (mc > PH_MXNS) {
                    kw = 14;
                    aj = 0.0;
                } else {
                    lum = 0.02 * Math.Pow(mt, 2.0 / 3.0) / Math.Pow(Math.Max(aj, 0.1), 2.0);
                    r = PH_RNS;
                }
                p.r = r; p.lum = lum; p.kw = kw; p.mc = mc;
                return p;
            }

            /* ------------------------------------------------ 黑洞 */
            if (kw == 14) {
                mc = mt;
                lum = 1.0e-10;
                r = 4.24e-6 * mt;
                p.r = r; p.lum = lum; p.kw = kw; p.mc = mc;
                return p;
            }

            /* -------------------------------------- H 星的核心半径 */
            {
                double lx = 0.0, rx = 0.0;
                if (kw <= 1 || kw == 7) {
                    rc = 0.0;
                } else if (kw <= 3) {
                    if (mass_f > sp.mhef) {
                        lx = SseTimescales.seq_l_zhe(sp, mc);
                        rx = SseTimescales.seq_r_zhe(sp, mc);
                        rc = rx;
                    } else {
                        lx = 300.0 * mc * Math.Pow(sp.z, 0.4)
                             / Math.Pow(PH_AHE * 0.1, 1.18);
                        rx = 0.0115 * Math.Sqrt(Math.Max(1.48204e-6,
                             Math.Pow(MCH_ / mc, 2.0 / 3.0)
                             - Math.Pow(mc / MCH_, 2.0 / 3.0)));
                        rc = 5.0 * rx;
                    }
                } else if (kw == 4) {
                    double tau = (aj - tscls[2]) / tscls[3];
                    double mc_he;
                    Sequence sp7 = new Sequence();
                    double am;
                    if (mass_f <= sp.mhef)
                        mc_he = SseTimescales.seq_mc_from_l(sp, lums[4], mt);
                    else
                        mc_he = SseTimescales.seq_mc_he_if(sp, mass_f, zp[10]);
                    SseTimescales.sequence_init(sp7, mc, sp.z);
                    am = Math.Max(0.0, 0.85 - 0.08 * mc);
                    lx = SseTimescales.seq_lzams(sp7, mc) * (1.0 + 0.45 * tau + am * tau * tau);
                    rx = SseTimescales.seq_r_zhe(sp7, mc);
                    am = Math.Max(0.0, 0.4 - 0.22 * Math.Log10(mc));
                    rx = rx * (1.0 + am * (tau - Math.Pow(tau, 6.0)));
                    rc = rx;
                    _ = mc_he;   /* C: (void)mc_he; 此值未被使用 */
                } else if (kw == 5) {
                    double tbagb_v = sp.t_bagb;
                    double tau = (sp.tn > tbagb_v)
                                 ? 3.0 * (aj - tbagb_v) / (sp.tn - tbagb_v) : 0.0;
                    double mcx = mc;
                    Sequence sp9 = new Sequence();
                    SseTimescales.sequence_init(sp9, mc, sp.z);
                    lx = SseTimescales.seq_l_from_mc(sp, mcx, mc);
                    if (tau < 1.0)
                        lx = lums[2] * Math.Pow(lx / lums[2], tau);
                    rx = SseTimescales.seq_r_zhe(sp9, mc);
                    {
                        double r1 = SseTimescales.seq_r_he_hg(sp9, mc, lx, rx, lums[2]);
                        double r2 = SseTimescales.seq_r_he_gb(sp9, lx);
                        rx = Math.Min(r1, r2);
                    }
                    rc = rx;
                } else if (kw <= 9) {
                    lx = 300.0 * mc * Math.Pow(sp.z, 0.4) / Math.Pow(PH_ACO * 0.1, 1.18);
                    rx = 0.0115 * Math.Sqrt(Math.Max(1.48204e-6,
                         Math.Pow(MCH_ / mc, 2.0 / 3.0)
                         - Math.Pow(mc / MCH_, 2.0 / 3.0)));
                    rc = 5.0 * rx;
                } else {
                    rc = r;
                    menv = 1.0e-10;
                    renv = 1.0e-10;
                    k2 = 0.21;
                }

                /* 小包层扰动 (eq. 83-84) */
                if (kw >= 2 && kw <= 9 && kw != 7 && mt > 0.0) {
                    double lum_pos = Math.Max(lum, 1.0e-30);
                    double mew = ((mt - mc) / mt)
                                 * Math.Min(5.0, Math.Max(1.2, Math.Pow(lum_pos / PH_LUM0,
                                                                        PH_KAP)));
                    if (kw >= 8)
                        mew = (mtc > 0.0) ? ((mtc - mc) / mtc) * 5.0 : 5.0;
                    if (mew < 1.0 && lx > 0.0 && rx > 0.0 && r > 0.0) {
                        double xx = lpert(mt, mew);
                        lum = lx * Math.Pow(lum_pos / lx, xx);
                        xx = (r <= rx) ? 0.0 : rpert(mt, mew, r, rx);
                        r = rx * Math.Pow(r / rx, xx);
                    }
                    rc = Math.Min(rc, r);
                }
            }

            /* ------------------------------------------ 对流包层 (mrenv) */
            if (kw >= 0 && kw < 10) {
                mrenv(kw, mass_f, mt, mc, lum, r, rc, aj, sp.tm,
                      lums[2], lums[3], lums[4],
                      SseTimescales.seq_rzams(sp, mass_f), SseTimescales.seq_r_tms(sp, mass_f), rg,
                      out menv, out renv, out k2);
            }

            p.r = r; p.lum = lum; p.kw = kw;
            p.mc = mc; p.rc = rc;
            p.menv = menv; p.renv = renv; p.k2 = k2;
            return p;
        }
    }
}
