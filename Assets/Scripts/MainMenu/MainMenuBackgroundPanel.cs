using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class MainMenuBackgroundPanel : MonoBehaviour {
    [Header("颜色")]
    [Tooltip("整体颜色（RGB），alpha 会按渐变覆盖")]
    public Color panelColor = new Color(0, 0, 0, 1);

    [Header("渐变参数")]
    [Tooltip("不透明段的 alpha（0~1），比如 150/255 ≈ 0.588")]
    [Range(0f, 1f)]
    public float baseAlpha = 150f / 255f;

    [Tooltip("渐变起点占宽度的比例，比如 350/450 ≈ 0.778")]
    [Range(0f, 1f)]
    public float fadeStartRatio = 350f / 450f;

    [Header("分辨率")]
    [Range(2, 1024)]
    public int textureWidth = 256;

    private RawImage _rawImage;
    private Texture2D _texture;

    void Start() {
        _rawImage = GetComponent<RawImage>();
        GenerateTexture();
    }

    void GenerateTexture() {
        if (_texture != null) {
            Destroy(_texture);
        }

        _texture = new Texture2D(textureWidth, 1, TextureFormat.RGBA32, false);
        _texture.wrapMode = TextureWrapMode.Clamp;
        _texture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < textureWidth; x++) {
            float t = (float)x / (textureWidth - 1);

            float alpha;
            if (t < fadeStartRatio) {
                // 前段：保持不变
                alpha = baseAlpha;
            } else {
                // 后段：从 baseAlpha 线性降到 0
                float u = (t - fadeStartRatio) / (1f - fadeStartRatio);
                alpha = baseAlpha * (1f - u);
            }

            Color c = panelColor;
            c.a = alpha;
            _texture.SetPixel(x, 0, c);
        }
        _texture.Apply();

        _rawImage.texture = _texture;
    }

    void OnValidate() {
        if (Application.isPlaying && _rawImage != null) {
            GenerateTexture();
        }
    }

    void OnDestroy() {
        if (_texture != null) {
            Destroy(_texture);
        }
    }
}