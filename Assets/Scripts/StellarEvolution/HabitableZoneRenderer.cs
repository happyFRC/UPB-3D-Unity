using UnityEngine;



/// <summary>

/// 宜居带填充圆环。单位环带 mesh（内 0.95 AU、外 1.67 AU），

/// 运行时只改 localScale = sqrt(L) * AU_km / kmPerUnit。

/// </summary>

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class HabitableZoneRenderer : MonoBehaviour {

    [Header("换算")]

    [Tooltip("1 个 Unity 单位对应多少 km")]

    public double kmPerUnit = CelestialScale.KmPerUnit;



    [Header("几何")]

    [Tooltip("环带分段数，越大越圆滑")]

    public int segments = 180;



    [Tooltip("内边界系数（AU）：r_inner = 0.95 * sqrt(L)")]

    public double innerFactor = 0.95;



    [Tooltip("外边界系数（AU）：r_outer = 1.67 * sqrt(L)")]

    public double outerFactor = 1.67;



    [Header("颜色")]

    public Color innerColor = new Color(0.35f, 0.85f, 0.45f, 0.18f);

    public Color outerColor = new Color(0.30f, 0.70f, 0.95f, 0.10f);



    [Header("Shader")]

    [Tooltip("留空则用 Sprites/Default")]

    public Shader ringShader;



    [Header("可见性")]

    public bool visible = false;



    private Mesh _mesh;

    private MeshRenderer _renderer;

    private Material _material;



    private static readonly double AU_KM = CelestialScale.AuKm;



    void Awake() {
        kmPerUnit = CelestialScale.KmPerUnit;

        if (ringShader == null) ringShader = Shader.Find("Sprites/Default");



        _mesh = BuildRingMesh(innerFactor, outerFactor, segments);

        GetComponent<MeshFilter>().mesh = _mesh;



        _renderer = GetComponent<MeshRenderer>();

        _material = new Material(ringShader);

        _material.color = Color.white; // 顶点色已含颜色，材质色乘 1

        _renderer.material = _material;

        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        _renderer.receiveShadows = false;



        SetVisible(visible);

    }



    /// <summary>根据当前光度 L (Lsun) 更新环带尺寸。</summary>

    public void UpdateLuminosity(double luminosity) {

        if (luminosity <= 0.0) {

            SetVisible(false);

            return;

        }

        double scale = System.Math.Sqrt(luminosity) * AU_KM / kmPerUnit;

        transform.localScale = new Vector3((float)scale, 1f, (float)scale);

    }



    public void SetVisible(bool v) {

        visible = v;

        if (_renderer != null) _renderer.enabled = v;

    }



    /// <summary>

    /// 构建单位环带 mesh：内半径 innerFactor、外半径 outerFactor（单位=AU），

    /// 位于 y=0 平面，内外圈用顶点色做渐变。

    /// </summary>

    Mesh BuildRingMesh(double innerR, double outerR, int seg) {

        seg = Mathf.Max(8, seg);

        int vertCount = (seg + 1) * 2; // 每个角度 2 个顶点（内、外），首尾多一份用于 UV/颜色闭合

        var vertices = new Vector3[vertCount];

        var colors = new Color[vertCount];

        var uvs = new Vector2[vertCount];

        var indices = new int[seg * 6];



        for (int i = 0; i <= seg; i++) {

            double t = 2.0 * System.Math.PI * i / seg;

            float cos = (float)System.Math.Cos(t);

            float sin = (float)System.Math.Sin(t);



            int vi = i * 2;

            vertices[vi] = new Vector3((float)innerR * cos, 0f, (float)innerR * sin);

            vertices[vi + 1] = new Vector3((float)outerR * cos, 0f, (float)outerR * sin);



            colors[vi] = innerColor;

            colors[vi + 1] = outerColor;



            uvs[vi] = new Vector2(0f, (float)i / seg);

            uvs[vi + 1] = new Vector2(1f, (float)i / seg);

        }



        for (int i = 0; i < seg; i++) {

            int i0 = i * 2;

            int i1 = i0 + 1;

            int i2 = i0 + 2;

            int i3 = i0 + 3;

            int idx = i * 6;

            // 两个三角形：内i, 外i, 内i+1 / 内i+1, 外i, 外i+1

            indices[idx + 0] = i0;

            indices[idx + 1] = i1;

            indices[idx + 2] = i2;

            indices[idx + 3] = i2;

            indices[idx + 4] = i1;

            indices[idx + 5] = i3;

        }



        var mesh = new Mesh { name = "HabitableZoneRing" };

        mesh.vertices = vertices;

        mesh.colors = colors;

        mesh.uv = uvs;

        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        mesh.RecalculateBounds();

        return mesh;

    }



    void OnDestroy() {

        if (_mesh != null) Destroy(_mesh);

        if (_material != null) Destroy(_material);

    }

}