using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行星创建卡使用一个隐藏的 3D 球体 + 相机渲染成缩略图，
/// 而不是把赤道贴图直接平铺在 RawImage 上。
/// </summary>
public static class SandboxPlanetCardRenderer {
    private const int Size = 256;
    private static readonly Dictionary<string, Texture2D> _cache = new();
    private static Camera _camera;
    private static Light _light;
    private static MeshRenderer _sphere;
    private static Material _material;
    private static RenderTexture _target;
    private static Transform _root;
    private static GameObject _box;
    private static Material _boxMaterial;
    private static readonly Dictionary<string, Texture2D> _boxCache = new();

    public static Texture2D Render(string texturePath) {
        if (string.IsNullOrEmpty(texturePath)) return null;
        if (_cache.TryGetValue(texturePath, out Texture2D cached) && cached != null) {
            return cached;
        }

        // 优先用预烘焙的 256×256 卡片图：列表里只需要缩略图，
        // 没必要为了它把 4K 地表贴图加载进显存。
        // 模组贴图没有预烘焙图，直接用源图渲染。
        if (!SandboxPlanet.IsModResource(texturePath)) {
            Texture2D baked = Resources.Load<Texture2D>(
                "Textures/PlanetCards/" + System.IO.Path.GetFileName(texturePath));
            if (baked != null) {
                _cache[texturePath] = baked;
                return baked;
            }
        }

        Texture2D source = SandboxPlanet.LoadTexture(texturePath);
        if (source == null) return null;
        EnsureScene();

        _material.SetTexture("_SurfaceMap", source);
        _material.SetColor("_BaseColor", Color.white);
        _material.SetFloat("_AtmosphereStrength", 0f);

        // 预览相机离真实恒星非常远，必须临时关掉沙盘恒星光照，
        // 让自定义 Shader 走方向光分支；渲染完再恢复全局状态。
        float previousStarCount = Shader.GetGlobalFloat("_SandboxStarCount");
        float previousFallbackOn = Shader.GetGlobalFloat("_SandboxFallbackLightOn");
        Vector4 previousFallbackDir = Shader.GetGlobalVector("_SandboxFallbackLightDir");
        Color previousFallbackColor = Shader.GetGlobalColor("_SandboxFallbackLightColor");
        Shader.SetGlobalFloat("_SandboxStarCount", 0f);
        Shader.SetGlobalFloat("_SandboxFallbackLightOn", 1f);
        Shader.SetGlobalVector("_SandboxFallbackLightDir",
            new Vector4(-0.45f, 0.65f, -0.6f, 0f));
        Shader.SetGlobalColor("_SandboxFallbackLightColor", Color.white);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _target;
        _camera.Render();
        Texture2D result = new(Size, Size, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        result.Apply(false, false);
        result.name = "PlanetCard_" + texturePath;
        RenderTexture.active = previous;

        Shader.SetGlobalFloat("_SandboxStarCount", previousStarCount);
        Shader.SetGlobalFloat("_SandboxFallbackLightOn", previousFallbackOn);
        Shader.SetGlobalVector("_SandboxFallbackLightDir", previousFallbackDir);
        Shader.SetGlobalColor("_SandboxFallbackLightColor", previousFallbackColor);

        _cache[texturePath] = result;
        return result;
    }

    /// <summary>
    /// 彩蛋卡片：用一个旋转过的长方体 + 三张贴图渲染缩略图，
    /// 这样创建面板里看到的就是长方体本身，而不是球。
    /// </summary>
    public static Texture2D RenderBox(string front, string sides, string top,
            double dxKm = 1.0, double dyKm = 1.0, double dzKm = 1.0) {
        if (string.IsNullOrEmpty(front)) return null;

        string key = front + "|" + sides + "|" + top + "|" + dxKm + "|" + dyKm + "|" + dzKm;
        if (_boxCache.TryGetValue(key, out Texture2D cached) && cached != null) {
            return cached;
        }

        EnsureScene();
        if (_box == null) {
            Shader shader = Resources.Load<Shader>("Shaders/EasterEggBox");
            if (shader == null) shader = Shader.Find("UPB/EasterEggBox");
            if (shader == null) return null;

            _boxMaterial = new Material(shader) { name = "EasterEggCardMaterial" };
            _box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _box.name = "PreviewBox";
            _box.layer = 31;
            _box.transform.SetParent(_root, false);
            _box.transform.localPosition = Vector3.zero;
            _box.transform.localRotation = Quaternion.Euler(12f, 212f, 8f);
            Collider collider = _box.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            MeshRenderer renderer = _box.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = _boxMaterial;
        }

        double maxDim = System.Math.Max(dxKm, System.Math.Max(dyKm, dzKm));
        if (maxDim <= 0.0) maxDim = 1.0;
        _box.transform.localScale = new Vector3(
            (float)(dxKm / maxDim), (float)(dyKm / maxDim), (float)(dzKm / maxDim)) * 1.15f;
        _boxMaterial.SetTexture("_FrontTex", SafeTexture(front));
        _boxMaterial.SetTexture("_SideTex", SafeTexture(sides));
        _boxMaterial.SetTexture("_TopTex", SafeTexture(top));

        if (_sphere != null) _sphere.gameObject.SetActive(false);
        _box.SetActive(true);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _target;
        _camera.Render();
        Texture2D result = new(Size, Size, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        result.Apply(false, false);
        result.name = "EasterEggCard_" + System.IO.Path.GetFileName(front);
        RenderTexture.active = previous;

        _box.SetActive(false);
        if (_sphere != null) _sphere.gameObject.SetActive(true);

        _boxCache[key] = result;
        return result;
    }

    private static Texture2D SafeTexture(string path) {
        if (string.IsNullOrWhiteSpace(path)) return Texture2D.whiteTexture;
        Texture2D tex = SandboxPlanet.LoadTexture(path);
        return tex != null ? tex : Texture2D.whiteTexture;
    }

    private static void EnsureScene() {
        if (_camera != null) return;

        GameObject root = new("PlanetCardPreviewRoot");
        root.transform.position = new Vector3(100000f, 100000f, 100000f);
        _root = root.transform;
        int layer = 31;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "PreviewSphere";
        sphere.transform.SetParent(root.transform, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one;
        sphere.layer = layer;
        _sphere = sphere.GetComponent<MeshRenderer>();
        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);

        // 不使用 URP/Lit：Player 里 URP Lit 可能被裁剪掉，导致创建卡片时 shader 为 null。
        // 改用已经强制打进包里的自定义行星 Shader。
        Shader shader = Shader.Find("UPB/SandboxPlanet");
        if (shader == null) shader = Shader.Find("UPB/SandboxPreview");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        _material = new Material(shader) { name = "PlanetCardPreviewMaterial" };
        _material.SetFloat("_AtmosphereStrength", 0f);
        _material.SetColor("_AmbientColor", new Color(0.03f, 0.035f, 0.045f, 1f));
        _sphere.material = _material;

        // 预览球没有恒星光照，使用自定义 Shader 的备用方向光。
        Shader.SetGlobalFloat("_SandboxFallbackLightOn", 1f);
        Shader.SetGlobalVector("_SandboxFallbackLightDir",
            new Vector4(-0.45f, 0.65f, -0.6f, 0f));
        Shader.SetGlobalColor("_SandboxFallbackLightColor", Color.white);

        GameObject cameraGo = new("PlanetCardPreviewCamera");
        cameraGo.transform.SetParent(root.transform, false);
        cameraGo.transform.localPosition = new Vector3(0f, 0f, -2.6f);
        cameraGo.transform.localRotation = Quaternion.identity;
        _camera = cameraGo.AddComponent<Camera>();
        _camera.enabled = false;
        _camera.orthographic = true;
        _camera.orthographicSize = 1.12f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.045f, 0.06f, 0.085f, 1f);
        _camera.cullingMask = 1 << layer;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 20f;
        _camera.allowHDR = false;
        _camera.allowMSAA = true;
        _target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        _camera.targetTexture = _target;

        GameObject lightGo = new("PlanetCardPreviewLight");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localRotation = Quaternion.Euler(35f, -35f, 0f);
        _light = lightGo.AddComponent<Light>();
        _light.type = LightType.Directional;
        _light.intensity = 1.25f;
        _light.color = Color.white;
        _light.cullingMask = 1 << layer;
    }
}
