using System;
using UnityEngine;

namespace UPB.Localization
{
    /// <summary>
    /// 当前界面语言：zh_cn / en_us。
    /// 选择结果保存在 PlayerPrefs("AppLanguage")，切换时广播 Changed 事件。
    /// </summary>
    public enum AppLanguage
    {
        ZhCn = 0,
        EnUs = 1
    }

    public static class LanguageManager
    {
        public const string PrefKey = "AppLanguage";

        static AppLanguage _current;
        static bool _loaded;

        static readonly string[] PrefValues = { "zh_cn", "en_us" };

        /// <summary>语言变化时触发，参数为新语言。</summary>
        public static event Action<AppLanguage> Changed;

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            int saved = PlayerPrefs.GetInt(PrefKey, (int)AppLanguage.ZhCn);
            if (saved < (int)AppLanguage.ZhCn || saved > (int)AppLanguage.EnUs)
            {
                saved = (int)AppLanguage.ZhCn;
            }
            _current = (AppLanguage)saved;
        }

        /// <summary>当前语言。</summary>
        public static AppLanguage Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        public static bool IsEnglish
        {
            get { return Current == AppLanguage.EnUs; }
        }

        /// <summary>当前语言的存档字符串："zh_cn" 或 "en_us"。</summary>
        public static string CurrentCode
        {
            get { return PrefValues[(int)Current]; }
        }

        /// <summary>切换到指定语言；语言没变化时不重复触发事件。</summary>
        public static void Set(AppLanguage language)
        {
            EnsureLoaded();
            if (_current == language) return;

            _current = language;
            PlayerPrefs.SetInt(PrefKey, (int)language);
            PlayerPrefs.Save();

            if (Changed != null) Changed(language);
        }

        /// <summary>按当前语言取中英文中的一条。</summary>
        public static string Get(string zh, string en)
        {
            return IsEnglish ? en : zh;
        }

        /// <summary>按名字查找场景对象（包含未激活的对象），找不到返回 null。</summary>
        public static Transform FindTransform(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            Transform[] all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return all[i];
            }
            return null;
        }
    }
}
