using TMPro;
using UnityEngine;

namespace UPB.Localization
{
    /// <summary>
    /// 全场景 UI 中英文对照表（Settings / MainMenu / 各演化场景 / Sandbox）。
    /// 不依赖物体层级或名字：按“当前文字内容”双向替换，所以中文 -> 英文 ->
    /// 中文都能来回切，重复执行结果不变，也不会记住任何运行时状态。
    /// </summary>
    public static class SettingsLocalization
    {
        struct Pair
        {
            public readonly string zh;
            public readonly string en;

            public Pair(string zh, string en)
            {
                this.zh = zh;
                this.en = en;
            }
        }

        static readonly Pair[] Pairs =
        {
            // ---------- 设置页 ----------
            new Pair("返回主菜单", "Back to Main Menu"),
            new Pair("选项与设置", "Options & Settings"),
            new Pair("音量", "Volume"),
            new Pair("切换背景音乐", "Change Background Music"),
            new Pair("打开演化数据文件", "Open Evolution Data Folder"),
            new Pair("删除全部数据文件", "Delete All Data Files"),
            new Pair("帮助", "Help"),
            new Pair("制作人员", "Credits"),
            new Pair("我的游戏数据", "My Game Data"),
            new Pair("创建桌面快捷方式", "Create Desktop Shortcut"),
            new Pair("语言……", "Language…"),

            // ---------- 主菜单 ----------
            new Pair("恒星演化", "Stellar Evolution"),
            new Pair("创建宇宙", "Create Universe"),
            new Pair("行星球宇宙", "Universe of PlanetBall"),
            new Pair("退出游戏", "Quit Game"),
            new Pair("官方网站", "Official Website"),
            new Pair("版本：Test-V4.1", "Version: Test-V4.1"),

            // ---------- 演化选择 ----------
            new Pair("主序星演化", "Main-Sequence Evolution"),
            new Pair("白矮星演化", "White Dwarf Evolution"),
            new Pair("其它天体计算", "Other Celestial Calculations"),
            new Pair("请选择演化模式", "Select an Evolution Mode"),
            new Pair("开始", "Start"),

            // ---------- 其它天体计算 ----------
            new Pair("超大质量恒星", "Supermassive Star"),
            new Pair("褐矮星", "Brown Dwarf"),
            new Pair("天体质量", "Body Mass"),
            new Pair("天体类型", "Body Type"),
            new Pair("天体名称", "Body Name"),
            new Pair("范围：10 ~ 75 MJup", "Range: 10 ~ 75 MJup"),
            new Pair("计算！", "Calculate!"),
            new Pair("返回主页", "Back to Home"),

            // ---------- 演化准备 / 模拟场景 ----------
            new Pair("步长（s）", "Step (s)"),
            new Pair("步长时间（s）", "Step Time (s)"),
            new Pair("金属度（Z）", "Metallicity (Z)"),
            new Pair("金属度", "Metallicity"),
            new Pair("演化起点（MYR）", "Evolution Start (MYR)"),
            new Pair("演化到终点", "Evolve to End"),
            new Pair("演化终点（MYR）", "Evolution End (MYR)"),
            new Pair("演化步长（MYR）", "Evolution Step (MYR)"),
            new Pair("演化步长（dex）", "Evolution Step (dex)"),
            new Pair("恒星主序星阶段演化", "Main-Sequence Evolution of the Star"),
            new Pair("白矮星演化计算", "White Dwarf Evolution Calculation"),
            new Pair("恒星质量（MSun）", "Stellar Mass (MSun)"),
            new Pair("恒星名称", "Star Name"),
            new Pair("行星名称", "Planet Name"),
            new Pair("重置参数", "Reset Parameters"),
            new Pair("开始模拟", "Start Simulation"),
            new Pair("保存并返回", "Save & Back"),
            new Pair("演化设置", "Evolution Settings"),
            new Pair("新建恒星系", "New Star System"),
            new Pair("选择恒星系：太阳系", "Select Star System: Solar System"),
            new Pair("太阳系", "Solar System"),
            new Pair("系统名", "System Name"),
            new Pair("半长轴", "Semi-Major Axis"),
            new Pair("半长轴(AU)", "Semi-Major Axis (AU)"),
            new Pair("偏心率", "Eccentricity"),
            new Pair("年龄", "Age"),
            new Pair("半径", "Radius"),
            new Pair("表面温度", "Surface Temperature"),
            new Pair("光谱", "Spectrum"),
            new Pair("赫罗图", "HR Diagram"),
            new Pair("光度", "Luminosity"),
            new Pair("恒星", "Star"),
            new Pair("返回", "Back"),

            // ---------- 沙盒 ----------
            new Pair("质量", "Mass"),
            new Pair("密度", "Density"),
            new Pair("温度", "Temperature"),
            new Pair("速度", "Velocity"),
            new Pair("轨道", "Orbit"),
            new Pair("结构参数", "Structural Parameters"),
            new Pair("转动", "Rotation"),
            new Pair("平动", "Translation"),
            new Pair("自转周期", "Rotation Period"),
            new Pair("轨道周期", "Orbital Period"),
            new Pair("轨道偏角", "Orbital Inclination"),
            new Pair("中心天体", "Central Body"),
            new Pair("中心温度", "Central Temperature"),
            new Pair("中心压强", "Central Pressure"),
            new Pair("表面重力", "Surface Gravity"),
            new Pair("表面气压", "Surface Pressure"),
            new Pair("表面水深", "Surface Water Depth"),
            new Pair("表面水参数", "Surface Water Parameters"),
            new Pair("大气质量", "Atmosphere Mass"),
            new Pair("大气标高", "Atmosphere Scale Height"),
            new Pair("大气传热", "Atmosphere Heat Transfer"),
            new Pair("大气温度梯度", "Atmospheric Temperature Gradient"),
            new Pair("赤道速度", "Equatorial Velocity"),
            new Pair("氯化钠浓度", "Sodium Chloride Concentration"),
            new Pair("水沸点", "Water Boiling Point"),
            new Pair("水冰点", "Water Freezing Point"),
            new Pair("核聚变", "Nuclear Fusion"),
            new Pair("热辐射", "Thermal Radiation"),
            new Pair("辐射加热", "Radiative Heating"),
            new Pair("能量输入", "Energy Input"),
            new Pair("能量输出", "Energy Output"),
            new Pair("总输入功率", "Total Input Power"),
            new Pair("动能", "Kinetic Energy"),
            new Pair("角动能", "Angular Kinetic Energy"),
            new Pair("动量", "Momentum"),
            new Pair("角动量", "Angular Momentum"),
            new Pair("受力", "Force"),
            new Pair("总受力", "Total Force"),
            new Pair("引力结合能", "Gravitational Binding Energy"),
            new Pair("x轴分量", "X Component"),
            new Pair("y轴分量", "Y Component"),
            new Pair("z轴分量", "Z Component"),
            new Pair("此项不适用", "Not Applicable"),
            new Pair("名称", "Name"),
            new Pair("请输入场景名", "Enter Scene Name"),
            new Pair("年/秒", "years/s"),
            new Pair("天/秒", "days/s"),
            new Pair("小时/秒", "hours/s"),
            new Pair("时/秒", "h/s"),
            new Pair("分钟/秒", "min/s"),
            new Pair("分/秒", "min/s"),
            new Pair("秒/秒", "s/s"),
            new Pair("毫秒/秒", "ms/s")
        };

        // 带动态内容的文字：只翻前缀，后面拼上原内容（原内容能翻就一起翻）。
        static readonly Pair[] Prefixes =
        {
            new Pair("页码 ", "Page "),
            new Pair("范围：", "Range: "),
            new Pair("范围:", "Range: "),
            new Pair("选择恒星系：", "Select Star System: "),
            new Pair("选择恒星系:", "Select Star System: ")
        };

        /// <summary>把 root 下面所有文字切到“当前语言”。</summary>
        public static void Apply(Transform root)
        {
            if (root == null) return;

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                ApplyTo(texts[i]);
            }
        }

        /// <summary>按当前语言刷新单个文本。不在对照表里的文字原样不动。</summary>
        public static void ApplyTo(TMP_Text text)
        {
            if (text == null) return;

            string current = text.text;
            if (string.IsNullOrEmpty(current)) return;

            string target = LanguageManager.IsEnglish
                ? TranslateToEnglish(current)
                : TranslateToChinese(current);

            if (target != null && target != current)
            {
                text.text = target;
            }
        }

        /// <summary>中文 -> 英文，翻不了返回 null。</summary>
        public static string TranslateToEnglish(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            string key = value.Trim();
            if (!HasCjk(key)) return null; // 已经是英文/数字，不用翻

            for (int i = 0; i < Pairs.Length; i++)
            {
                if (Pairs[i].zh == key) return Pairs[i].en;
            }

            for (int i = 0; i < Prefixes.Length; i++)
            {
                if (key.StartsWith(Prefixes[i].zh, System.StringComparison.Ordinal))
                {
                    string rest = key.Substring(Prefixes[i].zh.Length);
                    string restEn = TranslateToEnglish(rest);
                    return Prefixes[i].en + (restEn ?? rest);
                }
            }

            return null;
        }

        /// <summary>英文 -> 中文，翻不了返回 null。</summary>
        public static string TranslateToChinese(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            string key = value.Trim();
            if (HasCjk(key)) return null; // 已经是中文

            for (int i = 0; i < Pairs.Length; i++)
            {
                if (Pairs[i].en == key) return Pairs[i].zh;
            }

            for (int i = 0; i < Prefixes.Length; i++)
            {
                if (key.StartsWith(Prefixes[i].en, System.StringComparison.Ordinal))
                {
                    string rest = key.Substring(Prefixes[i].en.Length);
                    string restZh = TranslateToChinese(rest);
                    return Prefixes[i].zh + (restZh ?? rest);
                }
            }

            return null;
        }

        static bool HasCjk(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= 0x4E00 && c <= 0x9FFF) return true;
            }
            return false;
        }
    }
}
