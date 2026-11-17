using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新手向导：首次进入沙盘时询问，按 Y 开始，按 I 随时退出。
/// 每一步都要玩家完成指定操作才会自动进入下一步。
///
/// 位置规则：
///  · 向导只在屏幕左右两侧移动，表情边缘距屏幕边框恒为 220px；
///  · 表情在以锚点为圆心、半径 60px 的圆内移动，并水平镜像“看向”屏幕中心；
///  · 对话框统一放在向导正上方 200px；
///  · 需要看左侧界面时向导移到左边，看完回到右侧。
/// </summary>
public class SandboxGuide : MonoBehaviour {
    private const float EdgeMargin = 220f;
    private const float FaceRadius = 60f;
    private const float BubbleAbove = 275f;
    // 版本号：改了键名，旧的“已看过”记录不再生效（用户从没走完向导时会重新询问）。
    private const string IntroKey = "UPB_GuideIntroShown_v2";

    public static bool IsRunning { get; private set; }
    private static bool _pendingRestart;
    public static bool IsIntroActive { get; private set; }

    /// <summary>向导当前是否显示在屏幕上（询问/准备/教学/提示都算）。</summary>
    public static bool IsVisible { get; private set; }

    /// <summary>向导从“显示”变为“隐藏”时回调，用来补弹被压住的场景介绍。</summary>
    public Action OnHidden;

    private enum Side { Right, Left }
    private enum Mood { Neutral, Happy, Curious, Serious, Excited, Angry }

    private class Step {
        public string Text;
        public Func<bool> Done;
        public float AutoSeconds;
        public Side Side = Side.Right;
        public Mood Mood = Mood.Neutral;
        public Action OnEnter;
    }

    private SandboxSceneController _controller;
    private RectTransform _root;
    private RectTransform _face;
    private RawImage _faceImage;
    private RectTransform _bubble;
    private TMP_Text _bubbleText;
    private RawImage _bodyImage;

    private readonly System.Collections.Generic.List<Step> _steps = new();
    private readonly System.Collections.Generic.List<RectTransform> _panels = new();
    private readonly System.Collections.Generic.Dictionary<string, Texture2D> _expressions = new();
    private readonly System.Collections.Generic.HashSet<string> _visitedModules = new();

    private bool _intro;
    private bool _notice;
    private int _prepPhase;
    private int _prepLoadMark;
    private int _scenarioMark;
    private bool _tutorial;
    private int _index = -1;
    private float _stepTime;
    private int _bodyBaseline;
    private bool _labelsBaseline;
    private int _renderBaseline;
    private bool _renderBaselineCaptured;
    private double _speedBaseline;
    private double _speedPeak;
    private bool _trailBaseline;
    private bool _orbitBaseline;
    private bool _gridBaseline;
    private Vector2 _anchor;
    private Vector2 _faceOffset;
    private Side _side = Side.Right;
    private Mood _mood = Mood.Neutral;

    // ~~~ 初始化 ~~~

    public void Initialize(SandboxSceneController controller) {
        _controller = controller;

        _root = GetComponent<RectTransform>();
        _face = FindChild(_root, "RawImage");
        _faceImage = _face != null ? _face.GetComponent<RawImage>() : null;
        _bodyImage = GetComponent<RawImage>();
        _bubble = FindChild(_root, "ConversationPop");
        _bubbleText = _bubble != null ? _bubble.GetComponentInChildren<TMP_Text>(true) : null;

        CachePanels();
        LoadExpressions();
        BuildSteps();

        // 一直保持物体激活（只是隐藏画面），否则 Update 不跑，F9 也没法重播。
        SetVisible(false);
        Debug.Log("[Guide] init, flag=" + PlayerPrefs.GetInt(IntroKey, 0) +
                  " steps=" + _steps.Count + " controller=" + (_controller != null));

        if (_pendingRestart) {
            if (_controller != null && _controller.IsAquariusScenario) {
                _pendingRestart = false;
                StartTutorial();
                return;
            }
        }
        if (PlayerPrefs.GetInt(IntroKey, 0) == 0) BeginIntro();
    }

    private static RectTransform FindChild(Transform root, string name) {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++) {
            Transform c = root.GetChild(i);
            if (c.name == name) return c as RectTransform;
        }
        return null;
    }

    private static Transform FindScene(string name) {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>()) {
            if (t == null || t.name != name) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            return t;
        }
        return null;
    }

    private static bool IsPanelActive(string name) {
        Transform t = FindScene(name);
        return t != null && t.gameObject.activeInHierarchy;
    }

    private static void SetPanelActive(string name, bool active) {
        Transform t = FindScene(name);
        if (t != null) t.gameObject.SetActive(active);
    }

    private void CachePanels() {
        _panels.Clear();
        // 只把“会挡住操作的大面板”算进重叠检测。
        foreach (string n in new[] {
            "CelestialBodyCreatePanel", "StarParameterPanel", "PlanetParameterPanel",
            "SettingsPanelSandbox", "HomePagePanel" }) {
            Transform t = FindScene(n);
            if (t is RectTransform rt) _panels.Add(rt);
        }
    }

    private void LoadExpressions() {
        _expressions.Clear();
        foreach (string name in new[] {
            "Normal", "smile", "smile_eyes_opend", "wink", "bored", "angry",
            "eyes_closed", "left_confused", "right_confused" }) {
            Texture2D tex = Resources.Load<Texture2D>("Guider_Expressions/" + name);
            if (tex != null) _expressions[name] = tex;
        }
    }

    // ~~~ 步骤表 ~~~

    private void Add(string text, Func<bool> done, float auto, Side side, Mood mood,
            Action onEnter = null) {
        _steps.Add(new Step {
            Text = text, Done = done, AutoSeconds = auto, Side = side, Mood = mood, OnEnter = onEnter
        });
    }

    private void BuildSteps() {
        _steps.Clear();

        Add("先提醒一下：按 I 可以随时退出向导。", null, 3.5f, Side.Right, Mood.Happy);
        Add("点击左上角的 + 按钮，打开创建天体面板。",
            () => IsPanelActive("CelestialBodyCreatePanel"), -1f, Side.Right, Mood.Curious);
        Add("在面板里选一颗行星，进入放置模式。",
            () => IsPanelActive("SandboxPlacementPreview"), -1f, Side.Right, Mood.Curious);
        Add("如果不想放了，按鼠标右键可以取消放置状态。",
            null, 0f, Side.Right, Mood.Neutral);
        Add("在场景里点一下，把它放下来。",
            () => SandboxPhysicsSystem.Bodies.Count > _bodyBaseline, -1f, Side.Right, Mood.Excited);
        Add("放下之后先把这个创建面板关掉。",
            () => !IsPanelActive("CelestialBodyCreatePanel"), -1f, Side.Right, Mood.Neutral);

        Add("点击刚放下的天体，打开它的参数面板。",
            () => IsPanelActive("StarParameterPanel") || IsPanelActive("PlanetParameterPanel"),
            -1f, Side.Left, Mood.Curious, () => _visitedModules.Clear());
        // 打开面板时已经显示的那个模块直接记一次，不用再切回来重新看。
        Add("依次看看通用、力学、能量三个模块。", ModulesVisited,
            -1f, Side.Left, Mood.Curious, () => ModulesVisited());
        Add("看完之后，把参数面板关掉。",
            () => !IsPanelActive("StarParameterPanel") && !IsPanelActive("PlanetParameterPanel"),
            -1f, Side.Left, Mood.Neutral);

        Add("点齿轮打开设置面板。", () => IsPanelActive("SettingsPanelSandbox"),
            -1f, Side.Right, Mood.Happy);
        Add("设置分通用、引力、热学三个模块，先点通用，试试“恒星曝光度”。",
            () => _controller != null && _renderBaselineCaptured
                && _controller.StarRenderMode != _renderBaseline,
            -1f, Side.Right, Mood.Excited, CaptureRenderBaseline);
        Add("点击画面看效果，然后关掉设置。", () => !IsPanelActive("SettingsPanelSandbox"),
            -1f, Side.Right, Mood.Happy);

        Add("再打开设置，切到引力模块。", () => IsPanelActive("SettingsPanelSandbox"),
            -1f, Side.Right, Mood.Curious);
        Add("把“全局引力”关掉试试。", () => !SandboxPhysicsSystem.GravityEnabled,
            -1f, Side.Right, Mood.Angry);
        Add("我来帮你关掉面板、把时间调到 7 天/秒，并把镜头拉到 5 AU 之外，注意看轨道变化。", null, 1.2f,
            Side.Right, Mood.Serious, ApplyGravityDemo);
        Add("看到了吗？关掉引力后，天体不再互相吸引，会沿直线漂移。", null, 4.0f,
            Side.Right, Mood.Neutral);

        Add("现在重新加载一遍水瓶座一星系，看看不同的场景。", null, 1.0f,
            Side.Right, Mood.Happy, ReloadAquarius);
        Add("加载好了。再次打开设置。", () => IsPanelActive("SettingsPanelSandbox"),
            -1f, Side.Right, Mood.Happy);
        Add("切到第三个模块：热学。", () => IsPanelActive("ThermalModelPanel"),
            -1f, Side.Right, Mood.Curious);
        Add("把“热辐射散热”关掉，看行星会怎样。", () => !SandboxPhysicsSystem.ThermalRadiationLoss,
            -1f, Side.Right, Mood.Angry);
        Add("然后关掉设置面板。", () => !IsPanelActive("SettingsPanelSandbox"),
            -1f, Side.Right, Mood.Neutral);

        Add("打开参数面板，查看“热辐射”功率那一行。",
            () => IsPanelActive("PlanetParameterPanel") || IsPanelActive("StarParameterPanel"),
            -1f, Side.Left, Mood.Curious);
        Add("看完把它关掉。",
            () => !IsPanelActive("PlanetParameterPanel") && !IsPanelActive("StarParameterPanel"),
            -1f, Side.Left, Mood.Neutral);
        // HOME 面板教学：打开场景库，双击一个存档进入。
        Add("点左上角的 HOME 按钮，打开场景库。",
            () => IsPanelActive("HomePagePanel"), -1f, Side.Left, Mood.Happy);
        Add("双击一个你想进入的存档，就能加载它。",
            () => !IsPanelActive("HomePagePanel"), -1f, Side.Left, Mood.Excited);
        Add("回到这边来，接着讲时间面板。", null, 1.5f, Side.Right, Mood.Happy);

        Add("下面这个是时间面板：可以改时间流速。", null, 3.0f, Side.Right, Mood.Curious);
        Add("点一下“加速”，时间会跑得更快。",
            () => SpeedAbove(_speedBaseline), -1f, Side.Right, Mood.Excited,
            () => _speedBaseline = CurrentSpeed());
        Add("再点一下“减速”，把速度降回来。",
            () => SpeedBelow(_speedPeak), -1f, Side.Right, Mood.Happy,
            () => _speedPeak = CurrentSpeed());
        Add("点暂停按钮，时间会停下来。", () => {
            SandboxTimeManager t = SandboxTimeManager.Instance;
            return t != null && t.IsPaused;
        }, -1f, Side.Right, Mood.Curious);
        Add("再点一次继续运行。", () => {
            SandboxTimeManager t = SandboxTimeManager.Instance;
            return t != null && !t.IsPaused;
        }, -1f, Side.Right, Mood.Happy);

        Add("最后是几个快捷键：按 L 显示/隐藏天体标签。",
            () => _controller != null && _controller.LabelsVisible != _labelsBaseline,
            -1f, Side.Right, Mood.Curious);
        Add("按 R 切换恒星的渲染模式（曝光度）。", () => _controller != null
            && _renderBaselineCaptured && _controller.StarRenderMode != _renderBaseline,
            -1f, Side.Right, Mood.Excited);
        Add("按 T 显示/隐藏运动轨迹。",
            () => SandboxTrail.Visible != _trailBaseline, -1f, Side.Right, Mood.Curious,
            () => _trailBaseline = SandboxTrail.Visible);
        Add("按 O 显示/隐藏预测轨道。",
            () => SandboxTrail.OrbitVisible != _orbitBaseline, -1f, Side.Right, Mood.Curious,
            () => _orbitBaseline = SandboxTrail.OrbitVisible);
        Add("按 G 显示/隐藏地面网格。",
            () => GridVisible() != _gridBaseline, -1f, Side.Right, Mood.Curious,
            () => _gridBaseline = GridVisible());
        Add("按空格键可以暂停/继续时间。", null, 3.5f, Side.Right, Mood.Neutral);
        Add("按 F3 会显示 FPS / CPU / GPU 状态条。", null, 3.5f, Side.Right, Mood.Curious);
        Add("鼠标左键拖动旋转视角，滚轮缩放，WASD 平移。按 ESC 可以关闭当前面板。",
            null, 4.5f, Side.Right, Mood.Happy);
        Add("就到这里啦，剩下的交给你自己探索！\n如果没听明白，可以按 F4，我随叫随到。",
            null, 0f, Side.Right, Mood.Happy);
    }

    private static double CurrentSpeed() {
        return SandboxTimeManager.Instance != null ? SandboxTimeManager.Instance.SpeedValue : 0.0;
    }

    private static bool SpeedAbove(double value) => CurrentSpeed() > value + 1e-6;
    private static bool SpeedBelow(double value) => CurrentSpeed() < value - 1e-6;

    private static bool GridVisible() {
        InfiniteGrid grid = UnityEngine.Object.FindObjectOfType<InfiniteGrid>();
        return grid != null && grid.IsVisible;
    }
    private bool ModulesVisited() {
        foreach (string n in new[] { "GeneralPanel", "MechanicsPanel", "Energy&StructureParameter" }) {
            // 同名模块在恒星/行星两个面板里都有，必须检查“所有同名物体里有没有激活的”。
            if (AnyActive(n)) _visitedModules.Add(n);
        }
        return _visitedModules.Count >= 3;
    }

    /// <summary>所有同名场景物体里，只要有一个处于激活状态就返回 true。</summary>
    private static bool AnyActive(string name) {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>()) {
            if (t == null || t.name != name) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            if (t.gameObject.activeInHierarchy) return true;
        }
        return false;
    }

    private void CaptureRenderBaseline() {
        if (_controller == null) return;
        _renderBaseline = _controller.StarRenderMode;
        _renderBaselineCaptured = true;
    }

    private void ApplyGravityDemo() {
        SetPanelActive("SettingsPanelSandbox", false);
        SandboxTimeManager t = SandboxTimeManager.Instance;
        if (t != null) {
            t.SetUnit(SandboxTimeUnit.DaysPerSecond);
            t.SetSpeed(7.0);
        }
        PullCameraToAu(6.0);
    }

    /// <summary>把相机沿视线方向拉远到指定 AU 之外。</summary>
    private void PullCameraToAu(double au) {
        Camera cam = Camera.main;
        if (cam == null) return;
        double units = au * CelestialScale.AuKm / CelestialScale.KmPerUnit;
        UPB3DCamera orbit = cam.GetComponent<UPB3DCamera>();
        if (orbit != null) {
            orbit.SetDistance((float)units);
            orbit.SyncNow();
        } else {
            cam.transform.position -= cam.transform.forward * (float)units;
        }
    }

    private void ReloadAquarius() {
        // 重新加载场景的同时把引力模拟打开。
        SandboxPhysicsSystem.GravityEnabled = true;
        _controller?.LoadDefaultScenario(SandboxSaveSystem.BuiltinAquarius, true);
        _bodyBaseline = SandboxPhysicsSystem.Bodies.Count;
        if (_controller != null) _labelsBaseline = _controller.LabelsVisible;
    }

    // ~~~ 流程 ~~~

    private void BeginIntro() {
        _intro = true;
        _tutorial = false;
        IsIntroActive = true;
        SetVisible(true);
        transform.SetAsLastSibling();
        SetText("玩家你好！我是向导水瓶座一，请问您需要帮助？\n按 Y 是，按 N 否。");
        Debug.Log("[Guide] intro shown, flag=" + PlayerPrefs.GetInt(IntroKey, 0));
        _side = Side.Right;
        _mood = Mood.Happy;
        PlaceGuide(force: true);
    }

    private void StartTutorial() {
        _intro = false;
        IsIntroActive = false;
        _tutorial = true;
        IsRunning = true;
        _bodyBaseline = SandboxPhysicsSystem.Bodies.Count;
        _labelsBaseline = _controller != null && _controller.LabelsVisible;
        _scenarioMark = SandboxSceneController.ScenarioLoadCount;
        SetVisible(true);
        transform.SetAsLastSibling();
        GoToStep(0);
    }

    private void GoToStep(int index) {
        _index = index;
        _stepTime = 0f;
        if (index >= _steps.Count) { Finish(completed: true); return; }

        Step step = _steps[index];
        _side = step.Side;
        _mood = step.Mood;
        SetText(step.Done == null ? step.Text + "\n（按 N 下一步）" : step.Text);
        PlaceGuide(force: true);
        step.OnEnter?.Invoke();
    }

    /// <summary>
    /// 结束向导。completed = true 表示完整走完了教学，才会记“已看过”；
    /// 中途按 I 退出、或在首次询问按 N，都不算做过，下次还会问。
    /// </summary>
    private void Finish(bool completed = false) {
        _tutorial = false;
        _intro = false;
        _notice = false;
        _prepPhase = 0;
        IsIntroActive = false;
        IsRunning = false;
        // 只要进了向导流程（走完 / 中途按 I / 一开始按 N 拒绝），都算做过。
        PlayerPrefs.SetInt(IntroKey, 1);
        PlayerPrefs.Save();
        Debug.Log("[Guide] finish completed=" + completed +
                  " flag=" + PlayerPrefs.GetInt(IntroKey, 0));
        SetVisible(false);
    }

    /// <summary>只隐藏画面，不关物体，保证 Update/F9 一直有效。</summary>
    public void SetVisible(bool visible) {
        if (visible && !gameObject.activeSelf) gameObject.SetActive(true);
        bool wasVisible = IsVisible;
        IsVisible = visible;
        if (wasVisible && !visible) OnHidden?.Invoke();
        if (_bodyImage != null) _bodyImage.enabled = visible;
        if (_face != null) _face.gameObject.SetActive(visible);
        if (_bubble != null) _bubble.gameObject.SetActive(visible);
    }

    public bool IsOpen => _bodyImage != null && _bodyImage.enabled;

    /// <summary>F4：不管当前在哪个场景，都要求用户先打开水瓶座一星系再演示。</summary>
    private void RestartTutorial() {
        Debug.Log("[Guide] F4 restart");
        _prepPhase = 1;
        _intro = false;
        _tutorial = false;
        IsIntroActive = false;
        IsRunning = false;
        SetVisible(true);
        transform.SetAsLastSibling();
        _side = Side.Right;
        _mood = Mood.Curious;
        SetText("这个演示需要在原始的水瓶座一星系里进行，请打开水瓶座一星系。\n" +
                "如果你改变主意不想学了，或者误触了 F4，按 I 就好。\n请先按 HOME 按钮。");
        PlaceGuide(force: true);
    }

    private void ShowNotice(string text) {
        _notice = true;
        _intro = false;
        SetVisible(true);
        transform.SetAsLastSibling();
        SetText(text);
        _side = Side.Right;
        _mood = Mood.Curious;
        PlaceGuide(force: true);
    }

    /// <summary>调试用：清掉“已看过向导”的标记，下次进入沙盘会重新弹出。</summary>
    public static void ResetIntroFlag() {
        PlayerPrefs.DeleteKey(IntroKey);
        PlayerPrefs.Save();
    }

    private void SetText(string text) {
        if (_bubbleText != null) _bubbleText.text = text;
    }

    private void Update() {
        // F4/F9 放在最前面，任何状态（含首次询问、教学进行中）都有效。
        if (Input.GetKeyDown(KeyCode.F4)) { RestartTutorial(); return; }
        if (Input.GetKeyDown(KeyCode.F9) && !_tutorial) {
            ResetIntroFlag();
            BeginIntro();
            return;
        }

        if (_intro) {
            if (Input.GetKeyDown(KeyCode.Y)) StartTutorial();
            else if (Input.GetKeyDown(KeyCode.N)) Finish(completed: false);
            return;
        }

        if (_prepPhase > 0) {
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1) {
                transform.SetAsLastSibling();
            }
            if (Input.GetKeyDown(KeyCode.I)) {
                _prepPhase = 0;
                _pendingRestart = false;
                SetVisible(false);
                return;
            }
            if (_prepPhase == 1) {
                if (IsPanelActive("HomePagePanel")) {
                    _prepPhase = 2;
                    _prepLoadMark = SandboxSceneController.ScenarioLoadCount;
                    SetText("双击“水瓶座一星系”开始演示，若当前存档对你很重要，请先保存。");
                    PlaceGuide(force: true);
                }
            } else if (_prepPhase == 2) {
                if (_controller != null && _controller.IsAquariusScenario &&
                    SandboxSceneController.ScenarioLoadCount > _prepLoadMark) {
                    _prepPhase = 0;
                    StartTutorial();
                }
            }
            return;
        }

        if (_notice) {
            if (Input.GetKeyDown(KeyCode.N)) { _notice = false; SetVisible(false); }
            return;
        }

        if (!_tutorial) {

            if (_pendingRestart && _controller != null && _controller.IsAquariusScenario) {
                _pendingRestart = false;
                StartTutorial();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.I)) { Finish(completed: false); return; }
        if (_index < 0 || _index >= _steps.Count) return;

        // 向导永远浮在最上层。
        if (transform.GetSiblingIndex() != transform.parent.childCount - 1) {
            transform.SetAsLastSibling();
        }

        // 用户在教学里切了新场景（比如 HOME 双击存入）时，把热学模拟恢复回去。
        if (SandboxSceneController.ScenarioLoadCount != _scenarioMark) {
            _scenarioMark = SandboxSceneController.ScenarioLoadCount;
            SandboxPhysicsSystem.ThermalEnabled = true;
            SandboxPhysicsSystem.ThermalRadiationLoss = true;
        }

        Step step = _steps[_index];
        _stepTime += Time.unscaledDeltaTime;

        // 不需要操作的对话：只能按 N 继续，不会自动跳。
        if (step.Done == null) {
            if (Input.GetKeyDown(KeyCode.N)) GoToStep(_index + 1);
        } else if (step.Done()) {
            GoToStep(_index + 1);
            return;
        }

        UpdateFace();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) UpdateBubble((canvas.transform as RectTransform).rect.size);
        if (OverlapsPanel(_anchor)) PlaceGuide(force: true);
    }

    // ~~~ 位置 ~~~

    private void PlaceGuide(bool force) {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null) return;

        Vector2 size = canvasRect.rect.size;
        Vector2 half = _face != null && _face.rect.size.sqrMagnitude > 1f
            ? _face.rect.size * 0.5f
            : _root.rect.size * 0.5f;

        Vector2 right = new(size.x * 0.5f - (EdgeMargin + half.x),
                            -size.y * 0.5f + (EdgeMargin + half.y));
        Vector2 left = new(-size.x * 0.5f + (EdgeMargin + half.x),
                           -size.y * 0.5f + (EdgeMargin + half.y));

        Vector2 target = _side == Side.Left ? left : right;
        if (OverlapsPanel(target)) {
            Vector2 other = _side == Side.Left ? right : left;
            if (!OverlapsPanel(other)) target = other;
        }
        _anchor = target;

        Vector2 toCenter = -_anchor.normalized;
        _faceOffset = toCenter * (FaceRadius * 0.6f);
        _root.anchoredPosition = _anchor - _faceOffset;
        UpdateFace();
        UpdateBubble(size);
    }

    private bool OverlapsPanel(Vector2 anchored) {
        Vector2 extent = _face != null && _face.rect.size.sqrMagnitude > 1f
            ? _face.rect.size : _root.rect.size;
        Rect guide = new(anchored - extent * 0.5f, extent);
        foreach (RectTransform panel in _panels) {
            if (panel == null || !panel.gameObject.activeInHierarchy) continue;
            Rect r = new(panel.anchoredPosition - panel.rect.size * 0.5f, panel.rect.size);
            if (guide.Overlaps(r)) return true;
        }
        return false;
    }

    private void UpdateFace() {
        if (_face == null) return;

        float bobAmp, bobSpeed;
        switch (_mood) {
            case Mood.Happy: bobAmp = 12f; bobSpeed = 2.4f; break;
            case Mood.Excited: bobAmp = 18f; bobSpeed = 4.2f; break;
            case Mood.Curious: bobAmp = 10f; bobSpeed = 1.6f; break;
            case Mood.Serious: bobAmp = 4f; bobSpeed = 1.0f; break;
            case Mood.Angry: bobAmp = 16f; bobSpeed = 5.0f; break;
            default: bobAmp = 7f; bobSpeed = 1.6f; break;
        }

        float wobble = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmp;
        Vector2 offset = _faceOffset + new Vector2(0f, wobble);
        if (offset.magnitude > FaceRadius) offset = offset.normalized * FaceRadius;
        _face.anchoredPosition = offset;

        bool faceRight = _anchor.x >= 0f;
        _face.localRotation = Quaternion.identity;
        _face.localScale = new Vector3(faceRight ? -1f : 1f, 1f, 1f);

        if (_faceImage != null) {
            // 只换表情贴图，保持原色（不要把眼睛染色）。
            _faceImage.color = Color.white;
            Texture2D tex = ExpressionTexture();
            if (tex != null && _faceImage.texture != tex) _faceImage.texture = tex;
        }
    }

    private Texture2D ExpressionTexture() {
        string name = _mood switch {
            Mood.Happy => "smile",
            Mood.Excited => "smile_eyes_opend",
            Mood.Curious => _anchor.x >= 0f ? "left_confused" : "right_confused",
            Mood.Serious => "bored",
            Mood.Angry => "angry",
            _ => "Normal",
        };
        if (_expressions.TryGetValue(name, out Texture2D tex)) return tex;
        _expressions.TryGetValue("Normal", out Texture2D fallback);
        return fallback;
    }

    private void UpdateBubble(Vector2 canvasSize) {
        if (_bubble == null) return;
        // 统一放在向导正上方 200px（相对根节点）。
        _bubble.anchoredPosition = new Vector2(0f, BubbleAbove);
    }
}
