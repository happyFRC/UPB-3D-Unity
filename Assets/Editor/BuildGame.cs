using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildGame {
    // 打包输出不再是写死的绝对路径：
    //   默认 <项目根>/Builds/行星球宇宙Test-V4.1/（.gitignore 已忽略 /Builds/）
    //   需要换位置时设环境变量 UPB_BUILD_DIR（作为根目录，下面仍会再套一层产品名-版本）
    private const string ProductName = "行星球宇宙Test";
    private const string BundleVersion = "4.1";
    private const string OutputRootEnv = "UPB_BUILD_DIR";
    private const string DefaultOutputRoot = "Builds";
    private const string IconPath = "Assets/Resources/icon.png";

    private static string ProjectRoot =>
        Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    private static string BuildName => $"{ProductName}-V{BundleVersion}";

    private static string OutputFolder {
        get {
            string custom = System.Environment.GetEnvironmentVariable(OutputRootEnv);
            string root = string.IsNullOrWhiteSpace(custom)
                ? Path.Combine(ProjectRoot, DefaultOutputRoot)
                : custom;
            return Path.Combine(root, BuildName);
        }
    }

    private static string OutputExe => BuildName + ".exe";

    private static void EnsureAlwaysIncludedShaders() {
        string[] shaderPaths = {
            "Assets/Shaders/SandboxPlanet.shader",
            "Assets/Shaders/OrbitLine.shader",
            "Assets/Shaders/TrailLine.shader",
            "Assets/Shaders/StarPoint3D.shader",
            "Assets/Shaders/OrbitShader.shader",
            "Assets/Resources/Shaders/SandboxPreview.shader",
            "Assets/Resources/Shaders/StarLightPoint.shader"
        };

        UnityEngine.Object[] assets =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;

        SerializedObject settings = new(assets[0]);
        SerializedProperty list = settings.FindProperty("m_AlwaysIncludedShaders");
        if (list == null) return;

        foreach (string path in shaderPaths) {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) continue;

            bool exists = false;
            for (int i = 0; i < list.arraySize; i++) {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) {
                    exists = true;
                    break;
                }
            }
            if (exists) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    public static void Build() {
        EnsureAlwaysIncludedShaders();
        PlayerSettings.bundleVersion = BundleVersion;
        PlayerSettings.productName = ProductName;
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null) {
            PlayerSettings.SetIconsForTargetGroup(
                BuildTargetGroup.Standalone, new[] { icon });
        }

        Directory.CreateDirectory(OutputFolder);

        EditorBuildSettingsScene[] all = EditorBuildSettings.scenes;
        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in all) {
            if (scene != null && scene.enabled) scenes.Add(scene.path);
        }

        string path = Path.Combine(OutputFolder, OutputExe);
        BuildPlayerOptions options = new() {
            scenes = scenes.ToArray(),
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("[Build] result=" + summary.result +
                  " errors=" + summary.totalErrors +
                  " warnings=" + summary.totalWarnings +
                  " size=" + summary.totalSize +
                  " output=" + path);
        if (summary.result != BuildResult.Succeeded) {
            throw new System.Exception("[Build] failed: " + summary.result);
        }

        CopyBuiltinMods(path);
    }

    /// <summary>
    /// 把工程根目录的 BuiltinMods/ 拷进打包结果：
    ///   内置模组源码 → &lt;输出目录&gt;/&lt;exe名&gt;_Data/StreamingAssets/Mods/&lt;模组&gt;/
    /// 运行时 ModManager 从 Application.streamingAssetsPath/Mods 读。
    /// （内置模组源码不能放 Assets/StreamingAssets，否则 Unity 会把 .cs 当游戏脚本编译。）
    /// </summary>
    private static void CopyBuiltinMods(string exePath) {
        string source = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BuiltinMods"));
        if (!Directory.Exists(source)) {
            Debug.Log("[Build] 没有 BuiltinMods/，跳过内置模组");
            return;
        }

        string dataFolder = Path.ChangeExtension(exePath, null) + "_Data";
        string target = Path.Combine(dataFolder, "StreamingAssets", "Mods");
        Directory.CreateDirectory(target);

        int copied = 0;
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories)) {
            string relative = file.Substring(source.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // bin/obj 是 IDE 编译产物，不能塞进发行包。
            string normalized = "/" + relative.Replace('\\', '/') + "/";
            if (normalized.Contains("/bin/") || normalized.Contains("/obj/")) continue;

            string destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? target);
            File.Copy(file, destination, true);
            copied++;
        }

        Debug.Log("[Build] 内置模组已复制 " + copied + " 个文件到 " + target);
    }
}
