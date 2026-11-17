using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>HomePagePanel：三个存档一页、双击读档、保存当前场景。</summary>
public class SandboxHomePanel : MonoBehaviour {
    private const int SlotsPerPage = 3;

    private SandboxSceneController _controller;
    private GameObject _root;
    private Button _newSaveButton;
    private Button _saveButton;
    private Button _quitButton;
    private Button _lastPageButton;
    private Button _nextPageButton;
    private TMP_Text _pageLabel;
    private TMP_InputField _saveInput;

    private readonly RawImage[] _slotImages = new RawImage[SlotsPerPage];
    private readonly TMP_Text[] _slotLabels = new TMP_Text[SlotsPerPage];
    private readonly SandboxSaveSlot[] _slots = new SandboxSaveSlot[SlotsPerPage];
    private List<SandboxSaveEntry> _entries = new();
    private int _page;

    public bool IsShowing => _root != null && _root.activeSelf;

    public void Initialize(SandboxSceneController controller) {
        _controller = controller;
        _root = gameObject;

        _newSaveButton = FindButton("Button_NewSaves");
        _saveButton = FindButton("Button_SaveScene");
        _quitButton = FindButton("Button_QuitSandbox");
        _lastPageButton = FindButton("Button_LastPage");
        _nextPageButton = FindButton("Button_NextPage");
        _pageLabel = FindText("PageLabelText");
        _saveInput = FindDeep(_root.transform, "IF_InputSceneName")?.GetComponent<TMP_InputField>();

        if (_newSaveButton != null) _newSaveButton.onClick.AddListener(NewSave);
        if (_saveButton != null) _saveButton.onClick.AddListener(BeginSaveScene);
        if (_quitButton != null) _quitButton.onClick.AddListener(QuitSandbox);
        if (_lastPageButton != null) {
            _lastPageButton.onClick.AddListener(() => ChangePage(-1));
        }
        if (_nextPageButton != null) {
            _nextPageButton.onClick.AddListener(() => ChangePage(1));
        }
        if (_saveInput != null) {
            _saveInput.onSubmit.AddListener(CommitSaveScene);
            _saveInput.gameObject.SetActive(false);
        }

        for (int i = 0; i < SlotsPerPage; i++) {
            Transform slot = FindDeep(_root.transform, $"SavePos0{i + 1}");
            if (slot == null) continue;
            _slotImages[i] = slot.GetComponent<RawImage>();
            if (_slotImages[i] != null) {
                _slotImages[i].color = Color.white;
                _slotImages[i].raycastTarget = true;
                AspectRatioFitter fitter = slot.GetComponent<AspectRatioFitter>();
                if (fitter == null) fitter = slot.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                fitter.aspectRatio = 5f / 3f;
            }
            _slotLabels[i] = FindDeep(slot, "Label")?.GetComponent<TMP_Text>();
            if (_slotLabels[i] != null) _slotLabels[i].raycastTarget = false;
            _slots[i] = slot.GetComponent<SandboxSaveSlot>();
            if (_slots[i] == null) _slots[i] = slot.gameObject.AddComponent<SandboxSaveSlot>();
            int index = i;
            _slots[i].onDoubleClick = _ => LoadEntry(_entries, index);
        }

        Refresh();
        gameObject.SetActive(false);
    }

    private void OnEnable() {
        if (_controller != null) Refresh();
    }

    public void RefreshEntries() {
        Refresh();
    }

    public void Show() {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide() {
        gameObject.SetActive(false);
        if (_saveInput != null) _saveInput.gameObject.SetActive(false);
    }

    private void NewSave() {
        if (_controller == null) return;
        // 新场景也会丢弃当前存档的修改，所以统一走带确认的入口。
        _controller.RequestNewBlankScenario();
        Hide();
    }

    private void BeginSaveScene() {
        if (_saveInput == null) return;
        _saveInput.gameObject.SetActive(true);
        // 内置场景显示“副本”名，保存时不会覆盖模板。
        _saveInput.text = _controller != null ? _controller.SuggestedSaveName : "";
        _saveInput.Select();
        _saveInput.ActivateInputField();
    }

    private void CommitSaveScene(string value) {
        if (_controller == null) return;
        string name = value == null ? "" : value.Trim();
        if (string.IsNullOrEmpty(name)) {
            FloatingText.Show("场景名不能为空");
            _saveInput.Select();
            _saveInput.ActivateInputField();
            return;
        }

        if (_controller.RequestSave(name)) {
            _saveInput.gameObject.SetActive(false);
            Refresh();
        }
    }

    private void QuitSandbox() {
        if (_controller != null) {
            _controller.RequestQuitSandbox();
        } else {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void ChangePage(int delta) {
        int pages = Mathf.Max(1, Mathf.CeilToInt(_entries.Count / (float)SlotsPerPage));
        _page = Mathf.Clamp(_page + delta, 0, pages - 1);
        RefreshSlots();
    }

    private void Refresh() {
        _entries = SandboxSaveSystem.ListEntries();
        int pages = Mathf.Max(1, Mathf.CeilToInt(_entries.Count / (float)SlotsPerPage));
        _page = Mathf.Clamp(_page, 0, pages - 1);
        RefreshSlots();
    }

    private void RefreshSlots() {
        int pages = Mathf.Max(1, Mathf.CeilToInt(_entries.Count / (float)SlotsPerPage));
        int start = _page * SlotsPerPage;

        for (int i = 0; i < SlotsPerPage; i++) {
            int index = start + i;
            bool valid = index < _entries.Count;
            if (_slotImages[i] != null) {
                _slotImages[i].gameObject.SetActive(valid);
            }
            if (!valid) continue;

            SandboxSaveEntry entry = _entries[index];
            if (_slotImages[i] != null) {
                _slotImages[i].texture = entry.thumbnail;
                _slotImages[i].color = Color.white;
            }
            if (_slotLabels[i] != null) {
                _slotLabels[i].text = entry.name;
            }
            if (_slots[i] != null) _slots[i].entry = entry;
        }

        if (_pageLabel != null) _pageLabel.text = $"{_page + 1}/{pages}";
        if (_lastPageButton != null) _lastPageButton.interactable = _page > 0;
        if (_nextPageButton != null) _nextPageButton.interactable = _page < pages - 1;
    }

    private void LoadEntry(List<SandboxSaveEntry> entries, int index) {
        int absolute = _page * SlotsPerPage + index;
        if (absolute < 0 || absolute >= entries.Count) return;
        SandboxSaveEntry entry = entries[absolute];
        Hide();
        if (_controller == null) return;
        if (entry.builtin) {
            _controller.RequestLoadDefaultScenario(entry.builtinId);
        } else {
            _controller.RequestLoadSaveFile(entry.path);
        }
    }

    private Button FindButton(string name) =>
        FindDeep(_root.transform, name)?.GetComponent<Button>();

    private TMP_Text FindText(string name) {
        Transform t = FindDeep(_root.transform, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
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
}
