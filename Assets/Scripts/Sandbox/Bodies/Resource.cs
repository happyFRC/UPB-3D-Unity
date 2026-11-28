using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// 资源引用：一个名字空间 + 一个相对路径。
/// 只描述"指向哪个文件"，不读取内容。
///
/// 解析规则（模组 A 里写代码时）：
///   new Resource("Textures/apa.png", "Test")   → A/Resource/Test/Textures/apa.png
///   new Resource("Textures/apa.png")           → 内置 Resources（id 默认 "default"）
///
/// 覆盖默认资源：A/Resource/default/Textures/Planets/Earth.png
/// 会覆盖 Assets/Resources/Textures/Planets/Earth.png。
/// </summary>
public sealed class Resource : IEquatable<Resource> {
    private const string _defaultRoot = "default";
    private const string _resourceFolder = "Resource";

    /// <summary>相对名字空间目录的路径。</summary>
    public readonly string path;

    /// <summary>名字空间："default" = 内置 Resources；其它 = 模组 Resource/&lt;id&gt; 目录。</summary>
    public readonly string id;

    /// <summary>创建它的模组文件夹名；游戏自身代码创建时为 null。</summary>
    public readonly string owner;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Resource(string path, string id = _defaultRoot) {
        this.path = path ?? string.Empty;
        this.id = string.IsNullOrEmpty(id) ? _defaultRoot : id.Trim();
        owner = OwnerFromCallingAssembly();
    }

    /// <summary>是否指向内置 Resources（会优先被模组的 Resource/default 覆盖）。</summary>
    public bool IsDefault => string.Equals(id, _defaultRoot, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 解析出实际文件：
    ///   default → 各模组的 Resource/default/&lt;path&gt;（覆盖），没有就返回 null（用内置 Resources）
    ///   其它    → 自己的 Resource/&lt;id&gt;/&lt;path&gt;，其次其它模组的同名目录
    /// </summary>
    public FileInfo GetFile() {
        if (string.IsNullOrEmpty(path)) return null;

        foreach (string folder in CandidateModFolders()) {
            string root = Path.Combine(folder, _resourceFolder, IsDefault ? _defaultRoot : id);
            try {
                FileInfo file = new(Path.Combine(root,
                    path.Replace('/', Path.DirectorySeparatorChar)));
                if (file.Exists) return file;
            } catch {
                // 路径非法就试下一个模组
            }
        }
        return null;
    }

    /// <summary>序列化用："id:path"；内置资源就是纯路径。</summary>
    public override string ToString() => IsDefault ? path : id + ":" + path;

    public bool Equals(Resource other) {
        if (other is null) return false;
        return string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj) => Equals(obj as Resource);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());

    public static bool operator ==(Resource left, Resource right) => Equals(left, right);
    public static bool operator !=(Resource left, Resource right) => !Equals(left, right);

    /// <summary>自己的模组优先，然后玩家模组 → 内置模组。</summary>
    private IEnumerable<string> CandidateModFolders() {
        if (!string.IsNullOrEmpty(owner)) {
            string own = ModManager.FolderOf(owner);
            if (!string.IsNullOrEmpty(own)) yield return own;
        }

        if (ModManager.Instance == null) {
            // 编辑器里还没跑 ModManager（例如编辑器工具）时，直接扫磁盘上的模组目录。
            foreach (string folder in ScanModFolders()) {
                if (folder == owner) continue;
                yield return folder;
            }
            yield break;
        }

        foreach (UPBModEntry entry in ModManager.AllMods()) {
            if (entry == null || entry.id == owner || string.IsNullOrEmpty(entry.folder)) continue;
            yield return entry.folder;
        }
    }

    private static IEnumerable<string> ScanModFolders() {
        string data = Application.dataPath;
        string[] roots = {
            Path.GetFullPath(Path.Combine(data, "..", "Mods")),
            Path.GetFullPath(Path.Combine(data, "..", "BuiltinMods"))
        };

        foreach (string root in roots) {
            string[] folders;
            try {
                if (!Directory.Exists(root)) continue;
                folders = Directory.GetDirectories(root);
            } catch {
                continue;
            }
            foreach (string folder in folders) yield return folder;
        }
    }

    /// <summary>模组的程序集叫 Mod_&lt;文件夹名&gt;，用它判断资源属于哪个模组。</summary>
    private static string OwnerFromCallingAssembly() {
        try {
            string name = Assembly.GetCallingAssembly().GetName().Name;
            if (!string.IsNullOrEmpty(name) &&
                name.StartsWith("Mod_", StringComparison.Ordinal)) {
                return name.Substring(4);
            }
        } catch {
            // 拿不到调用程序集就当游戏代码
        }
        return null;
    }
}
