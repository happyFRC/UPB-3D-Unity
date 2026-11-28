using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

/// <summary>
/// 内置沙盘数据（天体 + 单位表）。
///
/// 只由 SandboxRegister 在首次初始化时调用一次；追加数据请用 SandboxRegister.Register*。
/// </summary>
public static class BuiltinSandboxObjects {
    internal static void RegisterAll() {
        RegisterPlanets();
        RegisterDwarfPlanets();
        RegisterMoons();
        RegisterEasterEggs();
        RegisterStars();
        RegisterUnits();
        RegisterSceneIntros();
        RegisterEvolutionSystems();
    }

    /// <summary>八大行星 + 水瓶座一系 b/c/d + 心大星Ab（12 个）。</summary>
    private static void RegisterPlanets() {
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "水星",
            temperature = 400.0,
            mass = 0.055,
            radius = 0.383,
            surface = new Resource("Textures/Planets/Mercury"),
            rotationPeriodDays = 58.646,
            axialTiltDeg = 0.034,
            trailColorR = 0.75f,
            trailColorG = 0.75f,
            trailColorB = 0.75f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "金星",
            temperature = 740.0,
            mass = 0.82,
            radius = 0.949,
            atmosphereMass = 90.0,
            surface = new Resource("Textures/Planets/Venus_Surface"),
            atmosphere = new Resource("Textures/Planets/Venus_Atmosphere"),
            rotationPeriodDays = 243.025,
            rotationDirection = -1,
            axialTiltDeg = 177.36,
            greenhouseFactor = 0.964,
            trailColorR = 1.0f,
            trailColorG = 0.82f,
            trailColorB = 0.45f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "地球",
            temperature = 287.0,
            mass = 1.0,
            radius = 1.0,
            atmosphereMass = 1.0,
            surface = new Resource("Textures/Planets/Earth_Surface"),
            atmosphere = new Resource("Textures/Planets/Earth_Atmosphere"),
            heightMap = "Textures/Planets/Earth_Heightmap",
            surfaceWaterDepthM = 5000.0,
            soluteConcentrationMmolL = 600.0,
            rotationPeriodDays = 0.99726968,
            axialTiltDeg = 23.44,
            greenhouseFactor = 0.122,
            atmosphereColorR = 0.7607843f,
            atmosphereColorG = 0.8745098f,
            atmosphereShell = true,
            trailColorR = 0.25f,
            trailColorG = 0.65f,
            trailColorB = 1.0f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "火星",
            temperature = 240.0,
            mass = 0.11,
            radius = 0.533,
            atmosphereMass = 0.005,
            surface = new Resource("Textures/Planets/Mars"),
            rotationPeriodDays = 1.02595676,
            axialTiltDeg = 25.19,
            atmosphereColorR = 1.0f,
            atmosphereColorG = 0.35f,
            atmosphereColorB = 0.15f,
            trailColorR = 1.0f,
            trailColorG = 0.35f,
            trailColorB = 0.2f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "木星",
            temperature = 125.0,
            mass = 318.0,
            radius = 11.21,
            atmosphereMass = 100.0,
            surface = new Resource("Textures/Planets/Jupiter"),
            rotationPeriodDays = 0.41354,
            axialTiltDeg = 3.13,
            atmosphereColorR = 0.544314f,
            atmosphereColorG = 0.548235f,
            atmosphereColorB = 0.6f,
            atmosphereShell = true,
            trailColorR = 0.55f,
            trailColorG = 0.35f,
            trailColorB = 0.2f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "土星",
            temperature = 95.0,
            mass = 95.2,
            radius = 9.45,
            atmosphereMass = 100.0,
            surface = new Resource("Textures/Planets/Saturn"),
            rotationPeriodDays = 0.44401,
            axialTiltDeg = 26.73,
            atmosphereColorR = 0.516471f,
            atmosphereColorG = 0.544314f,
            atmosphereColorB = 0.6f,
            atmosphereShell = true,
            trailColorR = 1.0f,
            trailColorG = 0.85f,
            trailColorB = 0.5f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "天王星",
            temperature = 60.0,
            mass = 14.5,
            radius = 4.01,
            surface = new Resource("Textures/Planets/Uranus"),
            rotationPeriodDays = 0.71833,
            rotationDirection = -1,
            axialTiltDeg = 97.77,
            trailColorR = 0.45f,
            trailColorG = 0.9f,
            trailColorB = 0.95f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "海王星",
            temperature = 60.0,
            mass = 17.26,
            radius = 3.88,
            surface = new Resource("Textures/Planets/Neptune"),
            rotationPeriodDays = 0.67125,
            axialTiltDeg = 28.32,
            trailColorR = 0.25f,
            trailColorG = 0.45f,
            trailColorB = 1.0f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "水瓶座一b",
            temperature = 261.15,
            mass = 3.6887795739358937,
            radius = 1.480301365562706,
            atmosphereMass = 0.32374404082273,
            surface = new Resource("Textures/Planets/Aquarius_One_b"),
            atmosphere = new Resource("Textures/Planets/Earth_Atmosphere"),
            heightMap = "Textures/Planets/Aquarius_One_b_Heightmap",
            surfaceWaterDepthM = 2000.0,
            landHeightMaxM = 16000.0,
            soluteConcentrationMmolL = 4100.0,
            rotationPeriodDays = 1.57,
            axialTiltDeg = 17.3,
            greenhouseFactor = 0.15769446786644747,
            atmosphereColorR = 0.2f,
            atmosphereColorG = 0.9f,
            atmosphereShell = true,
            trailColorR = 0.7843137f,
            trailColorG = 1.0f,
            trailColorB = 0.7843137f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "水瓶座一c",
            temperature = 237.45,
            mass = 0.302,
            radius = 0.714,
            atmosphereMass = 0.634439031596747,
            surface = new Resource("Textures/Planets/Aquarius_One_c"),
            rotationPeriodDays = 14.4,
            axialTiltDeg = 6.2,
            greenhouseFactor = 0.55876429550587525,
            atmosphereColorR = 0.1f,
            atmosphereColorG = 0.2f,
            atmosphereColorB = 0.55f,
            trailColorR = 0.1960784f,
            trailColorG = 0.1960784f,
            trailColorB = 0.3921569f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "水瓶座一d",
            temperature = 81.15,
            mass = 2740.0,
            radius = 15.91085,
            surface = new Resource("Textures/Planets/Aquarius_One_d"),
            rotationPeriodDays = 4.24,
            axialTiltDeg = 46.2,
            greenhouseFactor = 0.94549312859621837,
            trailColorR = 1.0f,
            trailColorG = 0.5019608f,
            trailColorB = 0.0f,
        });
        SandboxRegister.RegisterPlanet(new PlanetData {
            name = "心大星Ab",
            temperature = 300.0,
            mass = 665892.1,
            radius = 76.9108,
            atmosphereMass = 48.8684,
            surface = new Resource("Textures/Planets/Antares_Ab"),
            heightMap = "Textures/Planets/Antares_Ab_Heightmap",
            surfaceWaterDepthM = 2000.0,
            atmosphereColorR = 1.0f,
            atmosphereColorG = 1.0f,
            atmosphereShell = true,
            trailColorR = 0.95f,
            trailColorG = 0.62f,
            trailColorB = 0.42f,
            category = "SpecialPlanets",
        });
    }

    /// <summary>矮行星 / 小行星（8 个）。</summary>
    private static void RegisterDwarfPlanets() {
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "谷神星",
            temperature = 168.0,
            mass = 0.000157,
            radius = 0.0746,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Ceres"),
            rotationPeriodDays = 0.3781,
            trailColorR = 0.62f,
            trailColorG = 0.6f,
            trailColorB = 0.56f,
        });
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "冥王星",
            temperature = 44.0,
            mass = 0.00218,
            radius = 0.1817,
            atmosphereMass = 1E-05,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Pluto"),
            rotationPeriodDays = 6.3872,
            rotationDirection = -1,
            trailColorR = 0.55f,
            trailColorG = 0.48f,
            trailColorB = 0.42f,
        });
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "亡神星",
            temperature = 44.0,
            mass = 0.00010733,
            radius = 0.071967,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Orcus"),
            rotationPeriodDays = 9.13,
            trailColorR = 0.55f,
            trailColorG = 0.52f,
            trailColorB = 0.5f,
        });
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "创神星",
            temperature = 44.0,
            mass = 0.000234,
            radius = 0.085543,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Quaoar"),
            rotationPeriodDays = 0.736,
            trailColorR = 0.6f,
            trailColorG = 0.56f,
            trailColorB = 0.52f,
        });
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "阋神星",
            temperature = 42.0,
            mass = 0.00278,
            radius = 0.182546,
            atmosphereMass = 1E-05,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Eris"),
            rotationPeriodDays = 15.8,
            trailColorR = 0.82f,
            trailColorG = 0.82f,
            trailColorB = 0.84f,
        });
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "赛德娜",
            temperature = 12.0,
            mass = 0.000168,
            radius = 0.078481,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Sedna"),
            rotationPeriodDays = 0.428,
            trailColorR = 0.62f,
            trailColorG = 0.42f,
            trailColorB = 0.36f,
        });
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "鸟神星",
            temperature = 40.0,
            mass = 0.000519,
            radius = 0.112229,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Makemake"),
            rotationPeriodDays = 0.951,
            trailColorR = 0.66f,
            trailColorG = 0.58f,
            trailColorB = 0.52f,
        });
        SandboxRegister.RegisterDwarfPlanet(new PlanetData {
            name = "共工星",
            temperature = 41.0,
            mass = 0.000293,
            radius = 0.096532,
            surface = new Resource("Textures/DwarfPlants&Asteroids/Gonggong"),
            rotationPeriodDays = 0.933,
            trailColorR = 0.62f,
            trailColorG = 0.44f,
            trailColorB = 0.36f,
        });
    }

    /// <summary>卫星（18 个，orbitRadiusKm 是摆放卫星系场景时的默认轨道半径）。</summary>
    private static void RegisterMoons() {
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "月球",
            temperature = 250.0,
            mass = 0.0123,
            radius = 0.2727,
            surface = new Resource("Textures/Moons/Moon"),
            orbitRadiusKm = 384400.0,
            rotationPeriodDays = 27.3217,
            axialTiltDeg = 6.68,
            trailColorR = 0.78f,
            trailColorG = 0.78f,
            trailColorB = 0.76f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "木卫一",
            temperature = 110.0,
            mass = 0.01496,
            radius = 0.2859,
            surface = new Resource("Textures/Moons/Io"),
            orbitRadiusKm = 421700.0,
            rotationPeriodDays = 1.7691,
            trailColorR = 0.95f,
            trailColorG = 0.85f,
            trailColorB = 0.35f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "木卫二",
            temperature = 102.0,
            mass = 0.00804,
            radius = 0.245,
            surface = new Resource("Textures/Moons/Europa"),
            orbitRadiusKm = 671034.0,
            rotationPeriodDays = 3.5512,
            axialTiltDeg = 0.1,
            trailColorR = 0.88f,
            trailColorG = 0.88f,
            trailColorB = 0.82f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "木卫三",
            temperature = 110.0,
            mass = 0.02481,
            radius = 0.4135,
            surface = new Resource("Textures/Moons/Ganymede"),
            orbitRadiusKm = 1070412.0,
            rotationPeriodDays = 7.1546,
            axialTiltDeg = 0.33,
            trailColorR = 0.72f,
            trailColorG = 0.68f,
            trailColorB = 0.62f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "木卫四",
            temperature = 134.0,
            mass = 0.01802,
            radius = 0.3783,
            surface = new Resource("Textures/Moons/Callisto"),
            orbitRadiusKm = 1882709.0,
            rotationPeriodDays = 16.689,
            trailColorR = 0.6f,
            trailColorG = 0.56f,
            trailColorB = 0.5f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "土卫六",
            temperature = 94.0,
            mass = 0.02252,
            radius = 0.4041,
            atmosphereMass = 1.45,
            surface = new Resource("Textures/Moons/Titan_Surface"),
            atmosphere = new Resource("Textures/Moons/Titan_Atmosphere"),
            orbitRadiusKm = 1221870.0,
            rotationPeriodDays = 15.945,
            axialTiltDeg = 0.3,
            atmosphereColorR = 0.35f,
            atmosphereColorG = 0.55f,
            atmosphereColorB = 0.95f,
            atmosphereShell = true,
            trailColorR = 0.93f,
            trailColorG = 0.62f,
            trailColorB = 0.28f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "土卫二",
            temperature = 75.0,
            mass = 1.808E-05,
            radius = 0.03957,
            surface = new Resource("Textures/Moons/Enceladus"),
            orbitRadiusKm = 238020.0,
            rotationPeriodDays = 1.3702,
            trailColorR = 0.95f,
            trailColorG = 0.97f,
            trailColorB = 1.0f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "土卫三",
            temperature = 86.0,
            mass = 0.0001034,
            radius = 0.08336,
            surface = new Resource("Textures/Moons/Tethys"),
            orbitRadiusKm = 294660.0,
            rotationPeriodDays = 1.8878,
            trailColorR = 0.85f,
            trailColorG = 0.85f,
            trailColorB = 0.85f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "土卫四",
            temperature = 87.0,
            mass = 0.0001834,
            radius = 0.08812,
            surface = new Resource("Textures/Moons/Dione"),
            orbitRadiusKm = 377400.0,
            rotationPeriodDays = 2.7369,
            trailColorR = 0.78f,
            trailColorG = 0.78f,
            trailColorB = 0.8f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "冥卫一",
            temperature = 53.0,
            mass = 0.00026557,
            radius = 0.09512,
            surface = new Resource("Textures/Moons/Charon"),
            orbitRadiusKm = 19591.0,
            rotationPeriodDays = 6.3872,
            trailColorR = 0.65f,
            trailColorG = 0.65f,
            trailColorB = 0.65f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "海卫一",
            temperature = 38.0,
            mass = 0.003581,
            radius = 0.21244,
            surface = new Resource("Textures/Moons/Triton"),
            orbitRadiusKm = 354759.0,
            rotationPeriodDays = 5.877,
            rotationDirection = -1,
            trailColorR = 0.85f,
            trailColorG = 0.8f,
            trailColorB = 0.82f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "土卫八",
            temperature = 110.0,
            mass = 0.0003023,
            radius = 0.11529,
            surface = new Resource("Textures/Moons/Lapetus"),
            orbitRadiusKm = 3560820.0,
            rotationPeriodDays = 79.3215,
            trailColorR = 0.7f,
            trailColorG = 0.65f,
            trailColorB = 0.6f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "土卫五",
            temperature = 87.0,
            mass = 0.0003862,
            radius = 0.11989,
            surface = new Resource("Textures/Moons/Rhea"),
            orbitRadiusKm = 527040.0,
            rotationPeriodDays = 4.5182,
            trailColorR = 0.8f,
            trailColorG = 0.8f,
            trailColorB = 0.8f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "天卫一",
            temperature = 58.0,
            mass = 0.00022655,
            radius = 0.090865,
            surface = new Resource("Textures/Moons/Ariel"),
            orbitRadiusKm = 190900.0,
            rotationPeriodDays = 2.52,
            trailColorR = 0.72f,
            trailColorG = 0.72f,
            trailColorB = 0.74f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "天卫二",
            temperature = 61.0,
            mass = 0.00019624,
            radius = 0.091776,
            surface = new Resource("Textures/Moons/Umbriel"),
            orbitRadiusKm = 266000.0,
            rotationPeriodDays = 4.144,
            trailColorR = 0.48f,
            trailColorG = 0.48f,
            trailColorB = 0.5f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "天卫三",
            temperature = 70.0,
            mass = 0.00059057,
            radius = 0.123748,
            surface = new Resource("Textures/Moons/Tatania"),
            orbitRadiusKm = 436300.0,
            rotationPeriodDays = 8.706,
            trailColorR = 0.7f,
            trailColorG = 0.68f,
            trailColorB = 0.66f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "天卫四",
            temperature = 70.0,
            mass = 0.00050467,
            radius = 0.11951,
            surface = new Resource("Textures/Moons/Oberon"),
            orbitRadiusKm = 583500.0,
            rotationPeriodDays = 13.463,
            trailColorR = 0.58f,
            trailColorG = 0.55f,
            trailColorB = 0.53f,
        });
        SandboxRegister.RegisterMoon(new PlanetData {
            name = "天卫五",
            temperature = 60.0,
            mass = 1.1034E-05,
            radius = 0.037012,
            surface = new Resource("Textures/Moons/Miranda"),
            orbitRadiusKm = 129900.0,
            rotationPeriodDays = 1.413,
            trailColorR = 0.66f,
            trailColorG = 0.66f,
            trailColorB = 0.68f,
        });
    }

    /// <summary>彩蛋（2 个，长方体，不走球体渲染）。</summary>
    private static void RegisterEasterEggs() {
        SandboxRegister.RegisterEasterEgg(new PlanetData {
            name = "可乐摊车",
            temperature = 300.0,
            mass = 233062.23,
            radius = 94.18,
            surface = new Resource("Textures/EasterEggs/Coke_Car_Front"),
            rotationPeriodDays = 0.0,
            trailColorR = 0.85f,
            trailColorG = 0.15f,
            trailColorB = 0.15f,
            isBox = true,
            boxXKm = 1200000.0,
            boxYKm = 850000.0,
            boxZKm = 850000.0,
            textureFront = "Textures/EasterEggs/Coke_Car_Front",
            textureSides = "Textures/EasterEggs/Coke_Car_Sides",
            textureTopBackBottom = "Textures/EasterEggs/Coke_Car_TopBackButtom",
            ignoreGravity = true,
            ignoreCollision = true,
            ignoreThermal = true,
        });
        SandboxRegister.RegisterEasterEgg(new PlanetData {
            name = "分子氢箱",
            temperature = 250.0,
            mass = 66589.21,
            radius = 47.09,
            surface = new Resource("Textures/EasterEggs/HydrogenBox_0.2M_Front"),
            rotationPeriodDays = 0.0,
            trailColorR = 0.55f,
            trailColorG = 0.75f,
            trailColorB = 0.95f,
            isBox = true,
            boxXKm = 600000.0,
            boxYKm = 450000.0,
            boxZKm = 450000.0,
            textureFront = "Textures/EasterEggs/HydrogenBox_0.2M_Front",
            textureSides = "Textures/EasterEggs/HydrogenBox_0.2M_Sides",
            textureTopBackBottom = "Textures/EasterEggs/HydrogenBox_0.2M_TopBackButtom",
            impactGiftSun = 0.2,
        });
    }

    /// <summary>可选恒星（1500 个）：name, mass(Msun), metallicity(Z), age(Myr), temperature(K), rotationPeriodDays。</summary>
    private static void RegisterStars() {
        Star("太阳", 1.0, 0.018, 4450.0, 5773.5, 25.38);
        Star("水瓶座一", 1.55, 0.02, 621.6, 7311.8, 25.38);
        Star("比邻星", 0.1221, 0.02, 4850.0, 2948.8, 82.6);
        Star("巴纳德星", 0.144, 0.02, 10000.0, 3064.8, 130.4);
        Star("TRAPPIST-1", 0.0898, 0.02, 7600.0, 2502.6, 3.3);
        Star("天仓五", 0.783, 0.018, 5800.0, 4772.7, 34.0);
        Star("天苑四", 0.82, 0.02, 800.0, 4813.3, 11.3);
        Star("南门二 A", 1.1, 0.022, 5300.0, 5998.4, 22.0);
        Star("南河三A", 1.478, 0.016, 1870.0, 6781.4, 23.5);
        Star("天狼星 A", 2.063, 0.019, 240.0, 9408.8, 5.7);
        Star("北河三", 1.478, 0.0216, 3125.0, 4700.8, 38.0);
        Star("大角星", 1.08, 0.0054, 6682.0, 4794.7, 24.0);
        Star("毕宿五", 1.16, 0.0084, 5958.0, 4341.5, 520.0);
        Star("参宿七", 18.5, 0.0156, 10.12, 12552.7, 85.0);
        Star("参宿四", 16.0, 0.0202, 12.5, 3630.5, 3060.0);
        Star("心宿二", 17.2, 0.018, 11.3, 3785.2, 1800.0);
        Star("大犬座VY", 39.0, 0.0054, 6.2, 4546.4, 2000.0);
        Star("ST-2-18", 43.0, 0.018, 5.76, 3359.1, 25.38);
        Star("织女星", 2.135, 0.017, 455.0, 9575.1, 0.47);
        Star("牛郎星", 1.86, 0.02, 1000.0, 7720.9, 0.39);
        Star("北落师门", 1.92, 0.02, 440.0, 8751.3, 0.5);
        Star("北极星", 5.75, 0.02, 75.0, 5536.9, 3.97);
        Star("HD 240237", 8.76, 0.011247, 4420.0, 20287.7, 2.72);
        Star("HD 17092", 6.73, 0.03, 2600.0, 17973.5, 2.97);
        Star("HD 81817", 4.3, 0.013522, 150.0, 4547.6, 3.02);
        Star("HD 95127", 3.7, 0.013214, 5500.0, 12572.6, 3.98);
        Star("HD 142245", 3.5, 0.029582, 2300.0, 12070.5, 3.33);
        Star("HD 102329", 3.21, 0.02244, 1600.0, 11328.5, 4.31);
        Star("11 UMi", 2.78, 0.0191, 1560.0, 10381.2, 5.08);
        Star("WASP-72", 2.78, 0.017419, 3200.0, 10381.2, 5.49);
        Star("HAT-P-57", 2.77, 0.011247, 1000.0, 10362.7, 4.48);
        Star("kap And", 2.768, 0.02, 47.0, 11641.0, 5.34);
        Star("HD 139357", 2.69, 0.014826, 3070.0, 10213.5, 4.76);
        Star("HAT-P-14", 2.65, 0.025765, 1600.0, 10138.1, 4.9);
        Star("nu Oph", 2.61, 0.025179, 650.0, 4659.5, 4.39);
        Star("HD 180314", 2.6, 0.03, 1295.0, 10042.9, 4.52);
        Star("HAT-P-41", 2.56, 0.03, 2200.0, 9966.2, 6.1);
        Star("HD 82886", 2.53, 0.009796, 7000.0, 9908.2, 5.93);
        Star("WASP-73", 2.52, 0.027608, 4300.0, 9888.8, 6.44);
        Star("HIP 78530", 2.5, 0.006325, 5.0, 12358.1, 5.93);
        Star("HD 14067", 2.4, 0.015887, 690.0, 4507.0, 6.13);
        Star("KELT-9", 2.32, 0.023498, 450.0, 9491.1, 6.83);
        Star("tau Gem", 2.3, 0.027608, 1220.0, 9451.7, 4.83);
        Star("HD 100655", 2.28, 0.023498, 1309.0, 9410.9, 7.11);
        Star("WASP-17", 2.28, 0.011247, 2650.0, 9410.9, 6.29);
        Star("HAT-P-49", 2.22, 0.023498, 1500.0, 9287.5, 5.68);
        Star("HD 220074", 2.2, 0.011247, 4500.0, 9246.0, 7.44);
        Star("HD 110014", 2.17, 0.03, 950.0, 7205.2, 5.95);
        Star("HD 120084", 2.16, 0.025179, 1020.0, 4367.5, 7.36);
        Star("BD-13 2130", 2.12, 0.016635, 740.0, 8432.0, 7.51);
        Star("HD 25723", 2.12, 0.018665, 950.0, 6754.7, 6.87);
        Star("18 Del", 2.1, 0.017419, 1100.0, 4818.6, 7.76);
        Star("HD 47536", 2.1, 0.004477, 10150.0, 9035.6, 7.73);
        Star("IC 4651 9122", 2.1, 0.026365, 1140.0, 4465.2, 6.0);
        Star("HD 154391", 2.07, 0.025179, 1100.0, 5181.2, 5.42);
        Star("WASP-178", 2.07, 0.03, 430.0, 8809.1, 6.94);
        Star("HD 145934", 2.058, 0.024896, 1560.0, 8945.7, 5.69);
        Star("WASP-189", 2.03, 0.03, 730.0, 8103.4, 8.14);
        Star("TOI-4588", 2.018, 0.03, 860.0, 7668.9, 8.2);
        Star("HIP 8541", 2.009, 0.012913, 10970.0, 8839.6, 7.95);
        Star("HD 173416", 2.0, 0.012051, 1730.0, 8820.0, 7.26);
        Star("HD 40956", 2.0, 0.027608, 1350.0, 4677.4, 8.09);
        Star("HD 62509", 2.0, 0.025336, 980.0, 7290.7, 6.17);
        Star("TOI-1355", 2.0, 0.019545, 370.0, 9090.3, 6.21);
        Star("HD 154857", 1.96, 0.012051, 5640.0, 8665.7, 7.56);
        Star("KELT-3", 1.96, 0.02244, 3000.0, 8665.7, 6.4);
        Star("HD 111591", 1.94, 0.023498, 1410.0, 4684.1, 7.3);
        Star("HD 5891", 1.93, 0.008532, 1500.0, 8549.7, 8.3);
        Star("HD 11977", 1.91, 0.012332, 1200.0, 6513.1, 6.71);
        Star("HD 202696", 1.91, 0.020943, 1320.0, 6072.3, 8.63);
        Star("HD 145457", 1.9, 0.014489, 2605.0, 8433.5, 8.76);
        Star("HD 59686 A", 1.9, 0.028251, 1730.0, 4500.6, 9.22);
        Star("MASCARA-1", 1.9, 0.028251, 800.0, 7916.5, 9.21);
        Star("TOI-1431", 1.9, 0.024605, 290.0, 8491.4, 8.77);
        Star("HAT-P-70", 1.89, 0.017459, 600.0, 8689.6, 8.31);
        Star("HD 56414", 1.89, 0.02, 420.0, 8659.2, 7.01);
        Star("TYC 3667-1280-1", 1.87, 0.016635, 1350.0, 6111.4, 8.96);
        Star("WASP-78", 1.87, 0.008934, 2800.0, 8317.1, 9.49);
        Star("HD 1690", 1.86, 0.010741, 6700.0, 8278.2, 7.17);
        Star("HD 89744", 1.86, 0.03, 1880.0, 4540.8, 9.5);
        Star("TOI-1994", 1.86, 0.019545, 940.0, 7895.7, 8.55);
        Star("TOI-2497", 1.859, 0.024833, 1000.0, 7566.9, 8.88);
        Star("HD 18438", 1.84, 0.007962, 5500.0, 8200.4, 8.73);
        Star("HAT-P-33", 1.82, 0.02244, 2300.0, 8122.5, 9.16);
        Star("WASP-61", 1.82, 0.015887, 2700.0, 8122.5, 7.33);
        Star("HD 47366", 1.81, 0.0191, 1610.0, 4797.3, 7.97);
        Star("HD 112640", 1.8, 0.016257, 1600.0, 4586.7, 9.57);
        Star("HD 158996", 1.8, 0.012619, 1600.0, 8044.4, 7.12);
        Star("HD 174205", 1.8, 0.010496, 1700.0, 8044.4, 8.08);
        Star("HAT-P-6", 1.79, 0.014826, 2300.0, 8005.4, 8.68);
        Star("bet Pic", 1.789, 0.012332, 23.0, 8854.2, 7.07);
        Star("14 And", 1.78, 0.012332, 4500.0, 7966.3, 10.09);
        Star("gam Lib", 1.78, 0.010741, 2820.0, 7966.3, 9.68);
        Star("HATS-70", 1.78, 0.02198, 810.0, 7914.9, 7.35);
        Star("HD 33844", 1.78, 0.03, 1880.0, 4628.1, 7.34);
        Star("WASP-16", 1.78, 0.020466, 8600.0, 7966.3, 9.7);
        Star("TOI-4603", 1.765, 0.03, 1640.0, 6172.3, 9.43);
        Star("KELT-20", 1.76, 0.010257, 600.0, 8914.4, 9.93);
        Star("KELT-7", 1.76, 0.02, 1300.0, 7114.6, 8.28);
        Star("WASP-38", 1.76, 0.0191, 3290.0, 7888.0, 8.5);
        Star("51 Eri", 1.75, 0.026979, 23.2, 7834.6, 10.11);
        Star("HIP 56640", 1.75, 0.018665, 9150.0, 7848.9, 7.82);
        Star("MASCARA-4", 1.75, 0.02, 800.0, 7946.1, 7.64);
        Star("HIP 74890", 1.74, 0.03, 4410.0, 7809.7, 7.38);
        Star("xi Aql", 1.74, 0.015525, 7100.0, 7809.7, 7.32);
        Star("HAT-P-67", 1.73, 0.027608, 1460.0, 6711.4, 9.64);
        Star("HD 202772 A", 1.721, 0.03, 1630.0, 6380.3, 10.88);
        Star("HD 99706", 1.72, 0.027608, 2100.0, 4519.8, 7.68);
        Star("KOI-13", 1.72, 0.03, 1120.0, 7252.2, 9.62);
        Star("TOI-2145", 1.71, 0.03, 1600.0, 6476.7, 10.35);
        Star("bet Cnc", 1.7, 0.010257, 1850.0, 7652.6, 10.45);
        Star("eps CrB", 1.7, 0.016108, 1740.0, 6799.1, 8.01);
        Star("HD 31253", 1.7, 0.029582, 3000.0, 7652.6, 10.33);
        Star("HD 60292", 1.7, 0.013522, 1800.0, 5040.1, 11.37);
        Star("HD 181342", 1.69, 0.03, 2000.0, 6322.7, 8.39);
        Star("GPX-1", 1.68, 0.03, 270.0, 7551.3, 10.98);
        Star("WASP-94 A", 1.67, 0.03, 2700.0, 7534.5, 8.72);
        Star("TOI-159", 1.661, 0.03, 145.0, 7470.3, 9.76);
        Star("HD 102956", 1.66, 0.03, 2300.0, 4623.7, 8.03);
        Star("Kepler-340", 1.65, 0.022963, 1290.0, 7132.4, 8.99);
        Star("HAT-P-69", 1.648, 0.017062, 1270.0, 7372.1, 9.63);
        Star("HD 148427", 1.64, 0.028512, 2500.0, 4437.2, 9.65);
        Star("Kepler-1394", 1.64, 0.022963, 1910.0, 6240.1, 9.76);
        Star("Kepler-637", 1.64, 0.03, 2190.0, 5815.1, 10.87);
        Star("NGTS-2", 1.64, 0.017419, 2170.0, 4757.2, 11.28);
        Star("K2-260", 1.637, 0.03, 650.0, 7359.6, 12.01);
        Star("KELT-17", 1.635, 0.019188, 650.0, 7684.4, 9.04);
        Star("HD 16175", 1.63, 0.03, 3300.0, 7376.6, 11.83);
        Star("HD 4313", 1.63, 0.027608, 2000.0, 6100.6, 11.25);
        Star("HD 121504", 1.62, 0.028251, 1200.0, 7068.6, 11.99);
        Star("Kepler-516", 1.62, 0.020466, 1320.0, 7151.3, 10.74);
        Star("WASP-14", 1.62, 0.02, 750.0, 7557.1, 8.44);
        Star("WASP-3", 1.62, 0.02, 2100.0, 6493.0, 11.05);
        Star("HIP 90988", 1.613, 0.029582, 4550.0, 7309.4, 11.92);
        Star("HD 24064", 1.61, 0.006472, 9000.0, 7297.5, 9.04);
        Star("HD 208527", 1.6, 0.016257, 2000.0, 6298.8, 10.59);
        Star("HD 95086", 1.6, 0.027608, 17.0, 7237.6, 11.24);
        Star("Kepler-1115", 1.6, 0.006181, 1120.0, 8475.9, 11.21);
        Star("Kepler-959", 1.6, 0.020943, 1350.0, 7086.2, 11.6);
        Star("NGTS-33", 1.6, 0.015525, 30.0, 7748.1, 10.94);
        Star("KELT-12", 1.591, 0.03, 2200.0, 5973.4, 10.43);
        Star("HD 108863", 1.59, 0.03, 1800.0, 6463.1, 11.79);
        Star("Kepler-462", 1.59, 0.02244, 500.0, 7366.9, 10.75);
        Star("TOI-2005", 1.59, 0.02, 1600.0, 6841.1, 11.24);
        Star("WASP-167", 1.59, 0.025179, 1290.0, 7008.9, 11.91);
        Star("Kepler-1171", 1.58, 0.019545, 1350.0, 7119.4, 12.81);
        Star("Kepler-1517", 1.58, 0.020466, 1380.0, 7063.8, 9.47);
        Star("HATS-56", 1.573, 0.03, 1894.0, 6397.1, 12.22);
        Star("TOI-4153", 1.572, 0.03, 400.0, 7145.4, 12.85);
        Star("HD 213240", 1.57, 0.028251, 4600.0, 7138.9, 10.04);
        Star("Kepler-774", 1.57, 0.02193, 2140.0, 6200.7, 11.29);
        Star("nu Oct A", 1.57, 0.03, 2700.0, 4877.2, 9.72);
        Star("TOI-2049", 1.57, 0.03, 2200.0, 6058.7, 12.21);
        Star("HATS-64", 1.564, 0.03, 1861.0, 6451.9, 9.27);
        Star("HATS-40", 1.561, 0.020466, 2070.0, 6326.0, 13.34);
        Star("HAT-P-7", 1.56, 0.03, 1780.0, 6537.6, 10.86);
        Star("HD 212771", 1.56, 0.012332, 6000.0, 7099.1, 12.37);
        Star("HAT-P-30", 1.55, 0.026979, 1000.0, 7024.2, 11.04);
        Star("HD 125136", 1.55, 0.020943, 4030.0, 7059.3, 10.17);
        Star("HD 206610", 1.55, 0.027608, 3000.0, 7059.3, 9.33);
        Star("HD 212301", 1.55, 0.03, 1000.0, 6994.5, 10.88);
        Star("WASP-90", 1.55, 0.025765, 4400.0, 7059.3, 9.27);
        Star("TOI-6029", 1.548, 0.03, 2310.0, 6026.8, 10.53);
        Star("24 Sex", 1.54, 0.018665, 2700.0, 4691.0, 12.36);
        Star("HD 95089", 1.54, 0.02244, 2500.0, 5961.7, 9.93);
        Star("iot Dra", 1.54, 0.02143, 2650.0, 4989.3, 12.44);
        Star("KELT-2 A", 1.54, 0.02, 3970.0, 7019.5, 11.19);
        Star("Kepler-1518", 1.54, 0.022963, 1480.0, 6880.7, 13.14);
        Star("Kepler-522", 1.54, 0.03, 1700.0, 6619.6, 12.99);
        Star("WASP-187", 1.54, 0.02, 2550.0, 5377.7, 12.32);
        Star("Kepler-435", 1.538, 0.013214, 2250.0, 6219.1, 10.01);
        Star("TOI-4329", 1.538, 0.03, 2390.0, 5985.8, 11.84);
        Star("TOI-640", 1.536, 0.023606, 1990.0, 6448.4, 12.56);
        Star("HD 171028", 1.53, 0.006623, 8590.0, 6979.7, 11.34);
        Star("HD 28678", 1.53, 0.015525, 1800.0, 6783.7, 13.01);
        Star("TOI-2184", 1.53, 0.027608, 2300.0, 6117.8, 12.27);
        Star("PH1", 1.528, 0.03, 2000.0, 6388.9, 13.35);
        Star("KELT-18", 1.524, 0.024605, 1900.0, 6533.5, 13.0);
        Star("BD+49 828", 1.52, 0.012913, 2340.0, 6672.3, 13.63);
        Star("HD 33142", 1.52, 0.022963, 2720.0, 5433.2, 13.28);
        Star("HIP 39017", 1.52, 0.010991, 115.0, 7744.8, 12.98);
        Star("OGLE2-TR-L9", 1.52, 0.017825, 1000.0, 7251.4, 11.38);
        Star("TOI-2152", 1.516, 0.03, 830.0, 6923.9, 13.91);
        Star("Kepler-14", 1.512, 0.026365, 2200.0, 6276.8, 11.64);
        Star("8 UMi", 1.51, 0.018665, 1700.0, 6834.9, 11.01);
        Star("Kepler-1360", 1.51, 0.022963, 1550.0, 6808.2, 11.28);
        Star("Kepler-1626", 1.51, 0.02193, 1550.0, 6839.0, 10.72);
        Star("TOI-1789", 1.507, 0.03, 2730.0, 5811.1, 12.23);
        Star("TOI-5301", 1.507, 0.02244, 2290.0, 6263.1, 13.56);
        Star("TOI-1601", 1.5055, 0.03, 2400.0, 6097.8, 12.95);
        Star("HD 106906", 1.5, 0.02193, 13.0, 6986.5, 13.04);
        Star("HD 1666", 1.5, 0.03, 1760.0, 6578.6, 10.54);
        Star("HD 233604", 1.5, 0.00873, 2400.0, 5125.4, 11.44);
        Star("HD 60532", 1.5, 0.010991, 2800.0, 6860.0, 12.77);
        Star("HR 8799", 1.5, 0.006777, 60.0, 8072.8, 10.37);
        Star("Kepler-1137", 1.5, 0.02244, 1580.0, 6796.3, 11.4);
        Star("KOI-12", 1.5, 0.024128, 1450.0, 6808.8, 11.58);
        Star("mu Leo", 1.5, 0.03, 3350.0, 4405.5, 13.4);
        Star("HATS-41", 1.496, 0.03, 1340.0, 6758.1, 11.76);
        Star("HD 113996", 1.49, 0.026979, 3240.0, 4503.6, 9.94);
        Star("Kepler-1015", 1.49, 0.022963, 1580.0, 6770.6, 12.11);
        Star("Kepler-1364", 1.49, 0.02193, 1580.0, 6801.4, 11.16);
        Star("Kepler-471", 1.49, 0.023498, 1580.0, 6756.1, 14.28);
        Star("Kepler-644", 1.49, 0.024045, 1620.0, 6724.5, 10.82);
        Star("WASP-172", 1.49, 0.015887, 1790.0, 6839.4, 10.17);
        Star("WASP-68", 1.49, 0.03, 5700.0, 6838.2, 14.47);
        Star("K2-225", 1.482, 0.03, 12.1, 6786.4, 12.12);
        Star("HD 136418", 1.48, 0.015525, 4000.0, 6816.3, 11.4);
        Star("Kepler-40", 1.48, 0.025179, 2800.0, 5929.6, 11.2);
        Star("BD-10 3166", 1.47, 0.03, 250.0, 6767.3, 11.66);
        Star("HD 177830", 1.47, 0.03, 4400.0, 6794.4, 12.73);
        Star("Kepler-1375", 1.47, 0.024605, 1660.0, 6674.1, 14.85);
        Star("Kepler-1434", 1.47, 0.03, 2570.0, 6110.7, 12.63);
        Star("Kepler-1580", 1.47, 0.026365, 2750.0, 6001.0, 12.03);
        Star("NGTS-20", 1.47, 0.028251, 4100.0, 6794.4, 11.42);
        Star("TOI-4458", 1.47, 0.029582, 2800.0, 5949.8, 14.84);
        Star("XO-6", 1.47, 0.017023, 1880.0, 6758.6, 10.44);
        Star("TOI-5110", 1.469, 0.023229, 4000.0, 6792.2, 14.29);
        Star("TOI-1181", 1.467, 0.03, 2700.0, 6033.1, 11.9);
        Star("TOI-1842", 1.4643, 0.03, 2500.0, 6179.3, 14.0);
        Star("TOI-1333", 1.464, 0.026304, 2330.0, 6308.9, 12.35);
        Star("HD 1502", 1.46, 0.024605, 2400.0, 6294.1, 14.51);
        Star("Kepler-1244", 1.46, 0.02244, 1700.0, 6701.7, 10.22);
        Star("Kepler-433", 1.46, 0.019545, 2670.0, 6150.8, 10.73);
        Star("Kepler-880", 1.46, 0.02143, 1660.0, 6746.2, 15.01);
        Star("NGTS-38", 1.46, 0.03, 2200.0, 6368.4, 15.08);
        Star("WASP-22", 1.46, 0.017825, 3000.0, 5102.4, 11.1);
        Star("WASP-95", 1.46, 0.027608, 2400.0, 6264.8, 10.46);
        Star("KELT-21", 1.458, 0.007871, 1600.0, 7492.3, 11.57);
        Star("WASP-76", 1.458, 0.03, 1816.0, 6543.5, 12.32);
        Star("TOI-2109", 1.453, 0.02339, 1770.0, 6643.1, 11.4);
        Star("TOI-4773", 1.452, 0.025765, 1070.0, 6741.1, 10.62);
        Star("HAT-P-29", 1.45, 0.03, 2200.0, 6381.6, 12.0);
        Star("HD 40979", 1.45, 0.03, 3560.0, 4763.6, 14.62);
        Star("Kepler-1274", 1.45, 0.025765, 2880.0, 6010.2, 11.96);
        Star("Kepler-1298", 1.45, 0.026979, 2570.0, 6194.7, 12.63);
        Star("Kepler-1326", 1.45, 0.020943, 1700.0, 6733.6, 12.71);
        Star("Kepler-1586", 1.45, 0.022963, 1700.0, 6674.9, 13.93);
        Star("Kepler-812", 1.45, 0.020943, 1660.0, 6747.5, 12.09);
        Star("TOI-615", 1.449, 0.015887, 1700.0, 6897.4, 11.99);
        Star("XO-3", 1.449, 0.013869, 970.0, 7238.0, 14.87);
        Star("Kepler-1714", 1.448, 0.03, 2290.0, 6340.5, 15.05);
        Star("TOI-3362", 1.445, 0.020798, 2140.0, 6539.1, 11.78);
        Star("Kepler-1830", 1.443, 0.03, 2400.0, 6294.6, 12.91);
        Star("TOI-375", 1.441, 0.024268, 2940.0, 6023.9, 11.15);
        Star("K2-99", 1.44, 0.03, 3700.0, 4483.7, 14.14);
        Star("Kepler-1502", 1.44, 0.02244, 1700.0, 6675.4, 13.74);
        Star("TOI-163", 1.4352, 0.03, 1823.0, 6524.4, 15.43);
        Star("HAT-P-60", 1.435, 0.021779, 2765.0, 6177.0, 12.3);
        Star("HATS-67", 1.435, 0.03, 510.0, 6664.1, 11.34);
        Star("HD 2685", 1.43, 0.020943, 1310.0, 6785.1, 11.92);
        Star("Kepler-1051", 1.43, 0.02193, 1740.0, 6664.1, 13.32);
        Star("WASP-24", 1.43, 0.02, 3800.0, 6705.8, 12.62);
        Star("Kepler-264", 1.426, 0.022281, 2450.0, 6374.7, 13.93);
        Star("HAT-P-56", 1.42, 0.02, 2010.0, 6621.5, 13.7);
        Star("HD 149026", 1.42, 0.03, 2600.0, 6249.7, 13.86);
        Star("KELT-6", 1.42, 0.010496, 6100.0, 6683.4, 12.03);
        Star("TOI-2169 A", 1.42, 0.026548, 2510.0, 6300.4, 14.9);
        Star("WASP-11", 1.42, 0.02, 7600.0, 6683.4, 11.92);
        Star("Kepler-1936", 1.419, 0.025179, 2570.0, 6288.4, 11.15);
        Star("TOI-5646", 1.418, 0.03, 2820.0, 6156.7, 14.75);
        Star("HATS-27", 1.415, 0.024605, 2300.0, 6412.5, 12.78);
        Star("HATS-66", 1.411, 0.019232, 2170.0, 6573.9, 10.88);
        Star("BD-14 3065 A", 1.41, 0.009142, 2240.0, 6655.9, 15.25);
        Star("HD 113337", 1.41, 0.023498, 150.0, 6626.5, 11.27);
        Star("HD 180902", 1.41, 0.02193, 2800.0, 6238.0, 13.26);
        Star("HD 98219", 1.41, 0.0191, 4000.0, 6661.0, 14.12);
        Star("Kepler-1158", 1.41, 0.03, 2880.0, 6155.5, 14.34);
        Star("Kepler-1452", 1.41, 0.027608, 3240.0, 5988.9, 11.12);
        Star("Kepler-1618", 1.41, 0.02193, 1780.0, 6623.5, 15.13);
        Star("Kepler-541", 1.41, 0.023498, 3090.0, 6074.5, 13.37);
        Star("TOI-4641", 1.41, 0.016257, 2690.0, 6304.6, 12.71);
        Star("TOI-6884", 1.41, 0.024833, 2610.0, 6291.5, 13.42);
        Star("WASP-101", 1.41, 0.03, 1140.0, 6582.3, 10.93);
        Star("WASP-136", 1.41, 0.013214, 3620.0, 6661.0, 15.47);
        Star("WASP-159", 1.41, 0.03, 3400.0, 5922.5, 13.53);
        Star("Qatar-7", 1.409, 0.03, 1690.0, 6519.3, 13.9);
        Star("Kepler-1927", 1.408, 0.027608, 2880.0, 6161.2, 13.57);
        Star("Kepler-21", 1.408, 0.018665, 2840.0, 6230.6, 15.98);
        Star("KOI-134", 1.407, 0.03, 2400.0, 6344.0, 12.09);
        Star("TIC 147027702", 1.406, 0.016635, 2700.0, 6314.1, 12.59);
        Star("XO-7", 1.405, 0.03, 1180.0, 6566.7, 12.07);
        Star("HAT-P-39", 1.404, 0.03, 2000.0, 6452.7, 14.66);
        Star("91 Aqr", 1.4, 0.018665, 3560.0, 4970.6, 11.47);
        Star("bet UMi", 1.4, 0.010741, 2950.0, 6111.2, 11.74);
        Star("HD 111998", 1.4, 0.023498, 1700.0, 6578.9, 14.67);
        Star("HIP 21152", 1.4, 0.026979, 650.0, 6568.1, 14.55);
        Star("Kepler-1000", 1.4, 0.03, 1260.0, 6548.9, 14.71);
        Star("Kepler-1239", 1.4, 0.03, 1620.0, 6513.8, 12.4);
        Star("Kepler-642", 1.4, 0.022963, 1950.0, 6543.4, 14.84);
        Star("TOI-778", 1.4, 0.02, 1950.0, 6624.8, 12.0);
        Star("WASP-150", 1.394, 0.028644, 2950.0, 6164.8, 14.69);
        Star("WASP-120", 1.393, 0.017825, 700.0, 6821.8, 13.61);
        Star("HAT-P-34", 1.392, 0.03, 1700.0, 6487.1, 12.46);
        Star("TOI-4734", 1.391, 0.03, 3200.0, 6075.4, 14.14);
        Star("HD 106270", 1.39, 0.022963, 4300.0, 6615.9, 13.85);
        Star("HD 167042", 1.39, 0.019545, 2200.0, 6553.8, 16.05);
        Star("HD 175541", 1.39, 0.017023, 1900.0, 6717.9, 13.52);
        Star("HD 200964", 1.39, 0.014159, 3000.0, 6122.3, 13.26);
        Star("HD 4732", 1.39, 0.019545, 1470.0, 6711.9, 14.82);
        Star("HIP 114933", 1.39, 0.022963, 3400.0, 5998.6, 11.6);
        Star("Kepler-1543", 1.39, 0.023498, 1910.0, 6524.7, 16.46);
        Star("Kepler-419", 1.39, 0.029994, 2140.0, 6408.1, 15.95);
        Star("WASP-79", 1.39, 0.02143, 1400.0, 6651.4, 14.03);
        Star("TOI-5232", 1.389, 0.015741, 2370.0, 6552.6, 14.85);
        Star("HAT-P-47", 1.387, 0.018838, 1500.0, 6727.9, 16.1);
        Star("TOI-4214", 1.382, 0.03, 1100.0, 6508.2, 11.39);
        Star("HD 147873", 1.38, 0.02143, 4000.0, 4582.5, 13.26);
        Star("HD 156846", 1.38, 0.028251, 2400.0, 6345.4, 15.44);
        Star("HD 192699", 1.38, 0.014159, 1800.0, 6837.6, 11.52);
        Star("HD 33283", 1.38, 0.03, 3630.0, 5931.6, 11.33);
        Star("Kepler-1487", 1.38, 0.02193, 1950.0, 6542.1, 15.35);
        Star("TOI-1295", 1.38, 0.03, 2000.0, 6422.0, 15.87);
        Star("WASP-158", 1.38, 0.03, 1930.0, 6433.0, 11.2);
        Star("HATS-39", 1.379, 0.02, 2060.0, 6573.1, 15.48);
        Star("TOI-4487 A", 1.378, 0.03, 2450.0, 6328.9, 14.22);
        Star("HD 715", 1.375, 0.03, 2100.0, 6398.4, 15.1);
        Star("Kepler-5", 1.374, 0.02193, 2880.0, 6275.8, 16.42);
        Star("Kepler-1993", 1.373, 0.01824, 4680.0, 6577.3, 15.1);
        Star("TOI-4379", 1.372, 0.028909, 4510.0, 6575.0, 13.55);
        Star("CoRoT-3", 1.37, 0.027608, 2200.0, 6383.3, 15.81);
        Star("HAT-P-24", 1.37, 0.013837, 2800.0, 6330.7, 16.55);
        Star("HD 38283", 1.37, 0.013214, 6200.0, 6570.4, 13.52);
        Star("HD 96063", 1.37, 0.010024, 9000.0, 6570.4, 13.25);
        Star("Kepler-335", 1.367, 0.03, 3090.0, 6153.8, 14.84);
        Star("HD 19994", 1.365, 0.03, 2400.0, 6332.6, 16.0);
        Star("TOI-3464", 1.363, 0.022183, 2100.0, 6484.6, 16.48);
        Star("HD 18742", 1.36, 0.01824, 2300.0, 6543.6, 12.79);
        Star("HD 67087", 1.36, 0.03, 1450.0, 6440.1, 14.13);
        Star("Kepler-1382", 1.36, 0.03, 2750.0, 6253.4, 13.57);
        Star("Kepler-493", 1.36, 0.025765, 2000.0, 6420.5, 14.94);
        Star("TOI-4377", 1.36, 0.03, 3880.0, 5890.3, 12.22);
        Star("TrES-2", 1.36, 0.014159, 3550.0, 5260.5, 12.11);
        Star("WASP-197", 1.36, 0.02, 2600.0, 6418.0, 13.06);
        Star("Kepler-7", 1.359, 0.025765, 2900.0, 6234.9, 12.27);
        Star("HATS-68", 1.351, 0.03, 3020.0, 6188.7, 13.6);
        Star("HD 221420", 1.351, 0.03, 3650.0, 5997.2, 13.57);
        Star("TOI-150", 1.351, 0.03, 2346.0, 6330.5, 12.54);
        Star("HD 184010", 1.35, 0.013522, 2760.0, 6415.8, 14.05);
        Star("TOI-1107", 1.35, 0.015887, 2600.0, 6490.9, 13.06);
        Star("TOI-5340", 1.35, 0.03, 3500.0, 6049.4, 15.4);
        Star("TOI-558", 1.349, 0.019817, 1790.0, 6574.6, 13.38);
        Star("WASP-176", 1.345, 0.029176, 4810.0, 6513.1, 17.19);
        Star("TOI-2236", 1.343, 0.026304, 2520.0, 6315.3, 12.99);
        Star("Kepler-217", 1.342, 0.017023, 2400.0, 6541.3, 15.85);
        Star("7 CMa", 1.34, 0.03, 4300.0, 5653.2, 14.46);
        Star("HD 127195", 1.34, 0.01824, 2300.0, 6532.3, 16.7);
        Star("HD 131496", 1.34, 0.03, 2700.0, 6261.6, 14.57);
        Star("HD 8574", 1.34, 0.020466, 5600.0, 6501.6, 14.7);
        Star("HR 810", 1.34, 0.028909, 2720.0, 6259.3, 11.94);
        Star("Kepler-1112", 1.34, 0.02193, 2090.0, 6459.8, 16.65);
        Star("Kepler-1442", 1.34, 0.03, 1700.0, 6383.0, 17.63);
        Star("Kepler-521", 1.34, 0.03, 1780.0, 6377.9, 16.32);
        Star("Kepler-911", 1.34, 0.02143, 3550.0, 6093.2, 11.8);
        Star("NGTS-9", 1.34, 0.03, 960.0, 6397.7, 17.04);
        Star("WASP-8", 1.34, 0.029582, 3000.0, 6199.3, 12.84);
        Star("TOI-6695", 1.338, 0.03, 2100.0, 6346.7, 15.32);
        Star("WASP-169", 1.337, 0.022963, 3802.0, 5994.2, 15.09);
        Star("WASP-153", 1.336, 0.03, 4000.0, 5914.9, 17.37);
        Star("HD 103774", 1.335, 0.03, 1050.0, 6386.3, 17.08);
        Star("TOI-5882", 1.334, 0.03, 4110.0, 5881.7, 12.02);
        Star("WASP-93", 1.334, 0.023498, 700.0, 6432.1, 16.7);
        Star("HAT-P-2", 1.33, 0.03, 1440.0, 6372.1, 17.36);
        Star("HATS-11", 1.33, 0.02143, 7700.0, 6478.5, 14.31);
        Star("HD 116029", 1.33, 0.03, 2700.0, 6257.5, 14.67);
        Star("HD 1605", 1.33, 0.03, 4360.0, 5881.3, 17.3);
        Star("HD 210702", 1.33, 0.0191, 2100.0, 6528.7, 15.73);
        Star("kap CrB", 1.33, 0.024605, 3420.0, 6131.7, 15.31);
        Star("Kepler-1800", 1.33, 0.024045, 11750.0, 6478.5, 17.12);
        Star("Kepler-635", 1.33, 0.03, 2340.0, 6308.9, 14.47);
        Star("OGLE-TR-211", 1.33, 0.025765, 2600.0, 6300.3, 13.14);
        Star("TOI-2580", 1.33, 0.03, 2000.0, 6343.3, 15.07);
        Star("WASP-121", 1.33, 0.029582, 1110.0, 6373.2, 14.92);
        Star("WASP-142", 1.33, 0.03, 2000.0, 6343.3, 14.52);
        Star("WASP-66", 1.33, 0.009796, 3700.0, 4908.7, 16.42);
        Star("KOI-319", 1.325, 0.028909, 3860.0, 5988.4, 14.99);
        Star("TOI-2337", 1.325, 0.03, 4900.0, 4633.3, 17.94);
        Star("WASP-12", 1.325, 0.026365, 3050.0, 6210.5, 13.07);
        Star("HD 1397", 1.324, 0.03, 4510.0, 5496.5, 15.91);
        Star("TOI-6651", 1.323, 0.03, 3710.0, 6037.4, 12.99);
        Star("6 Lyn", 1.32, 0.013522, 2353.0, 6641.0, 17.99);
        Star("CoRoT-36", 1.32, 0.015887, 2100.0, 6625.8, 14.59);
        Star("HAT-P-13", 1.32, 0.03, 5000.0, 4473.3, 16.54);
        Star("HD 43691", 1.32, 0.03, 2390.0, 6291.6, 14.74);
        Star("Kepler-1345", 1.32, 0.02193, 2240.0, 6413.6, 15.37);
        Star("Kepler-145", 1.32, 0.019936, 3240.0, 6262.2, 15.8);
        Star("Kepler-43", 1.32, 0.03, 690.0, 6338.0, 14.27);
        Star("Kepler-432", 1.32, 0.017023, 4200.0, 5048.5, 12.75);
        Star("Kepler-56", 1.32, 0.03, 3500.0, 6097.1, 14.42);
        Star("tau Boo", 1.32, 0.03, 2000.0, 6326.5, 17.89);
        Star("TOI-201", 1.32, 0.03, 666.0, 6336.9, 16.63);
        Star("TOI-3682", 1.32, 0.03, 4200.0, 5892.7, 16.85);
        Star("TOI-813", 1.32, 0.025179, 3730.0, 6053.8, 13.47);
        Star("WASP-118", 1.32, 0.028909, 1170.0, 6348.7, 15.35);
        Star("WASP-113", 1.318, 0.025179, 6200.0, 6450.6, 13.48);
        Star("EPIC 246851721", 1.317, 0.027671, 3020.0, 6207.3, 17.23);
        Star("HATS-35", 1.317, 0.03, 2130.0, 6312.1, 14.53);
        Star("Kepler-312", 1.314, 0.028251, 2880.0, 6226.8, 15.5);
        Star("Kepler-207", 1.313, 0.03, 3630.0, 6074.6, 16.84);
        Star("TOI-4137", 1.313, 0.03, 2100.0, 6308.1, 17.11);
        Star("TOI-622", 1.313, 0.024605, 900.0, 6362.0, 17.23);
        Star("HD 48265", 1.312, 0.03, 4201.0, 5916.1, 13.89);
        Star("TOI-1408", 1.312, 0.03, 2700.0, 6246.9, 15.94);
        Star("TOI-628", 1.311, 0.03, 1280.0, 6330.4, 12.7);
        Star("HAT-P-31", 1.31, 0.028251, 3170.0, 6178.2, 17.34);
        Star("HD 224693", 1.31, 0.03, 4120.0, 5947.0, 14.12);
        Star("Kepler-1533", 1.31, 0.02143, 2190.0, 6417.1, 15.3);
        Star("Kepler-1622", 1.31, 0.02193, 2190.0, 6403.3, 14.35);
        Star("Kepler-91", 1.31, 0.025765, 4470.0, 5668.3, 13.83);
        Star("TOI-4551", 1.31, 0.03, 4920.0, 4863.6, 14.72);
        Star("TOI-6016", 1.31, 0.03, 300.0, 6290.5, 13.46);
        Star("TOI-6130", 1.31, 0.03, 300.0, 6290.5, 14.85);
        Star("TOI-2114", 1.309, 0.020046, 2000.0, 6472.4, 17.87);
        Star("OGLE-TR-132", 1.305, 0.03, 1200.0, 6316.0, 17.16);
        Star("HD 13931", 1.3, 0.02244, 8400.0, 6408.5, 18.08);
        Star("HD 150010", 1.3, 0.011247, 4900.0, 6408.5, 16.26);
        Star("HD 152581", 1.3, 0.006935, 12000.0, 6408.5, 12.94);
        Star("HD 158038", 1.3, 0.03, 2500.0, 6258.0, 15.37);
        Star("HD 185269", 1.3, 0.025765, 4000.0, 6017.2, 17.77);
        Star("HD 216435", 1.3, 0.03, 3000.0, 6200.3, 17.71);
        Star("HR 2562", 1.3, 0.020943, 600.0, 6403.6, 15.79);
        Star("K2-107", 1.3, 0.027608, 4250.0, 5935.0, 15.72);
        Star("Kepler-1104", 1.3, 0.026979, 2040.0, 6300.8, 13.57);
        Star("Kepler-1238", 1.3, 0.02244, 2140.0, 6376.6, 18.11);
        Star("Kepler-1515", 1.3, 0.023498, 1820.0, 6363.5, 16.64);
        Star("Kepler-1713", 1.3, 0.025179, 3720.0, 6096.1, 15.03);
        Star("Kepler-627", 1.3, 0.02244, 2240.0, 6370.4, 14.29);
        Star("Kepler-791", 1.3, 0.022963, 2240.0, 6357.4, 18.43);
        Star("Kepler-853", 1.3, 0.023498, 2690.0, 6304.6, 13.75);
        Star("NGTS-13", 1.3, 0.03, 4230.0, 5940.4, 18.12);
        Star("omi CrB", 1.3, 0.010741, 7827.0, 6408.5, 13.08);
        Star("ups And", 1.3, 0.02526, 5000.0, 4663.0, 15.2);
        Star("WASP-180 A", 1.3, 0.025179, 1200.0, 6329.7, 15.03);
        Star("WASP-195", 1.3, 0.02, 600.0, 6435.7, 17.68);
        Star("HATS-26", 1.299, 0.0191, 4040.0, 6033.0, 14.55);
        Star("Kepler-403", 1.299, 0.02399, 3090.0, 6239.8, 16.83);
        Star("HAT-P-64", 1.298, 0.015418, 2880.0, 6456.7, 17.5);
        Star("Kepler-1762", 1.298, 0.023498, 280.0, 6306.1, 16.59);
        Star("HAT-P-65", 1.297, 0.03, 3900.0, 6034.4, 16.31);
        Star("TOI-2207", 1.296, 0.03, 3300.0, 6154.7, 13.99);
        Star("HD 75898", 1.295, 0.03, 3200.0, 6170.6, 13.26);
        Star("WASP-18", 1.294, 0.025588, 1570.0, 6314.6, 12.67);
        Star("Kepler-33", 1.291, 0.021185, 4270.0, 5977.7, 17.97);
        Star("TOI-6038 A", 1.291, 0.026609, 3650.0, 6110.8, 18.79);
        Star("CoRoT-21", 1.29, 0.02, 4100.0, 6048.4, 17.42);
        Star("HD 13908", 1.29, 0.020466, 2900.0, 6343.0, 14.68);
        Star("HD 55696", 1.29, 0.03, 2640.0, 6234.7, 18.14);
        Star("HD 5608", 1.29, 0.02193, 1720.0, 6384.2, 15.45);
        Star("HD 75289", 1.29, 0.03, 5600.0, 6385.0, 16.58);
        Star("Kepler-1121", 1.29, 0.026365, 3020.0, 6214.4, 16.45);
        Star("Kepler-1300", 1.29, 0.02244, 3550.0, 6185.1, 17.39);
        Star("Kepler-39", 1.29, 0.025179, 2100.0, 6303.3, 18.42);
        Star("Kepler-999", 1.29, 0.02244, 3090.0, 6269.2, 16.93);
        Star("TOI-3071", 1.29, 0.03, 950.0, 6275.3, 18.36);
        Star("WASP-117", 1.29, 0.015525, 4600.0, 4757.9, 14.26);
        Star("WASP-194", 1.29, 0.02, 750.0, 6418.4, 14.46);
        Star("WASP-114", 1.289, 0.027608, 4300.0, 5951.6, 13.2);
        Star("TOI-1859", 1.287, 0.026365, 1410.0, 6288.2, 17.07);
        Star("Kepler-1833", 1.286, 0.03, 3890.0, 6053.4, 15.21);
        Star("HD 17156", 1.285, 0.03, 3300.0, 6156.2, 15.31);
        Star("HAT-P-46", 1.284, 0.03, 2500.0, 6239.2, 12.75);
        Star("HAT-P-9", 1.281, 0.026365, 1600.0, 6277.0, 16.16);
        Star("HD 103891", 1.28, 0.012913, 3680.0, 6183.6, 13.62);
        Star("HD 114082", 1.28, 0.02, 15.0, 6335.3, 14.97);
        Star("HD 135625", 1.28, 0.026365, 4400.0, 5952.6, 14.69);
        Star("HD 196885 A", 1.28, 0.03, 3120.0, 6178.5, 17.59);
        Star("HD 68988", 1.28, 0.03, 6000.0, 6361.4, 12.81);
        Star("HIP 111909", 1.28, 0.019545, 4300.0, 6007.4, 13.22);
        Star("HIP 67851", 1.28, 0.020466, 5460.0, 6361.4, 15.62);
        Star("HIP 75092", 1.28, 0.019545, 4300.0, 6007.4, 14.11);
        Star("Kepler-1270", 1.28, 0.0191, 4370.0, 6002.8, 17.96);
        Star("Kepler-1428", 1.28, 0.03, 2040.0, 6256.3, 15.47);
        Star("Kepler-483", 1.28, 0.024605, 2630.0, 6269.3, 18.3);
        Star("Kepler-514", 1.28, 0.03, 2690.0, 6220.4, 16.76);
        Star("Kepler-527", 1.28, 0.02143, 2400.0, 6357.2, 17.46);
        Star("TOI-892", 1.28, 0.03, 2200.0, 6250.4, 16.11);
        Star("WASP-63", 1.28, 0.024045, 8300.0, 6361.4, 17.1);
        Star("TOI-480", 1.2793, 0.029176, 2700.0, 6219.5, 14.07);
        Star("KOI-94", 1.277, 0.021078, 3160.0, 6288.5, 13.8);
        Star("WASP-7", 1.276, 0.02, 2400.0, 6394.4, 15.1);
        Star("HATS-31", 1.275, 0.02, 4300.0, 6030.2, 16.36);
        Star("HAT-P-50", 1.273, 0.013214, 3370.0, 6357.9, 18.41);
        Star("HATS-42", 1.273, 0.03, 3260.0, 6160.2, 15.08);
        Star("Kepler-1733", 1.273, 0.03, 200.0, 6195.5, 16.81);
        Star("HATS-45", 1.272, 0.020943, 1520.0, 6372.1, 13.96);
        Star("HD 148797", 1.272, 0.016145, 2940.0, 6443.0, 14.12);
        Star("TIC 241249530", 1.271, 0.024605, 3200.0, 6210.8, 17.88);
        Star("TOI-2346", 1.271, 0.018665, 1900.0, 6450.2, 16.24);
        Star("CoRoT-11", 1.27, 0.018665, 2000.0, 6446.2, 16.75);
        Star("HAT-P-8", 1.27, 0.020466, 3400.0, 6267.1, 13.25);
        Star("HD 118203", 1.27, 0.03, 4730.0, 5881.6, 16.94);
        Star("HD 196050", 1.27, 0.03, 4300.0, 5991.9, 13.69);
        Star("HD 5319", 1.27, 0.028251, 3100.0, 6180.9, 14.68);
        Star("HD 89307", 1.27, 0.014826, 2310.0, 6575.5, 14.28);
        Star("HD 96167", 1.27, 0.03, 4850.0, 5844.4, 14.49);
        Star("Kepler-1340", 1.27, 0.023498, 2400.0, 6290.4, 16.79);
        Star("Kepler-1640", 1.27, 0.025179, 2400.0, 6259.0, 18.75);
        Star("Kepler-391", 1.27, 0.02193, 4365.0, 6019.7, 16.31);
        Star("TOI-2578", 1.27, 0.020466, 5600.0, 6337.7, 17.43);
        Star("KELT-24", 1.268, 0.027481, 1980.0, 6240.5, 13.09);
        Star("Kepler-1743", 1.266, 0.03, 3720.0, 6101.2, 15.17);
        Star("NGTS-34", 1.266, 0.03, 2700.0, 6204.8, 18.34);
        Star("gam Cep", 1.26, 0.03, 5700.0, 4817.4, 13.21);
        Star("HD 75784", 1.26, 0.03, 4000.0, 6062.8, 15.95);
        Star("HD 86264", 1.26, 0.03, 2240.0, 6217.2, 18.43);
        Star("HD 88133", 1.26, 0.03, 5370.0, 5352.9, 15.38);
        Star("Kepler-1154", 1.26, 0.02193, 2290.0, 6316.1, 13.55);
        Star("Kepler-1383", 1.26, 0.02143, 2240.0, 6331.3, 13.58);
        Star("Kepler-910", 1.26, 0.026365, 3390.0, 6163.8, 14.35);
        Star("NGC 2682 Sand 1429", 1.26, 0.01941, 4300.0, 6086.3, 18.05);
        Star("NGTS-24", 1.26, 0.03, 4770.0, 5901.4, 17.24);
        Star("TOI-2382", 1.26, 0.03, 3100.0, 6170.1, 19.04);
        Star("HAT-P-45", 1.259, 0.023498, 2000.0, 6279.3, 16.78);
        Star("Kepler-849", 1.259, 0.027927, 4700.0, 5925.5, 13.52);
        Star("TOI-6649", 1.259, 0.03, 2400.0, 6210.5, 18.91);
        Star("HATS-65", 1.257, 0.03, 1780.0, 6218.0, 14.24);
        Star("K2-237", 1.256, 0.03, 1090.0, 6203.8, 14.53);
        Star("Kepler-393", 1.256, 0.021539, 2820.0, 6298.0, 16.73);
        Star("HAT-P-66", 1.255, 0.021679, 4660.0, 5976.1, 14.47);
        Star("HD 278555", 1.255, 0.03, 3800.0, 6094.9, 14.02);
        Star("TOI-3976 A", 1.254, 0.03, 3800.0, 6095.3, 19.85);
        Star("HD 50499", 1.253, 0.03, 2391.0, 6201.9, 16.6);
        Star("TOI-1836", 1.2518, 0.014623, 3200.0, 6441.5, 16.55);
        Star("HD 25463", 1.251, 0.019188, 2300.0, 6385.4, 16.0);
        Star("Kepler-1921", 1.251, 0.020943, 3390.0, 6260.4, 17.3);
        Star("CoRoT-31", 1.25, 0.02, 4700.0, 5983.3, 18.01);
        Star("DMPP-4", 1.25, 0.01968, 2150.0, 6369.6, 14.4);
        Star("HD 153950", 1.25, 0.019545, 4300.0, 6117.3, 17.76);
        Star("HD 156411", 1.25, 0.015172, 4280.0, 6121.8, 15.94);
        Star("HD 221287", 1.25, 0.018665, 1300.0, 6393.4, 14.3);
        Star("HD 33564", 1.25, 0.015172, 3000.0, 6464.2, 17.55);
        Star("HD 4203", 1.25, 0.03, 8520.0, 6290.1, 14.42);
        Star("HD 88986", 1.25, 0.022963, 7900.0, 6290.1, 16.77);
        Star("Kepler-1219", 1.25, 0.02244, 4370.0, 6069.3, 14.27);
        Star("Kepler-1421", 1.25, 0.02244, 2750.0, 6271.5, 13.5);
        Star("Kepler-1483", 1.25, 0.02193, 2400.0, 6296.5, 18.23);
        Star("Kepler-1569", 1.25, 0.022963, 2450.0, 6269.4, 18.98);
        Star("Kepler-1597", 1.25, 0.020943, 2400.0, 6324.7, 19.44);
        Star("Kepler-480", 1.25, 0.025765, 2340.0, 6220.1, 18.7);
        Star("Kepler-65", 1.25, 0.029582, 2900.0, 6175.5, 18.83);
        Star("Kepler-815", 1.25, 0.018665, 4900.0, 5544.6, 14.18);
        Star("Kepler-930", 1.25, 0.02193, 2340.0, 6297.7, 18.0);
        Star("TOI-4794", 1.25, 0.015814, 2700.0, 6480.6, 15.53);
        Star("TOI-5181 A", 1.25, 0.03, 4100.0, 6055.9, 18.96);
        Star("WASP-116", 1.25, 0.02, 1700.0, 6356.8, 16.18);
        Star("WASP-141", 1.25, 0.03, 5000.0, 5868.9, 18.16);
        Star("TOI-1898", 1.2484, 0.016071, 3100.0, 6422.4, 17.16);
        Star("HAT-P-4", 1.248, 0.03, 3900.0, 6083.4, 14.81);
        Star("Kepler-127", 1.248, 0.03, 2690.0, 6183.4, 15.61);
        Star("WASP-165", 1.248, 0.03, 4770.0, 5931.7, 13.92);
        Star("Kepler-108", 1.247, 0.03, 3890.0, 6084.4, 16.04);
        Star("TOI-4495", 1.247, 0.03, 1900.0, 6198.8, 15.85);
        Star("K2-98", 1.246, 0.018073, 3400.0, 6326.2, 15.66);
        Star("Kepler-79", 1.244, 0.022963, 1320.0, 6245.5, 17.32);
        Star("KOI-351", 1.242, 0.024212, 530.0, 6174.1, 13.65);
        Star("TOI-3819", 1.242, 0.03, 4500.0, 5993.5, 19.11);
        Star("TOI-4791", 1.242, 0.03, 3300.0, 6139.8, 15.71);
        Star("Kepler-1773", 1.241, 0.03, 2910.0, 6163.2, 17.64);
        Star("HAT-P-21", 1.24, 0.020466, 10200.0, 6266.1, 19.16);
        Star("Kepler-1169", 1.24, 0.03, 1380.0, 6178.1, 14.25);
        Star("Kepler-50", 1.24, 0.02143, 3800.0, 6194.9, 14.77);
        Star("Kepler-631", 1.24, 0.03, 2510.0, 6177.5, 16.26);
        Star("TOI-3980", 1.24, 0.03, 4700.0, 5956.9, 18.14);
        Star("TOI-5126", 1.24, 0.03, 800.0, 6157.0, 13.81);
        Star("TOI-5153", 1.24, 0.026365, 5400.0, 5672.3, 14.32);
        Star("WASP-1", 1.24, 0.029582, 3400.0, 6130.5, 16.66);
        Star("WASP-174", 1.24, 0.022963, 2200.0, 6253.7, 16.13);
        Star("WASP-94 B", 1.24, 0.03, 4000.0, 6069.3, 16.32);
        Star("KELT-14", 1.239, 0.03, 5110.0, 5861.4, 13.77);
        Star("TIC 87422071", 1.239, 0.028909, 3500.0, 6123.3, 14.52);
        Star("TIC 172900988 Aa", 1.2384, 0.03, 3100.0, 6149.4, 15.7);
        Star("Kepler-107", 1.238, 0.03, 4290.0, 6029.8, 15.52);
        Star("TOI-6457", 1.237, 0.03, 3000.0, 6153.3, 18.55);
        Star("HAT-P-35", 1.236, 0.025765, 3500.0, 6146.7, 16.1);
        Star("4 UMa", 1.234, 0.011247, 4604.0, 5377.0, 20.44);
        Star("TOI-2154", 1.233, 0.020513, 2900.0, 6291.3, 15.7);
        Star("TOI-1439", 1.2313, 0.03, 4600.0, 5983.4, 14.9);
        Star("HD 132563", 1.231, 0.017419, 2310.0, 6410.8, 18.16);
        Star("DMPP-7", 1.23, 0.026979, 3870.0, 6096.6, 20.45);
        Star("gam1 Leo", 1.23, 0.006181, 2412.0, 6942.4, 19.77);
        Star("HD 2039", 1.23, 0.03, 6000.0, 4919.8, 14.66);
        Star("HD 209458", 1.23, 0.020466, 3100.0, 6278.0, 20.25);
        Star("HD 224538", 1.23, 0.03, 2000.0, 6166.2, 16.01);
        Star("HD 27442", 1.23, 0.03, 10000.0, 6242.0, 15.86);
        Star("Kepler-1209", 1.23, 0.026365, 3090.0, 6158.0, 18.91);
        Star("Kepler-1275", 1.23, 0.02193, 2510.0, 6258.1, 15.58);
        Star("Kepler-1571", 1.23, 0.02193, 2340.0, 6259.7, 19.44);
        Star("Kepler-1666", 1.23, 0.012619, 410.0, 6555.1, 20.22);
        Star("Kepler-465", 1.23, 0.024605, 2190.0, 6200.7, 14.54);
        Star("Kepler-820", 1.23, 0.024605, 2630.0, 6197.4, 15.08);
        Star("TOI-1199", 1.23, 0.03, 4200.0, 6043.2, 16.53);
        Star("TOI-4127", 1.23, 0.027608, 4800.0, 5956.3, 15.37);
        Star("TOI-5786", 1.23, 0.024045, 2500.0, 6209.6, 14.32);
        Star("WASP-184", 1.23, 0.026365, 4700.0, 5984.5, 18.54);
        Star("Kepler-238", 1.229, 0.03, 4790.0, 5950.9, 18.17);
        Star("TOI-2886", 1.229, 0.03, 1700.0, 6161.8, 18.76);
        Star("OGLE-TR-56", 1.228, 0.03, 3000.0, 6141.7, 13.92);
        Star("HD 28109", 1.227, 0.025179, 3200.0, 6164.5, 15.79);
        Star("Kepler-278", 1.227, 0.03, 5761.0, 5683.2, 18.82);
        Star("Kepler-363", 1.226, 0.03, 5500.0, 5778.8, 20.5);
        Star("Kepler-4", 1.223, 0.029582, 4500.0, 6004.6, 16.25);
        Star("Kepler-1647", 1.2207, 0.014489, 4400.0, 6157.2, 15.02);
        Star("17 Sco", 1.22, 0.019545, 5130.0, 5993.3, 15.23);
        Star("GJ 504", 1.22, 0.03, 160.0, 6077.2, 15.29);
        Star("HD 108147", 1.22, 0.03, 3200.0, 6122.0, 19.31);
        Star("HD 11506", 1.22, 0.03, 2500.0, 6144.6, 14.71);
        Star("HD 205739", 1.22, 0.03, 2840.0, 6136.6, 18.39);
        Star("HD 216437", 1.22, 0.03, 4900.0, 5939.7, 20.72);
        Star("HIP 41378", 1.22, 0.017825, 2070.0, 6374.2, 18.01);
        Star("HIP 67522", 1.22, 0.02, 17.0, 6191.5, 19.03);
        Star("Kepler-1002", 1.22, 0.02193, 4070.0, 6148.2, 18.54);
        Star("Kepler-1054", 1.22, 0.024605, 2820.0, 6176.9, 18.7);
        Star("Kepler-1163", 1.22, 0.020943, 2880.0, 6260.0, 18.36);
        Star("Kepler-1373", 1.22, 0.02143, 2510.0, 6253.4, 20.61);
        Star("Kepler-1602", 1.22, 0.020943, 2510.0, 6267.5, 20.73);
        Star("Kepler-839", 1.22, 0.03, 2820.0, 6137.2, 15.6);
        Star("Kepler-997", 1.22, 0.02244, 3630.0, 6181.7, 14.45);
        Star("KIC 10068024", 1.22, 0.020466, 560.0, 6216.1, 16.53);
        Star("WASP-103", 1.22, 0.022963, 4000.0, 6136.6, 15.97);
        Star("WASP-138", 1.22, 0.016257, 3440.0, 6356.8, 19.87);
        Star("WASP-186", 1.22, 0.016635, 3100.0, 6381.7, 18.36);
        Star("TOI-1298", 1.2187, 0.03, 4300.0, 6030.7, 17.06);
        Star("AF Lep", 1.218, 0.03, 24.0, 6063.9, 16.06);
        Star("HAT-P-16", 1.218, 0.029582, 500.0, 6090.8, 19.85);
        Star("TOI-2583 A", 1.215, 0.028776, 4400.0, 6022.7, 19.22);
        Star("Kepler-410 A", 1.214, 0.020466, 2760.0, 6267.4, 14.62);
        Star("Kepler-1982", 1.213, 0.03, 2630.0, 6130.8, 15.52);
        Star("Kepler-342", 1.213, 0.019972, 2820.0, 6279.6, 14.31);
        Star("Kepler-8", 1.213, 0.017621, 3470.0, 6312.3, 19.2);
        Star("HD 128717", 1.212, 0.028909, 1400.0, 6115.5, 16.0);
        Star("HD 221416", 1.212, 0.016635, 4900.0, 6018.4, 16.06);
        Star("Kepler-103", 1.212, 0.028251, 6030.0, 5395.8, 20.36);
        Star("WASP-175", 1.212, 0.028251, 1745.0, 6124.6, 20.61);
        Star("KELT-16", 1.211, 0.019908, 3100.0, 6269.0, 14.22);
        Star("CoRoT-19", 1.21, 0.0191, 5000.0, 6002.5, 17.96);
        Star("HD 110082", 1.21, 0.024045, 250.0, 6086.1, 21.09);
        Star("HD 142", 1.21, 0.024605, 3120.0, 6153.7, 19.57);
        Star("HD 148164", 1.21, 0.03, 2410.0, 6128.3, 15.13);
        Star("HD 179949", 1.21, 0.027418, 2560.0, 6130.4, 17.99);
        Star("HD 195019", 1.21, 0.02244, 3200.0, 6195.2, 18.71);
        Star("HD 222155", 1.21, 0.013214, 8200.0, 6193.5, 18.62);
        Star("HD 231701", 1.21, 0.02244, 3430.0, 6185.0, 20.24);
        Star("HD 86081", 1.21, 0.03, 3610.0, 6087.9, 16.71);
        Star("HD 89839", 1.21, 0.02193, 1600.0, 6204.5, 16.91);
        Star("HIP 18606", 1.21, 0.019545, 5290.0, 5938.9, 17.96);
        Star("Kepler-1070", 1.21, 0.02143, 2750.0, 6232.7, 19.3);
        Star("Kepler-1100", 1.21, 0.02244, 2690.0, 6207.0, 18.43);
        Star("Kepler-1248", 1.21, 0.03, 1510.0, 6118.9, 15.08);
        Star("Kepler-1279", 1.21, 0.022963, 2820.0, 6193.0, 19.31);
        Star("Kepler-1408", 1.21, 0.02, 2570.0, 6277.5, 17.02);
        Star("Kepler-1435", 1.21, 0.018665, 2630.0, 6320.7, 18.79);
        Star("Kepler-1527", 1.21, 0.023498, 2820.0, 6181.4, 18.76);
        Star("Kepler-1603", 1.21, 0.023498, 2820.0, 6181.4, 21.23);
        Star("Kepler-1616", 1.21, 0.020943, 2750.0, 6246.5, 20.38);
        Star("Kepler-31", 1.21, 0.016789, 280.0, 6305.0, 16.52);
        Star("Kepler-464", 1.21, 0.03, 4900.0, 5949.4, 20.53);
        Star("Kepler-596", 1.21, 0.02143, 2690.0, 6233.5, 20.26);
        Star("Kepler-885", 1.21, 0.025765, 3160.0, 6135.2, 16.62);
        Star("Kepler-915", 1.21, 0.024045, 3390.0, 6153.6, 17.4);
        Star("Kepler-92", 1.21, 0.027487, 3980.0, 6071.9, 19.05);
        Star("TOI-1670", 1.21, 0.024605, 2530.0, 6162.8, 20.59);
        Star("TOI-2992", 1.21, 0.024268, 4600.0, 6040.4, 18.46);
        Star("WASP-99", 1.21, 0.03, 3000.0, 6116.9, 15.91);
        Star("HATS-3", 1.209, 0.013933, 3200.0, 6448.0, 16.41);
        Star("Kepler-6", 1.209, 0.03, 3800.0, 6074.2, 17.39);
        Star("HD 23127", 1.208, 0.03, 4508.0, 6006.0, 19.7);
        Star("TOI-3919", 1.208, 0.029925, 3100.0, 6110.5, 19.61);
        Star("HD 38801", 1.207, 0.03, 6072.0, 5661.4, 18.65);
        Star("HD 52265", 1.204, 0.03, 3500.0, 6088.2, 17.27);
        Star("HR 858", 1.204, 0.021185, 2000.0, 6223.9, 21.39);
        Star("Kepler-136", 1.204, 0.020009, 2750.0, 6264.4, 17.67);
        Star("TOI-1052", 1.204, 0.027608, 2300.0, 6118.3, 20.97);
        Star("Kepler-372", 1.202, 0.014826, 2349.0, 6458.3, 20.28);
        Star("KELT-4 A", 1.201, 0.015312, 4440.0, 6194.4, 19.0);
        Star("TOI-954", 1.201, 0.03, 6140.0, 5667.4, 15.92);
        Star("HD 126105", 1.2, 0.02, 4900.0, 6052.6, 19.94);
        Star("HD 12661", 1.2, 0.03, 6390.0, 5261.8, 14.95);
        Star("HD 145377", 1.2, 0.025765, 1300.0, 6098.5, 20.9);
        Star("HD 149143", 1.2, 0.03, 4280.0, 6029.4, 16.54);
        Star("HD 16417", 1.2, 0.022963, 7000.0, 6169.1, 14.45);
        Star("HD 44219", 1.2, 0.02143, 8400.0, 6169.1, 17.78);
        Star("HD 73344", 1.2, 0.027799, 1150.0, 6079.8, 14.89);
        Star("HD 94771", 1.2, 0.03, 5733.0, 5788.3, 16.55);
        Star("Kepler-1072", 1.2, 0.023498, 2880.0, 6163.6, 16.68);
        Star("Kepler-1280", 1.2, 0.02193, 2820.0, 6201.0, 18.78);
        Star("Kepler-1285", 1.2, 0.028909, 2750.0, 6106.2, 18.88);
        Star("Kepler-1288", 1.2, 0.027608, 2880.0, 6108.8, 20.75);
        Star("Kepler-1501", 1.2, 0.02193, 2750.0, 6201.5, 14.67);
        Star("Kepler-1633", 1.2, 0.020943, 2820.0, 6228.1, 19.25);
        Star("Kepler-76", 1.2, 0.015887, 620.0, 6343.1, 18.96);
        Star("TOI-333", 1.2, 0.020466, 1000.0, 6195.9, 15.22);
        Star("WASP-20", 1.2, 0.019635, 7000.0, 6169.1, 17.37);
        Star("WTS-1", 1.2, 0.006325, 2550.0, 6869.0, 19.26);
        Star("TOI-2025", 1.199, 0.028251, 4500.0, 6014.3, 20.52);
        Star("TOI-3688 A", 1.199, 0.03, 3300.0, 6091.2, 19.69);
        Star("Kepler-116", 1.198, 0.023498, 3160.0, 6155.5, 14.58);
        Star("Kepler-434", 1.198, 0.03, 4000.0, 6051.5, 18.84);
        Star("HD 77946", 1.1973, 0.03, 3100.0, 6095.5, 14.76);
        Star("HD 7449", 1.196, 0.015705, 2100.0, 6408.5, 21.24);
        Star("Kepler-1514", 1.196, 0.026304, 2900.0, 6112.5, 19.98);
        Star("HATS-55", 1.1955, 0.025647, 400.0, 6042.4, 21.46);
        Star("K2-290", 1.194, 0.017419, 4000.0, 6251.4, 21.63);
        Star("TOI-5592", 1.194, 0.01941, 3600.0, 6236.6, 19.68);
        Star("TOI-4562", 1.192, 0.024045, 700.0, 6073.7, 19.89);
        Star("Kepler-361", 1.191, 0.02193, 3890.0, 6151.3, 21.36);
        Star("HD 159868", 1.19, 0.016635, 7570.0, 6144.6, 19.85);
        Star("HD 221585", 1.19, 0.03, 6200.0, 5709.8, 20.58);
        Star("HD 62364", 1.19, 0.015525, 3901.0, 6307.5, 17.15);
        Star("HIP 98599", 1.19, 0.015172, 5230.0, 6007.4, 19.42);
        Star("K2-238", 1.19, 0.03, 5630.0, 5838.8, 18.69);
        Star("Kepler-1159", 1.19, 0.02193, 2750.0, 6183.2, 20.57);
        Star("Kepler-1249", 1.19, 0.022963, 2820.0, 6158.0, 21.56);
        Star("Kepler-1370", 1.19, 0.02244, 2880.0, 6170.0, 14.93);
        Star("Kepler-1443", 1.19, 0.02, 2630.0, 6239.4, 17.86);
        Star("Kepler-1510", 1.19, 0.02193, 2750.0, 6183.2, 16.18);
        Star("Kepler-1937", 1.19, 0.017419, 2450.0, 6328.9, 20.7);
        Star("Kepler-450", 1.19, 0.026843, 2570.0, 6097.9, 16.82);
        Star("Kepler-506", 1.19, 0.024605, 1740.0, 6108.1, 21.36);
        Star("Kepler-769", 1.19, 0.022963, 2880.0, 6157.7, 19.72);
        Star("Kepler-794", 1.19, 0.03, 4070.0, 6040.9, 20.63);
        Star("Kepler-798", 1.19, 0.02143, 2820.0, 6196.4, 21.07);
        Star("Kepler-805", 1.19, 0.017419, 4270.0, 6215.3, 19.85);
        Star("Kepler-852", 1.19, 0.03, 1480.0, 6075.9, 21.49);
        Star("Kepler-887", 1.19, 0.024045, 3090.0, 6133.4, 20.29);
        Star("psi1 Dra B", 1.19, 0.020466, 3300.0, 6215.5, 20.99);
        Star("TOI-2669", 1.19, 0.025179, 5900.0, 5845.2, 15.01);
        Star("TOI-4034", 1.19, 0.026365, 5700.0, 5833.0, 16.46);
        Star("TOI-4406", 1.19, 0.025179, 2900.0, 6116.3, 21.75);
        Star("WASP-166", 1.19, 0.03, 2100.0, 6089.6, 21.26);
        Star("WASP-60", 1.19, 0.01824, 1700.0, 6282.3, 16.03);
        Star("WASP-92", 1.19, 0.02, 2290.0, 6236.3, 15.77);
        Star("HD 187085", 1.189, 0.026365, 2747.0, 6100.5, 14.87);
        Star("Kepler-275", 1.189, 0.024045, 3390.0, 6125.8, 21.71);
        Star("Kepler-348", 1.189, 0.018665, 2290.0, 6279.5, 18.55);
        Star("Kepler-1838", 1.187, 0.02193, 4470.0, 6101.2, 15.0);
        Star("Kepler-1876", 1.187, 0.020466, 4400.0, 6139.1, 18.27);
        Star("TIC 257060897", 1.187, 0.022648, 5600.0, 5884.4, 17.3);
        Star("TOI-3364", 1.186, 0.03, 5400.0, 5890.6, 18.87);
        Star("Kepler-128", 1.184, 0.018665, 4070.0, 6212.3, 17.6);
        Star("TOI-2019", 1.1832, 0.03, 6400.0, 5664.1, 21.34);
        Star("TOI-3807", 1.181, 0.03, 5600.0, 5861.8, 20.76);
        Star("TOI-677", 1.181, 0.02, 2920.0, 6221.7, 18.77);
        Star("HD 154672", 1.18, 0.03, 8000.0, 6120.0, 17.64);
        Star("Kepler-1224", 1.18, 0.027608, 2340.0, 6071.7, 15.69);
        Star("Kepler-1293", 1.18, 0.02, 2820.0, 6220.2, 20.89);
        Star("Kepler-1323", 1.18, 0.020943, 3550.0, 6180.1, 16.03);
        Star("Kepler-1396", 1.18, 0.02193, 2750.0, 6164.2, 17.57);
        Star("Kepler-1475", 1.18, 0.02143, 2820.0, 6177.8, 16.54);
        Star("Kepler-1496", 1.18, 0.02244, 2950.0, 6151.6, 19.52);
        Star("Kepler-1589", 1.18, 0.024045, 2750.0, 6116.9, 21.49);
        Star("Kepler-1592", 1.18, 0.02193, 2690.0, 6163.9, 20.15);
        Star("Kepler-1609", 1.18, 0.025179, 3020.0, 6098.1, 19.91);
        Star("Kepler-535", 1.18, 0.026979, 3240.0, 6075.4, 17.37);
        Star("Kepler-703", 1.18, 0.02244, 2690.0, 6151.0, 15.64);
        Star("Kepler-718", 1.18, 0.022963, 2570.0, 6137.6, 21.31);
        Star("Kepler-74", 1.18, 0.03, 800.0, 6026.7, 16.64);
        Star("Kepler-873", 1.18, 0.023498, 2950.0, 6127.9, 15.1);
        Star("TOI-6564", 1.18, 0.03, 4000.0, 6037.0, 21.73);
        Star("WASP-15", 1.18, 0.013522, 2400.0, 6470.1, 21.18);
        Star("WASP-97", 1.18, 0.03, 4800.0, 5975.0, 21.3);
        Star("HAT-P-42", 1.178, 0.03, 5200.0, 5928.7, 18.32);
        Star("Kepler-129", 1.178, 0.03, 6430.0, 5683.8, 21.12);
        Star("TOI-4087", 1.178, 0.03, 800.0, 6022.3, 18.12);
        Star("WASP-106", 1.175, 0.0191, 2500.0, 6237.7, 18.41);
        Star("Kepler-1931", 1.174, 0.026365, 4570.0, 6018.1, 17.95);
        Star("TOI-2031 A", 1.174, 0.011509, 2000.0, 6525.6, 20.83);
        Star("Kepler-1819", 1.173, 0.03, 400.0, 5990.4, 22.35);
        Star("Kepler-401", 1.173, 0.02, 3550.0, 6197.2, 18.79);
        Star("Kepler-111", 1.171, 0.03, 2300.0, 6054.0, 18.05);
        Star("WASP-171", 1.171, 0.02193, 5908.0, 5851.1, 20.47);
        Star("EPIC 211945201", 1.17, 0.025179, 3990.0, 6062.7, 18.64);
        Star("HD 150706", 1.17, 0.019545, 3000.0, 6215.3, 20.58);
        Star("HD 169830", 1.17, 0.03, 2200.0, 6050.6, 19.09);
        Star("HD 29399", 1.17, 0.027608, 6200.0, 5759.1, 21.13);
        Star("HD 30856", 1.17, 0.017419, 3800.0, 6257.4, 15.21);
        Star("K2-108", 1.17, 0.03, 7800.0, 6095.2, 17.69);
        Star("K2-97", 1.17, 0.03, 7500.0, 4763.5, 17.6);
        Star("Kepler-101", 1.17, 0.03, 5900.0, 5824.0, 21.27);
        Star("Kepler-1181", 1.17, 0.02143, 3240.0, 6158.1, 22.29);
        Star("Kepler-1256", 1.17, 0.02244, 3720.0, 6123.7, 20.42);
        Star("Kepler-1395", 1.17, 0.026365, 2880.0, 6065.5, 20.21);
        Star("Kepler-1511", 1.17, 0.027608, 3310.0, 6053.7, 15.22);
        Star("Kepler-1524", 1.17, 0.02193, 2880.0, 6145.5, 17.66);
        Star("Kepler-270", 1.17, 0.028251, 2730.0, 6053.5, 15.15);
        Star("Kepler-512", 1.17, 0.03, 3720.0, 6038.4, 18.65);
        Star("Kepler-584", 1.17, 0.020943, 2880.0, 6172.6, 18.62);
        Star("Kepler-909", 1.17, 0.024045, 2880.0, 6098.4, 20.25);
        Star("TIC 365102760", 1.17, 0.024605, 6300.0, 5761.0, 21.53);
        Star("TOI-2295", 1.168, 0.03, 10500.0, 6090.3, 20.1);
        Star("Kepler-412", 1.167, 0.03, 5370.0, 5913.4, 21.17);
        Star("TOI-5350", 1.167, 0.022132, 3100.0, 6135.2, 18.56);
        Star("WASP-108", 1.167, 0.028108, 4640.0, 5994.0, 18.35);
        Star("Kepler-12", 1.166, 0.023498, 4000.0, 6086.8, 20.54);
        Star("Kepler-212", 1.166, 0.03, 5130.0, 5941.9, 19.04);
        Star("Kepler-430", 1.166, 0.03, 4680.0, 5982.6, 18.4);
        Star("WASP-123", 1.166, 0.03, 6900.0, 5560.3, 22.45);
        Star("Kepler-25", 1.165, 0.025765, 2750.0, 6061.3, 15.92);
        Star("Kepler-368", 1.165, 0.02143, 6460.0, 5472.2, 22.12);
        Star("TOI-5386 A", 1.165, 0.03, 3400.0, 6039.8, 20.61);
        Star("TOI-6420", 1.165, 0.03, 4400.0, 6001.1, 15.43);
        Star("HD 332231", 1.1642, 0.022082, 2900.0, 6130.4, 16.55);
        Star("Kepler-219", 1.164, 0.03, 4000.0, 6020.4, 21.74);
        Star("Kepler-356", 1.163, 0.018986, 4170.0, 6184.4, 21.53);
        Star("TOI-2485", 1.163, 0.025005, 6000.0, 5842.7, 22.13);
        Star("WASP-31", 1.163, 0.012619, 1000.0, 6416.1, 18.44);
        Star("Kepler-1764", 1.162, 0.014489, 240.0, 6280.5, 22.73);
        Star("Kepler-1812", 1.162, 0.025179, 1000.0, 6004.6, 16.38);
        Star("Kepler-1861", 1.161, 0.03, 4570.0, 5987.6, 19.35);
        Star("CoRoT-4", 1.16, 0.02244, 1000.0, 6048.2, 15.97);
        Star("HATS-12", 1.16, 0.015887, 2360.0, 6324.9, 19.3);
        Star("HD 196067", 1.16, 0.03, 4200.0, 6007.2, 22.22);
        Star("HD 220773", 1.16, 0.024605, 4400.0, 6043.4, 16.9);
        Star("HD 23079", 1.16, 0.01486, 2600.0, 6370.2, 22.71);
        Star("HD 73534", 1.16, 0.03, 7040.0, 5542.8, 16.39);
        Star("K2-34", 1.16, 0.020466, 2880.0, 6166.9, 16.73);
        Star("Kepler-1233", 1.16, 0.020943, 2880.0, 6152.8, 18.67);
        Star("Kepler-1283", 1.16, 0.02193, 2880.0, 6125.7, 19.07);
        Star("Kepler-1349", 1.16, 0.02143, 2880.0, 6139.0, 18.53);
        Star("Kepler-1354", 1.16, 0.02143, 2950.0, 6139.6, 22.83);
        Star("Kepler-1472", 1.16, 0.02143, 3090.0, 6140.3, 22.36);
        Star("Kepler-1531", 1.16, 0.018665, 2820.0, 6224.7, 16.0);
        Star("Kepler-1672", 1.16, 0.011247, 4190.0, 6325.0, 16.23);
        Star("Kepler-507", 1.16, 0.022963, 3800.0, 6095.9, 21.75);
        Star("Kepler-508", 1.16, 0.03, 3550.0, 6029.0, 19.89);
        Star("Kepler-544", 1.16, 0.020943, 2630.0, 6149.4, 17.35);
        Star("Kepler-741", 1.16, 0.023498, 3160.0, 6091.1, 15.67);
        Star("Kepler-758", 1.16, 0.015887, 3800.0, 6294.4, 15.46);
        Star("Kepler-788", 1.16, 0.02244, 3020.0, 6114.1, 15.46);
        Star("Kepler-904", 1.16, 0.02244, 3020.0, 6114.1, 18.18);
        Star("Kepler-982", 1.16, 0.02244, 3550.0, 6112.3, 15.71);
        Star("NGTS-39", 1.16, 0.03, 2200.0, 6029.6, 17.22);
        Star("TIC 150070085", 1.16, 0.024436, 135.0, 5961.3, 18.52);
        Star("TOI-2641", 1.16, 0.014159, 10800.0, 6070.4, 16.12);
        Star("TOI-3877", 1.16, 0.03, 5900.0, 5842.5, 22.37);
        Star("WASP-133", 1.16, 0.03, 6800.0, 5648.3, 19.36);
        Star("Kepler-1717", 1.158, 0.017419, 330.0, 6159.2, 21.65);
        Star("TOI-1294", 1.1578, 0.03, 6200.0, 5790.3, 16.81);
        Star("TOI-1296", 1.1576, 0.03, 7100.0, 5534.3, 18.7);
        Star("HD 89345", 1.157, 0.03, 7530.0, 4848.9, 19.89);
        Star("Qatar-10", 1.156, 0.020751, 3200.0, 6152.0, 19.18);
        Star("Kepler-144", 1.154, 0.013069, 940.0, 6370.7, 17.94);
        Star("HAT-P-1", 1.151, 0.026979, 3600.0, 6023.3, 16.71);
        Star("HD 112570", 1.15, 0.007962, 5450.0, 5265.0, 21.52);
        Star("HD 152843", 1.15, 0.013837, 3970.0, 6334.5, 16.6);
        Star("HD 208487", 1.15, 0.024605, 1950.0, 6025.0, 22.47);
        Star("HD 80653", 1.15, 0.03, 3300.0, 6016.0, 17.42);
        Star("HD 92788", 1.15, 0.03, 2550.0, 6013.9, 19.06);
        Star("K2-100", 1.15, 0.03, 750.0, 5956.1, 19.93);
        Star("Kepler-1201", 1.15, 0.02143, 2950.0, 6119.3, 18.06);
        Star("Kepler-1267", 1.15, 0.023498, 3240.0, 6072.2, 23.19);
        Star("Kepler-1385", 1.15, 0.03, 4370.0, 5989.1, 19.18);
        Star("Kepler-1422", 1.15, 0.02, 3020.0, 6162.2, 20.76);
        Star("Kepler-1617", 1.15, 0.020466, 3020.0, 6147.8, 22.74);
        Star("Kepler-1620", 1.15, 0.02143, 3020.0, 6120.1, 21.68);
        Star("Kepler-1621", 1.15, 0.02143, 2690.0, 6115.1, 22.84);
        Star("Kepler-328", 1.15, 0.024045, 2570.0, 6052.5, 15.97);
        Star("Kepler-41", 1.15, 0.03, 4400.0, 5987.7, 15.66);
        Star("Kepler-422", 1.15, 0.03, 3400.0, 6015.1, 17.3);
        Star("Kepler-502", 1.15, 0.02244, 3090.0, 6094.8, 20.49);
        Star("Kepler-526", 1.15, 0.03, 2140.0, 6006.6, 18.43);
        Star("Kepler-620", 1.15, 0.02143, 3020.0, 6120.1, 22.99);
        Star("Kepler-655", 1.15, 0.02, 3390.0, 6162.9, 16.52);
        Star("Kepler-669", 1.15, 0.027608, 3160.0, 6019.0, 16.75);
        Star("Kepler-750", 1.15, 0.02143, 3090.0, 6120.7, 15.79);
        Star("Kepler-814", 1.15, 0.020466, 3020.0, 6147.8, 17.64);
        Star("Kepler-824", 1.15, 0.027608, 3310.0, 6019.0, 19.29);
        Star("Kepler-919", 1.15, 0.023498, 3020.0, 6070.6, 16.33);
        Star("Kepler-937", 1.15, 0.024045, 3240.0, 6061.6, 15.88);
        Star("Kepler-972", 1.15, 0.02193, 2820.0, 6104.1, 20.28);
        Star("TOI-2587 A", 1.15, 0.026979, 6700.0, 5759.8, 18.37);
        Star("TYC 1422-614-1", 1.15, 0.012619, 5890.0, 5664.4, 17.47);
        Star("Kepler-1775", 1.149, 0.03, 2570.0, 6012.0, 16.0);
        Star("HD 152079", 1.147, 0.03, 1622.0, 5985.0, 22.11);
        Star("TOI-169", 1.147, 0.03, 4700.0, 5969.7, 19.45);
        Star("TOI-5398", 1.146, 0.024605, 650.0, 5955.8, 20.32);
        Star("Kepler-147", 1.145, 0.025179, 5500.0, 5948.1, 23.0);
        Star("Qatar-3", 1.145, 0.018198, 310.0, 6097.9, 18.41);
        Star("Kepler-381", 1.144, 0.012573, 2570.0, 6416.2, 21.23);
        Star("Kepler-1778", 1.143, 0.017419, 300.0, 6120.7, 21.26);
        Star("TOI-837", 1.142, 0.020466, 35.0, 6000.3, 15.94);
        Star("BD+03 2562", 1.14, 0.0039, 5200.0, 4639.0, 20.47);
        Star("CoRoT-20", 1.14, 0.027608, 100.0, 5889.4, 19.37);
        Star("CoRoT-23", 1.14, 0.02244, 7200.0, 5153.3, 21.27);
        Star("HD 179079", 1.14, 0.03, 6990.0, 5658.5, 18.87);
        Star("HD 217786", 1.14, 0.014656, 7600.0, 6020.4, 20.4);
        Star("HD 36384", 1.14, 0.013837, 113.0, 6243.7, 20.34);
        Star("Kepler-1094", 1.14, 0.022963, 3090.0, 6062.0, 19.67);
        Star("Kepler-1213", 1.14, 0.026979, 3470.0, 6004.4, 18.27);
        Star("Kepler-1276", 1.14, 0.0191, 3090.0, 6171.0, 23.55);
        Star("Kepler-1346", 1.14, 0.02193, 3090.0, 6086.8, 19.53);
        Star("Kepler-1433", 1.14, 0.023498, 3020.0, 6049.7, 22.14);
        Star("Kepler-1508", 1.14, 0.020466, 2690.0, 6120.7, 23.38);
        Star("Kepler-1551", 1.14, 0.020466, 2880.0, 6124.6, 22.07);
        Star("Kepler-1607", 1.14, 0.02244, 3160.0, 6075.0, 17.49);
        Star("Kepler-338", 1.14, 0.018708, 6460.0, 5834.1, 19.99);
        Star("Kepler-639", 1.14, 0.03, 4370.0, 5977.8, 17.34);
        Star("Kepler-889", 1.14, 0.03, 3980.0, 5989.2, 23.65);
        Star("Kepler-924", 1.14, 0.020466, 3090.0, 6127.8, 21.59);
        Star("OGLE-TR-10", 1.14, 0.03, 3100.0, 5997.6, 22.6);
        Star("OGLE-TR-182", 1.14, 0.03, 4300.0, 5980.2, 19.01);
        Star("TOI-1516", 1.14, 0.017825, 4820.0, 6150.8, 16.88);
        Star("TOI-481", 1.14, 0.03, 6700.0, 5732.0, 19.01);
        Star("WASP-75", 1.14, 0.023498, 3500.0, 6053.8, 23.31);
        Star("Kepler-1705", 1.139, 0.012619, 2590.0, 6402.4, 23.39);
        Star("Kepler-1824", 1.135, 0.03, 330.0, 5899.2, 21.76);
        Star("TOI-2842", 1.135, 0.03, 4700.0, 5958.9, 19.19);
        Star("HAT-P-32", 1.132, 0.013837, 2700.0, 6353.9, 22.69);
        Star("Kepler-1704", 1.132, 0.03, 7400.0, 5607.9, 19.46);
        Star("HATS-17", 1.131, 0.03, 2100.0, 5961.8, 17.55);
        Star("TOI-2421", 1.131, 0.03, 7500.0, 5576.9, 16.03);
        Star("TOI-3523 A", 1.131, 0.008611, 4300.0, 6386.7, 21.64);
        Star("alf Tau", 1.13, 0.010741, 6600.0, 4917.9, 18.51);
        Star("CoRoT-14", 1.13, 0.02244, 4200.0, 6055.1, 20.2);
        Star("HAT-P-22", 1.13, 0.03, 12400.0, 5995.2, 20.94);
        Star("HAT-P-23", 1.13, 0.028251, 4000.0, 5975.5, 22.28);
        Star("HD 109749", 1.13, 0.03, 3300.0, 5978.0, 20.07);
        Star("HD 160691", 1.13, 0.03, 5700.0, 5891.6, 19.48);
        Star("HD 32518", 1.13, 0.014159, 5830.0, 5999.8, 22.16);
        Star("Kepler-1093", 1.13, 0.019545, 2950.0, 6132.5, 19.62);
        Star("Kepler-1165", 1.13, 0.02244, 3240.0, 6054.8, 21.6);
        Star("Kepler-1225", 1.13, 0.020466, 3020.0, 6104.9, 23.97);
        Star("Kepler-1271", 1.13, 0.019545, 3630.0, 6138.7, 22.34);
        Star("Kepler-1398", 1.13, 0.020466, 3090.0, 6106.1, 16.5);
        Star("Kepler-1445", 1.13, 0.018665, 3800.0, 6165.2, 17.12);
        Star("Kepler-606", 1.13, 0.020466, 2880.0, 6102.0, 20.51);
        Star("Kepler-645", 1.13, 0.019545, 2950.0, 6132.5, 18.33);
        Star("Kepler-983", 1.13, 0.026365, 1410.0, 5935.2, 18.11);
        Star("TIC 279401253", 1.13, 0.03, 1200.0, 5927.9, 20.98);
        Star("TOI-2046", 1.13, 0.017419, 450.0, 6097.6, 23.72);
        Star("TOI-2567", 1.13, 0.03, 7600.0, 5544.2, 19.25);
        Star("TOI-5699", 1.13, 0.025765, 6600.0, 5801.1, 23.57);
        Star("TOI-6019", 1.13, 0.02193, 6400.0, 5869.6, 20.96);
        Star("Kepler-117", 1.129, 0.01824, 5300.0, 6094.8, 22.82);
        Star("Kepler-234", 1.129, 0.03, 2880.0, 5972.8, 18.45);
        Star("Qatar-5", 1.128, 0.03, 530.0, 5892.2, 23.21);
        Star("TOI-172", 1.128, 0.028121, 7400.0, 5627.6, 17.56);
        Star("HD 5278", 1.126, 0.015172, 3000.0, 6286.6, 20.17);
        Star("Kepler-216", 1.126, 0.014195, 280.0, 6204.6, 22.07);
        Star("HD 159243", 1.125, 0.02244, 1250.0, 5974.5, 21.46);
        Star("Kepler-223", 1.125, 0.025179, 8910.0, 5982.6, 17.55);
        Star("Kepler-379", 1.125, 0.024605, 2240.0, 5974.9, 22.12);
        Star("TOI-2986", 1.122, 0.02489, 6300.0, 5875.3, 16.71);
        Star("HATS-23", 1.121, 0.03, 4200.0, 5954.9, 17.27);
        Star("K2-232", 1.121, 0.025529, 3900.0, 5983.6, 24.26);
        Star("TOI-3474", 1.121, 0.027992, 4700.0, 5949.6, 22.54);
        Star("HAT-P-26", 1.12, 0.01824, 9000.0, 5969.9, 20.34);
        Star("HD 107148", 1.12, 0.03, 5600.0, 5897.9, 19.9);
        Star("HD 163607", 1.12, 0.03, 8600.0, 4813.7, 17.81);
        Star("HD 164509", 1.12, 0.03, 2320.0, 5941.0, 19.85);
        Star("HD 222582", 1.12, 0.020466, 10240.0, 5969.9, 16.53);
        Star("HD 30562", 1.12, 0.03, 3600.0, 5958.1, 22.54);
        Star("HD 68402", 1.12, 0.03, 2000.0, 5931.9, 18.19);
        Star("Kepler-1056", 1.12, 0.020943, 2950.0, 6066.1, 18.65);
        Star("Kepler-1084", 1.12, 0.0191, 4070.0, 6131.2, 17.69);
        Star("Kepler-1113", 1.12, 0.03, 3550.0, 5958.1, 19.71);
        Star("Kepler-1547", 1.12, 0.02193, 3390.0, 6047.7, 19.17);
        Star("Kepler-1581", 1.12, 0.023498, 4170.0, 6015.5, 19.15);
        Star("Kepler-1632", 1.12, 0.02, 3090.0, 6097.7, 22.97);
        Star("Kepler-1639", 1.12, 0.018665, 3160.0, 6142.9, 19.56);
        Star("Kepler-1641", 1.12, 0.0191, 3020.0, 6125.5, 19.95);
        Star("Kepler-1700", 1.12, 0.023498, 2750.0, 5998.0, 17.54);
        Star("Kepler-1972", 1.12, 0.03, 7400.0, 5638.7, 18.11);
        Star("Kepler-277", 1.12, 0.024605, 3890.0, 5995.5, 20.17);
        Star("Kepler-44", 1.12, 0.028251, 5800.0, 5892.6, 22.73);
        Star("Kepler-998", 1.12, 0.023498, 3310.0, 6010.4, 16.8);
        Star("TOI-2158", 1.12, 0.03, 7700.0, 5584.0, 17.02);
        Star("TOI-2498", 1.12, 0.029379, 3600.0, 5956.9, 20.09);
        Star("TOI-3023", 1.12, 0.024324, 7000.0, 5752.4, 18.1);
        Star("TOI-5120", 1.12, 0.03, 7500.0, 5653.0, 22.2);
        Star("WASP-126", 1.12, 0.03, 6400.0, 5823.3, 21.14);
        Star("WASP-185", 1.12, 0.0191, 6600.0, 5887.2, 16.33);
        Star("TOI-1135", 1.119, 0.018282, 3300.0, 6155.9, 21.98);
        Star("TOI-2416", 1.118, 0.03, 4900.0, 5932.3, 17.24);
        Star("TOI-2803 A", 1.118, 0.015705, 3700.0, 6247.3, 18.59);
        Star("Kepler-1831", 1.117, 0.023498, 300.0, 5882.9, 24.2);
        Star("TIC 245076932", 1.117, 0.020943, 4500.0, 6067.2, 24.0);
        Star("HD 214823", 1.115, 0.029582, 2700.0, 5935.7, 21.87);
        Star("Kepler-182", 1.114, 0.023606, 5250.0, 5981.2, 17.07);
        Star("Kepler-313", 1.114, 0.03, 7080.0, 5731.2, 19.45);
        Star("Kepler-168", 1.113, 0.024605, 2290.0, 5946.3, 16.98);
        Star("KELT-10", 1.112, 0.024605, 4500.0, 5978.5, 23.47);
        Star("TOI-2010", 1.112, 0.029446, 1900.0, 5905.1, 17.11);
        Star("HATS-52", 1.111, 0.010393, 1200.0, 6370.3, 18.01);
        Star("TOI-2180", 1.111, 0.03, 8100.0, 5503.1, 24.22);
        Star("HD 10647", 1.11, 0.02, 1400.0, 6008.9, 21.81);
        Star("HD 147513", 1.11, 0.022963, 300.0, 5875.1, 22.42);
        Star("HD 16141", 1.11, 0.026979, 7510.0, 5704.4, 19.25);
        Star("HD 197037", 1.11, 0.012619, 1900.0, 6306.9, 22.11);
        Star("Kepler-1003", 1.11, 0.020466, 3160.0, 6061.1, 23.83);
        Star("Kepler-1004", 1.11, 0.01824, 7760.0, 4978.5, 20.97);
        Star("Kepler-1085", 1.11, 0.024045, 3390.0, 5978.4, 23.45);
        Star("Kepler-1103", 1.11, 0.019545, 3310.0, 6093.0, 22.19);
        Star("Kepler-1106", 1.11, 0.02, 3090.0, 6073.9, 19.33);
        Star("Kepler-1119", 1.11, 0.02143, 3090.0, 6031.7, 18.27);
        Star("Kepler-1189", 1.11, 0.024045, 3390.0, 5978.4, 16.72);
        Star("Kepler-1278", 1.11, 0.020943, 3310.0, 6050.3, 20.82);
        Star("Kepler-1352", 1.11, 0.02, 3160.0, 6075.5, 21.37);
        Star("Kepler-1403", 1.11, 0.02143, 3310.0, 6036.6, 19.31);
        Star("Kepler-1485", 1.11, 0.020943, 2510.0, 6028.0, 24.18);
        Star("Kepler-1557", 1.11, 0.020943, 3310.0, 6050.3, 22.2);
        Star("Gliese 406", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 411", 0.2116, 0.02, 5000.0, 3339.0, 53.84);
        Star("Gliese 866 AB", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 15 B", 0.0863, 0.02, 5000.0, 2407.3, 85.28);
        Star("Gliese 15 A", 0.2188, 0.02, 5000.0, 3361.8, 53.12);
        Star("Gliese 845", 0.6216, 0.02, 5000.0, 4026.0, 30.95);
        Star("Gliese 820 B", 0.4404, 0.02, 5000.0, 3695.5, 37.98);
        Star("Gliese 820 A", 0.5486, 0.02, 5000.0, 3840.5, 33.38);
        Star("Gliese 887", 0.289, 0.02, 5000.0, 3530.0, 46.1);
        Star("Gliese 1111", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 273", 0.1216, 0.02, 5000.0, 2945.9, 70.68);
        Star("Gliese 825", 0.3924, 0.02, 5000.0, 3654.8, 40.38);
        Star("Gliese 191", 0.1772, 0.02, 5000.0, 3213.9, 59.56);
        Star("Gliese 860 A", 0.1256, 0.02, 5000.0, 2968.4, 69.88);
        Star("Gliese 860 B", 0.087, 0.02, 5000.0, 2426.5, 84.75);
        Star("Gliese 628", 0.1192, 0.02, 5000.0, 2932.1, 71.16);
        Star("Gliese 234 A", 0.0895, 0.02, 5000.0, 2494.4, 82.88);
        Star("Gliese 234 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1061", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 473 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1", 0.2284, 0.02, 5000.0, 3390.4, 52.16);
        Star("Gliese 674", 0.1652, 0.02, 5000.0, 3163.5, 61.96);
        Star("Gliese 832", 0.2236, 0.02, 5000.0, 3376.3, 52.64);
        Star("Gliese 380", 0.4524, 0.02, 5000.0, 3706.5, 37.38);
        Star("Gliese 1002", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 687", 0.1844, 0.02, 5000.0, 3242.4, 58.12);
        Star("Gliese 1245 A", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1245 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 682", 0.0984, 0.02, 5000.0, 2728.1, 76.2);
        Star("Gliese 166 A", 0.7784, 0.02, 5000.0, 4674.7, 25.72);
        Star("Gliese 388", 0.174, 0.02, 5000.0, 3200.8, 60.2);
        Star("Gliese 702 A", 0.822, 0.02, 5000.0, 4866.9, 25.0);
        Star("Gliese 702 B", 0.5514, 0.02, 5000.0, 3846.1, 33.29);
        Star("Gliese 445", 0.1116, 0.02, 5000.0, 2886.8, 72.68);
        Star("Gliese 1116 A", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1116 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 412 A", 0.2404, 0.02, 5000.0, 3423.5, 50.96);
        Star("Gliese 412 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1005 AB", 0.0859, 0.02, 5000.0, 2396.3, 85.58);
        Star("Gliese 663 A", 0.7096, 0.02, 5000.0, 4359.2, 28.01);
        Star("Gliese 663 B", 0.7032, 0.02, 5000.0, 4332.4, 28.23);
        Star("Gliese 526", 0.272, 0.02, 5000.0, 3497.4, 47.8);
        Star("Gliese 664", 0.5318, 0.02, 5000.0, 3809.1, 33.94);
        Star("Gliese 764", 0.7898, 0.02, 5000.0, 4730.6, 25.34);
        Star("Gliese 908", 0.2316, 0.02, 5000.0, 3399.5, 51.84);
        Star("Gliese 783 B", 0.096, 0.02, 5000.0, 2666.3, 78.0);
        Star("Gliese 783 A", 0.6904, 0.02, 5000.0, 4278.2, 28.65);
        Star("Gliese 752 A", 0.222, 0.02, 5000.0, 3371.5, 52.8);
        Star("Gliese 754", 0.0855, 0.02, 5000.0, 2385.3, 85.88);
        Star("Gliese 780", 1.0181, 0.02, 5000.0, 5808.5, 24.53);
        Star("Gliese 229", 0.318, 0.02, 5000.0, 3575.8, 44.1);
        Star("Gliese 570 A", 0.6264, 0.02, 5000.0, 4041.2, 30.79);
        Star("Gliese 570 B", 0.3348, 0.02, 5000.0, 3597.6, 43.26);
        Star("Gliese 205", 0.3432, 0.02, 5000.0, 3607.4, 42.84);
        Star("Gliese 643", 0.0904, 0.02, 5000.0, 2518.6, 82.2);
        Star("Gliese 300", 0.0875, 0.02, 5000.0, 2440.1, 84.38);
        Star("Gliese 34 B", 0.4032, 0.02, 5000.0, 3663.9, 39.84);
        Star("Gliese 34 A", 1.0904, 0.02, 5000.0, 6035.5, 22.65);
        Star("Gliese 213", 0.0972, 0.02, 5000.0, 2697.3, 77.1);
        Star("Gliese 693", 0.1264, 0.02, 5000.0, 2972.9, 69.72);
        Star("Gliese 588", 0.218, 0.02, 5000.0, 3359.3, 53.2);
        Star("Gliese 268", 0.0986, 0.02, 5000.0, 2733.3, 76.05);
        Star("Gliese 784", 0.3552, 0.02, 5000.0, 3620.3, 42.24);
        Star("Gliese 338 A", 0.3996, 0.02, 5000.0, 3660.9, 40.02);
        Star("Gliese 338 B", 0.3888, 0.02, 5000.0, 3651.7, 40.56);
        Star("Gliese 1128", 0.0819, 0.02, 5000.0, 2284.8, 88.58);
        Star("Gliese 139", 0.8993, 0.02, 5000.0, 5261.1, 25.0);
        Star("Gliese 661 B", 0.146, 0.02, 5000.0, 3074.8, 65.8);
        Star("Gliese 661 A", 0.1724, 0.02, 5000.0, 3194.2, 60.52);
        Star("Gliese 555", 0.1072, 0.02, 5000.0, 2859.3, 73.56);
        Star("Gliese 2097", 0.0845, 0.02, 5000.0, 2357.6, 86.62);
        Star("Gliese 19", 1.42, 0.02, 5000.0, 5822.0, 14.08);
        Star("Gliese 644 A", 0.1996, 0.02, 5000.0, 3298.5, 55.08);
        Star("Gliese 644 B", 0.186, 0.02, 5000.0, 3248.6, 57.8);
        Star("Gliese 644 C", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 881", 2.437, 0.02, 5000.0, 9167.0, 5.91);
        Star("Gliese 566 B", 0.5024, 0.02, 5000.0, 3762.7, 34.92);
        Star("Gliese 566 A", 0.8402, 0.02, 5000.0, 4960.2, 25.0);
        Star("Gliese 66 B", 0.6584, 0.02, 5000.0, 4152.2, 29.72);
        Star("Gliese 66 A", 0.6744, 0.02, 5000.0, 4213.1, 29.19);
        Star("Gliese 892", 0.7176, 0.02, 5000.0, 4393.4, 27.75);
        Star("Gliese 408", 0.1844, 0.02, 5000.0, 3242.4, 58.12);
        Star("Gliese 667 B", 0.494, 0.02, 5000.0, 3751.7, 35.3);
        Star("Gliese 667 C", 0.1724, 0.02, 5000.0, 3194.2, 60.52);
        Star("Gliese 667 A", 0.6172, 0.02, 5000.0, 4012.3, 31.09);
        Star("Gliese 17", 0.9754, 0.02, 5000.0, 5618.6, 25.0);
        Star("Gliese 514", 0.274, 0.02, 5000.0, 3501.5, 47.6);
        Star("Gliese 1286", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1230 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1230 A", 0.089, 0.02, 5000.0, 2480.9, 83.25);
        Star("Gliese 33", 0.7144, 0.02, 5000.0, 4379.7, 27.85);
        Star("Gliese 53 B", 0.12, 0.02, 5000.0, 2936.7, 71.0);
        Star("Gliese 53 A", 0.8066, 0.02, 5000.0, 4788.5, 25.0);
        Star("Gliese 809", 0.3384, 0.02, 5000.0, 3601.9, 43.08);
        Star("Gliese 178", 1.395, 0.02, 5000.0, 6500.0, 14.73);
        Star("Gliese 2005", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1224", 0.0788, 0.02, 5000.0, 2196.4, 90.0);
        Star("Gliese 713 AB", 1.2349, 0.02, 5000.0, 5953.1, 18.89);
        Star("Gliese 666 A", 0.7592, 0.02, 5000.0, 4582.4, 26.36);
        Star("Gliese 666 B", 0.3252, 0.02, 5000.0, 3585.5, 43.74);
        Star("Gliese 618 A", 0.1598, 0.02, 5000.0, 3139.5, 63.04);
        Star("Gliese 618 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 185 A", 0.3576, 0.02, 5000.0, 3622.8, 42.12);
        Star("Gliese 185 B", 0.164, 0.02, 5000.0, 3158.2, 62.2);
        Star("Gliese 105 A", 0.7192, 0.02, 5000.0, 4400.4, 27.69);
        Star("Gliese 673", 0.4704, 0.02, 5000.0, 3724.5, 36.48);
        Star("Gliese 1093", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 884", 0.4296, 0.02, 5000.0, 3686.1, 38.52);
        Star("Gliese 68", 0.8178, 0.02, 5000.0, 4845.5, 25.0);
        Star("Gliese 831 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 216 A", 1.2783, 0.02, 5000.0, 4801.6, 17.76);
        Star("Gliese 216 B", 0.6824, 0.02, 5000.0, 4245.1, 28.92);
        Star("Gliese 1105", 0.1016, 0.02, 5000.0, 2809.4, 74.68);
        Star("Gliese 450", 0.2348, 0.02, 5000.0, 3408.4, 51.52);
        Star("Gliese 1123", 0.0845, 0.02, 5000.0, 2357.6, 86.62);
        Star("Gliese 1289", 0.0898, 0.02, 5000.0, 2502.4, 82.65);
        Star("Gliese 747 B", 0.098, 0.02, 5000.0, 2717.9, 76.5);
        Star("Gliese 747 A", 0.108, 0.02, 5000.0, 2864.4, 73.4);
        Star("Gliese 1151", 0.0832, 0.02, 5000.0, 2321.3, 87.6);
        Star("Gliese 1227", 0.0817, 0.02, 5000.0, 2279.1, 88.72);
        Star("Gliese 479", 0.1646, 0.02, 5000.0, 3160.8, 62.08);
        Star("Gliese 54", 0.2332, 0.02, 5000.0, 3404.0, 51.68);
        Star("Gliese 117", 0.7112, 0.02, 5000.0, 4366.0, 27.96);
        Star("Gliese 232", 0.0854, 0.02, 5000.0, 2382.5, 85.95);
        Star("Gliese 877", 0.1868, 0.02, 5000.0, 3251.6, 57.64);
        Star("Gliese 502", 1.0651, 0.02, 5000.0, 5960.1, 23.31);
        Star("Gliese 438", 0.1908, 0.02, 5000.0, 3266.7, 56.84);
        Star("Gliese 1154 AB", 0.0796, 0.02, 5000.0, 2219.4, 90.0);
        Star("Gliese 1057", 0.0792, 0.02, 5000.0, 2207.9, 90.0);
        Star("Gliese 84", 0.206, 0.02, 5000.0, 3320.5, 54.4);
        Star("Gliese 257 A", 0.128, 0.02, 5000.0, 2981.7, 69.4);
        Star("Gliese 257 B", 0.12, 0.02, 5000.0, 2936.7, 71.0);
        Star("Gliese 487", 0.1562, 0.02, 5000.0, 3123.2, 63.76);
        Star("Gliese 434", 0.829, 0.02, 5000.0, 4902.7, 25.0);
        Star("Gliese 451 A", 0.6568, 0.02, 5000.0, 4146.3, 29.77);
        Star("Gliese 451 B", 0.12, 0.02, 5000.0, 2936.7, 71.0);
        Star("Gliese 827", 1.1084, 0.02, 5000.0, 6067.5, 22.18);
        Star("Gliese 695 A", 1.435, 0.02, 5000.0, 5556.0, 13.69);
        Star("Gliese 695 B", 0.1972, 0.02, 5000.0, 3290.0, 55.56);
        Star("Gliese 695 C", 0.164, 0.02, 5000.0, 3158.2, 62.2);
        Star("Gliese 475", 1.094, 0.02, 5000.0, 6044.0, 22.56);
        Star("Gliese 231", 0.8791, 0.02, 5000.0, 5159.5, 25.0);
        Star("Gliese 424", 0.291, 0.02, 5000.0, 3533.6, 45.9);
        Star("Gliese 2066", 0.2196, 0.02, 5000.0, 3364.2, 53.04);
        Star("Gliese 2069 A", 0.1132, 0.02, 5000.0, 2896.5, 72.36);
        Star("Gliese 2069 B", 0.084, 0.02, 5000.0, 2343.7, 87.0);
        Star("Gliese 1103 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1103 A", 0.0822, 0.02, 5000.0, 2293.2, 88.35);
        Star("Gliese 49", 0.268, 0.02, 5000.0, 3489.1, 48.2);
        Star("Gliese 745 A", 0.1694, 0.02, 5000.0, 3181.5, 61.12);
        Star("Gliese 745 B", 0.17, 0.02, 5000.0, 3184.1, 61.0);
        Star("Gliese 506", 0.9642, 0.02, 5000.0, 5569.3, 25.0);
        Star("Gliese 283 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 357", 0.1604, 0.02, 5000.0, 3142.2, 62.92);
        Star("Gliese 595", 0.116, 0.02, 5000.0, 2913.3, 71.8);
        Star("Gliese 51", 0.0812, 0.02, 5000.0, 2265.0, 89.1);
        Star("Gliese 183", 0.7096, 0.02, 5000.0, 4359.2, 28.01);
        Star("Gliese 250 B", 0.2276, 0.02, 5000.0, 3388.1, 52.24);
        Star("Gliese 250 A", 0.6552, 0.02, 5000.0, 4140.5, 29.83);
        Star("Gliese 785", 0.7926, 0.02, 5000.0, 4744.5, 25.25);
        Star("Gliese 367", 0.2268, 0.02, 5000.0, 3385.8, 52.32);
        Star("Gliese 91", 0.21, 0.02, 5000.0, 3333.8, 54.0);
        Star("Gliese 352 B", 0.17, 0.02, 5000.0, 3184.1, 61.0);
        Star("Gliese 352 A", 0.1716, 0.02, 5000.0, 3190.8, 60.68);
        Star("Gliese 1253", 0.079, 0.02, 5000.0, 2202.2, 90.0);
        Star("Gliese 902", 0.5878, 0.02, 5000.0, 3929.6, 32.07);
        Star("Gliese 803", 0.366, 0.02, 5000.0, 3631.1, 41.7);
        Star("Gliese 2033", 0.17, 0.02, 5000.0, 3184.1, 61.0);
        Star("Gliese 226", 0.2012, 0.02, 5000.0, 3304.1, 54.88);
        Star("Gliese 542", 0.6584, 0.02, 5000.0, 4152.2, 29.72);
        Star("Gliese 534", 1.954, 0.02, 5000.0, 6000.0, 7.46);
        Star("Gliese 654", 0.2356, 0.02, 5000.0, 3410.6, 51.44);
        Star("Gliese 633", 0.0944, 0.02, 5000.0, 2624.5, 79.2);
        Star("Gliese 137", 0.9799, 0.02, 5000.0, 5638.0, 25.0);
        Star("Gliese 1001", 0.0914, 0.02, 5000.0, 2545.3, 81.45);
        Star("Gliese 150", 1.5, 0.02, 5000.0, 5200.0, 12.0);
        Star("Gliese 370", 0.5206, 0.02, 5000.0, 3790.2, 34.31);
        Star("Gliese 638", 0.4584, 0.02, 5000.0, 3712.3, 37.08);
        Star("Gliese 222 AB", 1.1301, 0.02, 5000.0, 6084.7, 21.62);
        Star("Gliese 508 B", 0.26, 0.02, 5000.0, 3471.5, 49.0);
        Star("Gliese 1138", 0.0893, 0.02, 5000.0, 2489.0, 83.02);
        Star("Gliese 102", 0.0899, 0.02, 5000.0, 2505.1, 82.58);
        Star("Gliese 453", 0.6172, 0.02, 5000.0, 4012.3, 31.09);
        Star("Gliese 432 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 432 A", 0.7768, 0.02, 5000.0, 4667.0, 25.77);
        Star("Gliese 228 B", 0.1, 0.02, 5000.0, 2768.9, 75.0);
        Star("Gliese 1028", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 203", 0.1012, 0.02, 5000.0, 2799.3, 74.76);
        Star("Gliese 609", 0.099, 0.02, 5000.0, 2743.5, 75.75);
        Star("Gliese 341", 0.3, 0.02, 5000.0, 3548.8, 45.0);
        Star("Gliese 635 A", 1.877, 0.02, 5000.0, 6000.0, 8.23);
        Star("Gliese 635 B", 0.8724, 0.02, 5000.0, 5125.4, 25.0);
        Star("Gliese 846", 0.3408, 0.02, 5000.0, 3604.7, 42.96);
        Star("Gliese 449", 1.51, 0.02, 5000.0, 6000.0, 11.9);
        Star("Gliese 482 A", 1.585, 0.02, 5000.0, 7500.0, 11.15);
        Star("Gliese 482 B", 1.555, 0.02, 5000.0, 7500.0, 11.45);
        Star("Gliese 172", 0.4104, 0.02, 5000.0, 3669.9, 39.48);
        Star("Gliese 1125", 0.1332, 0.02, 5000.0, 3009.6, 68.36);
        Star("Gliese 1235", 0.0866, 0.02, 5000.0, 2415.5, 85.05);
        Star("Gliese 92", 1.0, 0.02, 5000.0, 5722.0, 25.0);
        Star("Gliese 1264", 0.27, 0.02, 5000.0, 3493.3, 48.0);
        Star("Gliese 1129", 0.0988, 0.02, 5000.0, 2738.4, 75.9);
        Star("Gliese 358", 0.194, 0.02, 5000.0, 3278.5, 56.2);
        Star("Gliese 75", 0.8402, 0.02, 5000.0, 4960.2, 25.0);
        Star("Gliese 442 A", 0.9978, 0.02, 5000.0, 5713.1, 25.0);
        Star("Gliese 442 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1083 AB", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1119", 0.0876, 0.02, 5000.0, 2442.9, 84.3);
        Star("Gliese 909 A", 0.7288, 0.02, 5000.0, 4442.6, 27.37);
        Star("Gliese 909 B", 0.136, 0.02, 5000.0, 3024.3, 67.8);
        Star("Gliese 1134", 0.092, 0.02, 5000.0, 2561.2, 81.0);
        Star("Gliese 423 B", 1.047, 0.02, 5000.0, 5896.5, 23.78);
        Star("Gliese 423 A", 1.2133, 0.02, 5000.0, 5992.6, 19.45);
        Star("Gliese 170", 0.0818, 0.02, 5000.0, 2281.9, 88.65);
        Star("Gliese 320", 0.7048, 0.02, 5000.0, 4339.1, 28.17);
        Star("Gliese 1265", 0.0852, 0.02, 5000.0, 2377.0, 86.1);
        Star("Gliese 218", 0.1996, 0.02, 5000.0, 3298.5, 55.08);
        Star("Gliese 631", 0.8304, 0.02, 5000.0, 4909.9, 25.0);
        Star("Gliese 27", 0.8164, 0.02, 5000.0, 4838.3, 25.0);
        Star("Gliese 536", 0.291, 0.02, 5000.0, 3533.6, 45.9);
        Star("Gliese 2036 B", 0.12, 0.02, 5000.0, 2936.7, 71.0);
        Star("Gliese 2036 A", 0.17, 0.02, 5000.0, 3184.1, 61.0);
        Star("Gliese 740", 0.348, 0.02, 5000.0, 3612.7, 42.6);
        Star("Gliese 431", 0.1466, 0.02, 5000.0, 3077.7, 65.68);
        Star("Gliese 568 A", 0.118, 0.02, 5000.0, 2925.1, 71.4);
        Star("Gliese 568 B", 0.098, 0.02, 5000.0, 2717.9, 76.5);
        Star("Gliese 136", 0.8724, 0.02, 5000.0, 5125.4, 25.0);
        Star("Gliese 1232", 0.0862, 0.02, 5000.0, 2404.5, 85.35);
        Star("Gliese 766 B", 0.088, 0.02, 5000.0, 2453.8, 84.0);
        Star("Gliese 1041 B", 0.082, 0.02, 5000.0, 2287.6, 88.5);
        Star("Gliese 1041 A", 0.1844, 0.02, 5000.0, 3242.4, 58.12);
        Star("Gliese 124", 1.36, 0.02, 5000.0, 6000.0, 15.64);
        Star("Gliese 624", 1.0325, 0.02, 5000.0, 5843.7, 24.16);
        Star("Gliese 1284", 0.17, 0.02, 5000.0, 3184.1, 61.0);
        Star("Gliese 821", 0.1956, 0.02, 5000.0, 3284.3, 55.88);
        Star("Gliese 1236", 0.1136, 0.02, 5000.0, 2899.0, 72.28);
        Star("Gliese 545", 0.096, 0.02, 5000.0, 2666.3, 78.0);
        Star("Gliese 856 B", 0.146, 0.02, 5000.0, 3074.8, 65.8);
        Star("Gliese 428 A", 0.5794, 0.02, 5000.0, 3908.6, 32.35);
        Star("Gliese 428 B", 0.468, 0.02, 5000.0, 3722.0, 36.6);
        Star("Gliese 1097", 0.1568, 0.02, 5000.0, 3125.9, 63.64);
        Star("Gliese 1238", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 190", 0.2428, 0.02, 5000.0, 3429.8, 50.72);
        Star("Gliese 433", 0.289, 0.02, 5000.0, 3530.0, 46.1);
        Star("Gliese 1277", 0.0815, 0.02, 5000.0, 2273.5, 88.88);
        Star("Gliese 362", 0.1708, 0.02, 5000.0, 3187.4, 60.84);
        Star("Gliese 865", 0.155, 0.02, 5000.0, 3117.6, 64.0);
        Star("Gliese 1187", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 86", 0.7968, 0.02, 5000.0, 4765.3, 25.11);
        Star("Gliese 1148", 0.134, 0.02, 5000.0, 3013.9, 68.2);
        Star("Gliese 653", 0.5542, 0.02, 5000.0, 3851.9, 33.19);
        Star("Gliese 706", 0.7576, 0.02, 5000.0, 4574.9, 26.41);
        Star("Gliese 349", 0.6296, 0.02, 5000.0, 4051.6, 30.68);
        Star("Gliese 327", 0.8164, 0.02, 5000.0, 4838.3, 25.0);
        Star("Gliese 399", 0.1682, 0.02, 5000.0, 3176.4, 61.36);
        Star("Gliese 325 A", 0.4284, 0.02, 5000.0, 3685.1, 38.58);
        Star("Gliese 325 B", 0.408, 0.02, 5000.0, 3667.9, 39.6);
        Star("Gliese 617 A", 0.4404, 0.02, 5000.0, 3695.5, 37.98);
        Star("Gliese 617 B", 0.214, 0.02, 5000.0, 3346.7, 53.6);
        Star("Gliese 138", 0.9731, 0.02, 5000.0, 5608.6, 25.0);
        Star("Gliese 103", 0.414, 0.02, 5000.0, 3672.9, 39.3);
        Star("Gliese 739", 0.182, 0.02, 5000.0, 3233.0, 58.6);
        Star("Gliese 356 A", 0.9373, 0.02, 5000.0, 5445.8, 25.0);
        Star("Gliese 317", 0.1328, 0.02, 5000.0, 3007.5, 68.44);
        Star("Gliese 356 B", 0.096, 0.02, 5000.0, 2666.3, 78.0);
        Star("Gliese 4 B", 0.3936, 0.02, 5000.0, 3655.8, 40.32);
        Star("Gliese 567", 0.8192, 0.02, 5000.0, 4852.6, 25.0);
        Star("Gliese 212", 0.3084, 0.02, 5000.0, 3561.9, 44.58);
        Star("Gliese 78", 0.1418, 0.02, 5000.0, 3054.0, 66.64);
        Star("Gliese 169", 0.4898, 0.02, 5000.0, 3746.5, 35.51);
        Star("Gliese 660 A", 0.132, 0.02, 5000.0, 3003.3, 68.6);
        Star("Gliese 660 B", 0.124, 0.02, 5000.0, 2959.5, 70.2);
        Star("Gliese 146", 0.45, 0.02, 5000.0, 3704.3, 37.5);
        Star("Gliese 528 A", 0.5836, 0.02, 5000.0, 3919.0, 32.21);
        Star("Gliese 585", 0.0867, 0.02, 5000.0, 2418.3, 84.98);
        Star("Gliese 575 A", 0.9978, 0.02, 5000.0, 5713.1, 25.0);
        Star("Gliese 2020", 0.312, 0.02, 5000.0, 3567.3, 44.4);
        Star("Gliese 1055", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 603", 1.565, 0.02, 5000.0, 6500.0, 11.35);
        Star("Gliese 505 A", 0.7464, 0.02, 5000.0, 4522.5, 26.79);
        Star("Gliese 505 B", 0.336, 0.02, 5000.0, 3599.0, 43.2);
        Star("Gliese 1243", 0.102, 0.02, 5000.0, 2819.5, 74.6);
        Star("Gliese 598", 1.2819, 0.02, 5000.0, 4635.4, 17.67);
        Star("Gliese 180", 0.2084, 0.02, 5000.0, 3328.5, 54.16);
        Star("Gliese 512 B", 0.0869, 0.02, 5000.0, 2423.7, 84.82);
        Star("Gliese 79", 0.4188, 0.02, 5000.0, 3676.9, 39.06);
        Star("Gliese 1223", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 676 B", 0.0918, 0.02, 5000.0, 2555.9, 81.15);
        Star("Gliese 676 A", 0.3384, 0.02, 5000.0, 3601.9, 43.08);
        Star("Gliese 488", 0.4692, 0.02, 5000.0, 3723.3, 36.54);
        Star("Gliese 97", 1.0108, 0.02, 5000.0, 5774.1, 24.72);
        Star("Gliese 1053", 0.0788, 0.02, 5000.0, 2196.4, 90.0);
        Star("Gliese 211", 0.8052, 0.02, 5000.0, 4781.5, 25.0);
        Star("Gliese 853 A", 0.9687, 0.02, 5000.0, 5589.2, 25.0);
        Star("Gliese 853 B", 0.3, 0.02, 5000.0, 3548.8, 45.0);
        Star("Gliese 145", 0.1664, 0.02, 5000.0, 3168.7, 61.72);
        Star("Gliese 309", 0.7842, 0.02, 5000.0, 4703.1, 25.53);
        Star("Gliese 359", 0.082, 0.02, 5000.0, 2287.6, 88.5);
        Star("Gliese 452 A", 0.1436, 0.02, 5000.0, 3062.9, 66.28);
        Star("Gliese 452 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 601 A", 2.213, 0.02, 5000.0, 7167.0, 6.47);
        Star("Gliese 364", 1.1193, 0.02, 5000.0, 6077.9, 21.9);
        Star("Gliese 443", 0.1544, 0.02, 5000.0, 3114.8, 64.12);
        Star("Gliese 798", 0.4332, 0.02, 5000.0, 3689.2, 38.34);
        Star("Gliese 55", 1.1084, 0.02, 5000.0, 6067.5, 22.18);
        Star("Gliese 680", 0.278, 0.02, 5000.0, 3509.5, 47.2);
        Star("Gliese 1288", 0.0807, 0.02, 5000.0, 2250.8, 89.48);
        Star("Gliese 1263", 0.1116, 0.02, 5000.0, 2886.8, 72.68);
        Star("Gliese 420 B", 0.17, 0.02, 5000.0, 3184.1, 61.0);
        Star("Gliese 269 A", 0.6664, 0.02, 5000.0, 4182.1, 29.45);
        Star("Gliese 269 B", 0.55, 0.02, 5000.0, 3843.3, 33.33);
        Star("Gliese 590", 0.104, 0.02, 5000.0, 2838.7, 74.2);
        Star("Gliese 400 B", 0.128, 0.02, 5000.0, 2981.7, 69.4);
        Star("Gliese 32 B", 0.4068, 0.02, 5000.0, 3666.9, 39.66);
        Star("Gliese 32 A", 0.4856, 0.02, 5000.0, 3741.5, 35.72);
        Star("Gliese 549 A", 1.5, 0.02, 5000.0, 6333.0, 12.0);
        Star("Gliese 688", 0.7704, 0.02, 5000.0, 4636.0, 25.99);
        Star("Gliese 118", 0.1764, 0.02, 5000.0, 3210.6, 59.72);
        Star("Gliese 1075", 0.4128, 0.02, 5000.0, 3671.9, 39.36);
        Star("Gliese 549 B", 0.1676, 0.02, 5000.0, 3173.8, 61.48);
        Star("Gliese 835", 0.3096, 0.02, 5000.0, 3563.7, 44.52);
        Star("Gliese 95", 0.7982, 0.02, 5000.0, 4772.2, 25.06);
        Star("Gliese 816", 0.19, 0.02, 5000.0, 3263.7, 57.0);
        Star("Gliese 611 A", 0.7512, 0.02, 5000.0, 4544.8, 26.63);
        Star("Gliese 1101", 0.0978, 0.02, 5000.0, 2712.7, 76.65);
        Star("Gliese 843", 0.138, 0.02, 5000.0, 3034.7, 67.4);
        Star("Gliese 1068", 0.089, 0.02, 5000.0, 2480.9, 83.25);
        Star("Gliese 507 B", 0.1356, 0.02, 5000.0, 3022.3, 67.88);
        Star("Gliese 848", 1.665, 0.02, 5000.0, 6667.0, 10.35);
        Star("Gliese 390", 0.282, 0.02, 5000.0, 3517.2, 46.8);
        Star("Gliese 435", 0.5808, 0.02, 5000.0, 3912.0, 32.31);
        Star("Gliese 200 A", 0.5906, 0.02, 5000.0, 3936.9, 31.98);
        Star("Gliese 200 B", 0.158, 0.02, 5000.0, 3131.4, 63.4);
        Star("Gliese 791", 0.1772, 0.02, 5000.0, 3213.9, 59.56);
        Star("Gliese 914 B", 0.42, 0.02, 5000.0, 3677.9, 39.0);
        Star("Gliese 1029", 0.0785, 0.02, 5000.0, 2187.8, 90.0);
        Star("Gliese 914 A", 0.8925, 0.02, 5000.0, 5227.1, 25.0);
        Star("Gliese 462", 0.366, 0.02, 5000.0, 3631.1, 41.7);
        Star("Gliese 107 B", 0.294, 0.02, 5000.0, 3538.8, 45.6);
        Star("Gliese 107 A", 1.485, 0.02, 5000.0, 6333.0, 12.39);
        Star("Gliese 302", 0.8567, 0.02, 5000.0, 5045.0, 25.0);
        Star("Gliese 810 B", 0.0798, 0.02, 5000.0, 2225.1, 90.0);
        Star("Gliese 448", 2.87, 0.02, 5000.0, 9167.0, 4.82);
        Star("Gliese 1248", 0.1368, 0.02, 5000.0, 3028.5, 67.64);
        Star("Gliese 177", 0.9664, 0.02, 5000.0, 5579.1, 25.0);
        Star("Gliese 810 A", 0.1224, 0.02, 5000.0, 2950.5, 70.52);
        Star("Gliese 477", 0.2036, 0.02, 5000.0, 3312.4, 54.64);
        Star("Gliese 1215", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 454", 0.9575, 0.02, 5000.0, 5539.2, 25.0);
        Star("Gliese 2045", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 1210", 0.0853, 0.02, 5000.0, 2379.7, 86.02);
        Star("Gliese 469", 0.1392, 0.02, 5000.0, 3040.8, 67.16);
        Star("Gliese 1218", 0.1144, 0.02, 5000.0, 2903.8, 72.12);
        Star("Gliese 395", 1.1916, 0.02, 5000.0, 6045.7, 20.02);
        Star("Gliese 891", 0.194, 0.02, 5000.0, 3278.5, 56.2);
        Star("Gliese 897 B", 0.186, 0.02, 5000.0, 3248.6, 57.8);
        Star("Gliese 897 A", 0.218, 0.02, 5000.0, 3359.3, 53.2);
        Star("Gliese 898", 0.474, 0.02, 5000.0, 3728.4, 36.3);
        Star("Gliese 671", 0.1852, 0.02, 5000.0, 3245.5, 57.96);
        Star("Gliese 1214", 0.0795, 0.02, 5000.0, 2216.5, 90.0);
        Star("Gliese 1073", 0.0926, 0.02, 5000.0, 2577.1, 80.55);
        Star("Gliese 401 A", 0.2124, 0.02, 5000.0, 3341.6, 53.76);
        Star("Gliese 842", 0.3372, 0.02, 5000.0, 3600.5, 43.14);
        Star("Gliese 529", 0.5346, 0.02, 5000.0, 3814.1, 33.85);
        Star("Gliese 805", 1.52, 0.02, 5000.0, 6667.0, 11.8);
        Star("Gliese 1049", 0.3504, 0.02, 5000.0, 3615.3, 42.48);
        Star("Gliese 1077", 0.1496, 0.02, 5000.0, 3092.2, 65.08);
        Star("Gliese 422", 0.1658, 0.02, 5000.0, 3166.1, 61.84);
        Star("Gliese 833", 0.6904, 0.02, 5000.0, 4278.2, 28.65);
        Star("Gliese 324 B", 0.0988, 0.02, 5000.0, 2738.4, 75.9);
        Star("Gliese 592", 0.1152, 0.02, 5000.0, 2908.6, 71.96);
        Star("Gliese 324 A", 0.8791, 0.02, 5000.0, 5159.5, 25.0);
        Star("Gliese 195 B", 0.089, 0.02, 5000.0, 2480.9, 83.25);
        Star("Gliese 472", 0.6936, 0.02, 5000.0, 4291.7, 28.55);
        Star("Gliese 500", 0.6568, 0.02, 5000.0, 4146.3, 29.77);
        Star("Gliese 813", 0.1448, 0.02, 5000.0, 3068.9, 66.04);
        Star("Gliese 807", 1.912, 0.02, 5000.0, 5200.0, 7.88);
        Star("Gliese 1180", 0.2316, 0.02, 5000.0, 3399.5, 51.84);
        Star("Gliese 41", 1.2205, 0.02, 5000.0, 5969.5, 19.27);
        Star("Gliese 2049", 0.2372, 0.02, 5000.0, 3414.9, 51.28);
        Star("Gliese 1092", 0.0884, 0.02, 5000.0, 2464.6, 83.7);
        Star("Gliese 675", 0.8038, 0.02, 5000.0, 4774.4, 25.0);
        Star("Gliese 167", 0.6158, 0.02, 5000.0, 4008.1, 31.14);
        Star("Gliese 613", 0.7016, 0.02, 5000.0, 4325.8, 28.28);
        Star("Gliese 368", 1.1301, 0.02, 5000.0, 6084.7, 21.62);
        Star("Gliese 164", 0.0924, 0.02, 5000.0, 2571.9, 80.7);
        Star("Gliese 732 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 597", 0.136, 0.02, 5000.0, 3024.3, 67.8);
        Star("Gliese 732 A", 0.1168, 0.02, 5000.0, 2918.1, 71.64);
        Star("Gliese 862", 0.6158, 0.02, 5000.0, 4008.1, 31.14);
        Star("Gliese 69", 0.5094, 0.02, 5000.0, 3772.8, 34.69);
        Star("Gliese 458 B", 0.0958, 0.02, 5000.0, 2661.1, 78.15);
        Star("Gliese 1267", 0.4272, 0.02, 5000.0, 3684.0, 38.64);
        Star("Gliese 1135", 0.318, 0.02, 5000.0, 3575.8, 44.1);
        Star("Gliese 1088", 0.132, 0.02, 5000.0, 3003.3, 68.6);
        Star("Gliese 1012", 0.1376, 0.02, 5000.0, 3032.6, 67.48);
        Star("Gliese 774 A", 0.1932, 0.02, 5000.0, 3275.5, 56.36);
        Star("Gliese 774 B", 0.1128, 0.02, 5000.0, 2894.1, 72.44);
        Star("Gliese 126", 0.1796, 0.02, 5000.0, 3223.5, 59.08);
        Star("Gliese 282 B", 0.4452, 0.02, 5000.0, 3699.9, 37.74);
        Star("Gliese 282 A", 0.692, 0.02, 5000.0, 4284.9, 28.6);
        Star("Gliese 771 B", 0.186, 0.02, 5000.0, 3248.6, 57.8);
        Star("Gliese 771 A", 1.765, 0.02, 5000.0, 5289.0, 9.35);
        Star("Gliese 767 A", 0.3, 0.02, 5000.0, 3548.8, 45.0);
        Star("Gliese 360", 0.258, 0.02, 5000.0, 3466.9, 49.2);
        Star("Gliese 767 B", 0.214, 0.02, 5000.0, 3346.7, 53.6);
        Star("Gliese 259", 0.7704, 0.02, 5000.0, 4636.0, 25.99);
        Star("Gliese 527 B", 0.226, 0.02, 5000.0, 3383.4, 52.4);
        Star("Gliese 527 A", 1.375, 0.02, 5000.0, 6333.0, 15.25);
        Star("Gliese 133", 0.2068, 0.02, 5000.0, 3323.2, 54.32);
        Star("Gliese 672", 1.0361, 0.02, 5000.0, 5856.8, 24.06);
        Star("Gliese 599 A", 0.8858, 0.02, 5000.0, 5193.4, 25.0);
        Star("Gliese 1194 A", 0.1276, 0.02, 5000.0, 2979.5, 69.48);
        Star("Gliese 1194 B", 0.089, 0.02, 5000.0, 2480.9, 83.25);
        Star("Gliese 377", 0.1892, 0.02, 5000.0, 3260.7, 57.16);
        Star("Gliese 1035", 0.0796, 0.02, 5000.0, 2219.4, 90.0);
        Star("Gliese 414 A", 0.529, 0.02, 5000.0, 3804.2, 34.03);
        Star("Gliese 93", 0.19, 0.02, 5000.0, 3263.7, 57.0);
        Star("Gliese 414 B", 0.324, 0.02, 5000.0, 3583.9, 43.8);
        Star("Gliese 67", 1.2024, 0.02, 5000.0, 6022.2, 19.74);
        Star("Gliese 737 A", 0.3876, 0.02, 5000.0, 3650.7, 40.62);
        Star("Gliese 737 B", 0.3216, 0.02, 5000.0, 3580.7, 43.92);
        Star("Gliese 646 B", 0.28, 0.02, 5000.0, 3513.3, 47.0);
        Star("Gliese 646 A", 0.5052, 0.02, 5000.0, 3766.7, 34.83);
        Star("Gliese 111", 1.41, 0.02, 5000.0, 6500.0, 14.34);
        Star("Gliese 189", 1.2892, 0.02, 5000.0, 4618.7, 17.48);
        Star("Gliese 788", 0.9082, 0.02, 5000.0, 5305.3, 25.0);
        Star("Gliese 333", 0.14, 0.02, 5000.0, 3044.9, 67.0);
        Star("Gliese 162", 0.3012, 0.02, 5000.0, 3550.7, 44.94);
        Star("Gliese 127 B", 0.78, 0.02, 5000.0, 4682.5, 25.67);
        Star("Gliese 127 A", 1.67, 0.02, 5000.0, 6333.0, 10.3);
        Star("Gliese 750 B", 0.312, 0.02, 5000.0, 3567.3, 44.4);
        Star("Gliese 750 A", 0.3096, 0.02, 5000.0, 3563.7, 44.52);
        Star("Gliese 760", 2.038, 0.02, 5000.0, 7500.0, 6.9);
        Star("Gliese 904", 1.585, 0.02, 5000.0, 6333.0, 11.15);
        Star("Gliese 611 B", 0.0847, 0.02, 5000.0, 2363.1, 86.48);
        Star("Gliese 1270", 0.099, 0.02, 5000.0, 2743.5, 75.75);
        Star("Gliese 524", 0.1284, 0.02, 5000.0, 2983.9, 69.32);
        Star("Gliese 311", 0.9776, 0.02, 5000.0, 5628.1, 25.0);
        Star("Gliese 1292", 0.1694, 0.02, 5000.0, 3181.5, 61.12);
        Star("Gliese 25 B", 0.822, 0.02, 5000.0, 4866.9, 25.0);
        Star("Gliese 25 A", 0.8472, 0.02, 5000.0, 4996.2, 25.0);
        Star("Gliese 394", 0.4828, 0.02, 5000.0, 3738.2, 35.86);
        Star("Gliese 407", 1.1807, 0.02, 5000.0, 6063.7, 20.3);
        Star("Gliese 722", 0.9306, 0.02, 5000.0, 5414.0, 25.0);
        Star("Gliese 1182", 0.0842, 0.02, 5000.0, 2349.2, 86.85);
        Star("Gliese 838", 0.9933, 0.02, 5000.0, 5694.5, 25.0);
        Star("Gliese 142", 0.5262, 0.02, 5000.0, 3799.5, 34.13);
        Star("Gliese 46", 0.167, 0.02, 5000.0, 3171.3, 61.6);
        Star("Gliese 703", 0.4856, 0.02, 5000.0, 3741.5, 35.72);
        Star("Gliese 770", 0.8612, 0.02, 5000.0, 5068.1, 25.0);
        Star("Gliese 836", 0.0958, 0.02, 5000.0, 2661.1, 78.15);
        Star("Gliese 215", 0.4452, 0.02, 5000.0, 3699.9, 37.74);
        Star("Gliese 1072", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 331 C", 0.226, 0.02, 5000.0, 3383.4, 52.4);
        Star("Gliese 331 A", 2.213, 0.02, 5000.0, 8056.0, 6.47);
        Star("Gliese 204", 0.6344, 0.02, 5000.0, 4067.5, 30.52);
        Star("Gliese 174", 0.5836, 0.02, 5000.0, 3919.0, 32.21);
        Star("Gliese 20", 1.7, 0.02, 5000.0, 8056.0, 10.0);
        Star("Gliese 685", 0.3324, 0.02, 5000.0, 3594.7, 43.38);
        Star("Gliese 1181 AB", 0.378, 0.02, 5000.0, 3642.2, 41.1);
        Star("Gliese 878", 0.1192, 0.02, 5000.0, 2932.1, 71.16);
        Star("Gliese 238", 0.1796, 0.02, 5000.0, 3223.5, 59.08);
        Star("Gliese 726", 0.4716, 0.02, 5000.0, 3725.8, 36.42);
        Star("Gliese 716", 0.7688, 0.02, 5000.0, 4628.3, 26.04);
        Star("Gliese 510", 0.226, 0.02, 5000.0, 3383.4, 52.4);
        Star("Gliese 844", 0.263, 0.02, 5000.0, 3478.2, 48.7);
        Star("Gliese 899", 0.2164, 0.02, 5000.0, 3354.3, 53.36);
        Star("Gliese 1220", 0.0857, 0.02, 5000.0, 2390.8, 85.72);
        Star("Gliese 775", 0.668, 0.02, 5000.0, 4188.2, 29.4);
        Star("Gliese 59 B", 0.1196, 0.02, 5000.0, 2934.4, 71.08);
        Star("Gliese 348 A", 1.38, 0.02, 5000.0, 6500.0, 15.12);
        Star("Gliese 59 A", 0.7464, 0.02, 5000.0, 4522.5, 26.79);
        Star("Gliese 383", 0.3384, 0.02, 5000.0, 3601.9, 43.08);
        Star("Gliese 192", 0.25, 0.02, 5000.0, 3447.9, 50.0);
        Star("Gliese 348 B", 0.7128, 0.02, 5000.0, 4372.8, 27.91);
        Star("Gliese 589 A", 0.1344, 0.02, 5000.0, 3016.0, 68.12);
        Star("Gliese 589 B", 0.0789, 0.02, 5000.0, 2199.3, 90.0);
        Star("Gliese 5", 0.8791, 0.02, 5000.0, 5159.5, 25.0);
        Star("Gliese 796", 0.8374, 0.02, 5000.0, 4945.8, 25.0);
        Star("Gliese 44", 0.3972, 0.02, 5000.0, 3658.9, 40.14);
        Star("Gliese 1279", 0.5332, 0.02, 5000.0, 3811.6, 33.89);
        Star("Gliese 90", 0.7288, 0.02, 5000.0, 4442.6, 27.37);
        Star("Gliese 292 A", 1.2024, 0.02, 5000.0, 6022.2, 19.74);
        Star("Gliese 292 B", 0.508, 0.02, 5000.0, 3770.8, 34.73);
        Star("Gliese 615", 0.66, 0.02, 5000.0, 4158.1, 29.67);
        Star("Gliese 156", 0.4512, 0.02, 5000.0, 3705.4, 37.44);
        Star("Gliese 652", 0.808, 0.02, 5000.0, 4795.6, 25.0);
        Star("Gliese 556", 0.708, 0.02, 5000.0, 4352.5, 28.07);
        Star("Gliese 240", 0.3792, 0.02, 5000.0, 3643.3, 41.04);
        Star("Gliese 564", 0.9463, 0.02, 5000.0, 5487.9, 25.0);
        Star("Gliese 253", 0.5668, 0.02, 5000.0, 3879.0, 32.77);
        Star("Gliese 197", 1.34, 0.02, 5000.0, 5822.0, 16.16);
        Star("Gliese 476", 0.1996, 0.02, 5000.0, 3298.5, 55.08);
        Star("Gliese 363", 0.132, 0.02, 5000.0, 3003.3, 68.6);
        Star("Gliese 40 A", 0.4608, 0.02, 5000.0, 3714.7, 36.96);
        Star("Gliese 714", 0.354, 0.02, 5000.0, 3619.1, 42.3);
        Star("Gliese 40 B", 0.078, 0.02, 5000.0, 2173.3, 90.0);
        Star("Gliese 868", 0.6172, 0.02, 5000.0, 4012.3, 31.09);
        Star("Gliese 354 B", 0.09, 0.02, 5000.0, 2507.8, 82.5);
    }

    /// <summary>单位表：12 个分类，换算率都落到国际单位；温度用 offset。</summary>
    private static void RegisterUnits() {
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "power",
            units = new[] {
                SandboxUnits.NewUnit("Lsun", 3.828E+26, 0.0, "L☉"),
                SandboxUnits.NewUnit("LR136a1", 3.0624E+33),
            }.Concat(SandboxRegister.StandardUnits("W", 1.0, false, true)).ToArray()
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "length",
            units = new[] {
                SandboxUnits.NewUnit("mi", 1609.344),
                SandboxUnits.NewUnit("Rearth", 6371000.0, 0.0, "R⊕"),
                SandboxUnits.NewUnit("Rsun", 695700000.0, 0.0, "R☉"),
                SandboxUnits.NewUnit("AU", 149597870700.0),
                SandboxUnits.NewUnit("ly", 9.4607304725808E+15),
                SandboxUnits.NewUnit("pc", 30856775814913672.0),
                SandboxUnits.NewUnit("Mpc", 3.085677581491367E+22),
            }.Concat(SandboxRegister.StandardUnits("m", 1.0, false, true)).ToArray()
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "time",
            units = new[] {
                SandboxUnits.NewUnit("ms", 0.001),
                SandboxUnits.NewUnit("s", 1.0),
                SandboxUnits.NewUnit("min", 60.0),
                SandboxUnits.NewUnit("hr", 3600.0),
                SandboxUnits.NewUnit("d", 86400.0),
                SandboxUnits.NewUnit("mth", 2629746.0),
            }.Concat(SandboxRegister.StandardUnits("yr", 31556952.0)).ToArray()
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "energy",
            units = SandboxRegister.StandardUnits("J", 1.0, false, true)
        });

        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "temperature",
            units = new[] {
                SandboxUnits.NewUnit("K", 1.0),
                SandboxUnits.NewUnit("C", 1.0, 273.15, "°C"),
                SandboxUnits.NewUnit("F", 0.55555555555555558, 255.37222222222221, "°F"),
            }
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "mass",
            units = new[] {
                SandboxUnits.NewUnit("kg", 1.0),
                SandboxUnits.NewUnit("t", 1000.0),
                SandboxUnits.NewUnit("Mearth", 5.9722E+24, 0.0, "M⊕"),
                SandboxUnits.NewUnit("Mjup", 1.89813E+27, 0.0, "M_J"),
                SandboxUnits.NewUnit("Msun", 1.98847E+30, 0.0, "M☉"),
            }
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "speed",
            units = new[] {
                SandboxUnits.NewUnit("m/s", 1.0),
                SandboxUnits.NewUnit("km/h", 0.27777777777777779),
                SandboxUnits.NewUnit("mph", 0.44704),
                SandboxUnits.NewUnit("km/s", 1000.0),
                SandboxUnits.NewUnit("c", 299792458.0),
            }
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "force",
            units = new[] {
                SandboxUnits.NewUnit("地月引力", 1.9805E+20),
                SandboxUnits.NewUnit("日地引力", 3.5413E+22),
            }.Concat(SandboxRegister.StandardUnits("N")).ToArray()
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "density",
            units = new[] {
                SandboxUnits.NewUnit("kg/m^3", 1.0, 0.0, "kg/m³"),
                SandboxUnits.NewUnit("g/cm^3", 1000.0, 0.0, "g/cm³"),
            }
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "concentration",
            units = new[] {
                SandboxUnits.NewUnit("mmol/L", 1.0),
                SandboxUnits.NewUnit("mol/L", 1000.0),
            }
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "angle",
            units = new[] {
                SandboxUnits.NewUnit("度", 1.0),
                SandboxUnits.NewUnit("弧度", 57.295779513082323),
                SandboxUnits.NewUnit("GRA", 0.9),
            }
        });
        SandboxRegister.RegisterUnitCategory(new SandboxUnitCategory {
            key = "wavelength",
            units = new[] {
                SandboxUnits.NewUnit("nm", 1.0),
                SandboxUnits.NewUnit("μm", 1000.0),
                SandboxUnits.NewUnit("Å", 0.1),
            }
        });
    }

    /// <summary>恒星简写，避免 1500 条数据每条都写一遍 new StarData {...}。</summary>
    private static void Star(string name, double mass, double metallicity, double age,
            double temperature, double rotationPeriodDays) {
        SandboxRegister.RegisterStar(new StarData {
            name = name,
            mass = mass,
            metallicity = metallicity,
            age = age,
            temp_review = temperature,
            rotationPeriodDays = rotationPeriodDays,
        });
    }

    /// <summary>场景介绍（沙盘打开内置场景时弹出的说明）。</summary>
    private static void RegisterSceneIntros() {
        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_solar",
            title = "太阳系",
            body = "太阳系以太阳为中心，八大行星沿各自的椭圆轨道公转。每颗行星的轨道倾角和升交点都不相同，所以轨道面并不是完全共面的：水星倾角 7.0°，冥王星 17.1°，阋神星更是达到 44°。\n\n内侧四颗是岩质行星（水星、金星、地球、火星），外侧四颗是气态与冰巨行星（木星、土星、天王星、海王星）。\n\n远处的柯伊伯带及更外面分布着矮行星：谷神星、冥王星、亡神星、创神星、阋神星、鸟神星、共工星，以及轨道极端扁长的赛德娜（偏心率 0.85，远日点超过 900 AU）。\n\n本场景所有天体都从远日点出发，赛德娜从近日点出发，初始速度按真实轨道根数计算。可以用下方的时间面板调整流速，观察轨道的演化；也可以拖动鼠标旋转视角，滚轮缩放。",
        });

        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_aquarius",
            title = "水瓶座一星系",
            body = "水瓶座一是本作作者虚构的一颗恒星，这个名字源于这位作者的bilibili名。虽然考虑到现实中有宝瓶座一（离珠三），但还是选择了这个名字（毕竟水瓶座是占星学术语，天文学用的很少，所以保留了这个名字，也还能区分的开）。它的质量是太阳的1.55倍。\n\n围绕它运行着三颗行星：\n\n水瓶座一b——内侧的岩质行星，轨道最靠近恒星；\n水瓶座一c——中间的冰质行星，表面覆盖着厚厚的冰层；\n水瓶座一d——最外侧的成员，在设定里它并不是原生的行星，而是被水瓶座一先后捕获的次褐矮星（sub-brown dwarf），质量约 8 倍木星质量。因为质量远大于普通行星，它的自转、热演化和表面纹理都保留了褐矮星的痕迹。\n\n这三颗行星的轨道互不相同，可以在场景里选中它们，查看质量、大气、能量收支等参数。",
        });

        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_antares",
            title = "心大星行星系",
            body = "这是本游戏的一位作者在 2017 年 11 月底自己的漫画里虚构的一个心宿二（心大星）行星系。\n\n作者本人当然清楚，这颗行星的大小和质量并不合理——2 倍太阳质量、半径 49 万公里，已经完全不是行星该有的样子。但当时设定的时候只是想着：毕竟是漫画，天马行空一点也不为过。于是这个设定就一直保留了下来，成了现在这颗“行星”。\n\n在心大星的设定里，心大星Ab 围绕心宿二（心大星A）主星运行，两者相距 1004.2 亿公里（约 671 AU），轨道离心率 0.057。\n\n这颗行星被标记为 SpecialPlanets：改它的质量不会把它变成气态巨行星或恒星，它会一直是漫画里那颗心大星Ab。",
        });

        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_moons_earth",
            title = "地球的主要卫星",
            body = "地球只有一颗天然卫星——月球。\n\n月球平均轨道半径 384,400 公里，轨道呈近圆形，由于潮汐锁定，它永远以同一面朝向地球。\n\n在场景里可以选中月球，查看它的质量、半径、表面温度和轨道参数。",
        });

        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_moons_jupiter",
            title = "木星的主要卫星",
            body = "木星拥有太阳系中最庞大的卫星系统，这里收录了创建面板里的四颗伽利略卫星：\n\n木卫一（Io）——离木星最近，火山活动最剧烈；\n木卫二（Europa）——冰壳之下可能存在液态海洋；\n木卫三（Ganymede）——太阳系最大的卫星，比水星还大；\n木卫四（Callisto）——表面布满古老撞击坑。\n\n四颗卫星的轨道半径从 42 万到 188 万公里不等，越往外公转周期越长。",
        });

        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_moons_saturn",
            title = "土星的主要卫星",
            body = "土星的卫星系统同样庞大，这里收录了创建面板里的六颗：\n\n土卫二（Enceladus）——南极喷出冰羽，是土星环物质的来源之一；\n土卫三（Tethys）——冰质卫星，表面有巨大的奥德修斯撞击坑；\n土卫四（Dione）——冰壳上分布着明亮的冰崖；\n土卫五（Rhea）——土星第二大卫星；\n土卫六（Titan）——拥有浓密大气和液态甲烷湖泊；\n土卫八（Iapetus）——两半球明暗分明，还有赤道脊。\n\n注意土卫六是唯一带大气层的，选中它可以看到大气质量与温室效应参数。",
        });

        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_moons_uranus",
            title = "天王星的主要卫星",
            body = "天王星的主要卫星都以莎士比亚、蒲柏作品中的角色命名，这里收录了五颗：\n\n天卫五（Miranda）——表面有高达 20 公里的悬崖；\n天卫一（Ariel）——冰质表面最年轻、最明亮；\n天卫二（Umbriel）——最暗的一颗；\n天卫三（Titania）——天王星最大的卫星；\n天卫四（Oberon）——布满古老撞击坑。\n\n它们的轨道都位于天王星赤道面附近，而天王星自转轴几乎躺倒，所以整个卫星系统是斜着绕行的。",
        });

        SandboxRegister.RegisterSceneIntro(new SceneIntro {
            id = "default_moons_neptune",
            title = "海王星的主要卫星",
            body = "海王星的卫星系统里，最特别的是海卫一（Triton）。\n\n海卫一轨道半径约 354,759 公里，但它是一颗逆行卫星——公转方向与海王星自转相反，通常认为它是被海王星捕获的柯伊伯带天体。它的表面温度只有约 38 K，却仍有稀薄的氮气大气和冰火山活动。\n\n场景里可以选中它，观察逆行轨道与极低表面温度的组合。",
        });

    }

    /// <summary>内置恒星系（演化场景里“选择恒星系”的列表）。值与原来 Data/EvolutionSystems 的 JSON 一致。</summary>
    private static void RegisterEvolutionSystems() {
        SandboxRegister.RegisterEvolutionSystem(new EvolutionSystemData {
            name = "太阳系",
            planets = new List<EvolutionPlanetData> {
                Orbit("水星", 0.387104, 57910000.0, 0.2056, 0.75f, 0.75f, 0.75f),
                Orbit("金星", 0.723272, 108200000.0, 0.0068, 1.0f, 0.82f, 0.45f),
                Orbit("地球", 1.000014, 149600000.0, 0.0167, 0.25f, 0.65f, 1.0f),
                Orbit("火星", 1.523417, 227900000.0, 0.0934, 1.0f, 0.35f, 0.2f),
                Orbit("木星", 5.203951, 778500000.0, 0.0489, 0.55f, 0.35f, 0.2f),
                Orbit("土星", 9.579013, 1433000000.0, 0.0565, 1.0f, 0.85f, 0.5f),
                Orbit("天王星", 19.198134, 2872000000.0, 0.0457, 0.45f, 0.9f, 0.95f),
                Orbit("海王星", 30.047219, 4495000000.0, 0.0113, 0.25f, 0.45f, 1.0f),
            }
        });

        SandboxRegister.RegisterEvolutionSystem(new EvolutionSystemData {
            name = "水瓶座一系",
            planets = new List<EvolutionPlanetData> {
                Orbit("Planet 01", 3.01, 450289590.807, 0.024, 0.7843137f, 1.0f, 0.7843137f),
                Orbit("Planet 02", 5.07, 758461204.449, 0.016, 0.1960784f, 0.1960784f, 0.3921569f),
                Orbit("Planet 03", 81.8, 12237105823.26, 0.534, 1.0f, 0.5019608f, 0.0f),
            }
        });
    }

    private static EvolutionPlanetData Orbit(string name, double au, double km, double eccentricity,
            float r, float g, float b) {
        return new EvolutionPlanetData {
            name = name,
            semiMajorAxisAu = au,
            semiMajorAxisKm = km,
            eccentricity = eccentricity,
            colorR = r,
            colorG = g,
            colorB = b
        };
    }
}
