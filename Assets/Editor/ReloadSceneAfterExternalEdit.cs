#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 外部修改了场景文件（例如批量换字体）之后，让编辑器从磁盘重载当前场景。
/// 只在 Temp 目录里存在标记文件时触发一次；场景有未保存改动时不会重载。
/// </summary>
[InitializeOnLoad]
public static class ReloadSceneAfterExternalEdit {
    private const string MarkerRelative = "Temp/upb_reload_scene.flag";

    static ReloadSceneAfterExternalEdit() {
        string marker = Path.Combine(Directory.GetCurrentDirectory(), MarkerRelative)
            .Replace('/', Path.DirectorySeparatorChar);
        if (!File.Exists(marker)) return;
        File.Delete(marker);

        EditorApplication.delayCall += Reload;
    }

    private static void Reload() {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) return;
        if (scene.isDirty) {
            Debug.LogWarning("[Font] 场景有未保存改动，跳过自动重载：" + scene.path);
            return;
        }
        EditorSceneManager.OpenScene(scene.path);
        Debug.Log("[Font] 已从磁盘重载场景：" + scene.path);
    }
}
#endif
