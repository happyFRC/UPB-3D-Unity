using System;
using UnityEngine;
using System.Collections.Generic;

public class RainbowStarColor : UPBMod
{
    public static Dictionary<double, (float r, float g, float b)> map = new();
    public void Startup(UPBLogger logger)
    {
        static float R() => 0.5f + UnityEngine.Random.value * 0.5f;
        Spectrum.CustomBlackbodyColor = (temperature) =>
        {
            if (map.TryGetValue(temperature, out var color))
            {
                return color;
            }
            else
            {
                var newColor = (R(), R(), R());
                map[temperature] = newColor;
                return newColor;
            }
        };
    }
}
