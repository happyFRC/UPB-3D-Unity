// SseZams.cs —— Tout et al. (1996) 零龄主序 (ZAMS) 拟合的 C# 机械翻译
// 严格对应 UPB-CLI src 下的 zams_coeffs.c / zams_coeffs.h
// 常数、系数表数值、分支、幂次与 C 版本完全一致，未做任何改写或优化。
//
//   zeta = log10(Z / 0.02)
//   每个多项式系数 c(zeta) = alpha + beta*zeta + gamma*zeta^2
//                            + eta*zeta^3 + mu*zeta^4
//
//   L_ZAMS = (l1 m^5.5 + l2 m^11) /
//            (l3 + m^3 + l4 m^5 + l5 m^7 + l6 m^8 + l7 m^9.5)
//
//   R_ZAMS = ((r1 m^2 + r2 m^6) m^0.5 + r3 m^11
//             + (r4 + r5 m^0.5) m^19) /
//            (r6 + r7 m^2 + (r8 m^8 + m^18 + r9 m^19) m^0.5)

using System;
using System.Collections.Generic;

namespace UPB.SSE {

    /// <summary>Tout et al. (1996) ZAMS 光度/半径拟合，见 zams_coeffs.c。</summary>
    public static class SseZams {

        /* --- L_ZAMS 系数: l1..l7 --- */
        private static readonly double[,] L_ZAMS = new double[,] {
            { 3.970417e-01, -3.2913574e-01,  3.4776688e-01,  3.7470851e-01,  9.011915e-02 },
            { 8.527626e+00, -2.441225973e+01, 5.643597107e+01, 3.706152575e+01, 5.4562406e+00 },
            { 2.5546e-04,   -1.23461e-03,   -2.3246e-04,    4.5519e-04,     1.6176e-04   },
            { 5.432889e+00, -8.62157806e+00, 1.344202049e+01, 1.451584135e+01, 3.39793084e+00 },
            { 5.563579e+00, -1.032345224e+01,1.944322980e+01, 1.897361347e+01, 4.16903097e+00 },
            { 7.8866060e-01,-2.90870942e+00, 6.54713531e+00,  4.05606657e+00,  5.3287322e-01 },
            { 5.86685e-03,  -1.704237e-02,   3.872348e-02,    2.570041e-02,    3.83376e-03  },
        };

        /* --- R_ZAMS 系数: r1..r9 (r6 是与 zeta 无关的常数) --- */
        private static readonly double[,] R_ZAMS = new double[,] {
            { 1.715359e+00,  6.2246212e-01, -9.2557761e-01, -1.16996966e+00, -3.0631491e-01 },
            { 6.597788e+00, -4.2450044e-01, -1.213339427e+01,-1.073509484e+01, -2.51487077e+00},
            { 1.008855000e+01,-7.11727086e+00,-3.167119479e+01,-2.424848322e+01, -5.33608972e+00},
            { 1.012495e+00,  3.2699690e-01, -9.23418e-03,   -3.876858e-02,    -4.12750e-03  },
            { 7.490166e-02,  2.410413e-02,   7.233664e-02,    3.040467e-02,     1.97741e-03  },
            { 1.077422e-02,  0.0,            0.0,             0.0,              0.0          },
            { 3.082234e+00,  9.447205e-01, -2.15200882e+00, -2.49219496e+00, -6.3848738e-01 },
            { 1.784778e+01, -7.4534569e+00, -4.896066856e+01,-4.005386135e+01, -9.09331816e+00},
            { 2.2582e-04,   -1.86899e-03,   3.88783e-03,     1.42402e-03,     -7.671e-05    },
        };

        /* alpha + beta*zeta + gamma*zeta^2 + eta*zeta^3 + mu*zeta^4
         * C 版签名 poly5(double zeta, const double row[5])；
         * 这里二维表以 (表, 行号) 传入，公式逐项对应。 */
        private static double poly5(double zeta, double[,] table, int row)
        {
            return table[row, 0] + zeta * (table[row, 1] + zeta * (table[row, 2]
                        + zeta * (table[row, 3] + zeta * table[row, 4])));
        }

        /// <summary>取 7 个 L_ZAMS 系数（按 l1..l7 顺序写入 out[0..6]）。</summary>
        public static void zams_lzams_coeffs(double z, double[] o)
        {
            double zeta = Math.Log10(z / 0.02);
            int i;
            for (i = 0; i < 7; i++)
                o[i] = poly5(zeta, L_ZAMS, i);
        }

        /// <summary>取 9 个 R_ZAMS 系数（按 r1..r9 顺序写入 out[0..8]）。</summary>
        public static void zams_rzams_coeffs(double z, double[] o)
        {
            double zeta = Math.Log10(z / 0.02);
            int i;
            for (i = 0; i < 9; i++)
                o[i] = poly5(zeta, R_ZAMS, i);
        }

        /// <summary>计算 ZAMS 光度 L_ZAMS (Lsun)。</summary>
        public static double zams_lzams(double m, double z)
        {
            double[] c = new double[7];
            double mx = Math.Sqrt(m);
            double num, den;

            zams_lzams_coeffs(z, c);

            num = c[0] * Math.Pow(m, 5.0) * mx + c[1] * Math.Pow(m, 11.0);
            den = c[2] + Math.Pow(m, 3.0)
                  + c[3] * Math.Pow(m, 5.0)
                  + c[4] * Math.Pow(m, 7.0)
                  + c[5] * Math.Pow(m, 8.0)
                  + c[6] * Math.Pow(m, 9.0) * mx;
            return num / den;
        }

        /// <summary>计算 ZAMS 半径 R_ZAMS (Rsun)。</summary>
        public static double zams_rzams(double m, double z)
        {
            double[] c = new double[9];
            double mx = Math.Sqrt(m);
            double num, den;

            zams_rzams_coeffs(z, c);

            num = (c[0] * m * m + c[1] * Math.Pow(m, 6.0)) * mx
                  + c[2] * Math.Pow(m, 11.0)
                  + (c[3] + c[4] * mx) * Math.Pow(m, 19.0);
            den = c[5] + c[6] * m * m
                  + (c[7] * Math.Pow(m, 8.0) + Math.Pow(m, 18.0) + c[8] * Math.Pow(m, 19.0)) * mx;
            return num / den;
        }
    }
}
