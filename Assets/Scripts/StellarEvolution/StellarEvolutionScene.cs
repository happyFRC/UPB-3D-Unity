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

    [Header("暂停特效")]
    public PauseScreenBorder pauseBorder;

    [Header("宜居带 / 轨道显示")]
    public HabitableZoneRenderer habitableZone;
    public PlanetaryOrbitRenderer orbitRenderer;
    public Toggle toggleShowHabitableZone;
    public Toggle toggleShowOrbits;

    [Header("提示文字 (可选)")]
    public TextMeshProUGUI textPauseTip; // 拖一个文本，用来提示“已暂停”

    private string _simFilePath;
    private bool _isPaused = false;
    [HideInInspector] public bool inputBlocked;
    private StarLightPoint _starLightPoint;

    private readonly List<TrailPoint> _trailPoints = new();
    private readonly int _maxTrailPoints = 1000;
    private GameObject _trailContainer;
    private readonly float _trailPointSpacing = 0.5f;

    private bool _orbitsVisibleFallback = false;
    private bool _hzVisibleFallback = false;

    // 触摸双击检测
    private float _lastTapTime = 0f;
    private const float DOUBLE_TAP_TIME = 0.3f;

    public void Start() {
        // 沙盘场景是从演化场景复制出来的，没有 starHRD 等旧 UI。缺引用时直接跳过，避免空引用。
        if (starHRD == null || star == null) {
            Debug.LogWarning("StellarEvolutionScene: star 或 starHRD 未赋值，已跳过演化初始化");
            enabled = false;
            return;
        }
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

        SEE.InitSSE();
        if (SEE.SSE_Track == null) {
            FloatingText.Show("参数超出 SSE 适用范围（0.077~100 MSun, Z 1e-4~0.03）");
            SceneManager.LoadScene("StellarEvolutionPreparationScene");
            return;
        }
        starMaterial = star.GetComponent<Renderer>().material;
        starHRDImage = starHRD.GetComponent<Image>();

        _starLightPoint = StarLightPoint.Attach(star, Camera.main);
        _starLightPoint?.SetStar(SEE.SE_Luminosity, SEE.SE_TemperatureSurf);

        _simFilePath = SimulationTextWriter.BeginMainSequence(
            SEE.SE_Mass,
            SEE.SE_Metal,
            SEE.SE_Start,
            SEE.SE_End,
            SEE.SE_Step,
            SEE.SE_Tau,
            SEE.name
        );

        InitToggles();

        if (textPauseTip != null) textPauseTip.gameObject.SetActive(false);

        StartCoroutine(Evolution());
    }

    void InitToggles() {
        if (toggleShowHabitableZone != null) {
            toggleShowHabitableZone.onValueChanged.RemoveAllListeners();
            toggleShowHabitableZone.isOn = false;
            toggleShowHabitableZone.onValueChanged.AddListener(OnHabitableZoneToggle);
            OnHabitableZoneToggle(false);
        } else if (habitableZone != null) {
            habitableZone.SetVisible(false);
        }

        if (toggleShowOrbits != null) {
            if (orbitRenderer != null) {
                orbitRenderer.toggleShowOrbits = toggleShowOrbits;
                toggleShowOrbits.isOn = false;
                orbitRenderer.SetVisible(false);
            } else {
                toggleShowOrbits.onValueChanged.RemoveAllListeners();
                toggleShowOrbits.isOn = false;
                toggleShowOrbits.onValueChanged.AddListener(v => _orbitsVisibleFallback = v);
            }
        } else if (orbitRenderer != null) {
            orbitRenderer.SetVisible(false);
        }
    }

    void OnHabitableZoneToggle(bool v) {
        if (habitableZone != null) habitableZone.SetVisible(v);
        _hzVisibleFallback = v;
    }

    void Update() {
        if (inputBlocked) return;

        // H：切换宜居带
        if (Input.GetKeyDown(KeyCode.H)) {
            if (toggleShowHabitableZone != null) toggleShowHabitableZone.isOn = !toggleShowHabitableZone.isOn;
            else if (habitableZone != null) { _hzVisibleFallback = !_hzVisibleFallback; habitableZone.SetVisible(_hzVisibleFallback); }
        }

        // O：切换行星轨道
        if (Input.GetKeyDown(KeyCode.O)) {
            if (toggleShowOrbits != null) toggleShowOrbits.isOn = !toggleShowOrbits.isOn;
            else if (orbitRenderer != null) { _orbitsVisibleFallback = !_orbitsVisibleFallback; orbitRenderer.SetVisible(_orbitsVisibleFallback); }
        }

        // 空格 / Android 双击：切换暂停
        HandlePauseInput();
    }

    void HandlePauseInput() {
        bool toggled = false;

        // PC端空格
        if (Input.GetKeyDown(KeyCode.Space)) {
            toggled = true;
        }

        // Android端双击（检测触摸抬起）
        if (Input.touchCount == 1) {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended) {
                if (Time.time - _lastTapTime < DOUBLE_TAP_TIME) {
                    toggled = true;
                    _lastTapTime = 0f; // 重置，防止三击连发
                } else {
                    _lastTapTime = Time.time;
                }
            }
        }

        if (toggled) {
            _isPaused = !_isPaused;
            if (textPauseTip != null) textPauseTip.gameObject.SetActive(_isPaused);
            if (pauseBorder != null) pauseBorder.SetVisible(_isPaused); // <--- 这行

            if (_isPaused) FloatingText.Show("演化已暂停");
            else FloatingText.Show("继续演化");
        }
    }

    public double GetRadius(double original) {
        string unit = unitRadius.GetUnit();
        if (unit == "RSun") return original;
        else if (unit == "REarth") return SEE.rSun * original / SEE.rEarth;
        else return SEE.rSun * original;
    }

    public double GetTemperature(double original) {
        string unit = unitTemperature.GetUnit();
        if (unit == "K") return original;
        else if (unit == "C" || unit == "°C") return original - 273.15;
        else return (original - 273.15) * (9.0 / 5.0) + 32;
    }

    public double GetLuminosity(double original) {
        string unit = unitLuminosity.GetUnit();
        if (unit == "LSun") return original;
        else return SEE.lSun * original;
    }

    public IEnumerator Evolution() {
        while (true) {
            while (_isPaused) {
                yield return null;
            }

            if (!SEE.HasNextStep()) break;

            SEE.EvolveStep();

            textName.text = SEE.name;
            textYear.text = Utils.FormatNumber(SEE.SE_Year);
            textRadius.text = Utils.FormatNumber(GetRadius(SEE.SE_Radius));
            textTemperature.text = Utils.FormatNumber(GetTemperature(SEE.SE_TemperatureSurf));
            textLuminosity.text = Utils.FormatNumber(GetLuminosity(SEE.SE_Luminosity));
            textSpectrum.text = SEE.SE_Spectrum.ToString();
            textYearProgress.text = $"({(SEE.SE_Year / SEE.SE_End * 100).ToString("F1")}%)";

            SimulationTextWriter.AppendMainSequenceStep(
                _simFilePath, SEE.SE_Year, SEE.SE_Spectrum.ToString(),
                SEE.SE_Luminosity, SEE.SE_Radius, SEE.SE_TemperatureSurf);

            var (starR, starG, starB) = Spectrum.GetBlackbodyColor(SEE.SE_TemperatureSurf);
            starMaterial.SetColor("_BaseColor", new Color(starR, starG, starB, 1));
            starMaterial.SetColor("_CellColor", new Color(starR * 1.38f, starG * 1.38f, starB * 1.38f, 1));
            starMaterial.SetFloat("_EmissionStrength", Utils.GetStarEmissionStrength(SEE.SE_TemperatureSurf / SEE.tSunSurf));

            star.transform.localScale = new((float)SEE.SE_Radius * 15f, (float)SEE.SE_Radius * 15f, (float)SEE.SE_Radius * 15f);

            _starLightPoint?.SetStar(SEE.SE_Luminosity, SEE.SE_TemperatureSurf);

            UpdateHRD();

            if (habitableZone != null) habitableZone.UpdateLuminosity(SEE.SE_Luminosity);

            yield return new WaitForSeconds((float)SEE.SE_StepTime);
        }

        SimulationTextWriter.EndMainSequence(
            _simFilePath, SEE.SE_StepCount, SEE.SE_Spectrum.ToString(),
            SEE.SE_Year, SEE.SE_Luminosity, SEE.SE_Radius, SEE.SE_TemperatureSurf);

        SceneManager.LoadScene("StellarEvolutionPreparationScene");
        FloatingText.Show("演化完成");
    }

    public void UpdateHRD() {
        float hrdx = (float)Utils.MapLinear(60000, 1000, HRDStart.x, HRDEnd.x, SEE.SE_TemperatureSurf);
        float hrdy = (float)Utils.MapLog(1e-5, 1e7, HRDStart.y, HRDEnd.y, SEE.SE_Luminosity);

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
            if (distance < _trailPointSpacing) return;
        }

        _trailPoints.Add(new TrailPoint { position = anchoredPos, color = color });
        if (_trailPoints.Count > _maxTrailPoints) _trailPoints.RemoveAt(0);

        UpdateTrailRenderer();
    }

    private void UpdateTrailRenderer() {
        foreach (Transform child in _trailContainer.transform) Destroy(child.gameObject);
        if (_trailPoints.Count < 2) return;

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
        foreach (var point in _trailPoints) line.pointColors.Add(point.color);
        line.lineThickness = 10f;

        List<Vector2> uiPoints = new();
        for (int i = 0; i < _trailPoints.Count; i++) uiPoints.Add(_trailPoints[i].position);
        line.points = uiPoints;
    }

    public void ClearTrail() {
        _trailPoints.Clear();
        UpdateTrailRenderer();
    }

    public void ButtonOnClick_Ret() => SceneManager.LoadScene("StellarEvolutionPreparationScene");
    public void ButtonOnClick_ExpandSidebar() { buttonExpandSidebar.SetActive(false); gameObjSidebar.SetActive(true); }
    public void ButtonOnClick_TakebackSidebar() { buttonExpandSidebar.SetActive(true); gameObjSidebar.SetActive(false); }
    public void ButtonOnClick_ExpandHRD() { buttonExpandHRD.SetActive(false); gameObjHRD.SetActive(true); }
    public void ButtonOnClick_TakebackHRD() { buttonExpandHRD.SetActive(true); gameObjHRD.SetActive(false); }
}

public class TrailPoint {
    public Vector2 position;
    public Color color;
}