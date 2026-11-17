using System;

/// <summary>
/// 波段辐射功率：把普朗克定律按波数 σ 积分，用级数展开求值。
///
/// I = 2k⁴T⁴/(h³c²) · Σ_{n=1..N} ∫ x³·e^{-nx} dx，
/// x = hcσ/(kT)，σ = 1/λ 是波数（m⁻¹）。
/// 分部积分后每一项的原函数是
/// F_n(x) = e^{-nx}(x³/n + 3x²/n² + 6x/n³ + 6/n⁴)，
/// 所以波段 [λmin, λmax] 的积分 = Σ_n (F_n(x1) − F_n(x2))，x1 < x2。
///
/// 总功率 = I · 恒星表面积 · π（把辐射亮度换成半球出射度）。
/// </summary>
public static class SandboxSpectrum {
    public const double PlanckH = 6.62607015e-34;      // J·s
    public const double LightC = 2.99792458e8;         // m/s
    public const double BoltzmannK = 1.380649e-23;     // J/K

    /// <summary>求和项数（默认 10，足够收敛）。</summary>
    public const int SeriesTerms = 10;

    /// <summary>
    /// 黑体在 [lambdaMinNm, lambdaMaxNm] 波段的辐射功率（瓦）。
    /// </summary>
    public static double BandPowerW(double temperatureK, double radiusM,
            double lambdaMinNm, double lambdaMaxNm, int terms = SeriesTerms) {
        if (temperatureK <= 0.0 || radiusM <= 0.0) return 0.0;

        double minNm = Math.Min(lambdaMinNm, lambdaMaxNm);
        double maxNm = Math.Max(lambdaMinNm, lambdaMaxNm);
        if (minNm <= 0.0) return 0.0;

        double lambdaMinM = minNm * 1e-9;
        double lambdaMaxM = maxNm * 1e-9;

        // 波长上限对应波数下限（x1 小），波长下限对应波数上限（x2 大）。
        double factor = PlanckH * LightC / (BoltzmannK * temperatureK);
        double x1 = factor / lambdaMaxM;
        double x2 = factor / lambdaMinM;

        double sum = 0.0;
        for (int n = 1; n <= terms; n++) {
            sum += Antiderivative(x1, n) - Antiderivative(x2, n);
        }

        double t2 = temperatureK * temperatureK;
        double prefactor = 2.0 * BoltzmannK * BoltzmannK * BoltzmannK * BoltzmannK *
                           t2 * t2 /
                           (PlanckH * PlanckH * PlanckH * LightC * LightC);

        double area = 4.0 * Math.PI * radiusM * radiusM;
        return prefactor * sum * area * Math.PI;
    }

    /// <summary>F_n(x) = e^{-nx}(x³/n + 3x²/n² + 6x/n³ + 6/n⁴)。</summary>
    private static double Antiderivative(double x, int n) {
        double invN = 1.0 / n;
        double n2 = n * n;
        double n3 = n2 * n;
        double n4 = n2 * n2;
        double poly = x * x * x * invN +
                      3.0 * x * x / n2 +
                      6.0 * x / n3 +
                      6.0 / n4;
        return Math.Exp(-n * x) * poly;
    }
}
