using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 沙盘浮点原点。
///
/// Unity Transform 是 float。天体放在几十亿公里外时，直接使用大世界坐标
/// 会让相机贴近行星时出现明显抖动。这里把所有天体逻辑坐标保存为 double，
/// 只在“当前聚焦天体”附近保留一个小的渲染坐标原点。
/// </summary>
public static class SandboxFloatingOrigin {
    public static double X { get; private set; }
    public static double Y { get; private set; }
    public static double Z { get; private set; }

    public static void Reset() {
        X = 0.0;
        Y = 0.0;
        Z = 0.0;
    }

    public static Vector3 ToRender(double x, double y, double z) {
        double scale = SandboxWorldScale.Scale;
        return new Vector3(
            (float)((x - X) * scale),
            (float)((y - Y) * scale),
            (float)((z - Z) * scale));
    }

    public static void FromRender(Vector3 render, out double x, out double y, out double z) {
        double scale = SandboxWorldScale.Scale;
        if (scale < 1e-30) scale = 1e-30;
        x = render.x / scale + X;
        y = render.y / scale + Y;
        z = render.z / scale + Z;
    }

    public static void Rebase(double x, double y, double z,
            IEnumerable<SandboxStar> stars, IEnumerable<SandboxPlanet> planets) {
        X = x;
        Y = y;
        Z = z;

        if (stars != null) {
            foreach (SandboxStar star in stars) {
                if (star != null) star.ApplyRenderPosition();
            }
        }
        if (planets != null) {
            foreach (SandboxPlanet planet in planets) {
                if (planet != null) planet.ApplyRenderPosition();
            }
        }
    }
}
