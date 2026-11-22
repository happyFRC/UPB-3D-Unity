using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// SandboxScene 的运行时控制器。
///
/// 数据流：
///   Resources/Data/Stars.json -> 左侧创建面板（每页 6 个）
///   点击某个恒星 -> 进入放置模式（相机拉到 5 倍直径，半透明白球跟随鼠标）
///   在网格平面点击 -> 创建一个恒星
///   单击已创建恒星 -> 显示 ButtonExpandStarParameter
///   双击已创建恒星 -> 相机跟随
///   参数面板里质量/金属度/年龄可编辑，半径/温度/密度/光度只读（SSE 模型计算）
/// </summary>
public class SandboxSceneController : MonoBehaviour {
    [Serializable]
    public class StarData {
        public string name;
        public double mass;
        public double metallicity;
        public double age;
        public double temp_review;
        public double rotationPeriodDays = 25.38;
    }

    [Serializable]
    public class StarCatalog {
        public StarData[] stars;
    }

    private const int PageSize = 6;
    private const float ClickDragThreshold = 8f;
    private const float DoubleClickSeconds = 0.3f;
    private const float ScreenSelectRadius = 30f;
    private const float LabelOffsetPixels = 15f;
    private const float LabelWidth = 220f;
    private const float LabelHeight = 36f;
    private const int LabelSortingOrder = -5;

    private static readonly Color PanelBg = new(0.045f, 0.06f, 0.085f, 1f);
    private static readonly Color RowBg = new(0.10f, 0.13f, 0.18f, 1f);
    private static readonly Color CardBg = new(0.12f, 0.16f, 0.22f, 1f);
    private static readonly Color CardSelectedBg = new(0.20f, 0.36f, 0.52f, 1f);
    private static readonly Color ButtonBg = new(0.16f, 0.22f, 0.30f, 1f);
    private static readonly Color Accent = new(0.36f, 0.68f, 1f, 1f);
    private static readonly Color TextMain = new(0.94f, 0.96f, 1f, 1f);
    private static readonly Color TextDim = new(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color InputBg = new(0.08f, 0.10f, 0.14f, 1f);

    private Camera _camera;
    private UPB3DCamera _orbitCamera;
    private Transform _canvas;
    private Transform _grid;

    private GameObject _createPanel;
    private GameObject _homePanel;
    private SandboxHomePanel _homePage;
    private Button _openHomeButton;
    private TMP_InputField _starOrbitAngleInput;
    private TMP_InputField _planetOrbitAngleInput;
    private string _currentScenarioName = "水瓶座一系";
    private string _activeSavePath;
    private string _activeSaveName;
    private bool _activeBuiltin;

    private sealed class PanelEntry {
        public GameObject root;
        public Action close;
    }

    private readonly List<PanelEntry> _panelStack = new();
    private GameObject _parameterPanel;
    private GameObject _planetParameterPanel;
    private GameObject _templateStar;
    private Vector3 _templatePosition = Vector3.zero;
    private Material _templateMaterial;

    private Button _openCreateButton;
    private Button _shutCreateButton;
    private Button _nextPageButton;
    private Button _lastPageButton;
    private Button _starCategoryButton;
    private Button _planetCategoryButton;
    private Button _dwarfCategoryButton;
    private Button _moonCategoryButton;
    private Button _eggCategoryButton;
    private Button _generalButton;
    private Button _mechanicsButton;
    private Button _energyStructureButton;
    private GameObject _generalPanel;
    private GameObject _mechanicsPanel;
    private GameObject _energyPanel;
    private TMP_Text _generalVelocityValue;
    private TMP_Text _mechCentralObject;
    private TMP_Text _mechForce;
    private TMP_Text _mechKineticValue;
    private TMP_Text _mechAngularKineticValue;
    private TMP_Text _mechEccentricityValue;
    private TMP_Text _mechEquatorVelocityValue;
    private TMP_Text _mechSemiMajorAxisValue;
    private TMP_Text _mechOrbitalPeriodValue;
    private TMP_Text _mechVelocityValue;
    private TMP_Text _mechMomentumValue;
    private TMP_Text _mechAngularMomentumValue;
    private TMP_InputField _mechXMotionInput;
    private TMP_InputField _mechYMotionInput;
    private TMP_InputField _mechZMotionInput;
    private TMP_InputField _mechSpinPeriodInput;
    private TMP_Text _energyCorePressureValue;
    private TMP_Text _energyCoreTemperatureValue;
    private TMP_Text _energyBindingValue;
    private TMP_Text _energyFusionValue;
    private TMP_Text _energyAtmosphereHeatingValue;
    private TMP_Text _energyRadiationHeatingValue;
    private TMP_Text _energyThermalRadiationValue;
    private TMP_Text _energyTotalInputValue;
    private TMP_Text _energyWavePowerValue;
    private TMP_InputField _energyWaveLowInput;
    private TMP_InputField _energyWaveHighInput;

    private enum StarModule {
        General,
        Mechanics,
        EnergyStructure
    }

    private StarModule _starModule = StarModule.General;
    private bool _mechanicsScrollerReady;
    private bool _starEnergyScrollerReady;
    private bool _planetMechanicsScrollerReady;
    private bool _planetEnergyScrollerReady;
    private readonly Dictionary<string, UnitButton> _starRowUnits = new();

    private GameObject _planetGeneralPanel;
    private GameObject _planetMechanicsPanel;
    private GameObject _planetEnergyPanel;
    private Button _planetGeneralButton;
    private Button _planetMechanicsButton;
    private Button _starLockPositionButton;
    private Button _planetLockPositionButton;
    private Button _planetEnergyButton;
    private PlanetModule _planetModule = PlanetModule.General;
    private TMP_InputField _planetGreenhouseInput;
    private UnitButton _planetDistanceUnit;
    private TMP_Text _planetDistanceValue;
    private TMP_Text _planetWavePowerValue;
    private TMP_InputField _planetWaveLowInput;
    private TMP_InputField _planetWaveHighInput;
    private TMP_Text _generalDistanceValue;
    private TMP_Text _planetTotalInputValue;
    private TMP_Text _planetThermalRadiationValue;
    private TMP_Text _planetFusionValue;
    private TMP_Text _planetRadiationHeatingValue;
    private TMP_Text _planetAtmosphereHeatingValue;
    private TMP_Text _planetBindingValue;
    private TMP_Text _planetBindingValue2;
    private TMP_Text _planetScaleHeightValue;
    private TMP_Text _planetTemperatureGradientValue;
    private TMP_Text _planetMechVelocity;
    private TMP_Text _planetMechMomentum;
    private TMP_Text _planetMechKinetic;
    private TMP_Text _planetMechEquator;
    private TMP_Text _planetMechCentral;
    private TMP_Text _planetMechEccentricity;
    private TMP_Text _planetMechAngularMomentum;
    private TMP_Text _planetMechAngularKinetic;
    private TMP_Text _planetMechSemiMajor;
    private TMP_Text _planetMechOrbitalPeriod;
    private TMP_Text _planetMechForce;
    private TMP_InputField _planetMechX;
    private TMP_InputField _planetMechY;
    private TMP_InputField _planetMechZ;
    private TMP_InputField _planetMechSpin;
    private TMP_InputField _planetMechSpinAngle;
    private UnitButton _planetMechSpinAngleUnit;

    private enum PlanetModule {
        General,
        Mechanics,
        EnergyStructure
    }

    private Button _expandParameterButton;
    private Button _shutParameterPanelButton;
    private Button _shutStarParameterButton;
    private Button _shutPlanetParameterButton;
    private Button _orbitModeButton;
    private Button _staticModeButton;

    private enum PlacementMotion {
        Orbiting,
        Static,
        Launch
    }

    private PlacementMotion _placementMotion = PlacementMotion.Orbiting;

    // 恒星渲染模式：0 = 默认，1 = 黑体光球（R 键切换，记在 PlayerPrefs）
    private const string StarRenderModeKey = "sandbox.starRenderMode";
    private int _starRenderMode;

    private readonly Button[] _cardButtons = new Button[PageSize];
    private readonly TMP_Text[] _cardNames = new TMP_Text[PageSize];
    private readonly Image[] _cardImages = new Image[PageSize];
    private readonly RawImage[] _cardPlates = new RawImage[PageSize];
    private readonly Image[] _cardDots = new Image[PageSize];
    private readonly RawImage[] _cardTextures = new RawImage[PageSize];

    private TMP_Text _pageText;
    private TMP_Text _titleContent;
    private TMP_InputField _nameInput;
    private TMP_InputField _massInput;
    private TMP_InputField _ageInput;
    private TMP_InputField _metalInput;

    private UnitButton _massUnit;
    private UnitButton _ageUnit;
    private UnitButton _radiusUnit;
    private UnitButton _temperatureUnit;
    private UnitButton _densityUnit;
    private UnitButton _luminosityUnit;

    // 发射模式：Button_Shoot 里的速度面板 / 输入框 / 单位切换
    private Button _shootModeButton;
    private GameObject _shootSpeedPanel;
    private TMP_InputField _shootSpeedInput;
    private UnitButton _shootSpeedUnit;
    private GameObject _shootUnitSwitch;

    private TMP_Text _massLabel;
    private TMP_Text _ageLabel;
    private TMP_Text _metalLabel;
    private TMP_Text _radiusValue;
    private TMP_Text _temperatureValue;
    private TMP_Text _densityValue;
    private TMP_Text _luminosityValue;

    private TMP_Text _planetTitleContent;
    private TMP_InputField _planetNameInput;
    private TMP_InputField _planetMassInput;
    private TMP_InputField _planetTempInput;
    private TMP_InputField _planetAtmosphereInput;
    private TMP_InputField _planetWaterDepthInput;
    private TMP_InputField _planetEnergyAtmosphereInput;
    private TMP_InputField _planetConcentrationInput;
    private string _planetConcentrationRowName;
    private TMP_InputField _planetFreezePointInput;
    private TMP_InputField _planetBoilPointInput;
    private TMP_Text _planetFreezePointText;
    private TMP_Text _planetBoilPointText;
    private string _planetFreezePointRowName;
    private string _planetBoilPointRowName;

    private UnitButton _planetMassUnit;
    private UnitButton _planetTempUnit;
    private UnitButton _planetAtmosphereUnit;
    private UnitButton _planetRadiusUnit;
    private UnitButton _planetDensityUnit;
    private UnitButton _planetGravityUnit;
    private UnitButton _planetPressureUnit;

    private TMP_Text _planetMassLabel;
    private TMP_Text _planetTempLabel;
    private TMP_Text _planetAtmosphereLabel;
    private TMP_Text _planetRadiusValue;
    private TMP_Text _planetDensityValue;
    private TMP_Text _planetGravityValue;
    private TMP_Text _planetPressureValue;

    private StarCatalog _catalog;
    private PlanetCatalog _planetCatalog;
    private PlanetCatalog _dwarfCatalog;
    private PlanetCatalog _moonCatalog;
    private PlanetCatalog _eggCatalog;
    private CreateCategory _category = CreateCategory.Star;
    private int _page;
    private int _starPage;
    private int _planetPage;
    private int _dwarfPage;
    private int _moonPage;
    private int _eggPage;
    private StarData _placementEntry;
    private SandboxStarSnapshot _placementSnapshot;
    private PlanetData _placementPlanetData;
    private SandboxPlanetSnapshot _placementPlanetSnapshot;
    private GameObject _preview;
    private LineRenderer _followGuideLine;
    private TMP_Text _followGuideText;
    private RectTransform _followGuideRect;
    private Material _previewMaterial;
    private GameObject _placementPivot;
    private Plane _placementPlane;

    // 搜索框 IF_Search：只影响卡片列表的显示，不改目录数据本身。
    private TMP_InputField _searchInput;
    private string _searchQuery = string.Empty;
    private readonly List<int> _starResults = new();
    private readonly List<int> _planetResults = new();
    private readonly List<int> _dwarfResults = new();
    private readonly List<int> _moonResults = new();
    private readonly List<int> _eggResults = new();

    private SandboxSaveChangePanel _saveChangePanel;
    private SandboxSceneDescription _sceneDescription;
    /// <summary>场景默认镜头距离（AU）；0 表示沿用 FinishScenarioLoad 的按恒星大小估算。</summary>
    private double _startDistanceAu;
    /// <summary>场景默认镜头距离（km）；0 表示不用。</summary>
    private double _startDistanceKm;
    /// <summary>被向导挡住的场景介绍，等向导消失后补弹。</summary>
    private string _pendingIntroId;
    private SandboxGuide _guide;
    private readonly Dictionary<string, SceneIntro> _sceneIntros = new();
    private SandboxSettingsPanel _settingsPanel;

    public static SandboxSceneController Instance { get; private set; }

    private readonly List<SandboxStar> _stars = new();
    private readonly List<SandboxPlanet> _planets = new();
    private SandboxStar _selectedStar;
    private SandboxPlanet _selectedPlanet;

    private enum CreateCategory {
        Star,
        Planet,
        DwarfPlanet,
        Moon,
        EasterEgg
    }

    private Canvas _labelCanvas;
    private RectTransform _labelCanvasRect;
    private TMP_FontAsset _labelFont;
    private readonly List<BodyLabel> _bodyLabels = new();

    // L 键：天体名字标签显隐
    private bool _labelsVisible = true;

    // F3 状态条：FPS / 本机 CPU / 本机 GPU（显示在左上角）
    private bool _statsVisible;
    private string _statsText = "";
    private float _statsFps = 60f;
    private float _statsCpu = -1f;
    private float _statsGpu = -1f;
    private float _statsCpuTimer;
    private GUIStyle _statsStyle;
    private Texture2D _statsBackground;
    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
    private Sprite _circleSprite;
    private Texture2D _circleTexture;

    private Vector2 _mouseDownPosition;
    private bool _mouseDownOverUI;
    private float _lastClickTime = -999f;
    private SandboxStar _lastClickStar;
    private SandboxPlanet _lastClickPlanet;

    private void Start() {
        Instance = this;
        SandboxFloatingOrigin.Reset();
        SandboxWorldScale.Reset();
        SandboxPhysicsSystem.Reset();
        _camera = Camera.main;
        if (_camera == null) _camera = FindObjectOfType<Camera>();
        if (_camera == null) {
            Debug.LogError("[Sandbox] 找不到 Main Camera");
            enabled = false;
            return;
        }
        _orbitCamera = _camera.GetComponent<UPB3DCamera>();
        if (_orbitCamera != null) _orbitCamera.EnableSandboxZoom(1f);

        _canvas = FindCanvas();
        if (_canvas == null) {
            Debug.LogError("[Sandbox] 找不到 Canvas");
            enabled = false;
            return;
        }

        DisableOldEvolutionController();
        CacheTemplateStar();
        CacheUi();
        UnitPickerPanel.InitializeExisting();
        if (_homePanel != null) {
            _homePage = _homePanel.GetComponent<SandboxHomePanel>();
            if (_homePage == null) _homePage = _homePanel.AddComponent<SandboxHomePanel>();
            _homePage.Initialize(this);
        }
        SetUpSaveChangePanel();
        SetUpSceneDescription();
        SetUpGuide();
        SandboxAdaptiveUI.Install(_canvas);
        LoadCatalog();
        LoadPlanetCatalog();
        SetupSearch();
        EnsureCatalogTemperatures();
        SetupCardDots();
        SetupGrid();
        SetupPreview();
        SetupPlacementPivot();
        WireUi();
        SetupFonts();
        SetupLabelCanvas();
        StyleUi();
        HideSceneExtras();

        SetCreatePanelOpen(false);
        if (_homePanel != null) _homePanel.SetActive(false);
        if (_parameterPanel != null) _parameterPanel.SetActive(false);
        if (_planetParameterPanel != null) _planetParameterPanel.SetActive(false);
        if (_expandParameterButton != null) _expandParameterButton.gameObject.SetActive(false);
        if (_titleContent != null) _titleContent.text = "恒星";
        if (_preview != null) _preview.SetActive(false);
        _category = CreateCategory.Star;
        ShowPage(0);
        // 启动时浮动原点刚在 Start 里清零，初始场景不需要再做“载入收尾”，
        // 这样可以保持编辑器里调好的默认视角。
        _starRenderMode = PlayerPrefs.GetInt(StarRenderModeKey, 0);

        _settingsPanel = gameObject.AddComponent<SandboxSettingsPanel>();
        _settingsPanel.Initialize(this);
        LoadDefaultScenario(SandboxSaveSystem.BuiltinAquarius, false);
        _homePage?.Hide();
        Invoke(nameof(EnsureDefaultScenarioLoaded), 0.25f);
    }

    private void EnsureDefaultScenarioLoaded() {
        if (SandboxPhysicsSystem.Bodies.Count == 0) {
            LoadDefaultScenario(SandboxSaveSystem.BuiltinAquarius, false);
            _homePage?.Hide();
        } else if (_currentScenarioName == "水瓶座一系") {
            // SandboxTimeManager.Start 可能在控制器 Start 之后执行，这里再压一次默认流速。
            SetTimeSpeed(35.0, SandboxTimeUnit.DaysPerSecond);
        }
    }

    private void Update() {
        TickStatsOverlay();
        UpdatePlacementPreview();
        HandlePointer();
        HandleDeleteKey();
        HandleCancel();
        HandlePauseKey();
        HandleTrailKey();
        UpdatePhysics();
        MaintainFocusedOrigin();
        UpdateSelectedStarLiveFields();
        UpdateSelectedPlanetTemperatureDisplay();
        UpdatePlanetLighting();
    }

    private void UpdateSelectedStarLiveFields() {
        if (_selectedStar == null || _selectedStar.snapshot == null) return;
        if (_parameterPanel == null || !_parameterPanel.activeSelf) return;

        SandboxStar star = _selectedStar;
        SandboxStarSnapshot snapshot = star.snapshot;

        SetInputIfNotFocused(_nameInput, star.starName);
        SetInputIfNotFocused(_massInput, FormatMass(star.mass, MassUnit));
        SetInputIfNotFocused(_ageInput, FormatAge(star.ageMyr, AgeUnit));
        SetInputIfNotFocused(_metalInput, FormatValue(star.metallicity));

        if (_titleContent != null) _titleContent.text = star.starName;
        if (_radiusValue != null) {
            _radiusValue.text = FormatRadius(snapshot.radiusRsun, RadiusUnit);
        }
        if (_temperatureValue != null) {
            _temperatureValue.text = FormatTemperature(snapshot.temperatureK, TemperatureUnit);
        }
        if (_densityValue != null) {
            _densityValue.text = FormatDensity(snapshot.densityGcm3, DensityUnit);
        }
        if (_luminosityValue != null) {
            _luminosityValue.text = FormatLuminosity(snapshot.luminosityLsun, LuminosityUnit);
        }

        RefreshStarDerivedFields(star);
    }

    private void UpdateSelectedPlanetTemperatureDisplay() {
        if (_selectedPlanet == null) return;
        if (_planetParameterPanel == null || !_planetParameterPanel.activeInHierarchy) return;

        if (_selectedPlanet.snapshot != null) {
            ApplyAutoPlanetUnits(_selectedPlanet.snapshot, _selectedPlanet.temperatureK,
                _selectedPlanet.atmosphereMassEarth);
        }

        // 距离：选中天体到玩家视角（主相机）的距离。
        if (_planetDistanceValue != null) {
            double distanceM = CameraDistanceMeters(_selectedPlanet.transform.position);
            _planetDistanceUnit?.ApplyAuto(distanceM);
            SandboxUnitDefinition distanceDef = _planetDistanceUnit != null
                ? SandboxUnits.Definition("length", _planetDistanceUnit.GetUnit())
                : null;
            _planetDistanceValue.text = distanceDef != null
                ? FormatValue(SandboxUnits.FromBase(distanceM, distanceDef))
                : FormatValue(distanceM / 1000.0);
        }

        SandboxPlanet planet = _selectedPlanet;
        SandboxPlanetSnapshot snapshot = planet.snapshot;

        if (_planetTempInput != null && !_planetTempInput.isFocused) {
            string text = FormatTemperature(planet.temperatureK, PlanetTempUnit);
            if (_planetTempInput.text != text) _planetTempInput.text = text;
        }

        if (_planetMassInput != null && !_planetMassInput.isFocused) {
            string massText = FormatPlanetMass(planet.massEarth, PlanetMassUnit);
            if (_planetMassInput.text != massText) _planetMassInput.text = massText;
        }

        if (_planetGreenhouseInput != null && !_planetGreenhouseInput.isFocused) {
            _planetGreenhouseInput.text = FormatValue(planet.greenhouseFactor);
        }

        if (snapshot == null) return;
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(planet);
        double radiusM = planet.radiusEarth * SandboxPlanetModel.EarthRadiusKm * 1000.0;
        double area = 4.0 * Math.PI * radiusM * radiusM;
        double thermal = SandboxPhysicsSystem.StefanBoltzmann *
                         Math.Pow(planet.temperatureK, 4.0) * area;
        double greenhouse = Math.Clamp(planet.greenhouseFactor, 0.0, 1.0);

        // 能量面板要跟设置面板里的热学开关保持一致：关掉的通道直接显示 0。
        bool thermalEnabled = SandboxPhysicsSystem.ThermalEnabled;
        double atmosphereHeat = thermalEnabled && SandboxPhysicsSystem.ThermalAtmosphereHeating
            ? thermal * greenhouse : 0.0;
        double radiationHeat = thermalEnabled && SandboxPhysicsSystem.ThermalReceiveRadiation &&
                               body != null ? body.lastAbsorbedPowerW : 0.0;
        double emittedPower = thermalEnabled && SandboxPhysicsSystem.ThermalRadiationLoss
            ? thermal : 0.0;
        // 气态巨行星（>10 M_Jup）的“核聚变功率”：L = (M/1000)^3.6 L☉。
        double massJup = planet.massEarth / SandboxPlanetMassModel.JupiterMassEarth;
        // SpecialPlanets（心大星Ab 这类）不走核聚变公式，功率恒为 0。
        double fusion = !planet.special && massJup > SandboxPlanetMassModel.GasGiantMinJup
            ? Math.Pow(massJup / 1000.0, 3.6) * SandboxStarModel.SunLuminosityW
            : 0.0;
        double totalInput = fusion + radiationHeat + atmosphereHeat;

        // 波段功率：行星按黑体处理，公式与恒星一致。
        if (_planetWavePowerValue != null) {
            double lowNm = ReadWaveLengthNm(_planetEnergyPanel, _planetWaveLowInput,
                "IF_Type02WaveLengthLow", 400.0);
            double highNm = ReadWaveLengthNm(_planetEnergyPanel, _planetWaveHighInput,
                "IF_Type02_WaveLengthup", 760.0);
            double bandPower = SandboxSpectrum.BandPowerW(planet.temperatureK, radiusM, lowNm, highNm);
            _planetWavePowerValue.text = FormatUnitValue(bandPower, "power",
                _planetEnergyPanel, "AutoParameter_WavePower", FormatValue(bandPower));
        }

        if (_planetFusionValue != null) {
            _planetFusionValue.text = FormatUnitValue(fusion, "power",
                _planetEnergyPanel, "AutoParameter_FusionPower", FormatValue(fusion));
        }
        if (_planetRadiationHeatingValue != null) {
            _planetRadiationHeatingValue.text = FormatUnitValue(radiationHeat, "power",
                _planetEnergyPanel, "AutoParameter_RadiationHeatingPower",
                FormatValue(radiationHeat));
        }
        if (_planetAtmosphereHeatingValue != null) {
            _planetAtmosphereHeatingValue.text = FormatUnitValue(atmosphereHeat, "power",
                _planetEnergyPanel, "AutoParameter_AtmosphereHeatingPower (1)",
                FormatValue(atmosphereHeat));
        }
        if (_planetThermalRadiationValue != null) {
            _planetThermalRadiationValue.text = FormatUnitValue(emittedPower, "power",
                _planetEnergyPanel, "AutoParameter_ThermalRadiationPower",
                FormatValue(emittedPower));
        }
        if (_planetTotalInputValue != null) {
            _planetTotalInputValue.text = FormatUnitValue(totalInput, "power",
                _planetEnergyPanel, "AutoParameter_TotalInputPower",
                FormatValue(totalInput));
        }

        // 引力结合能：3GM²/5R。面板里有两行同名“引力结合能”（场景复制遗留），
        // 两行都接到同一个数值，避免出现空白框。
        double planetMassKg = planet.massEarth * SandboxPhysicsSystem.EarthMassKg;
        double bindingEnergy = radiusM > 0.0
            ? 3.0 * SandboxPhysicsSystem.GravitationalConstant *
              planetMassKg * planetMassKg / (5.0 * radiusM)
            : 0.0;
        if (_planetBindingValue != null) {
            _planetBindingValue.text = FormatUnitValue(bindingEnergy, "energy",
                _planetEnergyPanel,
                "AutoParameter_Gravitational_Combinantion_Energy",
                FormatValue(bindingEnergy));
        }
        if (_planetBindingValue2 != null) {
            _planetBindingValue2.text = FormatUnitValue(bindingEnergy, "energy",
                _planetEnergyPanel,
                "AutoParameter_Gravitational_Combinantion_Energy (2)",
                FormatValue(bindingEnergy));
        }
        if (_planetScaleHeightValue != null) {
            double gravity = Math.Max(0.01,
                snapshot.gravityG * SandboxPlanetModel.EarthGravity);
            double scaleHeight = 287.05 * planet.temperatureK / gravity;
            _planetScaleHeightValue.text = FormatUnitValue(scaleHeight, "length",
                _planetEnergyPanel, "AutoParameter_Centural_Pressure",
                FormatValue(scaleHeight / 1000.0));
        }
        if (_planetTemperatureGradientValue != null) {
            double gravity = Math.Max(0.01,
                snapshot.gravityG * SandboxPlanetModel.EarthGravity);
            double gradient = gravity / 1004.0 * 1000.0;
            _planetTemperatureGradientValue.text = FormatValue(gradient);
        }

        RefreshPlanetMechanicsFields(planet, body);

        if (_planetRadiusValue != null) {
            _planetRadiusValue.text = FormatPlanetRadius(snapshot.radiusEarth, PlanetRadiusUnit);
        }
        if (_planetDensityValue != null) {
            _planetDensityValue.text = FormatDensity(snapshot.densityGcm3, PlanetDensityUnit);
        }
        if (_planetGravityValue != null) {
            _planetGravityValue.text = FormatPlanetGravity(snapshot.gravityG, PlanetGravityUnit);
        }
        if (_planetPressureValue != null) {
            _planetPressureValue.text = FormatPlanetPressure(snapshot.pressureAtm, PlanetPressureUnit);
        }
    }

    private void RefreshPlanetMechanicsFields(SandboxPlanet planet, SandboxPhysicsBody body) {
        if (body == null) return;

        SetInputIfNotFocused(_planetOrbitAngleInput,
            FormatValue(body.orbitInclinationDeg));
        double speed = body.velocityMps.Magnitude;
        if (_planetMechVelocity != null) {
            _planetMechVelocity.text = FormatUnitValue(speed, "speed",
                _planetMechanicsPanel, "AutoParameter_Velocity", FormatValue(speed / 1000.0));
        }
        SetInputIfNotFocused(_planetMechX, FormatUnitValue(body.velocityMps.x, "speed",
            _planetMechanicsPanel, "IF_Type02_XMotion", FormatValue(body.velocityMps.x / 1000.0)));
        SetInputIfNotFocused(_planetMechY, FormatUnitValue(body.velocityMps.y, "speed",
            _planetMechanicsPanel, "IF_Type02_YMotion", FormatValue(body.velocityMps.y / 1000.0)));
        SetInputIfNotFocused(_planetMechZ, FormatUnitValue(body.velocityMps.z, "speed",
            _planetMechanicsPanel, "IF_Type02_ZMotion", FormatValue(body.velocityMps.z / 1000.0)));
        if (_planetMechKinetic != null) {
            _planetMechKinetic.text = FormatUnitValue(body.KineticEnergyJ, "energy",
                _planetMechanicsPanel, "AutoParameter_Knetic", FormatValue(body.KineticEnergyJ));
        }
        if (_planetMechMomentum != null) {
            _planetMechMomentum.text = FormatValue(body.massKg * speed);
        }

        SetInputIfNotFocused(_planetMechSpin, FormatUnitValue(
            planet.rotationPeriodDays * 86400.0, "time",
            _planetMechanicsPanel, "IF_Type02_SpinPeriod",
            FormatValue(planet.rotationPeriodDays)));

        SetInputIfNotFocused(_planetMechSpinAngle, FormatUnitValue(
            planet.axialTiltDeg, "angle",
            _planetMechanicsPanel, "IF_Type02_SpinAngle",
            FormatValue(planet.axialTiltDeg)));

        UnitButton spinUnit = GetRowUnit(_planetMechanicsPanel, "IF_Type02_SpinPeriod");
        SandboxUnitDefinition spinDef = spinUnit != null
            ? SandboxUnits.Definition("time", spinUnit.GetUnit())
            : null;
        double spinSeconds = 0.0;
        if (_planetMechSpin != null && TryParse(_planetMechSpin.text, out double spinValue) &&
            spinValue > 0.0) {
            spinSeconds = spinDef != null
                ? SandboxUnits.ToBase(spinValue, spinDef)
                : spinValue * 86400.0;
        }

        if (spinSeconds > 0.0 && body.radiusM > 0.0) {
            double omega = 2.0 * Math.PI / spinSeconds;
            double inertia = 0.4 * body.massKg * body.radiusM * body.radiusM;
            if (_planetMechAngularKinetic != null) {
                _planetMechAngularKinetic.text = FormatUnitValue(
                    0.5 * inertia * omega * omega, "energy",
                    _planetMechanicsPanel, "AutoParameter_Angular_Knetic",
                    FormatValue(0.5 * inertia * omega * omega));
            }
            if (_planetMechEquator != null) {
                _planetMechEquator.text = FormatUnitValue(body.radiusM * omega, "speed",
                    _planetMechanicsPanel, "AutoParameter_Equator_Velocity",
                    FormatValue(body.radiusM * omega / 1000.0));
            }
            if (_planetMechAngularMomentum != null) {
                _planetMechAngularMomentum.text = FormatValue(inertia * omega);
            }
        } else {
            if (_planetMechAngularKinetic != null) _planetMechAngularKinetic.text = "—";
            if (_planetMechEquator != null) _planetMechEquator.text = "—";
            if (_planetMechAngularMomentum != null) _planetMechAngularMomentum.text = "—";
        }

        SandboxPhysicsBody target = body.orbitTarget;
        if (target == null) {
            if (_planetMechCentral != null) _planetMechCentral.text = "无";
            if (_planetMechEccentricity != null) _planetMechEccentricity.text = "—";
            if (_planetMechSemiMajor != null) _planetMechSemiMajor.text = "—";
            if (_planetMechOrbitalPeriod != null) _planetMechOrbitalPeriod.text = "—";
            if (_planetMechForce != null) _planetMechForce.text = "0.0";
            return;
        }

        if (_planetMechCentral != null) _planetMechCentral.text = target.name;
        SandboxDVec3 relPos = body.positionM - target.positionM;
        SandboxDVec3 relVel = body.velocityMps - target.velocityMps;
        double r = relPos.Magnitude;
        double mu = SandboxPhysicsSystem.GravitationalConstant *
                    (target.massKg + body.massKg);
        if (r <= 0.0 || mu <= 0.0) return;

        SandboxDVec3 hVec = CrossD(relPos, relVel);
        double h = hVec.Magnitude;
        double energy = 0.5 * relVel.SqrMagnitude - mu / r;
        double semiMajor = 0.0;
        double eccentricity = 0.0;
        double periodYears = 0.0;
        if (energy < -1e-30) {
            semiMajor = -mu / (2.0 * energy);
            double e2 = 1.0 + 2.0 * energy * h * h / (mu * mu);
            eccentricity = Math.Sqrt(Math.Max(0.0, e2));
            double periodSeconds = 2.0 * Math.PI *
                Math.Sqrt(semiMajor * semiMajor * semiMajor / mu);
            periodYears = periodSeconds / (86400.0 * 365.2425);
        }

        if (_planetMechEccentricity != null) {
            _planetMechEccentricity.text = energy < 0.0 ? FormatValue(eccentricity) : "双曲";
        }
        if (_planetMechSemiMajor != null) {
            _planetMechSemiMajor.text = energy < 0.0
                ? FormatUnitValue(semiMajor, "length", _planetMechanicsPanel,
                    "AutoParameter_Semi_Major_Axis",
                    FormatValue(semiMajor / CelestialScale.AuKm / 1000.0))
                : "—";
        }
        if (_planetMechOrbitalPeriod != null) {
            _planetMechOrbitalPeriod.text = energy < 0.0
                ? FormatUnitValue(periodYears * 31556952.0, "time",
                    _planetMechanicsPanel, "AutoParameter_Orbital_Period",
                    FormatValue(periodYears))
                : "—";
        }
        if (_planetMechForce != null) {
            double force = GravityForceN(body);
            _planetMechForce.text = FormatUnitValue(force, "force",
                _planetMechanicsPanel, "AutoParameter_GeneralForceReceived",
                FormatValue(force));
        }
    }

    private void UpdatePhysics() {
        if (SandboxTimeManager.Instance == null) return;
        double simulated = SandboxPhysicsSystem.Tick(
            SandboxTimeManager.Instance.DeltaSimulationSeconds);
        // 物理受子步上限限制时少走的那部分，让时钟在下一帧扣掉，
        // 避免“日期过了 600 年、行星却没转完一圈”。
        SandboxTimeManager.Instance.ReportSimulatedSeconds(simulated);
    }

    private Transform _lastFollowedTarget;

    /// <summary>
    /// 跟随中的天体每帧都重新作为浮动原点，避免它绕行后 render 坐标变大，
    /// 近距离时出现 Transform 浮点精度抖动。
    ///
    /// 判断依据是“相机实际跟随的目标”（_orbitCamera.target），
    /// 不是“当前选中的天体”——这样单击选中其它天体时，
    /// 坐标系/网格不会被切到被单击的天体上，只有双击跟随才切。
    /// </summary>
    private void MaintainFocusedOrigin() {
        if (_orbitCamera == null) return;

        Transform followed = _orbitCamera.target;
        if (followed != null && followed != _lastFollowedTarget) {
            // 临时诊断：只在“相机跟随目标发生变化”时打一行，方便确认单击是否误触发跟随。
            Debug.Log($"[Follow] 相机跟随目标 -> {followed.name}");
        }
        _lastFollowedTarget = followed;
        // WASD 速度也按“相机跟随的目标”算，而不是按单击选中的天体；
        // 没跟随时传 null，摄像机回退到“离系统原点距离”。
        _orbitCamera.SetMovementReference(followed);
        if (followed == null) return;

        SandboxPlanet followedPlanet = null;
        for (int i = 0; i < _planets.Count; i++) {
            SandboxPlanet planet = _planets[i];
            if (planet != null && planet.transform == followed) {
                followedPlanet = planet;
                break;
            }
        }

        if (followedPlanet != null) {
            RebaseTo(followedPlanet.logicalX, followedPlanet.logicalY,
                followedPlanet.logicalZ);
            _orbitCamera.SyncNow();
            return;
        }

        for (int i = 0; i < _stars.Count; i++) {
            SandboxStar star = _stars[i];
            if (star != null && star.transform == followed) {
                RebaseTo(star.logicalX, star.logicalY, star.logicalZ);
                _orbitCamera.SyncNow();
                return;
            }
        }
    }

    private void LateUpdate() {
        UpdateBodyLabels();
    }

    private Light _fallbackLight;

    private void UpdatePlanetLighting() {
        RefreshFallbackLight();
        if (_planets.Count == 0) return;
        SandboxPlanet.UploadStarLights(_stars);
    }

    private void RefreshFallbackLight() {
        bool noStars = _stars.Count == 0;
        if (noStars) {
            if (_fallbackLight == null) {
                GameObject go = new("SandboxFallbackSun");
                go.transform.SetParent(transform, false);
                go.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                _fallbackLight = go.AddComponent<Light>();
                _fallbackLight.type = LightType.Directional;
                _fallbackLight.color = new Color(1f, 0.97f, 0.92f);
                _fallbackLight.intensity = 1.15f;
                _fallbackLight.shadows = LightShadows.None;
            }
            _fallbackLight.enabled = true;
            Shader.SetGlobalFloat("_SandboxFallbackLightOn", 1f);
            Shader.SetGlobalVector("_SandboxFallbackLightDir",
                -_fallbackLight.transform.forward);
            Shader.SetGlobalColor("_SandboxFallbackLightColor",
                new Color(1f, 0.97f, 0.92f, 1f));
        } else {
            if (_fallbackLight != null) _fallbackLight.enabled = false;
            Shader.SetGlobalFloat("_SandboxFallbackLightOn", 0f);
        }
    }

    // ~~~ 初始化 ~~~ //

    private void DisableOldEvolutionController() {
        StellarEvolutionScene old = FindObjectOfType<StellarEvolutionScene>();
        if (old != null) old.enabled = false;
    }

    private void HideSceneExtras() {
        HideObject("HabitableZone");
        HideObject("PlanetaryOrbits");
        HideObject("PauseBorder");

        CameraDistanceHUD hud = FindObjectOfType<CameraDistanceHUD>();
        if (hud != null) hud.enabled = false;
    }

    private static void HideObject(string name) {
        GameObject go = GameObject.Find(name);
        if (go != null) go.SetActive(false);
    }

    private void CacheTemplateStar() {
        _templateStar = GameObject.Find("Star");
        if (_templateStar == null) return;

        Renderer renderer = _templateStar.GetComponent<Renderer>();
        if (renderer != null) _templateMaterial = renderer.sharedMaterial;
        _templatePosition = _templateStar.transform.position;

        // 这颗是演化场景自带的演示恒星，隐藏；它的材质作为创建恒星的模板。
        _templateStar.SetActive(false);
    }

    private Transform FindCanvas() {
        GameObject go = GameObject.Find("Canvas");
        if (go != null) {
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas != null) return go.transform;
        }

        foreach (Canvas canvas in FindObjectsOfType<Canvas>()) {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return canvas.transform;
        }
        return null;
    }

    private void CacheUi() {
        _createPanel = FindDeep(_canvas, "CelestialBodyCreatePanel")?.gameObject;
        _homePanel = FindDeep(_canvas, "HomePagePanel")?.gameObject ??
                     FindDeep(_canvas, "HomePanel")?.gameObject ??
                     FindDeep(_canvas, "HomePage")?.gameObject ??
                     FindDeep(_canvas, "Home")?.gameObject;
        _openHomeButton = FindButton("Button_OpenHome");
        _parameterPanel = FindDeep(_canvas, "StarParameterPanel")?.gameObject;
        _planetParameterPanel = FindDeep(_canvas, "PlanetParameterPanel")?.gameObject;
        if (_createPanel == null || _parameterPanel == null || _planetParameterPanel == null) {
            Debug.LogError("[Sandbox] 找不到创建/参数面板");
        }

        _openCreateButton = FindButton("Button_OpenCreate");
        _shutCreateButton = FindButton("Button_ShutCreatePanel");
        // UnitPanel 里也有 Button_NextPage / Button_LastPage，
        // 这里必须把创建面板的翻页按钮限定在 CelestialBodyCreatePanel 内。
        _nextPageButton = _createPanel != null
            ? FindDeepSkipping(_createPanel.transform, "Button_NextPage", "UnitChoosePanel")?.GetComponent<Button>()
            : null;
        _lastPageButton = _createPanel != null
            ? FindDeepSkipping(_createPanel.transform, "Button_LastPage", "UnitChoosePanel")?.GetComponent<Button>()
            : null;
        // 创建面板里也有同名的分类按钮；必须限定在创建面板内查找，
        // 否则 Player 的 Canvas/对象顺序变化时可能绑到另一个对象或绑不到。
        _starCategoryButton = _createPanel != null
            ? FindDeep(_createPanel.transform, "Button_Star")?.GetComponent<Button>()
            : null;
        _planetCategoryButton = _createPanel != null
            ? FindDeep(_createPanel.transform, "Button_Planet")?.GetComponent<Button>()
            : null;
        _dwarfCategoryButton = _createPanel != null
            ? FindDeep(_createPanel.transform, "Button_DwarfPlanet")?.GetComponent<Button>()
            : null;
        _moonCategoryButton = _createPanel != null
            ? FindDeep(_createPanel.transform, "Button_Moons")?.GetComponent<Button>()
            : null;
        _eggCategoryButton = _createPanel != null
            ? FindDeep(_createPanel.transform, "Button_Easter")?.GetComponent<Button>()
            : null;
        if (_starCategoryButton == null) _starCategoryButton = FindButton("Button_Star");
        if (_planetCategoryButton == null) _planetCategoryButton = FindButton("Button_Planet");
        if (_dwarfCategoryButton == null) _dwarfCategoryButton = FindButton("Button_DwarfPlanet");
        if (_moonCategoryButton == null) _moonCategoryButton = FindButton("Button_Moons");
        if (_eggCategoryButton == null) _eggCategoryButton = FindButton("Button_Easter");
        _orbitModeButton = FindButton("Button_Orbiting");
        _staticModeButton = FindButton("Button_Static");

        _shootModeButton = _createPanel != null
            ? FindDeep(_createPanel.transform, "Button_Shoot")?.GetComponent<Button>()
            : FindButton("Button_Shoot");
        if (_shootModeButton != null) {
            _shootSpeedPanel = FindDeep(_shootModeButton.transform, "Panel_Setspeed")?.gameObject;
            if (_shootSpeedPanel != null) {
                _shootSpeedInput = FindDeep(_shootSpeedPanel.transform, "InputField (TMP)")?.GetComponent<TMP_InputField>();
            }
            _shootUnitSwitch = FindDeep(_shootModeButton.transform, "Unit_Switch")?.gameObject;
            if (_shootUnitSwitch != null) {
                _shootSpeedUnit = _shootUnitSwitch.GetComponent<UnitButton>();
                if (_shootSpeedUnit == null) _shootSpeedUnit = _shootUnitSwitch.AddComponent<UnitButton>();
                SandboxUnits.EnsureLoaded();
                _shootSpeedUnit.SetCategory("speed", SandboxUnits.Symbols("speed"));

                // 默认 km/s，上次填过的速度/单位记在 PlayerPrefs 里。
                string savedUnit = PlayerPrefs.GetString(ShootSpeedUnitKey, "km/s");
                if (!string.IsNullOrEmpty(savedUnit)) _shootSpeedUnit.ApplyUnit(savedUnit);
                if (_shootSpeedInput != null) {
                    float savedSpeed = PlayerPrefs.GetFloat(ShootSpeedKey, 10f);
                    _shootSpeedInput.text = savedSpeed.ToString("0.###", CultureInfo.InvariantCulture);
                    _shootSpeedInput.onEndEdit.AddListener(_ => SaveShootSpeed());
                }
                _shootSpeedUnit.onUnitChanged += _ => SaveShootSpeed();
            }
        }
        _generalButton = _parameterPanel != null
            ? FindDeep(_parameterPanel.transform, "Button_General")?.GetComponent<Button>()
            : null;

        if (_parameterPanel != null) {
            _generalPanel = FindDeep(_parameterPanel.transform, "GeneralPanel")?.gameObject;
            _mechanicsPanel = FindDeep(_parameterPanel.transform, "MechanicsPanel")?.gameObject;
            _energyPanel = FindDeep(_parameterPanel.transform, "Energy&StructureParameter")?.gameObject;
            _mechanicsButton = FindDeep(_parameterPanel.transform, "Button_Mechanics")?.GetComponent<Button>();
            _starLockPositionButton = FindDeep(_parameterPanel.transform,
                "Button_LockPosition")?.GetComponent<Button>();
            if (_starLockPositionButton != null) {
                _starLockPositionButton.onClick.AddListener(OnStarLockPositionClick);
            }
            _energyStructureButton = FindDeep(_parameterPanel.transform,
                "Button_Energy&Structure")?.GetComponent<Button>();

            CacheStarRows();
            EnsurePanelScroller(_energyPanel, _parameterPanel,
                ref _starEnergyScrollerReady);
        }

        if (_planetParameterPanel != null) {
            _planetGeneralPanel = FindDeep(_planetParameterPanel.transform,
                "GeneralPanel")?.gameObject;
            _planetMechanicsPanel = FindDeep(_planetParameterPanel.transform,
                "MechanicsPanel")?.gameObject;
            _planetEnergyPanel = FindDeep(_planetParameterPanel.transform,
                "Energy&StructureParameter")?.gameObject;
            _planetGeneralButton = FindDeep(_planetParameterPanel.transform,
                "Button_General")?.GetComponent<Button>();
            _planetMechanicsButton = FindDeep(_planetParameterPanel.transform,
                "Button_Mechanics")?.GetComponent<Button>();
            _planetLockPositionButton = FindDeep(_planetParameterPanel.transform,
                "Button_LockPosition")?.GetComponent<Button>();
            if (_planetLockPositionButton != null) {
                _planetLockPositionButton.onClick.AddListener(OnPlanetLockPositionClick);
            }
            _planetEnergyButton = FindDeep(_planetParameterPanel.transform,
                "Button_Energy&Structure")?.GetComponent<Button>();

            if (_planetGeneralButton != null) {
                _planetGeneralButton.onClick.AddListener(OpenPlanetGeneralModule);
            }
            if (_planetMechanicsButton != null) {
                _planetMechanicsButton.onClick.AddListener(OpenPlanetMechanicsModule);
            }
            if (_planetEnergyButton != null) {
                _planetEnergyButton.onClick.AddListener(OpenPlanetEnergyModule);
            }
            SetPlanetModule(PlanetModule.General);
            EnsurePlanetMechanicsUi();
            EnsurePlanetEnergyUi();
            EnsurePanelScroller(_planetMechanicsPanel, _planetParameterPanel,
                ref _planetMechanicsScrollerReady);
            EnsurePanelScroller(_planetEnergyPanel, _planetParameterPanel,
                ref _planetEnergyScrollerReady);
        }

        if (_mechanicsButton != null) {
            _mechanicsButton.onClick.AddListener(OpenMechanicsModule);
        }
        if (_energyStructureButton != null) {
            _energyStructureButton.onClick.AddListener(OpenEnergyModule);
        }
        if (_generalButton != null) {
            _generalButton.onClick.AddListener(OpenGeneralModule);
        }
        SetStarModule(StarModule.General);

        if (_orbitModeButton != null) {
            _orbitModeButton.onClick.AddListener(
                () => SetPlacementMotion(PlacementMotion.Orbiting));
        }
        if (_staticModeButton != null) {
            _staticModeButton.onClick.AddListener(
                () => SetPlacementMotion(PlacementMotion.Static));
        }
        if (_shootModeButton != null) {
            _shootModeButton.onClick.AddListener(
                () => SetPlacementMotion(PlacementMotion.Launch));
        }
        if (_shootSpeedPanel != null) _shootSpeedPanel.SetActive(false);
        if (_shootUnitSwitch != null) _shootUnitSwitch.SetActive(false);
        UpdateMotionButtons();
        _expandParameterButton = FindButton("ButtonExpandStarParameter");
        _shutParameterPanelButton = FindButton("ShutTheParameterPanel");
        if (_shutParameterPanelButton == null) _shutParameterPanelButton = FindButton("ShutParameterPanel");
        _shutStarParameterButton = _parameterPanel != null
            ? FindDeep(_parameterPanel.transform, "ShutTheParameterPanel")?.GetComponent<Button>()
            : null;
        if (_shutStarParameterButton == null) _shutStarParameterButton = _shutParameterPanelButton;
        _shutPlanetParameterButton = _planetParameterPanel != null
            ? FindDeep(_planetParameterPanel.transform, "ShutTheParameterPanel")?.GetComponent<Button>()
            : null;

        if (_createPanel != null) {
            for (int i = 0; i < PageSize; i++) {
                Transform card = FindDeep(_createPanel.transform, $"Position0{i + 1}");
                if (card == null) continue;
                _cardButtons[i] = card.GetComponent<Button>();
                _cardImages[i] = card.GetComponent<Image>();
                _cardNames[i] = card.GetComponentInChildren<TMP_Text>(true);
                _cardPlates[i] = card.GetComponentInChildren<RawImage>(true);
            }

            foreach (Transform child in _createPanel.transform) {
                if (child.name == "Text (TMP)") {
                    _pageText = child.GetComponent<TMP_Text>();
                    break;
                }
            }
            if (_pageText == null) {
                foreach (TMP_Text text in _createPanel.GetComponentsInChildren<TMP_Text>(true)) {
                    if (text.text != null && text.text.Contains("页码")) {
                        _pageText = text;
                        break;
                    }
                }
            }
        }

        if (_parameterPanel != null) {
            _titleContent = FindText(_parameterPanel.transform, "Content");
            _nameInput = FindInput(_parameterPanel.transform, "IF_Type01_Name");
            _starOrbitAngleInput = FindInput(_parameterPanel.transform, "IF_Type02_OrbitAngle");
            if (_starOrbitAngleInput != null) {
                _starOrbitAngleInput.onEndEdit.AddListener(OnStarOrbitAngleEndEdit);
            }
            _massInput = FindInput(_parameterPanel.transform, "IF_Type02_Mass");
            _ageInput = FindInput(_parameterPanel.transform, "IF_Type02_age");
            _metalInput = FindInput(_parameterPanel.transform, "IF_Type02_Metal");

            _massLabel = FindText(_parameterPanel.transform, "Label_Mass");
            _ageLabel = FindText(_parameterPanel.transform, "Label_Age");
            _metalLabel = FindText(_parameterPanel.transform, "Label_Metal");

            _massUnit = AttachUnit(_massInput != null ? _massInput.transform : null, "Unit_Switch",
                new[] { "Msun", "Mearth" }, () => RefreshParameterFields());
            _ageUnit = AttachUnit(_ageInput != null ? _ageInput.transform : null, "Unit_Switch",
                new[] { "Myr", "Gyr" }, () => RefreshParameterFields());
            // 金属度只使用 Z；隐藏复制出来的单位按钮。
            Transform metalUnit = FindDeep(_metalInput != null ? _metalInput.transform : null, "Unit_Switch");
            if (metalUnit != null) metalUnit.gameObject.SetActive(false);

            Transform radiusRow = FindDeep(_parameterPanel.transform, "AutoParameter_Radius");
            Transform tempRow = FindDeep(_parameterPanel.transform, "AutoParameter_Temperature");
            Transform densityRow = FindDeep(_parameterPanel.transform, "AutoParameter_Density");
            Transform lumRow = FindDeep(_parameterPanel.transform, "AutoParameter_Lumionsity");

            _radiusUnit = AttachUnit(radiusRow, "Unit_Switch",
                new[] { "Rsun", "Rearth", "km" }, () => RefreshParameterFields());
            _temperatureUnit = AttachUnit(tempRow, "Unit_Switch",
                new[] { "K", "°C", "°F" }, () => RefreshParameterFields());
            _densityUnit = AttachUnit(densityRow, "Unit_Switch",
                new[] { "g/cm3", "kg/m3" }, () => RefreshParameterFields());
            _luminosityUnit = AttachUnit(lumRow, "Unit_Switch",
                new[] { "Lsun", "W" }, () => RefreshParameterFields());

            _radiusValue = FindText(radiusRow, "Text_Parameter");
            _temperatureValue = FindText(tempRow, "Text_Parameter");
            _densityValue = FindText(densityRow, "Text_Parameter");
            _luminosityValue = FindText(lumRow, "Text_Parameter");
        }

        if (_planetParameterPanel != null) {
            _planetTitleContent = FindText(_planetParameterPanel.transform, "Content");
            _planetNameInput = FindInput(_planetParameterPanel.transform, "IF_Type01_Name");
            _planetOrbitAngleInput = FindInput(_planetParameterPanel.transform,
                "IF_Type02_OrbitAngle");
            if (_planetOrbitAngleInput != null) {
                _planetOrbitAngleInput.onEndEdit.AddListener(OnPlanetOrbitAngleEndEdit);
            }
            _planetMassInput = FindInput(_planetParameterPanel.transform, "IF_Type02_Mass");
            _planetTempInput = FindInput(_planetParameterPanel.transform, "IF_Type02_Temperature");
            _planetAtmosphereInput = FindInput(_planetParameterPanel.transform,
                "IF_Type02_AutomspherericMass");

            _planetMassLabel = FindText(_planetParameterPanel.transform, "Label_Mass");
            _planetTempLabel = FindText(_planetParameterPanel.transform, "Label_Temperature");
            _planetAtmosphereLabel = FindText(_planetParameterPanel.transform,
                "Label_AtomSpherericMass");

            _planetMassUnit = AttachUnit(_planetMassInput != null ? _planetMassInput.transform : null,
                "Unit_Switch", new[] { "Mearth", "Msun" }, RefreshPlanetParameterFields);
            _planetTempUnit = AttachUnit(_planetTempInput != null ? _planetTempInput.transform : null,
                "Unit_Switch", new[] { "K", "°C", "°F" }, RefreshPlanetParameterFields);
            _planetAtmosphereUnit = AttachUnit(
                _planetAtmosphereInput != null ? _planetAtmosphereInput.transform : null,
                "Unit_Switch", new[] { "Mearth", "kg" }, RefreshPlanetParameterFields);

            Transform planetRadiusRow = FindDeep(_planetParameterPanel.transform, "AutoParameter_Radius");
            Transform planetDensityRow = FindDeep(_planetParameterPanel.transform, "AutoParameter_Density");
            Transform planetGravityRow = FindDeep(_planetParameterPanel.transform,
                "AutoParameter_SurfageGravity");
            if (planetGravityRow == null) {
                planetGravityRow = FindDeep(_planetParameterPanel.transform,
                    "AutoParameter_SurfaceGravity");
            }
            Transform planetPressureRow = FindDeep(_planetParameterPanel.transform,
                "AutoParameter_SurfacePressure");

            _planetRadiusUnit = AttachUnit(planetRadiusRow, "Unit_Switch",
                new[] { "Rearth", "km", "Rsun" }, RefreshPlanetParameterFields);
            _planetDensityUnit = AttachUnit(planetDensityRow, "Unit_Switch",
                new[] { "g/cm3", "kg/m3" }, RefreshPlanetParameterFields);
            _planetGravityUnit = AttachUnit(planetGravityRow, "Unit_Switch",
                new[] { "g", "m/s2" }, RefreshPlanetParameterFields);
            _planetPressureUnit = AttachUnit(planetPressureRow, "Unit_Switch",
                new[] { "atm", "Pa" }, RefreshPlanetParameterFields);

            _planetRadiusValue = FindText(planetRadiusRow, "Text_Parameter");
            _planetDensityValue = FindText(planetDensityRow, "Text_Parameter");
            _planetGravityValue = FindText(planetGravityRow, "Text_Parameter");
            _planetPressureValue = FindText(planetPressureRow, "Text_Parameter");

            Transform planetDistanceRow = FindDeep(_planetParameterPanel.transform,
                "AutoParameter_Distance");
            _planetDistanceUnit = AttachUnit(planetDistanceRow, "Unit_Switch",
                new[] { "m", "km", "AU", "ly", "Mpc" }, RefreshPlanetParameterFields);
            _planetDistanceValue = FindText(planetDistanceRow, "Text_Parameter");
        }
    }

    private void CacheStarRows() {
        _generalVelocityValue = StarRowText(_generalPanel, "AutoParameter_Velocity");
        // 距离行是复制“速度”行做出来的，所以物体名还是 AutoParameter_Velocity (1)。
        _generalDistanceValue = StarRowText(_generalPanel, "AutoParameter_Velocity (1)");
        SetRowUnit(_generalPanel, "AutoParameter_Velocity (1)", "km", "length",
            new[] { "km", "AU", "ly", "Mpc" });
        _mechCentralObject = StarRowText(_mechanicsPanel, "AutoParameter_Centural_Object");
        _mechForce = StarRowText(_mechanicsPanel, "AutoParameter_GeneralForceReceived");
        SetRowUnit(_mechanicsPanel, "AutoParameter_GeneralForceReceived", "N", "force");
        _mechKineticValue = StarRowText(_mechanicsPanel, "AutoParameter_Knetic");
        _mechAngularKineticValue = StarRowText(_mechanicsPanel, "AutoParameter_Angular_Knetic");
        _mechEccentricityValue = StarRowText(_mechanicsPanel, "AutoParameter_Eccentricity");
        _mechEquatorVelocityValue = StarRowText(_mechanicsPanel, "AutoParameter_Equator_Velocity");
        _mechSemiMajorAxisValue = StarRowText(_mechanicsPanel, "AutoParameter_Semi_Major_Axis");
        _mechOrbitalPeriodValue = StarRowText(_mechanicsPanel, "AutoParameter_Orbital_Period");
        _mechVelocityValue = StarRowText(_mechanicsPanel, "AutoParameter_Velocity");
        _mechMomentumValue = StarRowText(_mechanicsPanel, "AutoParameter_Momentum");
        _mechAngularMomentumValue = StarRowText(_mechanicsPanel, "AutoParameter_Angular_Momentum");

        _mechXMotionInput = StarRowInput(_mechanicsPanel, "IF_Type02_XMotion");
        _mechYMotionInput = StarRowInput(_mechanicsPanel, "IF_Type02_YMotion");
        _mechZMotionInput = StarRowInput(_mechanicsPanel, "IF_Type02_ZMotion");
        _mechSpinPeriodInput = StarRowInput(_mechanicsPanel, "IF_Type02_SpinPeriod");
        if (_mechSpinPeriodInput != null) {
            _mechSpinPeriodInput.onEndEdit.AddListener(OnSpinPeriodEndEdit);
        }

        _energyCorePressureValue = StarRowText(_energyPanel, "AutoParameter_Centural_Pressure");
        _energyCoreTemperatureValue = StarRowText(_energyPanel, "AutoParameter_Centural_Temperature");
        _energyBindingValue = StarRowText(_energyPanel,
            "AutoParameter_Gravitational_Combinantion_Energy");
        _energyFusionValue = StarRowText(_energyPanel, "AutoParameter_FusionPower");
        _energyAtmosphereHeatingValue = StarRowText(_energyPanel,
            "AutoParameter_AtmosphereHeatingPower (1)");
        _energyRadiationHeatingValue = StarRowText(_energyPanel,
            "AutoParameter_RadiationHeatingPower");
        _energyThermalRadiationValue = StarRowText(_energyPanel,
            "AutoParameter_ThermalRadiationPower");
        _energyTotalInputValue = StarRowText(_energyPanel, "AutoParameter_TotalInputPower");
        _energyWavePowerValue = StarRowText(_energyPanel, "AutoParameter_WavePower");
        _energyWaveLowInput = StarRowInput(_energyPanel, "IF_Type02WaveLengthLow");
        _energyWaveHighInput = StarRowInput(_energyPanel, "IF_Type02_WaveLengthup");

        SetRowUnit(_energyPanel, "AutoParameter_WavePower", "W", "power");
        SetRowUnit(_energyPanel, "IF_Type02WaveLengthLow", "nm", "wavelength");
        SetRowUnit(_energyPanel, "IF_Type02_WaveLengthup", "nm", "wavelength");
        if (_energyWaveLowInput != null) {
            if (string.IsNullOrEmpty(_energyWaveLowInput.text) ||
                _energyWaveLowInput.text == "\u200B") {
                _energyWaveLowInput.text = "400";
            }
            _energyWaveLowInput.onEndEdit.AddListener(OnWaveBandEndEdit);
        }
        if (_energyWaveHighInput != null) {
            if (string.IsNullOrEmpty(_energyWaveHighInput.text) ||
                _energyWaveHighInput.text == "\u200B") {
                _energyWaveHighInput.text = "760";
            }
            _energyWaveHighInput.onEndEdit.AddListener(OnWaveBandEndEdit);
        }

        // 力学模块字段很多：把它变成一个可滚动的裁剪区域。
        if (_mechanicsPanel != null && !_mechanicsScrollerReady) {
            SandboxPanelScroll scroller = _mechanicsPanel.GetComponent<SandboxPanelScroll>();
            if (scroller == null) scroller = _mechanicsPanel.AddComponent<SandboxPanelScroll>();
            scroller.Initialize(_mechanicsPanel.GetComponent<RectTransform>(),
                _parameterPanel.transform as RectTransform);
            _mechanicsScrollerReady = true;
        }

        ConfigureStarPanelPresentation();
    }

    private void EnsurePlanetEnergyUi() {
        if (_planetEnergyPanel == null) return;

        _planetTotalInputValue = StarRowText(_planetEnergyPanel, "AutoParameter_TotalInputPower");
        _planetThermalRadiationValue = StarRowText(_planetEnergyPanel,
            "AutoParameter_ThermalRadiationPower");
        _planetFusionValue = StarRowText(_planetEnergyPanel, "AutoParameter_FusionPower");
        _planetRadiationHeatingValue = StarRowText(_planetEnergyPanel,
            "AutoParameter_RadiationHeatingPower");
        _planetAtmosphereHeatingValue = StarRowText(_planetEnergyPanel,
            "AutoParameter_AtmosphereHeatingPower (1)");
        _planetBindingValue = StarRowText(_planetEnergyPanel,
            "AutoParameter_Gravitational_Combinantion_Energy");
        // 场景里还有一行同名“引力结合能”，是复制面板时的遗留行：
        // FindDeep 只会匹配到第一行，这里单独取出来一起填充。
        _planetBindingValue2 = StarRowText(_planetEnergyPanel,
            "AutoParameter_Gravitational_Combinantion_Energy (2)");
        // 波段功率（行星按黑体处理）
        _planetWavePowerValue = StarRowText(_planetEnergyPanel, "AutoParameter_WavePower");
        _planetWaveLowInput = StarRowInput(_planetEnergyPanel, "IF_Type02WaveLengthLow");
        _planetWaveHighInput = StarRowInput(_planetEnergyPanel, "IF_Type02_WaveLengthup");
        SetRowUnit(_planetEnergyPanel, "AutoParameter_WavePower", "W", "power");
        SetRowUnit(_planetEnergyPanel, "IF_Type02WaveLengthLow", "nm", "wavelength");
        SetRowUnit(_planetEnergyPanel, "IF_Type02_WaveLengthup", "nm", "wavelength");
        if (_planetWaveLowInput != null) {
            if (string.IsNullOrEmpty(_planetWaveLowInput.text) ||
                _planetWaveLowInput.text == "\u200B") {
                _planetWaveLowInput.text = "400";
            }
            _planetWaveLowInput.onEndEdit.AddListener(OnPlanetWaveBandEndEdit);
        }
        if (_planetWaveHighInput != null) {
            if (string.IsNullOrEmpty(_planetWaveHighInput.text) ||
                _planetWaveHighInput.text == "\u200B") {
                _planetWaveHighInput.text = "760";
            }
            _planetWaveHighInput.onEndEdit.AddListener(OnPlanetWaveBandEndEdit);
        }

        _planetScaleHeightValue = StarRowText(_planetEnergyPanel,
            "AutoParameter_Centural_Pressure");
        _planetTemperatureGradientValue = StarRowText(_planetEnergyPanel,
            "AutoParameter_Gravitational_Combinantion_Energy (1)");

        // 结构参数里的“大气质量”输入框（和 General 面板的那个同步）。
        _planetEnergyAtmosphereInput = StarRowInput(_planetEnergyPanel,
            "IF_Type02_AutomspherericMass");
        if (_planetEnergyAtmosphereInput != null) {
            _planetEnergyAtmosphereInput.onEndEdit.AddListener(
                OnPlanetEnergyAtmosphereEndEdit);
        }

        _planetWaterDepthInput = StarRowInput(_planetEnergyPanel,
            "IF_Type02_WaterDepth");
        if (_planetWaterDepthInput == null && _planetParameterPanel != null) {
            _planetWaterDepthInput = StarRowInput(_planetParameterPanel,
                "IF_Type02_WaterDepth");
        }
        if (_planetWaterDepthInput != null) {
            _planetWaterDepthInput.onEndEdit.AddListener(
                OnPlanetSurfaceWaterDepthEndEdit);
        }

        Transform concentrationRow = FindPlanetRowByLabel("浓度", "盐度");
        concentrationRow ??= FindDeep(_planetParameterPanel.transform,
            "IF_Type02_Concentration");
        concentrationRow ??= FindDeep(_planetParameterPanel.transform,
            "IF_Type02_SoluteConcentration");
        concentrationRow ??= FindDeep(_planetParameterPanel.transform,
            "IF_Type02_Salinity");
        if (concentrationRow != null) {
            _planetConcentrationRowName = concentrationRow.name;
            _planetConcentrationInput = concentrationRow.GetComponent<TMP_InputField>();
            if (_planetConcentrationInput != null) {
                _planetConcentrationInput.onEndEdit.AddListener(
                    OnPlanetConcentrationEndEdit);
            }
            SetPlanetRowUnit(_planetParameterPanel, _planetConcentrationRowName,
                "mmol/L", "concentration");
        }

        Transform freezeRow = FindPlanetRowByLabel("冰点");
        freezeRow ??= FindDeep(_planetParameterPanel.transform, "AutoParameter_FreezePoint");
        if (freezeRow != null) {
            _planetFreezePointRowName = freezeRow.name;
            _planetFreezePointInput = freezeRow.GetComponent<TMP_InputField>();
            _planetFreezePointText = _planetFreezePointInput == null
                ? FindText(freezeRow, "Text_Parameter") : null;
            if (_planetFreezePointInput != null) {
                _planetFreezePointInput.readOnly = true;
                _planetFreezePointInput.onEndEdit.AddListener(
                    _ => RefreshPlanetParameterFields());
            }
            SetPlanetRowUnit(_planetParameterPanel, _planetFreezePointRowName,
                "K", "temperature");
        }

        Transform boilRow = FindPlanetRowByLabel("沸点");
        boilRow ??= FindDeep(_planetParameterPanel.transform, "AutoParameter_BoilPoint");
        if (boilRow != null) {
            _planetBoilPointRowName = boilRow.name;
            _planetBoilPointInput = boilRow.GetComponent<TMP_InputField>();
            _planetBoilPointText = _planetBoilPointInput == null
                ? FindText(boilRow, "Text_Parameter") : null;
            if (_planetBoilPointInput != null) {
                _planetBoilPointInput.readOnly = true;
                _planetBoilPointInput.onEndEdit.AddListener(
                    _ => RefreshPlanetParameterFields());
            }
            SetPlanetRowUnit(_planetParameterPanel, _planetBoilPointRowName,
                "K", "temperature");
        }

        SetPlanetRowUnit("IF_Type02_AutomspherericMass", "Mearth", "mass");
        SetPlanetRowUnit("AutoParameter_TotalInputPower", "W", "power");
        SetPlanetRowUnit("AutoParameter_ThermalRadiationPower", "W", "power");
        SetPlanetRowUnit("AutoParameter_FusionPower", "W", "power");
        SetPlanetRowUnit("AutoParameter_RadiationHeatingPower", "W", "power");
        SetPlanetRowUnit("AutoParameter_AtmosphereHeatingPower (1)", "W", "power");
        SetPlanetRowUnit("AutoParameter_Gravitational_Combinantion_Energy", "J", "energy");
        SetPlanetRowUnit("AutoParameter_Gravitational_Combinantion_Energy (2)", "J", "energy");
        SetPlanetRowUnit("AutoParameter_Centural_Pressure", "km", "length");
        SetPlanetRowUnit("IF_Type02_WaterDepth", "km", "length");
        SetPlanetRowUnit("AutoParameter_Gravitational_Combinantion_Energy (1)",
            "K/km", "");

        Color sectionColor = new(1f, 0.78f, 0.22f, 1f);
        SetSectionLabelColor(_planetParameterPanel.transform, "Text_EnergyInput", sectionColor);
        SetSectionLabelColor(_planetParameterPanel.transform, "Text_EnergyOutput", sectionColor);
        SetSectionLabelColor(_planetParameterPanel.transform, "Text_Structure", sectionColor);
        SetSectionLabelColor(_planetParameterPanel.transform, "Text_Rotation", sectionColor);
        SetSectionLabelColor(_planetParameterPanel.transform, "Text_Orbit", sectionColor);
        SetSectionLabelColor(_planetParameterPanel.transform, "Text_Force_Received", sectionColor);
        SetSectionLabelColor(_planetParameterPanel.transform, "Text_TraslationMotion", sectionColor);

        // 不再在代码里克隆/追加 UI 行；完全使用场景里已有的布局，
        // 避免运行时多出来的行和水深行/结构标签重叠。
    }

    private void EnsurePlanetMechanicsUi() {
        if (_planetMechanicsPanel == null) return;

        _planetMechVelocity = StarRowText(_planetMechanicsPanel, "AutoParameter_Velocity");
        _planetMechMomentum = StarRowText(_planetMechanicsPanel, "AutoParameter_Momentum");
        _planetMechKinetic = StarRowText(_planetMechanicsPanel, "AutoParameter_Knetic");
        _planetMechEquator = StarRowText(_planetMechanicsPanel, "AutoParameter_Equator_Velocity");
        _planetMechCentral = StarRowText(_planetMechanicsPanel, "AutoParameter_Centural_Object");
        _planetMechEccentricity = StarRowText(_planetMechanicsPanel, "AutoParameter_Eccentricity");
        _planetMechAngularMomentum = StarRowText(_planetMechanicsPanel, "AutoParameter_Angular_Momentum");
        _planetMechAngularKinetic = StarRowText(_planetMechanicsPanel, "AutoParameter_Angular_Knetic");
        _planetMechSemiMajor = StarRowText(_planetMechanicsPanel, "AutoParameter_Semi_Major_Axis");
        _planetMechOrbitalPeriod = StarRowText(_planetMechanicsPanel, "AutoParameter_Orbital_Period");
        _planetMechForce = StarRowText(_planetMechanicsPanel, "AutoParameter_GeneralForceReceived");
        _planetMechX = StarRowInput(_planetMechanicsPanel, "IF_Type02_XMotion");
        _planetMechY = StarRowInput(_planetMechanicsPanel, "IF_Type02_YMotion");
        _planetMechZ = StarRowInput(_planetMechanicsPanel, "IF_Type02_ZMotion");
        _planetMechSpin = StarRowInput(_planetMechanicsPanel, "IF_Type02_SpinPeriod");
        if (_planetMechSpin != null) {
            _planetMechSpin.onEndEdit.AddListener(OnPlanetSpinPeriodEndEdit);
        }

        // 自转轴倾角：度 / 弧度 / GRA，默认度。
        _planetMechSpinAngle = StarRowInput(_planetMechanicsPanel, "IF_Type02_SpinAngle");
        if (_planetMechSpinAngle != null) {
            _planetMechSpinAngleUnit = AttachUnit(_planetMechSpinAngle.transform, "Unit_Switch",
                new[] { "度", "弧度", "GRA" }, () => {
                    RefreshPlanetParameterFields();
                    UpdateSelectedPlanetTemperatureDisplay();
                });
            if (_planetMechSpinAngleUnit != null) {
                _starRowUnits[RowUnitKey(_planetMechanicsPanel, "IF_Type02_SpinAngle")] = _planetMechSpinAngleUnit;
            }
            _planetMechSpinAngle.onEndEdit.AddListener(OnPlanetSpinAngleEndEdit);
        }

        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Centural_Object", string.Empty);
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Eccentricity", string.Empty);
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Velocity", "km/s", "speed");
        SetPlanetRowUnit(_planetMechanicsPanel, "IF_Type02_XMotion", "km/s", "speed");
        SetPlanetRowUnit(_planetMechanicsPanel, "IF_Type02_YMotion", "km/s", "speed");
        SetPlanetRowUnit(_planetMechanicsPanel, "IF_Type02_ZMotion", "km/s", "speed");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Momentum", "kg·m/s");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Knetic", "J", "energy");
        SetPlanetRowUnit(_planetMechanicsPanel, "IF_Type02_SpinPeriod", "d", "time");
        SetPlanetRowUnit(_planetMechanicsPanel, "IF_Type02_OrbitAngle", "°");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Equator_Velocity", "km/s", "speed");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Angular_Knetic", "J", "energy");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Angular_Momentum", "kg·m²/s");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Semi_Major_Axis", "AU", "length");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_Orbital_Period", "yr", "time");
        SetPlanetRowUnit(_planetMechanicsPanel, "AutoParameter_GeneralForceReceived", "N", "force");
    }

    private void SetPlanetRowUnit(string rowName, string unit, string category) {
        SetPlanetRowUnit(_planetEnergyPanel, rowName, unit, category);
    }

    private void SetPlanetRowUnit(GameObject panel, string rowName, string unit) {
        SetPlanetRowUnit(panel, rowName, unit, "");
    }

    private void SetPlanetRowUnit(GameObject panel, string rowName, string unit,
            string category, string[] autoCandidates = null) {
        if (panel == null) return;
        Transform row = FindDeep(panel.transform, rowName);
        if (row == null) return;
        Transform unitSwitch = FindDeep(row, "Unit_Switch");
        if (unitSwitch == null) return;

        if (string.IsNullOrEmpty(unit)) {
            unitSwitch.gameObject.SetActive(false);
            return;
        }

        unitSwitch.gameObject.SetActive(true);
        UnitButton picker = unitSwitch.GetComponent<UnitButton>();
        if (picker == null) picker = unitSwitch.gameObject.AddComponent<UnitButton>();
        picker.SetCategory(category, new[] { unit });
        picker.autoCandidates = autoCandidates;
        picker.onUnitChanged += _ => {
            if (_selectedPlanet != null) RefreshPlanetParameterFields();
        };
        _starRowUnits[RowUnitKey(panel, rowName)] = picker;
    }

    private void ConfigureStarPanelPresentation() {
        // General 新增的速度项
        SetRowUnit(_generalPanel, "AutoParameter_Velocity", "km/s", "speed");

        // Mechanics
        SetRowUnit(_mechanicsPanel, "AutoParameter_Centural_Object", string.Empty);
        SetRowUnit(_mechanicsPanel, "IF_Type02_XMotion", "km/s", "speed");
        SetRowUnit(_mechanicsPanel, "IF_Type02_YMotion", "km/s", "speed");
        SetRowUnit(_mechanicsPanel, "IF_Type02_ZMotion", "km/s", "speed");
        SetRowUnit(_mechanicsPanel, "AutoParameter_Knetic", "J", "energy");
        SetRowUnit(_mechanicsPanel, "AutoParameter_Angular_Knetic", "J", "energy");
        SetRowUnit(_mechanicsPanel, "AutoParameter_Eccentricity", string.Empty);
        SetRowUnit(_mechanicsPanel, "AutoParameter_Equator_Velocity", "km/s", "speed");
        SetRowUnit(_mechanicsPanel, "AutoParameter_Semi_Major_Axis", "AU", "length");
        SetRowUnit(_mechanicsPanel, "AutoParameter_Orbital_Period", "yr", "time");
        SetRowUnit(_mechanicsPanel, "AutoParameter_Velocity", "km/s", "speed");
        SetRowUnit(_mechanicsPanel, "AutoParameter_Momentum", string.Empty);
        SetRowUnit(_mechanicsPanel, "AutoParameter_Angular_Momentum", string.Empty);
        SetRowUnit(_mechanicsPanel, "IF_Type02_SpinPeriod", "d", "time");
        SetRowUnit(_mechanicsPanel, "IF_Type02_OrbitAngle", "°");

        // Energy & Structure
        SetRowUnit(_energyPanel, "AutoParameter_Centural_Temperature", "K", "temperature");
        SetRowUnit(_energyPanel, "AutoParameter_Centural_Pressure", string.Empty);
        SetRowUnit(_energyPanel, "AutoParameter_Gravitational_Combinantion_Energy", "J", "energy");
        SetRowUnit(_energyPanel, "AutoParameter_FusionPower", "W", "power");
        SetRowUnit(_energyPanel, "AutoParameter_RadiationHeatingPower", string.Empty);
        SetRowUnit(_energyPanel, "AutoParameter_AtmosphereHeatingPower (1)", string.Empty);
        SetRowUnit(_energyPanel, "AutoParameter_ThermalRadiationPower", "W", "power");
        SetRowUnit(_energyPanel, "AutoParameter_TotalInputPower", "W", "power");

        Color sectionColor = new(1f, 0.78f, 0.22f, 1f);
        SetSectionLabelColor("Text_EnergyInput", sectionColor);
        SetSectionLabelColor("Text_EnergyInput (1)", sectionColor);
        SetSectionLabelColor("Text_EnergyInput (2)", sectionColor);
        SetSectionLabelColor("Text_EnergyOutput", sectionColor);
        SetSectionLabelColor("Text_Structure", sectionColor);
        SetSectionLabelColor("Text_Rotation", sectionColor);
        SetSectionLabelColor("Text_Orbit", sectionColor);
        SetSectionLabelColor("Text_Force_Received", sectionColor);
        SetSectionLabelColor("Text_TraslationMotion", sectionColor);
    }

    private void SetRowUnit(GameObject panel, string rowName, string unit,
            string category = "", string[] autoCandidates = null) {
        if (panel == null) return;
        Transform row = FindDeep(panel.transform, rowName);
        if (row == null) return;
        Transform unitSwitch = FindDeep(row, "Unit_Switch");
        if (unitSwitch == null) return;

        if (string.IsNullOrEmpty(unit)) {
            unitSwitch.gameObject.SetActive(false);
            return;
        }

        unitSwitch.gameObject.SetActive(true);
        UnitButton picker = unitSwitch.GetComponent<UnitButton>();
        if (picker == null) picker = unitSwitch.gameObject.AddComponent<UnitButton>();
        picker.SetCategory(category, new[] { unit });
        picker.autoCandidates = autoCandidates;
        picker.onUnitChanged += _ => {
            if (_selectedStar != null) RefreshStarDerivedFields(_selectedStar);
        };
        _starRowUnits[RowUnitKey(panel, rowName)] = picker;
    }

    private UnitButton GetRowUnit(GameObject panel, string rowName) {
        if (panel == null) return null;
        _starRowUnits.TryGetValue(RowUnitKey(panel, rowName), out UnitButton unit);
        return unit;
    }

    private static string RowUnitKey(GameObject panel, string rowName) {
        string owner = "";
        Transform t = panel != null ? panel.transform : null;
        while (t != null) {
            if (t.name == "StarParameterPanel" || t.name == "PlanetParameterPanel") {
                owner = t.name;
                break;
            }
            t = t.parent;
        }
        return owner + "/" + (panel != null ? panel.name : "") + "/" + rowName;
    }

    private string FormatUnitValue(double baseValue, string category,
            GameObject panel, string rowName, string fallback) {
        UnitButton picker = GetRowUnit(panel, rowName);
        if (picker != null) picker.ApplyAuto(baseValue);
        SandboxUnitDefinition def = picker != null
            ? SandboxUnits.Definition(category, picker.GetUnit())
            : null;
        return def != null
            ? FormatValue(SandboxUnits.FromBase(baseValue, def))
            : fallback;
    }

    /// <summary>单位自适应：没手动选过就按数值挑单位。</summary>
    private static void ApplyAutoUnit(UnitButton picker, string category, double baseValue) {
        if (picker != null) picker.ApplyAuto(baseValue);
    }

    /// <summary>重新打开面板时，把所有单位恢复成自适应。</summary>
    private void ResetAutoUnits() {
        foreach (UnitButton unit in _starRowUnits.Values) {
            if (unit != null) unit.ResetAuto();
        }

        _massUnit?.ResetAuto();
        _ageUnit?.ResetAuto();
        _radiusUnit?.ResetAuto();
        _temperatureUnit?.ResetAuto();
        _densityUnit?.ResetAuto();
        _luminosityUnit?.ResetAuto();
        _planetMassUnit?.ResetAuto();
        _planetTempUnit?.ResetAuto();
        _planetAtmosphereUnit?.ResetAuto();
        _planetRadiusUnit?.ResetAuto();
        _planetDensityUnit?.ResetAuto();
        _planetGravityUnit?.ResetAuto();
        _planetPressureUnit?.ResetAuto();
        _planetDistanceUnit?.ResetAuto();
    }

    private void SetSectionLabelColor(string name, Color color) {
        if (_parameterPanel == null) return;
        SetSectionLabelColor(_parameterPanel.transform, name, color);
    }

    private static void SetSectionLabelColor(Transform root, string name, Color color) {
        if (root == null) return;
        Transform t = FindDeep(root, name);
        TMP_Text label = t != null ? t.GetComponent<TMP_Text>() : null;
        if (label != null) label.color = color;
    }

    private static TMP_Text StarRowText(GameObject panel, string rowName) {
        if (panel == null) return null;
        Transform row = FindDeep(panel.transform, rowName);
        return row != null ? FindText(row, "Text_Parameter") : null;
    }

    private static TMP_InputField StarRowInput(GameObject panel, string rowName) {
        if (panel == null) return null;
        Transform row = FindDeep(panel.transform, rowName);
        return row != null ? row.GetComponent<TMP_InputField>() : null;
    }

    private void EnsurePanelScroller(GameObject panel, GameObject owner, ref bool ready) {
        if (panel == null || owner == null || ready) return;
        SandboxPanelScroll scroller = panel.GetComponent<SandboxPanelScroll>();
        if (scroller == null) scroller = panel.AddComponent<SandboxPanelScroll>();
        scroller.Initialize(panel.GetComponent<RectTransform>(),
            owner.transform as RectTransform);
        ready = true;
    }

    private void OpenPlanetGeneralModule() {
        OpenParameterPanel();
        SetPlanetModule(PlanetModule.General);
    }

    private void OpenPlanetMechanicsModule() {
        OpenParameterPanel();
        SetPlanetModule(PlanetModule.Mechanics);
    }

    private void OpenPlanetEnergyModule() {
        OpenParameterPanel();
        SetPlanetModule(PlanetModule.EnergyStructure);
    }

    private void SetPlanetModule(PlanetModule module) {
        _planetModule = module;
        if (_planetGeneralPanel != null) {
            _planetGeneralPanel.SetActive(module == PlanetModule.General);
        }
        if (_planetMechanicsPanel != null) {
            _planetMechanicsPanel.SetActive(module == PlanetModule.Mechanics);
        }
        if (_planetEnergyPanel != null) {
            _planetEnergyPanel.SetActive(module == PlanetModule.EnergyStructure);
        }
        SetCategoryTint(_planetGeneralButton, module == PlanetModule.General ? Accent : TextDim);
        SetCategoryTint(_planetMechanicsButton, module == PlanetModule.Mechanics ? Accent : TextDim);
        SetCategoryTint(_planetEnergyButton,
            module == PlanetModule.EnergyStructure ? Accent : TextDim);
    }

    private void OpenGeneralModule() {
        OpenParameterPanel();
        SetStarModule(StarModule.General);
    }

    private void OpenMechanicsModule() {
        OpenParameterPanel();
        SetStarModule(StarModule.Mechanics);
    }

    private void OpenEnergyModule() {
        OpenParameterPanel();
        SetStarModule(StarModule.EnergyStructure);
    }

    private void SetStarModule(StarModule module) {
        _starModule = module;
        if (_generalPanel != null) {
            _generalPanel.SetActive(module == StarModule.General);
        }
        if (_mechanicsPanel != null) {
            _mechanicsPanel.SetActive(module == StarModule.Mechanics);
        }
        if (_energyPanel != null) {
            _energyPanel.SetActive(module == StarModule.EnergyStructure);
        }
        SetCategoryTint(_generalButton, module == StarModule.General ? Accent : TextDim);
        SetCategoryTint(_mechanicsButton, module == StarModule.Mechanics ? Accent : TextDim);
        SetCategoryTint(_energyStructureButton,
            module == StarModule.EnergyStructure ? Accent : TextDim);
    }

    private Button FindButton(string name) {
        Transform t = FindDeep(_canvas, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    /// <summary>FindDeep，但跳过名为 skipName 的整棵子树。</summary>
    private static Transform FindDeepSkipping(Transform root, string name, string skipName) {
        if (root == null) return null;
        if (root.name == skipName) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++) {
            Transform found = FindDeepSkipping(root.GetChild(i), name, skipName);
            if (found != null) return found;
        }
        return null;
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

    private static TMP_Text FindText(Transform root, string name) {
        if (root == null) return null;
        Transform t = root.name == name ? root : FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static TMP_InputField FindInput(Transform root, string name) {
        if (root == null) return null;
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_InputField>() : null;
    }

    private Transform FindPlanetRowByLabel(params string[] keywords) {
        if (_planetParameterPanel == null) return null;
        foreach (TMP_Text text in _planetParameterPanel.GetComponentsInChildren<TMP_Text>(true)) {
            if (text == null || string.IsNullOrEmpty(text.text)) continue;
            foreach (string keyword in keywords) {
                if (text.text.Contains(keyword)) return text.transform.parent;
            }
        }
        return null;
    }

    private UnitButton AttachUnit(Transform root, string unitSwitchName, string[] units, Action changed) {
        if (root == null) return null;
        Transform switchTransform = FindDeep(root, unitSwitchName);
        return switchTransform != null
            ? AttachUnit(switchTransform, units, changed)
            : null;
    }

    private UnitButton AttachUnit(Transform switchTransform, string[] units, Action changed) {
        if (switchTransform == null) return null;

        Button button = switchTransform.GetComponent<Button>();
        if (button == null) return null;

        UnitButton unit = switchTransform.GetComponent<UnitButton>();
        if (unit == null) unit = switchTransform.gameObject.AddComponent<UnitButton>();
        unit.SetCategory(InferUnitCategory(units), units);
        unit.currentUnit = 0;
        unit.onUnitChanged += _ => changed?.Invoke();

        TMP_Text label = switchTransform.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = unit.GetUnit();
        return unit;
    }

    private static string InferUnitCategory(string[] units) {
        if (units == null || units.Length == 0) return "";
        if (Array.IndexOf(units, "K") >= 0 || Array.IndexOf(units, "°C") >= 0) return "temperature";
        if (Array.IndexOf(units, "Lsun") >= 0 || Array.IndexOf(units, "W") >= 0) return "power";
        if (Array.IndexOf(units, "Rearth") >= 0 || Array.IndexOf(units, "Rsun") >= 0 ||
            Array.IndexOf(units, "km") >= 0) return "length";
        if (Array.IndexOf(units, "Mearth") >= 0 || Array.IndexOf(units, "Msun") >= 0) return "mass";
        if (Array.IndexOf(units, "Myr") >= 0 || Array.IndexOf(units, "Gyr") >= 0) return "time";
        if (Array.IndexOf(units, "g/cm3") >= 0 || Array.IndexOf(units, "kg/m3") >= 0) return "density";
        if (Array.IndexOf(units, "度") >= 0 || Array.IndexOf(units, "弧度") >= 0 ||
            Array.IndexOf(units, "GRA") >= 0) return "angle";
        return "";
    }

    private void LoadCatalog() {
        TextAsset asset = Resources.Load<TextAsset>("Data/Stars");
        if (asset != null) {
            _catalog = JsonUtility.FromJson<StarCatalog>(asset.text);
        }

        if (_catalog == null || _catalog.stars == null || _catalog.stars.Length == 0) {
            Debug.LogError("[Sandbox] 读取 Resources/Data/Stars.json 失败，使用 Sun 兜底");
            _catalog = new StarCatalog {
                stars = new[] {
                    new StarData {
                name = "太阳", mass = 1.0, metallicity = 0.018, age = 4450,
                rotationPeriodDays = 25.38
            }
                }
            };
        }
    }

    private void LoadPlanetCatalog() {
        _planetCatalog = SandboxPlanetModel.LoadCatalog();
        LoadDwarfCatalog();
        LoadMoonCatalog();
        LoadEggCatalog();
    }

    /// <summary>矮行星 / 小行星目录（Data/DwarfPlanets&amp;Asteroids.json，格式与 Planets 相同）。</summary>
    private void LoadDwarfCatalog() {
        _dwarfCatalog = SandboxPlanetModel.LoadCatalog("Data/DwarfPlanets&Asteroids");
        if (_dwarfCatalog == null || _dwarfCatalog.planets == null) {
            Debug.LogWarning("[Sandbox] 读取 Resources/Data/DwarfPlanets&Asteroids.json 失败");
        }
    }

    /// <summary>卫星目录（Data/Moons.json，格式与 Planets 相同）。</summary>
    private void LoadMoonCatalog() {
        _moonCatalog = SandboxPlanetModel.LoadCatalog("Data/Moons");
        if (_moonCatalog == null || _moonCatalog.planets == null) {
            Debug.LogWarning("[Sandbox] 读取 Resources/Data/Moons.json 失败");
        }
    }

    /// <summary>彩蛋目录（Data/EasterEggs.json，长方体，继承行星类）。</summary>
    private void LoadEggCatalog() {
        _eggCatalog = SandboxPlanetModel.LoadCatalog("Data/EasterEggs");
        if (_eggCatalog == null || _eggCatalog.planets == null) {
            Debug.LogWarning("[Sandbox] 读取 Resources/Data/EasterEggs.json 失败");
        }
    }

    /// <summary>
    /// 根据 JSON 里的 mass / metallicity / age 重新算一次温度，写回 temp_review。
    /// 编辑器里如果数值有变化会直接更新 Resources/Data/Stars.json。
    /// </summary>
    private void EnsureCatalogTemperatures() {
        if (_catalog == null || _catalog.stars == null) return;

        bool changed = false;
        foreach (StarData data in _catalog.stars) {
            if (data == null) continue;
            if (!SandboxStarModel.TryCompute(data.mass, data.metallicity, data.age,
                    out SandboxStarSnapshot snapshot, out _)) {
                continue;
            }

            double roundedTemperature = Math.Round(snapshot.temperatureK, 1);
            if (Math.Abs(data.temp_review - roundedTemperature) > 0.01) {
                data.temp_review = roundedTemperature;
                changed = true;
            }
        }

        if (!changed) return;

#if UNITY_EDITOR
        try {
            string path = Path.Combine(Application.dataPath, "Resources/Data/Stars.json");
            File.WriteAllText(path, JsonUtility.ToJson(_catalog, true));
            Debug.Log("[Sandbox] temp_review 已写回 Stars.json");
        } catch (Exception e) {
            Debug.LogWarning("[Sandbox] 写回 temp_review 失败: " + e.Message);
        }
#endif
    }

    private void SetupGrid() {
        GameObject gridGo = GameObject.Find("Grid");
        if (gridGo != null) {
            _grid = gridGo.transform;
            InfiniteGrid grid = gridGo.GetComponent<InfiniteGrid>();
            if (grid != null) {
                grid.startVisible = true;
                grid.SetVisible(true);
            }
            _placementPlane = new Plane(_grid.up, _grid.position);
        } else {
            _placementPlane = new Plane(Vector3.up, Vector3.zero);
        }
    }

    private void SetupPreview() {
        _preview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _preview.name = "SandboxPlacementPreview";
        Destroy(_preview.GetComponent<Collider>());

        Renderer renderer = _preview.GetComponent<Renderer>();
        if (renderer != null) {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Shader shader = Resources.Load<Shader>("Shaders/SandboxPreview");
            if (shader == null) shader = Shader.Find("UPB/SandboxPreview");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _previewMaterial = new Material(shader);
            _previewMaterial.color = new Color(1f, 1f, 1f, 0.30f);
            renderer.material = _previewMaterial;
        }

        _preview.SetActive(false);
        SetupFollowGuide();
    }

    /// <summary>创建天体预览球到“相机跟随目标”的浅灰色引导线。</summary>
    private void SetupFollowGuide() {
        if (_followGuideLine != null) return;

        GameObject lineGo = new("PlacementFollowGuide");
        lineGo.transform.SetParent(transform, false);
        _followGuideLine = lineGo.AddComponent<LineRenderer>();
        _followGuideLine.positionCount = 2;
        _followGuideLine.useWorldSpace = true;
        _followGuideLine.alignment = LineAlignment.View;
        _followGuideLine.textureMode = LineTextureMode.Stretch;
        _followGuideLine.numCapVertices = 2;
        _followGuideLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _followGuideLine.receiveShadows = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) _followGuideLine.material = new Material(shader);
        Color lineColor = new(0.78f, 0.78f, 0.78f, 0.95f);
        _followGuideLine.startColor = lineColor;
        _followGuideLine.endColor = lineColor;
        _followGuideLine.enabled = false;
    }

    private void EnsureFollowGuideLabel() {
        if (_followGuideText != null || _labelCanvasRect == null) return;

        GameObject go = new("FollowGuideDistance", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        // 挂在主 Canvas 上并放到最上层，避免被任何 UI 盖住。
        Transform parent = _canvas != null ? _canvas.transform : _labelCanvasRect;
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();

        _followGuideText = go.GetComponent<TextMeshProUGUI>();
        _followGuideText.font = _labelFont;
        if (_labelFont != null) _followGuideText.fontSharedMaterial = _labelFont.material;
        _followGuideText.fontSize = 22f;
        _followGuideText.color = new Color(1f, 1f, 0f, 1f);
        _followGuideText.outlineWidth = 0.2f;
        _followGuideText.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        _followGuideText.alignment = TextAlignmentOptions.Center;
        _followGuideText.enableWordWrapping = false;
        _followGuideText.raycastTarget = false;

        _followGuideRect = go.GetComponent<RectTransform>();
        _followGuideRect.anchorMin = _followGuideRect.anchorMax = new Vector2(0.5f, 0.5f);
        _followGuideRect.pivot = new Vector2(0.5f, 0.5f);
        _followGuideRect.sizeDelta = new Vector2(320f, 40f);

        go.SetActive(false);
    }

    private void HideFollowGuide() {
        if (_followGuideLine != null) _followGuideLine.enabled = false;
        if (_followGuideText != null && _followGuideText.gameObject.activeSelf) {
            _followGuideText.gameObject.SetActive(false);
        }
    }

    /// <summary>每帧更新：线两端 + 中点处的距离文字（自动单位，只读）。</summary>
    private void UpdateFollowGuide(Vector3 previewPosition) {
        if (_followGuideLine == null || _camera == null || _orbitCamera == null) return;

        Transform followed = _orbitCamera.target;
        if (followed == null) {
            HideFollowGuide();
            return;
        }

        EnsureFollowGuideLabel();

        _followGuideLine.enabled = true;
        _followGuideLine.SetPosition(0, previewPosition);
        _followGuideLine.SetPosition(1, followed.position);

        float camDistance = Vector3.Distance(_camera.transform.position, previewPosition);
        float width = Mathf.Max(camDistance * 0.0015f, 1e-5f);
        _followGuideLine.startWidth = width;
        _followGuideLine.endWidth = width;

        if (_followGuideText == null) return;

        double meters = SandboxWorldScale.ToVirtual(
                            Vector3.Distance(previewPosition, followed.position)) *
                        CelestialScale.KmPerUnit * 1000.0;
        _followGuideText.text = FormatDistanceAuto(meters);

        // 纯 2D UI 文字：固定在鼠标右下角，不依赖任何世界投影，
        // 拉多远、转什么角度都不会跑掉。
        Vector3 screen = Input.mousePosition + new Vector3(26f, -44f, 0f);
        float marginX = 70f;
        float marginY = 32f;
        screen.x = Mathf.Clamp(screen.x, marginX, Mathf.Max(marginX, Screen.width - marginX));
        screen.y = Mathf.Clamp(screen.y, marginY, Mathf.Max(marginY, Screen.height - marginY));

        _followGuideText.gameObject.SetActive(true);
        // 通过 Canvas 缩放换算，非 1:1 缩放的屏幕上也不会偏。
        RectTransform parentRect = _followGuideRect.parent as RectTransform;
        if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screen, null, out Vector2 localPoint)) {
            _followGuideRect.anchoredPosition = localPoint;
        } else {
            _followGuideRect.position = screen;
        }
        _followGuideText.transform.SetAsLastSibling();
    }

    /// <summary>距离文字用自动单位（没有按钮，用户改不了）。</summary>
    private static string FormatDistanceAuto(double meters) {
        SandboxUnitDefinition def = SandboxUnits.AutoDefinition("length", meters,
            new[] { "m", "km", "AU", "ly", "Mpc" });
        if (def == null) return FormatValue(meters) + " m";
        return FormatValue(SandboxUnits.FromBase(meters, def)) + " " + def.symbol;
    }

    private void SetupPlacementPivot() {
        _placementPivot = new GameObject("SandboxPlacementPivot");
        _placementPivot.transform.position = _templatePosition;
        _placementPivot.transform.localScale = Vector3.one;
    }

    private void WireUi() {
        if (_openCreateButton != null) _openCreateButton.onClick.AddListener(ToggleCreatePanel);
        if (_shutCreateButton != null) _shutCreateButton.onClick.AddListener(CloseCreatePanel);
        if (_openHomeButton != null) _openHomeButton.onClick.AddListener(OpenHomePanel);
        if (_nextPageButton != null) _nextPageButton.onClick.AddListener(() => ShowPage(_page + 1));
        if (_lastPageButton != null) _lastPageButton.onClick.AddListener(() => ShowPage(_page - 1));
        if (_starCategoryButton != null) {
            _starCategoryButton.onClick.AddListener(() => SetCategory(CreateCategory.Star));
        }
        if (_planetCategoryButton != null) {
            _planetCategoryButton.onClick.AddListener(() => SetCategory(CreateCategory.Planet));
        }
        if (_dwarfCategoryButton != null) {
            _dwarfCategoryButton.onClick.AddListener(() => SetCategory(CreateCategory.DwarfPlanet));
        }
        if (_moonCategoryButton != null) {
            _moonCategoryButton.onClick.AddListener(() => SetCategory(CreateCategory.Moon));
        }
        if (_eggCategoryButton != null) {
            _eggCategoryButton.onClick.AddListener(() => SetCategory(CreateCategory.EasterEgg));
        }
        if (_expandParameterButton != null) {
            _expandParameterButton.onClick.AddListener(OpenParameterPanel);
            // 展开按钮要在右侧面板之上，避免面板打开后挡住它。
            _expandParameterButton.transform.SetAsLastSibling();
        }
        if (_shutParameterPanelButton != null) {
            _shutParameterPanelButton.onClick.AddListener(CloseParameterPanel);
        }
        if (_shutStarParameterButton != null) {
            _shutStarParameterButton.onClick.AddListener(CloseParameterPanel);
        }
        if (_shutPlanetParameterButton != null) {
            _shutPlanetParameterButton.onClick.AddListener(CloseParameterPanel);
        }

        for (int i = 0; i < PageSize; i++) {
            int slot = i;
            if (_cardButtons[i] != null) {
                _cardButtons[i].onClick.AddListener(() => SelectCatalogCard(slot));
            }
        }

        if (_nameInput != null) _nameInput.onEndEdit.AddListener(OnNameEndEdit);
        if (_massInput != null) _massInput.onEndEdit.AddListener(OnMassEndEdit);
        if (_ageInput != null) _ageInput.onEndEdit.AddListener(OnAgeEndEdit);
        if (_metalInput != null) _metalInput.onEndEdit.AddListener(OnMetalEndEdit);

        if (_planetNameInput != null) _planetNameInput.onEndEdit.AddListener(OnPlanetNameEndEdit);
        if (_planetMassInput != null) _planetMassInput.onEndEdit.AddListener(OnPlanetMassEndEdit);
        if (_planetTempInput != null) _planetTempInput.onEndEdit.AddListener(OnPlanetTempEndEdit);
        if (_planetAtmosphereInput != null) {
            _planetAtmosphereInput.onEndEdit.AddListener(OnPlanetAtmosphereEndEdit);
        }
    }

    private void SetupFonts() {
        TMP_Text reference = FindText(_parameterPanel != null ? _parameterPanel.transform : null, "Label_Name");
        TMP_FontAsset font = reference != null ? reference.font : null;
        if (font == null && _titleContent != null) font = _titleContent.font;
        if (font == null) {
            TMP_Text any = _canvas.GetComponentInChildren<TMP_Text>(true);
            if (any != null) font = any.font;
        }
        if (font == null) return;

        foreach (TMP_Text text in _canvas.GetComponentsInChildren<TMP_Text>(true)) {
            if (text.font == null) {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
        }

        SetInputFont(_nameInput, font, "恒星名称");
        SetInputFont(_massInput, font, "1.0");
        SetInputFont(_ageInput, font, "4450");
        SetInputFont(_metalInput, font, "0.018");

        SetInputFont(_planetNameInput, font, "行星名称");
        SetInputFont(_planetMassInput, font, "1.0");
        SetInputFont(_planetTempInput, font, "288");
        SetInputFont(_planetAtmosphereInput, font, "1.0");

        foreach (TMP_Text text in _canvas.GetComponentsInChildren<TMP_Text>(true)) {
            if (text.color.a < 0.05f) text.color = TextMain;
        }
    }

    private static void SetInputFont(TMP_InputField input, TMP_FontAsset font, string placeholderText) {
        if (input == null) return;

        if (input.textComponent != null) {
            input.textComponent.font = font;
            input.textComponent.fontSharedMaterial = font.material;
            input.textComponent.color = TextMain;
        }

        if (input.placeholder is TMP_Text placeholder) {
            placeholder.font = font;
            placeholder.fontSharedMaterial = font.material;
            placeholder.color = TextDim;
            placeholder.text = placeholderText;
        }
    }

    private void StyleUi() {
        SetPanelImage(_createPanel, PanelBg);
        SetPanelImage(_parameterPanel, PanelBg);
        SetPanelImage(_planetParameterPanel, PanelBg);

        SetInputImage(_nameInput, InputBg);
        SetInputImage(_massInput, InputBg);
        SetInputImage(_ageInput, InputBg);
        SetInputImage(_metalInput, InputBg);

        SetInputImage(_planetNameInput, InputBg);
        SetInputImage(_planetMassInput, InputBg);
        SetInputImage(_planetTempInput, InputBg);
        SetInputImage(_planetAtmosphereInput, InputBg);

        // 所有可输入框统一成深色背景，包括 Mechanics 的 x/y/z、自转周期
        // 以及运行时生成的温室系数输入框。
        StyleAllInputs(_parameterPanel);
        StyleAllInputs(_planetParameterPanel);
        MakeSubPanelOpaque(_parameterPanel);
        MakeSubPanelOpaque(_planetParameterPanel);

        for (int i = 0; i < PageSize; i++) {
            if (_cardImages[i] != null) _cardImages[i].color = CardBg;
            if (_cardPlates[i] != null) _cardPlates[i].color = RowBg;
            if (_cardNames[i] != null) _cardNames[i].color = TextMain;
        }

        if (_createPanel != null) {
            foreach (RawImage raw in _createPanel.GetComponentsInChildren<RawImage>(true)) {
                if (raw.texture == null) raw.color = RowBg;
            }
            foreach (Image image in _createPanel.GetComponentsInChildren<Image>(true)) {
                if (image.gameObject.name.StartsWith("Button_")) image.color = ButtonBg;
            }
        }

        if (_parameterPanel != null) {
            foreach (RawImage raw in _parameterPanel.GetComponentsInChildren<RawImage>(true)) {
                if (raw.texture == null) raw.color = RowBg;
            }
            foreach (Image image in _parameterPanel.GetComponentsInChildren<Image>(true)) {
                if (image.gameObject.name.StartsWith("Button") ||
                    image.gameObject.name == "Unit_Switch") {
                    image.color = ButtonBg;
                }
            }
        }

        if (_planetParameterPanel != null) {
            foreach (RawImage raw in _planetParameterPanel.GetComponentsInChildren<RawImage>(true)) {
                if (raw.texture == null) raw.color = RowBg;
            }
            foreach (Image image in _planetParameterPanel.GetComponentsInChildren<Image>(true)) {
                if (image.gameObject.name.StartsWith("Button") ||
                    image.gameObject.name == "Unit_Switch") {
                    image.color = ButtonBg;
                }
            }
            foreach (TMP_Text text in _planetParameterPanel.GetComponentsInChildren<TMP_Text>(true)) {
                text.color = TextMain;
            }
            SetInputColors(_planetNameInput);
            SetInputColors(_planetMassInput);
            SetInputColors(_planetTempInput);
            SetInputColors(_planetAtmosphereInput);
        }

        // 创建面板里也有几个 Unit_Switch。
        if (_createPanel != null) {
            foreach (Image image in _createPanel.GetComponentsInChildren<Image>(true)) {
                if (image.gameObject.name == "Unit_Switch") image.color = ButtonBg;
            }
        }

        SetButtonImage(_openCreateButton, Accent);
        SetButtonImage(_shutCreateButton, ButtonBg);
        SetButtonImage(_nextPageButton, ButtonBg);
        SetButtonImage(_lastPageButton, ButtonBg);
        SetButtonImage(_expandParameterButton, Accent);
        SetButtonImage(_shutParameterPanelButton, ButtonBg);

        if (_createPanel != null) {
            foreach (TMP_Text text in _createPanel.GetComponentsInChildren<TMP_Text>(true)) {
                text.color = TextMain;
            }
        }
        if (_parameterPanel != null) {
            foreach (TMP_Text text in _parameterPanel.GetComponentsInChildren<TMP_Text>(true)) {
                text.color = TextMain;
            }
        }

        // 输入框占位符单独用暗色；输入文本用亮色。
        SetInputColors(_nameInput);
        SetInputColors(_massInput);
        SetInputColors(_ageInput);
        SetInputColors(_metalInput);

        if (_pageText != null) _pageText.color = TextDim;
        if (_titleContent != null) _titleContent.color = TextMain;

        TMP_Text nextText = FindText(_nextPageButton != null ? _nextPageButton.transform : null, "Text (TMP)");
        if (nextText != null) nextText.text = ">";
    }

    private static void SetPanelImage(GameObject go, Color color) {
        if (go == null) return;
        Image image = go.GetComponent<Image>();
        if (image != null) image.color = color;
    }

    private static void SetInputImage(TMP_InputField input, Color color) {
        if (input == null) return;
        Image image = input.GetComponent<Image>();
        if (image == null) return;
        // 输入框模板用的是浅色 InputFieldBackground sprite；直接换掉 sprite，
        // 只保留纯色背景，避免运行时仍显得发亮/半透明。
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
    }

    private static void StyleAllInputs(GameObject panel) {
        if (panel == null) return;
        foreach (TMP_InputField input in panel.GetComponentsInChildren<TMP_InputField>(true)) {
            SetInputImage(input, InputBg);
            SetInputColors(input);
        }
    }

    private static void MakeSubPanelOpaque(GameObject panel) {
        if (panel == null) return;
        ForceOpaque(FindDeep(panel.transform, "Block_Switch_Panel"));
        ForceOpaque(FindDeep(panel.transform, "TitlePanel"));
        ForceOpaque(FindDeep(panel.transform, "ShutTheParameterPanel"));
    }

    private static void ForceOpaque(Transform root) {
        if (root == null) return;
        foreach (Image image in root.GetComponentsInChildren<Image>(true)) {
            Color c = image.color;
            c.a = 1f;
            image.color = c;
        }
        foreach (RawImage raw in root.GetComponentsInChildren<RawImage>(true)) {
            Color c = raw.color;
            c.a = 1f;
            raw.color = c;
        }
    }

    private static void SetInputColors(TMP_InputField input) {
        if (input == null) return;
        if (input.textComponent != null) input.textComponent.color = TextMain;
        if (input.placeholder is TMP_Text placeholder) placeholder.color = TextDim;
    }

    private static void SetButtonImage(Button button, Color color) {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null) image.color = color;
        RawImage raw = button.GetComponent<RawImage>();
        if (raw != null && raw.texture == null) raw.color = color;
    }

    // ~~~ 创建卡片色圈 / 恒星标签 ~~~ //

    private void SetupCardDots() {
        EnsureCircleSprite();
        for (int i = 0; i < PageSize; i++) {
            if (_cardButtons[i] == null) continue;

            GameObject go = new("StarColorCircle",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_cardButtons[i].transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(190f, 190f);
            rect.anchoredPosition = new Vector2(0f, 25f);

            Image image = go.GetComponent<Image>();
            image.sprite = _circleSprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.color = Color.white;

            // 放在名字条后面，避免挡住卡片名字。
            go.transform.SetSiblingIndex(0);
            _cardDots[i] = image;

            GameObject textureGo = new("PlanetTextureCard",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            textureGo.transform.SetParent(_cardButtons[i].transform, false);
            RectTransform textureRect = textureGo.GetComponent<RectTransform>();
            textureRect.anchorMin = textureRect.anchorMax = new Vector2(0.5f, 0.5f);
            textureRect.pivot = new Vector2(0.5f, 0.5f);
            textureRect.sizeDelta = new Vector2(190f, 190f);
            textureRect.anchoredPosition = new Vector2(0f, 25f);
            RawImage raw = textureGo.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;
            raw.uvRect = new Rect(0f, 0f, 1f, 1f);
            textureGo.SetActive(false);
            textureGo.transform.SetSiblingIndex(0);
            _cardTextures[i] = raw;
        }
    }

    private static Texture2D LoadPlanetCardTexture(string path) {
        if (string.IsNullOrEmpty(path)) return null;
        return SandboxPlanetCardRenderer.Render(path);
    }

    private void EnsureCircleSprite() {
        if (_circleSprite != null) return;

        const int size = 128;
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 2f;

        _circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "SandboxStarCircle"
        };

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / 1.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        _circleTexture.SetPixels32(pixels);
        _circleTexture.Apply(false, false);
        _circleSprite = Sprite.Create(
            _circleTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Color TemperatureColor(double temperatureK) {
        if (temperatureK <= 1.0 || double.IsNaN(temperatureK)) {
            return new Color(0.55f, 0.55f, 0.55f, 1f);
        }

        var (r, g, b) = Spectrum.GetBlackbodyColor(temperatureK);
        return new Color(r, g, b, 1f);
    }

    private void SetupLabelCanvas() {
        GameObject go = new("SandboxStarLabelCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);

        _labelCanvas = go.GetComponent<Canvas>();
        _labelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _labelCanvas.sortingOrder = LabelSortingOrder;

        // 自适应：按 1920×1080 参考分辨率缩放，4K / 小屏上标签大小一致。
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        _labelCanvasRect = go.GetComponent<RectTransform>();
        _labelCanvasRect.anchorMin = Vector2.zero;
        _labelCanvasRect.anchorMax = Vector2.one;
        _labelCanvasRect.offsetMin = Vector2.zero;
        _labelCanvasRect.offsetMax = Vector2.zero;

        _labelFont = Resources.Load<TMP_FontAsset>("Fonts/KosefontP-JP SDF 1")
            ?? Resources.Load<TMP_FontAsset>("Fonts/FRCFONT SDF");
        if (_labelFont == null && _titleContent != null) _labelFont = _titleContent.font;
        if (_labelFont == null) {
            TMP_Text any = _canvas.GetComponentInChildren<TMP_Text>(true);
            if (any != null) _labelFont = any.font;
        }
    }

    /// <summary>物理碰撞把恒星打碎后，从沙盘 UI/列表里移除它（碎片本身不加 UI）。</summary>
    public void RemoveStarFromSandbox(SandboxStar star) {
        if (star == null) return;
        _stars.Remove(star);
        RemoveBodyLabel(star.transform);
        if (_selectedStar == star) DeselectStar();
        Destroy(star.gameObject);
    }

    /// <summary>
    /// 彩蛋撞击：把被撞的行星原地转成恒星（名字不变），质量 = 原质量 + addedMassKg。
    /// 返回 false 表示转换失败（模型参数越界等），此时调用方只加质量。
    /// </summary>
    public bool ConvertPlanetToStar(SandboxPhysicsBody body, double addedMassKg) {
        if (body == null || !body.alive || body.planet == null) return false;

        SandboxPlanet planet = body.planet;
        string starName = string.IsNullOrEmpty(planet.planetName) ? body.name : planet.planetName;
        double newMassSun = (body.massKg + addedMassKg) / SandboxPhysicsSystem.SunMassKg;
        if (newMassSun <= 0.0) return false;

        StarData starData = new() {
            name = starName,
            mass = newMassSun,
            metallicity = 0.02,
            age = 5000.0,
            rotationPeriodDays = 25.38
        };
        if (!SandboxStarModel.TryCompute(starData.mass, starData.metallicity, starData.age,
                starData.rotationPeriodDays,
                out SandboxStarSnapshot starSnapshot, out string error)) {
            Debug.LogWarning("[EasterEgg] 转恒星失败：" + error);
            return false;
        }

        SandboxDVec3 position = body.positionM;
        SandboxDVec3 velocity = body.velocityMps;
        bool wasOrbiting = body.isOrbiting;
        SandboxPhysicsBody orbitTarget = body.orbitTarget;
        double orbitRadius = body.orbitRadiusM;
        Vector3 renderPosition = planet.transform.position;

        // 先撤掉行星，再原地造一颗恒星。
        SandboxPhysicsSystem.Unregister(planet);
        RemovePlanetFromSandbox(planet);

        int before = _stars.Count;
        CreateStar(starData, starSnapshot, renderPosition);
        if (_stars.Count <= before) return false;

        SandboxStar star = _stars[_stars.Count - 1];
        // 转换完直接选中新恒星，让参数面板切过去（否则还停在旧的行星面板）。
        SelectStar(star);
        SandboxPhysicsBody starBody = SandboxPhysicsSystem.GetBody(star);
        if (starBody != null) {
            starBody.positionM = position;
            starBody.velocityMps = velocity;
            starBody.orbitTarget = wasOrbiting ? orbitTarget : null;
            starBody.isOrbiting = wasOrbiting && orbitTarget != null;
            starBody.orbitRadiusM = orbitRadius;
            star.SetLogicalPosition(position.x / MetersPerUnit,
                position.y / MetersPerUnit, position.z / MetersPerUnit);
        }

        FloatingText.Show("撞击让 " + starName + " 变成了恒星");
        return true;
    }
    /// <summary>物理碰撞把行星打碎后，从沙盘 UI/列表里移除它（碎片本身不加 UI）。</summary>
    public void RemovePlanetFromSandbox(SandboxPlanet planet) {        if (planet == null) return;
        _planets.Remove(planet);
        RemoveBodyLabel(planet.transform);
        if (_selectedPlanet == planet) DeselectStar();
        Destroy(planet.gameObject);
    }

    private void RemoveBodyLabel(Transform body) {
        for (int i = _bodyLabels.Count - 1; i >= 0; i--) {
            BodyLabel label = _bodyLabels[i];
            if (label == null) {
                _bodyLabels.RemoveAt(i);
                continue;
            }
            if (label.body != body && label.body != null) continue;
            if (label.text != null) Destroy(label.text.gameObject);
            _bodyLabels.RemoveAt(i);
        }
    }

    /// <summary>恒星碰撞重算参数后，刷新当前打开的恒星参数面板。</summary>
    public void RefreshSelectedStar(SandboxStar star) {
        if (star == null || _selectedStar != star) return;
        if (_parameterPanel != null && _parameterPanel.activeSelf) {
            RefreshParameterFields();
        }
        RefreshLockButtonState(_starLockPositionButton,
            SandboxPhysicsSystem.GetBody(star));
    }

    private void AddStarLabel(SandboxStar star) {
        if (star == null) return;
        AddBodyLabel(star.transform, star.starName, star, null, "StarLabel");
    }

    private void AddPlanetLabel(SandboxPlanet planet) {
        if (planet == null) return;
        AddBodyLabel(planet.transform, planet.planetName, null, planet, "PlanetLabel");
    }

    private void AddBodyLabel(Transform body, string displayName, SandboxStar star,
            SandboxPlanet planet, string objectName) {
        if (body == null || _labelCanvasRect == null) return;

        GameObject go = new(objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(_labelCanvasRect, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = _labelFont;
        if (_labelFont != null) text.fontSharedMaterial = _labelFont.material;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.color = star != null ? StarLabelColor(star) : Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = displayName;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(LabelWidth, LabelHeight);

        text.gameObject.SetActive(_labelsVisible);

        _bodyLabels.Add(new BodyLabel {
            body = body,
            star = star,
            planet = planet,
            text = text,
            rect = rect,
            lastName = displayName
        });
    }

    private static Color StarLabelColor(SandboxStar star) {
        if (star == null || star.snapshot == null) return Color.white;
        return TemperatureColor(star.snapshot.temperatureK);
    }

    private void UpdateBodyLabels() {
        if (_labelCanvasRect == null || _bodyLabels.Count == 0 || _camera == null) return;

        if (!_labelsVisible) {
            foreach (BodyLabel hidden in _bodyLabels) {
                if (hidden.text != null) hidden.text.gameObject.SetActive(false);
            }
            return;
        }

        List<LabelCandidate> candidates = new();
        foreach (BodyLabel label in _bodyLabels) {
            if (label.body == null || label.text == null) {
                if (label.text != null) label.text.gameObject.SetActive(false);
                continue;
            }

            string displayName = label.star != null
                ? label.star.starName
                : label.planet != null ? label.planet.planetName : "";
            if (label.lastName != displayName) {
                label.lastName = displayName;
                label.text.text = displayName;
            }

            // 恒星标签颜色跟随黑体辐射色；行星标签固定白色。
            label.text.color = label.star != null
                ? StarLabelColor(label.star)
                : Color.white;

            // 先检查相机深度范围：远裁剪面外的天体不再交给 WorldToScreenPoint，
            // 避免超远距离缩放时 Unity 反复刷 "Screen position out of view frustum"。
            Vector3 bodyPosition = label.body.position;
            float labelDepth = Vector3.Dot(
                bodyPosition - _camera.transform.position, _camera.transform.forward);
            if (labelDepth <= _camera.nearClipPlane || labelDepth >= _camera.farClipPlane) {
                label.text.gameObject.SetActive(false);
                continue;
            }

            Vector3 screen = _camera.WorldToScreenPoint(bodyPosition);
            if (screen.z <= 0f) {
                label.text.gameObject.SetActive(false);
                continue;
            }

            float radiusPixels;
            if (label.star != null && label.star.lightPoint != null &&
                label.star.lightPoint.IsDotMode) {
                StarLightPoint lightPoint = label.star.lightPoint;
                if (!lightPoint.DotVisible || lightPoint.DotAlpha < 0.05f) {
                    label.text.gameObject.SetActive(false);
                    continue;
                }
                radiusPixels = Mathf.Max(lightPoint.DotPixelSize * 0.5f, 2f);
            } else {
                // 投影在渲染空间里做：直接用已经乘过世界缩放的 localScale。
                double visualDiameter = label.body.localScale.x;
                float distance = Vector3.Distance(_camera.transform.position, label.body.position);
                float diameterPixels = StarLightPoint.ProjectedDiameterPixels(
                    visualDiameter, distance, _camera.fieldOfView, Screen.height);
                radiusPixels = Mathf.Max(diameterPixels * 0.5f, 2f);
            }

            float left = screen.x + radiusPixels + LabelOffsetPixels;
            float top = screen.y - LabelHeight * 0.5f;
            if (left + LabelWidth < 0f || left > Screen.width + LabelWidth ||
                top + LabelHeight < 0f || top > Screen.height) {
                label.text.gameObject.SetActive(false);
                continue;
            }

            candidates.Add(new LabelCandidate {
                label = label,
                screenPoint = new Vector2(left, screen.y),
                rect = new Rect(left, top, LabelWidth, LabelHeight),
                radius = radiusPixels
            });
        }

        // 屏幕空间上重叠时，保留投影半径更大的天体标签。
        candidates.Sort((a, b) => b.radius.CompareTo(a.radius));
        List<Rect> accepted = new();

        foreach (LabelCandidate candidate in candidates) {
            bool overlapped = false;
            foreach (Rect rect in accepted) {
                if (rect.Overlaps(candidate.rect)) {
                    overlapped = true;
                    break;
                }
            }

            if (overlapped) {
                candidate.label.text.gameObject.SetActive(false);
                continue;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _labelCanvasRect, candidate.screenPoint, null, out Vector2 localPoint)) {
                candidate.label.rect.anchoredPosition =
                    localPoint - _labelCanvasRect.rect.center;
                candidate.label.text.gameObject.SetActive(true);
                accepted.Add(candidate.rect);
            } else {
                candidate.label.text.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
        if (_circleSprite != null) Destroy(_circleSprite);
        if (_circleTexture != null) Destroy(_circleTexture);
    }

    // ~~~ 创建面板 / 分页 ~~~ //

    private int StarPageCount => Mathf.Max(1,
        Mathf.CeilToInt(_starResults.Count / (float)PageSize));

    private int PlanetPageCount => Mathf.Max(1,
        Mathf.CeilToInt(_planetResults.Count / (float)PageSize));

    private int DwarfPageCount => Mathf.Max(1,
        Mathf.CeilToInt(_dwarfResults.Count / (float)PageSize));

    private int MoonPageCount => Mathf.Max(1,
        Mathf.CeilToInt(_moonResults.Count / (float)PageSize));

    private int EggPageCount => Mathf.Max(1,
        Mathf.CeilToInt(_eggResults.Count / (float)PageSize));

    private int PageCount => _category switch {
        CreateCategory.Star => StarPageCount,
        CreateCategory.Planet => PlanetPageCount,
        CreateCategory.DwarfPlanet => DwarfPageCount,
        CreateCategory.Moon => MoonPageCount,
        _ => EggPageCount
    };

    private void EnsureHomePanel() {
        if (_homePanel != null || _canvas == null) return;
        _homePanel = FindDeep(_canvas, "HomePagePanel")?.gameObject ??
                     FindDeep(_canvas, "HomePanel")?.gameObject ??
                     FindDeep(_canvas, "HomePage")?.gameObject ??
                     FindDeep(_canvas, "Home")?.gameObject;
    }

    /// <summary>设置面板打开时把创建面板收起来（设置按钮本身也因此重新可见）。</summary>
    public void CloseCreatePanelForSettings() {
        if (_createPanel == null || !_createPanel.activeSelf) return;
        SetCreatePanelOpen(false);
    }

    private void SetCreatePanelOpen(bool open) {
        EnsureHomePanel();
        if (_createPanel != null) _createPanel.SetActive(open);
        if (open && _createPanel != null) {
            TrackPanel(_createPanel, () => SetCreatePanelOpen(false));
        }
        if (open && _homePanel != null) _homePanel.SetActive(false);
        if (_openCreateButton != null) _openCreateButton.gameObject.SetActive(!open);
        if (_openHomeButton != null) _openHomeButton.gameObject.SetActive(!open);
        _settingsPanel?.SetOpenButtonVisible(!open);
        UpdateMotionButtons();
    }

    private void OpenHomePanel() {
        EnsureHomePanel();
        if (_homePanel == null) return;
        SetCreatePanelOpen(false);
        _homePanel.SetActive(true);
        _homePanel.transform.SetAsLastSibling();
        _homePage?.Show();
        TrackPanel(_homePanel, () => _homePage?.Hide());
    }

    private double PendingMassKg {
        get {
            if (_placementSnapshot != null) {
                return _placementSnapshot.mass * SandboxPhysicsSystem.SunMassKg;
            }
            if (_placementPlanetSnapshot != null) {
                return _placementPlanetSnapshot.massEarth * SandboxPhysicsSystem.EarthMassKg;
            }
            return 0.0;
        }
    }

    private bool HasHeavierBody(double massKg) {
        foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
            if (body == null || !body.alive) continue;
            if (body.massKg > massKg + 1e-9) return true;
        }
        return false;
    }

    private void SetPlacementMotion(PlacementMotion motion) {
        if (motion == PlacementMotion.Orbiting && !HasHeavierBody(PendingMassKg)) {
            motion = PlacementMotion.Static;
        }
        _placementMotion = motion;
        UpdateMotionButtons();
        UpdatePlacementPreview();
    }

    private void ResetPlacementMotion() {
        _placementMotion = HasHeavierBody(PendingMassKg)
            ? PlacementMotion.Orbiting
            : PlacementMotion.Static;
        UpdateMotionButtons();
    }

    private void UpdateMotionButtons() {
        bool show = _createPanel != null && _createPanel.activeSelf &&
                    (_placementEntry != null || _placementPlanetData != null);

        if (_orbitModeButton != null) _orbitModeButton.gameObject.SetActive(show);
        if (_staticModeButton != null) _staticModeButton.gameObject.SetActive(show);
        if (_shootModeButton != null) _shootModeButton.gameObject.SetActive(show);

        // 速度面板和单位切换只在发射模式显示。
        bool launch = show && _placementMotion == PlacementMotion.Launch;
        if (_shootSpeedPanel != null) _shootSpeedPanel.SetActive(launch);
        if (_shootUnitSwitch != null) _shootUnitSwitch.SetActive(launch);

        if (!show) return;

        SetMotionButtonTint(_orbitModeButton, _placementMotion == PlacementMotion.Orbiting);
        SetMotionButtonTint(_staticModeButton, _placementMotion == PlacementMotion.Static);
        SetMotionButtonTint(_shootModeButton, _placementMotion == PlacementMotion.Launch);
    }

    private static void SetMotionButtonTint(Button button, bool selected) {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null) {
            image.color = selected ? Accent : ButtonBg;
            return;
        }
        RawImage raw = button.GetComponent<RawImage>();
        if (raw != null) raw.color = selected ? Accent : ButtonBg;
    }

    private void ToggleCreatePanel() {
        if (_createPanel == null) return;

        bool open = !_createPanel.activeSelf;
        SetCreatePanelOpen(open);
        if (open) {
            CancelPlacement();
            DeselectStar();
            ShowPage(_page);
        }
    }

    private void CloseCreatePanel() {
        SetCreatePanelOpen(false);
    }

    private void SetCategory(CreateCategory category) {
        if (_category != category) {
            CancelPlacement();
            DeselectStar();
        }
        _category = category;
        _starPage = 0;
        _planetPage = 0;
        _dwarfPage = 0;
        _moonPage = 0;
        _eggPage = 0;
        ShowPage(0);
        UpdateCategoryVisuals();
        Canvas.ForceUpdateCanvases();
    }

    private void ShowPage(int page) {
        switch (_category) {
            case CreateCategory.Star:
                ShowStarPage(page);
                break;
            case CreateCategory.Planet:
                ShowPlanetPage(page);
                break;
            case CreateCategory.DwarfPlanet:
                ShowDwarfPage(page);
                break;
            case CreateCategory.Moon:
                ShowMoonPage(page);
                break;
            default:
                ShowEggPage(page);
                break;
        }
        UpdateCategoryVisuals();
    }

    // ~~~ 搜索框 IF_Search ~~~ //

    /// <summary>
    /// 找到创建面板里的 IF_Search，输入时按名字过滤当前分类的卡片。
    /// 名字匹配不区分大小写，空串表示不过滤。
    /// </summary>
    private void SetupSearch() {
        if (_createPanel != null) {
            Transform input = FindDeep(_createPanel.transform, "IF_Search");
            if (input != null) _searchInput = input.GetComponent<TMP_InputField>();
        }

        if (_searchInput == null) {
            Debug.LogWarning("[Sandbox] 创建面板里没找到 IF_Search，搜索功能未启用");
        } else {
            _searchInput.onValueChanged.RemoveListener(OnSearchValueChanged);
            _searchInput.onValueChanged.AddListener(OnSearchValueChanged);
            _searchQuery = _searchInput.text ?? string.Empty;
        }

        RebuildSearchResults();
    }

    private void OnSearchValueChanged(string value) {
        _searchQuery = value ?? string.Empty;
        _starPage = 0;
        _planetPage = 0;
        _dwarfPage = 0;
        _moonPage = 0;
        _eggPage = 0;
        RebuildSearchResults();
        ShowPage(0);
        Canvas.ForceUpdateCanvases();
    }

    private void RebuildSearchResults() {
        _starResults.Clear();
        _planetResults.Clear();
        _dwarfResults.Clear();
        _moonResults.Clear();
        _eggResults.Clear();

        string query = (_searchQuery ?? string.Empty).Trim();

        if (_catalog?.stars != null) {
            for (int i = 0; i < _catalog.stars.Length; i++) {
                StarData star = _catalog.stars[i];
                if (star == null) continue;
                if (MatchesQuery(star.name, query)) _starResults.Add(i);
            }
        }

        if (_planetCatalog?.planets != null) {
            for (int i = 0; i < _planetCatalog.planets.Length; i++) {
                PlanetData planet = _planetCatalog.planets[i];
                if (planet == null) continue;
                if (MatchesQuery(planet.name, query)) _planetResults.Add(i);
            }
        }

        if (_dwarfCatalog?.planets != null) {
            for (int i = 0; i < _dwarfCatalog.planets.Length; i++) {
                PlanetData dwarf = _dwarfCatalog.planets[i];
                if (dwarf == null) continue;
                if (MatchesQuery(dwarf.name, query)) _dwarfResults.Add(i);
            }
        }

        if (_moonCatalog?.planets != null) {
            for (int i = 0; i < _moonCatalog.planets.Length; i++) {
                PlanetData moon = _moonCatalog.planets[i];
                if (moon == null) continue;
                if (MatchesQuery(moon.name, query)) _moonResults.Add(i);
            }
        }

        if (_eggCatalog?.planets != null) {
            for (int i = 0; i < _eggCatalog.planets.Length; i++) {
                PlanetData egg = _eggCatalog.planets[i];
                if (egg == null) continue;
                if (MatchesQuery(egg.name, query)) _eggResults.Add(i);
            }
        }

        int starPages = Mathf.Max(1, Mathf.CeilToInt(_starResults.Count / (float)PageSize));
        if (_starPage >= starPages) _starPage = starPages - 1;
        int planetPages = Mathf.Max(1, Mathf.CeilToInt(_planetResults.Count / (float)PageSize));
        if (_planetPage >= planetPages) _planetPage = planetPages - 1;
        int dwarfPages = Mathf.Max(1, Mathf.CeilToInt(_dwarfResults.Count / (float)PageSize));
        if (_dwarfPage >= dwarfPages) _dwarfPage = dwarfPages - 1;
        int moonPages = Mathf.Max(1, Mathf.CeilToInt(_moonResults.Count / (float)PageSize));
        if (_moonPage >= moonPages) _moonPage = moonPages - 1;
        int eggPages = Mathf.Max(1, Mathf.CeilToInt(_eggResults.Count / (float)PageSize));
        if (_eggPage >= eggPages) _eggPage = eggPages - 1;
    }

    private static bool MatchesQuery(string name, string query) {
        if (string.IsNullOrEmpty(query)) return true;
        if (string.IsNullOrEmpty(name)) return false;
        return name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
    private void ShowStarPage(int page) {
        if (_catalog == null || _catalog.stars == null) return;

        int pageCount = StarPageCount;
        _page = ((page % pageCount) + pageCount) % pageCount;
        _starPage = _page;

        for (int i = 0; i < PageSize; i++) {
            int index = _page * PageSize + i;
            bool has = index < _starResults.Count;
            if (_cardButtons[i] != null) _cardButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            StarData data = _catalog.stars[_starResults[index]];
            if (_cardNames[i] != null) _cardNames[i].text = data.name;
            bool selected = data == _placementEntry;
            if (_cardImages[i] != null) {
                _cardImages[i].color = selected ? CardSelectedBg : CardBg;
            }
            if (_cardPlates[i] != null) {
                _cardPlates[i].color = selected ? CardSelectedBg : RowBg;
            }
            if (_cardDots[i] != null) {
                _cardDots[i].gameObject.SetActive(true);
                _cardDots[i].color = TemperatureColor(data.temp_review);
            }
            if (_cardTextures[i] != null) _cardTextures[i].gameObject.SetActive(false);
        }

        if (_pageText != null) _pageText.text = $"页码 {_page + 1}/{pageCount}";
        if (_nextPageButton != null) _nextPageButton.interactable = pageCount > 1;
        if (_lastPageButton != null) _lastPageButton.interactable = pageCount > 1;
    }

    private void ShowPlanetPage(int page) {
        if (_planetCatalog == null || _planetCatalog.planets == null) return;

        int pageCount = PlanetPageCount;
        _page = ((page % pageCount) + pageCount) % pageCount;
        _planetPage = _page;

        for (int i = 0; i < PageSize; i++) {
            int index = _page * PageSize + i;
            bool has = index < _planetResults.Count;
            if (_cardButtons[i] != null) _cardButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            PlanetData data = _planetCatalog.planets[_planetResults[index]];
            if (_cardNames[i] != null) _cardNames[i].text = data.name;
            bool selected = data == _placementPlanetData;
            if (_cardImages[i] != null) {
                _cardImages[i].color = selected ? CardSelectedBg : CardBg;
            }
            if (_cardPlates[i] != null) {
                _cardPlates[i].color = selected ? CardSelectedBg : RowBg;
            }
            Texture2D texture = LoadPlanetCardTexture(data.SurfaceTexture);
            if (_cardTextures[i] != null) {
                _cardTextures[i].texture = texture;
                _cardTextures[i].gameObject.SetActive(texture != null);
                _cardTextures[i].color = Color.white;
            }
            if (_cardDots[i] != null) {
                _cardDots[i].gameObject.SetActive(texture == null);
                _cardDots[i].color = Color.white;
            }
        }

        if (_pageText != null) _pageText.text = $"页码 {_page + 1}/{pageCount}";
        if (_nextPageButton != null) _nextPageButton.interactable = pageCount > 1;
        if (_lastPageButton != null) _lastPageButton.interactable = pageCount > 1;
    }

    /// <summary>矮行星 / 小行星页：卡片规则与行星页一致，只是换成自己的目录。</summary>
    private void ShowDwarfPage(int page) {
        if (_dwarfCatalog == null || _dwarfCatalog.planets == null) return;

        int pageCount = DwarfPageCount;
        _page = ((page % pageCount) + pageCount) % pageCount;
        _dwarfPage = _page;

        for (int i = 0; i < PageSize; i++) {
            int index = _page * PageSize + i;
            bool has = index < _dwarfResults.Count;
            if (_cardButtons[i] != null) _cardButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            PlanetData data = _dwarfCatalog.planets[_dwarfResults[index]];
            if (_cardNames[i] != null) _cardNames[i].text = data.name;
            bool selected = data == _placementPlanetData;
            if (_cardImages[i] != null) {
                _cardImages[i].color = selected ? CardSelectedBg : CardBg;
            }
            if (_cardPlates[i] != null) {
                _cardPlates[i].color = selected ? CardSelectedBg : RowBg;
            }
            Texture2D texture = LoadPlanetCardTexture(data.SurfaceTexture);
            if (_cardTextures[i] != null) {
                _cardTextures[i].texture = texture;
                _cardTextures[i].gameObject.SetActive(texture != null);
                _cardTextures[i].color = Color.white;
            }
            if (_cardDots[i] != null) {
                _cardDots[i].gameObject.SetActive(texture == null);
                _cardDots[i].color = Color.white;
            }
        }

        if (_pageText != null) _pageText.text = $"页码 {_page + 1}/{pageCount}";
        if (_nextPageButton != null) _nextPageButton.interactable = pageCount > 1;
        if (_lastPageButton != null) _lastPageButton.interactable = pageCount > 1;
    }
    /// <summary>卫星页：卡片规则与行星页一致，只是换成卫星目录。</summary>
    private void ShowMoonPage(int page) {
        if (_moonCatalog == null || _moonCatalog.planets == null) return;

        int pageCount = MoonPageCount;
        _page = ((page % pageCount) + pageCount) % pageCount;
        _moonPage = _page;

        for (int i = 0; i < PageSize; i++) {
            int index = _page * PageSize + i;
            bool has = index < _moonResults.Count;
            if (_cardButtons[i] != null) _cardButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            PlanetData data = _moonCatalog.planets[_moonResults[index]];
            if (_cardNames[i] != null) _cardNames[i].text = data.name;
            bool selected = data == _placementPlanetData;
            if (_cardImages[i] != null) {
                _cardImages[i].color = selected ? CardSelectedBg : CardBg;
            }
            if (_cardPlates[i] != null) {
                _cardPlates[i].color = selected ? CardSelectedBg : RowBg;
            }
            Texture2D texture = LoadPlanetCardTexture(data.SurfaceTexture);
            if (_cardTextures[i] != null) {
                _cardTextures[i].texture = texture;
                _cardTextures[i].gameObject.SetActive(texture != null);
                _cardTextures[i].color = Color.white;
            }
            if (_cardDots[i] != null) {
                _cardDots[i].gameObject.SetActive(texture == null);
                _cardDots[i].color = Color.white;
            }
        }

        if (_pageText != null) _pageText.text = $"页码 {_page + 1}/{pageCount}";
        if (_nextPageButton != null) _nextPageButton.interactable = pageCount > 1;
        if (_lastPageButton != null) _lastPageButton.interactable = pageCount > 1;
    }
    /// <summary>彩蛋页：长方体，卡片规则同行星页。</summary>
    private void ShowEggPage(int page) {
        if (_eggCatalog == null || _eggCatalog.planets == null) return;

        int pageCount = EggPageCount;
        _page = ((page % pageCount) + pageCount) % pageCount;
        _eggPage = _page;

        for (int i = 0; i < PageSize; i++) {
            int index = _page * PageSize + i;
            bool has = index < _eggResults.Count;
            if (_cardButtons[i] != null) _cardButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            PlanetData data = _eggCatalog.planets[_eggResults[index]];
            if (_cardNames[i] != null) _cardNames[i].text = data.name;
            bool selected = data == _placementPlanetData;
            if (_cardImages[i] != null) {
                _cardImages[i].color = selected ? CardSelectedBg : CardBg;
            }
            if (_cardPlates[i] != null) {
                _cardPlates[i].color = selected ? CardSelectedBg : RowBg;
            }
            Texture2D texture = SandboxPlanetCardRenderer.RenderBox(
                data.textureFront, data.textureSides, data.textureTopBackBottom,
                data.boxXKm, data.boxYKm, data.boxZKm);
            if (_cardTextures[i] != null) {
                _cardTextures[i].texture = texture;
                _cardTextures[i].gameObject.SetActive(texture != null);
                _cardTextures[i].color = Color.white;
            }
            if (_cardDots[i] != null) {
                _cardDots[i].gameObject.SetActive(texture == null);
                _cardDots[i].color = Color.white;
            }
        }

        if (_pageText != null) _pageText.text = $"页码 {_page + 1}/{pageCount}";
        if (_nextPageButton != null) _nextPageButton.interactable = pageCount > 1;
        if (_lastPageButton != null) _lastPageButton.interactable = pageCount > 1;
    }
    private void UpdateCategoryVisuals() {
        SetCategoryTint(_starCategoryButton, _category == CreateCategory.Star
            ? new Color(0.75f, 0.95f, 1f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 1f));
        SetCategoryTint(_planetCategoryButton, _category == CreateCategory.Planet
            ? new Color(0.75f, 0.95f, 1f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 1f));
        SetCategoryTint(_dwarfCategoryButton, _category == CreateCategory.DwarfPlanet
            ? new Color(0.75f, 0.95f, 1f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 1f));
        SetCategoryTint(_moonCategoryButton, _category == CreateCategory.Moon
            ? new Color(0.75f, 0.95f, 1f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 1f));
        SetCategoryTint(_eggCategoryButton, _category == CreateCategory.EasterEgg
            ? new Color(0.75f, 0.95f, 1f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 1f));
    }

    private static void SetCategoryTint(Button button, Color color) {
        if (button == null) return;
        // 分类按钮的可见背景可能在子节点上，递归设置才能保证两个按钮同步切换。
        foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true)) {
            if (graphic is TMP_Text) continue;
            graphic.color = color;
        }
    }

    private void SelectCatalogCard(int slot) {
        int index = _page * PageSize + slot;
        if (_category == CreateCategory.Star) {
            if (_catalog == null || _catalog.stars == null) return;
            if (index < 0 || index >= _starResults.Count) return;
            int dataIndex = _starResults[index];
            if (dataIndex < 0 || dataIndex >= _catalog.stars.Length) return;
            SelectCatalog(_catalog.stars[dataIndex]);
        } else if (_category == CreateCategory.Planet) {
            if (_planetCatalog == null || _planetCatalog.planets == null) return;
            if (index < 0 || index >= _planetResults.Count) return;
            int dataIndex = _planetResults[index];
            if (dataIndex < 0 || dataIndex >= _planetCatalog.planets.Length) return;
            SelectPlanetCatalog(_planetCatalog.planets[dataIndex]);
        } else if (_category == CreateCategory.DwarfPlanet) {
            if (_dwarfCatalog == null || _dwarfCatalog.planets == null) return;
            if (index < 0 || index >= _dwarfResults.Count) return;
            int dataIndex = _dwarfResults[index];
            if (dataIndex < 0 || dataIndex >= _dwarfCatalog.planets.Length) return;
            SelectPlanetCatalog(_dwarfCatalog.planets[dataIndex]);
        } else if (_category == CreateCategory.Moon) {
            if (_moonCatalog == null || _moonCatalog.planets == null) return;
            if (index < 0 || index >= _moonResults.Count) return;
            int dataIndex = _moonResults[index];
            if (dataIndex < 0 || dataIndex >= _moonCatalog.planets.Length) return;
            SelectPlanetCatalog(_moonCatalog.planets[dataIndex]);
        } else {
            if (_eggCatalog == null || _eggCatalog.planets == null) return;
            if (index < 0 || index >= _eggResults.Count) return;
            int dataIndex = _eggResults[index];
            if (dataIndex < 0 || dataIndex >= _eggCatalog.planets.Length) return;
            SelectPlanetCatalog(_eggCatalog.planets[dataIndex]);
        }
    }

    private void SelectCatalog(StarData data) {
        if (data == null) return;

        if (!SandboxStarModel.TryCompute(data.mass, data.metallicity, data.age,
                data.rotationPeriodDays,
                out SandboxStarSnapshot snapshot, out string error)) {
            FloatingText.Show(error);
            return;
        }

        _placementEntry = data;
        _placementSnapshot = snapshot;
        _placementPlanetData = null;
        _placementPlanetSnapshot = null;
        DeselectStar();
        ResetPlacementMotion();
        UpdateMotionButtons();
        if (_preview == null) return;

        float diameter = (float)snapshot.VisualDiameter;
        float renderDiameter = (float)(diameter * SandboxWorldScale.Scale);
        _placementPivot.transform.position = _templatePosition;
        _placementPivot.transform.localScale = Vector3.one * renderDiameter;

        if (_orbitCamera != null) {
            if (_stars.Count == 0 && _planets.Count == 0) {
                // 空场景放第一个天体：无条件把相机拉到原点外 5 倍直径。
                _orbitCamera.Focus(_placementPivot.transform, diameter * 5f);
            } else {
                // 非空场景保持原逻辑：只有太近时才后退。
                _orbitCamera.EnsurePlaneHeight(
                    _placementPlane, diameter * 5f, _placementPivot.transform);
            }
        }

        _preview.transform.localScale = Vector3.one * renderDiameter;
        _preview.SetActive(true);
        UpdatePlacementPreview();
    }

    private void SelectPlanetCatalog(PlanetData data) {
        if (data == null) return;
        if (!SandboxPlanetModel.TryCreate(data, out SandboxPlanetSnapshot snapshot,
                out string error)) {
            FloatingText.Show(error);
            return;
        }

        _placementEntry = null;
        _placementSnapshot = null;
        _placementPlanetData = data;
        _placementPlanetSnapshot = snapshot;
        DeselectStar();
        ResetPlacementMotion();
        UpdateMotionButtons();
        if (_preview == null) return;

        float diameter = (float)snapshot.visualDiameter;
        float renderDiameter = (float)(diameter * SandboxWorldScale.Scale);
        _placementPivot.transform.position = _templatePosition;
        _placementPivot.transform.localScale = Vector3.one * renderDiameter;
        if (_orbitCamera != null) {
            if (_stars.Count == 0 && _planets.Count == 0) {
                // 空场景放第一个天体：无条件把相机拉到原点外 5 倍直径。
                _orbitCamera.Focus(_placementPivot.transform, diameter * 5f);
            } else {
                _orbitCamera.EnsurePlaneHeight(
                    _placementPlane, diameter * 5f, _placementPivot.transform);
            }
        }

        _preview.transform.localScale = Vector3.one * renderDiameter;
        _preview.SetActive(true);
        UpdatePlacementPreview();
    }

    private void CancelPlacement() {
        _placementEntry = null;
        _placementSnapshot = null;
        _placementPlanetData = null;
        _placementPlanetSnapshot = null;
        if (_preview != null) _preview.SetActive(false);
        UpdateMotionButtons();
    }

    // ~~~ 鼠标 / 放置 / 选择 ~~~ //

    private void UpdatePlacementPreview() {
        bool hasPlacement = _placementEntry != null || _placementPlanetData != null;
        if (hasPlacement) UpdatePlacementVisualScale();
        if (!hasPlacement || _preview == null) {
            if (_preview != null) _preview.SetActive(false);
            HideFollowGuide();
            return;
        }

        // 发射模式从相机出发，不需要网格平面上的预览球。
        if (_placementMotion == PlacementMotion.Launch) {
            _preview.SetActive(false);
            HideFollowGuide();
            return;
        }

        if (IsPointerOverUI() || !TryGetPlacementPoint(out Vector3 point)) {
            _preview.SetActive(false);
            HideFollowGuide();
            return;
        }

        _preview.transform.position = point;
        _preview.SetActive(true);
        UpdateFollowGuide(point);
    }

    private void UpdatePlacementVisualScale() {
        double realDiameter = 0.0;
        if (_placementSnapshot != null) realDiameter = _placementSnapshot.VisualDiameter;
        else if (_placementPlanetSnapshot != null) realDiameter = _placementPlanetSnapshot.visualDiameter;
        if (realDiameter <= 0.0) return;

        float renderDiameter = (float)(realDiameter * SandboxWorldScale.Scale);
        if (_placementPivot != null) {
            _placementPivot.transform.localScale = Vector3.one * renderDiameter;
        }
        if (_preview != null) {
            _preview.transform.localScale = Vector3.one * renderDiameter;
        }
    }

    /// <summary>
    /// 创建位置：发射模式下从相机出发、沿视线方向；其它模式仍然落在网格平面上。
    /// </summary>
    private Vector3 PickSpawnPoint() {
        if (_placementMotion != PlacementMotion.Launch || _camera == null) {
            return TryGetPlacementPoint(out Vector3 p) ? p : Vector3.zero;
        }

        // 出生点：相机位置沿视线方向 + 1 倍被选中天体直径。
        double diameter = 0.0;
        if (_placementSnapshot != null) diameter = _placementSnapshot.VisualDiameter;
        else if (_placementPlanetSnapshot != null) diameter = _placementPlanetSnapshot.visualDiameter;
        if (diameter <= 0.0 && _preview != null &&
            SandboxWorldScale.Scale > 1e-30) {
            diameter = _preview.transform.localScale.x / SandboxWorldScale.Scale;
        }

        float distance = diameter > 0.0001
            ? (float)(diameter * SandboxWorldScale.Scale)
            : 0.001f;
        Vector3 direction = MouseRay().direction;
        return _camera.transform.position + direction * distance;
    }

    private bool TryGetPlacementPoint(out Vector3 point) {
        point = default;
        if (_camera == null) return false;

        Ray ray = MouseRay();

        // 参照距离：相机到当前跟随目标（沙盘里渲染距离固定 30）。
        float reference = 50f;
        if (_orbitCamera != null && _orbitCamera.target != null) {
            reference = Mathf.Max(
                Vector3.Distance(_camera.transform.position, _orbitCamera.target.position), 0.01f);
        }

        // 拉远后视线几乎平行于放置平面，射线会和平面交在非常远的地方
        // （单个场景甚至到 1e18 单位），放出来的天体会跑到屏幕外。
        // 这里限制在“约 20 倍相机距离”以内；超出的就退化成沿视线、
        // 和当前观察距离差不多的位置，保证天体落在当前视野附近。
        if (_placementPlane.Raycast(ray, out float enter) && enter > 0f &&
            enter <= Mathf.Max(reference * 20f, 1f)) {
            point = ray.GetPoint(enter);
            return true;
        }

        point = ray.origin + ray.direction * reference;
        return true;
    }

    private void HandlePointer() {
        if (Input.GetMouseButtonDown(0)) {
            _mouseDownPosition = Input.mousePosition;
            _mouseDownOverUI = IsPointerOverUI();
        }

        if (!Input.GetMouseButtonUp(0)) return;

        bool startedOverUI = _mouseDownOverUI;
        _mouseDownOverUI = false;
        if (startedOverUI || IsPointerOverUI()) return;
        if (Vector2.Distance(_mouseDownPosition, Input.mousePosition) > ClickDragThreshold) return;

        if (_placementEntry != null) {
            CreateStar(_placementEntry, _placementSnapshot, PickSpawnPoint());
        } else if (_placementPlanetData != null) {
            CreatePlanet(_placementPlanetData, _placementPlanetSnapshot, PickSpawnPoint());
        } else {
            SelectBodyAtPointer();
        }
    }

    private void HandlePauseKey() {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        // 正在输入名称/参数时，空格属于文本，不触发暂停。
        if (EventSystem.current != null) {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.GetComponentInParent<TMP_InputField>() != null) {
                return;
            }
        }

        SandboxTimeManager.Instance?.TogglePause();
    }

    private void HandleTrailKey() {
        // T 切换轨迹显示，O 切换完整轨道线；输入框聚焦时不响应。
        if (EventSystem.current != null) {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.GetComponentInParent<TMP_InputField>() != null) {
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.T)) SandboxTrail.ToggleVisible();
        if (Input.GetKeyDown(KeyCode.O)) SandboxTrail.ToggleOrbitVisible();
        if (Input.GetKeyDown(KeyCode.R)) ToggleStarRenderMode();
        if (Input.GetKeyDown(KeyCode.L)) ToggleLabels();
        if (Input.GetKeyDown(KeyCode.F3)) ToggleStatsOverlay();
    }

    /// <summary>F3：显示 / 隐藏左上角的 FPS / CPU / GPU 状态条。</summary>
    private void ToggleStatsOverlay() {
        _statsVisible = !_statsVisible;
    }

    private void TickStatsOverlay() {
        float dt = Time.unscaledDeltaTime;
        if (dt > 0f) _statsFps = Mathf.Lerp(_statsFps, 1f / dt, 0.15f);

        _statsCpuTimer -= dt;
        if (_statsCpuTimer <= 0f) {
            _statsCpuTimer = 0.5f;
            _statsCpu = SampleCpuPercent();
            _statsGpu = SampleGpuPercent();
        }

        if (_statsVisible) _statsText = BuildStatsText();
    }

    private string BuildStatsText() {
        string cpu = _statsCpu >= 0f ? _statsCpu.ToString("0") + "%" : "n/a";
        string gpu = _statsGpu >= 0f ? _statsGpu.ToString("0") + "%" : "n/a";
        string first = "FPS " + _statsFps.ToString("0") + "   CPU " + cpu + "   GPU " + gpu;

        if (string.IsNullOrEmpty(_statsHardwareLine)) _statsHardwareLine = BuildHardwareLine();
        return first + "\n" + _statsHardwareLine;
    }

    private string _statsHardwareLine;

    /// <summary>本机 CPU / GPU 型号，分两行显示。</summary>
    private static string BuildHardwareLine() {
        string cpuName = SystemInfo.processorType ?? "";
        cpuName = cpuName.Replace("(R)", string.Empty)
                         .Replace("(TM)", string.Empty)
                         .Replace("  ", " ")
                         .Trim();
        if (cpuName.Length > 46) cpuName = cpuName.Substring(0, 46);

        string gpuName = SystemInfo.graphicsDeviceName ?? "";
        if (gpuName.Length > 40) gpuName = gpuName.Substring(0, 40);

        return "CPU: " + cpuName + " (" + SystemInfo.processorCount + ")\nGPU: " + gpuName;
    }

    // ~~~ 本机 CPU / GPU 采样 ~~~

    private ulong _cpuIdleTicks;
    private ulong _cpuKernelTicks;
    private ulong _cpuUserTicks;
    private bool _cpuPrimed;
    private DateTime _procCpuWall = DateTime.MinValue;
    private TimeSpan _procCpuTime;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [StructLayout(LayoutKind.Sequential)]
    private struct WinFileTime {
        public uint Low;
        public uint High;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out WinFileTime idle, out WinFileTime kernel,
        out WinFileTime user);

    private static ulong FileTimeValue(WinFileTime ft) => ((ulong)ft.High << 32) | ft.Low;
#endif

    /// <summary>整机 CPU 占用率（Windows 用 GetSystemTimes，其它平台退回本进程占用）。</summary>
    private float SampleCpuPercent() {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (GetSystemTimes(out WinFileTime idle, out WinFileTime kernel, out WinFileTime user)) {
            ulong idleTicks = FileTimeValue(idle);
            ulong kernelTicks = FileTimeValue(kernel);
            ulong userTicks = FileTimeValue(user);

            if (!_cpuPrimed) {
                _cpuPrimed = true;
                _cpuIdleTicks = idleTicks;
                _cpuKernelTicks = kernelTicks;
                _cpuUserTicks = userTicks;
                return -1f;
            }

            ulong idleDelta = idleTicks - _cpuIdleTicks;
            ulong kernelDelta = kernelTicks - _cpuKernelTicks;
            ulong userDelta = userTicks - _cpuUserTicks;
            _cpuIdleTicks = idleTicks;
            _cpuKernelTicks = kernelTicks;
            _cpuUserTicks = userTicks;

            // kernel 时间里包含 idle，所以总时间 = kernel + user。
            ulong total = kernelDelta + userDelta;
            if (total == 0UL) return -1f;
            double busy = (double)(total - System.Math.Min(idleDelta, total));
            return Mathf.Clamp((float)(busy / total * 100.0), 0f, 100f);
        }
#endif
        return SampleProcessCpuPercent();
    }

    private float SampleProcessCpuPercent() {
        try {
            TimeSpan now = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            DateTime wall = DateTime.UtcNow;
            if (_procCpuWall == DateTime.MinValue) {
                _procCpuWall = wall;
                _procCpuTime = now;
                return -1f;
            }
            double wallSeconds = (wall - _procCpuWall).TotalSeconds;
            double cpuSeconds = (now - _procCpuTime).TotalSeconds;
            _procCpuWall = wall;
            _procCpuTime = now;
            if (wallSeconds <= 0.0) return -1f;
            int cores = Mathf.Max(1, SystemInfo.processorCount);
            return Mathf.Clamp((float)(cpuSeconds / (wallSeconds * cores) * 100.0), 0f, 100f);
        } catch {
            return -1f;
        }
    }

    /// <summary>GPU 占用估算：GPU 帧时间 / 总帧时间（需要开启 Frame Timing Stats）。</summary>
    private float SampleGpuPercent() {
        uint count = FrameTimingManager.GetLatestTimings(1, _frameTimings);
        if (count == 0) return -1f;
        double gpuMs = _frameTimings[0].gpuFrameTime;
        if (gpuMs <= 0.0) return -1f;
        double frameMs = System.Math.Max(gpuMs, 1000.0 / System.Math.Max(_statsFps, 1f));
        return Mathf.Clamp((float)(gpuMs / frameMs * 100.0), 0f, 100f);
    }

    private void OnGUI() {
        if (!_statsVisible) return;

        if (_statsStyle == null) {
            _statsStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false
            };
            _statsStyle.normal.textColor = Color.white;
        }
        if (_statsBackground == null) _statsBackground = Texture2D.whiteTexture;

        string text = string.IsNullOrEmpty(_statsText) ? BuildStatsText() : _statsText;
        Vector2 size = _statsStyle.CalcSize(new GUIContent(text));

        // 右上角：距右边 8 像素、距顶 8 像素。
        float width = size.x + 18f;
        float height = size.y + 10f;
        Rect rect = new(Screen.width - width - 8f, 8f, width, height);

        GUI.depth = -1000;
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(rect, _statsBackground);
        GUI.color = previous;
        GUI.Label(new Rect(rect.x + 9f, rect.y + 4f, size.x, size.y), text, _statsStyle);
    }

    /// <summary>L 键：显示 / 隐藏所有天体名字标签。</summary>
    private void ToggleLabels() {
        SetLabelsVisible(!_labelsVisible);
    }

    public bool LabelsVisible => _labelsVisible;

    public void SetLabelsVisible(bool visible) {
        _labelsVisible = visible;
        foreach (BodyLabel label in _bodyLabels) {
            if (label.text != null) label.text.gameObject.SetActive(_labelsVisible);
        }
    }

    /// <summary>R 键：在默认渲染和黑体光球之间切换所有恒星。</summary>
    private void ToggleStarRenderMode() {
        SetStarRenderMode(_starRenderMode == 1 ? 0 : 1);
        FloatingText.Show(_starRenderMode == 1 ? "恒星渲染：黑体光球" : "恒星渲染：默认");
    }

    public int StarRenderMode => _starRenderMode;

    public void SetStarRenderMode(int mode) {
        _starRenderMode = mode == 1 ? 1 : 0;
        foreach (SandboxStar star in _stars) {
            if (star != null) star.SetStarRenderMode(_starRenderMode);
        }
        PlayerPrefs.SetInt(StarRenderModeKey, _starRenderMode);
        PlayerPrefs.Save();
    }

    private void HandleCancel() {
        if (Input.GetMouseButtonDown(1)) {
            if (_placementEntry != null || _placementPlanetData != null) CancelPlacement();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 设置面板优先：ESC 先关它。
        if (_settingsPanel != null && _settingsPanel.IsOpen) {
            _settingsPanel.Close();
            return;
        }

        // 有面板时先关闭最近打开的面板；全部关闭后再用 ESC 打开 HomePage。
        if (CloseTopPanel()) return;
        if (_homePage != null) OpenHomePanel();
    }

    public void TrackPanel(GameObject panel, Action close) {
        if (panel == null) return;
        _panelStack.RemoveAll(e => e == null || e.root == null || e.root == panel);
        _panelStack.Add(new PanelEntry { root = panel, close = close });
    }

    private bool CloseTopPanel() {
        for (int i = _panelStack.Count - 1; i >= 0; i--) {
            PanelEntry entry = _panelStack[i];
            if (entry == null || entry.root == null || !entry.root.activeInHierarchy) {
                _panelStack.RemoveAt(i);
                continue;
            }
            _panelStack.RemoveAt(i);
            entry.close?.Invoke();
            return true;
        }
        return false;
    }

    private void CreateStar(StarData data, SandboxStarSnapshot snapshot, Vector3 position) {
        Material material = _templateMaterial != null
            ? new Material(_templateMaterial)
            : CreateFallbackStarMaterial();

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = data.name;
        go.transform.position = position;

        SandboxStar star = go.AddComponent<SandboxStar>();
        star.Initialize(data.name, data.mass, data.metallicity, data.age, snapshot, material,
            data.rotationPeriodDays);
        star.SetStarRenderMode(_starRenderMode);
        SandboxFloatingOrigin.FromRender(position,
            out double logicalX, out double logicalY, out double logicalZ);
        star.SetLogicalPosition(logicalX, logicalY, logicalZ);
        _stars.Add(star);
        AddStarLabel(star);
        RefreshFallbackLight();
        SandboxPlanet.UploadStarLights(_stars);

        SandboxPhysicsBody body = SandboxPhysicsSystem.RegisterStar(star);
        ApplyPlacementMotion(body);
    }

    private static Material CreateFallbackStarMaterial() {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new(shader);
        material.color = Color.white;
        return material;
    }

    private void CreatePlanet(PlanetData data, SandboxPlanetSnapshot snapshot, Vector3 position) {
        Shader shader = Shader.Find("UPB/SandboxPlanet");
        if (shader == null) {
            Debug.LogWarning("[Sandbox] 找不到 UPB/SandboxPlanet，退回 URP/Lit 材质");
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new(shader);
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_AmbientColor", new Color(0.008f, 0.010f, 0.016f, 1f));
        material.SetColor("_StarColor", Color.white);
        material.SetFloat("_StarFade", 0f);
        material.SetFloat("_EmissionStrength", 0f);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = data.name;
        go.transform.position = position;

        SandboxPlanet planet = go.AddComponent<SandboxPlanet>();
        planet.Initialize(data, snapshot, material);
        SandboxFloatingOrigin.FromRender(position,
            out double logicalX, out double logicalY, out double logicalZ);
        planet.SetLogicalPosition(logicalX, logicalY, logicalZ);
        _planets.Add(planet);
        AddPlanetLabel(planet);
        if (_stars.Count == 0) RefreshFallbackLight();

        SandboxPhysicsBody body = SandboxPhysicsSystem.RegisterPlanet(planet);
        ApplyPlacementMotion(body);
    }

    private const string ShootSpeedKey = "sandbox.launch.speed";
    private const string ShootSpeedUnitKey = "sandbox.launch.speedUnit";

    /// <summary>把发射速度输入框的数值和单位存进 PlayerPrefs，下次打开沿用。</summary>
    private void SaveShootSpeed() {
        if (_shootSpeedInput != null && TryParse(_shootSpeedInput.text, out double value) && value > 0.0) {
            PlayerPrefs.SetFloat(ShootSpeedKey, (float)value);
        }
        if (_shootSpeedUnit != null && !string.IsNullOrEmpty(_shootSpeedUnit.GetUnit())) {
            PlayerPrefs.SetString(ShootSpeedUnitKey, _shootSpeedUnit.GetUnit());
        }
        PlayerPrefs.Save();
    }
    /// <summary>发射速度（m/s）：输入框数值 × Unit_Switch 选中的速度单位。</summary>
    private double LaunchSpeedMps() {
        double value = 10.0;
        if (_shootSpeedInput != null && TryParse(_shootSpeedInput.text, out double parsed) && parsed > 0.0) {
            value = parsed;
        }

        string unit = _shootSpeedUnit != null ? _shootSpeedUnit.GetUnit() : "km/s";
        SandboxUnitDefinition def = SandboxUnits.Definition("speed", unit);
        return def != null ? SandboxUnits.ToBase(value, def) : value * 1000.0;
    }

    private void ApplyPlacementMotion(SandboxPhysicsBody body) {
        if (body == null) return;

        if (_placementMotion == PlacementMotion.Static) {
            body.velocityMps = SandboxDVec3.Zero;
            body.isOrbiting = false;
            body.orbitTarget = null;
            body.orbitRadiusM = 0.0;
            return;
        }

        // 发射模式：沿鼠标位置指向的射线方向给一个初速度。
        if (_placementMotion == PlacementMotion.Launch) {
            double launchSpeed = LaunchSpeedMps();
            Vector3 direction = MouseRay().direction;
            body.velocityMps = new SandboxDVec3(direction.x, direction.y, direction.z) * launchSpeed;
            body.isOrbiting = false;
            body.orbitTarget = null;
            body.orbitRadiusM = 0.0;
            return;
        }

        SandboxPhysicsBody target = FindStrongestOrbitTarget(body);
        if (target == null) {
            body.velocityMps = SandboxDVec3.Zero;
            body.isOrbiting = false;
            body.orbitTarget = null;
            body.orbitRadiusM = 0.0;
            return;
        }

        SandboxDVec3 radial = body.positionM - target.positionM;
        double distance = radial.Magnitude;
        if (distance <= target.radiusM + body.radiusM + 1e-6) {
            body.velocityMps = SandboxDVec3.Zero;
            body.isOrbiting = false;
            body.orbitTarget = null;
            body.orbitRadiusM = 0.0;
            return;
        }

        radial = radial / distance;
        SandboxDVec3 reference = Math.Abs(radial.y) < 0.9
            ? new SandboxDVec3(0, 1, 0)
            : new SandboxDVec3(0, 0, 1);
        // 环绕模式的公转方向（之前是反的，这里把叉乘顺序换过来）。
        SandboxDVec3 tangent = Cross(radial, reference);
        double tangentLength = tangent.Magnitude;
        if (tangentLength < 1e-9) {
            body.velocityMps = SandboxDVec3.Zero;
            return;
        }
        tangent = tangent / tangentLength;

        // 圆轨道速度，垂直于目标天体的径向方向。
        double speed = Math.Sqrt(SandboxPhysicsSystem.GravitationalConstant *
                                 (target.massKg + body.massKg) / distance);
        body.velocityMps = target.velocityMps + tangent * speed;
        body.isOrbiting = true;
        body.orbitTarget = target;
        body.orbitRadiusM = distance;
    }

    private SandboxPhysicsBody FindStrongestOrbitTarget(SandboxPhysicsBody newBody) {
        if (newBody == null) return null;

        SandboxPhysicsBody best = null;
        double bestAcceleration = -1.0;

        foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
            if (body == null || !body.alive || ReferenceEquals(body, newBody)) continue;
            if (body.massKg <= newBody.massKg + 1e-9) continue;

            SandboxDVec3 delta = body.positionM - newBody.positionM;
            double d2 = delta.SqrMagnitude;
            if (d2 <= 1e-6) continue;

            double acceleration = SandboxPhysicsSystem.GravitationalConstant *
                                  body.massKg / d2;
            if (acceleration > bestAcceleration) {
                bestAcceleration = acceleration;
                best = body;
            }
        }
        return best;
    }

    private static SandboxDVec3 Cross(SandboxDVec3 a, SandboxDVec3 b) =>
        new(a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);

    private Ray MouseRay() {
        Vector3 mouse = Input.mousePosition;
        float x = Screen.width > 0 ? Mathf.Clamp01(mouse.x / Screen.width) : 0.5f;
        float y = Screen.height > 0 ? Mathf.Clamp01(mouse.y / Screen.height) : 0.5f;
        return _camera.ViewportPointToRay(new Vector3(x, y, 0f));
    }

    private void SelectBodyAtPointer() {
        if (_camera == null) return;

        Ray ray = MouseRay();
        if (Physics.Raycast(ray, out RaycastHit hit, 100000f)) {
            SandboxPlanet hitPlanet = hit.collider.GetComponentInParent<SandboxPlanet>();
            if (hitPlanet != null) {
                HandlePlanetClick(hitPlanet);
                return;
            }

            SandboxStar hitStar = hit.collider.GetComponentInParent<SandboxStar>();
            if (hitStar != null) {
                HandleStarClick(hitStar);
                return;
            }
        }

        SandboxStar nearestStar = null;
        SandboxPlanet nearestPlanet = null;
        float nearestDistance = ScreenSelectRadius;

        foreach (SandboxStar star in _stars) {
            if (star == null) continue;
            Vector3 screen = _camera.WorldToScreenPoint(star.transform.position);
            if (screen.z <= 0f) continue;
            float distance = Vector2.Distance(
                new Vector2(screen.x, screen.y),
                new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            if (distance <= nearestDistance) {
                nearestDistance = distance;
                nearestStar = star;
            }
        }

        foreach (SandboxPlanet planet in _planets) {
            if (planet == null) continue;
            Vector3 screen = _camera.WorldToScreenPoint(planet.transform.position);
            if (screen.z <= 0f) continue;
            float distance = Vector2.Distance(
                new Vector2(screen.x, screen.y),
                new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            if (distance <= nearestDistance) {
                nearestDistance = distance;
                nearestPlanet = planet;
                nearestStar = null;
            }
        }

        if (nearestPlanet != null) {
            HandlePlanetClick(nearestPlanet);
        } else if (nearestStar != null) {
            HandleStarClick(nearestStar);
        } else if (_selectedPlanet == null) {
            // 已选中行星时，点击空白处保持选中；只有点击其它天体才切换。
            DeselectStar();
        }
    }

    private void FocusPlanet(SandboxPlanet planet) {
        if (planet == null) return;
        RebaseTo(planet.logicalX, planet.logicalY, planet.logicalZ);
        if (_orbitCamera != null) {
            _orbitCamera.Focus(planet.transform, (float)(planet.VisualDiameter * 8.0));
        }
    }

    private void FocusStar(SandboxStar star) {
        if (star == null) return;
        RebaseTo(star.logicalX, star.logicalY, star.logicalZ);
        if (_orbitCamera != null) {
            _orbitCamera.Focus(star.transform, (float)(star.VisualDiameter * 5.0));
        }
    }

    private void RebaseTo(double x, double y, double z) {
        SandboxFloatingOrigin.Rebase(x, y, z, _stars, _planets);

        // 碰撞碎片不在沙盘的 stars/planets 列表里，也要跟着浮动原点移动。
        const double metersPerUnit = CelestialScale.KmPerUnit * 1000.0;
        foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
            if (body?.fragment == null) continue;
            body.fragment.SetLogicalPosition(
                body.positionM.x / metersPerUnit,
                body.positionM.y / metersPerUnit,
                body.positionM.z / metersPerUnit);
        }

        // 相机聚焦的目标现在在渲染原点附近；模板/放置点也一起归零。
        _templatePosition = Vector3.zero;
        if (_placementPivot != null) _placementPivot.transform.position = Vector3.zero;
        if (_grid != null) _placementPlane = new Plane(_grid.up, _grid.position);
    }

    private void HandlePlanetClick(SandboxPlanet planet) {
        if (planet == null) return;

        bool doubleClick = _lastClickPlanet == planet &&
                           Time.unscaledTime - _lastClickTime <= DoubleClickSeconds;
        SelectPlanet(planet);

        if (doubleClick) {
            _lastClickTime = -999f;
            _lastClickPlanet = null;
            FocusPlanet(planet);
        } else {
            _lastClickTime = Time.unscaledTime;
            _lastClickPlanet = planet;
        }
    }

    private void HandleStarClick(SandboxStar star) {
        if (star == null) return;

        bool doubleClick = _lastClickStar == star &&
                           Time.unscaledTime - _lastClickTime <= DoubleClickSeconds;

        SelectStar(star);

        if (doubleClick) {
            _lastClickTime = -999f;
            _lastClickStar = null;
            FocusStar(star);
        } else {
            _lastClickTime = Time.unscaledTime;
            _lastClickStar = star;
        }
    }

    private void SelectStar(SandboxStar star) {
        if (_selectedStar != star) ResetAutoUnits();
        _selectedStar = star;
        _selectedPlanet = null;
        // WASD 的速度参考由 MaintainFocusedOrigin 按“相机跟随目标”统一设置，
        // 这里不再改，避免单击选中影响移动速度。
        // 点选恒星时把输入框焦点清掉，避免 Backspace 还在编辑名字。
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (_planetParameterPanel != null) _planetParameterPanel.SetActive(false);
        bool panelOpen = _parameterPanel != null && _parameterPanel.activeSelf;

        if (_expandParameterButton != null) {
            _expandParameterButton.gameObject.SetActive(!panelOpen);
            if (!panelOpen) UpdateExpandButtonText();
        }
        if (_titleContent != null) _titleContent.text = star.starName;

        if (panelOpen) {
            RefreshParameterFields();
        }
        RefreshLockButtons();
    }

    private void ApplyAutoPlanetUnits(SandboxPlanetSnapshot snapshot,
            double temperatureK, double atmosphereMassEarth) {
        if (snapshot == null) return;
        ApplyAutoUnit(_planetMassUnit, "mass",
            snapshot.massEarth * SandboxPhysicsSystem.EarthMassKg);
        ApplyAutoUnit(_planetTempUnit, "temperature", temperatureK);
        ApplyAutoUnit(_planetAtmosphereUnit, "mass",
            atmosphereMassEarth * SandboxPlanetModel.EarthAtmosphereKg);
        ApplyAutoUnit(_planetRadiusUnit, "length",
            snapshot.radiusEarth * SandboxPlanetModel.EarthRadiusKm * 1000.0);
        ApplyAutoUnit(_planetDensityUnit, "density", snapshot.densityGcm3 * 1000.0);
    }

    private void SelectPlanet(SandboxPlanet planet) {
        if (_selectedPlanet != planet) ResetAutoUnits();
        _selectedPlanet = planet;
        _selectedStar = null;
        // WASD 的速度参考由 MaintainFocusedOrigin 统一设置，这里不再改。
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (_parameterPanel != null) _parameterPanel.SetActive(false);
        bool panelOpen = _planetParameterPanel != null && _planetParameterPanel.activeSelf;

        if (_expandParameterButton != null) {
            _expandParameterButton.gameObject.SetActive(!panelOpen);
            if (!panelOpen) UpdateExpandButtonText();
        }

        if (panelOpen) {
            RefreshPlanetParameterFields();
        }
        RefreshLockButtons();
    }

    private void DeselectStar() {
        _selectedStar = null;
        _selectedPlanet = null;
        RefreshLockButtons();
        // 速度参考跟随“相机跟随目标”，取消选中不影响 WASD。
        if (_expandParameterButton != null) _expandParameterButton.gameObject.SetActive(false);
        if (_parameterPanel != null) _parameterPanel.SetActive(false);
        if (_planetParameterPanel != null) _planetParameterPanel.SetActive(false);
    }

    private void OpenParameterPanel() {
        // 重新打开面板时，所有单位回到自适应（用户手动改的单位不保留）。
        ResetAutoUnits();
        if (_selectedPlanet != null && _planetParameterPanel != null) {
            _parameterPanel?.SetActive(false);
            _planetParameterPanel.SetActive(true);
            _planetParameterPanel.transform.SetAsLastSibling();
            TrackPanel(_planetParameterPanel, CloseParameterPanel);
            if (_expandParameterButton != null) _expandParameterButton.gameObject.SetActive(false);
            SetPlanetModule(_planetModule);
            RefreshPlanetParameterFields();
            return;
        }

        if (_selectedStar == null || _parameterPanel == null) return;

        _planetParameterPanel?.SetActive(false);
        _parameterPanel.SetActive(true);
        _parameterPanel.transform.SetAsLastSibling();
        TrackPanel(_parameterPanel, CloseParameterPanel);
        if (_expandParameterButton != null) _expandParameterButton.gameObject.SetActive(false);
        SetStarModule(_starModule);
        RefreshParameterFields();
    }

    private void CloseParameterPanel() {
        if (_parameterPanel != null) _parameterPanel.SetActive(false);
        if (_planetParameterPanel != null) _planetParameterPanel.SetActive(false);
        if ((_selectedStar != null || _selectedPlanet != null) &&
            _expandParameterButton != null) {
            _expandParameterButton.gameObject.SetActive(true);
            UpdateExpandButtonText();
        }
    }

    private void ToggleParameterPanel() {
        if (_selectedStar == null && _selectedPlanet == null) return;

        bool open = (_parameterPanel != null && _parameterPanel.activeSelf) ||
                    (_planetParameterPanel != null && _planetParameterPanel.activeSelf);
        if (open) CloseParameterPanel();
        else OpenParameterPanel();
    }

    private void HandleDeleteKey() {
        if (!Input.GetKeyDown(KeyCode.Backspace)) return;
        if (_selectedStar == null && _selectedPlanet == null) return;

        if (EventSystem.current != null) {
            GameObject current = EventSystem.current.currentSelectedGameObject;
            if (current != null && current.GetComponentInParent<TMP_InputField>() != null) return;
        }

        DeleteSelectedBody();
    }

    private void DeleteSelectedBody() {
        if (_selectedPlanet != null) {
            DeleteSelectedPlanet();
            return;
        }

        SandboxStar star = _selectedStar;
        if (star == null) return;

        _selectedStar = null;
        _stars.Remove(star);

        for (int i = _bodyLabels.Count - 1; i >= 0; i--) {
            if (_bodyLabels[i].star != star) continue;
            if (_bodyLabels[i].text != null) Destroy(_bodyLabels[i].text.gameObject);
            _bodyLabels.RemoveAt(i);
        }

        if (_lastClickStar == star) {
            _lastClickStar = null;
            _lastClickTime = -999f;
        }

        if (_orbitCamera != null && _orbitCamera.target == star.transform &&
            _placementPivot != null) {
            _orbitCamera.target = _placementPivot.transform;
        }

        DeselectStar();
        Destroy(star.gameObject);
    }

    private void DeleteSelectedPlanet() {
        SandboxPlanet planet = _selectedPlanet;
        if (planet == null) return;

        _planets.Remove(planet);
        for (int i = _bodyLabels.Count - 1; i >= 0; i--) {
            if (_bodyLabels[i].planet != planet) continue;
            if (_bodyLabels[i].text != null) Destroy(_bodyLabels[i].text.gameObject);
            _bodyLabels.RemoveAt(i);
        }

        if (_lastClickPlanet == planet) {
            _lastClickPlanet = null;
            _lastClickTime = -999f;
        }
        if (_orbitCamera != null && _orbitCamera.target == planet.transform &&
            _placementPivot != null) {
            _orbitCamera.target = _placementPivot.transform;
        }

        DeselectStar();
        Destroy(planet.gameObject);
    }

    private void UpdateExpandButtonText() {
        if (_expandParameterButton == null) return;
        bool open = (_parameterPanel != null && _parameterPanel.activeSelf) ||
                    (_planetParameterPanel != null && _planetParameterPanel.activeSelf);
        TMP_Text label = _expandParameterButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = open ? ">" : "<";
    }

    private bool IsPointerOverUI() {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // ~~~ 参数面板 ~~~ //

    private void RefreshParameterFields() {
        if (_selectedPlanet != null) {
            RefreshPlanetParameterFields();
            return;
        }
        if (_selectedStar == null || _selectedStar.snapshot == null) return;

        SandboxStar star = _selectedStar;
        SandboxStarSnapshot snapshot = star.snapshot;
        ApplyAutoStarUnits(snapshot);

        if (_nameInput != null) _nameInput.text = star.starName;
        if (_massInput != null) _massInput.text = FormatMass(star.mass, MassUnit);
        if (_ageInput != null) _ageInput.text = FormatAge(star.ageMyr, AgeUnit);
        if (_metalInput != null) _metalInput.text = FormatValue(star.metallicity);

        if (_titleContent != null) _titleContent.text = star.starName;
        if (_massLabel != null) _massLabel.text = "质量";
        if (_ageLabel != null) _ageLabel.text = "年龄";
        if (_metalLabel != null) _metalLabel.text = "金属度";

        if (_radiusValue != null) _radiusValue.text = FormatRadius(snapshot.radiusRsun, RadiusUnit);
        if (_temperatureValue != null) _temperatureValue.text = FormatTemperature(snapshot.temperatureK, TemperatureUnit);
        if (_densityValue != null) _densityValue.text = FormatDensity(snapshot.densityGcm3, DensityUnit);
        if (_luminosityValue != null) _luminosityValue.text = FormatLuminosity(snapshot.luminosityLsun, LuminosityUnit);

        RefreshStarDerivedFields(star);
        RefreshLockButtonState(_starLockPositionButton, SandboxPhysicsSystem.GetBody(star));
        UpdateExpandButtonText();
    }

    /// <summary>天体受到的万有引力合力大小（N）：Σ G·m·m_i / r_i²，按矢量求和后取模。</summary>
    private static double GravityForceN(SandboxPhysicsBody body) {
        if (body == null || !body.alive) return 0.0;

        SandboxDVec3 total = SandboxDVec3.Zero;
        foreach (SandboxPhysicsBody other in SandboxPhysicsSystem.Bodies) {
            if (other == null || !other.alive || other == body) continue;
            SandboxDVec3 delta = other.positionM - body.positionM;
            double r2 = delta.SqrMagnitude;
            if (r2 <= 1e-6) continue;
            double r = Math.Sqrt(r2);
            double factor = SandboxPhysicsSystem.GravitationalConstant *
                            body.massKg * other.massKg / r2;
            total += delta * (factor / r);
        }
        return total.Magnitude;
    }

    private void ApplyAutoStarUnits(SandboxStarSnapshot snapshot) {
        if (snapshot == null) return;
        ApplyAutoUnit(_massUnit, "mass", snapshot.mass * SandboxPhysicsSystem.SunMassKg);
        ApplyAutoUnit(_radiusUnit, "length",
            snapshot.radiusRsun * SandboxStarModel.SunRadiusKm * 1000.0);
        ApplyAutoUnit(_densityUnit, "density", snapshot.densityGcm3 * 1000.0);
        ApplyAutoUnit(_temperatureUnit, "temperature", snapshot.temperatureK);
        ApplyAutoUnit(_luminosityUnit, "power",
            snapshot.luminosityLsun * SandboxStarModel.SunLuminosityW);
        ApplyAutoUnit(_ageUnit, "time", snapshot.ageMyr * 1e6 * 31556952.0);
    }

    private void RefreshStarDerivedFields(SandboxStar star) {
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(star);
        if (body == null) return;

        ApplyAutoStarUnits(star.snapshot);

        if (_mechForce != null) {
            double force = GravityForceN(body);
            _mechForce.text = FormatUnitValue(force, "force",
                _mechanicsPanel, "AutoParameter_GeneralForceReceived",
                FormatValue(force));
        }

        SetInputIfNotFocused(_starOrbitAngleInput,
            FormatValue(body.orbitInclinationDeg));
        double speed = body.velocityMps.Magnitude;
        if (_generalVelocityValue != null) {
            _generalVelocityValue.text = FormatUnitValue(speed, "speed",
                _generalPanel, "AutoParameter_Velocity", FormatValue(speed / 1000.0));
        }

        if (_generalDistanceValue != null) {
            double distanceM = CameraDistanceMeters(star.transform.position);
            _generalDistanceValue.text = FormatUnitValue(distanceM, "length",
                _generalPanel, "AutoParameter_Velocity (1)",
                FormatValue(distanceM / 1000.0));
        }

        // 平动
        if (_mechVelocityValue != null) {
            _mechVelocityValue.text = FormatUnitValue(speed, "speed",
                _mechanicsPanel, "AutoParameter_Velocity", FormatValue(speed / 1000.0));
        }
        SetInputIfNotFocused(_mechXMotionInput, FormatUnitValue(body.velocityMps.x,
            "speed", _mechanicsPanel, "IF_Type02_XMotion",
            FormatValue(body.velocityMps.x / 1000.0)));
        SetInputIfNotFocused(_mechYMotionInput, FormatUnitValue(body.velocityMps.y,
            "speed", _mechanicsPanel, "IF_Type02_YMotion",
            FormatValue(body.velocityMps.y / 1000.0)));
        SetInputIfNotFocused(_mechZMotionInput, FormatUnitValue(body.velocityMps.z,
            "speed", _mechanicsPanel, "IF_Type02_ZMotion",
            FormatValue(body.velocityMps.z / 1000.0)));

        if (_mechKineticValue != null) {
            _mechKineticValue.text = FormatUnitValue(body.KineticEnergyJ, "energy",
                _mechanicsPanel, "AutoParameter_Knetic", FormatValue(body.KineticEnergyJ));
        }
        if (_mechMomentumValue != null) {
            _mechMomentumValue.text = FormatValue(body.massKg * speed);
        }

        // 转动
        SetInputIfNotFocused(_mechSpinPeriodInput, FormatUnitValue(
            star.rotationPeriodDays * 86400.0, "time",
            _mechanicsPanel, "IF_Type02_SpinPeriod", FormatValue(star.rotationPeriodDays)));
        double spinPeriodSeconds = ReadSpinPeriodSeconds();
        if (spinPeriodSeconds > 0.0 && body.radiusM > 0.0) {
            double omega = 2.0 * Math.PI / spinPeriodSeconds;
            double inertia = 0.4 * body.massKg * body.radiusM * body.radiusM;
            if (_mechAngularKineticValue != null) {
                _mechAngularKineticValue.text = FormatUnitValue(
                    0.5 * inertia * omega * omega, "energy",
                    _mechanicsPanel, "AutoParameter_Angular_Knetic",
                    FormatValue(0.5 * inertia * omega * omega));
            }
            if (_mechEquatorVelocityValue != null) {
                _mechEquatorVelocityValue.text = FormatUnitValue(
                    body.radiusM * omega, "speed",
                    _mechanicsPanel, "AutoParameter_Equator_Velocity",
                    FormatValue(body.radiusM * omega / 1000.0));
            }
            if (_mechAngularMomentumValue != null) {
                // 自转角动量 L = Iω
                _mechAngularMomentumValue.text = FormatValue(inertia * omega);
            }
        } else {
            if (_mechAngularKineticValue != null) _mechAngularKineticValue.text = "—";
            if (_mechEquatorVelocityValue != null) _mechEquatorVelocityValue.text = "—";
            if (_mechAngularMomentumValue != null) _mechAngularMomentumValue.text = "—";
        }

        FillStarEnergyFields(star, body);

        // 轨道
        SandboxPhysicsBody target = body.orbitTarget;
        if (target == null) {
            if (_mechCentralObject != null) _mechCentralObject.text = "无";
            if (_mechEccentricityValue != null) _mechEccentricityValue.text = "—";
            if (_mechSemiMajorAxisValue != null) _mechSemiMajorAxisValue.text = "—";
            if (_mechOrbitalPeriodValue != null) _mechOrbitalPeriodValue.text = "—";
            return;
        }

        if (_mechCentralObject != null) _mechCentralObject.text = target.name;
        SandboxDVec3 relPos = body.positionM - target.positionM;
        SandboxDVec3 relVel = body.velocityMps - target.velocityMps;
        double r = relPos.Magnitude;
        double mu = SandboxPhysicsSystem.GravitationalConstant *
                    (target.massKg + body.massKg);
        if (r <= 0.0 || mu <= 0.0) return;

        SandboxDVec3 hVec = CrossD(relPos, relVel);
        double h = hVec.Magnitude;
        double v2 = relVel.SqrMagnitude;
        double energy = 0.5 * v2 - mu / r;
        double eccentricity = 0.0;
        double semiMajor = 0.0;
        double periodYears = 0.0;

        if (energy < -1e-30) {
            semiMajor = -mu / (2.0 * energy);
            double e2 = 1.0 + 2.0 * energy * h * h / (mu * mu);
            eccentricity = Math.Sqrt(Math.Max(0.0, e2));
            double periodSeconds = 2.0 * Math.PI *
                Math.Sqrt(semiMajor * semiMajor * semiMajor / mu);
            periodYears = periodSeconds / (86400.0 * 365.2425);
        }

        if (_mechEccentricityValue != null) {
            _mechEccentricityValue.text = energy < 0.0 ? FormatValue(eccentricity) : "双曲";
        }
        if (_mechSemiMajorAxisValue != null) {
            _mechSemiMajorAxisValue.text = energy < 0.0
                ? FormatUnitValue(semiMajor, "length",
                    _mechanicsPanel, "AutoParameter_Semi_Major_Axis",
                    FormatValue(semiMajor / CelestialScale.AuKm / 1000.0))
                : "—";
        }
        if (_mechOrbitalPeriodValue != null) {
            _mechOrbitalPeriodValue.text = energy < 0.0
                ? FormatUnitValue(periodYears * 31556952.0, "time",
                    _mechanicsPanel, "AutoParameter_Orbital_Period",
                    FormatValue(periodYears))
                : "—";
        }
    }

    private void FillStarEnergyFields(SandboxStar star, SandboxPhysicsBody body) {
        if (star == null || star.snapshot == null || body == null) return;
        SandboxStarSnapshot s = star.snapshot;

        if (_energyCoreTemperatureValue != null) {
            _energyCoreTemperatureValue.text = s.coreTemperatureK > 0.0
                ? FormatUnitValue(s.coreTemperatureK, "temperature",
                    _energyPanel, "AutoParameter_Centural_Temperature",
                    FormatValue(s.coreTemperatureK) + " K")
                : "—";
        }
        if (_energyCorePressureValue != null) {
            _energyCorePressureValue.text = "—";
        }

        double massKg = body.massKg;
        double radiusM = body.radiusM;
        double binding = radiusM > 0.0
            ? 3.0 * SandboxPhysicsSystem.GravitationalConstant * massKg * massKg /
              (5.0 * radiusM)
            : 0.0;
        double fusion = s.luminosityLsun * SandboxStarModel.SunLuminosityW;
        double thermal = 4.0 * Math.PI * radiusM * radiusM *
                         SandboxPhysicsSystem.StefanBoltzmann *
                         Math.Pow(s.temperatureK, 4.0);

        if (_energyBindingValue != null) {
            _energyBindingValue.text = FormatUnitValue(binding, "energy",
                _energyPanel, "AutoParameter_Gravitational_Combinantion_Energy",
                FormatValue(binding));
        }
        if (_energyFusionValue != null) {
            _energyFusionValue.text = FormatUnitValue(fusion, "power",
                _energyPanel, "AutoParameter_FusionPower", FormatValue(fusion));
        }
        if (_energyAtmosphereHeatingValue != null) {
            _energyAtmosphereHeatingValue.text = "不适用";
        }
        if (_energyRadiationHeatingValue != null) {
            _energyRadiationHeatingValue.text = "不适用";
        }
        if (_energyThermalRadiationValue != null) {
            _energyThermalRadiationValue.text = FormatUnitValue(thermal, "power",
                _energyPanel, "AutoParameter_ThermalRadiationPower", FormatValue(thermal));
        }
        // 波段功率：普朗克定律在 [λmin, λmax] 内积分 × 恒星表面积 × π。
        if (_energyWavePowerValue != null) {
            double lowNm = ReadWaveLengthNm(_energyPanel, _energyWaveLowInput,
                "IF_Type02WaveLengthLow", 400.0);
            double highNm = ReadWaveLengthNm(_energyPanel, _energyWaveHighInput,
                "IF_Type02_WaveLengthup", 760.0);
            double bandPower = SandboxSpectrum.BandPowerW(s.temperatureK, radiusM, lowNm, highNm);
            _energyWavePowerValue.text = FormatUnitValue(bandPower, "power",
                _energyPanel, "AutoParameter_WavePower", FormatValue(bandPower));
        }

        if (_energyTotalInputValue != null) {
            // 大气加热/外部辐射输入不适用，总输入就是核聚变功率。
            _energyTotalInputValue.text = FormatUnitValue(fusion, "power",
                _energyPanel, "AutoParameter_TotalInputPower", FormatValue(fusion));
        }
    }

    private double ReadWaveLengthNm(GameObject panel, TMP_InputField input,
            string rowName, double fallback) {
        if (input == null) return fallback;
        if (!double.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture,
                out double value) || value <= 0.0) {
            return fallback;
        }
        UnitButton picker = GetRowUnit(panel, rowName);
        SandboxUnitDefinition def = picker != null
            ? SandboxUnits.Definition("wavelength", picker.GetUnit())
            : null;
        return def != null ? SandboxUnits.ToBase(value, def) : value;
    }

    private void OnWaveBandEndEdit(string value) {
        if (_selectedStar == null) return;
        RefreshStarDerivedFields(_selectedStar);
    }

    private void OnPlanetWaveBandEndEdit(string value) {
        if (_selectedPlanet == null) return;
        UpdateSelectedPlanetTemperatureDisplay();
    }

    /// <summary>世界坐标里两个点之间的距离（米），用 1 单位 = KmPerUnit 换算。</summary>
    private double CameraDistanceMeters(Vector3 worldPosition) {
        if (_camera == null) return 0.0;
        double units = SandboxWorldScale.ToVirtual(
            Vector3.Distance(_camera.transform.position, worldPosition));
        return units * CelestialScale.KmPerUnit * 1000.0;
    }

    private static void SetInputIfNotFocused(TMP_InputField input, string text) {
        if (input == null || input.isFocused) return;
        if (input.text != text) input.text = text;
    }

    private void OnSpinPeriodEndEdit(string value) {
        if (_selectedStar == null) return;
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                out double input) || input <= 0.0) {
            RefreshParameterFields();
            return;
        }

        UnitButton picker = GetRowUnit(_mechanicsPanel, "IF_Type02_SpinPeriod");
        SandboxUnitDefinition timeDef = picker != null
            ? SandboxUnits.Definition("time", picker.GetUnit())
            : null;
        double days = timeDef != null
            ? SandboxUnits.ToBase(input, timeDef) / 86400.0
            : input;
        if (days <= 0.0) {
            RefreshParameterFields();
            return;
        }

        _selectedStar.rotationPeriodDays = days;
        if (_selectedStar.snapshot != null) {
            _selectedStar.snapshot.rotationPeriodDays = days;
        }
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(_selectedStar);
        if (body != null) body.rotationPeriodDays = days;
        RefreshStarDerivedFields(_selectedStar);
    }

    private double ReadSpinPeriodSeconds() {
        if (_mechSpinPeriodInput == null) return 0.0;
        if (!double.TryParse(_mechSpinPeriodInput.text,
                NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
            value <= 0.0) {
            return 0.0;
        }
        UnitButton picker = GetRowUnit(_mechanicsPanel, "IF_Type02_SpinPeriod");
        SandboxUnitDefinition timeDef = picker != null
            ? SandboxUnits.Definition("time", picker.GetUnit())
            : null;
        return timeDef != null
            ? SandboxUnits.ToBase(value, timeDef)
            : value * 86400.0;
    }

    private static SandboxDVec3 CrossD(SandboxDVec3 a, SandboxDVec3 b) =>
        new(a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);

    private string MassUnit => _massUnit != null ? _massUnit.GetUnit() : "Msun";
    private string AgeUnit => _ageUnit != null ? _ageUnit.GetUnit() : "Myr";
    private string RadiusUnit => _radiusUnit != null ? _radiusUnit.GetUnit() : "Rsun";
    private string TemperatureUnit => _temperatureUnit != null ? _temperatureUnit.GetUnit() : "K";
    private string DensityUnit => _densityUnit != null ? _densityUnit.GetUnit() : "g/cm3";
    private string LuminosityUnit => _luminosityUnit != null ? _luminosityUnit.GetUnit() : "Lsun";

    private void OnNameEndEdit(string value) {
        if (_selectedStar == null) return;

        string name = string.IsNullOrEmpty(value) ? "" : value.Trim();
        if (string.IsNullOrEmpty(name)) {
            RefreshParameterFields();
            return;
        }

        _selectedStar.starName = name;
        _selectedStar.gameObject.name = name;
        if (_titleContent != null) _titleContent.text = name;
    }

    private void OnMassEndEdit(string value) {
        if (_selectedStar == null) return;
        if (!TryParse(value, out double input)) {
            RefreshParameterFields();
            return;
        }

        double mass = input;
        SandboxUnitDefinition massDef = SandboxUnits.Definition("mass", MassUnit);
        if (massDef != null) {
            mass = SandboxUnits.ToBase(input, massDef) / SandboxPhysicsSystem.SunMassKg;
        } else if (MassUnit == "Mearth") {
            mass = input / SandboxStarModel.EarthMassPerSun;
        }
        ApplyEdits(mass, _selectedStar.metallicity, _selectedStar.ageMyr);
    }

    private void OnAgeEndEdit(string value) {
        if (_selectedStar == null) return;
        if (!TryParse(value, out double input)) {
            RefreshParameterFields();
            return;
        }

        double ageMyr = input;
        SandboxUnitDefinition ageDef = SandboxUnits.Definition("time", AgeUnit);
        if (ageDef != null) {
            double seconds = SandboxUnits.ToBase(input, ageDef);
            ageMyr = seconds / (31556952.0 * 1e6);
        } else if (AgeUnit == "Gyr") {
            ageMyr = input * 1000.0;
        }
        ApplyEdits(_selectedStar.mass, _selectedStar.metallicity, ageMyr);
    }

    private void OnMetalEndEdit(string value) {
        if (_selectedStar == null) return;
        if (!TryParse(value, out double input)) {
            RefreshParameterFields();
            return;
        }

        ApplyEdits(_selectedStar.mass, input, _selectedStar.ageMyr);
    }

    private void ApplyEdits(double mass, double metallicity, double ageMyr) {
        if (_selectedStar == null) return;

        if (!SandboxStarModel.TryCompute(mass, metallicity, ageMyr,
                _selectedStar.rotationPeriodDays,
                out SandboxStarSnapshot snapshot, out string error)) {
            FloatingText.Show(error);
            RefreshParameterFields();
            return;
        }

        _selectedStar.mass = mass;
        _selectedStar.metallicity = metallicity;
        _selectedStar.ageMyr = ageMyr;
        _selectedStar.ApplySnapshot(snapshot);
        RefreshParameterFields();
    }

    // ~~~ 行星参数面板 ~~~ //

    private string PlanetMassUnit => _planetMassUnit != null ? _planetMassUnit.GetUnit() : "Mearth";
    private string PlanetTempUnit => _planetTempUnit != null ? _planetTempUnit.GetUnit() : "K";
    private string PlanetAtmosphereUnit => _planetAtmosphereUnit != null
        ? _planetAtmosphereUnit.GetUnit() : "Mearth";
    private string PlanetEnergyAtmosphereUnit {
        get {
            UnitButton picker = GetRowUnit(_planetEnergyPanel,
                "IF_Type02_AutomspherericMass");
            return picker != null ? picker.GetUnit() : "Mearth";
        }
    }
    private string PlanetRadiusUnit => _planetRadiusUnit != null ? _planetRadiusUnit.GetUnit() : "Rearth";
    private string PlanetDensityUnit => _planetDensityUnit != null ? _planetDensityUnit.GetUnit() : "g/cm3";
    private string PlanetGravityUnit => _planetGravityUnit != null ? _planetGravityUnit.GetUnit() : "g";
    private string PlanetPressureUnit => _planetPressureUnit != null ? _planetPressureUnit.GetUnit() : "atm";

    private void RefreshPlanetParameterFields() {
        if (_selectedPlanet == null || _selectedPlanet.snapshot == null) return;

        SandboxPlanet planet = _selectedPlanet;
        SandboxPlanetSnapshot snapshot = planet.snapshot;
        ApplyAutoPlanetUnits(snapshot, planet.temperatureK, planet.atmosphereMassEarth);

        if (_planetNameInput != null) _planetNameInput.text = planet.planetName;
        if (_planetMassInput != null) _planetMassInput.text = FormatPlanetMass(planet.massEarth, PlanetMassUnit);
        if (_planetTempInput != null) _planetTempInput.text = FormatTemperature(planet.temperatureK, PlanetTempUnit);
        if (_planetAtmosphereInput != null) {
            _planetAtmosphereInput.text = FormatPlanetAtmosphere(planet.atmosphereMassEarth,
                PlanetAtmosphereUnit);
        }
        if (_planetEnergyAtmosphereInput != null && !_planetEnergyAtmosphereInput.isFocused) {
            _planetEnergyAtmosphereInput.text = FormatPlanetAtmosphere(
                planet.atmosphereMassEarth, PlanetEnergyAtmosphereUnit);
        }
        if (_planetWaterDepthInput != null && !_planetWaterDepthInput.isFocused) {
            UnitButton waterUnit = GetRowUnit(_planetEnergyPanel,
                "IF_Type02_WaterDepth");
            _planetWaterDepthInput.text = waterUnit != null
                ? FormatUnitValue(planet.surfaceWaterDepthM, "length",
                    _planetEnergyPanel, "IF_Type02_WaterDepth", "0")
                : FormatValue(planet.surfaceWaterDepthM / 1000.0);
        }
        if (_planetConcentrationInput != null && !_planetConcentrationInput.isFocused) {
            _planetConcentrationInput.text = FormatUnitValue(
                planet.soluteConcentrationMmolL, "concentration",
                _planetParameterPanel, _planetConcentrationRowName,
                FormatValue(planet.soluteConcentrationMmolL));
        }
        RefreshCalculatedWaterPoint(_planetFreezePointInput, _planetFreezePointText,
            planet.freezingPointK, _planetFreezePointRowName);
        RefreshCalculatedWaterPoint(_planetBoilPointInput, _planetBoilPointText,
            planet.boilingPointK, _planetBoilPointRowName);

        if (_planetTitleContent != null) _planetTitleContent.text = planet.planetName;
        if (_planetMassLabel != null) _planetMassLabel.text = "质量";
        if (_planetTempLabel != null) _planetTempLabel.text = "温度";
        if (_planetAtmosphereLabel != null) _planetAtmosphereLabel.text = "大气质量";

        if (_planetRadiusValue != null) {
            _planetRadiusValue.text = FormatPlanetRadius(snapshot.radiusEarth, PlanetRadiusUnit);
        }
        if (_planetDensityValue != null) {
            _planetDensityValue.text = FormatDensity(snapshot.densityGcm3, PlanetDensityUnit);
        }
        if (_planetGravityValue != null) {
            _planetGravityValue.text = FormatPlanetGravity(snapshot.gravityG, PlanetGravityUnit);
        }
        if (_planetPressureValue != null) {
            _planetPressureValue.text = FormatPlanetPressure(snapshot.pressureAtm, PlanetPressureUnit);
        }

        RefreshLockButtonState(_planetLockPositionButton,
            SandboxPhysicsSystem.GetBody(_selectedPlanet));
        UpdateExpandButtonText();
    }

    private void OnPlanetNameEndEdit(string value) {
        if (_selectedPlanet == null) return;
        string name = string.IsNullOrEmpty(value) ? "" : value.Trim();
        if (string.IsNullOrEmpty(name)) {
            RefreshPlanetParameterFields();
            return;
        }

        _selectedPlanet.planetName = name;
        _selectedPlanet.gameObject.name = name;
        if (_planetTitleContent != null) _planetTitleContent.text = name;
    }

    private void OnPlanetMassEndEdit(string value) {
        if (_selectedPlanet == null || _selectedPlanet.snapshot == null) return;
        if (!TryParse(value, out double input) || input <= 0.0) {
            RefreshPlanetParameterFields();
            return;
        }

        double massEarth = input;
        SandboxUnitDefinition massDef = SandboxUnits.Definition("mass", PlanetMassUnit);
        if (massDef != null) {
            massEarth = SandboxUnits.ToBase(input, massDef) /
                        SandboxPhysicsSystem.EarthMassKg;
        } else if (PlanetMassUnit == "Msun") {
            massEarth = input * SandboxStarModel.EarthMassPerSun;
        }
        SandboxPlanet planet = _selectedPlanet;

        // SpecialPlanets（例如心大星Ab）：改质量只更新质量，不参与类型转换。
        if (planet.special) {
            ApplyPlanetEdits(massEarth, planet.radiusEarth, planet.atmosphereMassEarth,
                planet.temperatureK);
            return;
        }

        double massJup = massEarth / SandboxPlanetMassModel.JupiterMassEarth;

        // 1) 超过 77 M_Jup：直接变成恒星（金属度默认 0.02）。
        if (massJup > SandboxPlanetMassModel.StarThresholdJup) {
            SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(planet);
            if (body != null) {
                double targetKg = massEarth * SandboxPlanetMassModel.EarthMassKg;
                double addedKg = targetKg - body.massKg;
                if (ConvertPlanetToStar(body, addedKg)) return;
                FloatingText.Show("质量已超过 77 M_Jup，但转换为恒星失败");
            }
        }

        // 2a) 之前被岩石化的天体，质量改回 10 M_E 以上：变回气态巨行星。
        if (planet.rockyConverted && massEarth > SandboxPlanetMassModel.RockyMaxEarth) {
            planet.rockyConverted = false;
            planet.isGasGiant = true;
            if (planet.snapshot != null) {
                planet.snapshot.surfaceTexturePath = RandomGasGiantTexture();
            }
            double gasScale = SandboxPlanetMassModel.ConvertedGasRadiusKm;
            if (planet.snapshot != null) planet.snapshot.atmosphereTexturePath = "";
            double gasRadiusKm = SandboxPlanetMassModel.RadiusKm(massEarth, gasScale);
            double gasTemp = planet.temperatureK;
            if (massJup > SandboxPlanetMassModel.GasGiantMinJup) {
                gasTemp = SandboxPlanetMassModel.GasGiantTemperatureK(massJup, gasRadiusKm);
            }
            ApplyPlanetEdits(massEarth,
                gasRadiusKm / SandboxPlanetMassModel.EarthRadiusKm,
                planet.atmosphereMassEarth, gasTemp);
            return;
        }

        // 2b) 非原生岩石行星被改到 10 M_E 以下：岩石化（换贴图 + 固定密度 3.5 g/cm³）。
        if (planet.isGasGiant && massEarth <= SandboxPlanetMassModel.RockyMaxEarth) {
            planet.rockyConverted = true;
            planet.isGasGiant = false;
            if (planet.snapshot != null) {
                planet.snapshot.surfaceTexturePath = "Textures/Planets/Random_RockyPlanet";
                planet.snapshot.heightMapPath = null;
                planet.snapshot.surfaceWaterDepthM = 0.0;
            }
            double rockyRadius = SandboxPlanetMassModel.RockyRadiusKm(massEarth) /
                                 SandboxPlanetMassModel.EarthRadiusKm;
            ApplyPlanetEdits(massEarth, rockyRadius, 0.0, planet.temperatureK);
            return;
        }

        // 2c) 仍在岩石区间（≤10 M_E）：原生岩石行星按原来的恒密度关系，
        //     被岩石化过的按 3.5 g/cm³ 反算半径。绝不能掉进气态分支。
        if (massEarth <= SandboxPlanetMassModel.RockyMaxEarth) {
            double rockyRadiusEarth = planet.rockyConverted
                ? SandboxPlanetMassModel.RockyRadiusKm(massEarth) /
                  SandboxPlanetMassModel.EarthRadiusKm
                : SandboxPlanetModel.RadiusForMass(massEarth, planet.snapshot.densityGcm3);
            ApplyPlanetEdits(massEarth, rockyRadiusEarth, planet.atmosphereMassEarth,
                planet.temperatureK);
            return;
        }

        // 3) 气态行星：R = 比例系数 × f(M)；>10 M_Jup 时温度按聚变功率反推。
        bool wasGasBefore = planet.isGasGiant;
        // A 类（目录里的气态行星）用自身反推的比例系数；
        // B 类（岩石行星/恒星改造成的）一律用 70000 km 当 InitialRadius。
        bool convertedGas = planet.rockyOrigin || planet.rockyConverted;
        double scaleKm = convertedGas || planet.radiusScaleKm <= 0.0
            ? SandboxPlanetMassModel.ConvertedGasRadiusKm
            : planet.radiusScaleKm;
        double radiusKm = SandboxPlanetMassModel.RadiusKm(massEarth, scaleKm);
        double radiusEarth = radiusKm / SandboxPlanetMassModel.EarthRadiusKm;
        double temperature = planet.temperatureK;
        if (massJup > SandboxPlanetMassModel.GasGiantMinJup) {
            temperature = SandboxPlanetMassModel.GasGiantTemperatureK(massJup, radiusKm);
        }

        // 只要离开岩石区间就是气态行星：刚变成气态的随机挑一张，原本就在气态区间保持原贴图。
        if (planet.snapshot != null) {
            bool needGasTexture = planet.rockyOrigin || planet.rockyConverted ||
                string.IsNullOrEmpty(planet.snapshot.surfaceTexturePath) ||
                planet.snapshot.surfaceTexturePath.Contains("Random_RockyPlanet");
            if (needGasTexture) {
                if (!wasGasBefore) {
                    planet.snapshot.surfaceTexturePath = RandomGasGiantTexture();
                } else if (string.IsNullOrEmpty(planet.snapshot.surfaceTexturePath)) {
                    planet.snapshot.surfaceTexturePath = "Textures/Planets/Aquarius_One_d";
                }
                planet.snapshot.heightMapPath = null;
                planet.snapshot.surfaceWaterDepthM = 0.0;
                // 变成气态行星就没有云层了。
                planet.snapshot.atmosphereTexturePath = "";
            }
            planet.rockyConverted = false;
            planet.isGasGiant = true;
        }
        ApplyPlanetEdits(massEarth, radiusEarth, planet.atmosphereMassEarth, temperature);
    }

    private void OnPlanetTempEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input)) {
            RefreshPlanetParameterFields();
            return;
        }

        SandboxUnitDefinition tempDef =
            SandboxUnits.Definition("temperature", PlanetTempUnit);
        double kelvin = tempDef != null
            ? SandboxUnits.ToBase(input, tempDef)
            : input;
        ApplyPlanetEdits(_selectedPlanet.massEarth, _selectedPlanet.radiusEarth,
            _selectedPlanet.atmosphereMassEarth, kelvin);
    }

    private void OnStarOrbitAngleEndEdit(string value) {
        if (_selectedStar == null || !TryParse(value, out double degrees)) {
            RefreshParameterFields();
            return;
        }
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(_selectedStar);
        if (body != null) ApplyOrbitInclination(body, degrees);
    }

    private void OnPlanetOrbitAngleEndEdit(string value) {
        if (_selectedPlanet == null || !TryParse(value, out double degrees)) {
            RefreshPlanetParameterFields();
            return;
        }
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(_selectedPlanet);
        if (body != null) ApplyOrbitInclination(body, degrees);
    }

    private static void ApplyOrbitInclination(SandboxPhysicsBody body, double degrees) {
        double newAngle = Math.Clamp(degrees, -90.0, 90.0);
        double delta = (newAngle - body.orbitInclinationDeg) * Math.PI / 180.0;
        body.orbitInclinationDeg = newAngle;

        if (body.orbitTarget == null || Math.Abs(delta) < 1e-12) return;
        SandboxDVec3 radial = body.positionM - body.orbitTarget.positionM;
        double radius = radial.Magnitude;
        if (radius <= 1e-9) return;
        radial = radial / radius;

        SandboxDVec3 relative = body.velocityMps - body.orbitTarget.velocityMps;
        double cos = Math.Cos(delta);
        double sin = Math.Sin(delta);
        SandboxDVec3 cross = CrossD(radial, relative);
        double dot = radial.x * relative.x + radial.y * relative.y + radial.z * relative.z;
        SandboxDVec3 rotated = relative * cos + cross * sin + radial * (dot * (1.0 - cos));
        body.velocityMps = body.orbitTarget.velocityMps + rotated;
    }

    /// <summary>自转轴倾角输入框：按当前单位换算成度，写回行星并立即应用。</summary>
    private void OnPlanetSpinAngleEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input)) {
            RefreshPlanetParameterFields();
            return;
        }

        UnitButton picker = GetRowUnit(_planetMechanicsPanel, "IF_Type02_SpinAngle");
        SandboxUnitDefinition def = picker != null
            ? SandboxUnits.Definition("angle", picker.GetUnit())
            : null;
        double degrees = def != null ? SandboxUnits.ToBase(input, def) : input;
        degrees = Math.Clamp(degrees, -180.0, 180.0);

        _selectedPlanet.axialTiltDeg = degrees;
        if (_selectedPlanet.snapshot != null) {
            _selectedPlanet.snapshot.axialTiltDeg = degrees;
        }
        _selectedPlanet.ApplyAxialTilt();
        RefreshPlanetParameterFields();
    }
    private void OnPlanetSpinPeriodEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input) || input < 0.0) {
            RefreshPlanetParameterFields();
            return;
        }

        if (input == 0.0) {
            _selectedPlanet.rotationPeriodDays = 0.0;
            if (_selectedPlanet.snapshot != null) {
                _selectedPlanet.snapshot.rotationPeriodDays = 0.0;
            }
            SandboxPhysicsBody bodyAtZero = SandboxPhysicsSystem.GetBody(_selectedPlanet);
            if (bodyAtZero != null) bodyAtZero.rotationPeriodDays = 0.0;
            RefreshPlanetParameterFields();
            return;
        }

        UnitButton picker = GetRowUnit(_planetMechanicsPanel, "IF_Type02_SpinPeriod");
        SandboxUnitDefinition timeDef = picker != null
            ? SandboxUnits.Definition("time", picker.GetUnit())
            : null;
        double seconds = timeDef != null
            ? SandboxUnits.ToBase(input, timeDef)
            : input * 86400.0;
        if (seconds <= 0.0) {
            RefreshPlanetParameterFields();
            return;
        }

        double radiusM = _selectedPlanet.radiusEarth *
                         SandboxPlanetModel.EarthRadiusKm * 1000.0;
        double massKg = _selectedPlanet.massEarth * SandboxPhysicsSystem.EarthMassKg;
        double firstCosmic = Math.Sqrt(
            SandboxPhysicsSystem.GravitationalConstant * massKg /
            Math.Max(radiusM, 1e-9));
        double equatorSpeed = 2.0 * Math.PI * radiusM / seconds;

        if (equatorSpeed > firstCosmic) {
            FloatingText.Show("速度过快，会散架的");
            RefreshPlanetParameterFields();
            return;
        }

        double days = seconds / 86400.0;
        _selectedPlanet.rotationPeriodDays = days;
        if (_selectedPlanet.snapshot != null) {
            _selectedPlanet.snapshot.rotationPeriodDays = days;
        }
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(_selectedPlanet);
        if (body != null) body.rotationPeriodDays = days;
        RefreshPlanetParameterFields();
    }

    private void OnGreenhouseEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input) || double.IsNaN(input)) {
            RefreshPlanetParameterFields();
            return;
        }

        double factor = Math.Clamp(input, 0.0, 1.0);
        _selectedPlanet.greenhouseFactor = factor;
        if (_selectedPlanet.snapshot != null) {
            _selectedPlanet.snapshot.greenhouseFactor = factor;
        }
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(_selectedPlanet);
        if (body != null) body.greenhouseFactor = factor;
        _planetGreenhouseInput.text = FormatValue(factor);
    }

    private void OnStarLockPositionClick() {
        if (_selectedStar == null) return;
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(_selectedStar);
        if (body == null) return;
        bool locked = !body.positionLocked;
        SandboxPhysicsSystem.SetPositionLocked(body, locked);
        FloatingText.Show(locked ? "恒星位置已锁定" : "恒星位置已解锁");
        RefreshLockButtonState(_starLockPositionButton, body);
    }

    private void OnPlanetLockPositionClick() {
        if (_selectedPlanet == null) return;
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(_selectedPlanet);
        if (body == null) return;
        bool locked = !body.positionLocked;
        SandboxPhysicsSystem.SetPositionLocked(body, locked);
        FloatingText.Show(locked ? "行星位置已锁定" : "行星位置已解锁");
        RefreshLockButtonState(_planetLockPositionButton, body);
    }

    /// <summary>两个参数面板的锁定按钮都按当前选中天体刷新；没选中的恢复白色。</summary>
    private void RefreshLockButtons() {
        RefreshLockButtonState(_starLockPositionButton,
            SandboxPhysicsSystem.GetBody(_selectedStar));
        RefreshLockButtonState(_planetLockPositionButton,
            SandboxPhysicsSystem.GetBody(_selectedPlanet));
    }

    private static void RefreshLockButtonState(Button button, SandboxPhysicsBody body) {
        if (button == null || button.targetGraphic == null) return;
        button.targetGraphic.color = body != null && body.positionLocked
            ? new Color(0.2f, 0.9f, 0.35f, 1f)
            : Color.white;
    }

    private void OnPlanetConcentrationEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input) || input < 0.0) {
            RefreshPlanetParameterFields();
            return;
        }

        UnitButton picker = GetRowUnit(_planetParameterPanel,
            _planetConcentrationRowName);
        SandboxUnitDefinition def = picker != null
            ? SandboxUnits.Definition("concentration", picker.GetUnit())
            : null;
        double mmolL = def != null ? SandboxUnits.ToBase(input, def) : input;
        if (mmolL > 5400.0) {
            FloatingText.Show("超过饱和浓度");
            RefreshPlanetParameterFields();
            return;
        }

        _selectedPlanet.SetSoluteConcentration(mmolL);
        RefreshPlanetParameterFields();
    }

    private void RefreshCalculatedWaterPoint(TMP_InputField input, TMP_Text text,
            double kelvin, string rowName) {
        if (string.IsNullOrEmpty(rowName)) return;
        string value = FormatUnitValue(kelvin, "temperature",
            _planetParameterPanel, rowName, FormatValue(kelvin));
        if (input != null && !input.isFocused) input.text = value;
        if (text != null) text.text = value;
    }

    private void OnPlanetAtmosphereEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input) || input < 0.0) {
            RefreshPlanetParameterFields();
            return;
        }

        double atmosphere = input;
        SandboxUnitDefinition atmDef =
            SandboxUnits.Definition("mass", PlanetAtmosphereUnit);
        if (atmDef != null) {
            atmosphere = SandboxUnits.ToBase(input, atmDef) /
                         SandboxPlanetModel.EarthAtmosphereKg;
        } else if (PlanetAtmosphereUnit == "kg") {
            atmosphere = input / SandboxPlanetModel.EarthAtmosphereKg;
        }
        ApplyPlanetEdits(_selectedPlanet.massEarth, _selectedPlanet.radiusEarth,
            atmosphere, _selectedPlanet.temperatureK);
    }

    /// <summary>
    /// Energy&amp;Structure 面板里的“大气质量”输入框。
    /// 和 General 面板走同一套换算，只是单位按钮是这一行自己的。
    /// </summary>
    private void OnPlanetEnergyAtmosphereEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input) || input < 0.0) {
            RefreshPlanetParameterFields();
            return;
        }

        string unit = PlanetEnergyAtmosphereUnit;
        SandboxUnitDefinition def = SandboxUnits.Definition("mass", unit);
        double atmosphereMassEarth = def != null
            ? SandboxUnits.ToBase(input, def) / SandboxPlanetModel.EarthAtmosphereKg
            : (unit == "kg"
                ? input / SandboxPlanetModel.EarthAtmosphereKg
                : input);

        ApplyPlanetEdits(_selectedPlanet.massEarth, _selectedPlanet.radiusEarth,
            atmosphereMassEarth, _selectedPlanet.temperatureK);
    }

    private void OnPlanetSurfaceWaterDepthEndEdit(string value) {
        if (_selectedPlanet == null) return;
        if (!TryParse(value, out double input) || input < 0.0) {
            RefreshPlanetParameterFields();
            return;
        }

        UnitButton picker = GetRowUnit(_planetEnergyPanel,
            "IF_Type02_WaterDepth");
        SandboxUnitDefinition lengthDef = picker != null
            ? SandboxUnits.Definition("length", picker.GetUnit())
            : null;
        double meters = lengthDef != null
            ? SandboxUnits.ToBase(input, lengthDef)
            : input * 1000.0;
        _selectedPlanet.SetSurfaceWaterDepth(meters);
        RefreshPlanetParameterFields();
    }

    private void ApplyPlanetEdits(double massEarth, double radiusEarth,
            double atmosphereMassEarth, double temperatureK) {
        if (_selectedPlanet == null) return;

        if (!SandboxPlanetModel.TryCompute(massEarth, radiusEarth, atmosphereMassEarth,
                temperatureK,
                _selectedPlanet.snapshot != null
                    ? _selectedPlanet.snapshot.surfaceTexturePath
                    : null,
                _selectedPlanet.snapshot != null
                    ? _selectedPlanet.snapshot.atmosphereTexturePath
                    : null,
                out SandboxPlanetSnapshot snapshot, out string error)) {
            FloatingText.Show(error);
            RefreshPlanetParameterFields();
            return;
        }

        snapshot.rotationPeriodDays = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.rotationPeriodDays
            : 1.0;
        snapshot.rotationDirection = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.rotationDirection
            : 1;
        snapshot.greenhouseFactor = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.greenhouseFactor
            : 0.0;
        snapshot.trailColor = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.trailColor
            : new Color(200f / 255f, 200f / 255f, 200f / 255f, 1f);
        snapshot.heightMapPath = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.heightMapPath
            : null;
        snapshot.surfaceWaterDepthM = _selectedPlanet.surfaceWaterDepthM;
        snapshot.landHeightMaxM = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.landHeightMaxM
            : 20000.0;
        snapshot.soluteConcentrationMmolL = _selectedPlanet.soluteConcentrationMmolL;
        // 保留目录里的分类（SpecialPlanets 等），否则改一次质量就丢了。
        snapshot.category = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.category : null;
        snapshot.atmosphereColor = _selectedPlanet.snapshot != null
            ? _selectedPlanet.snapshot.atmosphereColor
            : new Color(0.55f, 0.75f, 1f, 1f);

        // 高温行星要自己发光：温度越高黑体辐射越亮。
        if (temperatureK > 500.0) {
            snapshot.emissionStrength = Mathf.Clamp01((float)((temperatureK - 500.0) / 1200.0));
        }
        _selectedPlanet.ApplySnapshot(snapshot);
        // 物理里旧的斑块温度会把新温度拉回去，这里强制重置整个温度场。
        SandboxPhysicsSystem.ResetSurfaceTemperature(_selectedPlanet, temperatureK);
        RefreshPlanetParameterFields();
    }

    private static string FormatPlanetMass(double massEarth, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("mass", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(
                massEarth * SandboxPhysicsSystem.EarthMassKg, def));
        }
        return unit == "Msun"
            ? FormatValue(massEarth / SandboxStarModel.EarthMassPerSun)
            : FormatValue(massEarth);
    }

    private static string FormatPlanetAtmosphere(double atmosphere, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("mass", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(
                atmosphere * SandboxPlanetModel.EarthAtmosphereKg, def));
        }
        return unit == "kg"
            ? FormatValue(atmosphere * SandboxPlanetModel.EarthAtmosphereKg)
            : FormatValue(atmosphere);
    }

    private static string FormatPlanetRadius(double radiusEarth, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("length", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(
                radiusEarth * SandboxPlanetModel.EarthRadiusKm * 1000.0, def));
        }
        return unit switch {
            "km" => FormatValue(radiusEarth * SandboxPlanetModel.EarthRadiusKm),
            "Rsun" => FormatValue(radiusEarth / SandboxStarModel.EarthRadiusPerSun),
            _ => FormatValue(radiusEarth),
        };
    }

    private static string FormatPlanetGravity(double gravity, string unit) {
        return unit == "m/s2"
            ? FormatValue(gravity * SandboxPlanetModel.EarthGravity)
            : FormatValue(gravity);
    }

    private static string FormatPlanetPressure(double pressure, string unit) {
        return unit == "Pa" ? FormatValue(pressure * 101325.0) : FormatValue(pressure);
    }

    private static bool TryParse(string text, out double value) {
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out value)) {
            return true;
        }
        return double.TryParse(text, out value);
    }

    // ~~~ 数值格式化 / 单位换算 ~~~ //

    private static string FormatValue(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "—";

        double abs = Math.Abs(value);
        if (abs != 0.0 && (abs >= 100000.0 || abs < 0.001)) {
            return value.ToString("0.###E+0", CultureInfo.InvariantCulture);
        }
        if (abs >= 1000.0) return value.ToString("0", CultureInfo.InvariantCulture);
        if (abs >= 10.0) return value.ToString("0.###", CultureInfo.InvariantCulture);
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static string FormatMass(double mass, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("mass", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(
                mass * SandboxPhysicsSystem.SunMassKg, def));
        }
        return unit == "Mearth"
            ? FormatValue(mass * SandboxStarModel.EarthMassPerSun)
            : FormatValue(mass);
    }

    private static string FormatAge(double ageMyr, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("time", unit);
        if (def != null) {
            double seconds = ageMyr * 31556952.0 * 1e6;
            return FormatValue(SandboxUnits.FromBase(seconds, def));
        }
        return unit == "Gyr" ? FormatValue(ageMyr / 1000.0) : FormatValue(ageMyr);
    }

    private static string FormatRadius(double radiusRsun, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("length", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(
                radiusRsun * SandboxStarModel.SunRadiusKm * 1000.0, def));
        }
        return unit switch {
            "Rearth" => FormatValue(radiusRsun * SandboxStarModel.EarthRadiusPerSun),
            "km" => FormatValue(radiusRsun * SandboxStarModel.SunRadiusKm),
            _ => FormatValue(radiusRsun),
        };
    }

    private static string FormatTemperature(double kelvin, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("temperature", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(kelvin, def));
        }
        return unit switch {
            "°C" => FormatValue(kelvin - 273.15),
            "°F" => FormatValue((kelvin - 273.15) * 9.0 / 5.0 + 32.0),
            _ => FormatValue(kelvin),
        };
    }

    private static string FormatDensity(double gcm3, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("density", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(gcm3 * 1000.0, def));
        }
        return unit == "kg/m3" ? FormatValue(gcm3 * 1000.0) : FormatValue(gcm3);
    }

    private static string FormatLuminosity(double lsun, string unit) {
        SandboxUnitDefinition def = SandboxUnits.Definition("power", unit);
        if (def != null) {
            return FormatValue(SandboxUnits.FromBase(
                lsun * SandboxStarModel.SunLuminosityW, def));
        }
        return unit == "W" ? FormatValue(lsun * SandboxStarModel.SunLuminosityW) : FormatValue(lsun);
    }

    // ~~~ 场景存档 / 默认场景 ~~~ //

    /// <summary>
    /// 找到场景里做好的 SaveChangePanel（Canvas/SaveChangePanel），
    /// 运行时挂上脚本并默认隐藏。
    /// </summary>
    private void SetUpSaveChangePanel() {
        if (_canvas == null) return;

        Transform panel = FindDeep(_canvas, "SaveChangePanel");
        if (panel == null) {
            Debug.LogWarning("[Save] 场景里没找到 SaveChangePanel");
            return;
        }

        _saveChangePanel = panel.GetComponent<SandboxSaveChangePanel>();
        if (_saveChangePanel == null) {
            _saveChangePanel = panel.gameObject.AddComponent<SandboxSaveChangePanel>();
        }
        _saveChangePanel.Initialize();
        _saveChangePanel.Hide();
    }

    /// <summary>找到场景里做好的 Description_Panel（场景介绍侧栏），挂上脚本并默认隐藏。</summary>
    private void SetUpSceneDescription() {
        if (_canvas == null) return;

        Transform panel = FindDeep(_canvas, "Description_Panel");
        if (panel == null) {
            return;
        }

        _sceneDescription = panel.GetComponent<SandboxSceneDescription>();
        if (_sceneDescription == null) {
            _sceneDescription = panel.gameObject.AddComponent<SandboxSceneDescription>();
        }
        _sceneDescription.Initialize();
        LoadSceneIntros();
    }

    /// <summary>找到场景里的向导（Canvas/Aquarius_One_Guider），挂上脚本。</summary>
    private void SetUpGuide() {
        if (_canvas == null) return;
        Transform guider = FindDeep(_canvas, "Aquarius_One_Guider");
        if (guider == null) return;
        Debug.Log("[Guide] setup activeSelf=" + guider.gameObject.activeSelf +
                  " activeInHierarchy=" + guider.gameObject.activeInHierarchy);
        // 有些流程会把向导物体关掉；这里强制打开，保证组件能收到 Update。
        guider.gameObject.SetActive(true);
        _guide = guider.GetComponent<SandboxGuide>();
        if (_guide == null) _guide = guider.gameObject.AddComponent<SandboxGuide>();
        _guide.Initialize(this);
        _guide.OnHidden = ShowPendingIntro;
    }

    private void LoadSceneIntros() {
        _sceneIntros.Clear();
        TextAsset asset = Resources.Load<TextAsset>("Data/Scenarios");
        if (asset == null) return;

        SceneIntroCatalog catalog = JsonUtility.FromJson<SceneIntroCatalog>(asset.text);
        if (catalog?.scenes == null) return;
        foreach (SceneIntro intro in catalog.scenes) {
            if (intro == null || string.IsNullOrEmpty(intro.id)) continue;
            _sceneIntros[intro.id] = intro;
        }
    }

    /// <summary>打开带介绍的场景时弹出侧栏；没有介绍就保持隐藏。</summary>
    /// <summary>向导隐藏后，把之前被压住的场景介绍补上。</summary>
    private void ShowPendingIntro() {
        if (string.IsNullOrEmpty(_pendingIntroId)) return;
        string id = _pendingIntroId;
        _pendingIntroId = null;
        ShowSceneDescription(id);
    }

    private void ShowSceneDescription(string scenarioId) {
        if (_sceneDescription == null) return;
        // 只要屏幕上有向导（询问/教学/准备/提示），先压住场景介绍，等向导消失再补弹。
        if (SandboxGuide.IsVisible || SandboxGuide.IsRunning || SandboxGuide.IsIntroActive) {
            _pendingIntroId = scenarioId;
            _sceneDescription.Hide();
            return;
        }
        _pendingIntroId = null;
        if (string.IsNullOrEmpty(scenarioId) ||
            !_sceneIntros.TryGetValue(scenarioId, out SceneIntro intro)) {
            _sceneDescription.Hide();
            return;
        }
        _sceneDescription.Show(intro.title, intro.body);
    }

    /// <summary>当前场景是否来自一个“已保存过的存档”（默认场景不算）。</summary>
    public bool IsExistingSaveScene =>
        !_activeBuiltin && !string.IsNullOrEmpty(_activeSavePath) && File.Exists(_activeSavePath);

    public string ActiveSaveName => _activeSaveName;

    /// <summary>每次加载默认场景都会 +1，向导用它判断“用户真的重新进了场景”。</summary>
    public static int ScenarioLoadCount { get; private set; }

    /// <summary>当前是否在水瓶座一星系（向导教学默认场景）。</summary>
    public bool IsAquariusScenario =>
        !string.IsNullOrEmpty(_currentScenarioName) && _currentScenarioName.Contains("水瓶座");

    // 存档/载入时记下“可编辑内容”的指纹；退出或切换存档前拿它判断有没有改过。
    private string _savedSignature = string.Empty;

    /// <summary>已保存存档里是否有未保存的修改。默认场景不算。</summary>
    public bool HasUnsavedChanges =>
        IsExistingSaveScene && ComputeEditableSignature() != _savedSignature;

    /// <summary>
    /// 可编辑内容指纹：天体组成 + 质量/半径/轨道关系 + 时间倍率 + 位置/速度。
    /// 位置和速度按 1 m / 0.001 m/s 量化，避免浮点噪声天天“有改动”。
    /// </summary>
    private string ComputeEditableSignature() {
        SandboxTimeManager time = SandboxTimeManager.Instance;

        List<string> rows = new();
        foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
            if (body == null || !body.alive) continue;
            if (body.fragment != null) continue;
            string orbit = body.orbitTarget != null ? body.orbitTarget.name : "-";
            rows.Add(body.name + "|" + body.massKg.ToString("R") + "|" +
                     body.radiusM.ToString("R") + "|" + orbit + "|" +
                     Quantize(body.positionM.x) + "," + Quantize(body.positionM.y) + "," +
                     Quantize(body.positionM.z) + "|" +
                     Quantize(body.velocityMps.x * 1000.0) + "," +
                     Quantize(body.velocityMps.y * 1000.0) + "," +
                     Quantize(body.velocityMps.z * 1000.0));
        }
        rows.Sort(StringComparer.Ordinal);

        return (time != null ? time.SpeedValue.ToString("R") + "|" + time.Unit : "-") +
               "#" + string.Join(";", rows);
    }

    private static string Quantize(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
        return Math.Round(value).ToString("R", CultureInfo.InvariantCulture);
    }

    private void RememberSavedState() {
        _savedSignature = ComputeEditableSignature();
    }

    /// <summary>
    /// 退出 / 切换存档前的统一确认：当前是“已保存过的存档”且被改过 → 弹 SaveChangePanel。
    /// 是 = 先保存再继续，否 = 直接继续（丢弃修改）。
    /// 返回 true 表示已经弹窗、调用方不要再继续。
    /// </summary>
    private bool ConfirmDiscardThen(Action onProceed) {
        if (!HasUnsavedChanges || _saveChangePanel == null) return false;

        _saveChangePanel.Ask(_activeSaveName,
            () => {
                if (SaveSceneNamed(_activeSaveName)) onProceed();
            },
            onProceed);
        return true;
    }

    public void RequestQuitSandbox() {
        if (ConfirmDiscardThen(QuitToMainMenu)) return;
        QuitToMainMenu();
    }

    private static void QuitToMainMenu() {
        SceneManager.LoadScene("MainMenu");
    }

    public void RequestLoadSaveFile(string path) {
        if (ConfirmDiscardThen(() => LoadSaveFile(path))) return;
        LoadSaveFile(path);
    }

    public void RequestLoadDefaultScenario(string id) {
        if (ConfirmDiscardThen(() => LoadDefaultScenario(id))) return;
        LoadDefaultScenario(id);
    }

    public void RequestNewBlankScenario() {
        if (ConfirmDiscardThen(NewBlankScenario)) return;
        NewBlankScenario();
    }

    /// <summary>
    /// 保存入口：如果当前打开的就是一个已保存过的存档（非默认场景），
    /// 先弹 SaveChangePanel 确认，是 = 覆盖保存，否 = 撤销保存。
    /// </summary>
    public bool RequestSave(string name) {
        if (string.IsNullOrWhiteSpace(name)) return false;
        name = name.Trim();

        if (IsExistingSaveScene && _saveChangePanel != null) {
            _saveChangePanel.Ask(name, () => SaveSceneNamed(name));
            return true;
        }

        return SaveSceneNamed(name);
    }

    public bool SaveSceneNamed(string name) {
        if (string.IsNullOrWhiteSpace(name)) return false;
        name = name.Trim();
        // 内置场景是只读模板，同名保存会自动落到“副本”，不覆盖模板。
        if (_activeBuiltin && (name == "水瓶座一系" || name == "太阳系")) {
            name += " 副本";
        }
        SandboxSaveData data = BuildSaveData(name);
        if (data.bodies.Count == 0) {
            FloatingText.Show("当前场景没有可保存的天体");
            return false;
        }
        string path = SandboxSaveSystem.SaveScene(data);
        if (string.IsNullOrEmpty(path)) {
            FloatingText.Show("存档写入失败");
            return false;
        }
        _activeSavePath = path;
        _activeSaveName = name;
        _activeBuiltin = false;
        _currentScenarioName = name;
        RememberSavedState();
        _homePage?.RefreshEntries();
        FloatingText.Show("已保存：" + name);
        return true;
    }

    public string SuggestedSaveName =>
        _activeBuiltin ? _currentScenarioName + " 副本" : _currentScenarioName;

    public void LoadSaveFile(string path) {
        SandboxSaveData data = SandboxSaveSystem.Load(path);
        if (data == null) {
            FloatingText.Show("存档读取失败");
            return;
        }
        if (!ApplySaveData(data)) {
            FloatingText.Show("存档损坏，已恢复默认场景");
            LoadDefaultScenario(SandboxSaveSystem.BuiltinAquarius);
            return;
        }
        FinishScenarioLoad();
        _activeSavePath = path;
        _activeSaveName = data.name;
        _activeBuiltin = false;
        _currentScenarioName = data.name;
        RememberSavedState();
    }

    public void LoadDefaultScenario(string id, bool finishLoad = true) {
        SandboxTimeManager.Instance?.ResetClock();
        ScenarioLoadCount++;
        if (id == SandboxSaveSystem.BuiltinSolar) LoadSolarScenario();
        else if (id == SandboxSaveSystem.BuiltinAntares) LoadAntaresScenario();
        else if (id == SandboxSaveSystem.BuiltinMoonsEarth) LoadMoonScenario("地球", "地球的主要卫星");
        else if (id == SandboxSaveSystem.BuiltinMoonsJupiter) LoadMoonScenario("木星", "木星的主要卫星");
        else if (id == SandboxSaveSystem.BuiltinMoonsSaturn) LoadMoonScenario("土星", "土星的主要卫星");
        else if (id == SandboxSaveSystem.BuiltinMoonsUranus) LoadMoonScenario("天王星", "天王星的主要卫星");
        else if (id == SandboxSaveSystem.BuiltinMoonsNeptune) LoadMoonScenario("海王星", "海王星的主要卫星");
        else LoadAquariusScenario();
        if (finishLoad) FinishScenarioLoad();
        _activeSavePath = null;
        _activeSaveName = _currentScenarioName;
        _activeBuiltin = true;
        RememberSavedState();
        ShowSceneDescription(id);
    }

    public void NewBlankScenario() {
        _sceneDescription?.Hide();
        ClearSandboxBodies();
        // 空场景从原点重建，避免沿用了上一次跟随天体留下的浮动原点。
        SandboxFloatingOrigin.Reset();
        _templatePosition = Vector3.zero;
        if (_placementPivot != null) _placementPivot.transform.position = Vector3.zero;
        SandboxTimeManager.Instance?.ResetClock();
        _currentScenarioName = "新场景";
        _activeSavePath = null;
        _activeSaveName = _currentScenarioName;
        _activeBuiltin = false;
        SetCreatePanelOpen(false);
        if (_parameterPanel != null) _parameterPanel.SetActive(false);
        if (_planetParameterPanel != null) _planetParameterPanel.SetActive(false);
        _homePage?.Hide();
        RememberSavedState();
    }

    private const double MetersPerUnit = CelestialScale.KmPerUnit * 1000.0;

    /// <summary>改质量变成气态巨行星时随机挑一张贴图。</summary>
    private static readonly string[] RandomGasGiantTextures = {
        "Textures/Planets/Random_GasGiant01",
        "Textures/Planets/Random_GasGiant02",
        "Textures/Planets/Random_GasGiant03",
        "Textures/Planets/Random_GasGiant04"
    };

    private static string RandomGasGiantTexture() {
        return RandomGasGiantTextures[UnityEngine.Random.Range(0, RandomGasGiantTextures.Length)];
    }

    private static bool IsFinite(SandboxDVec3 v) {
        return SandboxSaveSystem.IsFinite(v.x) &&
               SandboxSaveSystem.IsFinite(v.y) &&
               SandboxSaveSystem.IsFinite(v.z);
    }

    private static double ClampSpeed(double speed) {
        if (double.IsNaN(speed) || double.IsInfinity(speed)) return 2.0;
        return Math.Clamp(speed, 0.0, 1e6);
    }

    /// <summary>相对坐标的原点取最重的恒星；没有恒星就退而取最重的天体。</summary>
    private static SandboxPhysicsBody PickSaveAnchor(List<SandboxPhysicsBody> bodies) {
        SandboxPhysicsBody bestStar = null;
        SandboxPhysicsBody bestBody = null;

        for (int i = 0; i < bodies.Count; i++) {
            SandboxPhysicsBody body = bodies[i];
            if (body == null) continue;
            if (bestBody == null || body.massKg > bestBody.massKg) bestBody = body;
            if (!body.IsStar) continue;
            if (bestStar == null || body.massKg > bestStar.massKg) bestStar = body;
        }
        return bestStar ?? bestBody;
    }

    /// <summary>v1 存档用：从旧的绝对坐标里找出主星。</summary>
    private static SandboxSaveBody PickAnchorSave(List<SandboxSaveBody> bodies) {
        SandboxSaveBody bestStar = null;
        SandboxSaveBody bestBody = null;

        foreach (SandboxSaveBody save in bodies) {
            if (save == null) continue;
            if (bestBody == null || save.mass > bestBody.mass) bestBody = save;
            if (save.kind != "star") continue;
            if (bestStar == null || save.mass > bestStar.mass) bestStar = save;
        }
        return bestStar ?? bestBody;
    }

    private SandboxSaveData BuildSaveData(string name) {
        SandboxTimeManager time = SandboxTimeManager.Instance;
        SandboxSaveData data = new() {
            name = name,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            timeSpeedValue = time != null ? ClampSpeed(time.SpeedValue) : 2.0,
            timeUnit = time != null ? time.Unit.ToString() : "HoursPerSecond"
        };

        List<SandboxPhysicsBody> savable = new();
        foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
            if (body == null || !body.alive) continue;
            if (body.fragment != null) continue;                 // 撞击碎片不写进存档
            if (body.star == null && body.planet == null) continue;
            if (!IsFinite(body.positionM) || !IsFinite(body.velocityMps)) {
                Debug.LogWarning("[Save] 跳过数值异常的天体：" + body.name);
                continue;
            }
            savable.Add(body);
        }
        if (savable.Count == 0) return data;

        // v2 的关键：坐标/速度都相对主星保存，存档和浮动原点无关。
        SandboxPhysicsBody anchor = PickSaveAnchor(savable);
        if (anchor == null) return data;

        data.anchorName = anchor.name;
        data.anchorX = anchor.positionM.x;
        data.anchorY = anchor.positionM.y;
        data.anchorZ = anchor.positionM.z;
        data.anchorVx = anchor.velocityMps.x;
        data.anchorVy = anchor.velocityMps.y;
        data.anchorVz = anchor.velocityMps.z;

        foreach (SandboxPhysicsBody body in savable) {
            SandboxDVec3 rel = body.positionM - anchor.positionM;
            SandboxDVec3 relVelocity = body.velocityMps - anchor.velocityMps;
            if (!IsFinite(rel) || !IsFinite(relVelocity)) {
                Debug.LogWarning("[Save] 跳过相对坐标异常的天体：" + body.name);
                continue;
            }

            SandboxSaveBody save = new() {
                kind = body.IsStar ? "star" : "planet",
                name = body.name,
                positionX = rel.x,
                positionY = rel.y,
                positionZ = rel.z,
                velocityX = relVelocity.x,
                velocityY = relVelocity.y,
                velocityZ = relVelocity.z,
                orbitInclinationDeg = SandboxSaveSystem.Sanitize(body.orbitInclinationDeg),
                orbitTargetName = body.orbitTarget != null ? body.orbitTarget.name : "",
                isOrbiting = body.isOrbiting && body.orbitTarget != null
            };

            if (body.star != null) {
                SandboxStar star = body.star;
                save.mass = star.mass;
                save.metallicity = star.metallicity;
                save.ageMyr = star.ageMyr;
                save.rotationPeriodDays = star.rotationPeriodDays;
                save.temperatureK = body.temperatureK;
                if (star.snapshot != null) {
                    save.radius = star.snapshot.radiusRsun;
                    save.luminosityLsun = star.snapshot.luminosityLsun;
                }
            } else if (body.planet != null) {
                SandboxPlanet planet = body.planet;
                save.mass = planet.massEarth;
                save.radius = planet.radiusEarth;
                save.temperatureK = planet.temperatureK;
                save.rotationPeriodDays = planet.rotationPeriodDays;
                save.rotationDirection = planet.rotationDirection;
                save.axialTiltDeg = planet.axialTiltDeg;
                save.greenhouseFactor = planet.greenhouseFactor;
                save.atmosphereMassEarth = planet.atmosphereMassEarth;
                save.trailColorR = planet.trailColor.r;
                save.trailColorG = planet.trailColor.g;
                save.trailColorB = planet.trailColor.b;
                if (planet.snapshot != null) {
                    save.surfaceTexturePath = planet.snapshot.surfaceTexturePath;
                    save.atmosphereTexturePath = planet.snapshot.atmosphereTexturePath;
                    save.atmosphereShell = planet.snapshot.atmosphereShell;
                    save.isBox = planet.snapshot.isBox;
                    save.boxXKm = planet.snapshot.boxXKm;
                    save.boxYKm = planet.snapshot.boxYKm;
                    save.boxZKm = planet.snapshot.boxZKm;
                    save.textureFront = planet.snapshot.textureFront;
                    save.textureSides = planet.snapshot.textureSides;
                    save.textureTopBackBottom = planet.snapshot.textureTopBackBottom;
                    save.ignoreGravity = planet.snapshot.ignoreGravity;
                    save.ignoreCollision = planet.snapshot.ignoreCollision;
                    save.ignoreThermal = planet.snapshot.ignoreThermal;
                    save.impactGiftSun = planet.snapshot.impactGiftSun;
                    save.heightMapPath = planet.snapshot.heightMapPath;
                    save.surfaceWaterDepthM = planet.snapshot.surfaceWaterDepthM;
                    save.landHeightMaxM = planet.snapshot.landHeightMaxM;
                    save.soluteConcentrationMmolL = planet.snapshot.soluteConcentrationMmolL;
                    save.atmosphereColorR = planet.snapshot.atmosphereColor.r;
                    save.atmosphereColorG = planet.snapshot.atmosphereColor.g;
                    save.atmosphereColorB = planet.snapshot.atmosphereColor.b;
                }
            }
            data.bodies.Add(save);
        }
        return data;
    }

    /// <summary>
    /// 还原存档。整个过程包在 try/catch 里：任何一步失败都返回 false，
    /// 由调用方回退到默认场景，避免留下“半场景 + 每帧异常”的状态。
    /// </summary>
    private bool ApplySaveData(SandboxSaveData data) {
        if (data == null || data.bodies == null || data.bodies.Count == 0) {
            FloatingText.Show("存档为空");
            return false;
        }

        try {
            ClearSandboxBodies();
            // 读档一律从原点重建：存档里存的是相对主星的坐标，
            // 这样就不会被“上次跟随过某颗行星”留下的浮动原点带偏。
            SandboxFloatingOrigin.Reset();

            _currentScenarioName = data.name;
            _homePage?.Hide();
            SetCreatePanelOpen(false);
            if (_parameterPanel != null) _parameterPanel.SetActive(false);
            if (_planetParameterPanel != null) _planetParameterPanel.SetActive(false);

            bool relativeCoordinates = data.version >= 2;

            // v1 老存档是绝对坐标：折成相对坐标再处理，逻辑与 v2 一致。
            SandboxDVec3 offset = SandboxDVec3.Zero;
            SandboxDVec3 velocityOffset = SandboxDVec3.Zero;
            if (!relativeCoordinates) {
                SandboxSaveBody anchorSave = PickAnchorSave(data.bodies);
                if (anchorSave != null) {
                    offset = new SandboxDVec3(anchorSave.positionX, anchorSave.positionY,
                        anchorSave.positionZ);
                    velocityOffset = new SandboxDVec3(anchorSave.velocityX,
                        anchorSave.velocityY, anchorSave.velocityZ);
                }
            }

            List<SandboxPhysicsBody> created = new(data.bodies.Count);
            foreach (SandboxSaveBody save in data.bodies) {
                if (save == null) {
                    created.Add(null);
                    continue;
                }
                created.Add(save.kind == "star"
                    ? CreateStarFromSave(save)
                    : CreatePlanetFromSave(save));
            }

            Dictionary<string, SandboxPhysicsBody> byName = new();
            foreach (SandboxPhysicsBody body in created) {
                if (body == null || !body.alive) continue;
                if (!byName.ContainsKey(body.name)) byName[body.name] = body;
            }

            for (int i = 0; i < data.bodies.Count; i++) {
                SandboxSaveBody save = data.bodies[i];
                SandboxPhysicsBody body = i < created.Count ? created[i] : null;
                if (save == null || body == null || !body.alive) continue;

                SandboxDVec3 position = new(save.positionX, save.positionY, save.positionZ);
                SandboxDVec3 velocity = new(save.velocityX, save.velocityY, save.velocityZ);
                if (!relativeCoordinates) {
                    position = position - offset;
                    velocity = velocity - velocityOffset;
                }
                if (!IsFinite(position) || !IsFinite(velocity)) continue;

                body.positionM = position;
                body.velocityMps = velocity;
                body.orbitInclinationDeg = SandboxSaveSystem.Sanitize(save.orbitInclinationDeg);
                body.orbitRadiusM = 0.0;
                body.orbitTarget = null;
                body.isOrbiting = false;

                if (body.star != null) {
                    body.star.SetLogicalPosition(position.x / MetersPerUnit,
                        position.y / MetersPerUnit, position.z / MetersPerUnit);
                } else if (body.planet != null) {
                    body.planet.SetLogicalPosition(position.x / MetersPerUnit,
                        position.y / MetersPerUnit, position.z / MetersPerUnit);
                }
            }

            // 轨道关系要等所有天体的位置都落定后再连，避免拿到旧坐标。
            for (int i = 0; i < data.bodies.Count; i++) {
                SandboxSaveBody save = data.bodies[i];
                SandboxPhysicsBody body = i < created.Count ? created[i] : null;
                if (save == null || body == null || !body.alive) continue;
                if (!save.isOrbiting || string.IsNullOrEmpty(save.orbitTargetName)) continue;
                if (!byName.TryGetValue(save.orbitTargetName, out SandboxPhysicsBody target)) continue;
                if (target == body) continue;

                body.orbitTarget = target;
                body.isOrbiting = true;
                body.orbitRadiusM = (body.positionM - target.positionM).Magnitude;
            }

            if (SandboxPhysicsSystem.Bodies.Count == 0) {
                Debug.LogError("[Save] 存档里没有任何可以恢复的天体");
                return false;
            }

            if (SandboxTimeManager.Instance != null) {
                if (Enum.TryParse(data.timeUnit, out SandboxTimeUnit unit)) {
                    SandboxTimeManager.Instance.SetUnit(unit);
                }
                SandboxTimeManager.Instance.SetSpeed(ClampSpeed(data.timeSpeedValue));
                SandboxTimeManager.Instance.ResetClock();
            }

            return true;
        }
        catch (Exception e) {
            Debug.LogError("[Save] 读档失败：" + e);
            return false;
        }
    }

    private SandboxPhysicsBody CreateStarFromSave(SandboxSaveBody save) {
        StarData data = new() {
            name = save.name,
            mass = SandboxSaveSystem.Sanitize(save.mass),
            metallicity = SandboxSaveSystem.Sanitize(save.metallicity),
            age = SandboxSaveSystem.Sanitize(save.ageMyr),
            rotationPeriodDays = SandboxSaveSystem.Sanitize(save.rotationPeriodDays, 25.38)
        };
        if (data.mass <= 0.0) {
            Debug.LogWarning("[Save] 跳过质量无效的恒星：" + save.name);
            return null;
        }
        if (!SandboxStarModel.TryCompute(data.mass, data.metallicity, data.age,
                data.rotationPeriodDays, out SandboxStarSnapshot snapshot, out string error)) {
            Debug.LogWarning("[Save] 恒星恢复失败：" + error);
            return null;
        }

        int before = _stars.Count;
        CreateStar(data, snapshot, Vector3.zero);
        if (_stars.Count <= before) return null;
        return SandboxPhysicsSystem.GetBody(_stars[_stars.Count - 1]);
    }

    private SandboxPhysicsBody CreatePlanetFromSave(SandboxSaveBody save) {
        PlanetData data = new() {
            name = save.name,
            mass = SandboxSaveSystem.Sanitize(save.mass),
            radius = SandboxSaveSystem.Sanitize(save.radius),
            atmosphereMass = SandboxSaveSystem.Sanitize(save.atmosphereMassEarth),
            temperature = SandboxSaveSystem.Sanitize(save.temperatureK, 288.0),
            rotationPeriodDays = SandboxSaveSystem.Sanitize(save.rotationPeriodDays, 1.0),
            rotationDirection = save.rotationDirection == 0 ? 1 : save.rotationDirection,
            axialTiltDeg = SandboxSaveSystem.Sanitize(save.axialTiltDeg),
            greenhouseFactor = SandboxSaveSystem.Sanitize(save.greenhouseFactor),
            heightMap = save.heightMapPath,
            surfaceWaterDepthM = SandboxSaveSystem.Sanitize(save.surfaceWaterDepthM),
            landHeightMaxM = save.landHeightMaxM > 0.0 ? save.landHeightMaxM : 20000.0,
            soluteConcentrationMmolL = SandboxSaveSystem.Sanitize(save.soluteConcentrationMmolL),
            atmosphereColorR = save.atmosphereColorR > 0f ? save.atmosphereColorR : 0.55f,
            atmosphereColorG = save.atmosphereColorG > 0f ? save.atmosphereColorG : 0.75f,
            atmosphereColorB = save.atmosphereColorB > 0f ? save.atmosphereColorB : 1f,
            trailColorR = save.trailColorR + save.trailColorG + save.trailColorB > 0f
                ? save.trailColorR : 200f / 255f,
            trailColorG = save.trailColorR + save.trailColorG + save.trailColorB > 0f
                ? save.trailColorG : 200f / 255f,
            trailColorB = save.trailColorR + save.trailColorG + save.trailColorB > 0f
                ? save.trailColorB : 200f / 255f,
            surface = new PlanetLayer { texture = save.surfaceTexturePath },
            atmosphere = new PlanetLayer { texture = save.atmosphereTexturePath },
            atmosphereShell = save.atmosphereShell,
            isBox = save.isBox,
            boxXKm = save.boxXKm,
            boxYKm = save.boxYKm,
            boxZKm = save.boxZKm,
            textureFront = save.textureFront,
            textureSides = save.textureSides,
            textureTopBackBottom = save.textureTopBackBottom,
            ignoreGravity = save.ignoreGravity,
            ignoreCollision = save.ignoreCollision,
            ignoreThermal = save.ignoreThermal,
            impactGiftSun = save.impactGiftSun
        };
        if (data.mass <= 0.0 || data.radius <= 0.0) {
            Debug.LogWarning("[Save] 跳过质量/半径无效的行星：" + save.name);
            return null;
        }
        if (!SandboxPlanetModel.TryCreate(data, out SandboxPlanetSnapshot snapshot,
                out string error)) {
            Debug.LogWarning("[Save] 行星恢复失败：" + error);
            return null;
        }

        int before = _planets.Count;
        CreatePlanet(data, snapshot, Vector3.zero);
        if (_planets.Count <= before) return null;
        return SandboxPhysicsSystem.GetBody(_planets[_planets.Count - 1]);
    }

    private void ClearSandboxBodies() {
        CancelPlacement();
        // 切场景/读档时把放置模式收回默认值：否则如果上一场停在“发射”模式，
        // 新场景里 CreateStar/CreatePlanet 会按发射速度给它们一个初速度，
        // 看起来就是“整个存档的东西一起飞出去”。
        _placementMotion = PlacementMotion.Orbiting;
        DeselectStar();
        foreach (SandboxStar star in _stars) {
            if (star != null) Destroy(star.gameObject);
        }
        _stars.Clear();
        RefreshFallbackLight();
        foreach (SandboxPlanet planet in _planets) {
            if (planet != null) Destroy(planet.gameObject);
        }
        _planets.Clear();
        foreach (BodyLabel label in _bodyLabels) {
            if (label != null && label.text != null) Destroy(label.text.gameObject);
        }
        _bodyLabels.Clear();
        // 撞击产生的碎片只存在于物理层，不在 _stars/_planets 里；
        // 不在这里销毁的话，切换场景后上一场的碎块会残留在新场景里。
        foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
            if (body != null && body.fragment != null) {
                Destroy(body.fragment.gameObject);
            }
        }
        SandboxPhysicsSystem.Reset();
        if (_orbitCamera != null && _placementPivot != null) {
            _orbitCamera.target = _placementPivot.transform;
        }
    }

    /// <summary>
    /// 场景载入收尾：把浮动原点搬回主恒星，并把相机拉到恒星外侧。
    ///
    /// 上一次游玩如果双击跟随过某颗行星，SandboxFloatingOrigin 会停在几 AU 之外；
    /// 不归位的话，新场景的天体相对位置虽然对，渲染坐标/放置平面/放置点却仍然
    /// 以旧原点为基准，看起来就像“轨道乱掉”。相机如果还停在上一颗小天体的
    /// 距离上，也会直接钻进恒星内部。
    /// </summary>
    private void FinishScenarioLoad() {
        SandboxPhysicsBody primary = null;
        foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
            if (body == null || !body.alive || !body.IsStar) continue;
            primary = body;
            break;
        }
        // 卫星系没有恒星：改用质量最大的天体当观察中心，否则镜头距离永远不生效。
        if (primary == null) {
            foreach (SandboxPhysicsBody body in SandboxPhysicsSystem.Bodies) {
                if (body == null || !body.alive) continue;
                if (primary == null || body.massKg > primary.massKg) primary = body;
            }
        }
        if (primary == null) return;

        if (primary.star != null) {
            RebaseTo(primary.star.logicalX, primary.star.logicalY, primary.star.logicalZ);
        }
        if (_orbitCamera == null || _placementPivot == null) return;

        double primaryDiameter = primary.star != null
            ? primary.star.VisualDiameter
            : (primary.planet != null ? primary.planet.VisualDiameter : 1.0);
        double starDiameter = System.Math.Max(primaryDiameter, 1e-6);
        float distance = Mathf.Max(_orbitCamera.distance,
            (float)(starDiameter * 4.0));

        // 每个星系有自己的默认观察距离（太阳系 3AU / 水瓶座一 5AU / 心大星 1200AU，
        // 卫星系则是固定的 km 值）。
        if (_startDistanceAu > 0.0) {
            distance = (float)(CelestialScale.KmToUnits(_startDistanceAu * CelestialScale.AuKm));
            _startDistanceAu = 0.0;
        }
        if (_startDistanceKm > 0.0) {
            distance = (float)(CelestialScale.KmToUnits(_startDistanceKm));
            _startDistanceKm = 0.0;
        }
        _orbitCamera.Focus(_placementPivot.transform, distance);
    }

    private void LoadAquariusScenario() {
        _startDistanceAu = 5.0;
        ClearSandboxBodies();
        StarData starData = null;
        if (_catalog?.stars != null) {
            foreach (StarData s in _catalog.stars) {
                if (s != null && s.name == "水瓶座一") { starData = s; break; }
            }
        }
        starData ??= new StarData {
            name = "水瓶座一", mass = 1.55, metallicity = 0.02, age = 621.6,
            rotationPeriodDays = 25.38
        };

        if (!SandboxStarModel.TryCompute(starData.mass, starData.metallicity,
                starData.age, starData.rotationPeriodDays,
                out SandboxStarSnapshot starSnapshot, out string error)) {
            FloatingText.Show("水瓶座一初始化失败：" + error);
            return;
        }
        CreateStar(starData, starSnapshot, Vector3.zero);
        SandboxPhysicsBody starBody = SandboxPhysicsSystem.GetBody(_stars[_stars.Count - 1]);

        double starMassKg = starSnapshot.mass * SandboxPhysicsSystem.SunMassKg;
        double luminosityW = starSnapshot.luminosityLsun * SandboxStarModel.SunLuminosityW;

        AddAquariusPlanet("水瓶座一b", "Aquarius_One_b", 9431.0 / 6371.0, 0.0,
            261.15, 1.57, 3.01, 0.024, 0.0, 25.2, 17.3, starMassKg, luminosityW, starBody);
        AddAquariusPlanet("水瓶座一c", "Aquarius_One_c", 0.714, 0.302,
            237.45, 14.4, 5.07, 0.016, 1.9, 74.7, 6.2, starMassKg, luminosityW, starBody);
        AddAquariusPlanet("水瓶座一d", "Aquarius_One_d", 1.45 * 10.973, 2740.0,
            81.15, 4.24, 81.8, 0.534, 20.1, 0.0, 46.2, starMassKg, luminosityW, starBody);

        _currentScenarioName = "水瓶座一系";
        SetTimeSpeed(35.0, SandboxTimeUnit.DaysPerSecond);
        SandboxSaveSystem.SetBuiltinThumbnail(SandboxSaveSystem.BuiltinAquarius,
            SandboxSaveSystem.CaptureThumbnail());
    }

    private void AddAquariusPlanet(string name, string texture, double radiusEarth,
            double massEarth, double temperatureK, double rotationDays,
            double semiMajorAu, double eccentricity, double inclinationDeg,
            double pressureKpa, double tiltDeg, double starMassKg, double luminosityW,
            SandboxPhysicsBody starBody) {
        if (massEarth <= 0.0) {
            massEarth = 6.27 / SandboxPlanetModel.EarthDensityGcm3 *
                        radiusEarth * radiusEarth * radiusEarth;
        }

        double r2 = radiusEarth * radiusEarth;
        double pressureAtm = pressureKpa / 101.325;
        double atmosphereMass = pressureAtm <= 0.0
            ? 0.0
            : pressureAtm * r2 * r2 / massEarth;

        double radiusM = radiusEarth * SandboxPlanetModel.EarthRadiusKm * 1000.0;
        double thermal = SandboxPhysicsSystem.StefanBoltzmann *
                         Math.Pow(temperatureK, 4.0) * 4.0 * Math.PI * radiusM * radiusM;
        double distanceM = semiMajorAu * (1.0 + eccentricity) *
                           CelestialScale.AuKm * 1000.0;
        double absorbed = luminosityW * radiusM * radiusM /
                          (4.0 * Math.Max(distanceM * distanceM, 1.0));
        double greenhouse = Math.Clamp(thermal > 0.0 ? 1.0 - absorbed / thermal : 0.0,
            0.0, 1.0);

        PlanetData data = new() {
            name = name,
            mass = massEarth,
            radius = radiusEarth,
            atmosphereMass = atmosphereMass,
            temperature = temperatureK,
            rotationPeriodDays = rotationDays,
            rotationDirection = 1,
            axialTiltDeg = tiltDeg,
            greenhouseFactor = greenhouse,
            heightMap = name.EndsWith("b")
                ? "Textures/Planets/Aquarius_One_b_Heightmap" : string.Empty,
            surfaceWaterDepthM = name.EndsWith("b") ? 2000.0 : 0.0,
            landHeightMaxM = name.EndsWith("b") ? 16000.0 : 20000.0,
            soluteConcentrationMmolL = name.EndsWith("b") ? 4100.0 : 0.0,
            atmosphereColorR = name.EndsWith("b") ? 0.2f : 0.1f,
            atmosphereColorG = name.EndsWith("b") ? 0.9f : 0.2f,
            atmosphereColorB = name.EndsWith("b") ? 1f : 0.55f,
            trailColorR = name.EndsWith("b") ? 0.7843137f
                : name.EndsWith("c") ? 0.1960784f : 1f,
            trailColorG = name.EndsWith("b") ? 1f
                : name.EndsWith("c") ? 0.1960784f : 0.5019608f,
            trailColorB = name.EndsWith("b") ? 0.7843137f
                : name.EndsWith("c") ? 0.3921569f : 0f,
            surface = new PlanetLayer { texture = "Textures/Planets/" + texture },
            // 水瓶座一b 借用地球的云层（带 alpha 的云图）+ 外圈光晕。
            atmosphere = new PlanetLayer {
                texture = name.EndsWith("b")
                    ? "Textures/Planets/Earth_Atmosphere" : string.Empty
            },
            atmosphereShell = name.EndsWith("b")
        };

        if (!SandboxPlanetModel.TryCreate(data, out SandboxPlanetSnapshot snapshot,
                out string error)) {
            Debug.LogWarning("[Scenario] " + name + " 创建失败：" + error);
            return;
        }

        CreatePlanet(data, snapshot, Vector3.zero);
        SandboxPlanet planet = _planets[_planets.Count - 1];
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(planet);
        if (body == null) return;

        double mu = SandboxPhysicsSystem.GravitationalConstant *
                    (starMassKg + massEarth * SandboxPhysicsSystem.EarthMassKg);
        double r = semiMajorAu * (1.0 + eccentricity) * CelestialScale.AuKm * 1000.0;
        double speed = Math.Sqrt(mu * (1.0 - eccentricity) /
                                 Math.Max(semiMajorAu * CelestialScale.AuKm * 1000.0 *
                                          (1.0 + eccentricity), 1.0));
        double inclination = inclinationDeg * Math.PI / 180.0;

        // 远日点位置：距离 r，绕 x 轴倾斜 inclination。
        // 必须相对恒星当前位置摆放：浮动原点可能停在上一次跟随的天体上，
        // 直接写绝对坐标 (0, r…) 会把行星放到离恒星几 AU 以外，轨道就乱了。
        SandboxDVec3 starPosition = starBody != null
            ? starBody.positionM : SandboxDVec3.Zero;
        body.positionM = starPosition + new SandboxDVec3(0.0,
            r * Math.Sin(inclination), r * Math.Cos(inclination));
        body.velocityMps = new SandboxDVec3(-speed, 0.0, 0.0);
        body.orbitTarget = starBody;
        body.isOrbiting = true;
        body.orbitRadiusM = r;
        body.orbitInclinationDeg = inclinationDeg;
        planet.SetLogicalPosition(
            body.positionM.x / (CelestialScale.KmPerUnit * 1000.0),
            body.positionM.y / (CelestialScale.KmPerUnit * 1000.0),
            body.positionM.z / (CelestialScale.KmPerUnit * 1000.0));
    }

    /// <summary>心大星行星系：心大星A + 虚构行星心大星Ab（距离 1004.2 亿 km，e=0.057）。</summary>
    private void LoadAntaresScenario() {
        _startDistanceAu = 1200.0;
        ClearSandboxBodies();

        // 直接用恒星目录里的心宿二（Antares），不再自己造一颗主序星。
        StarData star = null;
        if (_catalog?.stars != null) {
            foreach (StarData s in _catalog.stars) {
                if (s != null && s.name == "心宿二") { star = s; break; }
            }
        }
        star ??= new StarData {
            name = "心宿二", mass = 17.2, metallicity = 0.018, age = 11.3,
            rotationPeriodDays = 1000.0
        };
        // 这个星系从 2017-11-25 00:00:00 开始计时。
        SandboxTimeManager.Instance?.ResetClock(new DateTime(2017, 11, 25, 0, 0, 0));
        if (!SandboxStarModel.TryCompute(star.mass, star.metallicity, star.age,
                star.rotationPeriodDays, out SandboxStarSnapshot starSnapshot, out string error)) {
            FloatingText.Show("心大星A 初始化失败：" + error);
            return;
        }
        CreateStar(star, starSnapshot, Vector3.zero);
        SandboxPhysicsBody starBody = SandboxPhysicsSystem.GetBody(_stars[_stars.Count - 1]);

        PlanetData ab = null;
        if (_planetCatalog?.planets != null) {
            foreach (PlanetData p in _planetCatalog.planets) {
                if (p != null && p.name == "心大星Ab") { ab = p; break; }
            }
        }
        if (ab != null) {
            const double distanceKm = 1004.2e8;   // 1004.2 亿 km
            const double eccentricity = 0.057;
            // 初始放在远日点：a(1+e) = 距离。
            double semiMajorAu = distanceKm / CelestialScale.AuKm / (1.0 + eccentricity);
            AddSolarPlanet(ClonePlanetData(ab), starSnapshot, starBody,
                semiMajorAu, eccentricity, 0.0, 0.0, 0.0);
        }

        _currentScenarioName = "心大星行星系";
        SetTimeSpeed(30.0, SandboxTimeUnit.MinutesPerSecond);
    }

    /// <summary>某颗行星的“主要卫星”场景：行星放在原点，卫星按真实轨道半径圆轨道摆放。</summary>
    private void LoadMoonScenario(string hostPlanetName, string scenarioName) {
        // 各大卫星系的默认镜头距离。
        _startDistanceKm = hostPlanetName switch {
            "地球" => 1_000_000.0,
            "木星" => 5_000_000.0,
            "土星" => 10_000_000.0,
            "天王星" => 2_000_000.0,
            "海王星" => 1_000_000.0,
            _ => 0.0,
        };
        ClearSandboxBodies();

        PlanetData host = null;
        if (_planetCatalog?.planets != null) {
            foreach (PlanetData p in _planetCatalog.planets) {
                if (p != null && p.name == hostPlanetName) { host = p; break; }
            }
        }
        if (host == null) {
            FloatingText.Show("找不到行星：" + hostPlanetName);
            return;
        }

        if (!SandboxPlanetModel.TryCreate(ClonePlanetData(host),
                out SandboxPlanetSnapshot hostSnapshot, out string error)) {
            FloatingText.Show(hostPlanetName + " 初始化失败：" + error);
            return;
        }
        CreatePlanet(ClonePlanetData(host), hostSnapshot, Vector3.zero);
        SandboxPlanet hostPlanet = _planets[_planets.Count - 1];
        SandboxPhysicsBody hostBody = SandboxPhysicsSystem.GetBody(hostPlanet);

        if (_moonCatalog?.planets != null && hostBody != null) {
            foreach (PlanetData moon in _moonCatalog.planets) {
                if (moon == null || MoonHost(moon.name) != hostPlanetName) continue;
                PlaceMoon(moon, hostBody);
            }
        }

        _currentScenarioName = scenarioName;
        SetTimeSpeed(1.0, SandboxTimeUnit.DaysPerSecond);
    }

    /// <summary>卫星名字前缀 → 主星。</summary>
    private static string MoonHost(string moonName) {
        if (moonName == "月球") return "地球";
        if (moonName.StartsWith("木卫")) return "木星";
        if (moonName.StartsWith("土卫")) return "土星";
        if (moonName.StartsWith("天卫")) return "天王星";
        if (moonName.StartsWith("海卫")) return "海王星";
        if (moonName.StartsWith("冥卫")) return "冥王星";
        return null;
    }

    private void PlaceMoon(PlanetData moon, SandboxPhysicsBody hostBody) {
        if (!SandboxPlanetModel.TryCreate(ClonePlanetData(moon),
                out SandboxPlanetSnapshot snapshot, out string error)) {
            Debug.LogWarning("[Scenario] " + moon.name + " 创建失败：" + error);
            return;
        }
        CreatePlanet(ClonePlanetData(moon), snapshot, Vector3.zero);
        SandboxPlanet planet = _planets[_planets.Count - 1];
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(planet);
        if (body == null) return;

        double radiusKm = moon.orbitRadiusKm > 0.0 ? moon.orbitRadiusKm : 384400.0;
        double radiusM = radiusKm * 1000.0;
        double mu = SandboxPhysicsSystem.GravitationalConstant *
                    (hostBody.massKg + body.massKg);
        double speed = Math.Sqrt(mu / Math.Max(radiusM, 1.0));

        // 沿 +Z 摆放，速度沿 +X（绕 +Y 公转），黄道面内。
        body.positionM = hostBody.positionM + new SandboxDVec3(0.0, 0.0, radiusM);
        body.velocityMps = new SandboxDVec3(speed, 0.0, 0.0);
        body.orbitTarget = hostBody;
        body.isOrbiting = true;
        body.orbitRadiusM = radiusM;
        body.orbitInclinationDeg = 0.0;
        planet.SetLogicalPosition(
            body.positionM.x / (CelestialScale.KmPerUnit * 1000.0),
            body.positionM.y / (CelestialScale.KmPerUnit * 1000.0),
            body.positionM.z / (CelestialScale.KmPerUnit * 1000.0));
    }

    private void LoadSolarScenario() {
        _startDistanceAu = 3.0;
        ClearSandboxBodies();
        StarData sun = null;
        if (_catalog?.stars != null) {
            foreach (StarData s in _catalog.stars) {
                if (s != null && s.name == "太阳") { sun = s; break; }
            }
        }
        sun ??= new StarData {
            name = "太阳", mass = 1.0, metallicity = 0.018, age = 4450,
            rotationPeriodDays = 25.38
        };
        if (!SandboxStarModel.TryCompute(sun.mass, sun.metallicity, sun.age,
                sun.rotationPeriodDays, out SandboxStarSnapshot sunSnapshot, out string error)) {
            FloatingText.Show("太阳初始化失败：" + error);
            return;
        }
        CreateStar(sun, sunSnapshot, Vector3.zero);
        SandboxPhysicsBody sunBody = SandboxPhysicsSystem.GetBody(_stars[_stars.Count - 1]);

        // J2000 轨道根数（a: AU, e, i: °, Ω 升交点经度, ϖ 近日点经度）
        double[] a = { 0.38709927, 0.72333566, 1.00000261, 1.52371034, 5.20288700,
                       9.53667594, 19.18916464, 30.06992276 };
        double[] e = { 0.20563593, 0.00677672, 0.01671123, 0.09339410, 0.04838624,
                       0.05386179, 0.04725744, 0.00859048 };
        double[] inc = { 7.00497902, 3.39467605, -0.00001531, 1.84969142, 1.30439695,
                         2.48599187, 0.77263783, 1.77004347 };
        double[] node = { 48.33076593, 76.67984255, 0.0, 49.55953891, 100.47390909,
                          113.66242448, 74.01692503, 131.78422574 };
        double[] peri = { 77.45779628, 131.60246718, 102.93768193, -23.94362959,
                          14.72847983, 92.59887831, 170.95427630, 44.96476227 };

        if (_planetCatalog?.planets != null) {
            int index = 0;
            foreach (PlanetData source in _planetCatalog.planets) {
                if (source == null || index >= a.Length) break;
                PlanetData data = new() {
                    name = source.name,
                    mass = source.mass,
                    radius = source.radius,
                    atmosphereMass = source.atmosphereMass,
                    temperature = source.temperature,
                    rotationPeriodDays = source.rotationPeriodDays,
                    rotationDirection = source.rotationDirection,
                    axialTiltDeg = source.axialTiltDeg,
                    atmosphereShell = source.atmosphereShell,
                    greenhouseFactor = source.greenhouseFactor,
                    heightMap = source.heightMap,
                    surfaceWaterDepthM = source.surfaceWaterDepthM,
                    landHeightMaxM = source.landHeightMaxM,
                    soluteConcentrationMmolL = source.soluteConcentrationMmolL,
                    atmosphereColorR = source.atmosphereColorR,
                    atmosphereColorG = source.atmosphereColorG,
                    atmosphereColorB = source.atmosphereColorB,
                    trailColorR = source.trailColorR,
                    trailColorG = source.trailColorG,
                    trailColorB = source.trailColorB,
                    surface = source.surface,
                    atmosphere = source.atmosphere
                };
                AddSolarPlanet(data, sunSnapshot, sunBody, a[index], e[index], inc[index],
                    node[index], peri[index]);
                index++;
            }
        }
        // 矮行星 / 小行星：轨道参数用真实值，同样从远日点出发。
        // 谷神星、冥王星、亡神星、创神星、阋神星、赛德娜、鸟神星、共工星
        double[] da = { 2.7678, 39.4821, 39.174, 43.694, 67.864, 506.0, 45.430, 67.38 };
        double[] de = { 0.0785, 0.24883, 0.2270, 0.0397, 0.4417, 0.8496, 0.159, 0.503 };
        double[] di = { 10.587, 17.140, 20.593, 7.990, 44.04, 11.93, 29.0, 30.70 };
        double[] dNode = { 80.268, 110.304, 268.799, 188.80, 35.951, 144.25, 79.62, 336.83 };
        double[] dPeri = { 73.72, 224.13, 341.11, 335.8, 187.59, 95.54, 14.45, 183.8 };
        if (_dwarfCatalog?.planets != null) {
            int dwarfIndex = 0;
            foreach (PlanetData source in _dwarfCatalog.planets) {
                if (source == null || dwarfIndex >= da.Length) break;
                // 赛德娜初始放在近日点（远日点 900+ AU 太远）。
                bool atPerihelion = source.name == "赛德娜";
                AddSolarPlanet(ClonePlanetData(source), sunSnapshot, sunBody,
                    da[dwarfIndex], de[dwarfIndex], di[dwarfIndex],
                    dNode[dwarfIndex], dPeri[dwarfIndex], atPerihelion);
                dwarfIndex++;
            }
        }

        _currentScenarioName = "太阳系";
        SetTimeSpeed(20.0, SandboxTimeUnit.DaysPerSecond);
        SandboxSaveSystem.SetBuiltinThumbnail(SandboxSaveSystem.BuiltinSolar,
            SandboxSaveSystem.CaptureThumbnail());
    }

    private static PlanetData ClonePlanetData(PlanetData source) {
        if (source == null) return null;
        return new PlanetData {
            name = source.name,
            mass = source.mass,
            radius = source.radius,
            atmosphereMass = source.atmosphereMass,
            temperature = source.temperature,
            rotationPeriodDays = source.rotationPeriodDays,
            rotationDirection = source.rotationDirection,
            axialTiltDeg = source.axialTiltDeg,
            atmosphereShell = source.atmosphereShell,
            greenhouseFactor = source.greenhouseFactor,
            heightMap = source.heightMap,
            surfaceWaterDepthM = source.surfaceWaterDepthM,
            landHeightMaxM = source.landHeightMaxM,
            soluteConcentrationMmolL = source.soluteConcentrationMmolL,
            atmosphereColorR = source.atmosphereColorR,
            atmosphereColorG = source.atmosphereColorG,
            atmosphereColorB = source.atmosphereColorB,
            trailColorR = source.trailColorR,
            trailColorG = source.trailColorG,
            trailColorB = source.trailColorB,
            surface = source.surface,
            atmosphere = source.atmosphere
        };
    }

    private void AddSolarPlanet(PlanetData data, SandboxStarSnapshot sunSnapshot,
            SandboxPhysicsBody sunBody, double semiMajorAu, double eccentricity,
            double inclinationDeg, double nodeDeg = 0.0, double perihelionDeg = 0.0,
            bool startAtPerihelion = false) {
        if (!SandboxPlanetModel.TryCreate(data, out SandboxPlanetSnapshot snapshot,
                out string error)) {
            Debug.LogWarning("[Scenario] " + data.name + " 创建失败：" + error);
            return;
        }
        CreatePlanet(data, snapshot, Vector3.zero);
        SandboxPlanet planet = _planets[_planets.Count - 1];
        SandboxPhysicsBody body = SandboxPhysicsSystem.GetBody(planet);
        if (body == null) return;

        // 默认放在远日点；赛德娜这类极端偏心的天体可以指定放在近日点。
        double radiusFactor = startAtPerihelion
            ? (1.0 - eccentricity) : (1.0 + eccentricity);
        double distanceM = semiMajorAu * radiusFactor * CelestialScale.AuKm * 1000.0;
        double mu = SandboxPhysicsSystem.GravitationalConstant *
                    (sunSnapshot.mass * SandboxPhysicsSystem.SunMassKg +
                     data.mass * SandboxPhysicsSystem.EarthMassKg);
        double speed = Math.Sqrt(mu * (startAtPerihelion
            ? (1.0 + eccentricity) : (1.0 - eccentricity)) / Math.Max(distanceM, 1.0));
        // 轨道朝向：黄道面是 X–Z（Y 是黄道北极）。
        // 用升交点方向 û、轨道面内正交方向 ŵ 组成轨道面基向量，
        // 远日点方向角 = 近日点经度 + 180°，这样每条轨道的倾斜方向都不同。
        double inclination = inclinationDeg * Math.PI / 180.0;
        double node = nodeDeg * Math.PI / 180.0;
        double perihelion = perihelionDeg * Math.PI / 180.0;
        double theta = perihelion + (startAtPerihelion ? 0.0 : Math.PI);

        double cosN = Math.Cos(node), sinN = Math.Sin(node);
        double cosI = Math.Cos(inclination), sinI = Math.Sin(inclination);

        // û：升交点方向（黄道面内）
        double ux = cosN, uy = 0.0, uz = -sinN;
        // ŵ：轨道面内与 û 垂直的方向（含倾角）
        double wx = -sinN * cosI, wy = sinI, wz = -cosN * cosI;

        double cosT = Math.Cos(theta), sinT = Math.Sin(theta);

        double dirX = cosT * ux + sinT * wx;
        double dirY = cosT * uy + sinT * wy;
        double dirZ = cosT * uz + sinT * wz;

        double velX = -sinT * ux + cosT * wx;
        double velY = -sinT * uy + cosT * wy;
        double velZ = -sinT * uz + cosT * wz;

        SandboxDVec3 sunPosition = sunBody != null
            ? sunBody.positionM : SandboxDVec3.Zero;
        body.positionM = sunPosition + new SandboxDVec3(
            distanceM * dirX, distanceM * dirY, distanceM * dirZ);
        body.velocityMps = new SandboxDVec3(
            speed * velX, speed * velY, speed * velZ);        body.orbitTarget = sunBody;
        body.isOrbiting = true;
        body.orbitRadiusM = distanceM;
        body.orbitInclinationDeg = inclinationDeg;
        planet.SetLogicalPosition(
            body.positionM.x / (CelestialScale.KmPerUnit * 1000.0),
            body.positionM.y / (CelestialScale.KmPerUnit * 1000.0),
            body.positionM.z / (CelestialScale.KmPerUnit * 1000.0));
    }

    private static void SetTimeSpeed(double value, SandboxTimeUnit unit) {
        if (SandboxTimeManager.Instance == null) return;
        SandboxTimeManager.Instance.SetUnit(unit);
        SandboxTimeManager.Instance.SetSpeed(value);
    }

    private sealed class BodyLabel {
        public Transform body;
        public SandboxStar star;
        public SandboxPlanet planet;
        public TextMeshProUGUI text;
        public RectTransform rect;
        public string lastName;
    }

    private sealed class LabelCandidate {
        public BodyLabel label;
        public Vector2 screenPoint;
        public Rect rect;
        public double radius;
    }
}