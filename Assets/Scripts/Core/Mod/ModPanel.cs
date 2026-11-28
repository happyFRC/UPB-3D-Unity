using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 模组管理面板（运行时构建，不改场景）。
/// 主菜单的“模组管理”按钮调用 <see cref="Open"/>，列表里每个模组一行：
/// 图标 / 名称+作者版本+状态 / 描述 / 开关按钮。
/// 开关状态由 <see cref="ModManager"/> 存进 PlayerPrefs，下次启动生效。
/// </summary>
public class ModPanel : MonoBehaviour {
    private static ModPanel _instance;

    private static readonly Color PanelColor = new(0.043f, 0.058f, 0.082f, 0.98f);
    private static readonly Color RowColor = new(0.10f, 0.13f, 0.18f, 1f);
    private static readonly Color TextMain = new(0.94f, 0.96f, 1f, 1f);
    private static readonly Color TextDim = new(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color OnColor = new(0.196f, 0.608f, 0.196f, 1f);
    private static readonly Color OffColor = new(0.28f, 0.30f, 0.34f, 1f);
    private static readonly Color DangerColor = new(1f, 0.30f, 0.30f, 1f);

    private TMP_FontAsset _font;
    private RectTransform _content;
    private ModManager _manager;

    public static void Open() {
        if (_instance != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) {
            Debug.LogError("[Mods] 找不到 Canvas，无法打开模组管理");
            return;
        }

        GameObject root = new("ModPanel", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.transform.SetAsLastSibling();

        _instance = root.AddComponent<ModPanel>();
        _instance.Build(rootRect);
    }

    private void Build(RectTransform root) {
        _font = Resources.Load<TMP_FontAsset>("Fonts/SIYUANHEITI SDF")
                ?? Resources.Load<TMP_FontAsset>("Fonts/KosefontP-JP SDF 1")
                ?? Resources.Load<TMP_FontAsset>("Fonts/FRCFONT SDF");
        _manager = ModManager.Instance;

        // 半透明遮罩，挡住后面的菜单
        Image dim = NewImage("Dim", root, new Color(0f, 0f, 0f, 0.65f));
        Stretch(dim.rectTransform);

        RectTransform panel = NewRect("Panel", root);
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(1100f, 720f));
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = PanelColor;

        TextMeshProUGUI title = NewText("Title", panel, "模组管理", 46f, TextMain,
            TextAlignmentOptions.Center);
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(900f, 56f));

        TextMeshProUGUI hint = NewText("Hint", panel,
            "模组默认关闭；打开开关后重启游戏生效", 21f, TextDim, TextAlignmentOptions.Center);
        Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -74f), new Vector2(900f, 28f));

        BuildList(panel);

        Button open = NewButton("ButtonOpenFolder", panel, "打开模组文件夹", new Color(0f, 0.588f, 1f, 0.85f), 24f);
        Anchor(open.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-190f, 26f), new Vector2(320f, 62f));
        open.onClick.AddListener(() => {
            if (_manager != null) _manager.RevealPlayerModsFolder();
        });

        Button close = NewButton("ButtonClose", panel, "关闭", OffColor, 26f);
        Anchor(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(190f, 26f), new Vector2(220f, 62f));
        close.onClick.AddListener(Close);

        if (_manager != null) {
            _manager.Changed += RefreshList;
        }
    }

    private void BuildList(RectTransform panel) {
        RectTransform scrollRect = NewRect("Scroll", panel);
        Anchor(scrollRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -8f), new Vector2(1020f, 470f));

        ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        RectTransform viewport = NewRect("Viewport", scrollRect);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        _content = NewRect("Content", viewport);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = _content;

        RefreshList();
    }

    private void RefreshList() {
        if (_content == null) return;

        for (int i = _content.childCount - 1; i >= 0; i--) {
            GameObject child = _content.GetChild(i).gameObject;
            child.transform.SetParent(null, false);
            Destroy(child);
        }

        if (_manager == null) {
            AddPlainText(_content, "模组系统未初始化（场景里没有 ModManager）", 24f, DangerColor, 80f);
            return;
        }

        if (!_manager.Loaded) {
            AddPlainText(_content, "正在加载模组…", 24f, TextDim, 80f);
            return;
        }

        if (_manager.Mods.Count == 0) {
            AddPlainText(_content,
                "没有找到模组。\n把模组文件夹放到下面这个目录里即可：\n" +
                _manager.PlayerModsPath, 20f, TextDim, 140f);
            return;
        }

        foreach (ModEntry entry in _manager.Mods) {
            BuildRow(entry);
        }
    }

    private void AddPlainText(RectTransform parent, string text, float size, Color color, float height) {
        TextMeshProUGUI label = NewText("Info", parent, text, size, color, TextAlignmentOptions.Center);
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
    }

    private void BuildRow(ModEntry entry) {
        RectTransform row = NewRect("Mod_" + entry.id, _content);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.color = RowColor;

        // 图标
        RectTransform iconRect = NewRect("Icon", row);
        Anchor(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f),
            new Vector2(72f, 72f));
        Image icon = iconRect.gameObject.AddComponent<Image>();
        if (entry.icon != null) {
            icon.sprite = Sprite.Create(entry.icon,
                new Rect(0f, 0f, entry.icon.width, entry.icon.height), new Vector2(0.5f, 0.5f));
            icon.color = Color.white;
        } else {
            icon.color = new Color(0.18f, 0.22f, 0.30f, 1f);
            TextMeshProUGUI letter = NewText("Letter", iconRect,
                string.IsNullOrEmpty(entry.displayName) ? "?" : entry.displayName.Substring(0, 1),
                34f, TextDim, TextAlignmentOptions.Center);
            Stretch(letter.rectTransform);
        }

        // 名称
        TextMeshProUGUI name = NewText("Name", row, entry.displayName, 30f, TextMain,
            TextAlignmentOptions.Left);
        Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -10f),
            new Vector2(680f, 36f));

        // 内置 / 外部 标签
        Image tag = NewImage("Tag", row, entry.isBuiltin
            ? new Color(0.16f, 0.38f, 0.68f, 1f)
            : new Color(0.22f, 0.25f, 0.30f, 1f));
        Anchor(tag.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-186f, 0f), new Vector2(64f, 26f));
        TextMeshProUGUI tagText = NewText("TagText", tag.rectTransform,
            entry.isBuiltin ? "内置" : "外部", 17f, TextMain, TextAlignmentOptions.Center);
        Stretch(tagText.rectTransform);

        // 作者 / 版本 / 状态
        string meta = BuildMeta(entry);
        Color metaColor = entry.state switch {
            ModLoadState.Failed => DangerColor,
            ModLoadState.Skipped => new Color(1f, 0.72f, 0.3f, 1f),
            _ => TextDim
        };
        TextMeshProUGUI metaText = NewText("Meta", row, meta, 20f, metaColor, TextAlignmentOptions.Left);
        metaText.enableWordWrapping = false;
        Anchor(metaText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -46f),
            new Vector2(720f, 26f));

        // 描述
        string description = string.IsNullOrWhiteSpace(entry.description) ? "（没有描述）" : entry.description;
        TextMeshProUGUI desc = NewText("Desc", row, description, 19f, TextDim, TextAlignmentOptions.Left);
        desc.enableWordWrapping = false;
        Anchor(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -72f),
            new Vector2(720f, 24f));

        // 开关
        Button toggle = NewButton("Toggle", row, entry.enabled ? "已开启" : "已关闭",
            entry.enabled ? OnColor : OffColor, 24f);
        Anchor(toggle.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-18f, 0f), new Vector2(150f, 54f));
        ModEntry captured = entry;
        toggle.onClick.AddListener(() => {
            if (_manager == null) return;
            _manager.SetEnabled(captured, !captured.enabled);
            RefreshList();
        });
    }

    private static string BuildMeta(ModEntry entry) {
        string author = string.IsNullOrWhiteSpace(entry.author) ? "未知作者" : entry.author.Trim();
        string version = string.IsNullOrWhiteSpace(entry.version) ? "v1.0" : entry.version.Trim();
        string meta = $"{author}   ·   {version}   ·   {entry.StateText}";
        if (entry.state == ModLoadState.Failed && !string.IsNullOrEmpty(entry.error)) {
            meta += "：" + entry.error;
        }
        return meta;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void OnDestroy() {
        if (_manager != null) _manager.Changed -= RefreshList;
        if (_instance == this) _instance = null;
    }

    private void Close() {
        if (_instance == this) _instance = null;
        Destroy(gameObject);
    }

    // ~~~ 构建小工具 ~~~

    private static RectTransform NewRect(string name, Transform parent) {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Color color) {
        RectTransform rect = NewRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, string text, float size,
            Color color, TextAlignmentOptions align) {
        RectTransform rect = NewRect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) label.font = _font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = align;
        label.raycastTarget = false;
        return label;
    }

    private Button NewButton(string name, Transform parent, string text, Color color, float fontSize) {
        RectTransform rect = NewRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.targetGraphic = image;

        TextMeshProUGUI label = NewText("Text", rect, text, fontSize, TextMain, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size) {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        // 文本是先赋值后定尺寸的，TMP 在尺寸为 0 时会缓存空文本；定了尺寸后强制重排一次。
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text != null) text.ForceMeshUpdate();
    }
}
