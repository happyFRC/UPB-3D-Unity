using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SEE = StellarEvolutionEngine;

public class StellarEvolutionScene : MonoBehaviour {
    public GameObject gameObjSidebar, buttonExpandSidebar, star;
    public Renderer starRenderer;
    public TextMeshProUGUI textName, textYear,
                           textRadius, textTemperature,
                           textLuminosity, textSpectrum,

                           textYearProgress;

    public UnitButton unitRadius, unitTemperature, unitLuminosity;

    public void Start() {
        SEE.SE_MassCore = SEEHelper.GetMassCore0(SEE.SE_Mass, SEE.SE_Metal);
        SEE.SE_Radius = SEEHelper.GetRadius0(SEE.SE_Mass, SEE.SE_Metal);
        SEE.SE_Year = 0.0;
        SEE.SE_TemperatureCore = SEEHelper.GetTemperatureCore0(SEE.SE_Mass);
        SEE.SE_TemperatureSurf = SEEHelper.GetTemperatureSurf0(SEE.SE_Mass, SEE.SE_Metal);
        SEE.SE_Luminosity = SEEHelper.GetLuminosity0(SEE.SE_Mass, SEE.SE_Metal);
        SEE.SE_Spectrum = new Spectrum("M", 0, "I");
        SEE.SE_X_H = 0.5697;
        SEE.SE_X_He = 1.0 - SEE.SE_X_H;
        SEE.SE_AvgMolecularWeight = 1 / (2 * SEE.SE_X_H + 0.75 * SEE.SE_X_He);
        SEE.SE_K = SEEHelper.GetK(SEE.SE_Mass, SEE.SE_Metal);
        SEE.SE_Tau = SEE.GetMainSequenceLifespan(SEE.SE_Mass, SEE.SE_Metal);
        (SEE.SE_PP, SEE.SE_CNO) = SEEHelper.GetPP0CNO0(SEE.SE_Mass, SEE.SE_Metal);
        SEE.SETAMS_Luminosity = 0;
        SEE.SETAMS_Radius = 0;
        SEE.SE_LuminosityCrit = SEEHelper.GetLuminosityCrit(SEE.SE_Mass, SEE.SE_Metal);
        SEE.SE_RadiusCrit = SEEHelper.GetRadiusCrit(SEE.SE_Mass, SEE.SE_Metal);
        starRenderer = star.GetComponent<Renderer>();
        StartCoroutine(Evolution());
    }

    public double GetRadius(double original) {
        string unit = unitRadius.GetUnit();
        if (unit == "RSun") {
            return original;
        } else if (unit == "REarth") {
            return SEE.rSun * original / SEE.rEarth;
        } else /*km*/ {
            return SEE.rSun * original;
        }
    }

    public double GetTemperature(double original) {
        string unit = unitTemperature.GetUnit();
        if (unit == "K") {
            return original;
        } else if (unit == "°C") {
            return original - 273.15;
        } else /* °F */ {
            return (original - 273.15) * (9.0 / 5.0) + 32;
        }
    }

    public double GetLuminosity(double original) {
        string unit = unitLuminosity.GetUnit();
        if (unit == "LSun") {
            return original;
        } else /* W */ {
            return SEE.lSun * original;
        }
    }

    public IEnumerator Evolution() {
        while (true) {
            if (SEE.SE_Year + SEE.SE_Step > SEE.SE_End || SEE.SE_Step == 0) {
                break;
            }

            SEE.EvolveStep();

            textName.text = SEE.name;
            textYear.text = Utils.FormatNumber(SEE.SE_Year);
            textRadius.text = Utils.FormatNumber(GetRadius(SEE.SE_Radius));
            textTemperature.text = Utils.FormatNumber(GetTemperature(SEE.SE_TemperatureSurf));
            textLuminosity.text = Utils.FormatNumber(GetLuminosity(SEE.SE_Luminosity));
            textSpectrum.text = SEE.SE_Spectrum.ToString();

            textYearProgress.text = $"({(SEE.SE_Year / SEE.SE_End * 100).ToString("F1")}%)";

            var (starR, starG, starB) = Spectrum.GetBlackbodyColor(SEE.SE_TemperatureSurf);
            starRenderer.material.color = new(starR, starG, starB);

            yield return new WaitForSeconds((float)SEE.SE_StepTime);
        }

        SceneManager.LoadScene("StellarEvolutionPreparationScene");
        FloatingText.Show("演化完成");
    }

    public void ButtonOnClick_Ret() {
        SceneManager.LoadScene("StellarEvolutionPreparationScene");
    }

    public void ButtonOnClick_ExpandSidebar() {
        buttonExpandSidebar.SetActive(false);
        gameObjSidebar.SetActive(true);
    }

    public void ButtonOnClick_TakebackSidebar() {
        buttonExpandSidebar.SetActive(true);
        gameObjSidebar.SetActive(false);
    }
}
