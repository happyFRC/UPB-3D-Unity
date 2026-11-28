using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetaryOrbitRenderer : MonoBehaviour {
    [Header("Toggle")]
    [Tooltip("勾选后显示行星轨道，取消勾选隐藏")]
    public Toggle toggleShowOrbits;

    [Header("换算")]
    [Tooltip("1 个 Unity 单位对应多少 km")]
    public double kmPerUnit = CelestialScale.KmPerUnit;

    [Header("线段")]
    [Tooltip("椭圆的分段数，越大越圆滑")]
    public int segments = 256;

    [Tooltip("屏幕空间线宽（像素）")]
    public float pixelWidth = 2f;

    [Header("Shader")]
    [Tooltip("留空则用 Custom/ScreenSpaceLine")]
    public Shader lineShader;

    [Header("层级")]
    [Tooltip("轨道物体挂在哪个 Transform 下，留空则挂到自己身上")]
    public Transform orbitParent;

    private EvolutionSystemData _system;
    private GameObject[] _orbitObjects;
    private bool _started;

    void Start() {
        kmPerUnit = CelestialScale.KmPerUnit;
        _started = true;
        if (_system == null) _system = EvolutionSystemDefaults.SolarSystem();

        if (lineShader == null) {
            lineShader = Shader.Find("Custom/ScreenSpaceLine");
        }

        BuildOrbits();

        if (toggleShowOrbits != null) {
            toggleShowOrbits.isOn = false;
            toggleShowOrbits.onValueChanged.AddListener(SetVisible);
            SetVisible(false);
        }
    }

    /// <summary>切换到指定恒星系；如果渲染器还没 Start，会等 Start 时再建。</summary>
    public void SetSystem(EvolutionSystemData system) {
        _system = system ?? EvolutionSystemDefaults.SolarSystem();
        if (!_started) return;
        Rebuild();
    }

    public void UseSolarSystem() {
        SetSystem(EvolutionSystemDefaults.SolarSystem());
    }

    public void Rebuild() {
        ClearOrbits();
        if (lineShader == null) lineShader = Shader.Find("Custom/ScreenSpaceLine");
        BuildOrbits();
        ApplyVisibility();
    }

    private void ApplyVisibility() {
        SetVisible(toggleShowOrbits != null && toggleShowOrbits.isOn);
    }

    void BuildOrbits() {
        Transform parent = orbitParent != null ? orbitParent : transform;
        List<EvolutionPlanetData> planets = _system != null && _system.planets != null
            ? _system.planets
            : new List<EvolutionPlanetData>();

        List<GameObject> objects = new();
        foreach (EvolutionPlanetData planet in planets) {
            if (planet == null || planet.EffectiveSemiMajorAxisKm <= 0.0) continue;

            GameObject go = new($"Orbit_{planet.name}");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            Material mat = new Material(lineShader);
            mat.SetColor("_Color", planet.Color);
            mat.SetFloat("_PixelWidth", pixelWidth);
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Mesh mesh = BuildOrbitMesh(planet);
            mf.mesh = mesh;

            objects.Add(go);
        }

        _orbitObjects = objects.ToArray();
    }

    Mesh BuildOrbitMesh(EvolutionPlanetData planet) {
        Mesh mesh = new Mesh();
        mesh.name = $"OrbitMesh_{planet.name}";

        double aUnit = planet.EffectiveSemiMajorAxisKm / kmPerUnit;
        double cUnit = aUnit * planet.eccentricity;
        double bUnit = aUnit * System.Math.Sqrt(1 - planet.eccentricity * planet.eccentricity);

        int n = segments;

        Vector3[] vertices = new Vector3[n * 2];
        Vector3[] normals = new Vector3[n * 2];
        Color[] colors = new Color[n * 2];

        Vector3[] centers = new Vector3[n];
        for (int s = 0; s < n; s++) {
            double t = 2.0 * System.Math.PI * s / n;
            double x = aUnit * System.Math.Cos(t) - cUnit;
            double z = bUnit * System.Math.Sin(t);
            centers[s] = new Vector3((float)x, 0f, (float)z);
        }

        for (int s = 0; s < n; s++) {
            Vector3 prev = centers[(s - 1 + n) % n];
            Vector3 next = centers[(s + 1) % n];
            Vector3 tangent = (next - prev).normalized;
            Vector3 normal = new Vector3(-tangent.z, 0f, tangent.x);

            int i0 = s * 2;
            int i1 = s * 2 + 1;

            vertices[i0] = centers[s];
            normals[i0] = -normal;
            colors[i0] = planet.Color;

            vertices[i1] = centers[s];
            normals[i1] = normal;
            colors[i1] = planet.Color;
        }

        int[] indices = new int[n * 6];
        for (int s = 0; s < n; s++) {
            int s0 = s;
            int s1 = (s + 1) % n;

            int i0 = s0 * 2;
            int i1 = s0 * 2 + 1;
            int i2 = s1 * 2;
            int i3 = s1 * 2 + 1;

            int idx = s * 6;
            indices[idx + 0] = i0;
            indices[idx + 1] = i1;
            indices[idx + 2] = i2;
            indices[idx + 3] = i2;
            indices[idx + 4] = i1;
            indices[idx + 5] = i3;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    public void SetVisible(bool visible) {
        if (_orbitObjects == null) return;
        foreach (GameObject go in _orbitObjects) {
            if (go != null) go.SetActive(visible);
        }
    }

    private void ClearOrbits() {
        if (_orbitObjects == null) return;
        foreach (GameObject go in _orbitObjects) {
            if (go == null) continue;
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.mesh != null) Destroy(mf.mesh);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null) Destroy(mr.material);
            Destroy(go);
        }
        _orbitObjects = null;
    }

    void OnDestroy() {
        ClearOrbits();
    }
}
