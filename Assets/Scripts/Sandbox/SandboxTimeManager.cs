using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SandboxTimeUnit {
    MillisecondsPerSecond,
    SecondsPerSecond,
    MinutesPerSecond,
    HoursPerSecond,
    DaysPerSecond,
    YearsPerSecond
}

/// <summary>
/// 沙盘的全局时间。默认从 2023-04-17 00:00:00 开始，2.00 小时/秒。
/// 按住左/右方向键可以按 0.1 dex/s 连续调慢/调快。
/// 后续物理过程统一读 CurrentTime / DeltaSimulationSeconds / SimulationSeconds。
/// </summary>
public class SandboxTimeManager : MonoBehaviour {
    public static SandboxTimeManager Instance { get; private set; }

    public static readonly DateTime DefaultStartTime = new(2023, 4, 17, 0, 0, 0);
    private const double DaysPerYear = 365.2425;

    private DateTime _startTime = DefaultStartTime;
    private DateTime _currentTime = DefaultStartTime;
    private double _simulationSeconds;
    private double _deltaSimulationSeconds;
    private double _pendingClockCorrection;
    private bool _paused;
    private double _speedValue = 2.0;
    private SandboxTimeUnit _unit = SandboxTimeUnit.HoursPerSecond;
    private double _secondsPerRealSecond = 7200.0;

    private Transform _canvas;
    private GameObject _timePanel;
    private GameObject _createPanel;
    private GameObject _parameterPanel;
    private GameObject _planetParameterPanel;

    private TMP_Text _dateDisplay;
    private TMP_Text _elapsedDisplay;
    private TMP_InputField _dateInput;
    private TMP_Text _speedDisplay;
    private TMP_InputField _speedInput;

    private Button _unitButton;
    private TMP_Text _unitLabel;
    private GameObject _selectableUnits;
    private Button _pauseButton;
    private Graphic _pauseGraphic;

    // TimePanel 里的“加速/减速”按钮（按住生效）。
    private HoldButton _speedUpHold;
    private HoldButton _speedDownHold;
    private RectTransform _speedUpRect;
    private RectTransform _speedDownRect;

    // 调节速率：方向键沿用 0.1 dex/s；按钮 1 dex / 2 s = 0.5 dex/s。
    private const double ArrowDexPerSecond = 0.1;
    private const double ButtonDexPerSecond = 0.5;
    private const double MinSpeedValue = 1e-6;
    private const double MaxSpeedValue = 1e6;

    private Button _msButton;
    private Button _secButton;
    private Button _minButton;
    private Button _hourButton;
    private Button _dayButton;
    private Button _yearButton;

    private bool _editingDate;
    private bool _editingSpeed;
    private bool _pauseButtonOutsidePanel;

    public DateTime StartTime => _startTime;
    public DateTime CurrentTime => _currentTime;
    public double SimulationSeconds => _simulationSeconds;
    public double DeltaSimulationSeconds => _deltaSimulationSeconds;
    public bool IsPaused => _paused;
    public double SpeedValue => _speedValue;
    public SandboxTimeUnit Unit => _unit;
    public double SecondsPerRealSecond => _secondsPerRealSecond;

    public event Action<DateTime> TimeChanged;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        _startTime = DefaultStartTime;
        _currentTime = _startTime;
        _simulationSeconds = 0.0;
        _paused = false;
        _speedValue = 2.0;
        _unit = SandboxTimeUnit.HoursPerSecond;
        RecalculateSpeed();

        CacheUi();
        WireUi();
        UpdateUnitLabel();
        UpdateDateDisplay(true);
        UpdateSpeedDisplay(true);
        UpdatePauseVisual();
        UpdatePanelVisibility(true);
    }

    private void Update() {
        if (Instance != this) return;

        UpdatePanelVisibility(false);
        HandleSpeedShortcut();

        if (!_paused) {
            // 先补上上一帧“物理实际时间 vs 请求时间”的差额，
            // 这样日期始终等于行星真正走过的模拟时间。
            if (_pendingClockCorrection != 0.0) {
                _simulationSeconds += _pendingClockCorrection;
                _pendingClockCorrection = 0.0;
            }
            _deltaSimulationSeconds = Time.deltaTime * _secondsPerRealSecond;
            _simulationSeconds += _deltaSimulationSeconds;
            // DateTime 最大只到 9999 年，超出会抛 ArgumentOutOfRangeException。
            // 这里只把 DateTime 夹住（供其他系统使用），年份本身不设上限，
            // 显示时由 FormatSimulationDate() 用 _simulationSeconds 继续往上加。
            _currentTime = _startTime.AddSeconds(
                System.Math.Min(_simulationSeconds, MaxDateTimeSeconds));
            TimeChanged?.Invoke(_currentTime);
        } else {
            _deltaSimulationSeconds = 0.0;
        }

        UpdateDateDisplay(false);
        UpdateSpeedDisplay(false);
        UpdateElapsedDisplay();
    }

    /// <summary>显示“模拟已运行 xx”，单位按数值自动切换：秒/分/时/天/月/年。</summary>
    private void UpdateElapsedDisplay() {
        if (_elapsedDisplay == null) return;
        _elapsedDisplay.text = "模拟已运行 " + FormatElapsed(_simulationSeconds);
    }

    private static string FormatElapsed(double seconds) {
        double s = seconds < 0.0 ? 0.0 : seconds;
        (double factor, string name)[] units = {
            (1.0, "秒"),
            (60.0, "分"),
            (3600.0, "时"),
            (86400.0, "天"),
            (86400.0 * 30.4375, "月"),
            (86400.0 * DaysPerYear, "年")
        };

        int pick = 0;
        for (int i = 0; i < units.Length; i++) {
            if (s / units[i].factor >= 0.1) pick = i;
        }

        double value = s / units[pick].factor;
        return FormatElapsedValue(value) + " " + units[pick].name;
    }

    private static string FormatElapsedValue(double value) {
        if (value >= 100.0) return value.ToString("0", CultureInfo.InvariantCulture);
        if (value >= 10.0) return value.ToString("0.#", CultureInfo.InvariantCulture);
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // ~~~ Global API ~~~ //

    /// <summary>从指定日期重新开始计时（场景自定义起始时间用）。</summary>
    public void ResetClock(DateTime startTime) {
        _startTime = startTime;
        _currentTime = startTime;
        _simulationSeconds = 0.0;
        _deltaSimulationSeconds = 0.0;
        _pendingClockCorrection = 0.0;
        _paused = false;
        TimeChanged?.Invoke(_currentTime);
        UpdateDateDisplay(true);
        UpdatePauseVisual();
    }

    public void ResetClock() {
        _startTime = DefaultStartTime;
        _currentTime = _startTime;
        _simulationSeconds = 0.0;
        _deltaSimulationSeconds = 0.0;
        _pendingClockCorrection = 0.0;
        _paused = false;
        TimeChanged?.Invoke(_currentTime);
        UpdateDateDisplay(true);
        UpdatePauseVisual();
    }

    public void SetPaused(bool paused) {
        _paused = paused;
        _deltaSimulationSeconds = 0.0;
        UpdatePauseVisual();
    }

    public void TogglePause() {
        SetPaused(!_paused);
    }

    public void SetUnit(SandboxTimeUnit unit) {
        _unit = unit;
        RecalculateSpeed();
        UpdateUnitLabel();
        UpdateSpeedDisplay(true);
        if (_selectableUnits != null) _selectableUnits.SetActive(false);
    }

    public void SetSpeed(double speed) {
        // 读档时可能拿到 NaN/Infinity 或离谱的倍率，这里夹一下，
        // 否则一帧的模拟时间会直接爆掉。
        if (double.IsNaN(speed) || double.IsInfinity(speed)) speed = 2.0;
        _speedValue = Math.Clamp(speed, 0.0, MaxSpeedValue);
        RecalculateSpeed();
        UpdateSpeedDisplay(true);
    }

    /// <summary>
    /// 按住左右方向键（0.1 dex/s）或 TimePanel 的加速/减速按钮（1 dex / 2 s）
    /// 连续调整时间流速。
    /// </summary>
    private void HandleSpeedShortcut() {
        bool upHeld = IsSpeedHeld(_speedUpHold, _speedUpRect);
        bool downHeld = IsSpeedHeld(_speedDownHold, _speedDownRect);

        // 按住按钮时不受输入框焦点影响：点过速度输入框之后也应该能继续按按钮调速。
        if (!upHeld && !downHeld && IsTypingInInput()) return;

        double dexPerSecond = 0.0;

        if (Input.GetKey(KeyCode.RightArrow)) dexPerSecond += ArrowDexPerSecond;
        if (Input.GetKey(KeyCode.LeftArrow)) dexPerSecond -= ArrowDexPerSecond;

        if (upHeld) dexPerSecond += ButtonDexPerSecond;
        if (downHeld) dexPerSecond -= ButtonDexPerSecond;

        if (Math.Abs(dexPerSecond) < 1e-9) return;

        double factor = Math.Pow(10.0, dexPerSecond * Time.unscaledDeltaTime);
        double next = _speedValue * factor;
        // 防止调到 0 之后再也调不回来；上限避免极端倍率下数值溢出。
        next = Math.Clamp(next, MinSpeedValue, MaxSpeedValue);
        SetSpeed(next);
    }

    private static bool IsSpeedHeld(HoldButton hold, RectTransform rect) {
        // 以“指针 + 矩形”为准：即使某帧丢了 PointerUp 事件也不会卡住一直加速。
        if (rect != null) return IsPointerHolding(rect);
        return hold != null && hold.IsHeld;
    }

    /// <summary>
    /// 指针是否正按在这个 UI 矩形上。直接按屏幕坐标判断，不依赖 EventSystem /
    /// raycastTarget，避免按钮事件没接上时整个功能失效。
    /// </summary>
    private static bool IsPointerHolding(RectTransform rect) {
        if (rect == null || !rect.gameObject.activeInHierarchy) return false;
        if (!Input.GetMouseButton(0)) return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) {
            camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
        return RectTransformUtility.RectangleContainsScreenPoint(
            rect, Input.mousePosition, camera);
    }

    private static bool IsTypingInInput() {
        if (EventSystem.current == null) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;
        return selected.GetComponentInParent<TMP_InputField>() != null;
    }

    public void SetCurrentTime(DateTime time) {
        _currentTime = time;
        _simulationSeconds = (time - _startTime).TotalSeconds;
        _pendingClockCorrection = 0.0;
        TimeChanged?.Invoke(_currentTime);
        UpdateDateDisplay(true);
    }

    /// <summary>
    /// 物理层报告本帧真正积分掉的模拟秒数。与请求值的差额暂存下来，
    /// 下一帧开头补进时钟：极端倍率下步长受限、物理少走的那部分，
    /// 日期也不会提前跑掉（否则会出现“日期过了 600 年、行星却没转完一圈”）。
    /// </summary>
    public void ReportSimulatedSeconds(double simulatedSeconds) {
        double requested = _deltaSimulationSeconds;
        if (simulatedSeconds < 0.0) simulatedSeconds = 0.0;
        // 物理不可能比请求走得多；出现这种情况只可能是本帧时钟还没推进。
        if (simulatedSeconds > requested) simulatedSeconds = requested;
        _deltaSimulationSeconds = simulatedSeconds;
        _pendingClockCorrection += simulatedSeconds - requested;
    }

    private void RecalculateSpeed() {
        double unitSeconds = _unit switch {
            SandboxTimeUnit.MillisecondsPerSecond => 0.001,
            SandboxTimeUnit.SecondsPerSecond => 1.0,
            SandboxTimeUnit.MinutesPerSecond => 60.0,
            SandboxTimeUnit.HoursPerSecond => 3600.0,
            SandboxTimeUnit.DaysPerSecond => 86400.0,
            SandboxTimeUnit.YearsPerSecond => 86400.0 * DaysPerYear,
            _ => 86400.0,
        };
        _secondsPerRealSecond = _speedValue * unitSeconds;
    }

    // ~~~ UI ~~~ //

    private void CacheUi() {
        _canvas = GameObject.Find("Canvas")?.transform;
        _timePanel = FindDeep(_canvas, "TimePanel")?.gameObject;
        if (_timePanel == null) _timePanel = FindInScene("TimePanel")?.gameObject;
        _createPanel = FindDeep(_canvas, "CelestialBodyCreatePanel")?.gameObject;
        _parameterPanel = FindDeep(_canvas, "StarParameterPanel")?.gameObject;
        _planetParameterPanel = FindDeep(_canvas, "PlanetParameterPanel")?.gameObject;
        if (_timePanel == null) {
            Debug.LogWarning("[SandboxTime] 找不到 TimePanel");
            return;
        }

        // “模拟已运行 …”文本（TimePanel/SimulationHadRunning）。
        _elapsedDisplay = FindText(_timePanel.transform, "SimulationHadRunning");

        // 场景里实际名字是 DateDisplatText（少了一个 y），兼容两种拼写。
        TMP_Text dateText = FindText(_timePanel.transform, "DateDisplatText");
        if (dateText == null) dateText = FindText(_timePanel.transform, "DateDisplayText");
        TMP_InputField input = FindInput(_timePanel.transform, "InputField (TMP)");

        bool inputDate = LooksLikeDate(input != null ? input.text : "") ||
                         LooksLikeDate(GetPlaceholder(input));
        bool inputNumber = LooksLikeNumber(input != null ? input.text : "") ||
                           LooksLikeNumber(GetPlaceholder(input));
        bool displayDate = dateText != null && LooksLikeDate(dateText.text);
        bool displayNumber = dateText != null && LooksLikeNumber(dateText.text);

        if (inputNumber && !inputDate && displayDate) {
            // InputField 是速度，DateDisplayText 是日期。
            _speedInput = input;
            _dateDisplay = dateText;
        } else if (inputDate && displayNumber) {
            // InputField 是日期，DateDisplayText 是速度。
            _dateInput = input;
            _speedDisplay = dateText;
        } else {
            // 默认：InputField 是日期，DateDisplayText 是日期显示。
            _dateInput = input;
            _dateDisplay = dateText;
        }

        _unitButton = FindDeep(_timePanel.transform, "ButtonUnitChage")?.GetComponent<Button>();
        if (_unitButton != null) {
            _unitLabel = _unitButton.GetComponentInChildren<TMP_Text>(true);
            if (_unitLabel == null) {
                _unitLabel = CreateUnitLabel(_unitButton.transform, dateText);
            }
        }

        _selectableUnits = FindDeep(_timePanel.transform, "SelectableUnits")?.gameObject;
        if (_selectableUnits == null) _selectableUnits = FindDeep(_canvas, "SelectableUnits")?.gameObject;
        if (_selectableUnits != null) _selectableUnits.SetActive(false);

        _msButton = FindButton("ButtonMilliSecsPerSec", _selectableUnits);
        _secButton = FindButton("ButtonSecPerSec", _selectableUnits);
        _minButton = FindButton("ButtonMinPerSec", _selectableUnits);
        _hourButton = FindButton("ButtonHourPerSec (1)", _selectableUnits);
        if (_hourButton == null) _hourButton = FindButton("ButtonHourPerSec", _selectableUnits);
        _dayButton = FindButton("ButtonDaysPerSec (2)", _selectableUnits);
        if (_dayButton == null) _dayButton = FindButton("ButtonDaysPerSec", _selectableUnits);
        _yearButton = FindButton("ButtonYrsPerSec (3)", _selectableUnits);
        if (_yearButton == null) _yearButton = FindButton("ButtonYrsPerSec", _selectableUnits);

        _pauseButton = FindDeep(_canvas, "ButtonPauseTime")?.GetComponent<Button>();
        if (_pauseButton == null) _pauseButton = FindInScene("ButtonPauseTime")?.GetComponent<Button>();
        if (_pauseButton != null) {
            _pauseGraphic = _pauseButton.targetGraphic != null
                ? _pauseButton.targetGraphic
                : _pauseButton.GetComponent<Graphic>();
            _pauseButtonOutsidePanel = _timePanel != null &&
                !_pauseButton.transform.IsChildOf(_timePanel.transform);
        }

        CacheSpeedButtons();
    }

    /// <summary>
    /// 找到 TimePanel 里的加速/减速按钮；场景里没有时克隆暂停按钮补两个。
    /// 找到后记录 RectTransform：按住判定直接按鼠标位置轮询，不依赖 EventSystem。
    /// </summary>
    private void CacheSpeedButtons() {
        if (_timePanel == null) return;

        // 场景里就叫 ButtonAccTime / ButtonDeaccTime，先按名字精确找。
        Transform up = FindDeep(_timePanel.transform, "ButtonAccTime");
        if (up == null) up = FindSpeedButtonTransform(_timePanel.transform, true);
        Transform down = FindDeep(_timePanel.transform, "ButtonDeaccTime");
        if (down == null) down = FindSpeedButtonTransform(_timePanel.transform, false);

        if (up == null) up = CreateSpeedButton("Button_SpeedUp", "+", true);
        if (down == null) down = CreateSpeedButton("Button_SpeedDown", "−", false);

        _speedUpRect = up as RectTransform;
        _speedDownRect = down as RectTransform;
        _speedUpHold = up != null ? EnsureHoldButton(up) : null;
        _speedDownHold = down != null ? EnsureHoldButton(down) : null;

        if (_speedUpRect == null || _speedDownRect == null) {
            Debug.LogWarning("[Time] 没找到加速/减速按钮，按住调速不可用。");
        } else {
            Debug.Log($"[Time] 调速按钮: 加速={_speedUpRect.name}, 减速={_speedDownRect.name}");
        }
    }

    private static HoldButton EnsureHoldButton(Transform target) {
        HoldButton hold = target.GetComponent<HoldButton>();
        if (hold == null) hold = target.gameObject.AddComponent<HoldButton>();
        return hold;
    }

    // 命名可以是 ButtonAccTime / ButtonDeaccTime / Button_Accelerate / 加速 等。
    // 减速优先判定，避免 "deacc" 里的 "acc" 被误判成加速。
    private static readonly string[] SpeedUpKeys = {
        "speedup", "speed_up", "acc", "accelerate", "accel", "fast", "plus", "加速", "加快"
    };
    private static readonly string[] SpeedDownKeys = {
        "deacc", "speeddown", "speed_down", "decel", "slow", "minus", "减速", "减慢"
    };

    private Transform FindSpeedButtonTransform(Transform root, bool wantUp) {
        foreach (Button button in root.GetComponentsInChildren<Button>(true)) {
            if (button == null) continue;
            string name = button.gameObject.name.ToLowerInvariant();
            // 暂停按钮的文字会在 "||" 和 ">" 之间切换，按名字排除掉。
            if (name.Contains("pause")) continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string text = label != null ? label.text.Trim().ToLowerInvariant() : "";

            bool isDown = ContainsAny(name, SpeedDownKeys) ||
                          text == "-" || text == "−" || text == "－" ||
                          text == "▼" || text == "↓";
            bool isUp = !isDown && (ContainsAny(name, SpeedUpKeys) ||
                          text == "+" || text == "＋" ||
                          text == "▲" || text == "↑");
            if (isUp != wantUp) continue;
            return button.transform;
        }
        return null;
    }

    private static bool ContainsAny(string value, string[] keys) {
        foreach (string key in keys) {
            if (value.Contains(key)) return true;
        }
        return false;
    }

    /// <summary>场景里没有加速/减速按钮时，克隆暂停按钮的样式补一个。</summary>
    private Transform CreateSpeedButton(string name, string label, bool up) {
        if (_pauseButton == null) return null;

        GameObject go = Instantiate(_pauseButton.gameObject, _pauseButton.transform.parent);
        go.name = name;

        Button button = go.GetComponent<Button>();
        if (button != null) button.onClick.RemoveAllListeners();

        RectTransform rect = go.GetComponent<RectTransform>();
        RectTransform pauseRect = _pauseButton.transform as RectTransform;
        if (rect != null && pauseRect != null) {
            Vector2 basePosition = pauseRect.anchoredPosition;
            rect.anchoredPosition = basePosition + new Vector2(up ? 58f : 116f, 0f);
        }

        TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;

        return go.transform;
    }

    private void WireUi() {
        if (_unitButton != null) _unitButton.onClick.AddListener(ToggleUnitList);
        if (_pauseButton != null) _pauseButton.onClick.AddListener(TogglePause);

        WireUnitButton(_msButton, SandboxTimeUnit.MillisecondsPerSecond, "毫秒/秒");
        WireUnitButton(_secButton, SandboxTimeUnit.SecondsPerSecond, "秒/秒");
        WireUnitButton(_minButton, SandboxTimeUnit.MinutesPerSecond, "分/秒");
        WireUnitButton(_hourButton, SandboxTimeUnit.HoursPerSecond, "时/秒");
        WireUnitButton(_dayButton, SandboxTimeUnit.DaysPerSecond, "天/秒");
        WireUnitButton(_yearButton, SandboxTimeUnit.YearsPerSecond, "年/秒");

        if (_dateInput != null) {
            _dateInput.onSelect.AddListener(_ => _editingDate = true);
            _dateInput.onDeselect.AddListener(_ => {
                ParseDateInput();
                _editingDate = false;
            });
            _dateInput.onEndEdit.AddListener(_ => {
                ParseDateInput();
                _editingDate = false;
            });
        }

        if (_speedInput != null) {
            _speedInput.onSelect.AddListener(_ => _editingSpeed = true);
            _speedInput.onDeselect.AddListener(_ => {
                ParseSpeedInput();
                _editingSpeed = false;
            });
            _speedInput.onEndEdit.AddListener(_ => {
                ParseSpeedInput();
                _editingSpeed = false;
            });
        }
    }

    private void WireUnitButton(Button button, SandboxTimeUnit unit, string label) {
        if (button == null) return;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        button.onClick.AddListener(() => SetUnit(unit));
    }

    private void ToggleUnitList() {
        if (_selectableUnits == null) return;
        bool open = !_selectableUnits.activeSelf;
        _selectableUnits.SetActive(open);
        if (open) {
            if (_unitButton != null) _unitButton.transform.SetAsLastSibling();
            _selectableUnits.transform.SetAsLastSibling();
        }
    }

    private void UpdateUnitLabel() {
        if (_unitLabel != null) _unitLabel.text = UnitLabel(_unit);
    }

    private static string UnitLabel(SandboxTimeUnit unit) {
        return unit switch {
            SandboxTimeUnit.MillisecondsPerSecond => "毫秒/秒",
            SandboxTimeUnit.SecondsPerSecond => "秒/秒",
            SandboxTimeUnit.MinutesPerSecond => "分/秒",
            SandboxTimeUnit.HoursPerSecond => "时/秒",
            SandboxTimeUnit.DaysPerSecond => "天/秒",
            SandboxTimeUnit.YearsPerSecond => "年/秒",
            _ => "天/秒",
        };
    }

    private void UpdateDateDisplay(bool force) {
        string value = FormatSimulationDate();

        if (_dateInput != null && (force || !_editingDate)) {
            _dateInput.text = value;
        }
        if (_dateDisplay != null && _dateDisplay != _dateInput) {
            _dateDisplay.text = value;
        }
    }

    /// <summary>DateTime 能表示的最大模拟秒数（留一天余量，避免边界抖动）。</summary>
    private double MaxDateTimeSeconds =>
        System.Math.Max(0.0, (DateTime.MaxValue - _startTime).TotalSeconds - 86400.0);

    /// <summary>
    /// 日期显示：9999 年以内用 DateTime；超过之后年份继续累加（不设上限），
    /// 月/日/时/分/秒按“一年内的余量”换算。
    /// </summary>
    private string FormatSimulationDate() {
        if (double.IsNaN(_simulationSeconds) || double.IsInfinity(_simulationSeconds)) {
            return FormatDate(_startTime);
        }

        double maxSeconds = MaxDateTimeSeconds;
        if (_simulationSeconds <= maxSeconds) return FormatDate(_currentTime);

        double secondsPerYear = 86400.0 * DaysPerYear;
        double overflow = _simulationSeconds - maxSeconds;
        long extraYears = (long)System.Math.Floor(overflow / secondsPerYear);
        double remainder = overflow - extraYears * secondsPerYear;

        // 用闰年 2000-01-01 当基准，换算一年内的月/日/时/分/秒。
        DateTime tail = new DateTime(2000, 1, 1).AddSeconds(remainder);
        long year = 9999 + 1 + extraYears;
        return year.ToString(CultureInfo.InvariantCulture)
               + tail.ToString("-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private void UpdateSpeedDisplay(bool force) {
        string value = _speedValue.ToString("0.00", CultureInfo.InvariantCulture);
        if (_speedInput != null && (force || !_editingSpeed)) {
            _speedInput.text = value;
        }
        if (_speedDisplay != null && _speedDisplay != _speedInput) {
            _speedDisplay.text = value;
        }
    }

    private void ParseDateInput() {
        if (_dateInput == null) return;
        string text = _dateInput.text != null ? _dateInput.text.Trim() : "";
        if (TryParseDate(text, out DateTime parsed)) {
            SetCurrentTime(parsed);
        }
        UpdateDateDisplay(true);
    }

    private void ParseSpeedInput() {
        if (_speedInput == null) return;
        string text = _speedInput.text != null ? _speedInput.text.Trim() : "";
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,
                out double value) && value >= 0.0) {
            SetSpeed(value);
        }
        UpdateSpeedDisplay(true);
    }

    private static bool TryParseDate(string text, out DateTime value) {
        string[] formats = {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-dd",
            "yyyy/MM/dd",
        };
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out value)) {
            return true;
        }
        return DateTime.TryParse(text, out value);
    }

    private void UpdatePauseVisual() {
        if (_pauseGraphic != null) {
            _pauseGraphic.color = _paused
                ? new Color(0.20f, 0.75f, 0.35f, 1f)
                : new Color(0.85f, 0.10f, 0.10f, 1f);
        }

        if (_pauseButton != null) {
            TMP_Text label = _pauseButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = _paused ? ">" : "||";
        }
    }

    private void UpdatePanelVisibility(bool force) {
        bool hide = (_createPanel != null && _createPanel.activeSelf) ||
                    (_parameterPanel != null && _parameterPanel.activeSelf) ||
                    (_planetParameterPanel != null && _planetParameterPanel.activeSelf);

        if (_timePanel != null) {
            bool shouldShow = !hide;
            if (force || _timePanel.activeSelf != shouldShow) {
                _timePanel.SetActive(shouldShow);
            }
        }

        if (hide && _selectableUnits != null) _selectableUnits.SetActive(false);

        if (_pauseButton != null && _pauseButtonOutsidePanel) {
            if (force || _pauseButton.gameObject.activeSelf == hide) {
                _pauseButton.gameObject.SetActive(!hide);
            }
        }
    }

    // ~~~ Helpers ~~~ //

    private static string FormatDate(DateTime time) {
        return time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string GetPlaceholder(TMP_InputField input) {
        if (input == null || input.placeholder == null) return "";
        return input.placeholder is TMP_Text text ? text.text : "";
    }

    private static bool LooksLikeDate(string text) {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Contains("-") || text.Contains("/") || text.Contains(":");
    }

    private static bool LooksLikeNumber(string text) {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private Button FindButton(string name, GameObject root) {
        Transform t = root != null ? FindDeep(root.transform, name) : null;
        if (t == null) t = FindDeep(_canvas, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static TMP_Text FindText(Transform root, string name) {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static TMP_InputField FindInput(Transform root, string name) {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_InputField>() : null;
    }

    private static Transform FindDeep(Transform root, string name) {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root) {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindInScene(string name) {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects()) {
            Transform found = FindDeep(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetButtonLabel(Button button, string text) {
        if (button == null) return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    private static TMP_Text CreateUnitLabel(Transform parent, TMP_Text fontSource) {
        GameObject go = new("UnitLabel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(4f, 2f);
        rect.offsetMax = new Vector2(-4f, -2f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = fontSource != null ? fontSource.font : null;
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts/KosefontP-JP SDF 1")
            ?? Resources.Load<TMP_FontAsset>("Fonts/FRCFONT SDF");
        text.font = font;
        if (font != null) text.fontSharedMaterial = font.material;
        text.fontSize = 24f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = "天/秒";
        return text;
    }
}
