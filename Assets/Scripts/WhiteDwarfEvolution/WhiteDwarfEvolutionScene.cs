using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WDE = WhiteDwarfEvolutionEngine;

public class WhiteDwarfEvolutionScene : MonoBehaviour {
    public GameObject gameObjSidebar, buttonExpandSidebar,
                      gameObjHRD, buttonExpandHRD,
                      star, starHRD;
    public Material starMaterial;
    public Image starHRDImage;
    public TextMeshProUGUI textName, textYear,
                           textRadius, textTemperature,
                           textLuminosity, textSpectrum,
                           textYearProgress;

    [Header("提示文字 (可选)")]
    public TextMeshProUGUI textPauseTip;

    [Header("暂停特效")]
    public PauseScreenBorder pauseBorder;

    public (double x, double y)
           HRDStart = (-19.55, -30.0),
           HRDEnd = (620, 460);

    private const double TEMP_MAX = 100000.0;
    private const double TEMP_MIN = 1000.0;
    private const double LUM_MAX = 1e1;
    private const double LUM_MIN = 1e-8;

    public UnitButton unitRadius, unitTemperature, unitLuminosity;

    private string _wdSimFilePath;

    private readonly List<TrailPoint> _trailPoints = new();
    private readonly int _maxTrailPoints = 1000;
    private GameObject _trailContainer;
    private readonly float _trailPointSpacing = 0.5f;

    private bool _isPaused = false;
    private StarLightPoint _starLightPoint;
    private float _lastTapTime = 0f;
    private const float DOUBLE_TAP_TIME = 0.3f;

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

        WDE.Init(WD_Preparation.MassOrig);
        WDE.name = WD_Preparation.Name;
        WDE.WD_TexpoEnd = WD_Preparation.TexpoEnd;
        WDE.WD_StepTexpo = WD_Preparation.StepTexpo;
        WDE.WD_StepTime = WD_Preparation.StepTime;

        starMaterial = star.GetComponent<Renderer>().material;
        starHRDImage = starHRD.GetComponent<Image>();

        _starLightPoint = StarLightPoint.Attach(star, Camera.main);

        _wdSimFilePath = SimulationTextWriter.BeginWhiteDwarf(
            WD_Preparation.MassOrig,
            WDE.WD_Mass,
            WDE.WD_Radius,
            WD_Preparation.StepTexpo,
            WD_Preparation.StepTime,
            WDE.WD_Year / 1e6,
            System.Math.Pow(10, WD_Preparation.TexpoEnd) / 1e6,
            WD_Preparation.Name
        );

        if (textPauseTip != null) textPauseTip.gameObject.SetActive(false);
        if (pauseBorder != null) pauseBorder.SetVisible(false);

        StartCoroutine(Evolution());
    }

    void Update() {
        bool toggled = false;

        if (Input.GetKeyDown(KeyCode.Space)) {
            toggled = true;
        }

        if (Input.touchCount == 1) {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended) {
                if (Time.time - _lastTapTime < DOUBLE_TAP_TIME) {
                    toggled = true;
                    _lastTapTime = 0f;
                } else {
                    _lastTapTime = Time.time;
                }
            }
        }

        if (toggled) {
            _isPaused = !_isPaused;
            if (textPauseTip != null) textPauseTip.gameObject.SetActive(_isPaused);
            if (pauseBorder != null) pauseBorder.SetVisible(_isPaused);

            if (_isPaused) FloatingText.Show("演化已暂停");
            else FloatingText.Show("继续演化");
        }
    }

    public double GetRadius(double original) {
        string unit = unitRadius.GetUnit();
        if (unit == "RSun") {
            return original / WDE.rSun;
        } else if (unit == "REarth") {
            return original / WDE.rSun * (WDE.rSun / 6371.0);
        } else {
            return original;
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
            return WDE.lSun * original;
        }
    }

    public IEnumerator Evolution() {
        while (!WDE.IsFinished()) {
            while (_isPaused) {
                yield return null;
            }

            WDE.EvolveStep();

            textName.text = WDE.name;
            textYear.text = Utils.FormatNumber(WDE.WD_Year / 1e6);
            textRadius.text = Utils.FormatNumber(GetRadius(WDE.WD_Radius));
            textTemperature.text = Utils.FormatNumber(GetTemperature(WDE.WD_TemperatureSurf));
            textLuminosity.text = Utils.FormatNumber(GetLuminosity(WDE.WD_Luminosity));
            textSpectrum.text = WDE.WD_Spectrum.ToString();

            double progress = (WDE.WD_Texpo - 7.0) / (WDE.WD_TexpoEnd - 7.0) * 100.0;
            textYearProgress.text = $"({progress.ToString("F1")}%)";

            SimulationTextWriter.AppendWhiteDwarfStep(
                _wdSimFilePath,
                WDE.WD_Year / 1e6,
                WDE.WD_TemperatureSurf,
                WDE.WD_Luminosity,
                WDE.WD_Radius
            );

            var (starR, starG, starB) = Spectrum.GetBlackbodyColor(WDE.WD_TemperatureSurf);
            starMaterial.SetColor("_BaseColor", new Color(starR, starG, starB, 1));
            starMaterial.SetColor("_CellColor", new Color(starR * 1.38f, starG * 1.38f, starB * 1.38f, 1));
            starMaterial.SetFloat(
                "_EmissionStrength",
                Utils.GetStarEmissionStrength(WDE.WD_TemperatureSurf / WDE.tSunSurf));

            float displayScale = (float)(WDE.WD_Radius / 700000.0) * 15f;
            star.transform.localScale = new(displayScale, displayScale, displayScale);

            _starLightPoint?.SetStar(WDE.WD_Luminosity, WDE.WD_TemperatureSurf);

            UpdateHRD();

            yield return new WaitForSeconds((float)WDE.WD_StepTime);
        }

        SceneManager.LoadScene("WhiteDwarfEvolutionPreparationScene");
        FloatingText.Show("冷却完成");
    }

    public void UpdateHRD() {
        float hrdx = (float)Utils.MapLinear(TEMP_MAX, TEMP_MIN, HRDStart.x, HRDEnd.x, WDE.WD_TemperatureSurf);
        float hrdy = (float)Utils.MapLog(LUM_MIN, LUM_MAX, HRDStart.y, HRDEnd.y, WDE.WD_Luminosity);

        RectTransform rect = starHRD.GetComponent<RectTransform>();
        Vector2 pos = new(hrdx, hrdy);
        rect.anchoredPosition = pos;

        var (starR, starG, starB) = Spectrum.GetBlackbodyColor(WDE.WD_TemperatureSurf);
        Color starColor = new(starR, starG, starB);
        starHRDImage.color = starColor;

        AddTrailPoint(pos, starColor);
    }

    private void AddTrailPoint(Vector2 anchoredPos, Color color) {
        if (_trailPoints.Count > 0) {
            float distance = Vector2.Distance(_trailPoints[^1].position, anchoredPos);
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
        SceneManager.LoadScene("WhiteDwarfEvolutionPreparationScene");
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