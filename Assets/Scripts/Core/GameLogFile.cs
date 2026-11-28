using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 把运行时的所有日志写进 Logs/ 目录（每次启动一个 log-yyyyMMdd-HHmmss.txt）：
///   编辑器：&lt;工程根&gt;/Logs/      （与 Assets 同级）
///   打包后：&lt;游戏根&gt;/Logs/      （与 exe 同级）
/// 如果游戏放在没有写权限的目录，会自动退到 Application.persistentDataPath/Logs。
///
/// 用 Application.logMessageReceivedThreaded 捕获，**不替换** Debug.unityLogger.logHandler，
/// 所以不会和 PlayerLogFilter / RuntimeLogFilter 抢 handler。
/// 注意：被那两个过滤器主动丢弃的噪音（TMP 缺字、屏幕坐标越界）不会出现在文件里。
/// </summary>
public static class GameLogFile {
    private static readonly object Gate = new();
    private static StreamWriter _writer;
    private static bool _installed;

    /// <summary>Logs 目录（绝对路径）。</summary>
    public static string FolderPath {
        get {
            if (!string.IsNullOrEmpty(FilePath)) return Path.GetDirectoryName(FilePath);
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
        }
    }

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

    private static string[] CandidateFolders() => new[] {
        // 首选：与 Assets / exe 同级的 Logs
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs")),
        // 兜底：游戏目录不可写时（例如装在 Program Files）放到持久化数据目录
        Path.Combine(Application.persistentDataPath, "Logs")
    };

    private static void Open() {
        foreach (string folder in CandidateFolders()) {
            try {
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder,
                    "log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");

                FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                StreamWriter writer = new(stream, new UTF8Encoding(true)) { AutoFlush = true };

                writer.WriteLine("# 行星球宇宙日志");
                writer.WriteLine("# 开始时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine("# 版本: " + Application.version +
                                 "   Unity: " + Application.unityVersion +
                                 "   平台: " + Application.platform);
                writer.WriteLine(new string('-', 60));

                lock (Gate) {
                    _writer = writer;
                    FilePath = path;
                }

                // 顺便在控制台打一行，方便确认日志写到哪儿了。
                Debug.Log("[LogFile] 日志文件: " + path);
                return;
            } catch (Exception e) {
                Debug.LogWarning("[LogFile] 无法在 " + folder + " 创建日志: " + e.Message);
            }
        }

        FilePath = null;
        Debug.LogWarning("[LogFile] 所有候选目录都不可写，已关闭文件日志");
    }

    private static void OnLog(string condition, string stackTrace, LogType type) {
        if (_writer == null) return;

        lock (Gate) {
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
        lock (Gate) {
            try {
                _writer?.Flush();
                _writer?.Dispose();
            } catch { }
            _writer = null;
        }
    }
}
