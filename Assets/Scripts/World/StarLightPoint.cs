using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 恒星拉远后的“光点”表现。
///
/// 规则：
/// 1. 相机到恒星的距离超过恒星直径的 20 倍后，隐藏 3D 恒星，改画一个屏幕空间光点；
/// 2. 光点的核心圆盘直径取切换瞬间恒星在屏幕上的投影像素数，之后不再随距离变小；
/// 3. 视星等 &lt; -17 时保持切换瞬间的亮度和大小；
/// 4. 视星等 -17 ~ +6 之间逐渐变小、变暗，到 +6 时完全看不见；
///    沙盘模式（3D billboard）例外：至少保留 2.5 像素、0.18 透明度的标记点，
///    这样缩放到百万光年外也不会整个系统消失。
/// 5. 光点颜色直接复用 <see cref="Spectrum.GetBlackbodyColor"/> 的黑体颜色，
///    核心稍微增白，外圈是同色光晕。
///
/// 绝对星等按 M = 4.83 - 2.5 log10(L/L☉) 计算，距离用 1 Unity 单位 = 92760 km
/// 换算成 pc 后求视星等。
/// </summary>
public class StarLightPoint : MonoBehaviour {
    public const double KmPerUnit = CelestialScale.KmPerUnit;
    public const double SunAbsoluteMagnitude = 4.83;
    public const double SwitchDiameterMultiple = 20.0;
    public const double FadeStartMagnitude = -17.0;
    public const double FadeEndMagnitude = 6.0;

    /// <summary>光晕外径相对核心圆盘直径的倍数。</summary>
    public const float HaloScale = 9.0f;

    private const double PcKm = 3.0856775814913673e13;
    private const double DefaultTemperature = 5773.15;
    private const float MinDotPixels = 0.01f;
    private const float MaxExtendedZoom = 1e13f;

    /// <summary>沙盘 3D 星点的最小屏幕直径，避免亚像素星点随相机移动闪烁。</summary>
    public const float SandboxMinDotPixels = 2.5f;

    /// <summary>沙盘 3D 星点的最低透明度；拉到百万光年外仍保留一个可辨认的标记点。</summary>
    public const float SandboxMinDotAlpha = 0.18f;

    [Tooltip("必要时自动放宽相机的最大缩放距离，保证能一直缩放到 +6 等消失。")]
    public bool extendCameraMaxZoom = true;

    private GameObject _star;
    private Camera _camera;
    private UPB3DCamera _zoomCamera;
    private Renderer[] _starRenderers;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private Image _dot;
    private Sprite _dotSprite;
    private Texture2D _dotTexture;
    private Material _dotMaterial;
    private bool _useShader;

    // 沙盘使用真正的 3D billboard 光点：它参与深度测试，行星会按实际遮挡面积挡住它。
    private bool _worldSpace;
    private SpriteRenderer _billboardRenderer;
    private Material _billboardMaterial;
    private float _billboardPixels;
    private Color _billboardColor = Color.clear;

    private double _luminositySolar = 1.0;
    private double _temperatureK = DefaultTemperature;
    private float _switchPixelSize = 1f;
    private bool _dotMode;
    private bool _ready;

    /// <summary>当前是否处于光点模式。</summary>
    public bool IsDotMode => _dotMode;

    /// <summary>第二种恒星渲染模式：无论拉多远都保持 3D 球体，不切换成星点。</summary>
    public bool SuppressDot { get; private set; }

    public void SetSuppressDot(bool suppress) {
        SuppressDot = suppress;
        if (suppress) {
            if (_dotMode) SetDotMode(false);
            if (_billboardRenderer != null) _billboardRenderer.enabled = false;
            if (_dot != null) _dot.gameObject.SetActive(false);
        }
    }

    /// <summary>调整光点 Canvas 的排序；沙盘里要放在 UI 面板下面。</summary>
    public void SetSortingOrder(int order) {
        if (_canvas != null) _canvas.sortingOrder = order;
    }

    /// <summary>
    /// 沙盘专用：把星点改成世界空间的 billboard。
    /// 它不再覆盖在屏幕上，而是正常参与深度测试，被行星挡住时只挡实际投影面积。
    /// </summary>
    public void UseWorldSpaceBillboard() {
        if (_worldSpace) return;
        _worldSpace = true;

        if (_canvas != null) _canvas.gameObject.SetActive(false);
        if (_dot != null) _dot.gameObject.SetActive(false);

        CreateBillboard();
    }

    /// <summary>光点中心圆盘的屏幕直径（像素），不含光晕。</summary>
    public float DotPixelSize => _worldSpace
        ? _billboardPixels
        : _dot != null ? _dot.rectTransform.sizeDelta.x / (_useShader ? HaloScale : 1f) : 0f;

    /// <summary>光点当前的透明度（0~1）。</summary>
    public float DotAlpha => _worldSpace
        ? _billboardColor.a
        : _dot != null ? _dot.color.a : 0f;

    /// <summary>光点当前的颜色（含透明度）。</summary>
    public Color DotColor => _worldSpace
        ? _billboardColor
        : _dot != null ? _dot.color : Color.clear;

    /// <summary>光点当前是否实际显示。</summary>
    public bool DotVisible => _worldSpace
        ? _billboardRenderer != null && _billboardRenderer.enabled
        : _dot != null && _dot.isActiveAndEnabled;

    /// <summary>切换时光点的基准像素直径。</summary>
    public float SwitchPixelSize => _switchPixelSize;

    /// <summary>最近一次计算出的视星等。</summary>
    public double ApparentMagnitude { get; private set; }

    /// <summary>
    /// 在恒星旁边创建一个光点控制器。
    /// </summary>
    public static StarLightPoint Attach(GameObject star, Camera camera) {
        if (star == null) return null;

        if (camera == null) camera = Camera.main;
        if (camera == null) camera = UnityEngine.Object.FindObjectOfType<Camera>();
        if (camera == null) return null;

        GameObject go = new("StarLightPoint");
        if (star.transform.parent != null) {
            go.transform.SetParent(star.transform.parent, false);
        }

        StarLightPoint point = go.AddComponent<StarLightPoint>();
        point.Init(star, camera);
        return point;
    }

    private void Init(GameObject star, Camera camera) {
        if (_ready) return;

        _star = star;
        _camera = camera;
        _zoomCamera = camera.GetComponent<UPB3DCamera>();
        _starRenderers = star.GetComponentsInChildren<Renderer>(true);

        CreateOverlay();
        _ready = true;
        UpdateMaxZoom();
    }

    /// <summary>
    /// 由演化场景在每一步更新恒星的物理量。
    /// </summary>
    /// <param name="luminosityLsun">当前光度，单位 L☉。</param>
    /// <param name="temperatureK">当前表面温度，单位 K。</param>
    public void SetStar(double luminosityLsun, double temperatureK) {
        _luminositySolar = luminosityLsun;
        if (temperatureK > 0) _temperatureK = temperatureK;
        UpdateMaxZoom();
    }

    private void LateUpdate() {
        Tick();
    }

    /// <summary>
    /// 根据当前相机距离刷新显示。LateUpdate 会自动调用；也可以手动调用（测试/工具）。
    /// </summary>
    public void Tick() {
        if (!_ready) return;

        // 黑体光球模式：始终保持 3D 球体可见，不切成星点。
        if (SuppressDot) {
            if (_dotMode) SetDotMode(false);
            return;
        }

        double diameter = System.Math.Max(_star.transform.localScale.x, 1e-6);
        double distance = Vector3.Distance(_camera.transform.position, _star.transform.position);
        double switchDistance = diameter * SwitchDiameterMultiple;

        bool wantDot = distance > switchDistance;
        if (wantDot != _dotMode) {
            if (wantDot) {
                _switchPixelSize = System.Math.Max(
                    ProjectedDiameterPixels(diameter, switchDistance, _camera.fieldOfView, Screen.height),
                    MinDotPixels);
            }
            SetDotMode(wantDot);
        }

        if (_dotMode) RefreshDot(distance);
    }

    private void SetDotMode(bool dotMode) {
        _dotMode = dotMode;

        if (_starRenderers != null) {
            foreach (Renderer renderer in _starRenderers) {
                if (renderer != null) renderer.enabled = !dotMode;
            }
        }

        if (_worldSpace) {
            if (_billboardRenderer != null) _billboardRenderer.enabled = dotMode;
            if (_dot != null) _dot.gameObject.SetActive(false);
            return;
        }

        if (_dot != null) {
            _dot.gameObject.SetActive(dotMode);
            if (dotMode) _dot.enabled = true;
        }
    }

    private void RefreshDot(double distance) {
        // distance 是渲染单位；视星等要用真实基准单位。
        double realDistance = SandboxWorldScale.ToVirtual(distance);
        double magnitude = ApparentMagnitudeAt(_luminositySolar, realDistance);
        ApparentMagnitude = magnitude;

        double fade = FadeFactor(magnitude);
        if (fade <= 0.0001) {
            if (!_worldSpace) {
                HideDot();
                return;
            }
            // 沙盘里即使视星等已经很暗，也保留一个最小星点，缩放时不会“突然消失”。
            fade = 0.0001;
        }

        float pixels = _switchPixelSize * (float)fade;
        if (!_worldSpace && pixels < MinDotPixels) {
            HideDot();
            return;
        }

        Color color = GetStarColor();
        color.a = (float)fade;

        if (_worldSpace) {
            color.a = Mathf.Max(color.a, SandboxMinDotAlpha);
            pixels = Mathf.Max(pixels, SandboxMinDotPixels);
            RefreshBillboard(distance, pixels, color);
            return;
        }

        _dot.enabled = true;

        // 核心圆盘保持为切换瞬间恒星的像素直径；光晕向四周再扩 HaloScale 倍。
        float imagePixels = _useShader ? pixels * HaloScale : pixels;
        _dot.rectTransform.sizeDelta = new Vector2(imagePixels, imagePixels);
        _dot.color = color;

        Vector3 screenPoint = _camera.WorldToScreenPoint(_star.transform.position);
        if (screenPoint.z <= 0f) {
            _dot.enabled = false;
            return;
        }

        // 屏幕空间模式下才需要手动射线遮挡；3D billboard 用深度测试。
        if (IsOccluded()) {
            _dot.enabled = false;
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint, null, out Vector2 localPoint)) {
            // localPoint 以 Canvas 的 pivot 为原点；光点锚在 Canvas 中心，
            // 所以再减去 rect.center，保证无论 pivot 在哪都能对准恒星。
            _dot.rectTransform.anchoredPosition = localPoint - _canvasRect.rect.center;
        } else {
            _dot.enabled = false;
        }
    }

    private void HideDot() {
        if (_worldSpace) {
            if (_billboardRenderer != null) _billboardRenderer.enabled = false;
            _billboardColor = Color.clear;
        } else if (_dot != null) {
            _dot.enabled = false;
        }
    }

    private void RefreshBillboard(double distance, float pixels, Color color) {
        if (_billboardRenderer == null || _camera == null || _star == null) return;

        _billboardPixels = pixels;

        // 3D 星点用加法混合，稍微提高 alpha 让亮度更接近原来的屏幕空间星点。
        color.a = Mathf.Clamp01(color.a * 1.8f);
        _billboardColor = color;

        // 放在恒星位置，始终面向相机。
        transform.position = _star.transform.position;
        transform.rotation = _camera.transform.rotation;

        // 把屏幕像素大小换算成世界空间尺寸。
        double halfFov = _camera.fieldOfView * 0.5 * System.Math.PI / 180.0;
        double worldHeight = 2.0 * distance * System.Math.Tan(halfFov) *
                             (pixels / System.Math.Max(Screen.height, 1));
        float size = Mathf.Max((float)worldHeight, 0.0001f);
        float spriteHeight = _dotSprite != null
            ? _dotSprite.bounds.size.y
            : 1f;
        float scale = size / Mathf.Max(spriteHeight, 0.0001f);
        transform.localScale = new Vector3(scale, scale, scale);

        if (_billboardMaterial != null) {
            _billboardMaterial.SetColor("_Color", color);
        }
        _billboardRenderer.color = Color.white;
        _billboardRenderer.enabled = true;
    }

    /// <summary>
    /// 星点只是屏幕空间的 UI 图层，不参与深度测试，必须自己判断是否被前面的天体挡住。
    /// 从相机往恒星方向打一条射线：正常只能撞到恒星自己，撞到别的物体就是被遮挡。
    /// </summary>
    private bool IsOccluded() {
        if (_star == null || _camera == null) return false;

        Vector3 origin = _camera.transform.position;
        Vector3 target = _star.transform.position;
        Vector3 direction = target - origin;
        double distance = direction.magnitude;
        if (distance < 1e-9) return false;

        // 射线终点略微超过恒星中心，保证能命中恒星自己的碰撞体。
        float maxDistance = (float)(distance * 1.02 + 1e-6);
        Ray ray = new(origin, direction / (float)distance);
        RaycastHit[] hits = Physics.RaycastAll(
            ray, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits) {
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(_star.transform)) return false;

            // 允许屏幕空间上贴着恒星边缘的遮挡（比如刚好擦过行星边缘），
            // 但整体被盖住时直接隐藏。
            return true;
        }
        return false;
    }

    private Color GetStarColor() {
        double temperature = _temperatureK > 0 ? _temperatureK : DefaultTemperature;
        var (r, g, b) = Spectrum.GetBlackbodyColor(temperature);
        return new Color(r, g, b, 1f);
    }

    /// <summary>光度 → 绝对星等（太阳为 4.83）。</summary>
    public static double AbsoluteMagnitude(double luminositySolar) {
        double luminosity = System.Math.Max(luminositySolar, 1e-30);
        return SunAbsoluteMagnitude - 2.5 * System.Math.Log10(luminosity);
    }

    /// <summary>光度 + 场景距离（Unity 单位） → 视星等。</summary>
    public static double ApparentMagnitudeAt(double luminositySolar, double distanceUnits) {
        double distancePc = distanceUnits * KmPerUnit / PcKm;
        if (distancePc <= 0) return double.PositiveInfinity;
        return AbsoluteMagnitude(luminositySolar) + 5.0 * System.Math.Log10(distancePc / 10.0);
    }

    /// <summary>
    /// 视星等 → 亮度/尺寸系数：-17 等及更亮为 1，+6 等及更暗为 0，中间线性过渡。
    /// </summary>
    public static double FadeFactor(double apparentMagnitude) {
        if (apparentMagnitude <= FadeStartMagnitude) return 1.0;
        if (apparentMagnitude >= FadeEndMagnitude) return 0.0;
        return (FadeEndMagnitude - apparentMagnitude) / (FadeEndMagnitude - FadeStartMagnitude);
    }

    /// <summary>
    /// 小目标在屏幕上的投影直径（像素）。distanceUnits 为相机到目标中心的距离。
    /// </summary>
    public static float ProjectedDiameterPixels(
            double diameterUnits, double distanceUnits, double verticalFovDeg, double screenHeight) {
        if (distanceUnits <= 0 || screenHeight <= 0) return 0f;

        double halfFovRad = verticalFovDeg * 0.5 * System.Math.PI / 180.0;
        double pixels = diameterUnits * (screenHeight * 0.5) / (distanceUnits * System.Math.Tan(halfFovRad));
        return (float)System.Math.Max(pixels, 0.0);
    }

    /// <summary>当前光度下，达到指定视星等需要的场景距离（Unity 单位）。</summary>
    public double DistanceForMagnitude(double magnitude) {
        double distancePc = 10.0 * System.Math.Pow(
            10.0, (magnitude - AbsoluteMagnitude(_luminositySolar)) / 5.0);
        return distancePc * PcKm / KmPerUnit;
    }

    private void UpdateMaxZoom() {
        if (!extendCameraMaxZoom || _zoomCamera == null) return;

        double required = DistanceForMagnitude(FadeEndMagnitude);
        if (required <= 0 || double.IsInfinity(required) || double.IsNaN(required)) return;

        double wanted = required * 1.02;
        if (wanted <= _zoomCamera.maxZoom) return;

        // 相机最远距离有统一硬上限（100 亿单位），这里不能把它顶上去。
        double capped = System.Math.Min(wanted, MaxExtendedZoom);
        capped = System.Math.Min(capped, UPB3DCamera.HardMaxZoomUnits);
        if (capped <= _zoomCamera.maxZoom) return;

        _zoomCamera.maxZoom = (float)capped;
    }

    private void CreateBillboard() {
        if (_billboardRenderer != null) return;

        _billboardRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (_billboardRenderer == null) {
            _billboardRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        _billboardRenderer.sprite = _dotSprite;
        _billboardRenderer.color = Color.white;
        _billboardRenderer.sortingOrder = -10;
        _billboardRenderer.enabled = false;

        Shader shader = Shader.Find("UPB/StarPoint3D");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        _billboardMaterial = new Material(shader) {
            name = "StarPointBillboardMaterial",
            color = Color.white
        };
        _billboardRenderer.material = _billboardMaterial;
    }

    private void CreateOverlay() {
        GameObject canvasGo = new(
            "StarLightPointCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        _canvasRect = canvasGo.GetComponent<RectTransform>();

        GameObject dotGo = new("Dot", typeof(RectTransform), typeof(Image));
        dotGo.transform.SetParent(canvasGo.transform, false);

        _dot = dotGo.GetComponent<Image>();
        _dotSprite = CreateGlowSprite();
        _dot.sprite = _dotSprite;
        _dot.type = Image.Type.Simple;
        _dot.preserveAspect = true;
        _dot.raycastTarget = false;
        _dot.color = Color.white;

        Shader shader = Resources.Load<Shader>("Shaders/StarLightPoint");
        if (shader == null) shader = Shader.Find("UPB/StarLightPoint");
        if (shader != null) {
            _dotMaterial = new Material(shader);
            _dotMaterial.SetFloat("_CoreRadius", 1f / HaloScale);
            _dot.material = _dotMaterial;
            _useShader = true;
        } else {
            Debug.LogError("StarLightPoint: 找不到 UPB/StarLightPoint shader，退回普通贴图光点");
        }

        RectTransform rect = _dot.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(48f, 48f);

        dotGo.SetActive(false);
    }

    private Sprite CreateGlowSprite() {
        const int size = 128;

        _dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _dotTexture.name = "StarLightPointGlow";
        _dotTexture.wrapMode = TextureWrapMode.Clamp;
        _dotTexture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);

                // 中心保持为实心圆点，外圈带一点柔和光晕。
                float disk = 1f - Mathf.SmoothStep(0.80f, 0.98f, r);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 3f) * 0.35f;
                float alpha = Mathf.Clamp01(disk + halo);

                pixels[y * size + x] = new Color32(
                    255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        _dotTexture.SetPixels32(pixels);
        _dotTexture.Apply(false, true);

        return Sprite.Create(
            _dotTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect);
    }

    private void OnDestroy() {
        if (Application.isPlaying) {
            if (_dotMaterial != null) Destroy(_dotMaterial);
            if (_billboardMaterial != null) Destroy(_billboardMaterial);
            if (_dotSprite != null) Destroy(_dotSprite);
            if (_dotTexture != null) Destroy(_dotTexture);
        } else {
            if (_dotMaterial != null) DestroyImmediate(_dotMaterial);
            if (_billboardMaterial != null) DestroyImmediate(_billboardMaterial);
            if (_dotSprite != null) DestroyImmediate(_dotSprite);
            if (_dotTexture != null) DestroyImmediate(_dotTexture);
        }
    }
}
