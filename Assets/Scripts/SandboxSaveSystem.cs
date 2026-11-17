using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

[Serializable]
public class SandboxSaveBody {
    public string kind;
    public string name;
    public double mass;
    public double metallicity;
    public double ageMyr;
    public double rotationPeriodDays;
    public int rotationDirection = 1;
    public double axialTiltDeg;
    public double temperatureK;
    public double radius;
    public double atmosphereMassEarth;
    public double greenhouseFactor;
    public float trailColorR;
    public float trailColorG;
    public float trailColorB;
    public double luminosityLsun;
    public double positionX;
    public double positionY;
    public double positionZ;
    public double velocityX;
    public double velocityY;
    public double velocityZ;
    public double orbitInclinationDeg;
    public string surfaceTexturePath;
    public string atmosphereTexturePath;
    public bool atmosphereShell;

    // 彩蛋（长方体）
    public bool isBox;
    public double boxXKm;
    public double boxYKm;
    public double boxZKm;
    public string textureFront;
    public string textureSides;
    public string textureTopBackBottom;
    public bool ignoreGravity;
    public bool ignoreCollision;
    public bool ignoreThermal;
    public double impactGiftSun;
    public string heightMapPath;
    public double surfaceWaterDepthM;
    public double landHeightMaxM;
    public double soluteConcentrationMmolL;
    public float atmosphereColorR;
    public float atmosphereColorG;
    public float atmosphereColorB;
    public string orbitTargetName;
    public bool isOrbiting;
}

[Serializable]
public class SandboxSaveData {
    public string format = "upbsav";

    // v2：天体坐标/速度改成“相对主星”，与浮动原点无关，读档不会再被
    // “上次跟随过某颗行星”留下的超大坐标带偏。v1 老存档仍然能读。
    public int version = 2;

    public string name;
    public string savedAt;
    public double timeSpeedValue;
    public string timeUnit;

    // 主星（相对坐标的原点），只用于调试/兼容，读档时不依赖它。
    public string anchorName;
    public double anchorX;
    public double anchorY;
    public double anchorZ;
    public double anchorVx;
    public double anchorVy;
    public double anchorVz;

    public List<SandboxSaveBody> bodies = new();
}

public class SandboxSaveEntry {
    public string name;
    public string path;
    public string builtinId;
    public bool builtin;
    public DateTime savedAt;
    public Texture2D thumbnail;
}

public static class SandboxSaveSystem {
    public const string Extension = ".upbsav";
    public const string BuiltinAquarius = "default_aquarius";
    public const string BuiltinSolar = "default_solar";
    public const string BuiltinAntares = "default_antares";
    public const string BuiltinMoonsEarth = "default_moons_earth";
    public const string BuiltinMoonsJupiter = "default_moons_jupiter";
    public const string BuiltinMoonsSaturn = "default_moons_saturn";
    public const string BuiltinMoonsUranus = "default_moons_uranus";
    public const string BuiltinMoonsNeptune = "default_moons_neptune";
    private static readonly Dictionary<string, Texture2D> _thumbs = new();

    public static string SaveDirectory =>
        Path.Combine(Application.persistentDataPath, "UPBSaves");

    public static List<SandboxSaveEntry> ListEntries() {
        List<SandboxSaveEntry> entries = new() {
            new SandboxSaveEntry {
                name = "水瓶座一系",
                builtin = true,
                builtinId = BuiltinAquarius,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinAquarius)
            },
            new SandboxSaveEntry {
                name = "太阳系",
                builtin = true,
                builtinId = BuiltinSolar,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinSolar)
            },
            new SandboxSaveEntry {
                name = "心大星行星系",
                builtin = true,
                builtinId = BuiltinAntares,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinAntares)
            },
            new SandboxSaveEntry {
                name = "地球的主要卫星",
                builtin = true,
                builtinId = BuiltinMoonsEarth,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinMoonsEarth)
            },
            new SandboxSaveEntry {
                name = "木星的主要卫星",
                builtin = true,
                builtinId = BuiltinMoonsJupiter,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinMoonsJupiter)
            },
            new SandboxSaveEntry {
                name = "土星的主要卫星",
                builtin = true,
                builtinId = BuiltinMoonsSaturn,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinMoonsSaturn)
            },
            new SandboxSaveEntry {
                name = "天王星的主要卫星",
                builtin = true,
                builtinId = BuiltinMoonsUranus,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinMoonsUranus)
            },
            new SandboxSaveEntry {
                name = "海王星的主要卫星",
                builtin = true,
                builtinId = BuiltinMoonsNeptune,
                savedAt = DateTime.MinValue,
                thumbnail = BuiltinThumbnail(BuiltinMoonsNeptune)
            }
        };

        try {
            Directory.CreateDirectory(SaveDirectory);
            foreach (string path in Directory.GetFiles(SaveDirectory, "*" + Extension)) {
                try {
                    string json = File.ReadAllText(path);
                    SandboxSaveData data = JsonUtility.FromJson<SandboxSaveData>(json);
                    if (data == null || string.IsNullOrEmpty(data.name)) continue;
                    entries.Add(new SandboxSaveEntry {
                        name = data.name,
                        path = path,
                        builtin = false,
                        savedAt = File.GetLastWriteTime(path),
                        thumbnail = FileThumbnail(path)
                    });
                }
                catch (Exception e) {
                    Debug.LogWarning("[Save] 跳过损坏存档 " + path + "：" + e.Message);
                }
            }
        }
        catch (Exception e) {
            Debug.LogWarning("[Save] 读取存档目录失败：" + e.Message);
        }

        entries.Sort((a, b) => {
            if (a.builtin != b.builtin) return a.builtin ? -1 : 1;
            return b.savedAt.CompareTo(a.savedAt);
        });
        return entries;
    }

    /// <summary>存档文件路径（同名存档就是覆盖同一个文件）。</summary>
    public static string PathForName(string name) {
        return Path.Combine(SaveDirectory, Sanitize(name) + Extension);
    }

    public static bool Exists(string name) {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return File.Exists(PathForName(name));
    }

    /// <summary>
    /// 写存档：先写 .tmp，再备份旧文件为 .bak，最后原子替换。
    /// 这样即使写入过程中崩溃/断电，旧存档也还在。
    /// </summary>
    public static string SaveScene(SandboxSaveData data) {
        if (data == null || string.IsNullOrWhiteSpace(data.name)) return null;

        try {
            Directory.CreateDirectory(SaveDirectory);

            string path = PathForName(data.name);
            data.version = 2;
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture);

            string json = JsonUtility.ToJson(data, true);
            if (string.IsNullOrEmpty(json)) {
                Debug.LogError("[Save] 存档序列化失败");
                return null;
            }

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);

            if (File.Exists(path)) {
                // File.Replace 是原子的：要么旧档还在，要么新档已就位，
                // 中途断电也不会留下半截文件；旧档自动留成 .bak。
                try {
                    File.Replace(tmp, path, path + ".bak");
                }
                catch (Exception e) {
                    Debug.LogWarning("[Save] 原子替换失败，改用覆盖写入：" + e.Message);
                    File.Copy(tmp, path, true);
                    File.Delete(tmp);
                }
            }
            else {
                File.Move(tmp, path);
            }

            SaveThumbnail(path, CaptureThumbnail());
            return path;
        }
        catch (Exception e) {
            Debug.LogError("[Save] 存档写入失败：" + e.Message);
            return null;
        }
    }

    /// <summary>读取存档；主文件坏了就退回 .bak。</summary>
    public static SandboxSaveData Load(string path) {
        SandboxSaveData data = ReadFile(path);
        if (data == null && !string.IsNullOrEmpty(path)) {
            data = ReadFile(path + ".bak");
        }
        return data;
    }

    private static SandboxSaveData ReadFile(string path) {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        try {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;

            SandboxSaveData data = JsonUtility.FromJson<SandboxSaveData>(json);
            if (data == null) return null;
            if (!string.IsNullOrEmpty(data.format) && data.format != "upbsav") return null;
            if (data.bodies == null) data.bodies = new List<SandboxSaveBody>();
            return data;
        }
        catch (Exception e) {
            Debug.LogWarning("[Save] 存档解析失败 " + path + "：" + e.Message);
            return null;
        }
    }

    public static bool IsFinite(double value) {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public static double Sanitize(double value, double fallback = 0.0) {
        return IsFinite(value) ? value : fallback;
    }

    public static void Delete(string path) {
        if (string.IsNullOrEmpty(path)) return;
        foreach (string file in new[] { path, path + ".bak", path + ".tmp" }) {
            try { if (File.Exists(file)) File.Delete(file); }
            catch (Exception e) { Debug.LogWarning("[Save] 删除失败 " + file + "：" + e.Message); }
        }
        string thumb = ThumbnailPath(path);
        if (File.Exists(thumb)) File.Delete(thumb);
    }

    public static void SetBuiltinThumbnail(string id, Texture2D tex) {
        if (tex == null) return;
        _thumbs[id] = tex;
        try {
            Directory.CreateDirectory(SaveDirectory);
            string file = Path.Combine(SaveDirectory, id + ".png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
        }
        catch (Exception e) {
            Debug.LogWarning("[Save] 默认缩略图写入失败：" + e.Message);
        }
    }

    public static Texture2D BuiltinThumbnail(string id) {
        if (_thumbs.TryGetValue(id, out Texture2D tex) && tex != null) return tex;
        Color a = id == BuiltinSolar
            ? new Color(0.16f, 0.10f, 0.02f)
            : new Color(0.04f, 0.03f, 0.12f);
        Color b = id == BuiltinSolar
            ? new Color(1f, 0.72f, 0.20f)
            : new Color(0.52f, 0.36f, 0.95f);
        tex = MakeGradient(a, b);
        _thumbs[id] = tex;
        return tex;
    }

    public static Texture2D FileThumbnail(string savePath) {
        string path = ThumbnailPath(savePath);
        if (!File.Exists(path)) return BuiltinThumbnail("file_default");
        try {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes)) return tex;
        }
        catch { }
        return BuiltinThumbnail("file_default");
    }

    public static Texture2D CaptureThumbnail() {
        try {
            int width = 300;
            int height = 180;
            RenderTexture rt = new(width, height, 24);
            Camera cam = Camera.main;
            if (cam != null) {
                RenderTexture prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prev;
                RenderTexture.active = rt;
                Texture2D tex = new(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                rt.Release();
                UnityEngine.Object.Destroy(rt);
                return tex;
            }
        }
        catch (Exception e) {
            Debug.LogWarning("[Save] 缩略图截取失败：" + e.Message);
        }
        return MakeGradient(new Color(0.06f, 0.07f, 0.10f), new Color(0.35f, 0.42f, 0.55f));
    }

    private static void SaveThumbnail(string savePath, Texture2D tex) {
        if (tex == null) return;
        try { File.WriteAllBytes(ThumbnailPath(savePath), tex.EncodeToPNG()); }
        catch (Exception e) { Debug.LogWarning("[Save] 缩略图写入失败：" + e.Message); }
    }

    private static string ThumbnailPath(string savePath) =>
        Path.ChangeExtension(savePath, ".png");

    private static string Sanitize(string name) {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "未命名场景" : name.Trim();
    }

    private static Texture2D MakeGradient(Color a, Color b) {
        const int w = 300;
        const int h = 180;
        Texture2D tex = new(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) {
            float t = y / (float)(h - 1);
            Color c = Color.Lerp(a, b, t);
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }
}
