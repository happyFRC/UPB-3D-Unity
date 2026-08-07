using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SEE = StellarEvolutionEngine;

public class StellarEvolutionScene : MonoBehaviour {
    public GameObject gameObjSidebar, buttonExpandSidebar,
                      gameObjHRD, buttonExpandHRD,
                      star, starHRD;
    public Material starMaterial;
    public Image starHRDImage;
    public TextMeshProUGUI textName, textYear,
                           textRadius, textTemperature,
                           textLuminosity, textSpectrum,

                           textYearProgress;

    public (double x, double y)
           HRDStart = (-19.55, -30.0),
           HRDEnd = (620, 460);

    public UnitButton unitRadius, unitTemperature, unitLuminosity;


    private readonly List<TrailPoint> _trailPoints = new();
    private readonly int _maxTrailPoints = 1000;
    private GameObject _trailContainer;
    private readonly float _trailPointSpacing = 0.5f;

    public void Start() {
        _trailContainer = new GameObject("TrailContainer");
        _trailContainer.transform.SetParent(starHRD.transform.parent);
        _trailContainer.transform.localPosition = Vector3.zero;
        _trailContainer.transform.localScale = Vector3.one;

        RectTransform containerRect = _trailContainer.AddComponent<RectTransform>();
        RectTransform starRect = starHRD.GetComponent<RectTransform>();
        containerRect.anchorMin = starRect.anchorMin;
        containerRect.anchorMax = starRect.anchorMax;
        containerRect.pivot = starRect.pivot;
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = starRect.sizeDelta;

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
        starMaterial = star.GetComponent<Renderer>().material;
        starHRDImage = starHRD.GetComponent<Image>();
        StartCoroutine(Evolution());
    }

    public double GetRadius(double original) {
        string unit = unitRadius.GetUnit();
        if (unit == "RSun") {
            return original;
        } else if (unit == "REarth") {
            return SEE.rSun * original / SEE.rEarth;
        } else {
            return SEE.rSun * original;
        }
    }

    public double GetTemperature(double original) {
        string unit = unitTemperature.GetUnit();
        if (unit == "K") {
            return original;
        } else if (unit == "°C") {
            return original - 273.15;
        } else {
            return (original - 273.15) * (9.0 / 5.0) + 32;
        }
    }

    public double GetLuminosity(double original) {
        string unit = unitLuminosity.GetUnit();
        if (unit == "LSun") {
            return original;
        } else {
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

            starMaterial.SetColor(
                "_BaseColor",
                new Color(
                    starR,
                    starG,
                    starB,
                    1
                )
            );

            starMaterial.SetColor(
                "_CellColor",
                new Color(
                    starR * 1.38f,
                    starG * 1.38f,
                    starB * 1.38f,
                    1
                )
            );

            starMaterial.SetFloat(
                "_EmissionStrength",
                Utils.GetStarEmissionStrength(SEE.SE_TemperatureSurf / SEE.tSunSurf)
            );


            star.transform.localScale = new(
                (float)SEE.SE_Radius * 15f,
                (float)SEE.SE_Radius * 15f,
                (float)SEE.SE_Radius * 15f
            );

            UpdateHRD();

            yield return new WaitForSeconds((float)SEE.SE_StepTime);
        }

        SceneManager.LoadScene("StellarEvolutionPreparationScene");
        FloatingText.Show("演化完成");
    }

    public void UpdateHRD() {
        float hrdx = (float)Utils.MapLinear(30000, 1000, HRDStart.x, HRDEnd.x, SEE.SE_TemperatureSurf);
        float hrdy = (float)Utils.MapLog(1e-5, 1e6, HRDStart.y, HRDEnd.y, SEE.SE_Luminosity);

        RectTransform rect = starHRD.GetComponent<RectTransform>();
        Vector2 pos = new(hrdx, hrdy);
        rect.anchoredPosition = pos;

        var (starR, starG, starB) = Spectrum.GetBlackbodyColor(SEE.SE_TemperatureSurf);
        Color starColor = new(starR, starG, starB);
        starHRDImage.color = starColor;

        AddTrailPoint(pos, starColor);
    }

    private void AddTrailPoint(Vector2 anchoredPos, Color color) {
        if (_trailPoints.Count > 0) {
            float distance = Vector2.Distance(_trailPoints[_trailPoints.Count - 1].position, anchoredPos);
            if (distance < _trailPointSpacing) {
                return;
            }
        }

        TrailPoint newPoint = new() { position = anchoredPos, color = color };
        _trailPoints.Add(newPoint);

        if (_trailPoints.Count > _maxTrailPoints) {
            _trailPoints.RemoveAt(0);
        }

        UpdateTrailRenderer();
    }

    private void UpdateTrailRenderer() {
        foreach (Transform child in _trailContainer.transform) {
            Destroy(child.gameObject);
        }

        if (_trailPoints.Count < 2) {
            return;
        }

        GameObject lineObj = new("Line");
        lineObj.transform.SetParent(_trailContainer.transform);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localScale = Vector3.one;

        RectTransform rect = lineObj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);

        UILineRenderer line = lineObj.AddComponent<UILineRenderer>();

        line.useGradient = true;
        line.pointColors = new List<Color>();
        foreach (var point in _trailPoints) {
            line.pointColors.Add(point.color);
        }
        line.lineThickness = 10f;

        List<Vector2> uiPoints = new();
        for (int i = 0; i < _trailPoints.Count; i++) {
            uiPoints.Add(_trailPoints[i].position);
        }

        line.points = uiPoints;
    }

    public void ClearTrail() {
        _trailPoints.Clear();
        UpdateTrailRenderer();
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

    public void ButtonOnClick_ExpandHRD() {
        buttonExpandHRD.SetActive(false);
        gameObjHRD.SetActive(true);
    }

    public void ButtonOnClick_TakebackHRD() {
        buttonExpandHRD.SetActive(true);
        gameObjHRD.SetActive(false);
    }
}


public class TrailPoint {
    public Vector2 position;
    public Color color;
}