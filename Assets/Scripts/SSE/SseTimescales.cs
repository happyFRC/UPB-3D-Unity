// SseTimescales.cs —— SSE (Hurley, Pols & Tout 2000) 的 timescales.c 的 C# 机械翻译
//
// 对应 UPB-CLI src/timescales.c 与 src/timescales.h。
// 逐行对照：所有系数、公式、分支、clamp、数组下标（lums[1..8]、tscls[1..15]、
// gb[1..9]、zpars[1..14]）、sequence_init 的初始化顺序与默认值均与 C 版一致。
//
// 约定：
//   - C 的 `const Sequence *sp` -> `Sequence sp`（类引用）
//   - C 的 `sp->x`              -> `sp.x`
//   - 文件级 static 函数        -> private static
//   - math.h                    -> System.Math
//   - mass.h 的 zams 系数        -> SseZams.zams_lzams / SseZams.zams_rzams
//   - zcoeffs.h                 -> SseZcoeffs.zcoeffs

using System;
using System.Collections.Generic;

namespace UPB.SSE {

    /// <summary>SSE 演化时标与阶段边界量（timescales.c 的移植）。</summary>
    public static class SseTimescales {

        /* 与 Python 版一致的常量（timescales.h） */
        private const double MCH_ = 1.44;      /* Chandrasekhar 质量 */
        private const double MLP_ = 12.0;      /* R_HeI 的 HM 插值分界质量 */
        private const double TINY_ = 1.0e-14;

        /* ------------------------------------------------------------ 小工具 */

        /* base**exponent，base<=0 时返回 0 */
        private static double pow_safe(double bs, double exponent) {
            if (bs <= 0.0) return 0.0;
            return Math.Pow(bs, exponent);
        }

        /* Fortran 风格的实数幂：负底数时取实分支 (-|base|^exp) */
        private static double real_pow(double bs, double exponent) {
            if (bs < 0.0) return -Math.Pow(-bs, exponent);
            return Math.Pow(bs, exponent);
        }

        /* ------------------------------------------------------------ 构造 */

        /// <summary>
        /// 构造 Sequence。mass &gt; 0, z &gt; 0。成功返回 0，失败返回 -1。
        /// 对应 C 的 int sequence_init(Sequence *sp, double mass, double z)。
        /// </summary>
        public static int sequence_init(Sequence sp, double mass, double z) {
            double m, lz, lzd;
            double a_h, bb, dd, p, q;
            double[] lums = new double[9];
            double[] tscls = new double[16];
            double[] gb = new double[10];
            double[] zp = new double[15];
            int i;

            if (sp == null) return -1;
            if (mass <= 0.0) return -1;
            if (z <= 0.0) return -1;

            /* 清零 */
            for (i = 0; i < 9; i++) lums[i] = 0.0;
            for (i = 0; i < 16; i++) tscls[i] = 0.0;
            for (i = 0; i < 10; i++) gb[i] = 0.0;
            for (i = 0; i < 15; i++) zp[i] = 0.0;

            sp.mass_in = mass;
            sp.mass = (mass > 100.0) ? 100.0 : mass;
            sp.z = z;

            if (SseZcoeffs.zcoeffs(z, sp.c) != 0)
                return -1;

            sp.mhook = sp.c.Mhook;
            sp.mhef = sp.c.MHeF;
            sp.mfgb = sp.c.MFGB;
            sp.zeta = sp.c.zeta;
            lz = Math.Log10(z);
            lzd = sp.zeta + 1.0;
            sp.lz = lz;
            sp.lzd = lzd;
            sp.x_h = 0.76 - 3.0 * z;
            sp.y_he = 0.24 + 2.0 * z;

            m = sp.mass;

            /* --- ZAMS 与终端主序 --- */
            lums[1] = seq_lzams(sp, m);
            lums[2] = seq_l_tms(sp, m);
            tscls[1] = seq_t_bgb(sp, m);
            sp.tm = seq_t_ms(sp, m);

            /* --- 这个质量的巨支参数 --- */
            {
                double log_d0 = 5.37 + sp.zeta * 0.135;
                a_h = Math.Pow(10.0, Math.Max(-4.8, Math.Min(-5.7 + 0.8 * m, -4.1 + 0.14 * m)));
                bb = Math.Max(3.0e4, 500.0 + 1.75e4 * Math.Pow(m, 0.6));
                if (m <= 2.0) {
                    dd = Math.Pow(10.0, log_d0);
                    p = 6.0;
                    q = 3.0;
                } else if (m < 2.5) {
                    double dx = log_d0 - (0.975 * log_d0 - 0.18 * 2.5);
                    double d = log_d0 - dx * (m - 2.0) / 0.5;
                    dd = Math.Pow(10.0, d);
                    p = 6.0 - (m - 2.0) / 0.5;
                    q = 3.0 - (m - 2.0) / 0.5;
                } else {
                    double d = Math.Max(-1.0, 0.5 * log_d0 - 0.06 * m);
                    d = Math.Max(d, 0.975 * log_d0 - 0.18 * m);
                    dd = Math.Pow(10.0, d);
                    p = 5.0;
                    q = 2.0;
                }
            }
            gb[1] = a_h;
            gb[2] = 1.27e-5;
            gb[3] = bb;
            gb[4] = dd;
            gb[5] = p;
            gb[6] = q;
            gb[7] = Math.Pow(bb / dd, 1.0 / (p - q));      /* Mx */
            gb[8] = 8.0e-5;
            lums[6] = dd * Math.Pow(gb[7], p);             /* L(Mx) */

            lums[4] = seq_l_he_i(sp, m);
            lums[7] = seq_l_bagb(sp, m);

            /* 参考核心质量 (M = MHeF) */
            zp[9] = seq_mc_he_if(sp, sp.mhef,
                                 seq_mc_from_l(sp, seq_l_bgb(sp, sp.mhef), sp.mhef));
            zp[10] = seq_mc_he_if(sp, sp.mhef,
                                  seq_mc_from_l(sp, lums[4], sp.mhef));

            if (m <= sp.mfgb) {
                lums[3] = seq_l_bgb(sp, m);
                /* 巨支时标 (eq. 34-36) */
                tscls[4] = tscls[1] + 1.0 / ((p - 1.0) * a_h * dd)
                           * Math.Pow(dd / lums[3], (p - 1.0) / p);
                tscls[6] = tscls[4] - (tscls[4] - tscls[1])
                           * Math.Pow(lums[3] / lums[6], (p - 1.0) / p);
                tscls[5] = tscls[6] + 1.0 / ((q - 1.0) * a_h * bb)
                           * Math.Pow(bb / lums[6], (q - 1.0) / q);
                /* 氦点火时间 (eq. 44) */
                if (lums[4] <= lums[6]) {
                    tscls[2] = tscls[4] - 1.0 / ((p - 1.0) * a_h * dd)
                               * Math.Pow(dd / lums[4], (p - 1.0) / p);
                } else {
                    tscls[2] = tscls[5] - 1.0 / ((q - 1.0) * a_h * bb)
                               * Math.Pow(bb / lums[4], (q - 1.0) / q);
                }
                if (m <= sp.mhef) {
                    double mc1 = seq_mc_from_l(sp, lums[4], m);
                    sp.mc_he_i_val = mc1;
                    lums[5] = seq_l_zahb(sp, mc1, m);
                    tscls[3] = seq_t_he(sp, mc1, m);
                } else {
                    sp.mc_he_i_val = seq_mc_he_if(sp, m, zp[10]);
                    lums[5] = seq_l_min_he(sp, m);
                    tscls[3] = seq_t_he(sp, 0.0, m);   /* mc = -inf 时不用，见实现 */
                }
            } else {
                tscls[2] = tscls[1];
                tscls[3] = seq_t_he(sp, 0.0, m);
                lums[5] = lums[7];
                lums[3] = lums[4];
            }

            /* --- BGB 核心质量 --- */
            if (m <= sp.mhef)
                gb[9] = seq_mc_from_l(sp, lums[3], m);
            else if (m <= sp.mfgb)
                gb[9] = seq_mc_he_if(sp, m, zp[9]);
            else
                gb[9] = seq_mc_he_if(sp, m, zp[10]);
            sp.mc_bgb_val = gb[9];

            /* --- FAGB (EAGB) 时标 (eq. 63-70) --- */
            {
                double tbagb = tscls[2] + tscls[3];
                sp.t_bagb = tbagb;
                tscls[7] = tbagb + 1.0 / ((p - 1.0) * gb[8] * dd)
                           * Math.Pow(dd / lums[7], (p - 1.0) / p);
                tscls[9] = tscls[7] - (tscls[7] - tbagb)
                           * Math.Pow(lums[7] / lums[6], (p - 1.0) / p);
                tscls[8] = tscls[9] + 1.0 / ((q - 1.0) * gb[8] * bb)
                           * Math.Pow(bb / lums[6], (q - 1.0) / q);
            }

            /* --- 第二 dredge-up (eq. 81) --- */
            {
                double mcbagb = seq_mc_bagb(sp, m);
                double mc1 = mcbagb;
                if (mc1 >= 0.8 && mc1 < 2.25)
                    mc1 = 0.44 * mc1 + 0.448;
                sp.mc_du = mc1;
                lums[8] = seq_l_from_mc(sp, mc1, m);
                if (mc1 <= gb[7])
                    tscls[13] = tscls[7] - 1.0 / ((p - 1.0) * gb[8] * dd)
                                * Math.Pow(mc1, 1.0 - p);
                else
                    tscls[13] = tscls[8] - 1.0 / ((q - 1.0) * gb[8] * bb)
                                * Math.Pow(mc1, 1.0 - q);
                sp.t_tp = tscls[13];
            }

            /* --- SAGB (TPAGB) 时标 --- */
            {
                double mc1 = sp.mc_du;
                if (mc1 <= gb[7]) {
                    tscls[10] = tscls[13] + 1.0 / ((p - 1.0) * gb[2] * dd)
                                * Math.Pow(dd / lums[8], (p - 1.0) / p);
                    tscls[12] = tscls[10] - (tscls[10] - tscls[13])
                                * Math.Pow(lums[8] / lums[6], (p - 1.0) / p);
                    tscls[11] = tscls[12] + 1.0 / ((q - 1.0) * gb[2] * bb)
                                * Math.Pow(bb / lums[6], (q - 1.0) / q);
                } else {
                    tscls[10] = tscls[7];
                    tscls[12] = tscls[9];
                    tscls[11] = tscls[13] + 1.0 / ((q - 1.0) * gb[2] * bb)
                                * Math.Pow(bb / lums[8], (q - 1.0) / q);
                }
            }

            /* --- 达到 Mc,max 的时间 (eq. 75-76) --- */
            {
                double tau = tscls[2] + tscls[3];
                double mc2 = seq_mc_from_t(sp, tau, gb[8], tscls[7], tscls[8], tscls[9], m);
                double mcbagb = seq_mc_bagb(sp, m);
                double mc1 = sp.mc_du;
                double mcmax = Math.Max(Math.Max(MCH_, 0.773 * mcbagb - 0.35), 1.05 * mc2);
                sp.mc_sn = mcmax;
                if (mcmax <= mc1) {
                    if (mcmax <= gb[7])
                        tscls[14] = tscls[7] - 1.0 / ((p - 1.0) * gb[8] * dd)
                                    * Math.Pow(mcmax, 1.0 - p);
                    else
                        tscls[14] = tscls[8] - 1.0 / ((q - 1.0) * gb[8] * bb)
                                    * Math.Pow(mcmax, 1.0 - q);
                } else {
                    double lam = Math.Min(0.9, 0.3 + 0.001 * Math.Pow(m, 5.0));
                    double mcmax2 = (mcmax - lam * mc1) / (1.0 - lam);
                    if (mcmax2 <= gb[7])
                        tscls[14] = tscls[10] - 1.0 / ((p - 1.0) * gb[2] * dd)
                                    * Math.Pow(mcmax2, 1.0 - p);
                    else
                        tscls[14] = tscls[11] - 1.0 / ((q - 1.0) * gb[2] * bb)
                                    * Math.Pow(mcmax2, 1.0 - q);
                }
                tscls[14] = Math.Max(sp.t_bagb, tscls[14]);
                sp.tn = tscls[14];
            }

            /* --- LM 星 CHeB 最小半径比例 --- */
            {
                double rmin_hef = seq_r_min_he(sp, sp.mhef);
                double rgbf_zahb = seq_r_gb(sp,
                    seq_l_zahb(sp, seq_mc_from_l(sp, lums[4], sp.mhef), sp.mhef),
                    sp.mhef);
                sp.rmin_ratio = (rgbf_zahb != 0.0) ? rmin_hef / rgbf_zahb : 0.0;
            }

            /* 写回数组 */
            for (i = 0; i < 9; i++) sp.lums[i] = lums[i];
            for (i = 0; i < 16; i++) sp.tscls[i] = tscls[i];
            for (i = 0; i < 10; i++) sp.gb[i] = gb[i];
            for (i = 0; i < 15; i++) sp.zpars[i] = zp[i];

            return 0;
        }

        /* ============================================================ ZAMS 辅助 */

        public static double seq_lzams(Sequence sp, double m) {
            return SseZams.zams_lzams(m, sp.z);
        }

        public static double seq_rzams(Sequence sp, double m) {
            return SseZams.zams_rzams(m, sp.z);
        }

        /* ============================================================ MS/HG 边界量 */

        public static double seq_t_bgb(Sequence sp, double m) {
            ZCoeffs a = sp.c;
            return (a.a1 + a.a2 * Math.Pow(m, 4.0) + a.a3 * Math.Pow(m, 5.5)
                    + Math.Pow(m, 7.0))
                   / (a.a4 * m * m + a.a5 * Math.Pow(m, 7.0));
        }

        public static double seq_t_hook(Sequence sp, double m) {
            ZCoeffs a = sp.c;
            double mu = 1.0 - 0.01 * Math.Max(a.a6 / Math.Pow(m, a.a7),
                                             a.a8 + a.a9 / Math.Pow(m, a.a10));
            return Math.Max(mu, 0.5);
        }

        public static double seq_x_factor(Sequence sp) {
            double z = sp.z;
            double v1 = 0.95 - (10.0 / 3.0) * (z - 0.01);
            double v2 = Math.Min(0.99, 0.98 - (100.0 / 7.0) * (z - 0.001));
            return Math.Max(0.95, Math.Max(v1, v2));
        }

        public static double seq_t_ms(Sequence sp, double m) {
            return Math.Max(seq_t_hook(sp, m), seq_x_factor(sp) * seq_t_bgb(sp, m));
        }

        public static double seq_l_tms(Sequence sp, double m) {
            ZCoeffs a = sp.c;
            return (a.a11 * Math.Pow(m, 3.0) + a.a12 * Math.Pow(m, 4.0)
                    + a.a13 * Math.Pow(m, a.a16 + 1.8))
                   / (a.a14 + a.a15 * Math.Pow(m, 5.0) + Math.Pow(m, a.a16));
        }

        public static double seq_r_tms(Sequence sp, double m) {
            ZCoeffs a = sp.c;
            double a17 = a.a17;
            double r_lo, r_hi, mstar;

            /* 分支 lo */
            {
                double r = (a.a18 + a.a19 * Math.Pow(m, a.a21))
                           / (a.a20 + Math.Pow(m, a.a22));
                r_lo = Math.Max(r, 1.5 * seq_rzams(sp, m));
            }
            /* 分支 hi */
            r_hi = (a.c1 * Math.Pow(m, 3.0) + a.a23 * Math.Pow(m, a.a26)
                    + a.a24 * Math.Pow(m, a.a26 + 1.5))
                   / (a.a25 + Math.Pow(m, 5.0));

            if (m <= a17) return r_lo;
            mstar = a17 + 0.1;
            if (m >= mstar) return r_hi;
            {
                double r_lo_ = (a.a18 + a.a19 * Math.Pow(a17, a.a21))
                               / (a.a20 + Math.Pow(a17, a.a22));
                r_lo_ = Math.Max(r_lo_, 1.5 * seq_rzams(sp, a17));
                r_hi = (a.c1 * Math.Pow(mstar, 3.0) + a.a23 * Math.Pow(mstar, a.a26)
                        + a.a24 * Math.Pow(mstar, a.a26 + 1.5))
                       / (a.a25 + Math.Pow(mstar, 5.0));
                return r_lo_ + (r_hi - r_lo_) * (m - a17) / 0.1;
            }
        }

        public static double seq_l_bgb(Sequence sp, double m) {
            ZCoeffs a = sp.c;
            return (a.a27 * Math.Pow(m, a.a31) + a.a28 * Math.Pow(m, a.c2))
                   / (a.a29 + a.a30 * Math.Pow(m, a.c3) + Math.Pow(m, a.a32));
        }

        /* ================================================== 巨支 L-Mc 关系 (5.2) */

        /* 返回 (A_H, B, D, p, q) —— 这里只用 B, D, p, q */
        private static void gb_params(Sequence sp, double m,
                                      out double A_H, out double B, out double D,
                                      out double p, out double q) {
            double log_d0 = 5.37 + sp.zeta * 0.135;
            A_H = Math.Pow(10.0, Math.Max(-4.8, Math.Min(-5.7 + 0.8 * m, -4.1 + 0.14 * m)));
            B = Math.Max(3.0e4, 500.0 + 1.75e4 * Math.Pow(m, 0.6));
            if (m <= 2.0) {
                D = Math.Pow(10.0, log_d0);
                p = 6.0;
                q = 3.0;
            } else if (m < 2.5) {
                double dx = log_d0 - (0.975 * log_d0 - 0.18 * 2.5);
                double d = log_d0 - dx * (m - 2.0) / 0.5;
                D = Math.Pow(10.0, d);
                p = 6.0 - (m - 2.0) / 0.5;
                q = 3.0 - (m - 2.0) / 0.5;
            } else {
                double d = Math.Max(-1.0, 0.5 * log_d0 - 0.06 * m);
                d = Math.Max(d, 0.975 * log_d0 - 0.18 * m);
                D = Math.Pow(10.0, d);
                p = 5.0;
                q = 2.0;
            }
        }

        public static double seq_l_from_mc(Sequence sp, double mc, double m) {
            double A_H, B, D, p, q, mx;
            gb_params(sp, m, out A_H, out B, out D, out p, out q);
            mx = Math.Pow(B / D, 1.0 / (p - q));
            if (mc <= mx) return D * Math.Pow(mc, p);
            return B * Math.Pow(mc, q);
        }

        public static double seq_mc_from_l(Sequence sp, double lum, double m) {
            double A_H, B, D, p, q, lx;
            gb_params(sp, m, out A_H, out B, out D, out p, out q);
            lx = B * Math.Pow(Math.Pow(B / D, 1.0 / (p - q)), q);
            if (lum <= lx) return pow_safe(lum / D, 1.0 / p);
            return pow_safe(lum / B, 1.0 / q);
        }

        /* 内部: 通用 (exp, coeff, base) 计算 */
        private static void gb_time_args(Sequence sp, double t, double a_rate,
                                         double tinf1, double tinf2, double tx, double m,
                                         out double exp_out, out double coeff_out, out double base_out) {
            double A_H, B, D, p, q;
            gb_params(sp, m, out A_H, out B, out D, out p, out q);
            if (t <= tx) {
                exp_out = p;
                coeff_out = D;
                base_out = (p - 1.0) * a_rate * D * (tinf1 - t);
            } else {
                exp_out = q;
                coeff_out = B;
                base_out = (q - 1.0) * a_rate * B * (tinf2 - t);
            }
        }

        public static double seq_mc_from_t(Sequence sp, double t, double a_rate,
                                           double tinf1, double tinf2, double tx, double m) {
            double e, c, bse;
            gb_time_args(sp, t, a_rate, tinf1, tinf2, tx, m, out e, out c, out bse);
            if (bse <= 0.0) return 0.0;
            return pow_safe(bse, 1.0 / (1.0 - e));
        }

        public static double seq_l_from_t(Sequence sp, double t, double a_rate,
                                          double tinf1, double tinf2, double tx, double m) {
            double e, c, bse;
            gb_time_args(sp, t, a_rate, tinf1, tinf2, tx, m, out e, out c, out bse);
            if (bse <= 0.0) return 0.0;
            return c * pow_safe(bse, e / (1.0 - e));
        }

        /* =================================================== 巨支半径 (5.2/5.4) */

        public static double seq_r_gb(Sequence sp, double lum, double m) {
            ZCoeffs b = sp.c;
            double aa = Math.Min(b.b4 / Math.Pow(m, b.b5), b.b6 / Math.Pow(m, b.b7));
            return aa * (Math.Pow(lum, b.b1) + b.b2 * Math.Pow(lum, b.b3));
        }

        public static double seq_ragbf(Sequence sp, double lum, double m) {
            ZCoeffs b = sp.c;
            double m1 = sp.mhef - 0.2;
            double xx, b50, aa;

            if (m >= sp.mhef) xx = b.b55;
            else if (m >= m1) xx = 1.0 + 5.0 * (b.b55 - 1.0) * (m - m1);
            else xx = 1.0;
            b50 = xx * b.b3;

            if (m <= m1)
                aa = b.b56 + b.b57 * m;
            else if (m >= sp.mhef)
                aa = Math.Min(b.b51 / Math.Pow(m, b.b52), b.b53 / Math.Pow(m, b.b54));
            else {
                double a_lo = b.b56 + b.b57 * m1;
                double a_hi = Math.Min(b.b51 / Math.Pow(sp.mhef, b.b52),
                                       b.b53 / Math.Pow(sp.mhef, b.b54));
                aa = a_lo + 5.0 * (a_hi - a_lo) * (m - m1);
            }
            return aa * (Math.Pow(lum, b.b1) + b.b2 * Math.Pow(lum, b50));
        }

        public static double seq_r_agb(Sequence sp, double lum, double m) {
            return seq_ragbf(sp, lum, m);
        }

        /* ==================================================== CHeB 关系 (5.3) */

        public static double seq_l_he_i(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            if (m < sp.mhef) {
                double l_im_hef = (b.b11 + b.b12 * Math.Pow(sp.mhef, 3.8))
                                  / (b.b13 + sp.mhef * sp.mhef);
                double one_plus_alpha1 = (b.b9 * Math.Pow(sp.mhef, b.b10)) / l_im_hef;
                return b.b9 * Math.Pow(m, b.b10) / one_plus_alpha1;
            }
            return (b.b11 + b.b12 * Math.Pow(m, 3.8)) / (b.b13 + m * m);
        }

        public static double seq_l_he_ratio(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            return (b.b14 + b.b15 * Math.Pow(m, b.b17 + 0.1))
                   / (b.b16 + Math.Pow(m, b.b17));
        }

        public static double seq_l_min_he(Sequence sp, double m) {
            return seq_l_he_i(sp, m) * seq_l_he_ratio(sp, m);
        }

        public static double seq_r_min_he(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            double m28 = Math.Pow(m, b.b28);
            return (b.b24 * m + Math.Pow(b.b25 * m, b.b26) * m28)
                   / (b.b27 + m28);
        }

        public static double seq_mu_zahb(Sequence sp, double mc, double m) {
            if (sp.mhef == mc) return 0.0;
            return Math.Min(1.0, Math.Max(0.0, (m - mc) / (sp.mhef - mc)));
        }

        public static double seq_l_zahb(Sequence sp, double mc, double m) {
            ZCoeffs b = sp.c;
            double lzhe = seq_l_zhe(sp, mc);
            double l_min_hef = seq_l_min_he(sp, sp.mhef);
            double alpha2, mm;

            if (l_min_hef == lzhe) return lzhe;
            alpha2 = (b.b18 + lzhe - l_min_hef) / (l_min_hef - lzhe);
            mm = Math.Max(seq_mu_zahb(sp, mc, m), 1.0e-12);
            return lzhe + (1.0 + b.b20) / (1.0 + b.b20 * Math.Pow(mm, 1.6479))
                   * (b.b18 * Math.Pow(mm, b.b19))
                   / (1.0 + alpha2 * Math.Exp(15.0 * (m - sp.mhef)));
        }

        public static double seq_r_zahb(Sequence sp, double mc, double m) {
            ZCoeffs b = sp.c;
            double mm = Math.Max(seq_mu_zahb(sp, mc, m), 1.0e-12);
            double f = (1.0 + b.b21) * Math.Pow(mm, b.b22)
                       / (1.0 + b.b21 * Math.Pow(mm, b.b23));
            return (1.0 - f) * seq_r_zhe(sp, mc)
                   + f * seq_r_gb(sp, seq_l_zahb(sp, mc, m), m);
        }

        public static double seq_t_he_ms(Sequence sp, double mc) {
            /* (void)sp; */
            if (mc < 1.0e-12) mc = 1.0e-12;
            return (0.4129 + 18.81 * Math.Pow(mc, 4.0) + 1.853 * Math.Pow(mc, 6.0))
                   / Math.Pow(mc, 6.5);
        }

        public static double seq_t_he_ratio(Sequence sp, double m, double mc) {
            ZCoeffs b = sp.c;
            if (m <= sp.mhef) {
                double mm;
                if (sp.mhef != mc)
                    mm = Math.Max((sp.mhef - m) / (sp.mhef - mc), 1.0e-12);
                else
                    mm = 1.0;
                return (b.b39 + (seq_t_he_ms(sp, mc) - b.b39) * Math.Pow(mm, b.b40))
                       * (1.0 + b.b57 * Math.Exp(m * b.b56));
            }
            {
                double m5 = Math.Pow(m, 5.0);
                return (b.b41 * Math.Pow(m, b.b42) + b.b43 * m5) / (b.b44 + m5);
            }
        }

        public static double seq_thef_im(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            double m5 = Math.Pow(m, 5.0);
            return seq_t_bgb(sp, m) * (b.b41 * Math.Pow(m, b.b42) + b.b43 * m5)
                   / (b.b44 + m5);
        }

        public static double seq_thef_rel(Sequence sp, double m, double mc) {
            ZCoeffs b = sp.c;
            if (m <= sp.mhef) {
                double mm;
                if (mc < 0.0) {          /* mc = None 时用 0，等价于 Python */
                    if (m >= sp.mhef) return seq_thef_im(sp, m);
                    mc = 0.0;
                }
                if (sp.mhef != mc)
                    mm = Math.Max((sp.mhef - m) / (sp.mhef - mc), 1.0e-12);
                else
                    mm = 1.0;
                return (b.b39 + (seq_t_he_ms(sp, mc) - b.b39) * Math.Pow(mm, b.b40))
                       * (1.0 + b.b57 * Math.Exp(m * b.b56));
            }
            return seq_thef_im(sp, m);
        }

        public static double seq_t_he(Sequence sp, double mc, double m) {
            ZCoeffs b = sp.c;
            if (m <= sp.mhef) {
                double t_he_im_hef = seq_thef_rel(sp, sp.mhef, -1.0);
                double one_plus_alpha4 = t_he_im_hef / b.b39;
                return Math.Abs(seq_t_he_ratio(sp, m, mc) * one_plus_alpha4);
            }
            return Math.Abs(seq_thef_rel(sp, m, -1.0));
        }

        public static double seq_tau_bl(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            double t;

            if (m < sp.mhef) return 1.0;
            {
                double mr = sp.mhef / sp.mfgb;
                if (m <= sp.mfgb) {
                    double m1 = m / sp.mfgb;
                    double m2 = (m1 > 0.0)
                                ? Math.Max(Math.Log10(m1) / Math.Log10(mr), 1.0e-12) : 1.0e-12;
                    double alpha_bl = (1.0 - b.b45 * Math.Pow(mr, 0.414))
                                      * real_pow(Math.Log10(mr), -b.b46);
                    t = b.b45 * Math.Pow(m1, 0.414) + alpha_bl * real_pow(m2, b.b46);
                } else {
                    double r1 = 1.0 - seq_r_min_he(sp, m)
                                / seq_ragbf(sp, seq_l_he_i(sp, m), m);
                    r1 = Math.Max(r1, 1.0e-12);
                    t = (1.0 - b.b47) * Math.Pow(m, b.b48) * Math.Pow(r1, b.b49)
                        / seq_f_bl(sp, sp.mfgb);
                }
            }
            t = Math.Min(1.0, Math.Max(0.0, t));
            return (t < 1.0e-10) ? 0.0 : t;
        }

        public static double seq_f_bl(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            double rr = seq_ragbf(sp, seq_l_he_i(sp, m), m);
            if (rr <= 0.0) return 0.0;
            return Math.Pow(m, b.b48)
                   * Math.Pow(Math.Max(0.0, 1.0 - seq_r_min_he(sp, m) / rr), b.b49);
        }

        /* ==================================================== 核心质量 (5.2-5.4) */

        public static double seq_mc_bagb(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            return Math.Pow(b.b38 + b.b36 * Math.Pow(m, b.b37), 0.25);
        }

        public static double seq_mc_he_if(Sequence sp, double m, double mc_ref) {
            ZCoeffs a = sp.c;
            double mcbagb = seq_mc_bagb(sp, m);
            double a3 = Math.Pow(mc_ref, 4.0)
                        - a.c1_mcbgb * Math.Pow(sp.mhef, a.c2_mcbgb);
            return Math.Min(0.95 * mcbagb,
                            Math.Pow(a3 + a.c1_mcbgb * Math.Pow(m, a.c2_mcbgb), 0.25));
        }

        /* ============================================ 裸氦星 (Section 6.1) */

        public static double seq_l_zhe(Sequence sp, double m) {
            /* (void)sp; */
            /* _LZHE_A = 1.5262e4, p = 41/4, D = (0.0469, 31.18, 29.54) */
            double m15 = m * Math.Sqrt(m);
            return 1.5262e4 * Math.Pow(m, 41.0 / 4.0)
                   / (0.0469 + Math.Pow(m, 6.0) * (31.18 + m15 * (29.54 + m15)));
        }

        public static double seq_r_zhe(Sequence sp, double m) {
            /* (void)sp; */
            /* 0.2391 m^4.6 / (0.0065 + (0.162 + m) m^3) */
            return 0.2391 * Math.Pow(m, 4.6)
                   / (0.0065 + (0.162 + m) * Math.Pow(m, 3.0));
        }

        public static double seq_t_hems(Sequence sp, double m) {
            /* (void)sp; */
            return (0.4129 + 18.81 * Math.Pow(m, 4.0) + 1.853 * Math.Pow(m, 6.0))
                   / Math.Pow(m, 6.5);
        }

        public static double seq_r_he_hg(Sequence sp, double m, double lum,
                                         double rx, double lx) {
            double cm;
            /* (void)sp; */
            cm = 2.0e-3 * Math.Pow(m, 2.5) / (2.0 + Math.Pow(m, 5.0));
            return rx * Math.Pow(lum / lx, 0.2)
                   + 0.02 * (Math.Exp(cm * lum) - Math.Exp(cm * lx));
        }

        public static double seq_r_he_gb(Sequence sp, double lum) {
            /* (void)sp; */
            return 0.08 * Math.Pow(lum, 0.75);
        }

        /* ==================================================== L_BAGB (eq. 56) */

        public static double seq_l_bagb(Sequence sp, double m) {
            ZCoeffs b = sp.c;
            if (m < sp.mhef) {
                double l_im_hef = (b.b31 + b.b32 * Math.Pow(sp.mhef, b.b33 + 1.8))
                                  / (b.b34 + Math.Pow(sp.mhef, b.b33));
                double one_plus_alpha3 = (b.b29 * Math.Pow(sp.mhef, b.b30)) / l_im_hef;
                return b.b29 * Math.Pow(m, b.b30) / one_plus_alpha3;
            }
            return (b.b31 + b.b32 * Math.Pow(m, b.b33 + 1.8))
                   / (b.b34 + Math.Pow(m, b.b33));
        }
    }
}
