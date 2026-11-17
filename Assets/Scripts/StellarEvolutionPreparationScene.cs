using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StellarEvolutionPreparationScene : MonoBehaviour {
    public TMP_InputField IF_Name, IF_Mass, IF_Metal,
                          IF_Start, IF_End, IF_Step, IF_StepTime; // 新增 IF_Start

    [Header("保存演化数据")]
    public Toggle toggleSaveData;

    void Start() {
        BindInputField(IF_Mass, "IF_Mass", (float)StellarEvolutionEngine.MassMin,
                       (float)StellarEvolutionEngine.MassMax);
        BindInputField(IF_Metal, "IF_Metal", 0.001f, 0.03f);

        // 起点范围：0 到 12000 Myr
        BindInputField(IF_Start, "IF_Start", 0f, 12000f);

        BindInputField(IF_End, "IF_End", 0.001f, 12000f);
        BindInputField(IF_Step, "IF_Step", 0.0001f, 12000f);
        BindInputField(IF_StepTime, "IF_StepTime", 0.001f, 1000f);

        ApplyNewModelDefaults();

        if (toggleSaveData != null) {
            toggleSaveData.isOn = true;
            toggleSaveData.onValueChanged.AddListener(OnSaveDataChanged);
            OnSaveDataChanged(true);
        }
    }

    void OnSaveDataChanged(bool value) {
        SimulationTextWriter.Enabled = value;
    }

    void BindInputField(TMP_InputField input, string tag, float min, float max) {
        if (input == null) return; // 防止没挂
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener((string value) => {
            OnEndEdit(value, tag, min, max);
        });
    }

    void OnEndEdit(string value, string tag, float min, float max) {
        if (float.TryParse(value, out float result)) {
            result = Mathf.Clamp(result, min, max);
            if (EventSystem.current.currentSelectedGameObject.TryGetComponent<TMP_InputField>(out var input)) {
                input.text = result.ToString();
            }
        } else {
            if (EventSystem.current.currentSelectedGameObject.TryGetComponent<TMP_InputField>(out var input)) {
                input.text = min.ToString();
            }
        }

        // 如果改的是质量/金属度，重新计算总寿命，限制终点
        if (tag == "IF_Mass" || tag == "IF_Metal") {
            double mass = double.Parse(IF_Mass.text);
            double metallicity = double.Parse(IF_Metal.text);
            double lifespan = StellarEvolutionEngine.GetLifespan(mass, metallicity);
            double step = StellarEvolutionEngine.GetDefaultStep(mass, metallicity);

            BindInputField(IF_End, "IF_End", 0, (float)lifespan);
            BindInputField(IF_Step, "IF_Step", 0, (float)lifespan);
            if (double.Parse(IF_End.text) > lifespan) {
                IF_End.text = lifespan.ToString("G6");
            }
            if (double.Parse(IF_Step.text) > lifespan || double.Parse(IF_Step.text) <= 0) {
                IF_Step.text = step.ToString("G6");
            }
        }

        // 如果改的是起点，确保终点 > 起点
        if (tag == "IF_Start" || tag == "IF_End") {
            double start = double.Parse(IF_Start.text);
            double end = double.Parse(IF_End.text);
            if (end <= start) {
                IF_End.text = (start + 10).ToString("G6"); // 强行加 10 Myr
            }
        }
    }

    public void ButtonOnClick_Ret() {
        SceneManager.LoadScene("EvolutionChoose");
    }

    public void ButtonOnClick_ResetParameters() {
        IF_Name.text = "";
        IF_Mass.text = "1.0";
        IF_Metal.text = "0.02";
        IF_Start.text = "0"; // 默认 0
        IF_End.text = "12000";
        IF_Step.text = "10";
        IF_StepTime.text = "0.2";
        FloatingText.Show("已重置参数");
    }

    void ApplyNewModelDefaults() {
        if (!double.TryParse(IF_Mass.text, out double mass)) mass = 1.0;
        if (!double.TryParse(IF_Metal.text, out double metallicity)) metallicity = 0.02;

        if (IF_Start != null && string.IsNullOrEmpty(IF_Start.text)) {
            IF_Start.text = "0";
        }

        double lifespan = StellarEvolutionEngine.GetLifespan(mass, metallicity);
        double step = StellarEvolutionEngine.GetDefaultStep(mass, metallicity);
        BindInputField(IF_End, "IF_End", 0, (float)lifespan);
        BindInputField(IF_Step, "IF_Step", 0, (float)lifespan);
        IF_End.text = lifespan.ToString("G6");
        IF_Step.text = step.ToString("G6");
    }

    public void ButtonOnClick_SimToEnd() {
        ApplyNewModelDefaults();
        FloatingText.Show($"已演化到终点：寿命 {IF_End.text} Myr，步长 {IF_Step.text} Myr");
    }

    public void ButtonOnClick_Start() {
        StellarEvolutionEngine.name = IF_Name.text == "" ? "恒星" : IF_Name.text;
        StellarEvolutionEngine.SE_Mass = double.Parse(IF_Mass.text);
        StellarEvolutionEngine.SE_Metal = double.Parse(IF_Metal.text);

        // 保存起点
        StellarEvolutionEngine.SE_Start = double.Parse(IF_Start.text);

        StellarEvolutionEngine.SE_End = double.Parse(IF_End.text);
        StellarEvolutionEngine.SE_Step = double.Parse(IF_Step.text);
        StellarEvolutionEngine.SE_StepTime = double.Parse(IF_StepTime.text);
        SceneManager.LoadScene("StellarEvolutionScene");
        FloatingText.Show("演化开始");
    }
}