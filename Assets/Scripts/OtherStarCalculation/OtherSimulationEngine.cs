using System;

public static class OtherSimulationEngine {
    // ~~~常量~~~ //
    public const double rSun = 695700;      // km
    public const double lSun = 3.828e26;    // W
    public const double tSunSurf = 5773.15;

    // ~~~全局结果（对应 Python 的 gL1/gR1/gT1/gM1）~~~ //
    public static double GL1, GR1, GT1, GM1;

    // ~~~天体类型~~~ //
    public enum OtherStarType {
        HyperStar,      // 超巨星（30~300 M☉）
        BrownDwarf      // 褐矮星（10~75 M_Jup）
    }

    /// <summary>
    /// 超巨星膨胀极限
    /// 质量范围：30 < M <= 300 (M☉)
    /// </summary>
    public static (double L, double R, double T) CalculateHyperStar(double M) {
        if (M < 30 || M > 300.001)
            throw new ArgumentException("质量需在 30~300 M☉ 之间");

        double L, R;

        // 极限亮度
        if (M > 40 && M <= 300) {
            L = Math.Pow(M, 1.550968) * Math.Pow(10, 3.018638);
        } else { // 30 < M <= 40
            L = Math.Pow(M, 1.2006) * Math.Pow(10, 3.626585);
        }

        // 极限半径
        if (M > 30 && M <= 40) {
            R = Math.Pow(M, 0.73);
        } else { // 40 < M <= 300
            R = Math.Pow(M, 0.42818) * 3.04379;
        }

        // 表面温度
        double T = Math.Pow(L / (R * R), 0.25) * tSunSurf;

        GM1 = M; GL1 = L; GR1 = R; GT1 = T;
        return (L, R, T);
    }

    /// <summary>
    /// 褐矮星极限
    /// 质量范围：10 <= M_JUP <= 75 (木星质量)
    /// </summary>
    public static (double L, double R, double T) CalculateBrownDwarf(double M_JUP) {
        if (M_JUP < 10 || M_JUP > 75)
            throw new ArgumentException("质量需在 10~75 M_Jup 之间");

        // 极限亮度
        double L = Math.Pow(M_JUP / 1000.0, 3.6);

        // 极限半径
        double logM = Math.Log10(M_JUP);
        double R = Math.Pow(10, (-(logM * logM) + logM) * 0.073) / 10.0;

        // 表面温度
        double T = Math.Pow(L / (R * R), 0.25) * tSunSurf;

        GM1 = M_JUP; GL1 = L; GR1 = R; GT1 = T;
        return (L, R, T);
    }

    /// <summary>
    /// 统一入口：根据类型计算
    /// </summary>
    public static (double L, double R, double T) Calculate(
        OtherStarType type, double mass) {

        return type switch {
            OtherStarType.HyperStar => CalculateHyperStar(mass),
            OtherStarType.BrownDwarf => CalculateBrownDwarf(mass),
            _ => throw new ArgumentException("未知天体类型")
        };
    }

    /// <summary>
    /// 每种天体的合法质量范围
    /// </summary>
    public static (double min, double max) GetMassRange(OtherStarType type) {
        return type switch {
            OtherStarType.HyperStar => (30.0, 300.0),
            OtherStarType.BrownDwarf => (10.0, 75.0),
            _ => (0.0, 0.0)
        };
    }

    /// <summary>
    /// 质量单位显示名
    /// </summary>
    public static string GetMassUnit(OtherStarType type) {
        return type switch {
            OtherStarType.HyperStar => "MSun",
            OtherStarType.BrownDwarf => "MJup",
            _ => ""
        };
    }
}