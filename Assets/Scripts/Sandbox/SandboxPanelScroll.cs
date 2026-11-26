using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 参数面板的鼠标滚轮竖向滚动。
/// 不重建 UI 层级：直接移动整个 MechanicsPanel，由父面板的 RectMask2D 负责裁剪。
/// </summary>
public class SandboxPanelScroll : MonoBehaviour {
    private RectTransform _panel;
    private RectTransform _maskRoot;
    private Vector2 _basePosition;
    private float _scroll;   // 0 = 顶部，max = 底部
    private float _maxScroll;

    public void Initialize(RectTransform panel, RectTransform maskRoot = null) {
        _panel = panel != null ? panel : GetComponent<RectTransform>();
        RectTransform parent = maskRoot != null ? maskRoot : _panel != null ? _panel.parent as RectTransform : null;
        if (_panel == null) return;

        // 关键：不要把 RectMask2D 直接加在 StarParameterPanel 上，
        // 否则同在参数面板下的 UnitPanel 也会被裁剪掉。
        // 只给 Mechanics 建一个独立 viewport。
        if (parent != null) {
            GameObject viewportGo = new("MechanicsViewport", typeof(RectTransform),
                typeof(RectMask2D));
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            viewport.SetParent(parent, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            _panel.SetParent(viewport, true);
            _maskRoot = viewport;
        }

        _basePosition = _panel.anchoredPosition;
    }

    private void Update() {
        if (_panel == null || !_panel.gameObject.activeInHierarchy) return;

        Vector2 mouse = Input.mousePosition;
        RectTransform hitArea = _maskRoot != null ? _maskRoot : _panel;
        if (!RectTransformUtility.RectangleContainsScreenPoint(hitArea, mouse, null)) return;

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) < 0.001f) return;

        _maxScroll = Mathf.Max(0f, MeasureContentHeight() - _panel.rect.height);
        // 滚轮向下 -> scroll 增大 -> UI 按钮整体向上移动，露出下方。
        // 滚轮向上 -> scroll 减小 -> UI 按钮整体向下移动，回到顶部。
        _scroll = Mathf.Clamp(_scroll - wheel * 120f, 0f, _maxScroll);
        _panel.anchoredPosition = _basePosition + new Vector2(0f, _scroll);
    }

    private float MeasureContentHeight() {
        float min = float.MaxValue;
        float max = float.MinValue;
        foreach (Transform child in _panel) {
            if (child is not RectTransform rect) continue;
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(_panel, rect);
            min = Mathf.Min(min, b.min.y);
            max = Mathf.Max(max, b.max.y);
        }
        if (min > max) return _panel.rect.height;
        return Mathf.Max(_panel.rect.height, max - min);
    }
}
