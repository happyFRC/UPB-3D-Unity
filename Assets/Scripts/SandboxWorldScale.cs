using UnityEngine;

/// <summary>
/// 沙盘的“世界缩放”层。
///
/// 缩放不再把相机拉到几亿/几万亿单位外，而是让相机始终停在焦点附近
/// （<see cref="CameraRenderDistance"/> 个渲染单位），同时把整个世界按
/// <see cref="Scale"/> 缩小/放大：
///
///   渲染坐标 = 逻辑坐标(基准单位) × Scale
///
/// 物理和参数计算仍使用真实单位（米、km、AU、光年，double），只有最后
/// 写入 Transform 时才乘 Scale。这样相机、投影、深度、射线都只在小数值
/// 上工作，避免 float 精度和大坐标引起的问题。
/// </summary>
public static class SandboxWorldScale {
    /// <summary>基准换算：1 个逻辑单位 = 92760 km。只在“真实单位 ⇄ 逻辑单位”用。</summary>
    public const double BaseKmPerUnit = CelestialScale.KmPerUnit;

    /// <summary>沙盘相机到焦点的固定渲染距离（Unity 单位）。</summary>
    public const double CameraRenderDistance = 30.0;

    private const double MinScale = 1e-18;
    private const double MaxScale = 1e12;

    /// <summary>当前世界缩放。1 = 和旧逻辑一致；越小表示世界被缩得越小、看得越远。</summary>
    public static double Scale => Enabled ? _scale : 1.0;
    private static double _scale = 1.0;
    private static bool Enabled = false;

    public static void Reset() {
        _scale = 1.0;
        Enabled = false;
    }

    /// <summary>相机虚拟距离（基准单位）→ 世界缩放。</summary>
    public static double ScaleForVirtualDistance(double virtualDistance) {
        if (virtualDistance <= 0.0) return MaxScale;
        return System.Math.Clamp(
            CameraRenderDistance / virtualDistance, MinScale, MaxScale);
    }

    /// <summary>由相机当前虚拟距离刷新世界缩放。</summary>
    public static void SetVirtualDistance(double virtualDistance) {
        _scale = ScaleForVirtualDistance(virtualDistance);
        Enabled = true;
    }

    /// <summary>逻辑坐标差（基准单位）→ 渲染坐标差。</summary>
    public static double ToRender(double baseUnits) => baseUnits * Scale;

    /// <summary>渲染坐标差 → 逻辑坐标差（基准单位）。</summary>
    public static double ToVirtual(double renderUnits) =>
        renderUnits / System.Math.Max(Scale, 1e-30);

    /// <summary>当前 1 个渲染单位对应多少 km。</summary>
    public static double KmPerRenderUnit => BaseKmPerUnit / Scale;

    /// <summary>当前 1 AU 对应多少渲染单位（行星 shader 用）。</summary>
    public static double UnitsPerAuRender => CelestialScale.UnitsPerAu * Scale;
}
