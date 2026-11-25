using UnityEngine;

/// <summary>
/// 沙盘里已经创建出来的恒星。保存用户可编辑的输入参数和模型算出的只读快照。
/// </summary>
public class SandboxStar : MonoBehaviour {
    public string starName;
    public double mass;
    public double metallicity;
    public double ageMyr;
    public double rotationPeriodDays = 25.38;
    public SandboxStarSnapshot snapshot;

    public Material starMaterial;
    public Renderer starRenderer;
    public StarLightPoint lightPoint;

    // 0 = 默认渲染；1 = 黑体光球（永远不切换星点）
    public int renderMode;
    private Material _glowMaterial;
    private double _appliedWorldScale = -1.0;

    /// <summary>这颗恒星作为行星照明的点光源（参数统一由行星 shader 的数组读取）。</summary>
    public Light planetLight;

    // 逻辑世界坐标用 double 保存；transform.position 只是浮点原点附近的渲染坐标。
    public double logicalX;
    public double logicalY;
    public double logicalZ;

    public double VisualDiameter => snapshot != null ? snapshot.VisualDiameter : 1.0;

    public void SetLogicalPosition(double x, double y, double z) {
        logicalX = x;
        logicalY = y;
        logicalZ = z;
        ApplyRenderPosition();
        SandboxPhysicsSystem.SyncPosition(this);
        SandboxTrail.Clear(SandboxPhysicsSystem.GetBody(this));
    }

    public void ApplyRenderPosition() {
        transform.position = SandboxFloatingOrigin.ToRender(logicalX, logicalY, logicalZ);
        ApplyRenderScale();
    }

    private void ApplyRenderScale() {
        _appliedWorldScale = SandboxWorldScale.Scale;
        float diameter = (float)(VisualDiameter * _appliedWorldScale);
        if (diameter < 1e-20f) diameter = 1e-20f;
        transform.localScale = new Vector3(diameter, diameter, diameter);
    }

    private void LateUpdate() {
        // 世界缩放变化时（相机缩放）同步渲染尺寸；位置由浮动原点负责。
        if (System.Math.Abs(_appliedWorldScale - SandboxWorldScale.Scale) > 0.0) {
            ApplyRenderPosition();
        }
    }

    public void Initialize(
            string displayName, double initialMass, double initialMetallicity,
            double initialAgeMyr, SandboxStarSnapshot starSnapshot, Material material,
            double initialRotationPeriodDays = 25.38) {
        starName = displayName;
        mass = initialMass;
        metallicity = initialMetallicity;
        ageMyr = initialAgeMyr;
        rotationPeriodDays = initialRotationPeriodDays > 0.0 ? initialRotationPeriodDays : 25.38;
        snapshot = starSnapshot;

        gameObject.name = displayName;
        starMaterial = material;
        starRenderer = GetComponent<Renderer>();
        // 用 sharedMaterial，避免 Renderer.material 再克隆一份导致参数写在另一个材质上。
        if (starRenderer != null) starRenderer.sharedMaterial = material;

        // 复用主序/白矮星演化场景那套“拉远后变成星点”的特效。
        lightPoint = StarLightPoint.Attach(gameObject, Camera.main);
        if (lightPoint != null) {
            // 沙盘里星点用 3D billboard，让行星按真实深度遮挡它。
            lightPoint.SetSortingOrder(-10);
            lightPoint.UseWorldSpaceBillboard();
            lightPoint.SetStar(starSnapshot.luminosityLsun, starSnapshot.temperatureK);
        }

        EnsurePlanetLight();
        ApplySnapshot(starSnapshot);
        SandboxPhysicsSystem.RegisterStar(this);
    }

    private void Start() {
        // 创建时 snapshot 可能刚设置好；统一在这里同步一次光源颜色。
        EnsurePlanetLight();
        if (snapshot != null && planetLight != null) {
            var (r, g, b) = Spectrum.GetBlackbodyColor(snapshot.temperatureK);
            planetLight.color = new Color(r, g, b, 1f);
        }
    }

    /// <summary>
    /// 行星照明的淡出阈值（单位：等效 AU）。
    /// 1 L☉ 恒星：90 AU 之内正常照明，90~900 AU 平滑淡出，900 AU 之外完全照不亮。
    /// 阈值随 √L 缩放——用“等效距离 = 实际距离 / √L”判断，所以亮星照得更远。
    /// 这两个值会同时喂给 UPB/SandboxPlanet 着色器，改这里就够了。
    /// </summary>
    public const double IlluminationFadeStartAu = 90.0;
    public const double IlluminationFadeEndAu = 900.0;

    /// <summary>
    /// 这颗恒星在给定距离上的照明系数（0~1），与着色器的淡出规则完全一致。
    /// </summary>
    public double IlluminationFactor(double distanceUnits) {
        if (snapshot == null) return 0.0;
        double luminosityLsun = System.Math.Max(snapshot.luminosityLsun, 1e-8);
        double distanceAu = CelestialScale.UnitsToAu(distanceUnits);
        double effectiveAu = distanceAu / System.Math.Sqrt(luminosityLsun);
        if (effectiveAu <= IlluminationFadeStartAu) return 1.0;
        if (effectiveAu >= IlluminationFadeEndAu) return 0.0;
        double t = (effectiveAu - IlluminationFadeStartAu) /
                   (IlluminationFadeEndAu - IlluminationFadeStartAu);
        return 1.0 - t * t * (3.0 - 2.0 * t);   // 与 HLSL smoothstep 一致
    }

    /// <summary>当前恒星在行星方向上是否足够亮（用来限制参与照明的光源数量）。</summary>
    public bool IsIlluminating(double distanceUnits) =>
        IlluminationFactor(distanceUnits) > 0.0;

    /// <summary>
    /// 创建时给每颗恒星挂一个点光源。
    /// 它会跟随恒星移动，行星 shader 每帧读取它的位置/颜色/强度作为多光源之一。
    /// </summary>
    private void EnsurePlanetLight() {
        if (planetLight != null) return;

        GameObject lightGo = new("StarPlanetLight");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = Vector3.zero;

        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 0f;          // 0 = 无限远，行星自己按距离做衰减
        light.intensity = 1f;
        light.color = Color.white;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForceVertex;
        planetLight = light;
    }

    /// <summary>
    /// 恒星碰撞后的质量变化：金属度保持不变，年龄按原寿命比例迁移到新寿命，
    /// 然后用新质量 + 新年龄重新计算温度、光度、半径等全部模型参数。
    /// </summary>
    public void ApplyAbsorbedMass(double massKg) {
        if (massKg <= 0.0 || snapshot == null) return;

        double addedSun = massKg / SandboxPhysicsSystem.SunMassKg;
        double newMass = mass + addedSun;

        // 年龄比例：旧年龄 / 旧寿命。
        double oldFraction = snapshot.lifespanMyr > 1e-9
            ? ageMyr / snapshot.lifespanMyr
            : 0.0;
        oldFraction = System.Math.Max(0.0, System.Math.Min(1.0, oldFraction));

        // 先在新质量下算一个寿命基准，再把年龄比例映射到新寿命。
        if (SandboxStarModel.TryCompute(newMass, metallicity, 0.0, rotationPeriodDays,
                out SandboxStarSnapshot seed, out string error)) {
            double newAge = seed.lifespanMyr * oldFraction;
            newAge = System.Math.Max(0.0, System.Math.Min(newAge, seed.lifespanMyr));

            if (SandboxStarModel.TryCompute(newMass, metallicity, newAge, rotationPeriodDays,
                    out SandboxStarSnapshot updated, out error)) {
                mass = newMass;
                ageMyr = newAge;
                ApplySnapshot(updated);
                return;
            }
        }

        // 超出恒星模型范围等异常情况：至少保证质量守恒，参数保持旧值。
        mass = newMass;
        snapshot.mass = newMass;
        Debug.LogWarning($"[SandboxStar] 恒星质量 {newMass:0.###} Msun 无法重新计算参数：{error}");
    }

    /// <summary>把模型快照应用到 3D 外观：大小、颜色、发光强度。</summary>
    /// <summary>
    /// 切换恒星渲染模式。
    /// 0 = 现在的样子（远处会变成星点）；
    /// 1 = 黑体辐射色的发光球体（中间微微泛白，永远不切换星点）。
    /// </summary>
    public void SetStarRenderMode(int mode) {
        renderMode = mode;
        if (starRenderer == null) return;

        if (mode == 1) {
            if (_glowMaterial == null) _glowMaterial = CreateGlowMaterial();
            if (_glowMaterial != null) starRenderer.sharedMaterial = _glowMaterial;
            if (lightPoint != null) lightPoint.SetSuppressDot(true);
        } else {
            if (starMaterial != null) starRenderer.sharedMaterial = starMaterial;
            if (lightPoint != null) lightPoint.SetSuppressDot(false);
        }
    }

    private Material CreateGlowMaterial() {
        if (snapshot == null) return null;

        Shader shader = Resources.Load<Shader>("Shaders/SandboxStarGlow");
        if (shader == null) shader = Shader.Find("UPB/SandboxStarGlow");
        if (shader == null) return null;

        Material material = new(shader) { name = starName + "_Glow" };
        ApplyGlowColor(material);
        return material;
    }

    private void ApplyGlowColor(Material material) {
        if (material == null || snapshot == null) return;
        var (r, g, b) = Spectrum.GetBlackbodyColor(snapshot.temperatureK);
        material.SetColor("_Color", new Color(r, g, b, 1f));
    }

    public void ApplySnapshot(SandboxStarSnapshot starSnapshot) {
        snapshot = starSnapshot;
        if (starSnapshot == null) return;
        rotationPeriodDays = starSnapshot.rotationPeriodDays > 0.0
            ? starSnapshot.rotationPeriodDays
            : 25.38;

        ApplyRenderScale();

        if (lightPoint != null) {
            lightPoint.SetStar(starSnapshot.luminosityLsun, starSnapshot.temperatureK);
        }

        if (planetLight != null) {
            var (lr, lg, lb) = Spectrum.GetBlackbodyColor(starSnapshot.temperatureK);
            planetLight.color = new Color(lr, lg, lb, 1f);
        }

        if (starMaterial == null) return;
        var (r, g, b) = Spectrum.GetBlackbodyColor(starSnapshot.temperatureK);
        starMaterial.SetColor("_BaseColor", new Color(r, g, b, 1f));
        starMaterial.SetColor("_CellColor", new Color(r * 1.38f, g * 1.38f, b * 1.38f, 1f));
        starMaterial.SetFloat(
            "_EmissionStrength",
            Utils.GetStarEmissionStrength(starSnapshot.temperatureK / SandboxStarModel.SunSurfaceK));

        // 黑体光球模式下调温度后，颜色跟着更新。
        ApplyGlowColor(_glowMaterial);

        SandboxPhysicsSystem.SyncStar(this);
    }

    private void OnDestroy() {
        SandboxPhysicsSystem.Unregister(this);
        if (lightPoint != null) Destroy(lightPoint.gameObject);
        if (starMaterial != null) Destroy(starMaterial);
        if (_glowMaterial != null) Destroy(_glowMaterial);
    }
}
