using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 参数行右侧的单位按钮。点击后打开通用 Unit_Panel，而不是原地循环。
/// category 由使用方声明，例如 length / mass / time / power。
/// </summary>
public class UnitButton : MonoBehaviour {
    public string[] units = { };
    public int currentUnit = 0;
    public string category;

    /// <summary>没被用户手动选过单位时，显示时按数值自动挑单位。</summary>
    public bool autoSelect = true;

    /// <summary>自适应单位只在候选列表里挑；为空时用整张单位表。</summary>
    public string[] autoCandidates;

    public event Action<string> onUnitChanged;

    private string _selectedUnit;
    private bool _hooked;

    private void Awake() {
        HookButton();
    }

    private void Start() {
        HookButton();
        if (string.IsNullOrEmpty(_selectedUnit) && units != null && units.Length > 0) {
            ApplyUnit(units[0], notify: false);
        } else {
            RefreshLabel();
        }
    }

    public void SetCategory(string unitCategory, string[] fallbackUnits = null) {
        HookButton();
        category = unitCategory;
        if (fallbackUnits != null && fallbackUnits.Length > 0) units = fallbackUnits;
        if (string.IsNullOrEmpty(_selectedUnit) && units != null && units.Length > 0) {
            ApplyUnit(units[0], notify: false);
        } else {
            RefreshLabel();
        }
    }

    private void HookButton() {
        if (_hooked) return;
        Button button = GetComponent<Button>();
        if (button == null) return;
        button.onClick.AddListener(OnClick);
        _hooked = true;
    }

    private void OnClick() {
        if (UnitPickerPanel.TryOpen(this)) return;

        // 面板不存在时的兼容回退。
        if (units == null || units.Length == 0) return;
        currentUnit = (currentUnit + 1) % units.Length;
        ApplyUnit(units[currentUnit], notify: true);
    }

    public void ApplyUnit(string symbol, bool notify = true) {
        if (string.IsNullOrEmpty(symbol)) return;
        // 用户手动选（notify=true）之后就不再用自适应单位。
        if (notify) autoSelect = false;
        _selectedUnit = SandboxUnits.Normalize(symbol);
        if (units != null) {
            for (int i = 0; i < units.Length; i++) {
                if (string.Equals(units[i], symbol, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(units[i], _selectedUnit, StringComparison.OrdinalIgnoreCase)) {
                    currentUnit = i;
                    break;
                }
            }
        }
        RefreshLabel();
        if (notify) onUnitChanged?.Invoke(_selectedUnit);
    }

    private void RefreshLabel() {
        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null && !string.IsNullOrEmpty(GetUnit())) label.text = GetDisplayUnit();
    }

    /// <summary>按当前数值自动挑单位；用户手动选过的单位不会被覆盖。</summary>
    public void ApplyAuto(double baseValue) {
        if (!autoSelect) return;
        string[] candidates = autoCandidates;
        if (candidates == null && units != null && units.Length > 1) candidates = units;
        SandboxUnitDefinition def = SandboxUnits.AutoDefinition(GetCategory(), baseValue, candidates);
        if (def == null) return;
        if (!string.Equals(GetUnit(), def.symbol, StringComparison.OrdinalIgnoreCase)) {
            ApplyUnit(def.symbol, notify: false);
        }
    }

    /// <summary>重新回到自适应模式（重新打开参数面板时调用）。</summary>
    public void ResetAuto() {
        autoSelect = true;
    }

    public string GetUnit() {
        if (!string.IsNullOrEmpty(_selectedUnit)) return _selectedUnit;
        return units != null && units.Length > 0 ? units[Mathf.Clamp(currentUnit, 0, units.Length - 1)] : "";
    }

    /// <summary>按钮上要显示的文字（R☉ / °C / kg/m³ 这类），内部标识仍是 GetUnit()。</summary>
    public string GetDisplayUnit() => SandboxUnits.Display(GetUnit());

    public string GetCategory() {
        if (!string.IsNullOrEmpty(category)) return category;

        if (units == null || units.Length == 0) return "";
        if (Array.IndexOf(units, "K") >= 0 || Array.IndexOf(units, "°C") >= 0) return "temperature";
        if (Array.IndexOf(units, "Lsun") >= 0 || Array.IndexOf(units, "W") >= 0) return "power";
        if (Array.IndexOf(units, "Rearth") >= 0 || Array.IndexOf(units, "Rsun") >= 0 ||
            Array.IndexOf(units, "km") >= 0) return "length";
        if (Array.IndexOf(units, "Mearth") >= 0 || Array.IndexOf(units, "Msun") >= 0) return "mass";
        if (Array.IndexOf(units, "Myr") >= 0 || Array.IndexOf(units, "Gyr") >= 0) return "time";
        if (Array.IndexOf(units, "g/cm3") >= 0 || Array.IndexOf(units, "kg/m3") >= 0) return "density";
        return "";
    }
}
