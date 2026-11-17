using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OSE = OtherSimulationEngine;

public class OtherStarCalculationScene : MonoBehaviour {
    [Header("侧边栏")]
    public GameObject gameObjSidebar;
    public GameObject buttonExpandSidebar;

    [Header("3D 恒星")]
    public GameObject star;
    public Material starMaterial;

    [Header("数据面板")]
    public TextMeshProUGUI textName;
    public TextMeshProUGUI textRadius;
    public TextMeshProUGUI textTemperature;
    public TextMeshProUGUI textLuminosity;
    public TextMeshProUGUI textSpectrum;

    [Header("单位切换")]
    public UnitButton unitRadius;
    public UnitButton unitTemperature;
    public UnitButton unitLuminosity;

    // 原始数据（R☉ / K / L☉）
    private double _rawRadius;
    private double _rawTemperature;
    private double _rawLuminosity;
    private string _name;
    private Spectrum _spectrum;
    private StarLightPoint _starLightPoint;

    void Start() {
        starMaterial = star.GetComponent<Renderer>().material;

        _name = OtherStar_Preparation.Name;
        var type = OtherStar_Preparation.Type;
        double mass = OtherStar_Preparation.Mass;

        double L, R, T;
        try {
            (L, R, T) = OSE.Calculate(type, mass);
        }
        catch (System.Exception e) {
            Debug.LogError("计算失败: " + e.Message);
            return;
        }

        // 存原始值
        _rawLuminosity = L;      // L☉
        _rawRadius = R;          // R☉
        _rawTemperature = T;     // K

        double massSolar = type == OSE.OtherStarType.BrownDwarf
            ? mass / 1047.57
            : mass;
        _spectrum = Spectrum.GetSpectrum(R, massSolar, T);

        // 绑定单位按钮
        unitRadius.onUnitChanged += _ => RefreshText();
        unitTemperature.onUnitChanged += _ => RefreshText();
        unitLuminosity.onUnitChanged += _ => RefreshText();

        // 3D 恒星外观（不随单位变化）
        ApplyStarVisual(T, R);

        _starLightPoint = StarLightPoint.Attach(star, Camera.main);
        _starLightPoint?.SetStar(_rawLuminosity, _rawTemperature);

        // 刷新文本
        RefreshText();
    }

    /// <summary>
    /// 用当前单位刷新所有文本
    /// </summary>
    void RefreshText() {
        textName.text = _name;
        textRadius.text = Utils.FormatNumber(GetRadius(_rawRadius));
        textTemperature.text = Utils.FormatNumber(GetTemperature(_rawTemperature));
        textLuminosity.text = Utils.FormatNumber(GetLuminosity(_rawLuminosity));
        textSpectrum.text = _spectrum.ToString();
    }

    void ApplyStarVisual(double T, double R) {
        var (r, g, b) = Spectrum.GetBlackbodyColor(T);

        starMaterial.SetColor("_BaseColor", new Color(r, g, b, 1));
        starMaterial.SetColor("_CellColor", new Color(r * 1.38f, g * 1.38f, b * 1.38f, 1));
        starMaterial.SetFloat(
            "_EmissionStrength",
            Utils.GetStarEmissionStrength(T / OSE.tSunSurf));

        float displayScale = (float)R * 15f;
        star.transform.localScale = new(displayScale, displayScale, displayScale);
    }

    // ~~~ 单位换算 ~~~ //
    // 注意：这里传入的 original 是 R☉（不是 km）
    public double GetRadius(double originalRsun) {
        string unit = unitRadius.GetUnit();
        if (unit == "RSun") return originalRsun;
        if (unit == "REarth") return originalRsun * (OSE.rSun / 6371.0);
        return originalRsun * OSE.rSun;   // km
    }

    public double GetTemperature(double originalK) {
        string unit = unitTemperature.GetUnit();
        if (unit == "K") return originalK;
        if (unit == "°C") return originalK - 273.15;
        return (originalK - 273.15) * (9.0 / 5.0) + 32;
    }

    public double GetLuminosity(double originalLsun) {
        string unit = unitLuminosity.GetUnit();
        if (unit == "LSun") return originalLsun;
        return OSE.lSun * originalLsun;
    }

    // ~~~ 按钮回调 ~~~ //
    public void ButtonOnClick_Ret() {
        SceneManager.LoadScene("OtherStarCalculationPreparationScene");
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