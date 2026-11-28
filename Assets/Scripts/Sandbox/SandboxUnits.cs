using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SandboxUnitDefinition {
    /// <summary>内部标识：换算、比较、存档都用它，保持 ASCII。</summary>
    public string symbol;

    /// <summary>界面上显示的名字；为空时用 symbol。例如 symbol="Rsun"、display="R☉"。</summary>
    public string display;

    public double factor = 1.0;
    public double offset;

    /// <summary>UI 用：有 display 用 display，否则用 symbol。</summary>
    public string DisplaySymbol => string.IsNullOrEmpty(display) ? symbol : display;
}

[Serializable]
public class SandboxUnitCategory {
    public string key;
    public SandboxUnitDefinition[] units;

    public SandboxUnitDefinition Find(string symbol) {
        if (units == null || string.IsNullOrEmpty(symbol)) return null;
        for (int i = 0; i < units.Length; i++) {
            if (string.Equals(units[i].symbol, symbol, StringComparison.OrdinalIgnoreCase)) {
                return units[i];
            }
        }
        return null;
    }
}

/// <summary>
/// 通用单位表。所有单位换算率都换算到国际单位（m/kg/s/K/W/J/N）。
/// 温度支持 offset：base = value * factor + offset。
/// 单位数据由 SandboxRegister（BuiltinSandboxObjects）注册，不再读取 units.json。
/// </summary>
public static class SandboxUnits {
    private static readonly Dictionary<string, SandboxUnitCategory> _categories =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _displayBySymbol =
        new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static SandboxUnitDefinition NewUnit(string symbol, double factor, double offset = 0.0,
            string display = null) {
        return new() { symbol = symbol, factor = factor, offset = offset, display = display };
    }

    /// <summary>
    /// UI 显示名：优先取注册时给的 display，其次查注册表里收集到的映射，最后回退成 symbol。
    /// 例：Rsun → R☉、Mearth → M⊕、C → °C、kg/m^3 → kg/m³。
    /// </summary>
    public static string Display(string symbol) {
        if (string.IsNullOrEmpty(symbol)) return symbol;

        EnsureLoaded();
        if (_displayBySymbol.TryGetValue(symbol, out string display) &&
            !string.IsNullOrEmpty(display)) {
            return display;
        }

        // 不在注册表里、但 UI 上会直接用到的符号（进化场景 / 引力单位等）。
        return symbol switch {
            "m/s2" => "m/s²",
            "RSun" or "Rsun" => "R☉",
            "REarth" or "Rearth" => "R⊕",
            "MSun" or "Msun" => "M☉",
            "MEarth" or "Mearth" => "M⊕",
            "MJup" => "M_J",
            "LSun" or "Lsun" => "L☉",
            "C" or "°C" => "°C",
            "F" or "°F" => "°F",
            "kg/m^3" or "kg/m3" => "kg/m³",
            "g/cm^3" or "g/cm3" => "g/cm³",
            _ => symbol
        };
    }

    public static void EnsureLoaded() {
        if (_loaded) return;
        _loaded = true;

        SandboxUnitCategory[] categories = SandboxRegister.UnitCategories;
        if (categories == null || categories.Length == 0) {
            Debug.LogWarning("[Units] 单位表为空（SandboxRegister.UnitCategories）");
            return;
        }

        foreach (SandboxUnitCategory category in categories) {
            if (category == null || string.IsNullOrEmpty(category.key) ||
                category.units == null) continue;

            // 单位顺序由注册代码维护，便于温度这类带偏移的单位保持 K/C/F 的展示顺序。
            _categories[category.key] = category;

            foreach (SandboxUnitDefinition unit in category.units) {
                if (unit == null || string.IsNullOrEmpty(unit.symbol)) continue;
                if (!string.IsNullOrEmpty(unit.display)) _displayBySymbol[unit.symbol] = unit.display;
            }
        }
    }

    public static SandboxUnitCategory Category(string key) {
        EnsureLoaded();
        if (string.IsNullOrEmpty(key)) return null;
        if (_categories.TryGetValue(key, out SandboxUnitCategory category)) return category;
        return null;
    }

    public static SandboxUnitDefinition Definition(string category, string symbol) {
        SandboxUnitCategory c = Category(category);
        return c?.Find(Normalize(symbol));
    }

    public static string Normalize(string symbol) {
        if (string.IsNullOrEmpty(symbol)) return symbol;
        return symbol switch {
            "°C" => "C",
            "°F" => "F",
            "M☉" => "Msun",
            "M⊕" => "Mearth",
            "M_J" => "MJup",
            "R☉" => "Rsun",
            "R⊕" => "Rearth",
            "L☉" => "Lsun",
            "kg/m³" => "kg/m3",
            "g/cm³" => "g/cm3",
            "m/s²" => "m/s2",
            _ => symbol,
        };
    }

    /// <summary>单位值 → 国际单位基准值。</summary>
    public static double ToBase(double value, SandboxUnitDefinition unit) {
        if (unit == null || unit.factor <= 0.0) return value;
        return value * unit.factor + unit.offset;
    }

    /// <summary>国际单位基准值 → 单位值。</summary>
    public static double FromBase(double baseValue, SandboxUnitDefinition unit) {
        if (unit == null || unit.factor <= 0.0) return baseValue;
        return (baseValue - unit.offset) / unit.factor;
    }

    /// <summary>
    /// 自适应单位：挑一个让数值不小于 0.1 的“最大”单位。
    /// 例如 5,300,000 m → 5300 km；0.05 km（=50 m）→ 50 m。
    /// </summary>
    /// <summary>英制单位：自动切换时跳过，只有用户手动选才用。</summary>
    private static readonly string[] _autoBlacklist = { "mi", "mph", "F", "°F", "ft", "in", "lb", "oz", "pc" };

    private static bool IsBlacklisted(SandboxUnitDefinition unit) {
        if (unit == null || string.IsNullOrEmpty(unit.symbol)) return false;
        for (int i = 0; i < _autoBlacklist.Length; i++) {
            if (string.Equals(_autoBlacklist[i], unit.symbol, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
        return false;
    }

    private static bool IsCandidate(SandboxUnitDefinition unit, System.Collections.Generic.IList<string> candidates) {
        if (IsBlacklisted(unit)) return false;
        if (candidates == null || candidates.Count == 0) return true;
        for (int i = 0; i < candidates.Count; i++) {
            if (string.Equals(candidates[i], unit.symbol, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 自适应单位：在候选单位里挑一个让数值落在 [0.1, 1000) 的最大单位。
    /// candidates 为空时用整个单位表。
    /// </summary>
    public static SandboxUnitDefinition AutoDefinition(string category, double baseValue,
            System.Collections.Generic.IList<string> candidates = null) {
        SandboxUnitCategory c = Category(category);
        if (c?.units == null || c.units.Length == 0) return null;

        double magnitude = Math.Abs(baseValue);
        if (double.IsNaN(magnitude) || double.IsInfinity(magnitude)) return c.units[0];

        SandboxUnitDefinition largest = null;
        double largestFactor = double.NegativeInfinity;
        SandboxUnitDefinition smallest = null;
        double smallestFactor = double.PositiveInfinity;

        foreach (SandboxUnitDefinition unit in c.units) {
            if (unit == null || unit.factor <= 0.0) continue;
            if (!IsCandidate(unit, candidates)) continue;

            if (unit.factor < smallestFactor) {
                smallestFactor = unit.factor;
                smallest = unit;
            }
            double value = (magnitude - unit.offset) / unit.factor;
            if (value >= 0.1 && unit.factor > largestFactor) {
                largest = unit;
                largestFactor = unit.factor;
            }
        }

        if (largest == null) return smallest ?? c.units[0];

        // 最大单位下数值超过 1000 时，退回一个仍在 [0.1, 1000) 内的单位。
        double largestValue = (magnitude - largest.offset) / largest.factor;
        if (largestValue >= 1000.0) {
            SandboxUnitDefinition better = null;
            double betterFactor = double.NegativeInfinity;
            foreach (SandboxUnitDefinition unit in c.units) {
                if (unit == null || unit.factor <= 0.0) continue;
                if (!IsCandidate(unit, candidates)) continue;
                double value = (magnitude - unit.offset) / unit.factor;
                if (value >= 0.1 && value < 1000.0 && unit.factor > betterFactor) {
                    better = unit;
                    betterFactor = unit.factor;
                }
            }
            if (better != null) return better;
        }
        return largest;
    }
    public static string[] Symbols(string category) {
        SandboxUnitCategory c = Category(category);
        if (c?.units == null) return Array.Empty<string>();
        string[] symbols = new string[c.units.Length];
        for (int i = 0; i < c.units.Length; i++) symbols[i] = c.units[i].symbol;
        return symbols;
    }
}
