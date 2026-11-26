using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>场景介绍数据（Resources/Data/Scenarios.json）。</summary>
[System.Serializable]
public class SceneIntro {
    public string id;
    public string title;
    public string body;
}

[System.Serializable]
public class SceneIntroCatalog {
    public SceneIntro[] scenes;
}

/// <summary>
/// 场景介绍侧栏：打开带介绍的场景时弹出，点“确认”关闭。
/// 场景里已经摆好 Canvas/Description_Panel（Title / Context / Button_Confirm）。
/// </summary>
public class SandboxSceneDescription : MonoBehaviour {
    private TMP_Text _title;
    private TMP_Text _context;
    private Button _confirm;

    public bool IsOpen => gameObject.activeSelf;

    public void Initialize() {
        _title = FindText("Title");
        _context = FindText("Context");

        Transform confirm = FindChild(transform, "Button_Confirm");
        if (confirm != null) {
            _confirm = confirm.GetComponent<Button>();
            if (_confirm != null) {
                _confirm.onClick.RemoveAllListeners();
                _confirm.onClick.AddListener(Hide);
            }
        }

        Hide();
    }

    public void Show(string title, string body) {
        if (_title != null) _title.text = title;
        if (_context != null) _context.text = body;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    private TMP_Text FindText(string name) {
        Transform t = FindChild(transform, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChild(Transform root, string name) {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++) {
            Transform child = root.GetChild(i);
            if (child.name == name) return child;
        }
        return null;
    }
}
