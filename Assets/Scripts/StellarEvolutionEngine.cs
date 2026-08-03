using Unity.Mathematics;

public static class StellarEvolutionEngine {
    public static readonly double
        // ~~~Constants~~~ //
        // Radius: km
        // Luminosity: W
        // Mass: kg
        // Temperature: K
        // Speed: m/s
        rSun = 695700,
        rEarth = 6371,
        lSun = 3.828e26,
        mSun = 1.989e30,
        tSunCore = 1.5e7,
        tPPCNOCross = 1.8e7,
        tSunSurf = 5773.15,
        c0 = 299792458,
        c02 = math.pow(c0, 2),
        MYR = 1e6 * 365.24 * 86400
        ;

    public static string name;
    public static double
        SE_Mass, SE_MassCore, SE_Metal,
        SE_End, SE_Step, SE_StepTime, SE_Tau,
        SE_Radius, SE_Year, SE_TemperatureSurf, SE_TemperatureCore, SE_Luminosity,
        SE_X_H, SE_X_He, SE_AvgMolecularWeight,
        SE_PP, SE_CNO,
        SE_K;

    public static double
        SETAMS_Luminosity, SETAMS_Radius,
        SE_LuminosityCrit, SE_RadiusCrit;

    public static Spectrum
        SE_Spectrum;

    public static double GetTau(double mass) {
        if (0.077 <= mass && mass < 0.8) {
            return 10000 * math.pow(mass, -2.7) * (1.2 - 0.6 * mass);
        } else if (0.8 <= mass && mass < 1) {
            return 10000 * math.pow(mass, -3) * (2 - mass);
        } else if (mass == 1) {
            return 10000;
        } else if (1 < mass && mass <= 2) {
            return 25000.0 / 3.0 * math.pow(mass, -3);
        } else if (2 < mass && mass <= 10.0 / 3.0) {
            return 12500.0 / 3.0 * math.pow(mass, -2);
        } else if (10.0 / 3.0 < mass && mass <= 10) {
            return 125000.0 / 9.0 * math.pow(mass, -3);
        } else if (10 < mass && mass <= 12) {
            return 50.0 / 3.0 * math.pow(mass / 10.0, -1.542);
        } else if (12 < mass && mass <= 16) {
            return 50.0 / 3.0 * math.pow(mass / 10.0, -1.542) * (0.0375 * mass + 0.55);
        } else if (16 < mass && mass <= 30) {
            return 3714.0 / 575.0 * (27.0 / 280.0 * mass - 11.0 / 28.0) * math.pow(mass / 16.0, -1.93);
        }
        return 0.0;
    }

    public static double GetMainSequenceLifespan(double mass, double metallicity) {
        return GetTau(mass) * math.pow(metallicity / 0.02, 0.19953);
    }

    public static double GetRedGiantLifespan(double mass, double metallicity) {
        // TODO
        return 0.2 * GetMainSequenceLifespan(mass, metallicity);
    }

    public static void EvolveStep() {
        if (SE_Year <= 1.15 * SE_Tau) {
            double deltaM = 140 * (SE_Luminosity * lSun) * (SE_Step * MYR) / c02;
            SE_X_H -= deltaM / (SE_MassCore * mSun);
            SE_X_He = 1 - SE_X_H;
            double mu = 1 / (2 * SE_X_H + 0.75 * SE_X_He);
            double Tc = SE_TemperatureCore;
            SE_TemperatureCore *= 1 + (1.5 - 0.5 * (SE_Year / SE_Tau))
                               * SE_K * (mu - SE_AvgMolecularWeight) / SE_AvgMolecularWeight;
            SE_AvgMolecularWeight = mu;
            double v1 = SE_TemperatureCore / Tc;
            double v15 = math.pow(v1, 5);
            double s1 = SE_PP * v15;
            double s2 = SE_CNO * math.pow(v15, 3);
            double l0 = SE_Luminosity;
            SE_Luminosity = s1 + s2;
            SE_PP = s1; SE_CNO = s2;
            if (SE_Mass >= 0.8) {
                double alpha = 0.24 * (1 + math.log10(SE_Mass))
                               + 0.0007 * SE_Mass * SE_Year / 20;
                SE_Radius *= math.pow(SE_Luminosity / l0, alpha);
            } else {
                double log20m = math.log(SE_Mass) / math.log(20);
                SE_Radius *= math.pow(SE_Luminosity / l0, 0.24 * (1 + log20m));
            }
            SE_TemperatureSurf = tSunSurf * math.pow(SE_Luminosity / (SE_Radius * SE_Radius), 0.25);
        } else if (1.15 * SE_Tau < SE_Year && SE_Year <= SE_End) {
            if (SETAMS_Luminosity == 0 || SETAMS_Radius == 0) {
                SETAMS_Luminosity = SE_Luminosity;
                SETAMS_Radius = SE_Radius;
            }

            double alpha = (SE_Year - 1.15 * SE_Tau) / (0.05 * SE_Tau);
            SE_Luminosity = SETAMS_Luminosity *
                            math.pow(SE_LuminosityCrit / SETAMS_Luminosity, alpha);
            SE_Radius = SETAMS_Radius *
                        math.pow(SE_RadiusCrit / SETAMS_Radius, alpha);
            SE_TemperatureSurf = tSunSurf * math.pow(SE_Luminosity / (SE_Radius * SE_Radius), 0.25);
        }
        SE_Spectrum = Spectrum.GetSpectrum(SE_Radius, SE_Mass, SE_TemperatureSurf);
        SE_Year += SE_Step;
    }
}

public class Spectrum {
    public bool isDwarf = false;
    public string type;  // Y T L | M K G F A B O
    public int subtype; // 0~9
    public string luminosity; // I~V
    public Spectrum(string type, int subtype, string luminosity) {
        if (type != "Y" && type != "T" && type != "L" &&
            type != "M" && type != "K" && type != "G" &&
            type != "F" && type != "A" && type != "B" &&
            type != "O") {
            throw new System.Exception("Unknown Spectrum Type: " + type);
        }

        if (subtype < 0 || subtype > 9) {
            throw new System.Exception("Unknown Spectrum Subtype: " + subtype + " (Should in [0, 9])");
        }

        if (luminosity != "I" && luminosity != "II" && luminosity != "III" &&
            luminosity != "IV" && luminosity != "V") {
            throw new System.Exception("Unknown Spectrum Luminosity: " + luminosity + " (Should in {I, II, III, IV, V})");
        }

        if (type == "Y" || type == "T" || type == "L") {
            isDwarf = true;
        }

        this.type = type;
        this.subtype = subtype;
        this.luminosity = luminosity;
    }

    public static (float r, float g, float b) GetBlackbodyColor(double temperature) {
        if (temperature >= 50000) {
            return (0.529f, 0.745f, 1.0f);
        }

        if (temperature < 800) {
            double t = temperature / 800.0;
            float r = (float)(0 * (1 - t) + 255 * t);
            float g = (float)(0 * (1 - t) + 79 * t);
            float b = (float)(0 * (1 - t) + 0 * t);
            return (r / 255f, g / 255f, b / 255f);
        }

        int temp = (int)math.round(temperature / 100.0) * 100;
        var entries = ColorDatabase.Entries;

        for (int i = 0; i < entries.Count - 1; i++) {
            if (entries[i].Temp == temp) {
                return (HexToR(entries[i].Hex) / 255f, HexToG(entries[i].Hex) / 255f, HexToB(entries[i].Hex) / 255f);
            }
            if (entries[i].Temp < temp && entries[i + 1].Temp > temp) {
                return (HexToR(entries[i].Hex) / 255f, HexToG(entries[i].Hex) / 255f, HexToB(entries[i].Hex) / 255f);
            }
        }

        var last = entries[^1];
        return (HexToR(last.Hex) / 255f, HexToG(last.Hex) / 255f, HexToB(last.Hex) / 255f);
    }

    private static int HexToR(string hex) => System.Convert.ToInt32(hex.Substring(1, 2), 16);
    private static int HexToG(string hex) => System.Convert.ToInt32(hex.Substring(3, 2), 16);
    private static int HexToB(string hex) => System.Convert.ToInt32(hex.Substring(5, 2), 16);

    public override string ToString() {
        if (isDwarf) {
            return type + " Dwarf";
        } else {
            return type + subtype + luminosity;
        }
    }

    public static Spectrum GetSpectrum(double R, double M, double T) {
        double gSurface = M / (R * R) * 27400.0;
        string luminosity = "V";
        if (gSurface > 0) {
            double logG = math.log10(gSurface);
            if (logG > 4.0)
                luminosity = "V";
            else if (logG > 3.5)
                luminosity = "IV";
            else if (logG > 2.5)
                luminosity = "III";
            else if (logG > 1.5)
                luminosity = "II";
            else
                luminosity = "I";
        }

        int subtype;
        string type;

        if (T >= 30000) {
            type = "O";
            subtype = T > 60000 ? 0 : (int)(10 * (60000 - T) / (60000 - 30000));
            subtype = math.max(0, math.min(9, subtype));
        } else if (T >= 10000) {
            type = "B";
            subtype = (int)(10 * (30000 - T) / (30000 - 10000));
            subtype = math.max(0, math.min(9, subtype));
        } else if (T >= 7500) {
            type = "A";
            subtype = (int)(10 * (10000 - T) / (10000 - 7500));
            subtype = math.max(0, math.min(9, subtype));
        } else if (T >= 6000) {
            type = "F";
            subtype = (int)(10 * (7500 - T) / (7500 - 6000));
            subtype = math.max(0, math.min(9, subtype));
        } else if (T >= 5200) {
            type = "G";
            subtype = (int)(10 * (6000 - T) / (6000 - 5200));
            subtype = math.max(0, math.min(9, subtype));
        } else if (T >= 3700) {
            type = "K";
            subtype = (int)(10 * (5200 - T) / (5200 - 3700));
            subtype = math.max(0, math.min(9, subtype));
        } else if (T >= 2400) {
            type = "M";
            subtype = (int)(10 * (3700 - T) / (3700 - 2400));
            subtype = math.max(0, math.min(9, subtype));
        } else if (T >= 1300) {
            return new Spectrum("L", 0, luminosity);
        } else if (T >= 600) {
            return new Spectrum("T", 0, luminosity);
        } else {
            return new Spectrum("Y", 0, luminosity);
        }

        return new Spectrum(type, subtype, luminosity);
    }
}

public static class SEEHelper {
    public static double A(double Z) {
        return 0.6 + math.pow(1.85, -200 * Z);
    }

    public static double B(double Z) {
        return 0.5889 * math.pow(Z, -0.1353);
    }

    public static double C(double Z) {
        return A(Z) / A(0.02);
    }

    public static double P(double Z) {
        return 85000.0 / 12160.0 * Z + 0.860197;
    }

    public static double GetLuminosity0(double M, double Z) {
        if (0.077 <= M && M < 0.8) {
            return 0.689 * math.pow(M, 3.094) * A(Z);
        } else if (0.8 <= M && M <= 2) {
            return math.pow(M, 4.65) * A(Z);
        } else if (2 < M && M <= 10) {
            return (math.pow(M, 4.2) + 6.73) * A(Z);
        } else if (10 < M && M <= 16) {
            return (34.452 * math.pow(M, 2.542) + 6.73) * A(Z);
        } else if (16 < M && M <= 20) {
            return 11.2201845 * math.pow(M, 2.93) * A(Z);
        } else if (20 < M && M <= 30) {
            return 14.62 * math.pow(M, 2.715) * (0.314 + A(Z));
        } else {
            return 0;
        }
    }

    public static double GetRadius0(double M, double Z) {
        if (0.077 <= M && M <= 12) {
            return 0.9 * math.pow(M, 0.9) * math.pow(Z / 0.02, -0.025);
        } else if (12 < M && M <= 30) {
            return 8.424 * math.sqrt(M / 12) * math.pow(Z / 0.02, -0.025);
        } else {
            return 0;
        }
    }

    public static double GetMassCore0(double M, double Z) {
        if (M >= 0.077 && M < 0.8) {
            return (-0.6 * M + 1.2) * 0.57 * B(Z) * C(Z);
        } else if (M >= 0.8 && M <= 10) {
            return math.pow(M, 0.6) * 0.57 * B(Z) * C(Z);
        } else if (M > 10 && M <= 18) {
            return math.pow(M / 10, 0.616) * 2.269 * B(Z) * C(Z);
        } else if (M > 18 && M <= 20) {
            return math.pow(M / 10, 0.785) * 2.269 * B(Z) * C(Z);
        } else if (M > 20 && M <= 25) {
            return math.pow(M / 10, 1) * 4.09 * math.pow(Z / 0.02, 0.1) * C(Z);
        } else if (M > 25 && M <= 30) {
            return math.pow(M / 10, 1.3) * 3.478 * math.pow(Z / 0.02, 0.1) * C(Z);
        } else {
            return 0;
        }
    }

    public static double GetK(double M, double Z) {
        double p = P(Z);
        if (M >= 0.077 && M < 0.8) {
            return -2 * M + 2.47;
        } else if (M >= 0.8 && M <= 1.6) {
            return p * (-0.71 * M + 1.435);
        } else if (M > 1.6 && M <= 2.0) {
            return p * (-0.4475 * M + 1.015);
        } else if (M > 2.0 && M <= 3.0) {
            return -1.0 / 60.0 * M + 4.6 / 30.0 * p;
        } else if (M > 3.0 && M <= 4.5) {
            return (-0.02 * M + 0.14) * p;
        } else if (M > 4.5 && M <= 8.0) {
            return (-0.01 * M + 0.1) * p;
        } else if (M > 8.0 && M <= 10.0) {
            return 0.02 * p;
        } else if (M > 10 && M <= 18) {
            return 0.02 * math.pow(2, -(M - 10) / 6) * p;
        } else if (M > 18 && M <= 20) {
            return 0.004 * p;
        } else if (M > 20 && M <= 30) {
            return 0.024 * p;
        } else {
            return 0;
        }
    }

    public static double GetTemperatureCore0(double M) {
        return StellarEvolutionEngine.tSunCore * math.pow(M, 0.34);
    }

    public static double GetTemperatureSurf0(double M, double Z) {
        double R0 = GetRadius0(M, Z);
        return StellarEvolutionEngine.tSunSurf * math.pow(
            GetLuminosity0(M, Z) / (R0 * R0)
        , 0.25);
    }

    public static (double pp0, double cno0) GetPP0CNO0(double M, double Z) {
        double x = GetTemperatureCore0(M) / StellarEvolutionEngine.tPPCNOCross;
        double x5 = math.pow(x, 5);
        double x15 = math.pow(x, 15);
        double L0 = GetLuminosity0(M, Z);
        double pp0 = L0 * (x5 / (x5 + x15));
        double cno0 = L0 - pp0;
        return (pp0, cno0);
    }

    public static double GetLuminosityCrit(double M, double Z) {
        double a = (0.6 + math.pow(1.85, -200 * Z)) / (0.6 + math.pow(1.85, -4));
        if (M > 0.799 && M <= 1.6) {
            return (math.pow(M, 0.6) * 0.57 - 0.52) * 59000 * a;
        } else if (M > 1.6 && M <= 3) {
            return math.pow(M / 1.6, 0.5) * 13906.04 * a;
        } else if (M > 3 && M <= 10) {
            return math.pow(M / 3.0, 1.4037755925716) * 19041.63 * a;
        } else if (M > 10) {
            return math.pow(M / 10, 1.16) * 103206.85 * a;
        } else {
            return 0;
        }
    }

    public static double GetRadiusCrit(double M, double Z) {
        double a = (0.6 + math.pow(1.85, -200 * Z)) / (0.6 + math.pow(1.85, -4));
        return math.pow(M, 0.644) * 200 * math.pow(a, 0.5);
    }
}