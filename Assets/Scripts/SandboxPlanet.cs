using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 沙盘里的一颗行星。基础颜色/贴图由行星数据决定；
/// 恒星照明与高温自发光都在 UPB/SandboxPlanet 里完成。
/// </summary>
public class SandboxPlanet : MonoBehaviour {
    public string planetName;
    public double massEarth;
    public double radiusEarth;

    /// <summary>改质量时用的半径比例系数（A 类由目录质量/半径反推，B 类用 7 万 km）。</summary>
    public double radiusScaleKm = -1.0;
    /// <summary>是否原生岩石行星（地球、火星这类），改质量变小时不触发岩石化。</summary>
    public bool rockyOrigin;
    /// <summary>是否是被改质量“岩石化”的天体（改回大质量要能变回气态巨行星）。</summary>
    public bool rockyConverted;
    /// <summary>当前是否处于气态巨行星状态（改质量时可以岩石↔气态互转）。</summary>
    public bool isGasGiant;
    /// <summary>特殊行星（SpecialPlanets）：改质量不会变气态/恒星。</summary>
    public bool special;
    public double atmosphereMassEarth;
    public double temperatureK;
    public double rotationPeriodDays = 1.0;
    public int rotationDirection = 1;
    public double axialTiltDeg;
    public double greenhouseFactor;
    public double surfaceWaterDepthM;
    public double landHeightMaxM = 20000.0;
    public double soluteConcentrationMmolL;
    public double freezingPointK = 273.15;
    public double boilingPointK = 373.15;
    public Color trailColor = new(200f / 255f, 200f / 255f, 200f / 255f, 1f);
    public SandboxPlanetSnapshot snapshot;

    private double _rotationDegrees;
    private double _appliedWorldScale = -1.0;

    public Renderer planetRenderer;
    public Material planetMaterial;

    // 逻辑世界坐标用 double 保存；transform.position 是浮点原点附近的渲染坐标。
    public double logicalX;
    public double logicalY;
    public double logicalZ;

    private Texture2D _surfaceTexture;
    private Texture2D _atmosphereTexture;
    private Texture2D _heightMapTexture;
    private GameObject _atmosphereHalo;
    private Material _atmosphereHaloMaterial;
    private Texture2D _temperatureTexture;
    private Color[] _temperaturePixels;
    private Texture2D _temperatureValueTexture;
    private float[] _temperatureValues;
    private const float TemperatureUploadInterval = 0.1f;
    private float _lastTemperatureUploadTime = -1f;

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
        ApplyBoxScale();
    }

    private void LateUpdate() {
        // 世界缩放变化时同步渲染尺寸；位置由浮动原点负责。
        if (System.Math.Abs(_appliedWorldScale - SandboxWorldScale.Scale) > 0.0) {
            ApplyRenderPosition();
        }
    }

    private void Update() {
        UpdateAtmosphereShellTransform();
        if (rotationPeriodDays > 0.0) {
            double simulationSeconds = SandboxTimeManager.Instance != null
                ? SandboxTimeManager.Instance.DeltaSimulationSeconds
                : 0.0;
            if (simulationSeconds > 0.0) {
                // Unity 的 +Y 旋转方向与从北极俯视的天文顺行方向相反，
                // 所以这里乘 -rotationDirection：JSON 的 +1 表示顺行。
                _rotationDegrees -= 360.0 * simulationSeconds / (rotationPeriodDays * 86400.0)
                                    * rotationDirection;
                _rotationDegrees %= 360.0;
            }
        }
        ApplyAxialTilt();
    }

    /// <summary>自转轴倾角 + 自转：先绕本地 Y 自转，再把整根轴倾斜 axialTiltDeg 度。</summary>
    public void ApplyAxialTilt() {
        Quaternion spin = Quaternion.Euler(0f, (float)_rotationDegrees, 0f);
        if (System.Math.Abs(axialTiltDeg) < 1e-6) {
            transform.localRotation = spin;
            return;
        }
        Quaternion tilt = Quaternion.AngleAxis((float)axialTiltDeg, Vector3.right);
        transform.localRotation = tilt * spin;
    }

    public double VisualDiameter => snapshot != null
        ? snapshot.visualDiameter
        : CelestialScale.DiameterToUnits(CelestialScale.EarthRadiusKm * radiusEarth);

    public void Initialize(PlanetData data, SandboxPlanetSnapshot initialSnapshot,
            Material material) {
        planetName = data != null ? data.name : "行星";
        gameObject.name = planetName;

        planetRenderer = GetComponent<Renderer>();
        planetMaterial = material;
        if (planetRenderer != null) planetRenderer.sharedMaterial = material;

        ApplySnapshot(initialSnapshot);

        // 记录初始状态：改质量时用来做半径归一化、判断是否原生岩石行星。
        if (initialSnapshot != null) {
            rockyOrigin = initialSnapshot.massEarth <= 10.0 &&
                          initialSnapshot.atmosphereMassEarth < 5.0;
            isGasGiant = !rockyOrigin;
            radiusScaleKm = SandboxPlanetMassModel.ScaleFromCatalog(
                initialSnapshot.massEarth, initialSnapshot.radiusEarth);
        }

        SandboxPhysicsSystem.RegisterPlanet(this);
    }

    public void ApplySnapshot(SandboxPlanetSnapshot newSnapshot) {
        // 每个行星持有自己的快照，热学演化时不会改到 catalog 里共享的那份。
        snapshot = newSnapshot != null ? newSnapshot.Clone() : null;
        if (snapshot == null) return;

        massEarth = snapshot.massEarth;
        special = snapshot.IsSpecial;
        radiusEarth = snapshot.radiusEarth;
        atmosphereMassEarth = snapshot.atmosphereMassEarth;
        temperatureK = snapshot.temperatureK;
        rotationPeriodDays = snapshot.rotationPeriodDays >= 0.0
            ? snapshot.rotationPeriodDays
            : 1.0;
        rotationDirection = snapshot.rotationDirection >= 0 ? 1 : -1;
        axialTiltDeg = snapshot.axialTiltDeg;
        greenhouseFactor = System.Math.Clamp(snapshot.greenhouseFactor, 0.0, 1.0);
        surfaceWaterDepthM = System.Math.Max(0.0, snapshot.surfaceWaterDepthM);
        landHeightMaxM = snapshot.landHeightMaxM > 0.0 ? snapshot.landHeightMaxM : 20000.0;
        soluteConcentrationMmolL = System.Math.Clamp(
            snapshot.soluteConcentrationMmolL, 0.0, 5400.0);
        UpdateWaterTemperaturePoints();
        trailColor = snapshot.trailColor;

        ApplyRenderScale();
        ApplyAxialTilt();

        // 彩蛋长方体：不按球体渲染，跳过地表/大气/海平面那套。
        if (snapshot.isBox) {
            SetupBoxVisual();
            return;
        }

        if (planetMaterial == null) return;

        planetMaterial.SetColor("_BaseColor", Color.white);

        // 地表层：没有贴图时保持白色。
        _surfaceTexture = LoadTexture(snapshot.surfaceTexturePath);
        planetMaterial.SetTexture("_SurfaceMap",
            _surfaceTexture != null ? _surfaceTexture : Texture2D.whiteTexture);

        // A 类使用现成大气贴图；B 类只用颜色球壳，不再生成云层。
        _atmosphereTexture = LoadTexture(snapshot.atmosphereTexturePath);
        SetupAtmosphere();

        // 高程图：黑色 = 0 km，白色 = 20 km；只有存在高程图时才启用海平面。
        _heightMapTexture = LoadTexture(snapshot.heightMapPath);
        planetMaterial.SetTexture("_HeightMap",
            _heightMapTexture != null ? _heightMapTexture : Texture2D.blackTexture);
        planetMaterial.SetFloat("_HasHeightMap", _heightMapTexture != null ? 1f : 0f);
        planetMaterial.SetFloat("_WaterDepthM", (float)surfaceWaterDepthM);
        planetMaterial.SetFloat("_LandHeightMaxM", (float)landHeightMaxM);
        planetMaterial.SetFloat("_FreezePointK", (float)freezingPointK);
        planetMaterial.SetFloat("_BoilPointK", (float)boilingPointK);
        // 水色 RGB(29,44,70)
        planetMaterial.SetColor("_WaterColor",
            new Color(29f / 255f, 44f / 255f, 70f / 255f, 1f));
        planetMaterial.SetFloat("_HasWater",
            _heightMapTexture != null && surfaceWaterDepthM > 0.0 ? 1f : 0f);
        planetMaterial.SetColor("_IceColor", new Color(0.78f, 0.80f, 0.82f, 1f));
        planetMaterial.SetColor("_SnowColor", new Color(0.96f, 0.97f, 1f, 1f));

        EnsureTemperatureTexture();
        planetMaterial.SetColor("_AmbientColor", new Color(0.008f, 0.010f, 0.016f, 1f));

        // 高温行星自发光：直接用黑体颜色，并加强到明显可见。
        planetMaterial.SetColor("_EmissionColor", snapshot.emissionColor);
        planetMaterial.SetFloat("_EmissionStrength", Mathf.Max(0f, snapshot.emissionStrength) * 1.5f);

        SandboxPhysicsSystem.SyncPlanet(this);
    }

    public void SetSoluteConcentration(double concentrationMmolL) {
        soluteConcentrationMmolL = System.Math.Clamp(concentrationMmolL, 0.0, 5400.0);
        if (snapshot != null) {
            snapshot.soluteConcentrationMmolL = soluteConcentrationMmolL;
        }
        UpdateWaterTemperaturePoints();
        if (planetMaterial != null) {
            planetMaterial.SetFloat("_FreezePointK", (float)freezingPointK);
            planetMaterial.SetFloat("_BoilPointK", (float)boilingPointK);
        }
    }

    private void UpdateWaterTemperaturePoints() {
        if (snapshot == null) {
            freezingPointK = 273.15;
            boilingPointK = 373.15;
            return;
        }

        double pressureKpa = System.Math.Max(snapshot.pressureAtm * 101.325, 0.0001);
        double concentration = soluteConcentrationMmolL;

        // C++ 参考公式：NaCl 稀溶液，c 用 mmol/L，P 用 kPa。
        double boilWaterC = 1730.63 /
            (8.07131 - System.Math.Log10(7.50062 * pressureKpa)) - 233.426;
        boilWaterC += 0.512 * 2.0 * (concentration / 1000.0) * 0.94;

        double freezeWaterC = -0.0742 * (pressureKpa / 1000.0 - 0.101325);
        freezeWaterC -= 1.86 * 2.0 * (concentration / 1000.0) * 0.94;
        freezeWaterC = System.Math.Max(freezeWaterC, -21.1);

        boilingPointK = boilWaterC + 273.15;
        freezingPointK = freezeWaterC + 273.15;
    }

    public void SetSurfaceWaterDepth(double depthM) {
        surfaceWaterDepthM = System.Math.Max(0.0, depthM);
        if (snapshot != null) snapshot.surfaceWaterDepthM = surfaceWaterDepthM;
        if (planetMaterial != null) {
            planetMaterial.SetFloat("_WaterDepthM", (float)surfaceWaterDepthM);
            planetMaterial.SetFloat("_HasWater",
                _heightMapTexture != null && surfaceWaterDepthM > 0.0 ? 1f : 0f);
        }
    }

    /// <summary>
    /// 吸收碎片后增加质量，但暂不改变半径和视觉大小。
    /// 密度/重力/表面气压同步到快照，质量守恒。
    /// </summary>
    public void ApplyAbsorbedMass(double massKg) {
        if (massKg <= 0.0 || snapshot == null) return;

        massEarth += massKg / SandboxPhysicsSystem.EarthMassKg;
        snapshot.massEarth = massEarth;

        double r2 = Math.Max(radiusEarth * radiusEarth, 1e-30);
        double r3 = r2 * Math.Max(radiusEarth, 1e-30);
        snapshot.densityGcm3 = SandboxPlanetModel.EarthDensityGcm3 * massEarth / r3;
        snapshot.gravityG = massEarth / r2;
        snapshot.pressureAtm = atmosphereMassEarth * massEarth /
                               Math.Max(r3 * Math.Max(radiusEarth, 1e-30), 1e-30);
        // radiusEarth / visualDiameter / transform.localScale 保持不变。
    }

    /// <summary>
    /// 物理热学算出的表面温度回写到外观/快照。只更新温度相关字段，
    /// 不调用 ApplySnapshot，避免把内能重新重置成用户刚才输入的初始值。
    /// </summary>
    public void ApplyThermalState(double newTemperatureK) {
        temperatureK = Mathf.Max(0f, (float)newTemperatureK);
        if (snapshot != null) snapshot.temperatureK = temperatureK;

        var (r, g, b) = Spectrum.GetBlackbodyColor(temperatureK);
        Color emissionColor = new(r, g, b, 1f);
        float emissionStrength = 0f;
        if (temperatureK >= 500.0) {
            emissionStrength = Mathf.Clamp01(
                Utils.GetStarEmissionStrength(temperatureK / SandboxPlanetModel.SunSurfaceK) * 0.35f);
        }

        if (snapshot != null) {
            snapshot.emissionColor = emissionColor;
            snapshot.emissionStrength = emissionStrength;
        }

        if (planetMaterial != null) {
            planetMaterial.SetColor("_EmissionColor", emissionColor);
            planetMaterial.SetFloat("_EmissionStrength", emissionStrength * 1.5f);
        }
    }

    /// <summary>着色器里恒星数据纹理的容量。</summary>
    public const int ShaderMaxStarLights = 256;

    // 用一张 256x2 的浮点纹理保存恒星数据：
    //   第 0 行：xyz = 世界位置，w = 光度
    //   第 1 行：rgb = 黑体颜色
    // 纹理比 shader 数组更可靠，URP/SRP Batcher 下不会出现只读到第 0 个元素的问题。
    private static Texture2D _starDataTexture;
    private static readonly Color[] _starPixels = new Color[ShaderMaxStarLights * 2];

    /// <summary>
    /// 把所有恒星的位置/颜色/光度写入全局纹理。
    /// 每帧调用一次，所有行星共享同一份数据，新加恒星后现有行星立刻生效。
    /// </summary>
    public static void UploadStarLights(List<SandboxStar> stars) {
        if (_starDataTexture == null) {
            _starDataTexture = new Texture2D(
                ShaderMaxStarLights, 2, TextureFormat.RGBAFloat, false, true) {
                name = "SandboxStarData",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        int count = 0;
        if (stars != null) {
            foreach (SandboxStar star in stars) {
                if (count >= ShaderMaxStarLights) break;
                if (star == null || star.snapshot == null) continue;

                Vector3 position = star.transform.position;
                _starPixels[count] = new Color(
                    position.x, position.y, position.z,
                    (float)System.Math.Max(star.snapshot.luminosityLsun, 1e-8));

                var (r, g, b) = Spectrum.GetBlackbodyColor(star.snapshot.temperatureK);
                _starPixels[ShaderMaxStarLights + count] = new Color(r, g, b, 1f);
                count++;
            }
        }

        for (int i = count; i < ShaderMaxStarLights; i++) {
            _starPixels[i] = Color.clear;
            _starPixels[ShaderMaxStarLights + i] = Color.clear;
        }

        _starDataTexture.SetPixels(_starPixels);
        _starDataTexture.Apply(false, false);

        Shader.SetGlobalTexture("_SandboxStarData", _starDataTexture);
        Shader.SetGlobalFloat("_SandboxStarCount", count);
        Shader.SetGlobalFloat("_SandboxUnitsPerAu",
            (float)SandboxWorldScale.UnitsPerAuRender);
        // 照明淡出阈值只在 SandboxStar 里维护一份，这里同步给 UPB/SandboxPlanet。
        Shader.SetGlobalFloat("_SandboxLightFadeStartAu",
            (float)SandboxStar.IlluminationFadeStartAu);
        Shader.SetGlobalFloat("_SandboxLightFadeEndAu",
            (float)SandboxStar.IlluminationFadeEndAu);
    }

    private void EnsureTemperatureTexture() {
        if (planetMaterial == null) return;
        if (_temperatureTexture == null) {
            _temperatureTexture = new Texture2D(
                SandboxPhysicsBody.SurfaceLongitudeSegments,
                SandboxPhysicsBody.SurfaceLatitudeSegments,
                TextureFormat.RGBA32, false, true) {
                name = "SurfaceTemperatureMap",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _temperaturePixels = new Color[
                SandboxPhysicsBody.SurfaceLongitudeSegments *
                SandboxPhysicsBody.SurfaceLatitudeSegments];
            for (int i = 0; i < _temperaturePixels.Length; i++) {
                _temperaturePixels[i] = Color.clear;
            }
            _temperatureTexture.SetPixels(_temperaturePixels);
            _temperatureTexture.Apply(false, false);
        }
        if (_temperatureValueTexture == null) {
            _temperatureValueTexture = new Texture2D(
                SandboxPhysicsBody.SurfaceLongitudeSegments,
                SandboxPhysicsBody.SurfaceLatitudeSegments,
                TextureFormat.RFloat, false, true) {
                name = "SurfaceTemperatureValueMap",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _temperatureValues = new float[
                SandboxPhysicsBody.SurfaceLongitudeSegments *
                SandboxPhysicsBody.SurfaceLatitudeSegments];
            _temperatureValueTexture.SetPixelData(_temperatureValues, 0);
            _temperatureValueTexture.Apply(false, false);
        }
        planetMaterial.SetTexture("_TemperatureMap", _temperatureTexture);
        planetMaterial.SetTexture("_TemperatureValueMap", _temperatureValueTexture);
        planetMaterial.SetFloat("_PatchEmissionStrength", 1f);
    }

    public void SetSurfaceTemperatures(double[] temperatures) {
        if (temperatures == null || planetMaterial == null) return;
        EnsureTemperatureTexture();

        // 288 个斑块的黑体颜色 + 两张纹理上传，逐帧做太浪费；
        // 温度场的变化远慢于渲染帧率，10Hz 已经完全看不出差别。
        float now = Time.unscaledTime;
        if (_lastTemperatureUploadTime >= 0f &&
            now - _lastTemperatureUploadTime < TemperatureUploadInterval) {
            return;
        }
        _lastTemperatureUploadTime = now;

        int count = Mathf.Min(temperatures.Length, _temperaturePixels.Length);
        for (int i = 0; i < count; i++) {
            double t = Math.Max(0.0, temperatures[i]);
            var (r, g, b) = Spectrum.GetBlackbodyColor(t);
            float glow = Mathf.Clamp01((float)((t - 500.0) / 1500.0));
            _temperaturePixels[i] = new Color(r, g, b, glow);
            _temperatureValues[i] = (float)t;
        }
        for (int i = count; i < _temperaturePixels.Length; i++) {
            _temperaturePixels[i] = Color.clear;
            _temperatureValues[i] = 0f;
        }
        _temperatureTexture.SetPixels(_temperaturePixels);
        _temperatureTexture.Apply(false, false);
        _temperatureValueTexture.SetPixelData(_temperatureValues, 0);
        _temperatureValueTexture.Apply(false, false);
    }

    // ~~~ 彩蛋长方体 ~~~

    private GameObject _boxVisual;
    private Material _boxMaterial;

    /// <summary>
    /// 用 Cube 代替球体显示；尺寸按数据里的 boxX/Y/Z（单位 km）换算成游戏单位。
    /// </summary>
    private void SetupBoxVisual() {
        if (planetRenderer != null) planetRenderer.enabled = false;

        if (_boxVisual == null) {
            Shader shader = Resources.Load<Shader>("Shaders/EasterEggBox");
            if (shader == null) shader = Shader.Find("UPB/EasterEggBox");
            if (shader != null) _boxMaterial = new Material(shader) { name = planetName + "_BoxMat" };

            _boxVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _boxVisual.name = "BoxVisual";
            Collider collider = _boxVisual.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            _boxVisual.transform.SetParent(transform, false);

            Renderer renderer = _boxVisual.GetComponent<Renderer>();
            if (renderer != null) {
                if (_boxMaterial != null) renderer.sharedMaterial = _boxMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        if (_boxMaterial != null) {
            _boxMaterial.SetTexture("_FrontTex", SafeTexture(snapshot.textureFront));
            _boxMaterial.SetTexture("_SideTex", SafeTexture(snapshot.textureSides));
            _boxMaterial.SetTexture("_TopTex", SafeTexture(snapshot.textureTopBackBottom));
            _boxMaterial.SetColor("_Color", Color.white);
        }

        ApplyBoxScale();
    }

    private void ApplyBoxScale() {
        if (_boxVisual == null || snapshot == null) return;

        // 盒子的本地缩放是相对父星球的：用真实尺寸相除，不能拿已经乘过世界缩放的 localScale。
        double parentScale = System.Math.Max(snapshot.visualDiameter, 1e-9);
        Vector3 worldUnits = new(
            (float)(snapshot.boxXKm / CelestialScale.KmPerUnit / parentScale),
            (float)(snapshot.boxYKm / CelestialScale.KmPerUnit / parentScale),
            (float)(snapshot.boxZKm / CelestialScale.KmPerUnit / parentScale));
        _boxVisual.transform.localScale = worldUnits;
    }

    private static Texture2D SafeTexture(string path) {
        Texture2D tex = LoadTexture(path);
        return tex != null ? tex : Texture2D.whiteTexture;
    }

    private void SetupAtmosphere() {
        bool hasAtmosphere = snapshot != null && snapshot.atmosphereMassEarth > 0.0;
        // 大气贴图（可能带 alpha 的云层）叠在地表上：金星=浓密大气，地球=云层。
        if (_atmosphereTexture != null) {
            planetMaterial.SetTexture("_AtmosphereMap", _atmosphereTexture);
            planetMaterial.SetFloat("_AtmosphereStrength", 1f);
        } else {
            planetMaterial.SetTexture("_AtmosphereMap", Texture2D.blackTexture);
            planetMaterial.SetFloat("_AtmosphereStrength", 0f);
        }

        // 默认“有贴图就不画外圈光晕”（金星）；数据里显式打开 atmosphereShell 才两者都要（地球）。
        bool wantShell = hasAtmosphere &&
                         (_atmosphereTexture == null || snapshot.atmosphereShell);
        if (!wantShell) {
            HideAtmosphereHalo();
            return;
        }

        double pressureKpa = snapshot.pressureAtm * 101.325;
        float alpha0 = pressureKpa >= 50.0
            ? 150f / 255f
            : (float)(150.0 / 255.0 * Mathf.Clamp01((float)(pressureKpa / 50.0)));
        if (alpha0 <= 0.001f) {
            HideAtmosphereHalo();
            return;
        }

        ShowAtmosphereShell(snapshot.atmosphereColor, alpha0);
    }

    private void ShowAtmosphereShell(Color color, float alpha0) {
        if (_atmosphereHalo == null) {
            Shader shader = Resources.Load<Shader>("Shaders/PlanetAtmosphere");
            if (shader == null) return;

            _atmosphereHalo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _atmosphereHalo.name = "AtmosphereShell";
            _atmosphereHalo.transform.SetParent(transform, false);
            _atmosphereHalo.transform.localPosition = Vector3.zero;
            _atmosphereHalo.transform.localScale = Vector3.one * 1.05f;
            Collider collider = _atmosphereHalo.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            _atmosphereHaloMaterial = new Material(shader) {
                name = "AtmosphereShellMaterial"
            };
            Renderer renderer = _atmosphereHalo.GetComponent<Renderer>();
            renderer.material = _atmosphereHaloMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        _atmosphereHalo.SetActive(true);
        _atmosphereHaloMaterial.SetColor("_Color", color);
        _atmosphereHaloMaterial.SetFloat("_Alpha0", alpha0);
        UpdateAtmosphereShellTransform();
    }

    private void UpdateAtmosphereShellTransform() {
        if (_atmosphereHaloMaterial == null) return;
        _atmosphereHaloMaterial.SetVector("_CenterWS", transform.position);
        _atmosphereHaloMaterial.SetFloat("_PlanetRadius", transform.lossyScale.x * 0.5f);
    }

    private void HideAtmosphereHalo() {
        if (_atmosphereHalo != null) _atmosphereHalo.SetActive(false);
    }

    private static Texture2D LoadTexture(string path) {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Resources.Load<Texture2D>(path);
    }

    private void OnDestroy() {
        SandboxPhysicsSystem.Unregister(this);
        if (planetMaterial != null) Destroy(planetMaterial);
        if (_boxMaterial != null) Destroy(_boxMaterial);
        if (_atmosphereHaloMaterial != null) Destroy(_atmosphereHaloMaterial);
        if (_temperatureTexture != null) Destroy(_temperatureTexture);
        if (_temperatureValueTexture != null) Destroy(_temperatureValueTexture);
    }
}
