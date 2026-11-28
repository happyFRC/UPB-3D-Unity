using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局加载遮罩：显示“正在加载”文字。
/// 走 DontDestroyOnLoad，切场景时不会被销毁，加载完成后调用 Hide()。
/// </summary>
public static class LoadingScreen {
    private static GameObject _root;
    private static Runner _runner;
    private static TMP_Text _label;

    public static void Show(string message) {
        if (_root == null) Build();

        _root.SetActive(true);
        if (_label != null) _label.text = message;
    }

    /// <summary>旧接口保留（没有进度条了）。</summary>
    public static void SetProgress(float value) { }

    /// <summary>
    /// 显示遮罩并异步切换场景。协程挂在常驻对象上——MainMenu 自己的协程
    /// 会在场景激活时被销毁，导致遮罩永远关不掉。
    /// </summary>
    public static void LoadScene(string sceneName) {
        Show("正在加载…");
        if (_runner == null) {
            GameObject go = new("LoadingScreenRunner");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }
        _runner.StartCoroutine(_runner.Load(sceneName));
    }

    public class Runner : MonoBehaviour {
        public System.Collections.IEnumerator Load(string sceneName) {
            // 先让遮罩画出来，再做耗时的场景加载。
            yield return null;
            yield return null;

            AsyncOperation operation = UnityEngine.SceneManagement.SceneManager
                .LoadSceneAsync(sceneName);
            if (operation == null) {
                Hide();
                yield break;
            }

            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f) {
                yield return null;
            }

            // 用 sceneLoaded 事件收尾：它在新场景真正加载完时触发，
            // 比轮询 AsyncOperation.isDone 更可靠（也不会被帧率限制卡住）。
            bool loaded = false;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                    UnityEngine.SceneManagement.LoadSceneMode mode) {
                loaded = true;
            }

            yield return new WaitForSecondsRealtime(0.1f);
            operation.allowSceneActivation = true;

            float deadline = Time.realtimeSinceStartup + 30f;
            while (!loaded && Time.realtimeSinceStartup < deadline) yield return null;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

            Hide();
        }
    }

    public static void Hide() {
        if (_root != null) Object.Destroy(_root);
        _root = null;
        _label = null;
    }

    private static void Build() {
        _root = new GameObject("LoadingScreen",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Object.DontDestroyOnLoad(_root);

        Canvas canvas = _root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = _root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image background = CreateImage(rootRect, "Background",
            new Color(0.03f, 0.04f, 0.06f, 0.98f));
        Stretch(background.rectTransform);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/KosefontP-JP SDF 1")
            ?? Resources.Load<TMP_FontAsset>("Fonts/FRCFONT SDF");

        _label = CreateText(rootRect, "Label", "正在加载…", font, 44f,
            new Color(0.92f, 0.94f, 1f, 1f));
        _label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _label.rectTransform.anchoredPosition = Vector2.zero;
        _label.rectTransform.sizeDelta = new Vector2(800f, 80f);
    }

    private static Image CreateImage(RectTransform parent, string name, Color color) {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(RectTransform parent, string name, string text,
            TMP_FontAsset font, float size, Color color) {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        if (font != null) label.fontSharedMaterial = font.material;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        return label;
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
