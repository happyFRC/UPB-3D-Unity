using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主序星演化场景的“演化设置 / 新建恒星系”界面。
/// 所有 UI 都按场景里现有的物体名字自动找，不需要手动拖引用。
/// </summary>
public class EvolutionSettingsController : MonoBehaviour {
    private const int PlanetsPerPage = 2;
    private static readonly Color RowBg = new(0.10f, 0.13f, 0.18f, 0.95f);
    private static readonly Color RowSelectedBg = new(0.20f, 0.36f, 0.52f, 1f);
    private static readonly Color TextMain = new(0.94f, 0.96f, 1f, 1f);

    private GameObject _canvasRoot;
    private GameObject _settingsPanel;
    private GameObject _newSystemPanel;
    private GameObject _planetPanel;
    private Image _settingsBackground;

    private Button _openSettingsButton;
    private Button _newSystemButton;
    private Button _selectSystemButton;
    private Button _returnEvolutionButton;
    private Button _saveReturnButton;
    private Button _nextPageButton;
    private Button _lastPageButton;
    private Button _addNewPageButton;

    private TMP_InputField _systemNameInput;
    private TMP_InputField _aInput;
    private TMP_InputField _eInput;
    private TMP_InputField _rInput;
    private TMP_InputField _gInput;
    private TMP_InputField _bInput;

    private TMP_Text _newPlanetLabel;
    private TMP_Text _pageText;
    private TMP_Text _selectButtonLabel;
    private TMP_Text _semiMajorLabel;

    private Toggle _orbitToggle;
    private Toggle _hzToggle;
    private PlanetaryOrbitRenderer _orbitRenderer;
    private HabitableZoneRenderer _habitableZone;
    private StellarEvolutionScene _evolutionScene;
    private CameraDistanceHUD _distanceHud;

    private readonly List<GameObject> _evolutionUi = new();
    private readonly Dictionary<GameObject, bool> _evolutionUiInitial = new();
    private readonly Dictionary<GameObject, bool> _evolutionUiBefore = new();

    private readonly List<PlanetPageData> _pageData = new();
    private readonly PlanetForm[] _forms = new PlanetForm[2];
    private int _page;

    private EvolutionSystemData _selectedSystem;
    private string _selectedSystemName = EvolutionSystemDefaults.SolarSystemName;

    private bool _settingsOpen;
    private float _timeScaleBefore = 1f;
    private Sprite _dotSprite;
    private Texture2D _dotTexture;

    private void Start() {
        // 防止上一次场景在设置界面里退出时把 timeScale 留成 0。
        if (Time.timeScale <= 0f) Time.timeScale = 1f;

        _canvasRoot = GameObject.Find("Canvas");
        if (_canvasRoot == null) {
            Debug.LogError("[EvolutionSettings] 找不到 Canvas");
            enabled = false;
            return;
        }

        CacheUi();
        if (_settingsPanel == null || _newSystemPanel == null) {
            Debug.LogError("[EvolutionSettings] 找不到 SettingsPanel / NewSystemPanel");
            enabled = false;
            return;
        }

        CacheEvolutionUi();
        _settingsPanel.SetActive(false);
        _newSystemPanel.SetActive(false);
        SetSettingsButtonsVisible(true);

        _evolutionScene = FindObjectOfType<StellarEvolutionScene>();
        _orbitRenderer = FindObjectOfType<PlanetaryOrbitRenderer>();
        _habitableZone = FindObjectOfType<HabitableZoneRenderer>();
        _distanceHud = FindObjectOfType<CameraDistanceHUD>();
        _orbitToggle = FindToggle("TogglePlanetOrbit");
        _hzToggle = FindToggle("ToggleHabitableZone");

        _selectedSystem = EvolutionSystemDefaults.SolarSystem();
        _selectedSystemName = EvolutionSystemDefaults.SolarSystemName;
        if (_orbitRenderer != null && _orbitRenderer.toggleShowOrbits == null && _orbitToggle != null) {
            _orbitRenderer.toggleShowOrbits = _orbitToggle;
        }
        _orbitRenderer?.SetSystem(_selectedSystem);
        UpdateSelectButtonText();

        if (_addNewPageButton != null) _addNewPageButton.gameObject.SetActive(false);
        WireUi();
        ResetNewSystemForm();
        ClearUiSelection();
    }

    private void Update() {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_newSystemPanel != null && _newSystemPanel.activeSelf) {
            BackToSettings();
        } else if (_settingsOpen) {
            CloseToEvolution();
        } else {
            OpenSettings();
        }
    }

    // ~~~ 初始化 ~~~ //

    private void CacheUi() {
        _settingsPanel = FindDeep(_canvasRoot.transform, "SettingsPanel")?.gameObject;
        _settingsBackground = _settingsPanel != null ? _settingsPanel.GetComponent<Image>() : null;
        _newSystemPanel = FindDeep(_canvasRoot.transform, "NewSystemPanel")?.gameObject;
        _planetPanel = FindDeep(_newSystemPanel != null ? _newSystemPanel.transform : null,
            "SetNewPlanetPanel")?.gameObject;

        _openSettingsButton = FindButton("ButtonEvolutionSetting");
        _newSystemButton = FindButton("ButtonNewStellarSystem");
        _selectSystemButton = FindButton("ButtonSelectStellarSystem");
        _returnEvolutionButton = FindButton("ButtonReturnEvolution");
        _saveReturnButton = FindButton("ButtonReturnToSettings");

        _nextPageButton = FindButton("NextPage");
        _lastPageButton = FindButton("LastPage");
        _addNewPageButton = FindButton("AddNewPage");
        if (_addNewPageButton == null) _addNewPageButton = FindButton("ButtonAddNewPage");

        _systemNameInput = FindInput("IF_SetSystemName");
        _aInput = FindInput("IF_SetOribitSemiMajorAxis");
        _eInput = FindInput("IF_SetOribitEccentricity");
        _rInput = FindInput("IF_SetColorRed");
        _gInput = FindInput("IF_SetColorGreen");
        _bInput = FindInput("IF_SetColorBlue");

        _newPlanetLabel = FindText(_planetPanel != null ? _planetPanel.transform : null,
            "NewPlanetLabel");
        _pageText = FindDirectChild(_newSystemPanel != null ? _newSystemPanel.transform : null,
            "Text (TMP)")?.GetComponent<TMP_Text>();
        _selectButtonLabel = _selectSystemButton != null
            ? _selectSystemButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        _semiMajorLabel = FindText(_planetPanel != null ? _planetPanel.transform : null,
            "LabelSemiMajorAxis");
        if (_semiMajorLabel != null) _semiMajorLabel.text = "半长轴(AU)";
        BuildPlanetForms();
    }

    private void CacheEvolutionUi() {
        foreach (Transform child in _canvasRoot.transform) {
            if (child.gameObject == _settingsPanel) continue;
            _evolutionUi.Add(child.gameObject);
            _evolutionUiInitial[child.gameObject] = child.gameObject.activeSelf;
        }
    }

    private void WireUi() {
        if (_openSettingsButton != null) _openSettingsButton.onClick.AddListener(OpenSettings);
        if (_newSystemButton != null) _newSystemButton.onClick.AddListener(OpenNewSystem);
        if (_selectSystemButton != null) _selectSystemButton.onClick.AddListener(ChooseSystem);
        if (_returnEvolutionButton != null) _returnEvolutionButton.onClick.AddListener(CloseToEvolution);
        if (_saveReturnButton != null) _saveReturnButton.onClick.AddListener(SaveAndBack);
        if (_addNewPageButton != null) _addNewPageButton.onClick.AddListener(AddNewPage);
        if (_nextPageButton != null) _nextPageButton.onClick.AddListener(() => ShowPage(_page + 1));
        if (_lastPageButton != null) _lastPageButton.onClick.AddListener(() => ShowPage(_page - 1));

        if (_orbitToggle != null) {
            _orbitToggle.onValueChanged.AddListener(visible => {
                if (!_settingsOpen) _orbitRenderer?.SetVisible(visible);
            });
        }
    }

    // ~~~ 面板开关 / 暂停 ~~~ //

    private void OpenSettings() {
        if (_settingsOpen) return;

        _settingsOpen = true;
        _timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        if (_evolutionScene != null) _evolutionScene.inputBlocked = true;

        _evolutionUiBefore.Clear();
        foreach (GameObject go in _evolutionUi) {
            if (go != null) _evolutionUiBefore[go] = go.activeSelf;
        }
        SetEvolutionUiVisible(false);
        HideSceneExtras();
        // CameraDistanceHUD 每帧会把距离文本重新 SetActive(true)，设置界面里要停掉它。
        if (_distanceHud != null) _distanceHud.enabled = false;

        _settingsPanel.SetActive(true);
        _settingsPanel.transform.SetAsLastSibling();
        _newSystemPanel.SetActive(false);
        SetSettingsButtonsVisible(true);
        if (_addNewPageButton != null) _addNewPageButton.gameObject.SetActive(false);
        UpdateSelectButtonText();
        ClearUiSelection();
    }

    private void CloseToEvolution() {
        if (!_settingsOpen) return;

        _settingsOpen = false;
        _newSystemPanel.SetActive(false);
        _settingsPanel.SetActive(false);
        if (_addNewPageButton != null) _addNewPageButton.gameObject.SetActive(false);
        if (_settingsBackground != null) _settingsBackground.enabled = true;
        if (_distanceHud != null) _distanceHud.enabled = true;
        foreach (GameObject go in _evolutionUi) {
            if (go == null) continue;
            bool active = _evolutionUiBefore.TryGetValue(go, out bool previous)
                ? previous
                : (_evolutionUiInitial.TryGetValue(go, out bool initial) && initial);
            go.SetActive(active);
        }

        if (_evolutionScene != null) _evolutionScene.inputBlocked = false;
        Time.timeScale = _timeScaleBefore > 0f ? _timeScaleBefore : 1f;
        RestoreSceneExtras();
        // 点过“演化设置”后该按钮会保持 EventSystem 选中状态，
        // 回到演化再按空格会触发 UI Submit，又把设置面板打开。
        ClearUiSelection();
    }

    private void OpenNewSystem() {
        _settingsPanel.SetActive(true);
        if (_settingsBackground != null) _settingsBackground.enabled = false;
        SetSettingsButtonsVisible(false);
        _newSystemPanel.SetActive(true);
        _newSystemPanel.transform.SetAsLastSibling();
        if (_addNewPageButton != null) {
            _addNewPageButton.gameObject.SetActive(true);
            // NewSystemPanel 是全屏 Image，AddNewPage 是它的兄弟；
            // 不放到最后会被 NewSystemPanel 挡住射线，导致“看得见但点不到”。
            _addNewPageButton.transform.SetAsLastSibling();
        }
        ResetNewSystemForm();
        ClearUiSelection();
    }

    private void BackToSettings() {
        if (_newSystemPanel != null) _newSystemPanel.SetActive(false);
        if (_addNewPageButton != null) _addNewPageButton.gameObject.SetActive(false);
        if (_settingsBackground != null) _settingsBackground.enabled = true;
        SetSettingsButtonsVisible(true);
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
        ClearUiSelection();
    }

    private void SetSettingsButtonsVisible(bool visible) {
        if (_newSystemButton != null) _newSystemButton.gameObject.SetActive(visible);
        if (_selectSystemButton != null) _selectSystemButton.gameObject.SetActive(visible);
        if (_returnEvolutionButton != null) _returnEvolutionButton.gameObject.SetActive(visible);
    }

    private void SetEvolutionUiVisible(bool visible) {
        foreach (GameObject go in _evolutionUi) {
            if (go == null) continue;
            bool initial = _evolutionUiInitial.TryGetValue(go, out bool active) && active;
            go.SetActive(visible && initial);
        }
    }

    private void HideSceneExtras() {
        _orbitRenderer?.SetVisible(false);
        _habitableZone?.SetVisible(false);
    }

    private void RestoreSceneExtras() {
        if (_orbitRenderer != null) {
            _orbitRenderer.SetVisible(_orbitToggle != null && _orbitToggle.isOn);
        }
        if (_habitableZone != null) {
            _habitableZone.SetVisible(_hzToggle != null && _hzToggle.isOn);
        }
    }

    // ~~~ 新建恒星系 ~~~ //

    private void BuildPlanetForms() {
        Transform pos1 = FindDeep(_newSystemPanel.transform, "SetNewPlanetPos1");
        Transform pos2 = FindDeep(_newSystemPanel.transform, "SetNewPlanetPos2");
        if (pos1 == null || pos2 == null) {
            Debug.LogError("[EvolutionSettings] 找不到 SetNewPlanetPos1 / SetNewPlanetPos2");
            return;
        }

        _forms[0] = BuildPlanetForm(pos1, 0);
        _forms[1] = BuildPlanetForm(pos2, 1);
    }

    private PlanetForm BuildPlanetForm(Transform root, int slot) {
        PlanetForm form = new() {
            root = root.gameObject,
            slot = slot,
            a = FindInputIn(root, "IF_SetOribitSemiMajorAxis"),
            e = FindInputIn(root, "IF_SetOribitEccentricity"),
            r = FindInputIn(root, "IF_SetColorRed"),
            g = FindInputIn(root, "IF_SetColorGreen"),
            b = FindInputIn(root, "IF_SetColorBlue"),
            delete = FindButtonIn(root, "DeleteThePlanet"),
            label = FindTextIn(root, "NewPlanetLabel")
        };

        TMP_Text semiLabel = FindTextIn(root, "LabelSemiMajorAxis");
        if (semiLabel != null) semiLabel.text = "半长轴(AU)";

        if (form.delete != null) {
            form.delete.onClick.AddListener(() => DeleteForm(form));
        }

        SetFormValue(form, EmptyFormValue());
        if (form.label != null) form.label.text = EvolutionSystemDefaults.PlanetName(slot);
        return form;
    }

    private void ResetNewSystemForm() {
        _pageData.Clear();
        _page = 0;
        _pageData.Add(CreatePageData());

        if (_systemNameInput != null) _systemNameInput.text = "";
        LoadCurrentPage();
    }

    private static PlanetPageData CreatePageData() {
        return new PlanetPageData {
            values = new[] { EmptyFormValue(), EmptyFormValue() }
        };
    }

    private static PlanetFormValue EmptyFormValue() {
        return new PlanetFormValue { a = "", e = "", r = "0", g = "0", b = "0" };
    }

    private void SaveCurrentPage() {
        if (_pageData.Count == 0) _pageData.Add(CreatePageData());
        _page = Mathf.Clamp(_page, 0, _pageData.Count - 1);

        PlanetPageData page = _pageData[_page];
        page.values[0] = ReadForm(_forms[0]);
        page.values[1] = ReadForm(_forms[1]);
    }

    private void LoadCurrentPage() {
        if (_pageData.Count == 0) _pageData.Add(CreatePageData());
        _page = Mathf.Clamp(_page, 0, _pageData.Count - 1);

        PlanetPageData page = _pageData[_page];
        SetFormValue(_forms[0], page.values[0]);
        SetFormValue(_forms[1], page.values[1]);

        if (_forms[0] != null && _forms[0].label != null) {
            _forms[0].label.text = EvolutionSystemDefaults.PlanetName(_page * 2);
        }
        if (_forms[1] != null && _forms[1].label != null) {
            _forms[1].label.text = EvolutionSystemDefaults.PlanetName(_page * 2 + 1);
        }

        if (_pageText != null) _pageText.text = $"{_page + 1}/{_pageData.Count}";
        if (_nextPageButton != null) _nextPageButton.interactable = _page < _pageData.Count - 1;
        if (_lastPageButton != null) _lastPageButton.interactable = _page > 0;
    }

    private void ShowPage(int page) {
        SaveCurrentPage();
        _page = Mathf.Clamp(page, 0, _pageData.Count - 1);
        LoadCurrentPage();
        ClearUiSelection();
    }

    private void AddNewPage() {
        SaveCurrentPage();
        PlanetPageData page = _pageData[_page];

        // 当前页两个行星都有效，才允许新建下一页。
        if (!IsValidFormValue(page.values[0]) || !IsValidFormValue(page.values[1])) {
            FloatingText.Show("当前页两个行星都要填有效值，才能新增下一页");
            return;
        }

        _pageData.Add(CreatePageData());
        _page = _pageData.Count - 1;
        LoadCurrentPage();
        ClearUiSelection();
    }

    private void DeleteForm(PlanetForm form) {
        if (form == null || _pageData.Count == 0) return;
        SaveCurrentPage();

        int index = _page * 2 + form.slot;
        List<PlanetFormValue> values = new();
        foreach (PlanetPageData page in _pageData) {
            values.Add(page.values[0]);
            values.Add(page.values[1]);
        }
        if (index < 0 || index >= values.Count) return;

        values.RemoveAt(index);
        values.Add(EmptyFormValue());

        for (int i = 0; i < _pageData.Count; i++) {
            _pageData[i].values[0] = values[i * 2];
            _pageData[i].values[1] = values[i * 2 + 1];
        }

        LoadCurrentPage();
        ClearUiSelection();
    }

    private static PlanetFormValue ReadForm(PlanetForm form) {
        if (form == null) return EmptyFormValue();
        return new PlanetFormValue {
            a = form.a != null ? form.a.text : "",
            e = form.e != null ? form.e.text : "",
            r = form.r != null ? form.r.text : "0",
            g = form.g != null ? form.g.text : "0",
            b = form.b != null ? form.b.text : "0"
        };
    }

    private static void SetFormValue(PlanetForm form, PlanetFormValue value) {
        if (form == null) return;
        if (form.a != null) form.a.text = value.a;
        if (form.e != null) form.e.text = value.e;
        if (form.r != null) form.r.text = value.r;
        if (form.g != null) form.g.text = value.g;
        if (form.b != null) form.b.text = value.b;
    }

    private static bool IsValidFormValue(PlanetFormValue value) {
        if (!TryParse(value.a, out double aAu) || aAu <= 0.0) return false;
        if (string.IsNullOrWhiteSpace(value.e)) return true;
        return TryParse(value.e, out double e) && e >= 0.0 && e < 1.0;
    }

    private List<EvolutionPlanetData> CollectPlanets(out string error) {
        error = null;
        SaveCurrentPage();
        List<EvolutionPlanetData> planets = new();

        for (int pageIndex = 0; pageIndex < _pageData.Count; pageIndex++) {
            for (int slot = 0; slot < 2; slot++) {
                PlanetFormValue value = _pageData[pageIndex].values[slot];
                if (string.IsNullOrWhiteSpace(value.a)) continue;
                if (!TryParse(value.a, out double aAu)) {
                    error = "半长轴格式错误";
                    return null;
                }
                if (aAu <= 0.0) continue;

                double eccentricity = 0.0;
                if (!string.IsNullOrWhiteSpace(value.e)) {
                    if (!TryParse(value.e, out eccentricity) ||
                        eccentricity < 0.0 || eccentricity >= 1.0) {
                        error = "偏心率必须在 0 到 1 之间";
                        return null;
                    }
                }

                float r = ParseColor(value.r);
                float g = ParseColor(value.g);
                float b = ParseColor(value.b);
                if (r + g + b <= 0.001f) r = g = b = 0.8f;

                int globalIndex = pageIndex * 2 + slot;
                planets.Add(new EvolutionPlanetData {
                    name = EvolutionSystemDefaults.PlanetName(globalIndex),
                    semiMajorAxisAu = aAu,
                    semiMajorAxisKm = aAu * EvolutionSystemDefaults.AuKm,
                    eccentricity = eccentricity,
                    colorR = r,
                    colorG = g,
                    colorB = b
                });
            }
        }

        return planets;
    }

    private static TMP_InputField FindInputIn(Transform root, string name) {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_InputField>() : null;
    }

    private static Button FindButtonIn(Transform root, string name) {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static TMP_Text FindTextIn(Transform root, string name) {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    // ~~~ 保存 / 选择恒星系 ~~~ //

    private static string SystemsDirectory {
        get {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "Data", "EvolutionSystems");
#else
            return Path.Combine(Application.persistentDataPath, "Data", "EvolutionSystems");
#endif
        }
    }

    private void SaveAndBack() {
        List<EvolutionPlanetData> planets = CollectPlanets(out string planetError);
        if (!string.IsNullOrEmpty(planetError)) {
            FloatingText.Show(planetError);
            return;
        }
        if (planets == null || planets.Count == 0) {
            FloatingText.Show("无有效行星，无法保存");
            BackToSettings();
            return;
        }

        string systemName = _systemNameInput != null ? _systemNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(systemName)) {
            FloatingText.Show("请输入恒星系名");
            return;
        }

        EvolutionSystemData system = new() {
            name = systemName,
            planets = planets
        };

        try {
            Directory.CreateDirectory(SystemsDirectory);
            string fileName = SanitizeFileName(systemName);
            if (string.IsNullOrEmpty(fileName)) {
                FloatingText.Show("恒星系名无效");
                return;
            }
            string path = Path.Combine(SystemsDirectory, fileName + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(system, true));
            Debug.Log("[EvolutionSettings] 已保存 " + path);
        } catch (Exception e) {
            FloatingText.Show("保存失败：" + e.Message);
            return;
        }

        _selectedSystem = system;
        _selectedSystemName = system.name;
        _orbitRenderer?.SetSystem(system);
        if (_settingsOpen) _orbitRenderer?.SetVisible(false);
        UpdateSelectButtonText();

        FloatingText.Show("已保存：" + system.name);
        BackToSettings();
    }

    private void ChooseSystem() {
        Directory.CreateDirectory(SystemsDirectory);

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel(
            "选择恒星系（双击 JSON）", SystemsDirectory, "json");
        if (!string.IsNullOrEmpty(path)) LoadSystemFile(path);
#else
        Application.OpenURL("file://" + SystemsDirectory);
        FloatingText.Show("请打开 Data/EvolutionSystems，双击 JSON 选择");
#endif
    }

    private void LoadSystemFile(string path) {
        try {
            string json = File.ReadAllText(path);
            EvolutionSystemData system = JsonUtility.FromJson<EvolutionSystemData>(json);
            if (system == null || system.planets == null) {
                FloatingText.Show("恒星系 JSON 无效");
                return;
            }

            system.planets.RemoveAll(p => p == null || p.EffectiveSemiMajorAxisKm <= 0.0);
            if (system.planets.Count == 0) {
                FloatingText.Show("该恒星系没有有效行星");
                return;
            }

            if (string.IsNullOrWhiteSpace(system.name)) {
                system.name = Path.GetFileNameWithoutExtension(path);
            }

            _selectedSystem = system;
            _selectedSystemName = system.name;
            _orbitRenderer?.SetSystem(system);
            if (_settingsOpen) _orbitRenderer?.SetVisible(false);
            UpdateSelectButtonText();
            FloatingText.Show("已选择：" + system.name);
        } catch (Exception e) {
            FloatingText.Show("读取恒星系失败：" + e.Message);
        }
    }

    private void UpdateSelectButtonText() {
        if (_selectButtonLabel != null) {
            _selectButtonLabel.text = "选择恒星系：" + _selectedSystemName;
        }
    }

    private static void ClearUiSelection() {
        if (EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private static string SanitizeFileName(string name) {
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid) name = name.Replace(c, '_');
        return name.Trim();
    }

    // ~~~ 工具 ~~~ //

    private static bool TryParse(string text, out double value) {
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out value)) {
            return true;
        }
        return double.TryParse(text, out value);
    }

    private static float ParseColor(string text) {
        if (string.IsNullOrWhiteSpace(text)) return 0f;
        if (!TryParse(text, out double value)) return 0f;
        if (value > 1.0) value /= 255.0;
        return Mathf.Clamp01((float)value);
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

    private static Transform FindDirectChild(Transform root, string name) {
        if (root == null) return null;
        foreach (Transform child in root) {
            if (child.name == name) return child;
        }
        return null;
    }

    private Button FindButton(string name) {
        Transform t = FindDeep(_canvasRoot.transform, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private TMP_InputField FindInput(string name) {
        Transform t = FindDeep(_canvasRoot.transform, name);
        return t != null ? t.GetComponent<TMP_InputField>() : null;
    }

    private static TMP_Text FindText(Transform root, string name) {
        if (root == null) return null;
        Transform t = root.name == name ? root : FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private Toggle FindToggle(string name) {
        Transform t = FindDeep(_canvasRoot.transform, name);
        return t != null ? t.GetComponent<Toggle>() : null;
    }

    private void OnDestroy() {
        // 如果场景在设置界面打开时被卸载，也要把时间恢复，避免下次演化卡在 0 倍速。
        if (_settingsOpen) {
            Time.timeScale = _timeScaleBefore > 0f ? _timeScaleBefore : 1f;
            if (_evolutionScene != null) _evolutionScene.inputBlocked = false;
        }
        if (_dotSprite != null) Destroy(_dotSprite);
        if (_dotTexture != null) Destroy(_dotTexture);
    }

    private sealed class PlanetPageData {
        public PlanetFormValue[] values;
    }

    private sealed class PlanetForm {
        public GameObject root;
        public TMP_InputField a;
        public TMP_InputField e;
        public TMP_InputField r;
        public TMP_InputField g;
        public TMP_InputField b;
        public Button delete;
        public TMP_Text label;
        public int slot;
    }

    private struct PlanetFormValue {
        public string a;
        public string e;
        public string r;
        public string g;
        public string b;
    }
}
