using System;
using UnityEngine;

public class RandomStars : UPBMod
{
    public static void NewRandomStar(System.Random random)
    {
        SandboxRegister.RegisterStar(new StarData
        {
            name = $"RS {random.Next(2000000)}",
            mass = 0.078 + (random.NextDouble() * 10),
            metallicity = 0.02,
            age = 0,
            temp_review = random.Next(50000),
            rotationPeriodDays = random.NextDouble() * 2000,
        });
    }

    public void Startup(UPBLogger logger)
    {
        System.Random random = new();
        for (int i = 0; i < 1000; i++)
        {
            NewRandomStar(random);
        }
    }
}
