using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildGame {
    private const string OutputFolder =
        @"C:\Users\14806\Desktop\UPBUnity\行星球宇宙Test-V4.1-等待261101发布-非最终定稿";
    private const string OutputExe = "行星球宇宙Test-V4.1.exe";
    private const string IconPath = "Assets/Resources/icon.png";

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
        PlayerSettings.bundleVersion = "4.1";
        PlayerSettings.productName = "行星球宇宙Test";
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
    }
}
