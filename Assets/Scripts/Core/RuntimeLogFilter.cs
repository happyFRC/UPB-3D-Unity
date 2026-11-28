using System;
using UnityEngine;

/// <summary>
/// 玩家日志过滤：屏蔽已知无害的 TMP 缺字警告和相机屏幕点警告。
/// 只过滤这两类，其他日志照常写入 Player.log。
/// </summary>
public static class RuntimeLogFilter {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install() {
        if (Debug.unityLogger.logHandler is FilteredHandler) return;
        Debug.unityLogger.logHandler =
            new FilteredHandler(Debug.unityLogger.logHandler);
    }

    private sealed class FilteredHandler : ILogHandler {
        private readonly ILogHandler _inner;

        public FilteredHandler(ILogHandler inner) {
            _inner = inner;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context,
                string format, params object[] args) {
            if (ShouldSuppress(Format(format, args))) return;
            _inner.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context) {
            if (exception != null && ShouldSuppress(exception.Message)) return;
            _inner.LogException(exception, context);
        }

        private static string Format(string format, object[] args) {
            if (string.IsNullOrEmpty(format)) return "";
            if (args == null || args.Length == 0) return format;
            try { return string.Format(format, args); }
            catch { return format; }
        }

        private static bool ShouldSuppress(string message) {
            if (string.IsNullOrEmpty(message)) return false;
            if (message.Contains("The character with Unicode value") &&
                message.Contains("was not found")) return true;
            if (message.Contains("Screen position out of view frustum")) return true;
            return false;
        }
    }
}
