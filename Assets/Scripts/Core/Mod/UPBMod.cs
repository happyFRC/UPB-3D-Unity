using System.Collections.Generic;
using UnityEngine;
using UPB.SSE;

public interface UPBMod {
    void Startup(UPBModInstance instance);
}

public sealed class UPBModInstance {
    public readonly UPBModEntry entry;
    public readonly UPBLogger logger;

    public UPBModInstance(UPBModEntry entry) {
        this.entry = entry;
        logger = new UPBLogger(entry.displayName);
    }
}

public delegate void UPBEvent();

public delegate void UPBEvent<TArgs>(TArgs args);

public delegate TResult UPBEvent<TArgs, TResult>(TArgs args);

public class EventResult {
    public bool Ret = false;
    public EventResult(bool ret) {
        Ret = ret;
    }


    public readonly static EventResult OK = new(false);
    public readonly static EventResult Skip = new(true);
}

public static class UPBEventBus {
    // 恒星演化引擎事件
    public static List<UPBEvent> BeforeInitSSEEvent = new();
    public static List<UPBEvent> AfterInitSSEEvent = new();
    // ARG: int -> _sseIndex
    public static List<UPBEvent<int, EventResult>> OnEvolveStepEvent = new();
    public static List<UPBEvent<(TrackPoint point, int sseIndex), EventResult>>
        BeforeApplySSEPointEvent = new();
    public static List<UPBEvent<(TrackPoint point, int sseIndex)>>
        AfterApplySSEPointEvent = new();
    // RES: double -> life
    public static UPBEvent<(double mass, double metallicity), double> GetLifespanEvent {
        get => _evaluatingLifespan ? null : _lifespanEvent;
        set => _lifespanEvent = value == null ? null : args => {
            _evaluatingLifespan = true;
            try {
                return value(args);
            } finally {
                _evaluatingLifespan = false;
            }
        };
    }
    // ARG: double -> temperature
    public static UPBEvent<double, (float r, float g, float b)> GetBlackbodyColorEvent {
        get => _evaluatingBlackbodyColor ? null : _blackbodyColorEvent;
        set => _blackbodyColorEvent = value == null ? null : args => {
            _evaluatingBlackbodyColor = true;
            try {
                return value(args);
            } finally {
                _evaluatingBlackbodyColor = false;
            }
        };
    }
    public static List<UPBEvent<(Spectrum spectrum, string type,
                                 int subtype, string luminosity), EventResult>>
        OnInitSpectrum = new();

    

    private static UPBEvent<(double mass, double metallicity), double> _lifespanEvent;
    private static UPBEvent<double, (float r, float g, float b)> _blackbodyColorEvent;
    private static bool _evaluatingLifespan;
    private static bool _evaluatingBlackbodyColor;
}

public sealed class UPBLogger {
    public readonly string _displayName;

    public UPBLogger(string displayName) {
        _displayName = displayName;
    }

    public void info(string s) {
        Debug.Log($"[{_displayName} INFO] " + s);
    }

    public void warn(string s) {
        Debug.LogWarning($"[{_displayName} WARN] " + s);
    }

    public void err(string s) {
        Debug.LogError($"[{_displayName} ERR] " + s);
    }

    public void floatText(string s, Color color) {
        FloatingText.Show(s, color);
    }

    public void floatText(string s) {
        FloatingText.Show(s, Color.yellow);
    }
}
