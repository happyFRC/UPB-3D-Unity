using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StellarEvolutionPreparationScene : MonoBehaviour {
    public TMP_InputField IF_Name, IF_Mass, IF_Metal,
                          IF_End, IF_Step, IF_StepTime;

    void Start() {
        BindInputField(IF_Mass, "IF_Mass", 0.077f, 30f);
        BindInputField(IF_Metal, "IF_Metal", 0.001f, 0.03f);
        BindInputField(IF_End, "IF_End", 0.001f, 12000f);
        BindInputField(IF_Step, "IF_Step", 0.001f, 12000f);
        BindInputField(IF_StepTime, "IF_StepTime", 0.001f, 1000f);
    }

    void BindInputField(TMP_InputField input, string tag, float min, float max) {
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
        if (tag == "IF_Mass" || tag == "IF_Metal") {
            double mass = double.Parse(IF_Mass.text);
            double metallicity = double.Parse(IF_Metal.text);
            double MSLifespan = StellarEvolutionEngine.GetMainSequenceLifespan(mass, metallicity);
            double RGLifespan = StellarEvolutionEngine.GetRedGiantLifespan(mass, metallicity);
            double lifespan = MSLifespan + RGLifespan;
            BindInputField(IF_End, "IF_End", 0, (float)lifespan);
            BindInputField(IF_Step, "IF_Step", 0, (float)lifespan);
            if (double.Parse(IF_End.text) > lifespan) {
                IF_End.text = lifespan.ToString();
            }
            if (double.Parse(IF_Step.text) > lifespan) {
                IF_Step.text = (lifespan / 500.0).ToString();
            }
        }
    }

    public void ButtonOnClick_Ret() {
        SceneManager.LoadScene("MainMenu");
    }

    public void ButtonOnClick_ResetParameters() {
        IF_Name.text = "";
        IF_Mass.text = "1.0";
        IF_Metal.text = "0.02";
        IF_End.text = "4540";
        IF_Step.text = "20";
        IF_StepTime.text = "0.2";
        FloatingText.Show("已重置参数");
    }

    public void ButtonOnClick_SimToEnd() {
        double mass = double.Parse(IF_Mass.text);
        double metallicity = double.Parse(IF_Metal.text);
        double MSLifespan = StellarEvolutionEngine.GetMainSequenceLifespan(mass, metallicity);
        double RGLifespan = StellarEvolutionEngine.GetRedGiantLifespan(mass, metallicity);
        double lifespan = MSLifespan + RGLifespan;
        IF_End.text = lifespan.ToString();
        IF_Step.text = (lifespan * 0.002).ToString();
        FloatingText.Show("已演化到终点");
    }

    public void ButtonOnClick_Start() {
        StellarEvolutionEngine.name = IF_Name.text == "" ? "恒星" : IF_Name.text;
        StellarEvolutionEngine.SE_Mass = double.Parse(IF_Mass.text);
        StellarEvolutionEngine.SE_Metal = double.Parse(IF_Metal.text);
        StellarEvolutionEngine.SE_End = double.Parse(IF_End.text);
        StellarEvolutionEngine.SE_Step = double.Parse(IF_Step.text);
        StellarEvolutionEngine.SE_StepTime = double.Parse(IF_StepTime.text);
        SceneManager.LoadScene("StellarEvolutionScene");
    }
}
