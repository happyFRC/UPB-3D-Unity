using System;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// 沙盘注册表：天体（行星 / 矮行星 / 卫星 / 彩蛋 / 可选恒星）和单位表。
///
/// 内置条目由 <see cref="BuiltinSandboxObjects"/> 在第一次访问时注册一次，
/// 其它脚本也可以自己调用 Register* 追加。
///
/// 注意：注册要在沙盒 UI 建立之前完成（Start 之前，或
/// [RuntimeInitializeOnLoadMethod] 里），UI 只会读取一次列表。
/// </summary>
public static class SandboxRegister {
    private static readonly List<PlanetData> _planets = new();
    private static readonly List<PlanetData> _dwarfPlanets = new();
    private static readonly List<PlanetData> _moons = new();
    private static readonly List<PlanetData> _easterEggs = new();
    private static readonly List<StarData> _stars = new();
    private static readonly List<SandboxUnitCategory> _unitCategories = new();

    // 列表的数组快照，注册后置空，下次访问重新生成。
    private static PlanetData[] _planetCache;
    private static PlanetData[] _dwarfCache;
    private static PlanetData[] _moonCache;
    private static PlanetData[] _eggCache;
    private static StarData[] _starCache;
    private static SandboxUnitCategory[] _unitCache;

    private static bool _initialized;
    private static bool _initializing;

    /// <summary>行星。返回的数组不要修改。</summary>
    public static PlanetData[] Planets {
        get {
            EnsureInitialized();
            return _planetCache ??= _planets.ToArray();
        }
    }

    /// <summary>矮行星 / 小行星。返回的数组不要修改。</summary>
    public static PlanetData[] DwarfPlanets {
        get {
            EnsureInitialized();
            return _dwarfCache ??= _dwarfPlanets.ToArray();
        }
    }

    /// <summary>卫星。返回的数组不要修改。</summary>
    public static PlanetData[] Moons {
        get {
            EnsureInitialized();
            return _moonCache ??= _moons.ToArray();
        }
    }

    /// <summary>彩蛋（长方体）。返回的数组不要修改。</summary>
    public static PlanetData[] EasterEggs {
        get {
            EnsureInitialized();
            return _eggCache ??= _easterEggs.ToArray();
        }
    }

    /// <summary>沙盘可选的恒星（按注册顺序，UI 每页 6 个）。返回的数组不要修改。</summary>
    public static StarData[] Stars {
        get {
            EnsureInitialized();
            return _starCache ??= _stars.ToArray();
        }
    }

    /// <summary>单位表分类（顺序即单位选择器的候选顺序）。返回的数组不要修改。</summary>
    public static SandboxUnitCategory[] UnitCategories {
        get {
            EnsureInitialized();
            return _unitCache ??= _unitCategories.ToArray();
        }
    }

    /// <summary>注册一颗行星。</summary>
    public static void RegisterPlanet(PlanetData data) {
        Add(_planets, ref _planetCache, data);
    }

    /// <summary>注册一颗矮行星 / 小行星。</summary>
    public static void RegisterDwarfPlanet(PlanetData data) {
        Add(_dwarfPlanets, ref _dwarfCache, data);
    }

    /// <summary>注册一颗卫星（orbitRadiusKm 作为摆放时的默认轨道半径）。</summary>
    public static void RegisterMoon(PlanetData data) {
        Add(_moons, ref _moonCache, data);
    }

    /// <summary>注册一个彩蛋（isBox = true 时按长方体渲染）。</summary>
    public static void RegisterEasterEgg(PlanetData data) {
        Add(_easterEggs, ref _eggCache, data);
    }

    /// <summary>按名字找一颗行星（找不到返回 null）。</summary>
    public static PlanetData FindPlanet(string name) {
        EnsureInitialized();
        return Find(_planets, name);
    }

    /// <summary>按名字找一颗卫星（找不到返回 null）。</summary>
    public static PlanetData FindMoon(string name) {
        EnsureInitialized();
        return Find(_moons, name);
    }

    /// <summary>注册一颗可选恒星（只有质量/金属度/年龄等参数，其余由 SSE 实时算）。</summary>
    public static void RegisterStar(StarData data) {
        EnsureInitialized();
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrEmpty(data.name)) {
            throw new ArgumentException("恒星的 name 不能为空", nameof(data));
        }

        _stars.Add(data);
        _starCache = null;
    }

    /// <summary>注册一个单位分类（temperature 这类带 offset 的也走同一个入口）。</summary>
    public static void RegisterUnitCategory(SandboxUnitCategory category) {
        EnsureInitialized();
        if (category == null) throw new ArgumentNullException(nameof(category));
        if (string.IsNullOrEmpty(category.key)) {
            throw new ArgumentException("单位分类的 key 不能为空", nameof(category));
        }
        if (category.units == null || category.units.Length == 0) {
            throw new ArgumentException("单位分类 " + category.key + " 没有任何单位", nameof(category));
        }

        _unitCategories.Add(category);
        _unitCache = null;
    }

    /// <summary>按名字找一颗可选恒星（找不到返回 null）。</summary>
    public static StarData FindStar(string name) {
        EnsureInitialized();
        if (string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < _stars.Count; i++) {
            StarData data = _stars[i];
            if (data != null && data.name == name) return data;
        }
        return null;
    }

    /// <summary>注册内置天体。重复调用没有副作用。</summary>
    public static void EnsureInitialized() {
        if (_initialized || _initializing) return;

        _initializing = true;
        try {
            BuiltinSandboxObjects.RegisterAll();
            _initialized = true;
        }
        finally {
            _initializing = false;
        }
    }

    private static void Add(List<PlanetData> list, ref PlanetData[] cache, PlanetData data) {
        EnsureInitialized();
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrEmpty(data.name)) {
            throw new ArgumentException("沙盘天体的 name 不能为空", nameof(data));
        }

        list.Add(data);
        cache = null;
    }

    private static PlanetData Find(List<PlanetData> list, string name) {
        if (string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < list.Count; i++) {
            PlanetData data = list[i];
            if (data != null && data.name == name) return data;
        }
        return null;
    }

    /// <summary>
    /// 批量生成"某单位 + SI 前缀"的条目，例如 StandardUnits("W") → W/kW/MW/GW/TW/PW。
    /// baseFactor 是这个单位本身换算到国际单位基准值的倍数（yr → 31556952）；
    /// 传 0 或负数会被当成 1，避免调用方写 default 时把所有换算率乘成 0。
    /// </summary>
    public static SandboxUnitDefinition[] StandardUnits(string symbol,
                                                        double baseFactor = 1.0,
                                                        bool needSmallUnits = false,
                                                        bool needBigUnits = false) {
        if (baseFactor <= 0.0) baseFactor = 1.0;

        List<SandboxUnitDefinition> units = new(){
            SandboxUnits.NewUnit(symbol, 1.0 * baseFactor),
            SandboxUnits.NewUnit("k" + symbol, 1e3 * baseFactor),
            SandboxUnits.NewUnit("M" + symbol, 1e6 * baseFactor),
            SandboxUnits.NewUnit("G" + symbol, 1e9 * baseFactor),
            SandboxUnits.NewUnit("T" + symbol, 1e12 * baseFactor),
            SandboxUnits.NewUnit("P" + symbol, 1e15 * baseFactor),
        };

        List<SandboxUnitDefinition> bigUnits = new(){
            SandboxUnits.NewUnit("E" + symbol, 1e18 * baseFactor),
            SandboxUnits.NewUnit("Z" + symbol, 1e21 * baseFactor),
            SandboxUnits.NewUnit("Y" + symbol, 1e24 * baseFactor),
        };

        List<SandboxUnitDefinition> smallUnits = new(){
            SandboxUnits.NewUnit("m" + symbol, 1e-3 * baseFactor),
            SandboxUnits.NewUnit("μ" + symbol, 1e-6 * baseFactor),
            SandboxUnits.NewUnit("n" + symbol, 1e-9 * baseFactor),
        };

        if (needSmallUnits) units.AddRange(smallUnits);
        if (needBigUnits) units.AddRange(bigUnits);

        return units.ToArray();
    }
}
