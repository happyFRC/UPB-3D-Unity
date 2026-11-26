using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用单位选择面板。五个单位一页，按换算率从小到大排序。
/// 打开时直接定位到当前单位所在页，而不是总从第一页开始。
/// </summary>
public class UnitPickerPanel : MonoBehaviour {
    private const int PageSize = 5;

    private RectTransform _panelRect;
    private RectTransform _content;
    private readonly List<Button> _unitButtons = new();
    private readonly List<TMP_Text> _unitLabels = new();
    private TMP_Text _pageLabel;
    private Button _prevButton;
    private Button _nextButton;
    private UnitButton _source;
    private string _category;
    private string[] _units;
    private int _page;
    private int _pageCount;
    private bool _usingExisting;

    /// <summary>场景里每个参数面板下各有一个 UnitPanel，启动时全部接管并隐藏。</summary>
    public static void InitializeExisting() {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>()) {
            if (t == null || (t.name != "UnitPanel" && t.name != "Unit_Panel" &&
                              t.name != "UnitChoosePanel")) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            UnitPickerPanel picker = t.GetComponent<UnitPickerPanel>();
            if (picker == null) picker = t.gameObject.AddComponent<UnitPickerPanel>();
            picker.BuildExisting(t.gameObject);
        }
    }

    public static bool TryOpen(UnitButton source) {
        if (source == null) return false;
        SandboxUnits.EnsureLoaded();

        Transform ownerPanel = FindOwnerPanel(source.transform);
        Transform unitPanel = ownerPanel != null
            ? FindDeep(ownerPanel, "UnitPanel") ?? FindDeep(ownerPanel, "Unit_Panel")
            : null;

        // 创建面板里的速度单位用自带的 UnitChoosePanel（在 Button_Shoot 下面）。
        if (unitPanel == null) unitPanel = FindNearestUnitPanel(source.transform);

        if (unitPanel == null) {
            Canvas canvas = FindCanvas();
            if (canvas == null) return false;
            unitPanel = FindDeep(canvas.transform, "UnitPanel") ??
                        FindDeep(canvas.transform, "Unit_Panel");
        }

        UnitPickerPanel picker;
        if (unitPanel != null) {
            picker = unitPanel.GetComponent<UnitPickerPanel>();
            if (picker == null) picker = unitPanel.gameObject.AddComponent<UnitPickerPanel>();
            picker.BuildExisting(unitPanel.gameObject);
        } else {
            Canvas canvas = FindCanvas();
            if (canvas == null) return false;
            GameObject go = new("Unit_Panel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            picker = go.AddComponent<UnitPickerPanel>();
            picker.BuildUi(go);
        }

        picker.Open(source);
        SandboxSceneController.Instance?.TrackPanel(picker.gameObject,
            () => picker.gameObject.SetActive(false));
        return true;
    }

    /// <summary>向上逐层找随参数面板走的单位面板（例如 Button_Shoot 下的 UnitChoosePanel）。</summary>
    private static Transform FindNearestUnitPanel(Transform source) {
        for (Transform t = source; t != null; t = t.parent) {
            Transform found = FindDeep(t, "UnitChoosePanel");
            if (found != null) return found;
            found = FindDeep(t, "UnitPanel") ?? FindDeep(t, "Unit_Panel");
            if (found != null) return found;
            if (t.name == "Canvas") break;
        }
        return null;
    }

    private static Transform FindOwnerPanel(Transform source) {        Transform t = source;
        while (t != null) {
            if (t.name == "StarParameterPanel" || t.name == "PlanetParameterPanel") return t;
            t = t.parent;
        }
        return null;
    }

    private void BuildExisting(GameObject panel) {
        if (_usingExisting) return;
        _usingExisting = true;
        _panelRect = panel.GetComponent<RectTransform>();

        _unitButtons.Clear();
        _unitLabels.Clear();
        for (int i = 1; i <= PageSize; i++) {
            Transform slot = FindDeep(panel.transform, $"Unit_Pos{i}");
            if (slot == null) continue;
            Button button = slot.GetComponent<Button>();
            TMP_Text label = slot.GetComponentInChildren<TMP_Text>(true);
            if (button == null || label == null) continue;
            int slotIndex = i - 1;
            button.onClick.AddListener(() => SelectUnit(slotIndex));
            _unitButtons.Add(button);
            _unitLabels.Add(label);
        }

        // 用户面板的命名：LastPage 是上一页，PageNumber 是下一页按钮，
        // NextPage 实际承载页码文本。
        Transform prev = FindDeep(panel.transform, "Button_LastPage");
        Transform next = FindDeep(panel.transform, "Button_PageNumber");
        Transform page = FindDeep(panel.transform, "Button_NextPage");
        if (prev != null) {
            _prevButton = prev.GetComponent<Button>();
            if (_prevButton != null) _prevButton.onClick.AddListener(() => ChangePage(-1));
        }
        if (next != null) {
            _nextButton = next.GetComponent<Button>();
            if (_nextButton != null) _nextButton.onClick.AddListener(() => ChangePage(1));
        }
        if (page != null) _pageLabel = page.GetComponentInChildren<TMP_Text>(true);

        panel.SetActive(false);
    }

    private static Canvas FindCanvas() {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in canvases) {
            if (c != null && c.gameObject.scene.IsValid()) return c;
        }
        return Object.FindObjectOfType<Canvas>();
    }

    private static Transform FindDeep(Transform root, string name) {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root) {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void BuildUi(GameObject panel) {
        _panelRect = panel.GetComponent<RectTransform>();
        if (_panelRect == null) _panelRect = panel.AddComponent<RectTransform>();

        Image bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.11f, 0.98f);
        // 贴着参数面板的右下角弹出。
        _panelRect.anchorMin = new Vector2(1f, 0f);
        _panelRect.anchorMax = new Vector2(1f, 0f);
        _panelRect.pivot = new Vector2(1f, 0f);
        _panelRect.sizeDelta = new Vector2(300f, 390f);
        _panelRect.anchoredPosition = new Vector2(-370f, 24f);

        _content = panel.GetComponent<RectTransform>();
        CreateText("Title", "单位", new Vector2(0f, 168f), 24f, TextAlignmentOptions.Center);
        CreateCloseButton();

        for (int i = 0; i < PageSize; i++) {
            int index = i;
            GameObject buttonGo = new($"UnitOption_{i}", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panel.transform, false);
            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(230f, 42f);
            rt.anchoredPosition = new Vector2(0f, 118f - i * 50f);

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.18f, 0.20f, 0.25f, 1f);
            Button button = buttonGo.GetComponent<Button>();
            button.onClick.AddListener(() => SelectUnit(index));

            TMP_Text label = CreateText("Text", "", Vector2.zero, 20f,
                TextAlignmentOptions.Center, buttonGo.transform);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            _unitButtons.Add(button);
            _unitLabels.Add(label);
        }

        _pageLabel = CreateText("Page", "1/1", new Vector2(0f, -150f), 18f,
            TextAlignmentOptions.Center);

        _prevButton = CreateSmallButton("<", new Vector2(-80f, -190f), () => ChangePage(-1));
        _nextButton = CreateSmallButton(">", new Vector2(80f, -190f), () => ChangePage(1));

        panel.SetActive(false);
    }

    private void Open(UnitButton source) {
        _source = source;
        _category = source.GetCategory();

        string[] catalogUnits = SandboxUnits.Symbols(_category);
        if (catalogUnits.Length > 0) {
            _units = catalogUnits;
        } else {
            _units = source.units ?? new string[0];
        }

        if (_units.Length == 0) {
            Debug.LogWarning($"[Units] 分类 {_category} 没有可用单位");
            return;
        }

        _pageCount = Mathf.Max(1, Mathf.CeilToInt(_units.Length / (float)PageSize));
        string current = SandboxUnits.Normalize(source.GetUnit());
        int currentIndex = 0;
        for (int i = 0; i < _units.Length; i++) {
            if (string.Equals(_units[i], current, System.StringComparison.OrdinalIgnoreCase)) {
                currentIndex = i;
                break;
            }
        }

        // 关键：从当前单位所在页开始，而不是第一页。
        _page = currentIndex / PageSize;
        _page = Mathf.Clamp(_page, 0, _pageCount - 1);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RefreshPage();
    }

    private void RefreshPage() {
        if (_units == null || _units.Length == 0) return;

        int start = _page * PageSize;
        for (int i = 0; i < PageSize; i++) {
            int unitIndex = start + i;
            bool valid = unitIndex < _units.Length;
            _unitButtons[i].gameObject.SetActive(valid);
            if (!valid) continue;
            _unitLabels[i].text = _units[unitIndex];
        }

        if (_pageLabel != null) _pageLabel.text = $"{_page + 1}/{_pageCount}";
        if (_prevButton != null) _prevButton.interactable = _page > 0;
        if (_nextButton != null) _nextButton.interactable = _page < _pageCount - 1;
    }

    private void ChangePage(int delta) {
        _page = Mathf.Clamp(_page + delta, 0, _pageCount - 1);
        RefreshPage();
    }

    private void SelectUnit(int slot) {
        int index = _page * PageSize + slot;
        if (_units == null || index < 0 || index >= _units.Length) return;
        if (_source != null) _source.ApplyUnit(_units[index]);
        gameObject.SetActive(false);
    }

    private void Update() {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
            gameObject.SetActive(false);
        }
    }

    private TMP_Text CreateText(string name, string text, Vector2 position, float size,
            TextAlignmentOptions alignment, Transform parent = null) {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent != null ? parent : transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(260f, 40f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = new Color(0.92f, 0.92f, 0.86f, 1f);
        label.raycastTarget = false;

        TMP_Text fontSource = FindFontSource();
        if (fontSource != null) label.font = fontSource.font;
        return label;
    }

    private Button CreateSmallButton(string text, Vector2 position, UnityEngine.Events.UnityAction action) {
        GameObject go = new("Button_" + text, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(48f, 36f);
        go.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.25f, 1f);

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        TMP_Text label = CreateText("Text", text, Vector2.zero, 20f,
            TextAlignmentOptions.Center, go.transform);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private void CreateCloseButton() {
        Button close = CreateSmallButton("×", new Vector2(128f, 168f),
            () => gameObject.SetActive(false));
        close.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 32f);
    }

    private static TMP_Text FindFontSource() {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text t in texts) {
            if (t != null && t.font != null) return t;
        }
        return null;
    }
}
