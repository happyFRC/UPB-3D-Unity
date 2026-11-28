using System;
using System.IO;
using UnityEngine;

/// <summary>
/// F2 截图，保存到“游戏根目录/screenshots”。
///
/// 根目录的取法：
/// - 编辑器里 Application.dataPath 是 &lt;工程&gt;/Assets，上一级就是工程根目录；
/// - 打包后是 &lt;游戏名&gt;_Data，上一级就是可执行文件所在目录。
///
/// 通过 RuntimeInitializeOnLoadMethod 自动挂载并 DontDestroyOnLoad，
/// 所有场景都能用，不需要改场景或拖引用。
/// </summary>
public class ScreenshotCapture : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        if (FindObjectOfType<ScreenshotCapture>() != null) return;

        GameObject go = new("ScreenshotCapture");
        go.AddComponent<ScreenshotCapture>();
        DontDestroyOnLoad(go);
    }

    /// <summary>截图目录：游戏根目录下的 screenshots。</summary>
    public static string ScreenshotDirectory {
        get {
            string dataPath = Application.dataPath;
            string root = Directory.GetParent(dataPath)?.FullName ?? dataPath;
            return Path.Combine(root, "screenshots");
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F2)) Capture();
    }

    private static void Capture() {
        try {
            string directory = ScreenshotDirectory;
            try {
                Directory.CreateDirectory(directory);
            } catch {
                // 安装目录不可写时退到 persistentDataPath，至少别丢截图。
                directory = Path.Combine(Application.persistentDataPath, "screenshots");
                Directory.CreateDirectory(directory);
            }

            string fileName = "screenshot_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png";
            string path = Path.Combine(directory, fileName);

            // 在本帧渲染结束后落盘，包含 UI。
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[Screenshot] " + path);
            FloatingText.Show("截图已保存：" + fileName);
        } catch (Exception e) {
            Debug.LogWarning("[Screenshot] 保存失败：" + e.Message);
            FloatingText.Show("截图失败：" + e.Message);
        }
    }
}
