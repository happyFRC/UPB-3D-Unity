using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 沙盘自适应 UI：
///  · Canvas 用 Expand 缩放，保证 1920×1080 设计稿在任何比例下都不会超出屏幕；
///  · 侧边面板宽度按画布宽度百分比取（窄比例下不会把内容挤掉），并始终贴左/右/底边。
/// </summary>
public class SandboxAdaptiveUI : MonoBehaviour {
    private const float RefWidth = 1920f;
    private const float RefHeight = 1080f;
    private const float Edge = 16f;

    /// <summary>侧边面板宽度占画布宽度的比例。</summary>
    private const float SideWidthRatio = 0.32f;
    private const float SideWidthMin = 560f;
    private const float SideWidthMax = 900f;

    /// <summary>场景介绍栏宽度比例。</summary>
    private const float InfoWidthRatio = 0.28f;
    private const float InfoWidthMin = 460f;
    private const float InfoWidthMax = 760f;

    private CanvasScaler _scaler;
    private RectTransform _canvasRect;
    private RectTransform _timePanel;
    private RectTransform _createPanel;
    private RectTransform _starPanel;
    private RectTransform _planetPanel;
    private RectTransform _infoPanel;
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
        _infoPanel = FindRect(canvas, "Description_Panel");
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
        // 兜底：如果画布比设计稿窄（说明缩放模式被改回 Match 或丢失），重新应用 Expand。
        if (_canvasRect != null && _canvasRect.rect.width < RefWidth - 1f) ApplyScale();
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
        if (_canvasRect == null) return;
        float canvasWidth = _canvasRect.rect.width;
        if (canvasWidth <= 1f) return;

        float sideWidth = Mathf.Clamp(canvasWidth * SideWidthRatio, SideWidthMin, SideWidthMax);
        float infoWidth = Mathf.Clamp(canvasWidth * InfoWidthRatio, InfoWidthMin, InfoWidthMax);

        DockSide(_createPanel, true, sideWidth, 0f);
        DockSide(_starPanel, false, sideWidth, 0f);
        DockSide(_planetPanel, false, sideWidth, 0f);
        DockSide(_infoPanel, false, infoWidth, -300f);
        DockBottom(_timePanel, canvasWidth);
    }

    /// <summary>贴左/右，横向宽度固定，纵向拉伸铺满。</summary>
    private static void DockSide(RectTransform rt, bool left, float width, float heightInset) {
        if (rt == null) return;
        float x = left ? 0f : 1f;
        rt.anchorMin = new Vector2(x, 0f);
        rt.anchorMax = new Vector2(x, 1f);
        rt.pivot = new Vector2(x, 0.5f);
        rt.sizeDelta = new Vector2(width, heightInset);
        rt.anchoredPosition = new Vector2(left ? Edge : -Edge, 0f);
    }

    /// <summary>贴底居中，宽度不超过画布宽度。</summary>
    private static void DockBottom(RectTransform rt, float canvasWidth) {
        if (rt == null) return;
        float height = rt.rect.height > 1f ? rt.rect.height : 100f;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(Mathf.Min(RefWidth, canvasWidth), height);
        rt.anchoredPosition = Vector2.zero;
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
