using System;
using System.IO;
using UnityEngine;

public static class SimulationTextWriter {
    // 全局开关：是否保存演化数据
    public static bool Enabled = true;

    // 输出根目录：
    //   编辑器 → 项目根
    //   Windows/Mac/Linux 打包 → exe 所在目录
    //   Android → persistentDataPath（唯一可写路径）
    public static string RootDir {
        get {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
#elif UNITY_ANDROID
            return Application.persistentDataPath;
#else
            return Path.GetDirectoryName(Application.dataPath);
#endif
        }
    }

    public static string MainSeqDir => Path.Combine(RootDir, "simulations", "sim_text");
    public static string WhiteDwarfDir => Path.Combine(RootDir, "simulations", "sim_text_wd");

    // ~~~ 主序星 ~~~ //
    public static string BeginMainSequence(double mass, double metallicity, double startTime,
                                           double endTime, double stepSize,
                                           double tau, string name) {
        if (!Enabled) return null;

        Directory.CreateDirectory(MainSeqDir);

        string filename;
        if (!string.IsNullOrEmpty(name))
            filename = name + ".txt";
        else
            filename = $"star_M{mass:F2}_Z{metallicity:F4}_t{endTime:F0}.txt";

        string filepath = Path.Combine(MainSeqDir, filename);

        using (var sw = new StreamWriter(filepath, false, System.Text.Encoding.UTF8)) {
            sw.WriteLine("恒星演化模拟数据 - 行星球宇宙");
            sw.WriteLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sw.WriteLine($"恒星质量: {mass:F4} Msun");
            sw.WriteLine($"金属度: {metallicity:F6}");
            sw.WriteLine($"演化起点: {startTime:F2} Myr");
            sw.WriteLine($"演化终点: {endTime:F2} Myr");
            sw.WriteLine($"演化步长: {stepSize:F4} Myr");
            sw.WriteLine($"预估主序寿命: {tau:F2} Myr");
            sw.WriteLine(new string('=', 60));
            sw.WriteLine("年龄(Myr)\t类型\t光度(Lsun)\t半径(Rsun)\t表面温度(K)");
            sw.WriteLine(new string('=', 60));
        }

        return filepath;
    }

    public static void AppendMainSequenceStep(string filepath,
                                              double t, string cl,
                                              double L, double R, double T) {
        if (!Enabled) return;
        if (string.IsNullOrEmpty(filepath)) return;
        try {
            using (var sw = new StreamWriter(filepath, true, System.Text.Encoding.UTF8)) {
                sw.WriteLine($"{t:G10}\t{cl}\t{L:G7}\t{R:G7}\t{T:G10}");
            }
        }
        catch (Exception e) {
            Debug.LogWarning($"写入主序星数据失败: {e.Message}");
        }
    }

    public static void EndMainSequence(string filepath,
                                       int totalSteps, string finalCl,
                                       double finalAge, double finalL,
                                       double finalR, double finalT) {
        if (!Enabled) return;
        if (string.IsNullOrEmpty(filepath)) return;
        try {
            using (var sw = new StreamWriter(filepath, true, System.Text.Encoding.UTF8)) {
                sw.WriteLine(new string('=', 60));
                sw.WriteLine($"模拟完成于: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"总步数: {totalSteps}");
                sw.WriteLine($"最终类型: {finalCl}");
                sw.WriteLine($"最终年龄: {finalAge:F2} Myr");
                sw.WriteLine($"最终光度: {finalL:F4} Lsun");
                sw.WriteLine($"最终半径: {finalR:F4} Rsun");
                sw.WriteLine($"最终温度: {finalT:F2} K");
            }
        }
        catch (Exception e) {
            Debug.LogWarning($"写入主序星结尾失败: {e.Message}");
        }
    }

    // ~~~ 白矮星 ~~~ //
    public static string BeginWhiteDwarf(double massOrig, double massWd,
                                         double radiusKm, double stepTexpo,
                                         double stepTime, double startMyr,
                                         double endMyr, string name) {
        if (!Enabled) return null;

        Directory.CreateDirectory(WhiteDwarfDir);

        string filename;
        if (!string.IsNullOrEmpty(name))
            filename = name + ".txt";
        else
            filename = $"{massOrig:F2}_wd.txt";

        string filepath = Path.Combine(WhiteDwarfDir, filename);

        using (var sw = new StreamWriter(filepath, false, System.Text.Encoding.UTF8)) {
            sw.WriteLine("恒星演化模拟数据 - 行星球宇宙");
            sw.WriteLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sw.WriteLine($"恒星原质量: {massOrig:F4} Msun");
            sw.WriteLine($"白矮星质量: {massWd:F4} Msun");
            sw.WriteLine($"白矮星半径: {radiusKm:F1} km");
            sw.WriteLine($"演化步长指数: {stepTexpo:F3}");
            sw.WriteLine($"刷新时间: {stepTime:F2} s");
            sw.WriteLine($"演化起点: {startMyr:F1} Myr");
            sw.WriteLine($"演化终点: {endMyr:F1} Myr");
            sw.WriteLine(new string('-', 60));
            sw.WriteLine("年龄(Myr)\t表面温度(K)\t光度(Lsun)\t半径(km)");
        }

        return filepath;
    }

    public static void AppendWhiteDwarfStep(string filepath,
                                            double ageMyr, double temp,
                                            double lum, double radiusKm) {
        if (!Enabled) return;
        if (string.IsNullOrEmpty(filepath)) return;
        try {
            using (var sw = new StreamWriter(filepath, true, System.Text.Encoding.UTF8)) {
                sw.WriteLine($"{ageMyr:F6}\t{temp:F2}\t{lum:E6}\t{radiusKm:F1}");
            }
        }
        catch (Exception e) {
            Debug.LogWarning($"写入白矮星数据失败: {e.Message}");
        }
    }
}