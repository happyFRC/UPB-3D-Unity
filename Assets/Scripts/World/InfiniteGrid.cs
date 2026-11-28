using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class InfiniteGrid : MonoBehaviour {
    [Header("换算比例")]
    [Tooltip("1 个 Unity 单位对应多少 km")]
    public double kmPerUnit = CelestialScale.KmPerUnit;

    [Header("网格 Mesh 参数")]
    [Tooltip("每层网格的单边格子数。总跨度 = 2 * gridExtent * stepUnit")]
    public int gridExtent = 100;

    [Header("默认可见性")]
    [Tooltip("启动时是否显示网格（按 G 键可切换）")]
    public bool startVisible = false;

    [Header("动态层级控制")]
    [Tooltip("渲染的网格层数。通常 3~4 层视觉上最舒服。")]
    public int layerCount = 4;

    [Tooltip("每层之间的大小倍数。比如 10 表示 100, 1000, 10000。")]
    public double layerMultiplier = 10.0;

    [Header("淡入淡出 (对数尺度)")]
    [Tooltip("density 小于此值 → 格子太密，淡出")]
    public float fadeInMultiplier = 0.5f;

    [Tooltip("density 大于此值 → 格子太稀，淡出")]
    public float fadeOutMultiplier = 50f;

    [Header("渲染范围（跟随相机）")]
    [Tooltip("网格只渲染“相机到原点距离 × 此倍数”以内的区域，圆盘外不绘制。")]
    public float renderRadiusFactor = 5f;

    [Tooltip("圆盘外缘柔和过渡的宽度（占渲染半径的比例），越大淡出越自然。")]
    [Range(0.02f, 0.9f)]
    public float edgeFadeRatio = 0.3f;

    [Header("颜色")]
    public Color gridColor = new(0.2f, 0.5f, 1f, 0.35f);
    public Color majorGridColor = new(0.5f, 0.8f, 1f, 0.9f);

    [Header("Shader")]
    [Tooltip("留空则自动使用 Resources/Shaders/GridLine（UPB/GridLine）")]
    public Shader gridShader;

    private Camera _mainCamera;
    private LayerData[] _layers;
    private bool _isVisible;
    private float _cameraDistance = 1f;

    private static readonly int ClipCenterId = Shader.PropertyToID("_GridClipCenter");
    private static readonly int ClipRadiusId = Shader.PropertyToID("_GridClipRadius");
    private static readonly int ClipFadeId = Shader.PropertyToID("_GridClipFade");

    private class LayerData {
        public GameObject go;
        public Mesh mesh;
        public MeshRenderer renderer;
        public Material material;
        public double relativeMultiplier; // 1, 10, 100...
    }

    void Start() {
        kmPerUnit = CelestialScale.KmPerUnit / SandboxWorldScale.Scale;
        _mainCamera = Camera.main;

        if (gridShader == null) {
            gridShader = Resources.Load<Shader>("Shaders/GridLine");
        }
        if (gridShader == null) {
            gridShader = Shader.Find("UPB/GridLine");
        }
        if (gridShader == null) {
            Debug.LogWarning("InfiniteGrid: 找不到 UPB/GridLine，回退到 Sprites/Default（网格不会被裁剪）");
            gridShader = Shader.Find("Sprites/Default");
        }

        _layers = new LayerData[layerCount];
        for (int i = 0; i < layerCount; i++) {
            _layers[i] = CreateLayer(i);
        }

        SetVisible(startVisible);
        UpdateClip();
    }

    LayerData CreateLayer(int index) {
        GameObject go = new($"GridLayer_{index}");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        if (gridShader == null) {
            Debug.LogError("InfiniteGrid: 找不到 Shader，网格无法显示");
            return null;
        }

        Material mat = new Material(gridShader);
        mat.color = Color.white;
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // 构建一个步长为 1 的网格
        Mesh mesh = BuildGridMesh(1.0, gridExtent);
        mf.mesh = mesh;

        return new LayerData {
            go = go,
            mesh = mesh,
            renderer = mr,
            material = mat,
            relativeMultiplier = System.Math.Pow(layerMultiplier, index)
        };
    }

    Mesh BuildGridMesh(double stepUnit, int extent) {
        Mesh mesh = new Mesh();
        mesh.name = $"GridMesh_Unit";

        int halfExtent = Mathf.Max(1, extent);
        int linesPerAxis = halfExtent * 2 + 1;
        int totalLines = linesPerAxis * 2;
        int vertexCount = totalLines * 2;

        Vector3[] vertices = new Vector3[vertexCount];
        Color[] colors = new Color[vertexCount];
        int[] indices = new int[vertexCount];

        float halfRange = (float)(halfExtent * stepUnit);
        int index = 0;

        for (int z = -halfExtent; z <= halfExtent; z++) {
            float zPos = (float)(z * stepUnit);
            vertices[index] = new Vector3(-halfRange, 0, zPos);
            vertices[index + 1] = new Vector3(halfRange, 0, zPos);

            Color c = (z == 0) ? majorGridColor : gridColor;
            colors[index] = c;
            colors[index + 1] = c;

            indices[index] = index;
            indices[index + 1] = index + 1;
            index += 2;
        }

        for (int x = -halfExtent; x <= halfExtent; x++) {
            float xPos = (float)(x * stepUnit);
            vertices[index] = new Vector3(xPos, 0, -halfRange);
            vertices[index + 1] = new Vector3(xPos, 0, halfRange);

            Color c = (x == 0) ? majorGridColor : gridColor;
            colors[index] = c;
            colors[index + 1] = c;

            indices[index] = index;
            indices[index + 1] = index + 1;
            index += 2;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void LateUpdate() {
        if (_mainCamera == null || _layers == null) return;

        if (Input.GetKeyDown(KeyCode.G)) {
            SetVisible(!_isVisible);
        }

        if (!_isVisible) return;

        UpdateClip();

        // 世界缩放后，1 个渲染单位对应的 km 会变化；网格步长仍按真实 km 取整。
        kmPerUnit = CelestialScale.KmPerUnit / SandboxWorldScale.Scale;

        // 网格尺度参考距离：默认用相机到平面的高度，保持原来的密度手感；
        // 贴地远飞（高度远小于相机到原点的距离）时自动放大，保证网格 mesh
        // 至少能铺满渲染半径的圆盘，不会在半路露出方形硬边。
        float height = Mathf.Abs(_mainCamera.transform.position.y);
        if (height < 1e-6f) height = 1e-6f;

        float coverageDistance = 10f * Mathf.Max(0.01f, renderRadiusFactor) *
                                 _cameraDistance / Mathf.Max(1, gridExtent);
        float distance = Mathf.Max(height, coverageDistance);

        // 换算成 km
        double distanceKm = distance * kmPerUnit;

        // 算出当前距离对应的 10 的整数次幂（回到之前的阶梯式缩放）
        // 比如距离 1e5 km，baseExp = 5
        double baseExp = System.Math.Floor(System.Math.Log10(distanceKm));

        // 算出一个基准大小
        double currentBaseSizeKm = System.Math.Pow(10, baseExp);

        // 1. 更新每一层的缩放大小
        for (int i = 0; i < _layers.Length; i++) {
            var layer = _layers[i];
            if (layer == null) continue;

            double actualSizeKm = currentBaseSizeKm * layer.relativeMultiplier;
            double actualSizeUnit = actualSizeKm / kmPerUnit;

            layer.go.transform.localScale = new Vector3((float)actualSizeUnit, 1f, (float)actualSizeUnit);
        }

        // 2. 计算透明度
        float logIn = Mathf.Log10(fadeInMultiplier);
        float logOut = Mathf.Log10(fadeOutMultiplier);
        float logMid = (logIn + logOut) * 0.5f;

        foreach (var layer in _layers) {
            if (layer == null || layer.material == null) continue;

            double actualGridSizeKm = currentBaseSizeKm * layer.relativeMultiplier;
            double density = distanceKm / actualGridSizeKm;

            float logDensity = Mathf.Log10(Mathf.Max((float)density, 1e-6f));

            // 基础透明度：在对数尺度上用 smoothstep 过渡，避免阈值处突然出现/消失。
            float tIn = Mathf.InverseLerp(logIn, logMid, logDensity);
            float tOut = Mathf.InverseLerp(logMid, logOut, logDensity);
            float alpha = Mathf.SmoothStep(0f, 1f, tIn) *
                          (1f - Mathf.SmoothStep(0f, 1f, tOut));

            Color c = gridColor;
            c.a *= alpha;
            layer.material.color = c;
        }
    }

    /// <summary>
    /// 把“相机到原点距离 × renderRadiusFactor”的圆盘范围写入 Shader 全局参数。
    /// 圆盘外由 Shader 直接丢弃，圆盘边缘按 edgeFadeRatio 平滑淡出。
    /// </summary>
    void UpdateClip() {
        if (_mainCamera == null) {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        Vector3 origin = transform.position;
        float camDistance = Vector3.Distance(_mainCamera.transform.position, origin);
        if (camDistance < 1e-5f) camDistance = 1e-5f;
        _cameraDistance = camDistance;

        float radius = camDistance * Mathf.Max(0.01f, renderRadiusFactor);
        float fade = radius * Mathf.Clamp(edgeFadeRatio, 0.02f, 0.9f);

        Shader.SetGlobalVector(ClipCenterId, origin);
        Shader.SetGlobalFloat(ClipRadiusId, radius);
        Shader.SetGlobalFloat(ClipFadeId, fade);
    }

    void OnDestroy() {
        if (_layers == null) return;
        foreach (var layer in _layers) {
            if (layer == null) continue;
            if (layer.mesh != null) Destroy(layer.mesh);
            if (layer.material != null) Destroy(layer.material);
        }
    }

    /// <summary>当前网格是否可见（设置面板用）。</summary>
    public bool IsVisible => _isVisible;

    public void SetVisible(bool visible) {
        _isVisible = visible;
        if (_layers == null) return;
        foreach (var layer in _layers) {
            if (layer?.renderer != null)
                layer.renderer.enabled = visible;
        }
    }
}