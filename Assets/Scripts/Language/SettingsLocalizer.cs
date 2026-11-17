using UnityEngine;

namespace UPB.Localization
{
    /// <summary>
    /// 挂在设置页 Canvas 上：场景启动时按当前语言刷新一遍文字，
    /// 之后语言一变就自动再刷新。也可以手动调用 Apply()。
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsLocalizer : MonoBehaviour
    {
        [Tooltip("要翻译的根节点，留空则用自己所在的物体")]
        [SerializeField] Transform root;

        bool _subscribed;

        void OnEnable()
        {
            if (!_subscribed)
            {
                LanguageManager.Changed += OnLanguageChanged;
                _subscribed = true;
            }
            Apply();
        }

        void Start()
        {
            Apply();
        }

        void OnDisable()
        {
            if (_subscribed)
            {
                LanguageManager.Changed -= OnLanguageChanged;
                _subscribed = false;
            }
        }

        void OnLanguageChanged(AppLanguage language)
        {
            Apply();
        }

        public void Apply()
        {
            SettingsLocalization.Apply(root != null ? root : transform);
        }
    }
}
