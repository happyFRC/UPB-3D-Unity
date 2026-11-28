using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OSE = OtherSimulationEngine;

public class OtherStarCalculationPreparationScene : MonoBehaviour {
    [Header("输入框")]
    public TMP_InputField IF_Name;
    public TMP_InputField IF_Mass;

    [Header("质量单位与范围提示")]
    public TextMeshProUGUI textMassUnit;    // MSun 或 MJup
    public TextMeshProUGUI textMassRange;   // "范围: 30 ~ 300 MSun"

    [Header("模式选择")]
    public Button buttonModeSelect;         // 主按钮，显示当前模式
    public TextMeshProUGUI textModeLabel;   // 主按钮上的文字
    public Button buttonHyperStar;          // 选项1：超大质量恒星
    public Button buttonBrownDwarf;         // 选项2：褐矮星

    [Header("当前模式（运行时）")]
    public OSE.OtherStarType currentType = OSE.OtherStarType.HyperStar;

    void Start() {
        // 主按钮：切换弹窗
        buttonModeSelect.onClick.AddListener(ToggleOptionButtons);

        // 两个选项按钮
        buttonHyperStar.onClick.AddListener(
            () => SelectMode(OSE.OtherStarType.HyperStar));
        buttonBrownDwarf.onClick.AddListener(
            () => SelectMode(OSE.OtherStarType.BrownDwarf));

        // 初始隐藏两个选项按钮
        buttonHyperStar.gameObject.SetActive(false);
        buttonBrownDwarf.gameObject.SetActive(false);

        // 应用默认模式
        ApplyType(OSE.OtherStarType.HyperStar);
    }

    void Update() {
        // 点击选项按钮之外的任意位置 → 关闭
        bool anyActive = buttonHyperStar.gameObject.activeSelf ||
                         buttonBrownDwarf.gameObject.activeSelf;
        if (anyActive && Input.GetMouseButtonDown(0)) {
            if (!IsPointerOver(buttonHyperStar.gameObject) &&
                !IsPointerOver(buttonBrownDwarf.gameObject) &&
                !IsPointerOver(buttonModeSelect.gameObject)) {
                HideOptions();
            }
        }
    }

    void ToggleOptionButtons() {
        bool anyActive = buttonHyperStar.gameObject.activeSelf ||
                         buttonBrownDwarf.gameObject.activeSelf;
        if (anyActive) HideOptions();
        else ShowOptions();
    }

    void ShowOptions() {
        buttonHyperStar.gameObject.SetActive(true);
        buttonBrownDwarf.gameObject.SetActive(true);
    }

    void HideOptions() {
        buttonHyperStar.gameObject.SetActive(false);
        buttonBrownDwarf.gameObject.SetActive(false);
    }

    void SelectMode(OSE.OtherStarType type) {
        ApplyType(type);
        HideOptions();
    }

    void ApplyType(OSE.OtherStarType type) {
        currentType = type;

        var (min, max) = OSE.GetMassRange(type);
        string unit = OSE.GetMassUnit(type);

        // 更新主按钮文字
        if (textModeLabel != null)
            textModeLabel.text = type == OSE.OtherStarType.HyperStar
                ? "超大质量恒星" : "褐矮星";

        // 更新单位与范围提示
        if (textMassUnit != null) textMassUnit.text = unit;
        if (textMassRange != null)
            textMassRange.text = $"范围: {min:G} ~ {max:G} {unit}";

        // 重置质量到范围中值
        if (IF_Mass != null)
            IF_Mass.text = ((min + max) / 2).ToString("G");

        // 重新绑定范围
        BindInputField(IF_Mass);
    }

    void BindInputField(TMP_InputField input) {
        input.onEndEdit.RemoveAllListeners();

        var (min, max) = OSE.GetMassRange(currentType);
        input.onEndEdit.AddListener((string value) => {
            OnEndEdit(value, min, max);
        });
    }

    void OnEndEdit(string value, double min, double max) {
        TMP_InputField current = EventSystem.current.currentSelectedGameObject
            ?.GetComponent<TMP_InputField>();

        if (double.TryParse(value, out double result)) {
            result = System.Math.Clamp(result, min, max);
            if (current != null) current.text = result.ToString("G");
        } else {
            if (current != null) current.text = min.ToString("G");
        }
    }

    // ~~~ 判断鼠标是否在某个 UI 上 ~~~ //
    bool IsPointerOver(GameObject go) {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            rt, Input.mousePosition, null);
    }

    // ~~~ 按钮回调 ~~~ //

    public void ButtonOnClick_Ret() {
        SceneManager.LoadScene("EvolutionChoose");
    }

    public void ButtonOnClick_ResetParameters() {
        var (min, max) = OSE.GetMassRange(currentType);
        IF_Name.text = "";
        IF_Mass.text = ((min + max) / 2).ToString("G");
        FloatingText.Show("已重置参数");
    }

    public void ButtonOnClick_Calculate() {
        OtherStar_Preparation.Name =
            string.IsNullOrEmpty(IF_Name.text) ? "天体" : IF_Name.text;
        OtherStar_Preparation.Type = currentType;
        OtherStar_Preparation.Mass = double.Parse(IF_Mass.text);

        SceneManager.LoadScene("OtherStarCalculationScene");
        FloatingText.Show("计算开始");
    }
}

public static class OtherStar_Preparation {
    public static string Name;
    public static OtherSimulationEngine.OtherStarType Type;
    public static double Mass;   // 单位取决于 Type
}