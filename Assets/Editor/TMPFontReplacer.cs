using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Linq;

public class TMPFontReplacer : EditorWindow
{
    public TMP_FontAsset newFont;

    [MenuItem("Tools/一键替换全项目字体")]
    static void Open()
    {
        GetWindow<TMPFontReplacer>("TMP 字体替换");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("将全项目（Prefab + 场景）所有的 TMP 字体替换为以下字体：", EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);
        
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("新 TMP 字体", newFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("开始全项目替换", GUILayout.Height(40)))
        {
            if (newFont == null)
            {
                EditorUtility.DisplayDialog("错误", "请先指定新的 TMP 字体！", "确定");
                return;
            }
            ReplaceAll();
        }
        GUI.backgroundColor = Color.white;
    }

    void ReplaceAll()
    {
        // 弹窗确认
        if (!EditorUtility.DisplayDialog("确认替换", 
            "这将修改项目中所有的场景和 Prefab，建议先提交 Git 备份。\n\n确定要继续吗？", 
            "确定替换", "取消"))
        {
            return;
        }

        int prefabCount = 0;
        int sceneCount = 0;

        try
        {
            // 1. 遍历所有 Prefab
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EditorUtility.DisplayProgressBar("替换字体中...", $"正在处理 Prefab: {path}", (float)i / prefabGuids.Length);

                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                if (ReplaceInGameObject(go))
                {
                    EditorUtility.SetDirty(go);
                    prefabCount++;
                }
            }

            // 2. 遍历所有场景
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                EditorUtility.DisplayProgressBar("替换字体中...", $"正在处理场景: {path}", (float)i / sceneGuids.Length);

                // 打开场景（附加模式，不影响当前打开的场景）
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                bool sceneChanged = false;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (ReplaceInGameObject(root))
                    {
                        sceneChanged = true;
                    }
                }

                if (sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    sceneCount++;
                }
                
                EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("替换完成", $"成功！\n\n修改了 {prefabCount} 个 Prefab\n修改了 {sceneCount} 个场景", "确定");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    bool ReplaceInGameObject(GameObject go)
    {
        bool changed = false;
        // 获取所有子物体，包括隐藏的
        TMP_Text[] allTexts = go.GetComponentsInChildren<TMP_Text>(true);
        
        foreach (var text in allTexts)
        {
            if (text.font != newFont)
            {
                text.font = newFont;
                changed = true;
            }
        }
        return changed;
    }
}