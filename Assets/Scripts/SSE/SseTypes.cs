// SseTypes.cs —— SSE (Hurley, Pols & Tout 2000) C# 移植版的共享数据结构
// 严格对应 UPB-CLI src 下的头文件：
//   zcoeffs.h -> ZCoeffs
//   timescales.h -> Sequence
//   phases.h -> Point
//   evolve.h -> TrackPoint / Track / EvolveOptions
// 字段名与 C 版一一对应，便于与参考实现逐行比对。

using System.Collections.Generic;

namespace UPB.SSE {

    /// <summary>SSE 附录系数（随金属度 Z 变化），见 zcoeffs.h。</summary>
    public class ZCoeffs {
        public double z, zeta, sigma, rho;

        public double Mhook, MHeF, MFGB;

        public double a1, a2, a3, a4, a5;
        public double a6, a7, a8, a9, a10;
        public double a11, a12, a13, a14, a15, a16;
        public double a17, a18, a19, a20, a21, a22, a23, a24, a25, a26;
        public double a27, a28, a29, a30, a31, a32;
        public double a33, a34, a35, a36, a37;
        public double a38, a39, a40, a41, a42, a43, a44;
        public double a45, a46, a47, a48, a49, a50, a51, a52, a53;
        public double a54, a55, a56, a57;
        public double a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68;
        public double a69, a70, a71, a72, a73, a74;
        public double a75, a76, a77, a78, a79, a80, a81;

        public double b1, b2, b3, b4, b5, b6, b7;
        public double b9, b10, b11, b12, b13;
        public double b14, b15, b16, b17;
        public double b18, b19, b20, b21, b22, b23;
        public double b24, b25, b26, b27, b28;
        public double b29, b30, b31, b32, b33, b34;
        public double b36, b37, b38;
        public double b39, b40, b41, b42, b43, b44;
        public double b45, b46, b47, b48, b49;
        public double b51, b52, b53, b54, b55, b56, b57;

        public double c1, c2, c3, c1_mcbgb, c2_mcbgb, eta_max;
    }

    /// <summary>给定 (M, Z) 的序列参数，见 timescales.h。数组下标与 C 版一一对应。</summary>
    public class Sequence {
        public double mass_in, mass, z;

        public ZCoeffs c = new ZCoeffs();

        public double mhook, mhef, mfgb, zeta, lz, lzd, x_h, y_he;

        public double[] lums = new double[9];
        public double[] tscls = new double[16];
        public double[] gb = new double[10];
        public double[] zpars = new double[15];

        public double tm, t_bagb, t_tp, tn, mc_du, mc_sn,
                      mc_he_i_val, mc_bgb_val, rmin_ratio;
    }

    /// <summary>hrdiag 的输出，见 phases.h。</summary>
    public class Point {
        public double r, lum, mc, rc, mass, mt, menv, renv, k2;
        public int kw;
    }

    /// <summary>演化轨道上的一个记录点，见 evolve.h TrackPoint。</summary>
    public class TrackPoint {
        public double time, luminosity, radius, temperature, mass, core_mass;
        public int phase;
        public string spectral_type;
        public double core_temperature, menv, renv;
    }

    /// <summary>演化选项，见 evolve.h EvolveOptions。</summary>
    public class EvolveOptions {
        public double end_time;     // 停止年龄 (Myr)；<=0 表示演化到残骸
        public double step_size;    // 固定步长 (Myr)；<=0 表示默认 t_MS/1000
        public int max_steps;
        public int record;
        public int mass_loss;
        public int stop_at_remnant;

        public static EvolveOptions Default() {
            return new EvolveOptions {
                end_time = 0.0,
                step_size = 0.0,
                max_steps = 100000,
                record = 1,
                mass_loss = 1,
                stop_at_remnant = 1,
            };
        }
    }

    /// <summary>一次演化运行的结果，见 evolve.h Track。</summary>
    public class Track {
        public double mass, metallicity;

        public List<TrackPoint> points = new List<TrackPoint>();

        public double t_ms, t_bgb, t_he_i, t_he, t_bagb, t_tp, t_nuc;
        public double l_zams, r_zams, final_mass;

        public int success;
        public string error;
        public int steps;
        public int stagnated;

        public TrackPoint last;

        public int n_points => points?.Count ?? 0;
    }
}
