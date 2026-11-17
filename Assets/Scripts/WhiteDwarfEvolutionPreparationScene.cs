using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WhiteDwarfEvolutionPreparationScene : MonoBehaviour {
    public TMP_InputField IF_Name, IF_Mass,
                          IF_End, IF_Step, IF_StepTime;

        [Header("暂停特效")]
    public PauseScreenBorder pauseBorder;

    [Header("保存演化数据")]
    public Toggle toggleSaveData;

    // 默认值
    private const string DEF_NAME = "";
    private const string DEF_MASS = "1.0";
    private const string DEF_END = "10000000";   // MYR (= 10^13 年)
    private const string DEF_STEPTIME = "0.01";  // s

    // 范围
    private const double MASS_MIN = WhiteDwarfEvolutionEngine.MassMin;  // 0.077
    private const double MASS_MAX = WhiteDwarfEvolutionEngine.MassMax;  // 100 M☉
    private const double TEXPO_MIN = WhiteDwarfEvolutionEngine.TexpoStart; // 10^7 年
    private const double TEXPO_MAX = 13.0;       // 10^13 年
    private const double STEP_MIN = 0.001;
    private const double STEP_MAX = 0.99;
    private const double STEPTIME_MIN = 0.0;

    // 默认步长 = 冷却寿命 / 1000
    private static string DefaultStep => WhiteDwarfEvolutionEngine
        .GetDefaultStepTexpo(TEXPO_MAX).ToString("G");

    void Start() {
        // 绑定输入框
        BindInputField(IF_Mass, "IF_Mass");
        BindInputField(IF_End, "IF_End");
        BindInputField(IF_Step, "IF_Step");
        BindInputField(IF_StepTime, "IF_StepTime");
        // IF_Name 不绑定，自由文本

        // 设置初始值
        ResetParameters();

        // 保存演化数据开关
        if (toggleSaveData != null) {
            toggleSaveData.isOn = true;
            toggleSaveData.onValueChanged.AddListener(OnSaveDataChanged);
            OnSaveDataChanged(true);
        }
    }

    void OnSaveDataChanged(bool value) {
        SimulationTextWriter.Enabled = value;
    }

    void BindInputField(TMP_InputField input, string tag) {
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener((string value) => {
            OnEndEdit(value, tag);
        });
    }

    void OnEndEdit(string value, string tag) {
        switch (tag) {
            case "IF_Mass": {
                    if (double.TryParse(value, out double mass)) {
                        mass = System.Math.Clamp(mass, MASS_MIN, MASS_MAX);
                        IF_Mass.text = mass.ToString("G");
                    } else {
                        IF_Mass.text = DEF_MASS;
                    }
                    break;
                }
            case "IF_End": {
                    if (double.TryParse(value, out double endMyr)) {
                        // MYR → 年 → texpo
                        double endYear = endMyr * 1e6;
                        double texpo = System.Math.Log10(endYear);
                        texpo = System.Math.Clamp(texpo, TEXPO_MIN, TEXPO_MAX);
                        // 回显成 MYR
                        double finalMyr = System.Math.Pow(10, texpo) / 1e6;
                        IF_End.text = finalMyr.ToString("G");
                    } else {
                        IF_End.text = DEF_END;
                    }
                    break;
                }
            case "IF_Step": {
                    if (double.TryParse(value, out double step)) {
                        step = System.Math.Clamp(step, STEP_MIN, STEP_MAX);
                        IF_Step.text = step.ToString("G");
                    } else {
                        IF_Step.text = DefaultStep;
                    }
                    break;
                }
            case "IF_StepTime": {
                    if (double.TryParse(value, out double st)) {
                        st = System.Math.Max(st, STEPTIME_MIN);
                        IF_StepTime.text = st.ToString("G");
                    } else {
                        IF_StepTime.text = DEF_STEPTIME;
                    }
                    break;
                }
        }
    }

    public void ButtonOnClick_Ret() {
        SceneManager.LoadScene("EvolutionChoose");
    }

    public void ButtonOnClick_ResetParameters() {
        ResetParameters();
        FloatingText.Show("已重置参数");
    }

    void ResetParameters() {
        IF_Name.text = DEF_NAME;
        IF_Mass.text = DEF_MASS;
        IF_End.text = DEF_END;
        IF_Step.text = DefaultStep;
        IF_StepTime.text = DEF_STEPTIME;
    }

    public void ButtonOnClick_SimToEnd() {
        // 演化到终点：主动写入新模型计算的寿命与步长
        // 寿命 = 10^TEXPO_MAX 年；步长 = 寿命 / 1000（对数步长 = (texpoEnd - 7) / 1000）
        double texpoEnd = TEXPO_MAX;
        double endMyr = WhiteDwarfEvolutionEngine.GetCoolingLifespanYear(texpoEnd) / 1e6;
        double step = WhiteDwarfEvolutionEngine.GetDefaultStepTexpo(texpoEnd);

        IF_End.text = endMyr.ToString("G");
        IF_Step.text = step.ToString("G");
        FloatingText.Show($"已设置到演化终点：寿命 {endMyr:G4} Myr，步长 {step:G4} dex");
    }

    public void ButtonOnClick_Start() {
        // 存参数到静态中转类
        WD_Preparation.Name = string.IsNullOrEmpty(IF_Name.text) ? "白矮星" : IF_Name.text;
        WD_Preparation.MassOrig = double.Parse(IF_Mass.text);

        double endMyr = double.Parse(IF_End.text);
        double endYear = endMyr * 1e6;
        WD_Preparation.TexpoEnd = System.Math.Clamp(
            System.Math.Log10(endYear), TEXPO_MIN, TEXPO_MAX);

        WD_Preparation.StepTexpo = double.Parse(IF_Step.text);
        WD_Preparation.StepTime = double.Parse(IF_StepTime.text);

        SceneManager.LoadScene("WhiteDwarfEvolutionScene");
        FloatingText.Show("冷却开始");
    }
}

public static class WD_Preparation {
    public static string Name;
    public static double MassOrig;    // 原恒星质量 M☉
    public static double TexpoEnd;    // log10(终点年龄/年)
    public static double StepTexpo;   // 对数步长 dex
    public static double StepTime;    // 每步现实秒数
}