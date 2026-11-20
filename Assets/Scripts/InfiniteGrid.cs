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

    [Header("颜色")]
    public Color gridColor = new(0.2f, 0.5f, 1f, 0.35f);
    public Color majorGridColor = new(0.5f, 0.8f, 1f, 0.9f);

    [Header("Shader")]
    [Tooltip("可指定抗锯齿网格 Shader；留空则使用内置的顶点色 Unlit Shader")]
    public Shader gridShader;

    private static readonly int GridLineColor = Shader.PropertyToID("_GridLineColor");
    private static readonly int GridAxisColor = Shader.PropertyToID("_GridAxisColor");
    private Camera _mainCamera;
    private LayerData[] _layers;
    private bool _isVisible;
    private bool _useAntialiasedGrid;

    private class LayerData {
        public GameObject go;
        public Mesh mesh;
        public MeshRenderer renderer;
        public Material material;
        public double relativeMultiplier; // 1, 10, 100...
    }

    void Start() {
        kmPerUnit = CelestialScale.KmPerUnit;
        _mainCamera = Camera.main;

        if (gridShader == null) {
            gridShader = Shader.Find("Sprites/Default");
        }
        _useAntialiasedGrid = gridShader != null &&
            gridShader.FindPropertyIndex("_GridLineColor") >= 0 && gridShader.FindPropertyIndex("_GridAxisColor") >= 0;

        _layers = new LayerData[layerCount];
        for (int i = 0; i < layerCount; i++) {
            _layers[i] = CreateLayer(i);
        }

        SetVisible(startVisible);
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
        if (_useAntialiasedGrid) {
            mat.SetVector(GridLineColor, gridColor);
            mat.SetVector(GridAxisColor, majorGridColor);
        }
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Mesh mesh = _useAntialiasedGrid ? BuildGridPlane(gridExtent) : BuildGridMesh(1.0, gridExtent);
        mf.sharedMesh = mesh;

        return new LayerData {
            go = go,
            mesh = mesh,
            renderer = mr,
            material = mat,
            relativeMultiplier = System.Math.Pow(layerMultiplier, index)
        };
    }

    static Mesh BuildGridPlane(int extent) {
        float e = Mathf.Max(1, extent);
        return new Mesh {
            name = "GridPlane_Unit",
            vertices = new[] {
                new Vector3(-e, 0, -e), new Vector3(-e, 0, e),
                new Vector3(e, 0, e), new Vector3(e, 0, -e)
            },
            // UV 以格子为单位，生成的线条和原有 Mesh 网格对齐。
            uv = new[] { new Vector2(-e, -e), new Vector2(-e, e), new Vector2(e, e), new Vector2(e, -e) },
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
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

        // distance 是相机到网格平面 (y=0) 的垂直距离，单位：Unity 单位
        float distance = Mathf.Abs(_mainCamera.transform.position.y);
        if (distance < 1e-6f) distance = 1e-6f;

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
        float logHalf = (logOut - logIn) * 0.5f;

        foreach (var layer in _layers) {
            if (layer == null || layer.material == null) continue;

            double actualGridSizeKm = currentBaseSizeKm * layer.relativeMultiplier;
            double density = distanceKm / actualGridSizeKm;

            float logDensity = Mathf.Log10(Mathf.Max((float)density, 1e-6f));

            // 基础透明度计算
            float alpha = Mathf.Clamp01(1f - Mathf.Abs(logDensity - logMid) / logHalf);

            Color c = gridColor;
            c.a *= alpha;
            layer.material.color = c;
            if (_useAntialiasedGrid) layer.renderer.enabled = alpha > 0f;
        }
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
