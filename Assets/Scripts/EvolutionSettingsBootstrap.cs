using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主序星演化场景加载时自动挂 EvolutionSettingsController。
/// 这样新加的演化设置 UI 不需要手动拖任何脚本。
/// </summary>
public static class EvolutionSettingsBootstrap {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureForActiveScene() {
        if (SceneManager.GetActiveScene().name == "StellarEvolutionScene") {
            EnsureController();
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == "StellarEvolutionScene") EnsureController();
    }

    private static void EnsureController() {
        // 有些退出路径会把暂停时的 0 倍速带进下一次场景。
        if (Time.timeScale <= 0f) Time.timeScale = 1f;

        if (UnityEngine.Object.FindObjectOfType<EvolutionSettingsController>() != null) return;
        GameObject go = new("EvolutionSettingsController");
        go.AddComponent<EvolutionSettingsController>();
    }
}
