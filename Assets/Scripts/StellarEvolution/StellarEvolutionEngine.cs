using System;
using Unity.Mathematics;
using UPB.SSE;

public static class StellarEvolutionEngine {
    public static readonly double
        // ~~~Constants~~~ //
        rSun = 695700,
        rEarth = 6371,
        lSun = 3.828e26,
        mSun = 1.989e30,
        tSunCore = 1.5e7,
        tPPCNOCross = 1.8e7,
        tSunSurf = 5773.15,
        c0 = 299792458,
        c02 = math.pow(c0, 2),
        MYR = 1e6 * 365.24 * 86400,

        MassMin = 0.077,
        MassMax = 100.0,
        DefaultStepDivisor = 1000.0;

    public static string name;
    public static double
        SE_Mass, SE_MassCore, SE_Metal,
        SE_Start, // 【新增】起点
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

    public static Track SSE_Track;
    public static int SE_Phase;
    public static double SE_MassCurrent;
    public static string SE_PhaseName;

    private static int _sseIndex;
    private static double _lifeMass = -1, _lifeZ = -1, _lifeValue, _lifeStep;

    public static int SE_StepCount => SSE_Track == null ? 0 : SSE_Track.points.Count;

    public static bool HasNextStep() {
        return SSE_Track != null && _sseIndex + 1 < SSE_Track.points.Count;
    }

    public static void InitSSE() {
        foreach (var ev in UPBEventBus.BeforeInitSSEEvent) {
            ev();
        }
        SE_Mass = math.clamp(SE_Mass, MassMin, MassMax);
        SE_Metal = math.clamp(SE_Metal, 1e-4, 0.03);

        double end = SE_End;
        double start = SE_Start;

        var track = new Track();

        // 始终从 0 开始算到残骸（保证 SSE 参数推导正确）
        int rc = SseEngine.engine_evolve(SE_Mass, SE_Metal, 0.0,
                                         SE_Step > 0 ? SE_Step : 0.0,
                                         1, 1, track);
        if (rc != 0 || track.points.Count == 0) {
            SSE_Track = null;
            return;
        }

        // 1. 裁剪终点
        double lastTime = track.points[^1].time;
        if (end > 0 && end < lastTime * 0.99) {
            int keep = track.points.Count;
            while (keep > 1 && track.points[keep - 1].time > end) keep--;
            track.points.RemoveRange(keep, track.points.Count - keep);
        }

        // 2. 裁剪起点：把 time < SE_Start 的点全删掉
        if (start > 0) {
            int removeCount = 0;
            while (removeCount < track.points.Count && track.points[removeCount].time < start) {
                removeCount++;
            }
            // 保留至少一个点
            if (removeCount < track.points.Count) {
                track.points.RemoveRange(0, removeCount);
            }
        }

        SSE_Track = track;
        _sseIndex = 0;
        SE_Tau = track.t_ms;
        ApplySSEPoint(0);
        SE_End = track.points[^1].time;
        foreach (var ev in UPBEventBus.AfterInitSSEEvent) {
            ev();
        }
    }

    public static void EvolveStep() {
        bool shouldContinue = HasNextStep();
        foreach (var ev in UPBEventBus.OnEvolveStepEvent) {
            EventResult res = ev(_sseIndex);
            if (res.Ret) {
                shouldContinue = false;
            }
        }
        if (!shouldContinue) {
            return;
        } else {
            _sseIndex++;
        }
        ApplySSEPoint(_sseIndex);
    }

    private static void ApplySSEPoint(int i) {
        TrackPoint p = SSE_Track.points[i];
        foreach (var ev in UPBEventBus.BeforeApplySSEPointEvent) {
            if (ev((p, i)).Ret) {
                return;
            }
        }
        SE_Year = p.time;
        SE_Luminosity = math.max(p.luminosity, 1e-8);
        SE_Radius = math.max(p.radius, 1e-6);
        SE_TemperatureSurf = p.temperature > 1.0 ? p.temperature : 1.0;
        SE_MassCurrent = p.mass;
        SE_MassCore = p.core_mass;
        SE_Phase = p.phase;
        SE_PhaseName = SseEngine.phase_name_cn(p.phase);
        SE_Spectrum = Spectrum.GetSpectrum(SE_Radius, p.mass, SE_TemperatureSurf);
        foreach (var ev in UPBEventBus.AfterApplySSEPointEvent) {
            ev((p, i));
        }
    }

    public static double GetMainSequenceLifespan(double mass, double metallicity) {
        var sp = new Sequence();
        if (SseTimescales.sequence_init(sp, math.clamp(mass, MassMin, MassMax),
                                        math.clamp(metallicity, 1e-4, 0.03)) != 0) {
            return 0.0;
        }
        return sp.tm;
    }

    public static double GetRedGiantLifespan(double mass, double metallicity) {
        return 0.2 * GetMainSequenceLifespan(mass, metallicity);
    }

    public static double GetLifespan(double mass, double metallicity) {
        if (UPBEventBus.GetLifespanEvent != null) {
            return UPBEventBus.GetLifespanEvent((mass, metallicity));
        }
        mass = math.clamp(mass, MassMin, MassMax);
        metallicity = math.clamp(metallicity, 1e-4, 0.03);
        if (mass == _lifeMass && metallicity == _lifeZ) return _lifeValue;

        var track = new Track();
        double life = 0.0;
        if (SseEngine.engine_evolve(mass, metallicity, 0.0, 0.0, 0, 1, track) == 0
            && track.last != null) {
            life = track.last.time;
        }
        _lifeMass = mass;
        _lifeZ = metallicity;
        _lifeValue = life;
        _lifeStep = track.t_ms > 0 ? track.t_ms / DefaultStepDivisor : 0.0;
        return life;
    }

    public static double GetDefaultStep(double mass, double metallicity) {
        mass = math.clamp(mass, MassMin, MassMax);
        metallicity = math.clamp(metallicity, 1e-4, 0.03);
        if (mass == _lifeMass && metallicity == _lifeZ && _lifeStep > 0) return _lifeStep;
        return GetMainSequenceLifespan(mass, metallicity) / DefaultStepDivisor;
    }
}

public class Spectrum {
    public bool isDwarf = false;
    public string type;  // Y T L | M K G F A B O
    public int subtype; // 0~9
    public string luminosity; // I~V
    public Spectrum(string type, int subtype, string luminosity) {
        foreach (var ev in UPBEventBus.OnInitSpectrum) {
            if (ev((this, type, subtype, luminosity)).Ret) {
                return;
            }
        }
        
        if (type != "Y" && type != "T" && type != "L" &&
            type != "M" && type != "K" && type != "G" &&
            type != "F" && type != "A" && type != "B" &&
            type != "O") {
            throw new Exception("Unknown Spectrum Type: " + type);
        }

        if (subtype < 0 || subtype > 9) {
            throw new Exception("Unknown Spectrum Subtype: " + subtype + " (Should in [0, 9])");
        }

        if (luminosity != "I" && luminosity != "II" && luminosity != "III" &&
            luminosity != "IV" && luminosity != "V") {
            throw new Exception("Unknown Spectrum Luminosity: " + luminosity + " (Should in {I, II, III, IV, V})");
        }

        if (type == "Y" || type == "T" || type == "L") {
            isDwarf = true;
        }

        this.type = type;
        this.subtype = subtype;
        this.luminosity = luminosity;
    }

    // 沙盘每帧要为 288 个表面斑块取色，原来每次都线性扫表 + 3 次 Substring/Convert，
    // 这里按 100K 一档缓存结果（表本身就是 100K 一档，取值完全相同）。
    private static readonly (float r, float g, float b)[] _colorCache =
        new (float r, float g, float b)[512];
    private static readonly bool[] _colorCacheValid = new bool[512];

    public static (float r, float g, float b) GetBlackbodyColor(double temperature) {
        if (UPBEventBus.GetBlackbodyColorEvent != null) {
            return UPBEventBus.GetBlackbodyColorEvent(temperature);
        }

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
        int cacheIndex = temp / 100;
        if (cacheIndex >= 0 && cacheIndex < _colorCache.Length &&
            _colorCacheValid[cacheIndex]) {
            return _colorCache[cacheIndex];
        }

        (float r, float g, float b) color = LookupBlackbodyColor(temp);
        if (cacheIndex >= 0 && cacheIndex < _colorCache.Length) {
            _colorCache[cacheIndex] = color;
            _colorCacheValid[cacheIndex] = true;
        }
        return color;
    }

    private static (float r, float g, float b) LookupBlackbodyColor(int temp) {
        var entries = ColorDatabase.Entries;

        for (int i = 0; i < entries.Count - 1; i++) {
            if (entries[i].Temp == temp) {
                return (HexToR(entries[i].Hex) / 255f, HexToG(entries[i].Hex) / 255f, HexToB(entries[i].Hex) / 255f);
            }
            if (entries[i].Temp < temp && entries[i + 1].Temp > temp) {
                return (HexToR(entries[i].Hex) / 255f, HexToG(entries[i].Hex) / 255f, HexToB(entries[i].Hex) / 255f);
            }
        }

        var (_, Hex, _, _) = entries[^1];
        return (HexToR(Hex) / 255f, HexToG(Hex) / 255f, HexToB(Hex) / 255f);
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
        } else if (20 < M && M <= 100) {
            return 14.62 * math.pow(M, 2.715) * (0.314 + A(Z));
        } else {
            return 0;
        }
    }

    public static double GetRadius0(double M, double Z) {
        if (0.077 <= M && M <= 12) {
            return 0.9 * math.pow(M, 0.9) * math.pow(Z / 0.02, -0.025);
        } else if (12 < M && M <= 100) {
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
        } else if (M > 25 && M <= 100) {
            // 上限：核心质量不超过总质量的 55%
            double core = math.pow(M / 10, 1.3) * 3.478 * math.pow(Z / 0.02, 0.1) * C(Z);
            return math.min(core, 0.55 * M);
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
        } else if (M > 20 && M <= 100) {
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