using System;

public static class WhiteDwarfEvolutionEngine {
    // ~~~常量~~~ //
    public const double rSun = 695700;      // km
    public const double lSun = 3.828e26;    // W
    public const double mSun = 1.989e30;    // kg
    public const double tSunSurf = 5773.15; // K
    public const double MCh = 1.44;         // Chandrasekhar 质量 (src/timescales.h: MCH_)

    // ~~~新模型范围（与 src/engine.h 一致）~~~ //
    public const double MassMin = 0.077;    // 原恒星质量下限 M☉
    public const double MassMax = 100.0;    // 原恒星质量上限 M☉（主序范围提高到 100 M☉）
    public const double TexpoStart = 7.0;   // 冷却起点 log10(年) = 10^7 年
    public const double DefaultStepDivisor = 1000.0; // 默认步长 = 寿命的 1/1000

    // ~~~状态~~~ //
    public static string name;

    public static double
        WD_MassOrig,        // 原恒星质量 M☉
        WD_Mass,            // 白矮星质量 M☉
        WD_Radius0,         // 初始半径 km
        WD_Radius,          // 当前半径 km
        WD_Luminosity,      // 当前光度 L☉
        WD_TemperatureSurf, // 表面温度 K
        WD_Year,            // 当前年龄 年
        WD_Texpo,           // log10(Year)
        WD_TexpoEnd,        // 终点 texpo
        WD_StepTexpo,       // 对数步长 dex
        WD_StepTime;        // 每步现实秒数

    public static Spectrum WD_Spectrum;

    // 是否已初始化
    public static bool WD_Initialized = false;

    /// <summary>
    /// 默认对数步长 = 冷却寿命 / 1000（对应 src/evolve.h 的 t_MS/1000 约定）
    /// </summary>
    public static double GetDefaultStepTexpo(double texpoEnd) {
        double span = Math.Max(texpoEnd - TexpoStart, 0.0);
        return span / DefaultStepDivisor;
    }

    /// <summary>
    /// 冷却寿命（年）
    /// </summary>
    public static double GetCoolingLifespanYear(double texpoEnd) {
        return Math.Pow(10.0, texpoEnd);
    }

    /// <summary>
    /// 根据原恒星质量初始化白矮星
    /// </summary>
    public static void Init(double massOrig) {
        massOrig = Math.Clamp(massOrig, MassMin, MassMax);
        WD_MassOrig = massOrig;

        // Python: M_wd = M^0.439 * 0.57，上限取 Chandrasekhar 质量
        WD_Mass = Math.Min(Math.Pow(massOrig, 0.439) * 0.57, MCh);

        // Python: R_wd0 = 10000 / (M_wd / 0.701)^(1/3)  [km]
        WD_Radius0 = 10000.0 / Math.Pow(WD_Mass / 0.701, 1.0 / 3.0);
        WD_Radius = WD_Radius0;

        // 起点：10^7 年
        WD_Texpo = TexpoStart;
        WD_Year = Math.Pow(10, WD_Texpo);

        WD_Luminosity = 0.0;
        WD_TemperatureSurf = 0.0;
        WD_Spectrum = new Spectrum("O", 0, "V");

        WD_Initialized = true;
    }

    /// <summary>
    /// 推进一步。调用前必须已经 Init。
    /// </summary>
    public static void EvolveStep() {
        if (!WD_Initialized) return;

        double t = WD_Year;
        double M = WD_Mass;
        double L;

        // Python 三段式光度公式
        if (WD_Texpo <= 10.0) {
            // Mestel 冷却
            L = M / Math.Pow(t / 1e7, 1.4);
        } else if (WD_Texpo <= Math.Log10(2e10)) {
            // Debye 冷却
            L = M * Math.Pow(1000, -1.4) / Math.Pow(t / 1e10, 2.28);
        } else {
            // 深度结晶
            L = M * Math.Pow(1000, -1.4) / Math.Pow(2, 2.28)
                / Math.Pow(t / 2e10, 4);
        }

        WD_Luminosity = L;

        // Python: R_wd = R_wd0 * ((L/M)*0.1 + 0.9)
        WD_Radius = WD_Radius0 * ((L / M) * 0.1 + 0.9);

        // Python: Temp = (L / (R/700000)^2)^0.25 * 5773.15
        WD_TemperatureSurf = Math.Pow(
            L / Math.Pow(WD_Radius / 700000.0, 2), 0.25) * tSunSurf;

        // 光谱分类：半径换算成 R☉ 传给 Spectrum
        WD_Spectrum = Spectrum.GetSpectrum(
            WD_Radius / rSun,   // km → R☉
            WD_Mass,
            WD_TemperatureSurf);

        // 推进时间
        WD_Texpo += WD_StepTexpo;
        WD_Year = Math.Pow(10, WD_Texpo);
    }

    /// <summary>
    /// 是否已经到达终点
    /// </summary>
    public static bool IsFinished() {
        return WD_Texpo > WD_TexpoEnd;
    }

    /// <summary>
    /// 半径换算：km → R☉
    /// </summary>
    public static double RadiusRsun() {
        return WD_Radius / rSun;
    }
}
