using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 直接挂在已有的 CreditsPanel 上；不依赖 Settings 的执行顺序。
/// Button_Credits 的 OnClick 在场景里直接指向 PlayCredits。
///
/// 注意：场景里这块面板默认是隐藏的，挂在它身上的 VideoPlayer 在“激活的同一帧”
/// 直接 Play 会卡在准备阶段（isPrepared 永远为 false，帧号一直 -1）。
/// 所以这里改用一个运行时创建的独立播放器，画面照样输出到面板的 RenderTexture。
/// </summary>
public class CreditsPanelController : MonoBehaviour {
    private VideoPlayer _player;
    private RawImage _rawImage;
    private RenderTexture _runtimeTexture;

    /// <summary>给自检/外部查询用：视频是否正在播放。</summary>
    public bool IsPlaying => _player != null && _player.isPlaying;

    private void Awake() {
        _rawImage = GetComponent<RawImage>();
        // 场景里的 VideoPlayer 不要用，避免它在面板激活时先进入播放状态，
        // 把整个对象上的视频状态弄坏。
        VideoPlayer scenePlayer = GetComponent<VideoPlayer>();
        if (scenePlayer != null) {
            scenePlayer.playOnAwake = false;
            scenePlayer.Stop();
            scenePlayer.enabled = false;
        }
    }

    public void PlayCredits() {
        if (_rawImage == null) _rawImage = GetComponent<RawImage>();
        PrepareAspectRatio();

        VideoClip clip = Resources.Load<VideoClip>("videos/credits");
        if (clip == null) clip = Resources.Load<VideoClip>("videos/cresits"); // 兼容旧文件名
        if (clip == null || _rawImage == null) {
            Debug.LogWarning("[Credits] 找不到视频 Resources/videos/credits");
            return;
        }

        int width = Mathf.Max(2, (int)clip.width);
        int height = Mathf.Max(2, (int)clip.height);
        if (_runtimeTexture == null || _runtimeTexture.width != width ||
            _runtimeTexture.height != height) {
            if (_runtimeTexture != null) {
                _runtimeTexture.Release();
                Destroy(_runtimeTexture);
            }
            _runtimeTexture = new RenderTexture(width, height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _runtimeTexture.name = "CreditsRT_Runtime";
            _runtimeTexture.filterMode = FilterMode.Bilinear;
            _runtimeTexture.wrapMode = TextureWrapMode.Clamp;
            _runtimeTexture.Create();
        }

        _rawImage.texture = _runtimeTexture;
        _rawImage.enabled = true;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        StartRuntimePlayer(clip);
    }

    private void StartRuntimePlayer(VideoClip clip) {
        if (_player != null) {
            _player.Stop();
            Destroy(_player.gameObject);
            _player = null;
        }

        GameObject go = new("CreditsVideoPlayer");
        go.hideFlags = HideFlags.HideAndDontSave;
        _player = go.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.skipOnDrop = true;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.audioOutputMode = VideoAudioOutputMode.Direct;
        _player.targetTexture = _runtimeTexture;
        _player.clip = clip;
        _player.loopPointReached += _ => CloseCredits();
        _player.Play();
    }

    private void PrepareAspectRatio() {
        Canvas.ForceUpdateCanvases();
        RectTransform rect = GetComponent<RectTransform>();
        RectTransform parent = rect != null ? rect.parent as RectTransform : null;
        if (rect == null || parent == null) return;

        // 视频本身是 16:9，面板也强制成 16:9，避免被方形/竖条 RectTransform 拉伸。
        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        if (parentWidth <= 1f || parentHeight <= 1f) return;

        float width = parentHeight * 16f / 9f;
        float height = parentHeight;
        if (width > parentWidth) {
            width = parentWidth;
            height = parentWidth * 9f / 16f;
        }

        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    private void Update() {
        if (Input.GetKey(KeyCode.Escape)) CloseCredits();
    }

    public void CloseCredits() {
        if (_player != null) {
            _player.Stop();
            Destroy(_player.gameObject);
            _player = null;
        }
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        if (_runtimeTexture != null) _runtimeTexture.Release();
        if (_player != null && _player.gameObject != null) Destroy(_player.gameObject);
    }
}
