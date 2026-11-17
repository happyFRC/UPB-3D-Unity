using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 沙盘自适应 UI：
///  · Canvas 用 Expand 缩放，保证 1920×1080 设计稿在任何比例下都不会超出屏幕；
///  · 时间面板固定贴底、创建面板固定贴左、参数面板固定贴右。
/// </summary>
public class SandboxAdaptiveUI : MonoBehaviour {
    private const float RefWidth = 1920f;
    private const float RefHeight = 1080f;
    private const float Edge = 16f;

    private CanvasScaler _scaler;
    private RectTransform _canvasRect;
    private RectTransform _timePanel;
    private RectTransform _createPanel;
    private RectTransform _starPanel;
    private RectTransform _planetPanel;
    private int _lastWidth;
    private int _lastHeight;

    public static SandboxAdaptiveUI Install(Transform canvas) {
        if (canvas == null) return null;
        SandboxAdaptiveUI existing = canvas.GetComponent<SandboxAdaptiveUI>();
        if (existing != null) return existing;
        SandboxAdaptiveUI adaptive = canvas.gameObject.AddComponent<SandboxAdaptiveUI>();
        adaptive.Bind(canvas);
        return adaptive;
    }

    private void Bind(Transform canvas) {
        _canvasRect = canvas as RectTransform;
        _scaler = canvas.GetComponent<CanvasScaler>();
        _timePanel = FindRect(canvas, "TimePanel");
        _createPanel = FindRect(canvas, "CelestialBodyCreatePanel");
        _starPanel = FindRect(canvas, "StarParameterPanel");
        _planetPanel = FindRect(canvas, "PlanetParameterPanel");
        ApplyScale();
        Dock();
    }

    private void Start() {
        ApplyScale();
        Dock();
    }

    private void Update() {
        if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
        ApplyScale();
        Dock();
    }

    private void LateUpdate() {
        // 面板被打开/切页时可能重置位置，这里兜一下。
        Dock();
    }

    private void ApplyScale() {
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
        if (_scaler == null) return;
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
        // Expand：宽高两边都按“至少覆盖设计分辨率”放大，任何比例都不会切掉内容。
        _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    private void Dock() {
        DockBottomCenter(_timePanel);
        DockLeftMiddle(_createPanel);
        DockRightMiddle(_starPanel);
        DockRightMiddle(_planetPanel);
    }

    private static void DockBottomCenter(RectTransform rt) {
        // 时间面板要求下边缘完全贴底，不留间隙。
        SetDock(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero);
    }

    private static void DockLeftMiddle(RectTransform rt) {
        SetDock(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(Edge, 0f));
    }

    private static void DockRightMiddle(RectTransform rt) {
        SetDock(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-Edge, 0f));
    }

    private static void SetDock(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 offset) {
        if (rt == null) return;
        bool changed = rt.anchorMin != anchor || rt.anchorMax != anchor || rt.pivot != pivot;
        Vector2 size = rt.rect.size;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        if (changed) rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    private static RectTransform FindRect(Transform root, string name) {
        if (root == null) return null;
        Transform found = FindDeep(root, name);
        return found as RectTransform;
    }

    private static Transform FindDeep(Transform root, string name) {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++) {
            Transform hit = FindDeep(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }
}
