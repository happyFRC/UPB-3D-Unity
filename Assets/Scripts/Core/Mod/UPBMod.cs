using UnityEngine;

public interface UPBMod {
    void Startup(UPBLogger logger);
}

public class UPBLogger {
    readonly string _displayName;

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