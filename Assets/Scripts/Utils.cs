using UnityEngine;
using TMPro;
using UnityEngine.UI;

public static class FloatingText {
    private static TextMeshProUGUI _textMesh;

    public static void Show(string message, float time = 2f) {
        if (_textMesh != null) {
            Object.Destroy(_textMesh.gameObject);
            _textMesh = null;
        }

        GameObject go = new GameObject("FloatingText");
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        go.transform.SetParent(canvas.transform);
        go.transform.localPosition = Vector3.zero;

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(20, 20);
        rect.sizeDelta = new Vector2(300, 100);

        _textMesh = go.AddComponent<TextMeshProUGUI>();
        _textMesh.font = Resources.Load<TMP_FontAsset>("Fonts/FRCFONT SDF");
        _textMesh.autoSizeTextContainer = true;
        _textMesh.fontSize = 64;
        _textMesh.color = Color.yellow;
        _textMesh.alignment = TextAlignmentOptions.BottomLeft;

        _textMesh.text = message;
        Object.Destroy(_textMesh.gameObject, time);
    }
}