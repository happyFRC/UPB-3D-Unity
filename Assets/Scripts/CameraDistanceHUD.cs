using TMPro;

using UnityEngine;



/// <summary>

/// 显示相机到目标恒星的距离，单位自动切换 km / AU / ly。

/// 按住 D 显示，松开隐藏；双击 D 持续显示，再次双击取消。

/// </summary>

public class CameraDistanceHUD : MonoBehaviour {

    [Header("引用")]

    [Tooltip("观察相机，留空则用 Camera.main")]

    public Camera observer;

    [Tooltip("目标恒星 Transform，留空则自动找带 Player 标签的物体")]

    public Transform target;

    [Tooltip("显示距离的文本（TextMeshProUGUI）")]

    public TextMeshProUGUI distanceText;



    [Header("换算")]

    [Tooltip("1 个 Unity 单位对应多少 km（与 InfiniteGrid 一致）")]

    public double kmPerUnit = CelestialScale.KmPerUnit;



    [Header("显示控制")]

    [Tooltip("启动时是否显示")]

    public bool startVisible = true;

    [Tooltip("切换/按住用的按键")]

    public KeyCode toggleKey = KeyCode.D;

    [Tooltip("双击判定时间窗口（秒）")]

    public float doubleTapTime = 0.3f;



    [Header("单位阈值")]

    [Tooltip("小于此 AU 值用 km 显示")]

    public double kmThresholdAu = 0.1;

    [Tooltip("小于此 ly 值用 AU 显示，否则用 ly")]

    public double auThresholdLy = 0.1;



    private const double AU_KM = CelestialScale.AuKm;

    private const double LY_KM = 9.4607304725808e12;



    private bool _sticky;

    private bool _held;

    private float _lastPressTime = -999f;



    void Start() {
        kmPerUnit = CelestialScale.KmPerUnit;

        if (observer == null) observer = Camera.main;

        if (target == null) {

            GameObject go = GameObject.FindGameObjectWithTag("Player");

            if (go != null) target = go.transform;

        }

        _sticky = startVisible;

        ApplyVisibility();

    }



    void Update() {

        if (Input.GetKeyDown(toggleKey)) {

            if (Time.unscaledTime - _lastPressTime <= doubleTapTime) {

                _sticky = !_sticky;

                _lastPressTime = -999f;

            } else {

                _lastPressTime = Time.unscaledTime;

            }

        }



        _held = Input.GetKey(toggleKey);



        ApplyVisibility();



        if (IsVisible()) RefreshText();

    }



    bool IsVisible() {

        return _sticky || _held;

    }



    void ApplyVisibility() {

        if (distanceText != null) distanceText.gameObject.SetActive(IsVisible());

    }



    void RefreshText() {

        if (distanceText == null || observer == null || target == null) return;



        double dUnit = SandboxWorldScale.ToVirtual(
            Vector3.Distance(observer.transform.position, target.position));

        double dKm = dUnit * kmPerUnit;

        if (dKm < 0) dKm = 0;



        distanceText.text = FormatDistance(dKm);

    }



    string FormatDistance(double km) {

        double au = km / AU_KM;

        double ly = km / LY_KM;



        if (au < kmThresholdAu) {

            return $"{FormatNumber(km)} km";

        } else if (ly < auThresholdLy) {

            return $"{FormatNumber(au)} AU";

        } else {

            return $"{FormatNumber(ly)} ly";

        }

    }



    string FormatNumber(double v) {

        if (v < 1000) return v.ToString("F2");

        if (v < 1e6) return v.ToString("F0");

        int exp = (int)System.Math.Floor(System.Math.Log10(v));

        double mant = v / System.Math.Pow(10, exp);

        return $"{mant:F2}e{exp}";

    }

}