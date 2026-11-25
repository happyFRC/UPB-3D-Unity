using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Settings : MonoBehaviour {
    [Header("音量")]
    public Slider musicVolumeSlider;
    public TextMeshProUGUI musicValueText;

    [Header("音乐切换")]
    public Button buttonChangeBGM;
    public GameObject musicListPanel;
    public Button buttonSubstitute;
    public Button buttonFreeze;
    public Button buttonInfinity;
    public TextMeshProUGUI textSubstitute;
    public TextMeshProUGUI textFreeze;
    public TextMeshProUGUI textInfinity;

    [Header("帮助")]
    public Button buttonHelp;
    public GameObject helpPanel;
    public Image helpImage;

    [Header("我的数据记录")]
    public Button buttonOpenRecords;

    [Header("删除全部记录")]
    public Button buttonDeleteRecords;

    [Header("我的数据")]
    public Button buttonMyData;

    [Header("制作人员")]
    public Button buttonCredits;
    public GameObject creditsPanel;
    public RawImage creditsRawImage;
    public VideoPlayer creditsVideoPlayer;

    [Header("创建快捷方式")]
    public Button buttonCreateShortcut;

    [Header("返回")]
    public Button buttonRet;

    private AudioClip _clipSubstitute;
    private AudioClip _clipFreeze;
    private AudioClip _clipInfinity;

    private enum MusicChoice { Substitute, Freeze, Infinity }
    private MusicChoice _current = MusicChoice.Substitute;

    private static readonly Color SELECTED_COLOR = new Color(0f, 1f, 0f, 1f);
    private static readonly Color NORMAL_COLOR = new Color(0f, 0f, 0f, 1f);

    private const float PlaytimeTickInterval = 1f;
    private float _playtimeTimer = 0f;
    private double _playtimeSeconds = 0.0;

    void Start() {
        _clipSubstitute = Resources.Load<AudioClip>("music/SubstituteForSunrise");
        _clipFreeze = Resources.Load<AudioClip>("music/Freeze");
        _clipInfinity = Resources.Load<AudioClip>("music/infinity");

        BindVolumeControls();

        float vol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (musicVolumeSlider != null) {
            musicVolumeSlider.value = vol;
            musicVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        } else {
            Debug.LogWarning("[Settings] musicVolumeSlider 未绑定，音量滑条将不可用");
        }
        UpdateVolumeText(vol);

        SetPanelVisible(false);

        if (buttonChangeBGM != null) buttonChangeBGM.onClick.AddListener(ToggleMusicPanel);
        if (buttonSubstitute != null) buttonSubstitute.onClick.AddListener(() => SelectMusic(MusicChoice.Substitute));
        if (buttonFreeze != null) buttonFreeze.onClick.AddListener(() => SelectMusic(MusicChoice.Freeze));
        if (buttonInfinity != null) buttonInfinity.onClick.AddListener(() => SelectMusic(MusicChoice.Infinity));
        if (buttonRet != null) buttonRet.onClick.AddListener(OnRetClick);

        if (buttonHelp != null) buttonHelp.onClick.AddListener(OnHelpClick);
        if (buttonOpenRecords != null) buttonOpenRecords.onClick.AddListener(OnOpenRecordsClick);
        if (buttonDeleteRecords != null) buttonDeleteRecords.onClick.AddListener(OnDeleteRecordsClick);
        if (buttonMyData != null) buttonMyData.onClick.AddListener(OnMyDataClick);
        if (buttonCredits != null) buttonCredits.onClick.AddListener(OnCreditsClick);
        if (buttonCreateShortcut != null) {
            // 清掉场景里从其它按钮复制过来的持久 OnClick（旧 target 为空），只保留这里的处理。
            buttonCreateShortcut.onClick = new Button.ButtonClickedEvent();
            buttonCreateShortcut.onClick.AddListener(OnCreateShortcutClick);
        }

        if (helpPanel != null) helpPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        if (helpImage != null) {
            Sprite helpSprite = Resources.Load<Sprite>("Help");
            if (helpSprite != null) {
                helpImage.sprite = helpSprite;
            } else {
                Debug.LogWarning("找不到 Resources/Help.png，请检查 Sprite 导入");
            }
        }

        SetupCreditsVideo();

        int savedChoice = PlayerPrefs.GetInt("MusicChoice", 0);
        _current = (MusicChoice)savedChoice;
        ApplyMusicChoice();

        _playtimeSeconds = LoadPlaytime();
    }

    void Update() {
        if (musicListPanel != null && musicListPanel.activeSelf &&
            Input.GetMouseButtonDown(0)) {
            if (!IsPointerOver(musicListPanel) &&
                (buttonChangeBGM == null ||
                 !IsPointerOver(buttonChangeBGM.gameObject))) {
                SetPanelVisible(false);
            }
        }

        if (helpPanel != null && helpPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
            helpPanel.SetActive(false);
        }

        if (creditsPanel != null && creditsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
            CloseCredits();
        }

        _playtimeTimer += Time.unscaledDeltaTime;
        if (_playtimeTimer >= PlaytimeTickInterval) {
            _playtimeSeconds += _playtimeTimer;
            _playtimeTimer = 0f;
            SavePlaytime(_playtimeSeconds);
        }
    }

    void OnApplicationQuit() {
        SavePlaytime(_playtimeSeconds);
    }

    // ~~~ 音量 ~~~ //
    void OnVolumeChanged(float value) {
        if (BGMPlayer.Instance != null) {
            BGMPlayer.Instance.SetVolume(value);
        } else {
            AudioListener.volume = value;
        }

        UpdateVolumeText(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    void UpdateVolumeText(float value) {
        if (musicValueText != null) {
            musicValueText.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }

    /// <summary>
    /// 绑定重建后的音量滑条及其百分比文本。
    /// 场景里有可能改名为 MusicVolumeSlider / Label_VolumePercentage，
    /// 所以这里按名字优先查找；滑条组件缺失时自动补上并接好 Fill/Handle。
    /// </summary>
    private void BindVolumeControls() {
        GameObject sliderRoot = GameObject.Find("MusicVolumeSlider");
        Slider namedSlider = FindSliderByName("MusicVolumeSlider");
        if (namedSlider == null && sliderRoot != null) {
            namedSlider = sliderRoot.GetComponent<Slider>();
            if (namedSlider == null) {
                namedSlider = sliderRoot.GetComponentInChildren<Slider>(true);
            }
            // 重建的滑条根节点没有 Slider 组件时，以它为准补一个，
            // 不要退回旧场景里那个已经废弃的 Slider。
            if (namedSlider == null) {
                namedSlider = sliderRoot.AddComponent<Slider>();
            }
        }

        if (namedSlider != null) {
            musicVolumeSlider = namedSlider;
        } else if (musicVolumeSlider == null) {
            musicVolumeSlider = FindObjectOfType<Slider>(true);
        }

        if (musicVolumeSlider != null) {
            ConfigureVolumeSlider(musicVolumeSlider);
        }

        TextMeshProUGUI namedLabel = FindLabelByName("Label_VolumePercentage");
        if (namedLabel != null) {
            musicValueText = namedLabel;
        } else if (musicValueText == null) {
            musicValueText = FindLabelByName("MusicValueText");
        }
    }

    private static Slider FindSliderByName(string objectName) {
        foreach (Slider slider in FindObjectsOfType<Slider>(true)) {
            if (slider.name == objectName) return slider;
        }
        return null;
    }

    private static TextMeshProUGUI FindLabelByName(string objectName) {
        foreach (TextMeshProUGUI text in FindObjectsOfType<TextMeshProUGUI>(true)) {
            if (text.name == objectName) return text;
        }
        return null;
    }

    private static void ConfigureVolumeSlider(Slider slider) {
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        // Slider 组件可能挂在 MusicVolumeSlider 根节点，也可能挂在其子节点；
        // Fill/Handle 统一从重建后的根节点找。
        Transform root = slider.transform;
        GameObject namedRoot = GameObject.Find("MusicVolumeSlider");
        if (namedRoot != null) root = namedRoot.transform;

        Transform fill = root.Find("Fill Area/Fill");
        if (fill == null) fill = slider.transform.Find("Fill Area/Fill");
        Transform handle = root.Find("Handle Slide Area/Handle");
        if (handle == null) handle = slider.transform.Find("Handle Slide Area/Handle");

        if (fill != null) slider.fillRect = fill as RectTransform;
        if (handle != null) {
            slider.handleRect = handle as RectTransform;
            Image handleImage = handle.GetComponent<Image>();
            if (handleImage != null) slider.targetGraphic = handleImage;
        }
    }

    // ~~~ 音乐切换 ~~~ //
    void ToggleMusicPanel() {
        if (musicListPanel != null) SetPanelVisible(!musicListPanel.activeSelf);
    }

    void SetPanelVisible(bool visible) {
        if (musicListPanel != null) musicListPanel.SetActive(visible);
    }

    void SelectMusic(MusicChoice choice) {
        _current = choice;
        PlayerPrefs.SetInt("MusicChoice", (int)choice);
        ApplyMusicChoice();
        SetPanelVisible(false);
    }

    void ApplyMusicChoice() {
        SetTextColor(textSubstitute, _current == MusicChoice.Substitute);
        SetTextColor(textFreeze, _current == MusicChoice.Freeze);
        SetTextColor(textInfinity, _current == MusicChoice.Infinity);

        AudioClip clip = _current switch {
            MusicChoice.Substitute => _clipSubstitute,
            MusicChoice.Freeze => _clipFreeze,
            MusicChoice.Infinity => _clipInfinity,
            _ => null
        };

        if (clip != null && BGMPlayer.Instance != null) {
            BGMPlayer.Instance.PlayClip(clip);
        }
    }

    void SetTextColor(TextMeshProUGUI text, bool selected) {
        if (text == null) return;
        text.color = selected ? SELECTED_COLOR : NORMAL_COLOR;
    }

    // ~~~ 帮助 ~~~ //
    void OnHelpClick() {
        if (helpPanel != null) {
            helpPanel.SetActive(true);
        }
    }

    // ~~~ 我的数据记录 ~~~ //
    void OnOpenRecordsClick() {
        string root = GetSimulationRoot();
        string path = Path.Combine(root, "simulations");
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }
        OpenFolder(path);
    }

    // ~~~ 删除全部记录 ~~~ //
    void OnDeleteRecordsClick() {
        string root = GetSimulationRoot();
        string mainSeq = Path.Combine(root, "simulations", "sim_text");
        string wd = Path.Combine(root, "simulations", "sim_text_wd");

        int deleted = 0;
        deleted += DeleteFilesIn(mainSeq);
        deleted += DeleteFilesIn(wd);

        FloatingText.Show($"已删除 {deleted} 个记录文件");
    }

    int DeleteFilesIn(string folder) {
        if (!Directory.Exists(folder)) return 0;
        int count = 0;
        foreach (string file in Directory.GetFiles(folder)) {
            try {
                File.Delete(file);
                count++;
            }
            catch (Exception e) {
                Debug.LogWarning($"删除失败 {file}: {e.Message}");
            }
        }
        return count;
    }

    // ~~~ 我的数据 ~~~ //
    void OnMyDataClick() {
        double seconds = LoadPlaytime();
        double minutes = seconds / 60.0;
        double hours = minutes / 60.0;

        string msg;
        if (hours >= 1) {
            msg = $"游戏时长：{hours:F2} 小时";
        } else {
            msg = $"游戏时长：{minutes:F2} 分钟";
        }

        FloatingText.Show(msg, 3f);
    }

    // ~~~ 制作人员 ~~~ //
    void SetupCreditsVideo() {
        if (creditsVideoPlayer == null) return;

        VideoClip clip = Resources.Load<VideoClip>("videos/credits");
        if (clip == null) clip = Resources.Load<VideoClip>("videos/cresits"); // 视频文件名拼写沿用原资源
        if (clip == null) {
            Debug.LogWarning("找不到 Resources/videos/credits.mp4");
            return;
        }

        creditsVideoPlayer.clip = clip;
        creditsVideoPlayer.playOnAwake = false;
        creditsVideoPlayer.isLooping = false;
        creditsVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        creditsVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        creditsVideoPlayer.skipOnDrop = true;

        if (creditsRawImage != null && creditsRawImage.texture is RenderTexture rt) {
            creditsVideoPlayer.targetTexture = rt;
        }

        creditsVideoPlayer.loopPointReached += _ => CloseCredits();
    }

    void OnCreditsClick() {
        if (creditsPanel == null) return;
        creditsPanel.SetActive(true);
        if (creditsVideoPlayer != null) {
            creditsVideoPlayer.Stop();
            creditsVideoPlayer.Play();
        }
    }

    void CloseCredits() {
        if (creditsVideoPlayer != null) {
            creditsVideoPlayer.Stop();
        }
        if (creditsPanel != null) {
            creditsPanel.SetActive(false);
        }
    }

    // ~~~ 创建快捷方式 ~~~ //
    void OnCreateShortcutClick() {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string lnkPath = Path.Combine(desktop, "行星球宇宙Test.lnk");

            if (File.Exists(lnkPath)) {
                FloatingText.Show("快捷方式已存在", 3f);
                return;
            }

            CreateWindowsShortcut(lnkPath, exePath);
            FloatingText.Show("快捷方式创建成功", 3f);
        } catch (Exception e) {
            FloatingText.Show($"创建失败：{e.Message}", 3f);
        }
#elif UNITY_ANDROID
        FloatingText.Show("Android 端不支持创建桌面快捷方式", 3f);
#else
        FloatingText.Show("该功能仅在 Windows 版本可用", 3f);
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    void CreateWindowsShortcut(string lnkPath, string targetPath) {
        // Mono 下不能直接创建 COM 对象（Activator.CreateInstance 会报
        // "Unmanaged activation is not supported"），所以改用 PowerShell 的 WScript.Shell。
        string escape(string s) { return s.Replace("'", "''"); }
        string dir = Path.GetDirectoryName(targetPath) ?? "";
        string script =
            "$ws = New-Object -ComObject WScript.Shell;" +
            "$sc = $ws.CreateShortcut('" + escape(lnkPath) + "');" +
            "$sc.TargetPath = '" + escape(targetPath) + "';" +
            "$sc.WorkingDirectory = '" + escape(dir) + "';" +
            "$sc.Save()";

        var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe") {
            Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"" +
                        script + "\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var proc = System.Diagnostics.Process.Start(psi)) {
            if (proc == null) throw new Exception("无法启动 powershell");
            if (!proc.WaitForExit(15000)) {
                try { proc.Kill(); } catch { }
                throw new Exception("创建快捷方式超时");
            }
            if (proc.ExitCode != 0) {
                throw new Exception("powershell 返回错误码 " + proc.ExitCode);
            }
        }

        if (!File.Exists(lnkPath)) throw new Exception("快捷方式未创建");
    }
#endif

    // ~~~ 游戏时长读取 ~~~ //
    string GetPlaytimePath() {
        string root = GetSimulationRoot();
        string dir = Path.Combine(root, "UPB-data");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return Path.Combine(dir, "playtime.json");
    }

    double LoadPlaytime() {
        try {
            string path = GetPlaytimePath();
            if (!File.Exists(path)) return 0.0;
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<PlaytimeData>(json);
            return data != null ? data.total_seconds : 0.0;
        }
        catch {
            return 0.0;
        }
    }

    void SavePlaytime(double seconds) {
        try {
            string path = GetPlaytimePath();
            var data = new PlaytimeData { total_seconds = seconds };
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception e) {
            Debug.LogWarning($"读取游戏时长失败: {e.Message}");
        }
    }

    [Serializable]
    private class PlaytimeData {
        public double total_seconds;
    }

    // ~~~ 路径与文件夹 ~~~ //
    string GetSimulationRoot() {
#if UNITY_EDITOR
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
#elif UNITY_ANDROID
        return Application.persistentDataPath;
#else
        return Path.GetDirectoryName(Application.dataPath);
#endif
    }

    void OpenFolder(string path) {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        path = path.Replace("/", "\\");
        System.Diagnostics.Process.Start("explorer.exe", path);
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        System.Diagnostics.Process.Start("open", path);
#elif UNITY_ANDROID
        FloatingText.Show("Android 端不支持打开文件夹，路径已打印", 3f);
#else
        System.Diagnostics.Process.Start(path);
#endif
    }

    // ~~~ 返回 ~~~ //
    void OnRetClick() {
        SavePlaytime(_playtimeSeconds);
        SceneManager.LoadScene("MainMenu");
    }

    bool IsPointerOver(GameObject go) {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            rt, Input.mousePosition, null);
    }
}
