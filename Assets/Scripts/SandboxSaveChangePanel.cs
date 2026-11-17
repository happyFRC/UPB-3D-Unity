using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SaveChangePanel：覆盖已有存档前的确认框。
/// 场景结构（SandboxScene.unity → Canvas/SaveChangePanel）：
///   Text (TMP)     “是否保存针对【场景名】的更改？”
///   ButtonYes      “是” → 确认保存
///   ButtonYes (1)  “否” → 撤销保存
/// 运行时不依赖 Inspector 连线，直接按名字找子物体。
/// </summary>
public class SandboxSaveChangePanel : MonoBehaviour
{
    private const string YesButtonName = "ButtonYes";
    private const string NoButtonName = "ButtonYes (1)";
    private const string TextName = "Text (TMP)";

    private GameObject _root;
    private Button _yesButton;
    private Button _noButton;
    private TMP_Text _message;
    private Action _onConfirm;
    private Action _onDiscard;
    private bool _wired;
    private bool _asking;

    public bool IsAsking => _asking;

    // 拖到 Inspector 上也能用；控制器运行时 AddComponent 时也只是复用。
    // 注意：这里只做引用/接线，不要隐藏自己——如果面板物体初始是隐藏的，
    // SetActive(true) 会触发 Awake，那样会在弹出来的同一帧又被关掉。
    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        _root = gameObject;

        Button[] buttons = GetComponentsInChildren<Button>(true);

        // 优先按按钮上的文字（是/否）认，其次按物体名，最后按子物体顺序兜底。
        _yesButton = FindButtonByLabel(buttons, "是") ?? FindButtonByName(buttons, YesButtonName);
        _noButton = FindButtonByLabel(buttons, "否") ?? FindButtonByName(buttons, NoButtonName);

        if (_yesButton == null || _noButton == null) {
            Button[] ordered = OrderedButtons(buttons);
            if (_yesButton == null && ordered.Length > 0) _yesButton = ordered[0];
            if (_noButton == null && ordered.Length > 1) _noButton = ordered[1];
        }

        _message = FindMessage(buttons);

        if (_yesButton == null) Debug.LogWarning("[Save] SaveChangePanel 找不到“是”按钮");
        if (_noButton == null) Debug.LogWarning("[Save] SaveChangePanel 找不到“否”按钮");
        if (_message == null) Debug.LogWarning("[Save] SaveChangePanel 找不到提示文字");

        if (!_wired) {
            if (_yesButton != null) _yesButton.onClick.AddListener(Confirm);
            if (_noButton != null) _noButton.onClick.AddListener(Cancel);
            _wired = true;
        }
    }

    /// <summary>隐藏弹窗（初始化时由控制器调用一次）。</summary>
    public void Hide()
    {
        _asking = false;
        _onConfirm = null;
        _onDiscard = null;
        if (_root == null) _root = gameObject;
        _root.SetActive(false);
    }

    private static Button FindButtonByLabel(Button[] buttons, string label)
    {
        foreach (Button button in buttons) {
            if (button == null) continue;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null && text.text != null && text.text.Trim() == label) return button;
        }
        return null;
    }

    private static Button FindButtonByName(Button[] buttons, string name)
    {
        foreach (Button button in buttons) {
            if (button != null && button.name == name) return button;
        }
        return null;
    }

    /// <summary>按层级顺序排列按钮，用来给“是/否”兜底。</summary>
    private static Button[] OrderedButtons(Button[] buttons)
    {
        System.Array.Sort(buttons, (a, b) =>
            string.CompareOrdinal(GetPath(a), GetPath(b)));
        return buttons;
    }

    private static string GetPath(Button button)
    {
        if (button == null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        Transform t = button.transform;
        while (t != null) {
            sb.Insert(0, "/" + t.GetSiblingIndex().ToString("D3"));
            t = t.parent;
        }
        return sb.ToString();
    }

    private TMP_Text FindMessage(Button[] buttons)
    {
        // 不在按钮里的那个文字就是提示文案。
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) {
            bool insideButton = false;
            foreach (Button button in buttons) {
                if (button != null && text.transform.IsChildOf(button.transform)) {
                    insideButton = true;
                    break;
                }
            }
            if (!insideButton) return text;
        }
        return FindText(TextName);
    }

    /// <summary>
    /// 弹窗询问：是否把当前状态保存到 sceneName。
    /// 是 → onConfirm；否 → onDiscard（不传表示“取消这次保存”）。
    /// 退出 / 切换存档时把 onDiscard 传成“继续执行切换”即可。
    /// </summary>
    public void Ask(string sceneName, Action onConfirm, Action onDiscard = null)
    {
        if (_root == null) _root = gameObject;

        _onConfirm = onConfirm;
        _onDiscard = onDiscard;
        _asking = true;

        if (_message != null) {
            _message.text = "是否保存针对【" + sceneName + "】的更改？";
        }

        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    private void Confirm()
    {
        Action callback = _onConfirm;
        _asking = false;
        _onConfirm = null;
        _onDiscard = null;
        _root.SetActive(false);
        callback?.Invoke();
    }

    private void Cancel()
    {
        Action callback = _onDiscard;
        _asking = false;
        _onConfirm = null;
        _onDiscard = null;
        _root.SetActive(false);

        if (callback != null) {
            callback();
        } else {
            FloatingText.Show("已取消保存");
        }
    }

    private Button FindButton(string name)
    {
        Transform t = FindChild(transform, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private TMP_Text FindText(string name)
    {
        Transform t = FindChild(transform, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root) {
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
