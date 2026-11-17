using System;
using UnityEngine;

/// <summary>
/// 打包后过滤掉两类“无意义但会刷屏”的警告，避免写进 player.log：
///
/// 1) Unity 的
///    "Screen position out of view frustum (screen pos ...) (Camera rect ...)"
///    这是相机/UI 用屏幕坐标做换算时，坐标跑到相机矩形外触发的。
///    在编辑器里鼠标移出 Game 视图就会刷屏；打包后正常情况下不会出现，
///    这里仍然兜底过滤一次。
///
/// 2) TextMeshPro 的缺字警告
///    "The character with Unicode value \uXXXX was not found in the [...] font asset ..."
///    FRCFONT SDF 没有收录部分生僻字，运行时会替代成空格并打印警告。
///
/// 只拦截 Warning，其它级别的日志原样输出；不改变任何业务逻辑。
/// </summary>
public static class PlayerLogFilter {
    private static bool _installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install() {
        if (_installed) return;
        _installed = true;

        // 包一层原来的 handler，保证非目标日志照常输出。
        Debug.unityLogger.logHandler = new FilteredLogHandler(Debug.unityLogger.logHandler);
    }

    private sealed class FilteredLogHandler : ILogHandler {
        private readonly ILogHandler _inner;

        public FilteredLogHandler(ILogHandler inner) {
            _inner = inner;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context,
                string format, params object[] args) {
            if (ShouldDrop(logType, format)) return;
            _inner.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context) {
            _inner.LogException(exception, context);
        }

        private static bool ShouldDrop(LogType logType, string message) {
            if (logType != LogType.Warning || string.IsNullOrEmpty(message)) return false;

            // 1) 屏幕坐标越界（相机 / UGUI 换算触发）。
            if (message.StartsWith("Screen position out of view frustum",
                    StringComparison.Ordinal)) {
                return true;
            }

            // 2) TMP 字体缺字。
            if (message.IndexOf("was not found in the", StringComparison.Ordinal) >= 0 &&
                message.IndexOf("font asset", StringComparison.OrdinalIgnoreCase) >= 0) {
                return true;
            }

            return false;
        }
    }
}
