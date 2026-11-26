using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>双精度三维向量。物理位置/速度只用 SI 单位。</summary>
public struct SandboxDVec3 {
    public double x;
    public double y;
    public double z;

    public SandboxDVec3(double x, double y, double z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static SandboxDVec3 Zero => new(0, 0, 0);
    public double SqrMagnitude => x * x + y * y + z * z;
    public double Magnitude => Math.Sqrt(SqrMagnitude);

    public static SandboxDVec3 operator +(SandboxDVec3 a, SandboxDVec3 b) =>
        new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static SandboxDVec3 operator -(SandboxDVec3 a, SandboxDVec3 b) =>
        new(a.x - b.x, a.y - b.y, a.z - b.z);
    public static SandboxDVec3 operator *(SandboxDVec3 a, double s) =>
        new(a.x * s, a.y * s, a.z * s);
    public static SandboxDVec3 operator /(SandboxDVec3 a, double s) =>
        new(a.x / s, a.y / s, a.z / s);
}

public enum SandboxPhysicsBodyKind {
    Star,
    Planet,
    Fragment
}

/// <summary>
/// 参与引力/热学模拟的天体。
/// 温度是独立状态量，不再维护一个全局内能账本。
/// </summary>
public sealed class SandboxPhysicsBody {
    public SandboxPhysicsBodyKind kind;
    public string name;
    public bool alive = true;

    public double massKg;
    public double radiusM;

    public SandboxDVec3 positionM;
    public SandboxDVec3 velocityMps;

    // 表面温度，K。恒星温度固定，行星温度由辐射/撞击直接改变。
    public double temperatureK;
    public double rotationPeriodDays = 25.38;
    public int rotationDirection = 1;
    public double greenhouseFactor;

    /// <summary>内部热源功率（W）：气态巨行星/褐矮星的核聚变，平摊到整个表面加热。</summary>
    public double internalHeatW;
    public Color trailColor = new(200f / 255f, 200f / 255f, 200f / 255f, 1f);

    // 行星表面按经纬网格拆分后的独立温度。
    public const int SurfaceLongitudeSegments = 24;
    public const int SurfaceLatitudeSegments = 12;
    public double[] surfaceTemperatures;
    public double rotationAngleRad;

    // 表面斑块几何缓存：只跟半径有关，自转只绕 Y 轴旋转，不必每步重算三角函数。
    internal double[] patchNormalX;
    internal double[] patchNormalY;
    internal double[] patchNormalZ;
    internal double[] patchAreaM2;
    internal double patchGeometryRadiusM = -1.0;
    internal double patchTotalAreaM2;

    public double luminosityW;
    public double lastAbsorbedPowerW;
    public double lastEmittedPowerW;
    public double lastAtmosphereHeatingPowerW;
    public double lastNetEmittedPowerW;

    public SandboxStar star;
    public SandboxPlanet planet;
    public SandboxFragment fragment;
    public SandboxTrail trail;

    public bool isOrbiting;
    public SandboxPhysicsBody orbitTarget;
    public double orbitRadiusM;
    public double orbitInclinationDeg;

    public bool positionLocked;
    public SandboxDVec3 preLockVelocity;
    public bool preLockOrbiting;
    public SandboxPhysicsBody preLockOrbitTarget;
    public double preLockOrbitRadius;

    // ~~~ 彩蛋（Coke_Car / HydrogenBox）~~~
    public bool ignoreGravity;      // 不参与引力（自己不被加速，也不吸引别人）
    public bool ignoreCollision;    // 不参与碰撞合并
    public bool ignoreThermal;      // 不参与热学计算
    public double impactGiftSun;    // 撞击它时给对方增加的质量（太阳质量）

    public bool IsStar => kind == SandboxPhysicsBodyKind.Star;

    public double KineticEnergyJ {
        get {
            double v2 = velocityMps.SqrMagnitude;
            return 0.5 * massKg * v2;
        }
    }

    /// <summary>外部引力势能，仅作诊断，不进入任何全局能量账本。</summary>
    public double ExternalGravitationalPotentialJ {
        get {
            double sum = 0.0;
            IReadOnlyList<SandboxPhysicsBody> all = SandboxPhysicsSystem.Bodies;
            for (int i = 0; i < all.Count; i++) {
                SandboxPhysicsBody other = all[i];
                if (other == null || other == this || !other.alive) continue;
                SandboxDVec3 delta = other.positionM - positionM;
                double r = Math.Sqrt(delta.SqrMagnitude +
                    SandboxPhysicsSystem.SofteningM * SandboxPhysicsSystem.SofteningM);
                sum += other.massKg / Math.Max(r, 1e-9);
            }
            return -SandboxPhysicsSystem.GravitationalConstant * massKg * sum;
        }
    }
}

/// <summary>
/// 沙盘内部物理世界。
///
/// 这里刻意不再维护“总能量账本”。各过程分家：
/// - 引力：只负责加速，机械能变化是自然结果；
/// - 恒星辐射/自身黑体辐射：用 RadiativeHeatCapacity 改变表面温度；
/// - 撞击：只把碰撞损失的机械动能，用 ImpactHeatCapacity 变成表面温升。
///
/// 这样温度不会为了凑一个全局守恒量而被清零或截断。
/// </summary>
public static class SandboxPhysicsSystem {
    public const double GravitationalConstant = 6.67430e-11;
    public const double StefanBoltzmann = 5.670374419e-8;
    public const double SofteningM = 1e-3;

    // ~~~ 沙盘设置面板里的全局开关 ~~~
    /// <summary>全局引力（关掉后所有天体不再互相吸引，按当前速度惯性漂移）。</summary>
    public static bool GravityEnabled = true;
    /// <summary>全局热学模拟总开关（关掉后温度不再更新）。</summary>
    public static bool ThermalEnabled = true;
    /// <summary>行星是否接收恒星辐射加热。</summary>
    public static bool ThermalReceiveRadiation = true;
    /// <summary>大气温室加热与热量再分配。</summary>
    public static bool ThermalAtmosphereHeating = true;
    /// <summary>黑体热辐射散热。</summary>
    public static bool ThermalRadiationLoss = true;
    /// <summary>撞击产生的加热。</summary>
    public static bool ThermalCollisionHeat = true;

    // 普通热学过程使用的表面热容。
    public const double RadiativeHeatCapacityJPerM2K = 1.67e8;

    // 撞击热容：由“静止、相距 100,000 km 的水星撞地球，最终地球 2800°C”逆解得到。
    public const double ImpactHeatCapacityJPerM2K = 9.535e12;

    // 表层横向热传导系数：把被照半球的热量摊到相邻斑块（无量纲，和斑块面积相乘）。
    public const double SurfaceConductionCoefficient = 0.45;

    public const double SunMassKg = 1.98847e30;
    public const double EarthMassKg = 5.9722e24;
    public const double SunRadiusM = CelestialScale.SunRadiusKm * 1000.0;
    public const double EarthRadiusM = CelestialScale.EarthRadiusKm * 1000.0;
    public const double SunLuminosityW = 3.828e26;

    private const double MetersPerUnit = CelestialScale.KmPerUnit * 1000.0;

    // 引力积分步长上限。时间倍率不高时和以前完全一样（≤1 小时）。
    private const double MaxSubstepSeconds = 3600.0;
    private const int MaxSubstepsPerFrame = 256;

    // 每个轨道周期至少积分这么多步。宽轨道系统靠它把步长放大，
    // 高倍率下才不会因为“每帧只有 256 步”而偷偷丢掉模拟时间。
    private const int OrbitStepsTarget = 200;
    private const double MinSubstepSeconds = 30.0;

    // 热学不需要和引力同样密：温度的时间尺度是小时~天，比轨道慢得多。
    // 默认 35 天/秒 时每帧 14 步，仍然按 1 小时积分；只有倍率更高时才放大步长。
    private const int MaxThermalSubstepsPerFrame = 24;

    private static readonly List<SandboxPhysicsBody> _bodies = new();
    private static readonly Dictionary<SandboxStar, SandboxPhysicsBody> _starBodies = new();
    private static readonly Dictionary<SandboxPlanet, SandboxPhysicsBody> _planetBodies = new();

    // 复用临时缓存，避免高时间倍率下每个子步产生大量 GC。
    private static readonly List<SandboxPhysicsBody> _radiationStars = new();
    private static readonly List<SandboxPhysicsBody> _radiationPlanets = new();
    private static double[] _accelX = System.Array.Empty<double>();
    private static double[] _accelY = System.Array.Empty<double>();
    private static double[] _accelZ = System.Array.Empty<double>();
    private static double[] _surfaceTempScratch = System.Array.Empty<double>();

    // 每个行星每个热学子步只算一次“到各恒星的单位方向 + 辐照度”。
    private static double[] _starDirX = System.Array.Empty<double>();
    private static double[] _starDirY = System.Array.Empty<double>();
    private static double[] _starDirZ = System.Array.Empty<double>();
    private static double[] _starFlux = System.Array.Empty<double>();

    public static IReadOnlyList<SandboxPhysicsBody> Bodies => _bodies;

    public static SandboxDVec3 TotalMomentum {
        get {
            SandboxDVec3 p = SandboxDVec3.Zero;
            foreach (SandboxPhysicsBody b in _bodies) {
                if (b != null && b.alive) p += b.velocityMps * b.massKg;
            }
            return p;
        }
    }

    public static double LastCollisionMomentumError { get; private set; }
    public static double LastCollisionMechanicalEnergyJ { get; private set; }
    public static double LastCollisionDeltaT { get; private set; }

    public static void Reset() {
        _bodies.Clear();
        _starBodies.Clear();
        _planetBodies.Clear();
        LastCollisionMomentumError = 0.0;
        LastCollisionMechanicalEnergyJ = 0.0;
        LastCollisionDeltaT = 0.0;
        SandboxTrail.ResetAll();
    }

    // ~~~ 注册 / 同步 ~~~ //

    public static SandboxPhysicsBody RegisterStar(SandboxStar star) {
        if (star == null || star.snapshot == null) return null;
        if (_starBodies.TryGetValue(star, out SandboxPhysicsBody existing)) return existing;

        SandboxPhysicsBody body = new() {
            kind = SandboxPhysicsBodyKind.Star,
            name = star.starName,
            star = star,
            massKg = star.snapshot.mass * SunMassKg,
            radiusM = star.snapshot.radiusRsun * SunRadiusM,
            positionM = LogicalToMeters(star.logicalX, star.logicalY, star.logicalZ),
            temperatureK = star.snapshot.temperatureK,
            rotationPeriodDays = star.snapshot.rotationPeriodDays,
            luminosityW = star.snapshot.luminosityLsun * SunLuminosityW
        };
        _bodies.Add(body);
        _starBodies[star] = body;
        body.trail = SandboxTrail.Attach(body);
        return body;
    }

    public static SandboxPhysicsBody RegisterPlanet(SandboxPlanet planet) {
        if (planet == null || planet.snapshot == null) return null;
        if (_planetBodies.TryGetValue(planet, out SandboxPhysicsBody existing)) return existing;

        SandboxPhysicsBody body = new() {
            kind = SandboxPhysicsBodyKind.Planet,
            name = planet.planetName,
            planet = planet,
            massKg = planet.snapshot.massEarth * EarthMassKg,
            radiusM = planet.snapshot.radiusEarth * EarthRadiusM,
            positionM = LogicalToMeters(planet.logicalX, planet.logicalY, planet.logicalZ),
            temperatureK = planet.snapshot.temperatureK,
            rotationPeriodDays = planet.snapshot.rotationPeriodDays,
            rotationDirection = planet.snapshot.rotationDirection,
            greenhouseFactor = planet.snapshot.greenhouseFactor,
            trailColor = planet.snapshot.trailColor,
            ignoreGravity = planet.snapshot.ignoreGravity,
            ignoreCollision = planet.snapshot.ignoreCollision,
            ignoreThermal = planet.snapshot.ignoreThermal,
            impactGiftSun = planet.snapshot.impactGiftSun
        };
        EnsureSurfaceTemperatures(body);
        _bodies.Add(body);
        _planetBodies[planet] = body;
        body.trail = SandboxTrail.Attach(body);
        return body;
    }

    public static SandboxPhysicsBody GetBody(SandboxStar star) {
        if (star == null) return null;
        _starBodies.TryGetValue(star, out SandboxPhysicsBody body);
        return body;
    }

    public static SandboxPhysicsBody GetBody(SandboxPlanet planet) {
        if (planet == null) return null;
        _planetBodies.TryGetValue(planet, out SandboxPhysicsBody body);
        return body;
    }

    public static void Unregister(SandboxStar star) {
        if (star == null) return;
        if (_starBodies.TryGetValue(star, out SandboxPhysicsBody body)) {
            body.alive = false;
            _bodies.Remove(body);
            _starBodies.Remove(star);
            SandboxTrail.Remove(body);
        }
    }

    public static void Unregister(SandboxPlanet planet) {
        if (planet == null) return;
        if (_planetBodies.TryGetValue(planet, out SandboxPhysicsBody body)) {
            body.alive = false;
            _bodies.Remove(body);
            _planetBodies.Remove(planet);
            SandboxTrail.Remove(body);
        }
    }

    public static void SyncPosition(SandboxStar star) {
        if (star != null && _starBodies.TryGetValue(star, out SandboxPhysicsBody b)) {
            b.positionM = LogicalToMeters(star.logicalX, star.logicalY, star.logicalZ);
        }
    }

    public static void SyncPosition(SandboxPlanet planet) {
        if (planet != null && _planetBodies.TryGetValue(planet, out SandboxPhysicsBody b)) {
            b.positionM = LogicalToMeters(planet.logicalX, planet.logicalY, planet.logicalZ);
        }
    }

    public static void SyncStar(SandboxStar star) {
        if (star == null || star.snapshot == null) return;
        if (!_starBodies.TryGetValue(star, out SandboxPhysicsBody b)) {
            RegisterStar(star);
            return;
        }
        b.massKg = star.snapshot.mass * SunMassKg;
        b.radiusM = star.snapshot.radiusRsun * SunRadiusM;
        b.temperatureK = star.snapshot.temperatureK;
        b.rotationPeriodDays = star.snapshot.rotationPeriodDays;
        b.luminosityW = star.snapshot.luminosityLsun * SunLuminosityW;
        b.name = star.starName;
    }

    public static void SyncPlanet(SandboxPlanet planet) {
        if (planet == null || planet.snapshot == null) return;
        if (!_planetBodies.TryGetValue(planet, out SandboxPhysicsBody b)) {
            RegisterPlanet(planet);
            return;
        }
        b.massKg = planet.snapshot.massEarth * EarthMassKg;
        b.radiusM = planet.snapshot.radiusEarth * EarthRadiusM;
        b.temperatureK = planet.snapshot.temperatureK;
        b.rotationPeriodDays = planet.snapshot.rotationPeriodDays;
        b.rotationDirection = planet.snapshot.rotationDirection;
        b.greenhouseFactor = planet.snapshot.greenhouseFactor;
        // 褐矮星/气态巨行星的核聚变：L = (M/1000)^3.6 L☉，作为内部热源参与热学。
        double massJup = planet.snapshot.massEarth / 317.8;
        // SpecialPlanets 不用核聚变公式，内部热源为 0。
        b.internalHeatW = !planet.snapshot.IsSpecial && massJup > 10.0
            ? Math.Pow(massJup / 1000.0, 3.6) * SunLuminosityW
            : 0.0;
        b.trailColor = planet.snapshot.trailColor;
        b.name = planet.planetName;
        EnsureSurfaceTemperatures(b);
    }

    /// <summary>改质量/温度后强制把整个表面温度场重置成给定值（否则旧的斑块温度会把新温度拉回去）。</summary>
    public static void ResetSurfaceTemperature(SandboxPlanet planet, double temperatureK) {
        if (planet == null) return;
        if (!_planetBodies.TryGetValue(planet, out SandboxPhysicsBody body)) return;
        body.temperatureK = temperatureK;
        if (body.surfaceTemperatures != null) {
            for (int i = 0; i < body.surfaceTemperatures.Length; i++) {
                body.surfaceTemperatures[i] = temperatureK;
            }
        }
    }

    private static void EnsureSurfaceTemperatures(SandboxPhysicsBody body) {
        int count = SandboxPhysicsBody.SurfaceLongitudeSegments *
                    SandboxPhysicsBody.SurfaceLatitudeSegments;
        if (body.surfaceTemperatures == null ||
            body.surfaceTemperatures.Length != count) {
            body.surfaceTemperatures = new double[count];
            for (int i = 0; i < count; i++) {
                body.surfaceTemperatures[i] = body.temperatureK;
            }
        }
    }

    // ~~~ 主循环 ~~~ //

    public static double Tick(double simulationSeconds) {
        if (simulationSeconds <= 0.0) return 0.0;
        if (_bodies.Count == 0) return simulationSeconds;

        // 读档/撞击后偶尔会出现 NaN/Infinity，一旦进入积分就会污染所有天体。
        // 每帧开头把异常数值就地复位，宁可把那个天体钉在原点，也不要整场崩掉。
        SanitizeBodyValues();

        // 步长上限由“最短局部轨道周期 / OrbitStepsTarget”决定：
        // 宽轨道系统可以用很大的步长，靠近恒星的行星自动用小步长。
        double accuracyDt = ComputeAccuracySubstepSeconds();
        int preferredSteps = (int)Math.Ceiling(simulationSeconds / MaxSubstepSeconds);
        int requiredSteps = accuracyDt == double.MaxValue
            ? 1
            : (int)Math.Ceiling(simulationSeconds / accuracyDt);

        int steps;
        if (preferredSteps <= MaxSubstepsPerFrame) {
            // 低倍率：保持原来的 ≤1 小时步长（行为不变）。
            steps = Math.Min(MaxSubstepsPerFrame, Math.Max(preferredSteps, requiredSteps));
        } else {
            // 高倍率：不再死守 1 小时，按精度需要的最少步数跑完这一帧。
            steps = Math.Min(MaxSubstepsPerFrame, Math.Max(1, requiredSteps));
        }
        steps = Math.Max(steps, 1);

        double dt = Math.Min(simulationSeconds / steps, accuracyDt);
        double simulatedSeconds = dt * steps;

        for (int i = 0; i < steps; i++) {
            IntegrateGravity(dt);
            ResolveCollisions(dt);
        }

        // 热学：每帧最多 MaxThermalSubstepsPerFrame 次，步长随倍率自适应。
        IntegrateThermal(simulatedSeconds);

        // 物理可以分多个子步，但视觉同步和温度纹理每帧只需要一次。
        PushPositionsToVisuals();

        // 返回真正积分掉的秒数：极端倍率下如果步长已经粗到影响轨道，
        // 物理会少走一点，调用方据此把时钟一起放慢，日期和行星位置才不脱节。
        return simulatedSeconds;
    }

    /// <summary>
    /// 把 NaN / Infinity 复位成 0，避免一帧的数值爆炸污染整个场景。
    /// 正常情况下所有值都是有限的，这里只是保险丝。
    /// </summary>
    private static void SanitizeBodyValues() {
        for (int i = 0; i < _bodies.Count; i++) {
            SandboxPhysicsBody body = _bodies[i];
            if (body == null) continue;

            if (!IsFinite(body.positionM) || !IsFinite(body.velocityMps)) {
                Debug.LogWarning("[Physics] 天体数值异常，已复位：" + body.name);
                body.positionM = SandboxDVec3.Zero;
                body.velocityMps = SandboxDVec3.Zero;
                body.isOrbiting = false;
                body.orbitTarget = null;
                body.orbitRadiusM = 0.0;
            }

            if (double.IsNaN(body.temperatureK) || double.IsInfinity(body.temperatureK)) {
                body.temperatureK = 0.0;
            }
        }
    }

    private static bool IsFinite(SandboxDVec3 v) {
        return !double.IsNaN(v.x) && !double.IsInfinity(v.x) &&
               !double.IsNaN(v.y) && !double.IsInfinity(v.y) &&
               !double.IsNaN(v.z) && !double.IsInfinity(v.z);
    }

    /// <summary>
    /// 按“最短局部轨道周期 / OrbitStepsTarget”给出精度允许的最大步长。
    /// leapfrog 的周期误差约 (1/N)²，N = 200 时每圈约 3e-5，跑几百圈也看不出来。
    /// </summary>
    private static double ComputeAccuracySubstepSeconds() {
        double dt = double.MaxValue;
        int n = _bodies.Count;
        for (int i = 0; i < n; i++) {
            SandboxPhysicsBody body = _bodies[i];
            if (body == null || !body.alive || body.positionLocked || body.ignoreGravity) continue;

            // 只取最重的那个天体作为“中心天体”：行星绕恒星、碎片也绕恒星。
            // 不逐对计算，避免碎片之间临时靠近时把步长压到没有意义的量级。
            SandboxPhysicsBody primary = null;
            for (int j = 0; j < n; j++) {
                SandboxPhysicsBody other = _bodies[j];
                if (other == null || other == body || !other.alive) continue;
                if (other.massKg <= body.massKg) continue;
                if (primary == null || other.massKg > primary.massKg) primary = other;
            }
            if (primary == null) continue;

            double r = (primary.positionM - body.positionM).Magnitude;
            double contact = primary.radiusM + body.radiusM;
            if (r <= contact * 1.5) continue;   // 已在接触范围内，交给碰撞处理

            double mu = GravitationalConstant * (primary.massKg + body.massKg);
            if (mu <= 0.0) continue;
            double period = 2.0 * Math.PI * Math.Sqrt(r * r * r / mu);
            double candidate = period / OrbitStepsTarget;
            if (candidate < dt) dt = candidate;
        }
        if (dt == double.MaxValue) return double.MaxValue;
        return Math.Max(dt, MinSubstepSeconds);
    }

    /// <summary>
    /// 把一帧的模拟时间切成若干热学子步。
    /// 低倍率下等价于原来的“每个 1 小时子步都算一次温度”，
    /// 高倍率下（1 年/秒 ≈ 每帧 146 小时）不再做 146 次全表面辐射积分。
    /// </summary>
    private static void IntegrateThermal(double frameSeconds) {
        if (!ThermalEnabled) return;

        int steps = (int)Math.Ceiling(frameSeconds / MaxSubstepSeconds);
        if (steps < 1) steps = 1;
        if (steps > MaxThermalSubstepsPerFrame) steps = MaxThermalSubstepsPerFrame;
        double dt = frameSeconds / steps;
        for (int i = 0; i < steps; i++) {
            IntegrateRadiation(dt);
        }
    }

    public static void SetPositionLocked(SandboxPhysicsBody body, bool locked) {
        if (body == null || body.positionLocked == locked) return;
        if (locked) {
            body.preLockVelocity = body.velocityMps;
            body.preLockOrbiting = body.isOrbiting;
            body.preLockOrbitTarget = body.orbitTarget;
            body.preLockOrbitRadius = body.orbitRadiusM;
            body.positionLocked = true;
            body.velocityMps = SandboxDVec3.Zero;
            body.isOrbiting = false;
            body.orbitTarget = null;
        } else {
            body.positionLocked = false;
            body.velocityMps = body.preLockVelocity;
            body.isOrbiting = body.preLockOrbiting;
            body.orbitTarget = body.preLockOrbitTarget;
            body.orbitRadiusM = body.preLockOrbitRadius;
        }
    }

    private static void IntegrateGravity(double dt) {
        int n = _bodies.Count;
        if (n < 2) return;

        if (_accelX.Length < n) {
            int capacity = Math.Max(n, _accelX.Length * 2);
            _accelX = new double[capacity];
            _accelY = new double[capacity];
            _accelZ = new double[capacity];
        }
        double[] ax = _accelX;
        double[] ay = _accelY;
        double[] az = _accelZ;
        ComputeAccelerations(ax, ay, az);

        for (int i = 0; i < n; i++) {
            SandboxPhysicsBody b = _bodies[i];
            if (b == null || !b.alive || b.positionLocked || b.ignoreGravity) continue;
            b.velocityMps.x += 0.5 * ax[i] * dt;
            b.velocityMps.y += 0.5 * ay[i] * dt;
            b.velocityMps.z += 0.5 * az[i] * dt;
            b.positionM.x += b.velocityMps.x * dt;
            b.positionM.y += b.velocityMps.y * dt;
            b.positionM.z += b.velocityMps.z * dt;
        }

        ComputeAccelerations(ax, ay, az);
        for (int i = 0; i < n; i++) {
            SandboxPhysicsBody b = _bodies[i];
            if (b == null || !b.alive || b.positionLocked || b.ignoreGravity) continue;
            b.velocityMps.x += 0.5 * ax[i] * dt;
            b.velocityMps.y += 0.5 * ay[i] * dt;
            b.velocityMps.z += 0.5 * az[i] * dt;
        }
    }

    private static void ComputeAccelerations(double[] ax, double[] ay, double[] az) {
        int n = _bodies.Count;
        for (int i = 0; i < n; i++) {
            ax[i] = ay[i] = az[i] = 0.0;
        }

        // 全局引力关闭：加速度恒为 0，天体按已有速度惯性漂移。
        if (!GravityEnabled) return;

        for (int i = 0; i < n; i++) {
            SandboxPhysicsBody a = _bodies[i];
            if (a == null || !a.alive) continue;
            for (int j = i + 1; j < n; j++) {
                SandboxPhysicsBody b = _bodies[j];
                if (b == null || !b.alive) continue;
                if (a.ignoreGravity || b.ignoreGravity) continue;

                SandboxDVec3 d = b.positionM - a.positionM;
                double r2 = d.SqrMagnitude + SofteningM * SofteningM;
                double r = Math.Sqrt(r2);
                double factor = GravitationalConstant / Math.Max(r2 * r, 1e-18);

                ax[i] += factor * b.massKg * d.x;
                ay[i] += factor * b.massKg * d.y;
                az[i] += factor * b.massKg * d.z;
                ax[j] -= factor * a.massKg * d.x;
                ay[j] -= factor * a.massKg * d.y;
                az[j] -= factor * a.massKg * d.z;
            }
        }
    }

    /// <summary>
    /// 热辐射过程独立于撞击热容：吸收/黑体辐射都用 RadiativeHeatCapacity。
    /// 温度是状态量，不再先转成“内能J”。
    ///
    /// 这里是 1 年/秒 下的主要热点，所以做了三件事：
    /// 1. 斑块法线/面积只跟半径有关，缓存在天体上，每步只做一次 Y 轴旋转；
    /// 2. “到各恒星的单位方向 + 辐照度”每个行星每步只算一次，
    ///    不再对 288 个斑块重复开方/除法；
    /// 3. 温度更新改用隐式牛顿，步长放大到数小时也不会振荡。
    /// </summary>
    private static void IntegrateRadiation(double dt) {
        List<SandboxPhysicsBody> stars = _radiationStars;
        List<SandboxPhysicsBody> planets = _radiationPlanets;
        stars.Clear();
        planets.Clear();
        foreach (SandboxPhysicsBody b in _bodies) {
            if (b == null || !b.alive) continue;
            b.lastAbsorbedPowerW = 0.0;
            b.lastEmittedPowerW = 0.0;
            b.lastAtmosphereHeatingPowerW = 0.0;
            b.lastNetEmittedPowerW = 0.0;
            if (b.ignoreThermal) continue;
            if (b.IsStar) stars.Add(b);
            else planets.Add(b);
        }
        if (planets.Count == 0) return;

        const int lonCount = SandboxPhysicsBody.SurfaceLongitudeSegments;
        const int latCount = SandboxPhysicsBody.SurfaceLatitudeSegments;
        const double twoPi = 2.0 * Math.PI;
        const int patchCount = lonCount * latCount;

        EnsureStarScratch(stars.Count);

        foreach (SandboxPhysicsBody planet in planets) {
            // 碎片没有 24×12 温度网格，用整球能量平衡即可。
            if (planet.kind == SandboxPhysicsBodyKind.Fragment) {
                IntegrateFragmentThermal(planet, stars, dt);
                continue;
            }

            if (planet.surfaceTemperatures == null ||
                planet.surfaceTemperatures.Length != patchCount) {
                EnsureSurfaceTemperatures(planet);
            }
            EnsurePatchGeometry(planet);

            double greenhouse = Math.Clamp(planet.greenhouseFactor, 0.0, 1.0);
            double absorbedTotal = 0.0;
            double emittedTotal = 0.0;
            double atmosphereTotal = 0.0;

            // 自转只改变太阳照射到哪一片；0 = 不自转，温度场固定。
            if (planet.rotationPeriodDays > 0.0) {
                double periodSeconds = Math.Max(planet.rotationPeriodDays * 86400.0, 1.0);
                planet.rotationAngleRad = (planet.rotationAngleRad -
                    twoPi * dt / periodSeconds * planet.rotationDirection) % twoPi;
            }

            // 恒星几何量对整个行星是一样的，先算一次，斑块里只做点乘。
            int starCount = stars.Count;
            for (int s = 0; s < starCount; s++) {
                SandboxPhysicsBody star = stars[s];
                SandboxDVec3 toStar = star.positionM - planet.positionM;
                double distance = Math.Max(toStar.Magnitude,
                    Math.Max(star.radiusM, 1.0));
                double inv = 1.0 / distance;
                _starDirX[s] = toStar.x * inv;
                _starDirY[s] = toStar.y * inv;
                _starDirZ[s] = toStar.z * inv;
                _starFlux[s] = Math.Max(0.0, star.luminosityW) /
                               (4.0 * Math.PI * distance * distance);
            }

            if (_surfaceTempScratch.Length < patchCount) {
                _surfaceTempScratch = new double[patchCount];
            }
            System.Array.Copy(planet.surfaceTemperatures,
                _surfaceTempScratch, patchCount);
            double[] oldTemps = _surfaceTempScratch;

            double[] normalX = planet.patchNormalX;
            double[] normalY = planet.patchNormalY;
            double[] normalZ = planet.patchNormalZ;
            double[] areas = planet.patchAreaM2;
            double cosR = Math.Cos(planet.rotationAngleRad);
            double sinR = Math.Sin(planet.rotationAngleRad);

            for (int lat = 0; lat < latCount; lat++) {
                for (int lon = 0; lon < lonCount; lon++) {
                    int idx = lat * lonCount + lon;
                    double area = areas[idx];

                    // 星体坐标系里的法线是固定的，自转只是绕 Y 轴转。
                    double nx = normalX[idx] * cosR - normalZ[idx] * sinR;
                    double ny = normalY[idx];
                    double nz = normalX[idx] * sinR + normalZ[idx] * cosR;

                    double absorbed = 0.0;
                    for (int s = 0; s < starCount; s++) {
                        double incidence = nx * _starDirX[s] +
                                           ny * _starDirY[s] +
                                           nz * _starDirZ[s];
                        if (incidence > 0.0) {
                            absorbed += _starFlux[s] * incidence;
                        }
                    }
                    absorbed *= area;
                    if (!ThermalReceiveRadiation) absorbed = 0.0;

                    // 内部聚变热：与恒星辐射无关，直接平摊到每一块表面。
                    if (planet.internalHeatW > 0.0 && planet.patchTotalAreaM2 > 0.0) {
                        absorbed += planet.internalHeatW / planet.patchTotalAreaM2 * area;
                    }

                    double t = Math.Max(0.0, oldTemps[idx]);
                    double emitted = StefanBoltzmann * t * t * t * t * area;
                    double atmosphereHeat = ThermalAtmosphereHeating
                        ? emitted * 0.5 * greenhouse : 0.0;

                    // 相邻斑块平均温度：大气扩散和表层热传导都用它。
                    double neighborSum = 0.0;
                    int neighborCount = 0;
                    int leftIndex = lat * lonCount + (lon + lonCount - 1) % lonCount;
                    int rightIndex = lat * lonCount + (lon + 1) % lonCount;
                    neighborSum += oldTemps[leftIndex] + oldTemps[rightIndex];
                    neighborCount += 2;
                    if (lat > 0) {
                        neighborSum += oldTemps[idx - lonCount];
                        neighborCount++;
                    }
                    if (lat + 1 < latCount) {
                        neighborSum += oldTemps[idx + lonCount];
                        neighborCount++;
                    }
                    double neighborAvg = neighborSum / Math.Max(neighborCount, 1);

                    // 表层横向热传导：被恒星照到的半球整体升温，热量往相邻斑块摊开，
                    // 不会出现“只有一个点在加热”的观感。照不到的一面仍然主要靠它自己散热。
                    double diffusion = SurfaceConductionCoefficient * (neighborAvg - t) * area;

                    if (ThermalAtmosphereHeating && greenhouse > 0.0) {
                        // 大气把热量从暖斑块输送到冷斑块；气压/温室因子越大越强。
                        diffusion += greenhouse * 2.0 * (neighborAvg - t) * area;
                    }

                    double capacity = RadiativeHeatCapacityJPerM2K * area;
                    double emissionCoefficient = ThermalRadiationLoss
                        ? StefanBoltzmann * area : 0.0;
                    planet.surfaceTemperatures[idx] = SolvePatchTemperature(
                        t, absorbed + atmosphereHeat + diffusion,
                        emissionCoefficient, capacity, dt);

                    absorbedTotal += absorbed;
                    emittedTotal += emitted;
                    atmosphereTotal += atmosphereHeat;
                }
            }

            double totalArea = planet.patchTotalAreaM2;
            double weighted = 0.0;
            for (int i = 0; i < patchCount; i++) {
                weighted += planet.surfaceTemperatures[i] * areas[i];
            }
            planet.temperatureK = totalArea > 0.0 ? weighted / totalArea : 0.0;
            planet.lastAbsorbedPowerW = absorbedTotal;
            planet.lastEmittedPowerW = emittedTotal;
            planet.lastAtmosphereHeatingPowerW = atmosphereTotal;
            planet.lastNetEmittedPowerW = Math.Max(0.0,
                emittedTotal - atmosphereTotal);
        }
    }

    /// <summary>
    /// 碎片（碰撞产物）用整球能量平衡：截面 πr² 吸热、表面积 4πr² 辐射。
    /// 以前它们也会走 288 斑块的积分，一次撞击产生 20~40 块碎片时开销会爆炸。
    /// </summary>
    private static void IntegrateFragmentThermal(SandboxPhysicsBody fragment,
            List<SandboxPhysicsBody> stars, double dt) {
        double radius = Math.Max(fragment.radiusM, 1e-9);
        double area = 4.0 * Math.PI * radius * radius;
        double crossSection = Math.PI * radius * radius;
        double absorbed = 0.0;
        if (ThermalReceiveRadiation)
        for (int s = 0; s < stars.Count; s++) {
            SandboxPhysicsBody star = stars[s];
            SandboxDVec3 toStar = star.positionM - fragment.positionM;
            double distance = Math.Max(toStar.Magnitude,
                Math.Max(star.radiusM, 1.0));
            absorbed += Math.Max(0.0, star.luminosityW) /
                        (4.0 * Math.PI * distance * distance) * crossSection;
        }

        double t = Math.Max(0.0, fragment.temperatureK);
        double capacity = RadiativeHeatCapacityJPerM2K * area;
        fragment.temperatureK = SolvePatchTemperature(
            t, absorbed, ThermalRadiationLoss ? StefanBoltzmann * area : 0.0, capacity, dt);
        fragment.lastAbsorbedPowerW = absorbed;
        fragment.lastEmittedPowerW = StefanBoltzmann * t * t * t * t * area;
        fragment.lastAtmosphereHeatingPowerW = 0.0;
        fragment.lastNetEmittedPowerW = fragment.lastEmittedPowerW;
    }

    /// <summary>
    /// 单个斑块的温度更新：dT/dt = (input - k·T⁴) / capacity。
    /// 1 年/秒 时热学步长约 6 小时，显式欧拉会让高温斑块剧烈过冲
    /// （5000K 的斑块一步就会跌到 0 再回弹），所以这里用两步牛顿解隐式方程；
    /// 步长很小时收敛结果与原来的显式欧拉基本一致。
    /// </summary>
    private static double SolvePatchTemperature(double t, double input,
            double lossK, double capacity, double dt) {
        if (capacity <= 0.0 || dt <= 0.0) return Math.Max(0.0, t);
        if (t <= 0.0 && input <= 0.0) return 0.0;

        double t2 = t * t;
        double t4 = t2 * t2;

        // 默认流速下 dt ≤ 1 小时，保持原来的显式欧拉，数值行为与改造前完全一致。
        if (dt <= MaxSubstepSeconds) {
            return Math.Max(0.0, t + (input - lossK * t4) * dt / capacity);
        }

        // 1 年/秒 时热学步长约 6 小时，显式解会让高温斑块剧烈过冲
        // （5000K 的斑块一步就跌到 0 再回弹），用两步牛顿解隐式方程。
        double c = capacity / dt;
        double temp = t;
        for (int i = 0; i < 2; i++) {
            double temp2 = temp * temp;
            double f = (temp - t) * c - input + lossK * temp2 * temp2;
            double df = c + 4.0 * lossK * temp * temp2;
            if (df <= 0.0) break;
            temp -= f / df;
            if (temp < 0.0) temp = 0.0;
        }
        return temp;
    }

    private static void EnsureStarScratch(int count) {
        if (_starDirX.Length >= count) return;
        int capacity = Math.Max(count, 4);
        _starDirX = new double[capacity];
        _starDirY = new double[capacity];
        _starDirZ = new double[capacity];
        _starFlux = new double[capacity];
    }

    /// <summary>缓存经纬网格的单位法线与斑块面积；半径变化（撞击吸积）时才重建。</summary>
    private static void EnsurePatchGeometry(SandboxPhysicsBody body) {
        const int lonCount = SandboxPhysicsBody.SurfaceLongitudeSegments;
        const int latCount = SandboxPhysicsBody.SurfaceLatitudeSegments;
        int count = lonCount * latCount;

        if (body.patchNormalX == null || body.patchNormalX.Length != count) {
            body.patchNormalX = new double[count];
            body.patchNormalY = new double[count];
            body.patchNormalZ = new double[count];
            body.patchAreaM2 = new double[count];
            body.patchGeometryRadiusM = -1.0;
        }
        if (body.patchGeometryRadiusM == body.radiusM) return;

        double r2 = body.radiusM * body.radiusM;
        double totalArea = 0.0;
        for (int lat = 0; lat < latCount; lat++) {
            double lat0 = -0.5 * Math.PI + Math.PI * lat / latCount;
            double lat1 = -0.5 * Math.PI + Math.PI * (lat + 1) / latCount;
            double midLat = 0.5 * (lat0 + lat1);
            double cosLat = Math.Cos(midLat);
            double sinLat = Math.Sin(midLat);
            double area = r2 * (2.0 * Math.PI / lonCount) *
                          Math.Max(1e-9, Math.Sin(lat1) - Math.Sin(lat0));
            for (int lon = 0; lon < lonCount; lon++) {
                double lonAngle = 2.0 * Math.PI * (lon + 0.5) / lonCount;
                int idx = lat * lonCount + lon;
                body.patchNormalX[idx] = cosLat * Math.Cos(lonAngle);
                body.patchNormalY[idx] = sinLat;
                body.patchNormalZ[idx] = cosLat * Math.Sin(lonAngle);
                body.patchAreaM2[idx] = area;
            }
            totalArea += area * lonCount;
        }
        body.patchTotalAreaM2 = totalArea;
        body.patchGeometryRadiusM = body.radiusM;
    }

    /// <summary>撞击热仍然是全球性的：所有表面斑块一起升温。</summary>
    private static void AddGlobalTemperature(SandboxPhysicsBody body, double deltaT) {
        if (!ThermalCollisionHeat) return;
        if (body == null || deltaT == 0.0) return;
        body.temperatureK += deltaT;
        if (body.surfaceTemperatures == null) return;
        for (int i = 0; i < body.surfaceTemperatures.Length; i++) {
            body.surfaceTemperatures[i] =
                Math.Max(0.0, body.surfaceTemperatures[i] + deltaT);
        }
    }

    private static void ResolveCollisions(double dt) {
        for (int i = 0; i < _bodies.Count; i++) {
            SandboxPhysicsBody a = _bodies[i];
            if (a == null || !a.alive) continue;

            for (int j = i + 1; j < _bodies.Count; j++) {
                SandboxPhysicsBody b = _bodies[j];
                if (b == null || !b.alive) continue;
                if (a.ignoreCollision || b.ignoreCollision) continue;
                // 锁定位置的天体照样可以被撞：
                // 只是合并后它保持原地不动，不会被撞飞（见 MergeCollision）。

                SandboxDVec3 delta = b.positionM - a.positionM;
                SandboxDVec3 relVel = b.velocityMps - a.velocityMps;
                double contact = a.radiusM + b.radiusM;
                double distance = delta.Magnitude;

                if (distance > contact) {
                    // 高倍率下步长可达数天，只测“末位置是否重叠”会漏掉撞击；
                    // 对恒星/行星用相对匀速运动做一次扫掠检测。
                    // 碎片仍走原来的判定，避免刚生成的碎块被立刻重新吞掉。
                    if (a.kind == SandboxPhysicsBodyKind.Fragment ||
                        b.kind == SandboxPhysicsBodyKind.Fragment) {
                        continue;
                    }
                    double vv = relVel.SqrMagnitude;
                    if (vv <= 0.0) continue;
                    double t = -(delta.x * relVel.x + delta.y * relVel.y +
                                 delta.z * relVel.z) / vv;
                    if (t <= 0.0 || t >= dt) continue;
                    SandboxDVec3 closest = delta + relVel * t;
                    if (closest.SqrMagnitude > contact * contact) continue;

                    MergeCollision(a, b);
                    return;
                }

                if (distance > 1e-9 &&
                    relVel.x * delta.x + relVel.y * delta.y + relVel.z * delta.z > 0.0) {
                    continue;
                }

                MergeCollision(a, b);
                return;
            }
        }
    }

    /// <summary>
    /// 较大天体吞并较小天体。
    /// 只用碰撞损失的机械动能，通过 ImpactHeatCapacity 给幸存者表面升温。
    /// 不把引力势能、被吞并天体内能或自身束缚能再转成温度。
    /// </summary>
    /// <summary>
    /// 彩蛋撞击结算：给被撞天体 +impactGiftSun 太阳质量；
    /// 如果它是行星，直接转成恒星（名字不变）。彩蛋自身消耗掉。
    /// </summary>
    private static void ResolveEasterEggImpact(SandboxPhysicsBody egg,
            SandboxPhysicsBody target) {
        if (egg == null || target == null || !target.alive) return;

        double giftKg = egg.impactGiftSun * SunMassKg;

        bool converted = false;
        if (target.planet != null && SandboxSceneController.Instance != null) {
            converted = SandboxSceneController.Instance.ConvertPlanetToStar(target, giftKg);
        }

        if (!converted) {
            // 撞的是恒星/碎片：只加质量，重新同步一下恒星参数。
            if (target.star != null) {
                target.star.ApplyAbsorbedMass(giftKg);
                SyncStar(target.star);
            } else {
                target.massKg += giftKg;
            }
            LastCollisionMechanicalEnergyJ = 0.0;
        }

        RemoveBody(egg);
    }

    private static void MergeCollision(SandboxPhysicsBody a, SandboxPhysicsBody b) {
        // 彩蛋撞击：被撞的一方 +0.2 M☉（行星直接变恒星，名字不变），彩蛋本身消耗掉。
        if (a.impactGiftSun > 0.0 || b.impactGiftSun > 0.0) {
            SandboxPhysicsBody egg = a.impactGiftSun > 0.0 ? a : b;
            SandboxPhysicsBody target = ReferenceEquals(egg, a) ? b : a;
            ResolveEasterEggImpact(egg, target);
            return;
        }

        // 锁定位置的一方优先活下来：锁定的天体应该原地不动地挨撞，
        // 而不是被质量更大的来客吞掉。
        SandboxPhysicsBody survivor;
        if (a.positionLocked != b.positionLocked) {
            survivor = a.positionLocked ? a : b;
        } else {
            survivor = a.massKg >= b.massKg ? a : b;
        }
        SandboxPhysicsBody donor = ReferenceEquals(survivor, a) ? b : a;

        // 只有被撞毁的一方是行星才产生碎片；碎片再撞不会继续产生碎片。
        if (donor.kind == SandboxPhysicsBodyKind.Planet &&
            survivor.kind != SandboxPhysicsBodyKind.Fragment &&
            donor.kind != SandboxPhysicsBodyKind.Fragment) {
            FragmentPlanetImpact(survivor, donor);
            return;
        }

        double survivorMassBefore = survivor.massKg;
        double donorMass = donor.massKg;
        double totalMass = survivorMassBefore + donorMass;
        if (totalMass <= 0.0) return;

        SandboxDVec3 momentum = survivor.velocityMps * survivorMassBefore +
                                donor.velocityMps * donorMass;
        double kineticBefore = survivor.KineticEnergyJ + donor.KineticEnergyJ;

        RemoveBody(donor);
        if (!survivor.alive) return;

        survivor.massKg = totalMass;
        if (survivor.positionLocked) {
            // 锁定体被撞后仍在原地：不掉速度，也不会因为吸收质量而被推开。
            survivor.velocityMps = SandboxDVec3.Zero;
            survivor.preLockVelocity = SandboxDVec3.Zero;
            survivor.isOrbiting = false;
            survivor.orbitTarget = null;
            survivor.orbitRadiusM = 0.0;
        } else {
            survivor.velocityMps = momentum / totalMass;
        }
        if (survivor.planet != null) {
            survivor.planet.ApplyAbsorbedMass(donorMass);
        } else if (survivor.star != null) {
            survivor.star.ApplyAbsorbedMass(donorMass);
            SandboxSceneController.Instance?.RefreshSelectedStar(survivor.star);
        }

        double kineticAfter = survivor.KineticEnergyJ;
        double mechanicalLoss = Math.Max(0.0, kineticBefore - kineticAfter);
        LastCollisionMechanicalEnergyJ = mechanicalLoss;

        if (!survivor.IsStar) {
            double area = 4.0 * Math.PI * survivor.radiusM * survivor.radiusM;
            double impactCapacity = ImpactHeatCapacityJPerM2K * area;
            if (impactCapacity > 0.0) {
                double deltaT = mechanicalLoss / impactCapacity;
                AddGlobalTemperature(survivor, deltaT);
                LastCollisionDeltaT = deltaT;
            }
        } else {
            LastCollisionDeltaT = 0.0;
        }

        LastCollisionMomentumError =
            (survivor.velocityMps * survivor.massKg - momentum).Magnitude;
    }

    private static void FragmentPlanetImpact(SandboxPhysicsBody survivor,
            SandboxPhysicsBody donor) {
        if (survivor == null || donor == null || !donor.alive) return;

        double survivorMassBefore = survivor.massKg;
        double donorMass = donor.massKg;
        double absorbedMass = donorMass * 0.7;
        double fragmentMass = donorMass - absorbedMass;
        if (absorbedMass <= 0.0 || fragmentMass <= 0.0) return;

        double kineticBefore = survivor.KineticEnergyJ + donor.KineticEnergyJ;
        double survivorMassAfter = survivorMassBefore + absorbedMass;
        SandboxDVec3 survivorMomentum = survivor.velocityMps * survivorMassBefore +
                                         donor.velocityMps * absorbedMass;
        SandboxDVec3 survivorVelocity = survivorMomentum / survivorMassAfter;

        // 碎片整体沿用被撞毁行星的速度，动量仍然严格守恒。
        double kineticAfter = 0.5 * survivorMassAfter *
                              survivorVelocity.SqrMagnitude +
                              0.5 * fragmentMass * donor.velocityMps.SqrMagnitude;
        double heatJ = Math.Max(0.0, kineticBefore - kineticAfter);

        RemoveBody(donor);
        if (!survivor.alive) return;

        survivor.massKg = survivorMassAfter;
        survivor.velocityMps = survivorVelocity;
        if (survivor.planet != null) {
            survivor.planet.ApplyAbsorbedMass(absorbedMass);
        } else if (survivor.star != null) {
            survivor.star.ApplyAbsorbedMass(absorbedMass);
            SandboxSceneController.Instance?.RefreshSelectedStar(survivor.star);
        }

        // 90% 撞击热进入幸存者表面。
        double survivorHeat = heatJ * 0.9;
        if (survivor.planet != null) {
            double area = 4.0 * Math.PI * survivor.radiusM * survivor.radiusM;
            double capacity = ImpactHeatCapacityJPerM2K * area;
            double deltaT = capacity > 0.0 ? survivorHeat / capacity : 0.0;
            AddGlobalTemperature(survivor, deltaT);
            LastCollisionDeltaT = deltaT;
        } else {
            LastCollisionDeltaT = 0.0;
        }

        double density = donor.radiusM > 0.0
            ? donor.massKg / (4.0 / 3.0 * Math.PI * Math.Pow(donor.radiusM, 3.0))
            : 1000.0;
        density = Math.Max(density, 1e-9);
        double solidMass = fragmentMass * 0.8;
        double mistMass = fragmentMass * 0.2;
        int solidCount = UnityEngine.Random.Range(10, 21);
        int mistCount = UnityEngine.Random.Range(10, 21);
        double fragmentHeat = heatJ * 0.1;

        CreateFragmentBodies(survivor, donor, solidMass, solidCount, density,
            false, fragmentHeat * 0.8, donor.trailColor);
        CreateFragmentBodies(survivor, donor, mistMass, mistCount, 40.0,
            true, fragmentHeat * 0.2, donor.trailColor);

        LastCollisionMechanicalEnergyJ = heatJ;
        SandboxDVec3 momentumAfter = survivor.velocityMps * survivor.massKg;
        foreach (SandboxPhysicsBody body in _bodies) {
            if (body != null && body.alive && body.kind == SandboxPhysicsBodyKind.Fragment &&
                body.name.StartsWith(donor.name + "_")) {
                momentumAfter += body.velocityMps * body.massKg;
            }
        }
        LastCollisionMomentumError =
            (momentumAfter - (survivor.velocityMps * survivorMassAfter +
                              donor.velocityMps * fragmentMass)).Magnitude;
    }

    private static void CreateFragmentBodies(SandboxPhysicsBody survivor,
            SandboxPhysicsBody donor, double totalMass, int count, double density,
            bool isMist, double totalHeat, Color solidColor) {
        if (totalMass <= 0.0 || count <= 0) return;

        double massEach = totalMass / count;
        Shader shader = Resources.Load<Shader>(
            isMist ? "Shaders/FragmentMist" : "Shaders/FragmentSolid");
        if (shader == null) {
            shader = Shader.Find(isMist ? "UPB/FragmentMist" : "UPB/FragmentSolid");
        }
        double donorRadius = donor.radiusM;
        double minDistance = Math.Max(survivor.radiusM, donorRadius) * 1.15;

        for (int i = 0; i < count; i++) {
            double radius = Math.Pow(3.0 * massEach /
                (4.0 * Math.PI * Math.Max(density, 1e-9)), 1.0 / 3.0);
            Vector3 direction = UnityEngine.Random.onUnitSphere;
            double distance = minDistance + radius * 1.5 +
                              UnityEngine.Random.Range(0.5f, 1.5f) * donorRadius;
            SandboxDVec3 position = donor.positionM +
                new SandboxDVec3(direction.x, direction.y, direction.z) * distance;
            double heatShare = totalHeat * (massEach / Math.Max(totalMass, 1e-9));
            double areaEq = Math.Max(radius * 2.0, 1e-9);
            double deltaT = heatShare /
                (ImpactHeatCapacityJPerM2K * areaEq);
            double temperature = Math.Max(0.0, donor.temperatureK + deltaT);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = donor.name + (isMist ? "_雾团_" : "_碎块_") + i;
            go.transform.localScale = Vector3.one *
                (float)(radius * 2.0 / MetersPerUnit);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);

            SandboxFragment fragment = go.AddComponent<SandboxFragment>();
            fragment.isMist = isMist;
            fragment.body = null;
            fragment.SetLogicalPosition(position.x / MetersPerUnit,
                position.y / MetersPerUnit, position.z / MetersPerUnit);

            Material material = shader != null ? new Material(shader) : null;
            if (material != null) {
                Color color = isMist
                    ? BlackbodyColor(temperature)
                    : solidColor;
                color.a = 1f;
                material.SetColor("_Color", color);
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null) {
                    renderer.material = material;
                    renderer.shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            fragment.fragmentMaterial = material;

            SandboxPhysicsBody body = new() {
                kind = SandboxPhysicsBodyKind.Fragment,
                name = go.name,
                alive = true,
                massKg = massEach,
                radiusM = radius,
                positionM = position,
                velocityMps = donor.velocityMps,
                temperatureK = temperature,
                fragment = fragment
            };
            fragment.body = body;
            _bodies.Add(body);
        }
    }

    private static Color BlackbodyColor(double temperatureK) {
        var (r, g, b) = Spectrum.GetBlackbodyColor(temperatureK);
        return new Color(r, g, b, 1f);
    }

    private static void RemoveBody(SandboxPhysicsBody body) {
        if (body == null || !body.alive) return;
        body.alive = false;
        _bodies.Remove(body);
        SandboxTrail.Remove(body);

        if (body.star != null) {
            _starBodies.Remove(body.star);
            SandboxSceneController.Instance?.RemoveStarFromSandbox(body.star);
        } else if (body.planet != null) {
            _planetBodies.Remove(body.planet);
            SandboxSceneController.Instance?.RemovePlanetFromSandbox(body.planet);
        } else if (body.fragment != null) {
            UnityEngine.Object.Destroy(body.fragment.gameObject);
        }
    }

    private static void PushPositionsToVisuals() {
        foreach (SandboxPhysicsBody body in _bodies) {
            if (body == null || !body.alive) continue;

            if (body.star != null) {
                body.star.logicalX = body.positionM.x / MetersPerUnit;
                body.star.logicalY = body.positionM.y / MetersPerUnit;
                body.star.logicalZ = body.positionM.z / MetersPerUnit;
                body.star.ApplyRenderPosition();
            } else if (body.planet != null) {
                body.planet.logicalX = body.positionM.x / MetersPerUnit;
                body.planet.logicalY = body.positionM.y / MetersPerUnit;
                body.planet.logicalZ = body.positionM.z / MetersPerUnit;
                body.planet.ApplyRenderPosition();
                body.planet.ApplyThermalState(body.temperatureK);
                body.planet.SetSurfaceTemperatures(body.surfaceTemperatures);
            } else if (body.fragment != null) {
                body.fragment.SetLogicalPosition(
                    body.positionM.x / MetersPerUnit,
                    body.positionM.y / MetersPerUnit,
                    body.positionM.z / MetersPerUnit);
                body.fragment.ApplyThermalState(body.temperatureK);
            }
        }
    }

    private static SandboxDVec3 LogicalToMeters(double x, double y, double z) =>
        new(x * MetersPerUnit, y * MetersPerUnit, z * MetersPerUnit);
}
