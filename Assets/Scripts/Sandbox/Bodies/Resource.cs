using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 资源引用（类似 Minecraft 的 ResourceLocation）：一个根目录 + 一个相对路径。
/// 它只描述"指向哪个文件"，不读取内容；需要文件时用 <see cref="GetFile"/> 拿到 FileInfo。
///
///   id   根目录："default" = 游戏内置 Resources；其它 = 模组文件夹名（相对 Mods/ 或 BuiltinMods/）
///   path 相对路径，例如 "Textures/Planet.png"
///
/// 例：
///   new Resource("Textures/Planets/Earth_Surface")        // 内置
///   new Resource("Textures/Planet.png", "MyMod")          // 模组 MyMod 根目录下
/// </summary>
public sealed class Resource : IEquatable<Resource> {
    private const string _defaultRoot = "default";

    /// <summary>根目录：default 或模组文件夹名。</summary>
    public readonly string id;

    /// <summary>相对根目录的路径。</summary>
    public readonly string path;

    public Resource(string path, string id = _defaultRoot) {
        this.path = path ?? string.Empty;
        this.id = string.IsNullOrEmpty(id) ? _defaultRoot : id.Trim();
    }

    /// <summary>是否指向内置 Resources。</summary>
    public bool IsDefault => string.Equals(id, _defaultRoot, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 解析出对应的磁盘文件：模组资源在 Mods/ 或 BuiltinMods/ 里找。
    /// 内置 Resources 没有实体文件、或文件不存在时返回 null。
    /// </summary>
    public FileInfo GetFile() {
        if (IsDefault || string.IsNullOrEmpty(path)) return null;

        foreach (string root in ModRoots()) {
            try {
                FileInfo file = new(Path.Combine(root, id,
                    path.Replace('/', Path.DirectorySeparatorChar)));
                if (file.Exists) return file;
            } catch {
                // 路径非法就试下一个根目录
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

    private static IEnumerable<string> ModRoots() {
        ModManager manager = ModManager.Instance;
        if (manager != null) {
            yield return manager.PlayerModsPath;
            yield return manager.BuiltinModsPath;
            yield break;
        }

        string data = UnityEngine.Application.dataPath;
        yield return Path.GetFullPath(Path.Combine(data, "..", "Mods"));
        yield return Path.GetFullPath(Path.Combine(data, "..", "BuiltinMods"));
    }
}
