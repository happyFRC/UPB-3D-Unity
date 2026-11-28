using System.Collections.Generic;
using UnityEngine;

public class RainbowStarColor : UPBMod {
    private static readonly Dictionary<double, (float r, float g, float b)> Colors = new();

    public void Startup(UPBModInstance instance) {
        UPBEventBus.GetBlackbodyColorEvent = temperature => {
            if (Colors.TryGetValue(temperature, out var cached)) return cached;

            var color = (RandomChannel(), RandomChannel(), RandomChannel());
            Colors[temperature] = color;
            return color;
        };
    }

    private static float RandomChannel() => 0.5f + UnityEngine.Random.value * 0.5f;
}
