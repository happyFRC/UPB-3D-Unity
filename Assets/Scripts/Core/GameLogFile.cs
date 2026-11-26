using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 把运行时的所有日志写进 Assets 同级的 Logs/ 目录：
///   Logs/log-yyyyMMdd-HHmmss.txt   （每次启动一个文件）
///
/// 用 Application.logMessageReceivedThreaded 捕获，**不替换** Debug.unityLogger.logHandler，
/// 所以不会和 PlayerLogFilter / RuntimeLogFilter 抢 handler。
/// 注意：被那两个过滤器主动丢弃的噪音（TMP 缺字、屏幕坐标越界）不会出现在文件里。
///
/// 打包后路径是 exe 所在目录下的 Logs/；编辑器里是工程根目录下的 Logs/。
/// </summary>
public static class GameLogFile {
    private static readonly object _gate = new();
    private static StreamWriter _writer;
    private static bool _installed;

    /// <summary>Logs 目录（绝对路径）。</summary>
    public static string FolderPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));

    /// <summary>当前这次运行的日志文件（绝对路径），创建失败时为 null。</summary>
    public static string FilePath { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install() {
        if (_installed) {
            // 关闭域重载时静态状态会保留，但文件可能已经在退出时关掉了，重新打开一个。
            if (_writer == null) Open();
            return;
        }

        _installed = true;
        Open();
        Application.logMessageReceivedThreaded += OnLog;
        Application.quitting += Shutdown;
    }

    private static void Open() {
        try {
            Directory.CreateDirectory(FolderPath);
            FilePath = Path.Combine(FolderPath,
                "log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");

            FileStream stream = new(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            // 带 BOM 的 UTF-8：记事本 / 旧版编辑器在中文 Windows 上也能正确识别。
            _writer = new StreamWriter(stream, new UTF8Encoding(true)) { AutoFlush = true };

            _writer.WriteLine("# 行星球宇宙日志");
            _writer.WriteLine("# 开始时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _writer.WriteLine("# 版本: " + Application.version +
                              "   Unity: " + Application.unityVersion +
                              "   平台: " + Application.platform);
            _writer.WriteLine(new string('-', 60));
        } catch (Exception e) {
            _writer = null;
            FilePath = null;
            Debug.LogWarning("[LogFile] 无法创建日志文件: " + e.Message);
        }
    }

    private static void OnLog(string condition, string stackTrace, LogType type) {
        if (_writer == null) return;

        lock (_gate) {
            if (_writer == null) return;
            try {
                _writer.Write('[');
                _writer.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                _writer.Write("] [");
                _writer.Write(Tag(type));
                _writer.Write("] ");
                _writer.WriteLine(condition ?? string.Empty);

                // 非普通日志带调用栈，方便定位。
                if (type != LogType.Log && !string.IsNullOrEmpty(stackTrace)) {
                    foreach (string line in stackTrace.Split('\n')) {
                        string trimmed = line.TrimEnd();
                        if (trimmed.Length == 0) continue;
                        _writer.Write("    ");
                        _writer.WriteLine(trimmed);
                    }
                }
            } catch {
                // 写日志失败不能影响游戏
            }
        }
    }

    private static string Tag(LogType type) => type switch {
        LogType.Warning => "WARN",
        LogType.Error => "ERROR",
        LogType.Exception => "EXCEPTION",
        LogType.Assert => "ASSERT",
        _ => "LOG"
    };

    private static void Shutdown() {
        lock (_gate) {
            try {
                _writer?.Flush();
                _writer?.Dispose();
            } catch { }
            _writer = null;
        }
    }
}
