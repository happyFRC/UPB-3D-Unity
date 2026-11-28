using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个天体的 10 秒实时轨迹。轨迹点保存为逻辑坐标，因此浮动原点切换时不会错位。
/// </summary>
public class SandboxTrail : MonoBehaviour {
    public static bool Visible = true;
    public static bool OrbitVisible = false;

    private const float TrailSeconds = 10f;
    private const float SampleInterval = 0.05f;
    private const double MetersPerUnit = CelestialScale.KmPerUnit * 1000.0;

    // 屏幕空间线宽：所有天体轨迹使用同样的像素宽度。
    private const float ScreenPixelWidth = 4.0f;
    private const int OrbitSegments = 160;

    private static Transform _root;

    private SandboxPhysicsBody _body;
    private LineRenderer _line;
    private LineRenderer _orbitLine;
    private Gradient _gradient;
    private GradientColorKey[] _colorKeys;
    private Gradient _orbitGradient;
    private GradientColorKey[] _orbitColorKeys;
    private readonly GradientAlphaKey[] _orbitAlphaKeys = new GradientAlphaKey[2];
    private float _lastOrbitAlpha = float.NaN;
    private readonly List<TrailPoint> _points = new();
    private float _lastSampleTime = -1f;

    // 每帧重建 Gradient 会持续产生小对象；透明度没明显变化时不必重设。
    private readonly GradientAlphaKey[] _alphaKeys = new GradientAlphaKey[2];
    private float _lastGradientAlpha = float.NaN;

    private struct TrailPoint {
        public float time;
        public SandboxDVec3 logical;
    }

    public static SandboxTrail Attach(SandboxPhysicsBody body) {
        if (body == null) return null;

        if (_root == null) {
            _root = new GameObject("SandboxTrails").transform;
        }

        GameObject go = new("Trail_" + body.name);
        go.transform.SetParent(_root, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        // 轨迹和完整轨道都使用同一个 3D 深度测试 Shader：
        // 线宽按屏幕像素换算，但位置/遮挡仍属于真实世界空间。
        Shader shader = Shader.Find("UPB/OrbitLine");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new(shader) { name = "TrailMaterial", renderQueue = 3000 };
        line.material = material;
        line.sortingOrder = 0;

        Color color;
        if (body.kind == SandboxPhysicsBodyKind.Planet) {
            color = body.trailColor;
            color.a = 1f;
        } else {
            var (r, g, b) = Spectrum.GetBlackbodyColor(body.temperatureK);
            color = new Color(r, g, b, 1f);
        }

        line.widthMultiplier = 0.04f;
        line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        Gradient gradient = new();
        GradientColorKey[] colorKeys = {
            new(color, 0f),
            new(color, 1f)
        };
        gradient.SetKeys(
            colorKeys,
            new[] { new GradientAlphaKey(0.08f, 0f), new GradientAlphaKey(1f, 1f) });
        line.colorGradient = gradient;
        line.enabled = Visible;

        SandboxTrail trail = go.AddComponent<SandboxTrail>();
        trail._body = body;
        trail._line = line;
        trail._gradient = gradient;
        trail._colorKeys = colorKeys;
        trail.AddPoint(Time.unscaledTime);
        return trail;
    }

    public static void Remove(SandboxPhysicsBody body) {
        if (body == null || body.trail == null) return;
        // 轨道线之前挂在 SandboxTrails 根节点下，是轨迹物体的兄弟节点；
        // 只销毁轨迹会让轨道线变成孤儿，永远留在场景里。
        body.trail.DestroyOrbitLine();
        Destroy(body.trail.gameObject);
        body.trail = null;
    }

    private void DestroyOrbitLine() {
        if (_orbitLine == null) return;
        Destroy(_orbitLine.gameObject);
        _orbitLine = null;
    }

    public static void ResetAll() {
        if (_root != null) Destroy(_root.gameObject);
        _root = null;
        Visible = true;
        OrbitVisible = false;
    }

    public static void SetVisible(bool visible) {
        Visible = visible;
    }

    public static void ToggleVisible() {
        Visible = !Visible;
    }

    public static void SetOrbitVisible(bool visible) {
        OrbitVisible = visible;
    }

    public static void ToggleOrbitVisible() {
        OrbitVisible = !OrbitVisible;
    }

    public static void Clear(SandboxPhysicsBody body) {
        if (body == null || body.trail == null) return;
        body.trail.ClearTrail();
    }

    private void ClearTrail() {
        _points.Clear();
        if (_body != null && _body.alive) AddPoint(Time.unscaledTime);
    }

    private void AddPoint(float time) {
        _points.Add(new TrailPoint {
            time = time,
            logical = CurrentLogicalPosition()
        });
        _lastSampleTime = time;

        float cutoff = time - TrailSeconds;
        int firstKept = 0;
        while (firstKept < _points.Count && _points[firstKept].time < cutoff) {
            firstKept++;
        }
        if (firstKept > 0) _points.RemoveRange(0, firstKept);
    }

    private void LateUpdate() {
        if (_body == null || !_body.alive || _line == null) {
            if (_line != null) _line.enabled = false;
            return;
        }

        float now = Time.unscaledTime;
        if (_points.Count == 0 || now - _lastSampleTime >= SampleInterval) {
            AddPoint(now);
        }

        UpdateScreenWidth();
        float orbitFade = ComputeOrbitFade();
        UpdateOrbitLine(orbitFade);

        float alpha = Visible ? orbitFade : 0f;
        _line.enabled = alpha > 0.001f;
        if (!_line.enabled) return;

        if (!Mathf.Approximately(alpha, _lastGradientAlpha)) {
            _lastGradientAlpha = alpha;
            _alphaKeys[0] = new GradientAlphaKey(0.08f * alpha, 0f);
            _alphaKeys[1] = new GradientAlphaKey(alpha, 1f);
            _gradient.SetKeys(_colorKeys, _alphaKeys);
            _line.colorGradient = _gradient;
        }

        // 历史点按固定间隔采样以控制数量，但最新端点必须逐帧贴合天体。
        // 否则高时间倍率下端点到下个采样点才更新，会在画面里周期性跳动。
        SandboxDVec3 head = CurrentLogicalPosition();
        bool appendHead = ShouldAppendHead(head);
        int positionCount = _points.Count + (appendHead ? 1 : 0);
        _line.positionCount = positionCount;
        for (int i = 0; i < _points.Count; i++) {
            SandboxDVec3 p = _points[i].logical;
            _line.SetPosition(i, SandboxFloatingOrigin.ToRender(p.x, p.y, p.z));
        }
        if (appendHead) {
            _line.SetPosition(_points.Count,
                SandboxFloatingOrigin.ToRender(head.x, head.y, head.z));
        }
    }

    private void UpdateOrbitLine(float orbitFade) {
        if (_body == null || _body.planet == null || !_body.isOrbiting ||
            _body.orbitTarget == null || !OrbitVisible) {
            if (_orbitLine != null) _orbitLine.enabled = false;
            return;
        }

        // 缩放到很远时轨道直径只有亚像素，宽度却按屏幕像素换算，会变成一团；
        // 和轨迹使用同一套连续淡出，避免距离在阈值附近时整条线反复闪隐。
        if (orbitFade <= 0.001f) {
            if (_orbitLine != null) _orbitLine.enabled = false;
            return;
        }

        SandboxDVec3 rel = _body.positionM - _body.orbitTarget.positionM;
        SandboxDVec3 vel = _body.velocityMps - _body.orbitTarget.velocityMps;
        double r = rel.Magnitude;
        if (r <= 1e-9) return;

        double mu = SandboxPhysicsSystem.GravitationalConstant *
                    (_body.orbitTarget.massKg + _body.massKg);
        double energy = 0.5 * vel.SqrMagnitude - mu / r;
        if (energy >= -1e-30) {
            if (_orbitLine != null) _orbitLine.enabled = false;
            return;
        }

        double a = -mu / (2.0 * energy);
        SandboxDVec3 h = Cross(rel, vel);
        double hLen = h.Magnitude;
        if (hLen <= 1e-12) {
            if (_orbitLine != null) _orbitLine.enabled = false;
            return;
        }
        SandboxDVec3 hHat = h / hLen;

        SandboxDVec3 eVec = Cross(vel, h) / mu - rel / r;
        double eccentricity = eVec.Magnitude;
        SandboxDVec3 eHat = eccentricity > 1e-9 ? eVec / eccentricity : rel / r;
        SandboxDVec3 qHat = Cross(hHat, eHat);
        double qLen = qHat.Magnitude;
        if (qLen <= 1e-12) return;
        qHat = qHat / qLen;

        if (_orbitLine == null) CreateOrbitLine();
        if (_orbitLine == null) return;

        float orbitAlpha = 0.65f * orbitFade;
        if (!Mathf.Approximately(orbitAlpha, _lastOrbitAlpha)) {
            _lastOrbitAlpha = orbitAlpha;
            _orbitAlphaKeys[0] = new GradientAlphaKey(orbitAlpha, 0f);
            _orbitAlphaKeys[1] = new GradientAlphaKey(orbitAlpha, 1f);
            _orbitGradient.SetKeys(_orbitColorKeys, _orbitAlphaKeys);
            _orbitLine.colorGradient = _orbitGradient;
        }

        // 轨道是真实 3D 物体（位置、深度、遮挡都按世界空间），
        // 但线宽按屏幕像素换算，让它看起来始终是同样粗细。
        Camera camera = Camera.main;
        Transform visual = BodyTransform();
        if (camera != null && visual != null) {
            float distance = Vector3.Distance(camera.transform.position, visual.position);
            float halfFov = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float worldPerPixel = 2f * distance * Mathf.Tan(halfFov) /
                                  Mathf.Max(Screen.height, 1);
            _orbitLine.widthMultiplier = Mathf.Max(worldPerPixel * 2.0f, 1e-7f);
        }

        _orbitLine.positionCount = OrbitSegments;
        for (int i = 0; i < OrbitSegments; i++) {
            double theta = i / (double)OrbitSegments * Math.PI * 2.0;
            double radius = a * (1.0 - eccentricity * eccentricity) /
                            Math.Max(1.0 + eccentricity * Math.Cos(theta), 1e-9);
            SandboxDVec3 point = eHat * (radius * Math.Cos(theta)) +
                                 qHat * (radius * Math.Sin(theta));
            SandboxDVec3 world = _body.orbitTarget.positionM + point;
            _orbitLine.SetPosition(i, SandboxFloatingOrigin.ToRender(
                world.x / MetersPerUnit, world.y / MetersPerUnit, world.z / MetersPerUnit));
        }
        _orbitLine.enabled = true;
    }

    private void CreateOrbitLine() {
        GameObject go = new("Orbit_" + (_body != null ? _body.name : ""));
        // 挂在轨迹物体下面：删除天体时随轨迹一起销毁，不会再留下“僵尸轨道”。
        go.transform.SetParent(transform, false);
        _orbitLine = go.AddComponent<LineRenderer>();
        _orbitLine.useWorldSpace = true;
        _orbitLine.loop = true;
        _orbitLine.alignment = LineAlignment.View;
        _orbitLine.textureMode = LineTextureMode.Stretch;
        _orbitLine.numCapVertices = 2;
        _orbitLine.numCornerVertices = 2;
        _orbitLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _orbitLine.receiveShadows = false;
        _orbitLine.sortingOrder = 0;

        Shader shader = Shader.Find("UPB/OrbitLine");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        Material material = new(shader) { name = "OrbitMaterial", renderQueue = 3000 };
        _orbitLine.material = material;

        Color color = _body != null ? _body.trailColor : Color.white;
        color.a = 1f;
        _orbitColorKeys = new[] {
            new GradientColorKey(color, 0f),
            new GradientColorKey(color, 1f)
        };
        _orbitGradient = new Gradient();
        _orbitGradient.SetKeys(
            _orbitColorKeys,
            new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0.65f, 1f) });
        _orbitLine.colorGradient = _orbitGradient;
        _lastOrbitAlpha = 0.65f;
    }

    private SandboxDVec3 CurrentLogicalPosition() => new(
        _body.positionM.x / MetersPerUnit,
        _body.positionM.y / MetersPerUnit,
        _body.positionM.z / MetersPerUnit);

    /// <summary>
    /// 当前帧的天体位置是否已经明显偏离最后一个历史采样点。
    /// 接近重合时不再追加退化线段，避免线宽计算和透明叠画出噪点。
    /// </summary>
    private bool ShouldAppendHead(SandboxDVec3 head) {
        if (_points.Count == 0) return true;
        SandboxDVec3 last = _points[_points.Count - 1].logical;
        Vector3 a = SandboxFloatingOrigin.ToRender(last.x, last.y, last.z);
        Vector3 b = SandboxFloatingOrigin.ToRender(head.x, head.y, head.z);
        return (b - a).sqrMagnitude > 1e-12f;
    }

    private static SandboxDVec3 Cross(SandboxDVec3 a, SandboxDVec3 b) =>
        new(a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);

    /// <summary>把线宽换算成固定屏幕像素宽度，而不是固定世界宽度。</summary>
    private void UpdateScreenWidth() {
        Camera camera = Camera.main;
        Transform visual = BodyTransform();
        if (camera == null || visual == null || Screen.height <= 0) return;

        float distance = Vector3.Distance(camera.transform.position, visual.position);
        float halfFov = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float worldPerPixel = 2f * distance * Mathf.Tan(halfFov) / Screen.height;
        _line.widthMultiplier = Mathf.Max(worldPerPixel * ScreenPixelWidth, 1e-7f);
    }

    private Transform BodyTransform() {
        if (_body == null) return null;
        if (_body.star != null) return _body.star.transform;
        if (_body.planet != null) return _body.planet.transform;
        if (_body.fragment != null) return _body.fragment.transform;
        return null;
    }

    /// <summary>
    /// 环绕天体：摄像机离它超过 10 倍轨道直径后开始渐隐，到 20 倍完全消失。
    /// 非环绕天体不做这个距离淡出。
    /// </summary>
    private float ComputeOrbitFade() {
        if (_body == null || !_body.isOrbiting || _body.orbitRadiusM <= 0.0) return 1f;

        Camera camera = Camera.main;
        if (camera == null) return 1f;

        Transform visual = null;
        if (_body.star != null) visual = _body.star.transform;
        else if (_body.planet != null) visual = _body.planet.transform;
        else if (_body.fragment != null) visual = _body.fragment.transform;
        if (visual == null) return 1f;

        double diameterUnits = 2.0 * _body.orbitRadiusM / MetersPerUnit *
                               SandboxWorldScale.Scale;
        float fadeStart = (float)(10.0 * diameterUnits);
        float fadeEnd = Mathf.Max(fadeStart * 2f, fadeStart + 1e-4f);
        float distance = Vector3.Distance(camera.transform.position, visual.position);

        if (distance <= fadeStart) return 1f;
        if (distance >= fadeEnd) return 0f;
        return 1f - (distance - fadeStart) / (fadeEnd - fadeStart);
    }

    /// <summary>
    /// 销毁时把运行时新建的两份线材质一起释放，
    /// 否则反复删/建天体时材质会一直堆积。
    /// </summary>
    private void OnDestroy() {
        if (_line != null && _line.material != null) Destroy(_line.material);
        if (_orbitLine != null && _orbitLine.material != null) Destroy(_orbitLine.material);
    }
}
