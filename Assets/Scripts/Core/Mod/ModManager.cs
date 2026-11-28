using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>desc.json 的描述结构。除 name 外都可以省略。</summary>
[Serializable]
public class ModDescription {
    public string name;
    public string author;
    public string version;
    public string description;
}

public enum ModLoadState {
    Disabled = 0,
    Loaded = 1,
    Failed = 2,
    Skipped = 3
}

/// <summary>一个模组的运行时信息（目录名、描述、图标、开关状态、加载结果）。</summary>
public class ModEntry {
    public string id;                 // 文件夹名，作为唯一标识
    public string folder;             // 绝对路径
    public bool isBuiltin;            // 来自 StreamingAssets 的内置模组
    public string displayName;
    public string author;
    public string version;
    public string description;
    public Texture2D icon;
    public bool enabled = true;
    public ModLoadState state = ModLoadState.Disabled;
    public string error;
    public UPBMod instance;

    public string StateText => state switch {
        ModLoadState.Loaded => "已加载",
        ModLoadState.Failed => "加载失败",
        ModLoadState.Skipped => "已跳过",
        _ => "已关闭"
    };
}

/// <summary>
/// 模组管理器。两个来源：
///   1) 内置模组：Assets/StreamingAssets/Mods/  （打包后是 &lt;游戏&gt;_Data/StreamingAssets/Mods）
///   2) 玩家模组：&lt;游戏根目录&gt;/Mods/            （编辑器里是工程根目录的 Mods/）
///
/// 每个模组目录：
///   ModName/
///     ModName.cs      ← 主文件：必须与文件夹同名，里面必须有实现 UPBMod 的类
///     任意其它 .cs     ← 会一起编译，供主文件引用；里面即使实现了 UPBMod 也会被忽略
///     ModName.csproj  ← IDE 补全用，不参与编译
///     icon.png
///     desc.json       ← name / author / version / description
///
/// 所有模组**默认关闭**，开关存在 UPB-data/mods.json（玩家数据目录，已 gitignore），下次启动生效。
/// 同名时内置模组优先，玩家目录里的同名文件夹会被跳过。
/// </summary>
public class ModManager : MonoBehaviour {
    public static ModManager Instance { get; private set; }

    public string modsFolderName = "Mods";

    /// <summary>按文件夹名找模组目录；没有返回 null。</summary>
    public static string FolderOf(string id) {
        if (Instance == null || string.IsNullOrEmpty(id)) return null;
        foreach (ModEntry entry in Instance.Mods) {
            if (entry != null && string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase)) {
                return entry.folder;
            }
        }
        return null;
    }

    /// <summary>当前所有模组（没有实例时为空）。</summary>
    public static IReadOnlyList<ModEntry> AllMods() =>
        Instance != null ? Instance.Mods : Array.Empty<ModEntry>();

    public readonly List<ModEntry> Mods = new();
    public int FailedCount { get; private set; }
    public bool Loaded { get; private set; }

    /// <summary>任意模组状态变化（开关、加载完成）时触发，模组面板据此刷新。</summary>
    public event Action Changed;

    /// <summary>旧版开关记录（PlayerPrefs），只在迁移时读一次。</summary>
    private const string EnabledPrefPrefix = "UPB.Mod.Enabled.";

    private static List<MetadataReference> _references;

    // ~~~ 开关记录 ~~~
    // 存在 <数据根>/UPB-data/mods.json，跟着玩家数据一起被 .gitignore 掉。
    private static readonly HashSet<string> _enabledIds = new(StringComparer.OrdinalIgnoreCase);
    private static bool _stateLoaded;

    private static string DataRoot {
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

    /// <summary>开关记录文件：UPB-data/mods.json。</summary>
    public static string StatePath => Path.Combine(DataRoot, "UPB-data", "mods.json");

    [Serializable]
    private class ModStateData {
        public List<string> enabled = new();
    }

    private static void LoadEnabledState() {
        if (_stateLoaded) return;
        _stateLoaded = true;

        try {
            if (!File.Exists(StatePath)) return;
            ModStateData data = JsonUtility.FromJson<ModStateData>(File.ReadAllText(StatePath));
            if (data?.enabled == null) return;
            foreach (string id in data.enabled) {
                if (!string.IsNullOrEmpty(id)) _enabledIds.Add(id);
            }
        } catch (Exception e) {
            Debug.LogWarning($"[Mods] 读取开关记录失败: {e.Message}");
        }
    }

    private static void SaveEnabledState() {
        try {
            string dir = Path.GetDirectoryName(StatePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            ModStateData data = new() { enabled = new List<string>(_enabledIds) };
            File.WriteAllText(StatePath, JsonUtility.ToJson(data, true));
        } catch (Exception e) {
            Debug.LogWarning($"[Mods] 保存开关记录失败: {e.Message}");
        }
    }

    /// <summary>模组是否开启（默认关闭）。旧版 PlayerPrefs 记录会自动迁移过来。</summary>
    public static bool IsEnabled(string id) {
        LoadEnabledState();
        if (_enabledIds.Contains(id)) return true;

        string legacyKey = EnabledPrefPrefix + id;
        if (PlayerPrefs.HasKey(legacyKey)) {
            bool on = PlayerPrefs.GetInt(legacyKey, 0) != 0;
            PlayerPrefs.DeleteKey(legacyKey);
            PlayerPrefs.Save();
            if (on) {
                _enabledIds.Add(id);
                SaveEnabledState();
            }
            return on;
        }

        return false;
    }

    /// <summary>玩家的模组目录（与 exe 同级）。</summary>
    public string PlayerModsPath => Path.GetFullPath(
        Path.Combine(Application.dataPath, "..", modsFolderName));

    /// <summary>
    /// 内置模组目录。
    /// 编辑器里读工程根目录的 BuiltinMods/（不能放 Assets/StreamingAssets：Unity 会把那里的
    /// .cs 当成游戏脚本一起编译）；打包时由 BuildGame 把它整个拷进
    /// &lt;游戏&gt;_Data/StreamingAssets/Mods/，运行时从那里读。
    /// </summary>
    public string BuiltinModsPath {
        get {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BuiltinMods"));
#else
            return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, modsFolderName));
#endif
        }
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        LoadAll();
        ReportResult();
    }

    /// <summary>扫描并加载全部模组（内置优先，然后玩家目录）。</summary>
    public void LoadAll() {
        Mods.Clear();
        FailedCount = 0;
        Loaded = false;

        try {
            // 玩家目录不一定存在，第一次运行时自动建好，方便玩家直接往里丢模组。
            Directory.CreateDirectory(PlayerModsPath);
        } catch (Exception e) {
            Debug.LogWarning($"[Mods] 无法创建玩家模组目录 {PlayerModsPath}: {e.Message}");
        }

        List<MetadataReference> references = BuildReferences();
        LoadFolder(BuiltinModsPath, true, references);
        LoadFolder(PlayerModsPath, false, references);

        Loaded = true;
        Changed?.Invoke();
        Debug.Log($"[Mods] 加载完成：{Mods.Count} 个模组，失败 {FailedCount} 个；" +
                  $"内置 {BuiltinModsPath}，玩家 {PlayerModsPath}");
    }

    /// <summary>开关模组（下次启动生效）。</summary>
    public void SetEnabled(ModEntry entry, bool enabled) {
        if (entry == null) return;
        entry.enabled = enabled;

        if (enabled) _enabledIds.Add(entry.id);
        else _enabledIds.Remove(entry.id);
        SaveEnabledState();
        Changed?.Invoke();
    }

    /// <summary>在资源管理器里打开玩家模组目录（按钮用）。</summary>
    public void RevealPlayerModsFolder() {
        string path = PlayerModsPath;
        try {
            Directory.CreateDirectory(path);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(path);
#else
            if (Application.platform == RuntimePlatform.WindowsPlayer) {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
            } else if (Application.platform == RuntimePlatform.OSXPlayer) {
                Process.Start("open", "\"" + path + "\"");
            } else {
                Process.Start("xdg-open", "\"" + path + "\"");
            }
#endif
        } catch (Exception e) {
            Debug.LogWarning($"[Mods] 打开模组目录失败: {e.Message}");
            FloatingText.Show("打开模组文件夹失败：" + e.Message, new Color(1f, 0.4f, 0.4f, 1f), 3f);
        }
    }

    private void ReportResult() {
        if (Mods.Count == 0) return;

        if (FailedCount > 0) {
            FloatingText.Show("有模组加载失败，请查看日志", new Color(1f, 0.25f, 0.25f, 1f), 4f);
        } else if (Mods.Any(entry => entry.state == ModLoadState.Loaded)) {
            // 全部关闭时没有实际加载任何模组，不打扰玩家。
            FloatingText.Show("模组加载成功！", new Color(1f, 0.92f, 0.3f, 1f), 3f);
        }
    }

    private void LoadFolder(string root, bool isBuiltin, List<MetadataReference> references) {
        if (!Directory.Exists(root)) return;

        foreach (string folder in Directory.GetDirectories(root)
                     .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)) {
            ModEntry entry = CreateEntry(folder, isBuiltin);
            if (entry == null) continue;

            if (Mods.Any(existing => string.Equals(existing.id, entry.id, StringComparison.OrdinalIgnoreCase))) {
                Debug.LogWarning($"[Mods] {entry.id}: 与已扫描的模组同名，跳过 {folder}");
                entry.state = ModLoadState.Skipped;
                entry.error = "与内置模组同名，已跳过";
                Mods.Add(entry);
                FailedCount++;
                continue;
            }

            Mods.Add(entry);
            if (!entry.enabled) {
                entry.state = ModLoadState.Disabled;
                continue;
            }

            switch (TryLoad(entry, references)) {
                case LoadOutcome.Ok:
                    entry.state = ModLoadState.Loaded;
                    break;
                case LoadOutcome.Skipped:
                    entry.state = ModLoadState.Skipped;
                    FailedCount++;
                    break;
                default:
                    entry.state = ModLoadState.Failed;
                    FailedCount++;
                    break;
            }
        }
    }

    private ModEntry CreateEntry(string folder, bool isBuiltin) {
        string id = Path.GetFileName(folder);
        if (string.IsNullOrEmpty(id)) return null;

        ModEntry entry = new() {
            id = id,
            folder = folder,
            isBuiltin = isBuiltin,
            displayName = id,
            enabled = IsEnabled(id)
        };

        string descPath = Path.Combine(folder, "desc.json");
        if (File.Exists(descPath)) {
            try {
                ModDescription desc = JsonUtility.FromJson<ModDescription>(
                    File.ReadAllText(descPath));
                if (desc != null) {
                    if (!string.IsNullOrWhiteSpace(desc.name)) entry.displayName = desc.name.Trim();
                    entry.author = desc.author;
                    entry.version = desc.version;
                    entry.description = desc.description;
                }
            } catch (Exception e) {
                Debug.LogWarning($"[Mods] {id}: desc.json 解析失败（{e.Message}），使用文件夹名作为显示名");
            }
        }

        string iconPath = Path.Combine(folder, "icon.png");
        if (File.Exists(iconPath)) {
            try {
                Texture2D texture = new(2, 2);
                if (texture.LoadImage(File.ReadAllBytes(iconPath))) entry.icon = texture;
                else Debug.LogWarning($"[Mods] {id}: icon.png 读取失败");
            } catch (Exception e) {
                Debug.LogWarning($"[Mods] {id}: icon.png 读取失败（{e.Message}）");
            }
        }

        return entry;
    }

    private enum LoadOutcome {
        Ok,
        Failed,
        Skipped
    }

    private LoadOutcome TryLoad(ModEntry entry, List<MetadataReference> references) {
        // 子文件夹里的 .cs 也一起编译（bin/obj 是 IDE 产物，跳过）。
        string[] sources = Directory.GetFiles(entry.folder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildArtifact(entry.folder, file))
            .ToArray();
        if (sources.Length == 0) {
            entry.error = "目录下没有 .cs 文件";
            Debug.LogError($"[Mods] {entry.id}: {entry.error}");
            return LoadOutcome.Failed;
        }

        // 主文件必须与文件夹同名（放在模组根目录）；没有就直接跳过，不编译。
        string mainFile = Path.GetFullPath(Path.Combine(entry.folder, entry.id + ".cs"));
        if (!File.Exists(mainFile)) {
            entry.error = $"缺少主文件 {entry.id}.cs，已跳过";
            Debug.LogWarning($"[Mods] {entry.id}: {entry.error}");
            return LoadOutcome.Skipped;
        }

        try {
            SyntaxTree[] trees = sources
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Select(f => CSharpSyntaxTree.ParseText(
                    File.ReadAllText(f),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
                    path: f))
                .ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                "Mod_" + entry.id,
                trees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using MemoryStream stream = new();
            Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
            if (!result.Success) {
                string[] errors = result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToArray();
                entry.error = errors.Length > 0 ? errors[0] : "编译失败";
                Debug.LogError($"[Mods] {entry.id} 编译失败:\n{string.Join("\n", errors)}");
                return LoadOutcome.Failed;
            }

            stream.Seek(0, SeekOrigin.Begin);
            Assembly assembly = Assembly.Load(stream.ToArray());

            // 只认主文件里定义的 UPBMod 实现，其它文件里的实现一律忽略。
            Type entryType = FindEntryType(mainFile, compilation, assembly);
            if (entryType == null) {
                entry.error = $"主文件 {entry.id}.cs 里没有实现 UPBMod 的类，已跳过";
                Debug.LogWarning($"[Mods] {entry.id}: {entry.error}");
                return LoadOutcome.Skipped;
            }

            entry.instance = (UPBMod)Activator.CreateInstance(entryType);
            entry.instance.Startup(new UPBLogger(entry.displayName));
            Debug.Log($"[Mods] 已加载 {(entry.isBuiltin ? "[内置] " : "")}{entry.displayName}（{entryType.Name}）");
            return LoadOutcome.Ok;
        } catch (Exception e) {
            entry.error = e.InnerException != null ? e.InnerException.Message : e.Message;
            Debug.LogError($"[Mods] {entry.id} 加载异常: {entry.error}\n{e}");
            return LoadOutcome.Failed;
        }
    }

    /// <summary>
    /// 在主文件（&lt;文件夹名&gt;.cs）里找实现 UPBMod 的类；
    /// 主文件之外的实现不参与选择。
    /// </summary>
    private static Type FindEntryType(string mainFile, CSharpCompilation compilation,
            Assembly assembly) {
        foreach (SyntaxTree tree in compilation.SyntaxTrees) {
            if (!string.Equals(Path.GetFullPath(tree.FilePath), mainFile,
                    StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (TypeDeclarationSyntax declaration in tree.GetRoot()
                         .DescendantNodes().OfType<TypeDeclarationSyntax>()) {
                if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol) continue;
                if (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract) continue;
                if (!ImplementsModInterface(symbol)) continue;

                Type runtimeType = ResolveType(assembly, symbol);
                if (runtimeType != null && !runtimeType.IsAbstract &&
                    typeof(UPBMod).IsAssignableFrom(runtimeType) &&
                    runtimeType.GetConstructor(Type.EmptyTypes) != null) {
                    return runtimeType;
                }
            }
        }
        return null;
    }

    private static bool ImplementsModInterface(INamedTypeSymbol symbol) {
        foreach (INamedTypeSymbol iface in symbol.AllInterfaces) {
            if (iface.ToDisplayString() == "UPBMod") return true;
        }
        return false;
    }

    private static Type ResolveType(Assembly assembly, INamedTypeSymbol symbol) {
        string displayName = symbol.ToDisplayString();
        Type type = assembly.GetType(displayName);
        if (type != null) return type;

        // 嵌套类在反射里是 "Ns.Outer+Inner"，ToDisplayString 用的是点号。
        return assembly.GetTypes().FirstOrDefault(t =>
            string.Equals(t.FullName?.Replace('+', '.'), displayName, StringComparison.Ordinal));
    }

    /// <summary>bin/obj 这类 IDE 编译产物不参与模组编译。</summary>
    private static bool IsBuildArtifact(string root, string file) {
        string relative = file.Substring(root.Length).Replace('\\', '/');
        return relative.Contains("/bin/") || relative.Contains("/obj/");
    }

    /// <summary>编译模组用的引用集合：当前进程里已加载的程序集。</summary>
    private static List<MetadataReference> BuildReferences() {
        if (_references != null) return _references;

        _references = new List<MetadataReference>();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (assembly.IsDynamic) continue;
            string location;
            try { location = assembly.Location; } catch { continue; }
            if (string.IsNullOrEmpty(location) || !seen.Add(location)) continue;
            try { _references.Add(MetadataReference.CreateFromFile(location)); } catch { /* 忽略不可读程序集 */ }
        }
        return _references;
    }
}
