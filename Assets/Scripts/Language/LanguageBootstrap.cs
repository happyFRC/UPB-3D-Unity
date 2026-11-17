using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPB.Localization
{
    /// <summary>
    /// 零配置入口：每次场景加载后，把当前场景里的 UI 文字切成所选语言；
    /// 语言一变，当前已加载的场景立刻整体刷新一遍；
    /// 另外低频补刷，覆盖脚本在 Start / 运行时才写进去的文案。
    /// 还负责给 Settings 场景的 Language_Panel 挂上按钮逻辑。
    /// </summary>
    public static class LanguageBootstrap
    {
        public const string SettingsSceneName = "Settings";

        /// <summary>关掉就不再自动翻译 / 自动挂组件。</summary>
        public static bool IsEnabled = true;

        static bool _subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!IsEnabled) return;

            if (!_subscribed)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                LanguageManager.Changed += OnLanguageChanged;
                Canvas.willRenderCanvases += OnWillRenderCanvases;
                _subscribed = true;
            }

            ApplyAllScenes();
            WireLanguagePanel(SceneManager.GetActiveScene());
        }

        // 有些文案是脚本在 Start / 运行时才写进去的，低频补刷一遍即可覆盖。
        static float _nextRefresh;

        static void OnWillRenderCanvases()
        {
            if (!IsEnabled) return;

            float now = Time.unscaledTime;
            if (now < _nextRefresh) return;
            _nextRefresh = now + 0.25f;

            ApplyAllScenes();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAllScenes();
            WireLanguagePanel(scene);
        }

        static void OnLanguageChanged(AppLanguage language)
        {
            ApplyAllScenes();
        }

        /// <summary>把所有已加载场景（含 DontDestroyOnLoad）的 UI 文字刷成当前语言。</summary>
        public static void ApplyAllScenes()
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    SettingsLocalization.Apply(roots[r].transform);
                }
            }
        }

        static void WireLanguagePanel(Scene scene)
        {
            if (!scene.IsValid() || scene.name != SettingsSceneName) return;

            Transform panel = LanguageManager.FindTransform("Language_Panel");
            if (panel != null)
            {
                LanguagePanel languagePanel = panel.GetComponent<LanguagePanel>();
                if (languagePanel == null)
                {
                    languagePanel = panel.gameObject.AddComponent<LanguagePanel>();
                }

                // 面板默认隐藏，组件自己的 Start 不会执行，
                // 所以在这里把“语言……”按钮接上，否则点了没反应。
                languagePanel.WireOpenButton();
            }

#if UNITY_EDITOR
            Debug.Log("[Language] scene '" + scene.name + "', languagePanel=" + (panel != null) +
                      ", language=" + LanguageManager.CurrentCode);
#endif
        }
    }
}
