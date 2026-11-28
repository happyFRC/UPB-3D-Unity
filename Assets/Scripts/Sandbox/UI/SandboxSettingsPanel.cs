using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 沙盘设置面板：通用设置 / 引力模块 / 热学模块。
///
/// 开关按钮直接在文字上显示当前状态（“网格：开” / “网格：关”），
/// 开启时边框绿色，关闭时黑色。
/// “关闭全局热学模拟”没有开关状态，点一下就把四个热学通道全部关掉并刷新显示。
///
/// 面板里的按钮是场景里手搭的、名字/数量可能变化，所以这里不依赖一次性接线：
/// 面板打开时会持续扫描，按“按钮名”或“按钮文字前缀”把新按钮自动接上，
/// 并每 0.15 秒把文字/颜色刷新一遍，避免出现“点了不更新”的情况。
/// </summary>
public class SandboxSettingsPanel : MonoBehaviour {
    private static readonly Color SelectedModule = new(0.75f, 0.95f, 1f, 1f);
    private static readonly Color NormalModule = new(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Color StateOn = new(0.2f, 0.9f, 0.35f, 1f);
    private static readonly Color StateOff = new(0.06f, 0.06f, 0.06f, 1f);

    private class Binding {
        public string ButtonName;
        public string Label;
        public string OnWord;
        public string OffWord;
        public System.Func<bool> Get;
        public System.Action<bool> Set;
    }

    private readonly List<Binding> _bindings = new();
    private readonly Dictionary<Button, Binding> _wired = new();
    private readonly HashSet<Button> _shutWired = new();

    private SandboxSceneController _controller;
    private GameObject _panel;
    private Button _openButton;

    private GameObject _generalPanel;
    private GameObject _gravityPanel;
    private GameObject _thermalPanel;
    private Button _generalButton;
    private Button _gravityButton;
    private Button _thermalButton;

    private Slider _volumeSlider;
    private TMP_Text _volumeText;
    private InfiniteGrid _grid;
    private int _module;
    private float _nextScan;

    public bool IsOpen => _panel != null && _panel.activeSelf;

    public void Initialize(SandboxSceneController controller) {
        _controller = controller;

        _panel = Find("SettingsPanelSandbox")?.gameObject;
        if (_panel == null) {
            Debug.LogWarning("[Settings] 场景里找不到 SettingsPanelSandbox");
            return;
        }
        _panel.SetActive(false);

        _openButton = Find("Button_OpenSettings")?.GetComponent<Button>();
        if (_openButton != null) _openButton.onClick.AddListener(Open);

        _generalPanel = Find("GeneralModelPanel")?.gameObject;
        _gravityPanel = Find("GravitationPanel")?.gameObject;
        _thermalPanel = Find("ThermalModelPanel")?.gameObject;

        _generalButton = Find("Button_GeneralModel")?.GetComponent<Button>();
        _gravityButton = Find("Button_GravitationModel")?.GetComponent<Button>();
        _thermalButton = Find("Button_ThermalModel")?.GetComponent<Button>();
        if (_generalButton != null) _generalButton.onClick.AddListener(() => ShowModule(0));
        if (_gravityButton != null) _gravityButton.onClick.AddListener(() => ShowModule(1));
        if (_thermalButton != null) _thermalButton.onClick.AddListener(() => ShowModule(2));

        // “返回”按钮：关掉设置面板，回到模拟。
        foreach (Transform t in FindAll("Button_Return")) {
            Button back = t.GetComponent<Button>();
            if (back == null) continue;
            back.onClick.RemoveListener(Close);
            back.onClick.AddListener(Close);
        }

        _grid = FindObjectOfType<InfiniteGrid>();

        AddBinding("Button_StarRendering", "恒星曝光度", "高", "低",
            () => _controller != null && _controller.StarRenderMode == 0,
            on => _controller?.SetStarRenderMode(on ? 0 : 1));

        AddBinding("Button_Grids", "网格", "开", "关",
            () => _grid != null && _grid.IsVisible,
            on => _grid?.SetVisible(on));

        AddBinding("Button_Trails", "轨迹", "开", "关",
            () => SandboxTrail.Visible,
            on => SandboxTrail.SetVisible(on));

        AddBinding("Button_Orbits", "轨道", "开", "关",
            () => SandboxTrail.OrbitVisible,
            on => SandboxTrail.SetOrbitVisible(on));

        AddBinding("Button_Labels", "天体标签", "开", "关",
            () => _controller != null && _controller.LabelsVisible,
            on => _controller?.SetLabelsVisible(on));

        AddBinding("Button_GlobalGravitation", "全局引力", "开", "关",
            () => SandboxPhysicsSystem.GravityEnabled,
            on => SandboxPhysicsSystem.GravityEnabled = on);

        AddBinding("Button_ReceiveRadiation", "辐射加热", "开", "关",
            () => SandboxPhysicsSystem.ThermalReceiveRadiation,
            on => SandboxPhysicsSystem.ThermalReceiveRadiation = on);

        AddBinding("Button_AtmoSpherericHeating", "大气加热", "开", "关",
            () => SandboxPhysicsSystem.ThermalAtmosphereHeating,
            on => SandboxPhysicsSystem.ThermalAtmosphereHeating = on);

        AddBinding("Button_ThermalRadiationLossHeat", "热辐射散热", "开", "关",
            () => SandboxPhysicsSystem.ThermalRadiationLoss,
            on => SandboxPhysicsSystem.ThermalRadiationLoss = on);

        AddBinding("Button_CollisionHeating", "碰撞加热", "开", "关",
            () => SandboxPhysicsSystem.ThermalCollisionHeat,
            on => SandboxPhysicsSystem.ThermalCollisionHeat = on);

        SetupVolume();
        ShowModule(0);
        ScanAndWire();
        RefreshState();
    }

    private void AddBinding(string buttonName, string label, string onWord, string offWord,
            System.Func<bool> get, System.Action<bool> set) {
        _bindings.Add(new Binding {
            ButtonName = buttonName, Label = label, OnWord = onWord, OffWord = offWord,
            Get = get, Set = set
        });
    }

    // ~~~ 扫描 & 接线 ~~~

    private bool _loggedTree;

    private void ScanAndWire() {
        if (_panel == null) return;

        foreach (Binding binding in _bindings) {
            foreach (Transform t in FindButtons(binding)) {
                Button button = t.GetComponent<Button>();
                if (button == null || _wired.ContainsKey(button)) continue;
                Binding captured = binding;
                button.onClick.AddListener(() => {
                    captured.Set(!captured.Get());
                    RefreshState();
                });
                _wired[button] = binding;
            }
        }

        if (!_loggedTree && _panel != null) {
            _loggedTree = true;
            LogTree(_panel.transform, "");
        }
        foreach (Transform t in FindAll("Button_ShutGlobalThermalSim")) {
            Button button = t.GetComponent<Button>();
            if (button == null || _shutWired.Contains(button)) continue;
            button.onClick.AddListener(ShutAllThermalChannels);
            _shutWired.Add(button);
        }
    }

    /// <summary>一次性把面板里的按钮和文字打出来，方便排查名字对不上的情况。</summary>
    private static void LogTree(Transform root, string indent) {
        Button button = root.GetComponent<Button>();
        if (button != null) {
            TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
            Debug.Log("[Settings] button '" + root.name + "' text='" +
                      (text != null ? text.text : "-") + "'");
        }
        for (int i = 0; i < root.childCount; i++) {
            LogTree(root.GetChild(i), indent + "  ");
        }
    }

    private List<Transform> FindButtons(Binding binding) {
        List<Transform> result = FindAll(binding.ButtonName);
        if (result.Count > 0) return result;

        // 名字对不上时按文字兜底：文字以“标签：”或“标签”开头
        Transform root = _panel != null ? _panel.transform : null;
        if (root == null) return result;
        CollectByLabel(root, binding.Label, result);
        return result;
    }

    private static void CollectByLabel(Transform root, string label, List<Transform> result) {
        Button button = root.GetComponent<Button>();
        if (button != null) {
            TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
            if (text != null && text.text != null && text.text.StartsWith(label)) {
                result.Add(root);
                return;
            }
        }
        for (int i = 0; i < root.childCount; i++) CollectByLabel(root.GetChild(i), label, result);
    }

    // ~~~ 状态刷新 ~~~

    private void RefreshState() {
        foreach (KeyValuePair<Button, Binding> pair in _wired) {
            RefreshToggle(pair.Key, pair.Value);
        }
    }

    private static void RefreshToggle(Button button, Binding binding) {
        if (button == null) return;
        bool on = binding.Get();
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) {
            text.text = binding.Label + "：" + (on ? binding.OnWord : binding.OffWord);
        }
        if (button.targetGraphic != null) {
            button.targetGraphic.color = on ? StateOn : StateOff;
        }
    }

    private void Update() {
        if (!IsOpen) return;
        if (Time.unscaledTime < _nextScan) return;
        _nextScan = Time.unscaledTime + 0.15f;
        ScanAndWire();
        RefreshState();
    }

    /// <summary>一键关闭所有热学通道。</summary>
    private void ShutAllThermalChannels() {
        SandboxPhysicsSystem.ThermalReceiveRadiation = false;
        SandboxPhysicsSystem.ThermalAtmosphereHeating = false;
        SandboxPhysicsSystem.ThermalRadiationLoss = false;
        SandboxPhysicsSystem.ThermalCollisionHeat = false;
        RefreshState();
        FloatingText.Show("已关闭全部热学通道");
    }

    // ~~~ 模块 ~~~

    public void Open() {
        if (_panel == null) return;
        _controller?.CloseCreatePanelForSettings();
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        ShowModule(_module);
        ScanAndWire();
        RefreshState();
    }

    public void Close() {
        if (_panel != null) _panel.SetActive(false);
    }

    private void ShowModule(int index) {
        _module = index;
        if (_generalPanel != null) _generalPanel.SetActive(index == 0);
        if (_gravityPanel != null) _gravityPanel.SetActive(index == 1);
        if (_thermalPanel != null) _thermalPanel.SetActive(index == 2);

        SetModuleTint(_generalButton, index == 0);
        SetModuleTint(_gravityButton, index == 1);
        SetModuleTint(_thermalButton, index == 2);
    }

    private static void SetModuleTint(Button button, bool selected) {
        if (button == null || button.targetGraphic == null) return;
        button.targetGraphic.color = selected ? SelectedModule : NormalModule;
    }

    /// <summary>创建天体面板打开时，把设置按钮一起藏起来。</summary>
    public void SetOpenButtonVisible(bool visible) {
        if (_openButton != null) _openButton.gameObject.SetActive(visible);
    }

    // ~~~ 音量（BGM）~~~

    private void SetupVolume() {
        _volumeSlider = Find("VolumeSlider")?.GetComponent<Slider>();
        _volumeText = Find("Text_VolumePercentage")?.GetComponent<TMP_Text>();
        if (_volumeSlider == null) return;

        float volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        _volumeSlider.SetValueWithoutNotify(volume);
        ApplyVolume(volume);

        _volumeSlider.onValueChanged.AddListener(value => {
            ApplyVolume(value);
            PlayerPrefs.SetFloat("MusicVolume", value);
            PlayerPrefs.Save();
        });
    }

    private void ApplyVolume(float value) {
        value = Mathf.Clamp01(value);
        if (BGMPlayer.Instance != null) {
            BGMPlayer.Instance.SetVolume(value);
        } else {
            AudioListener.volume = value;
        }
        if (_volumeText != null) _volumeText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // ~~~ 查找 ~~~

    /// <summary>在设置面板里找出所有同名子物体（复制面板时容易留下重名按钮）。</summary>
    private List<Transform> FindAll(string name) {
        List<Transform> result = new();
        Transform root = _panel != null ? _panel.transform : null;
        if (root != null) {
            CollectByName(root, name, result);
            if (result.Count > 0) return result;
        }
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>()) {
            if (t == null || t.name != name) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            result.Add(t);
        }
        return result;
    }

    private static void CollectByName(Transform root, string name, List<Transform> result) {
        if (root.name == name) result.Add(root);
        for (int i = 0; i < root.childCount; i++) {
            CollectByName(root.GetChild(i), name, result);
        }
    }

    private static Transform Find(string name) {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>()) {
            if (t == null || t.name != name) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            return t;
        }
        return null;
    }
}
