using Unity.Mathematics;

public static class StellarEvolutionEngine {
    public static double GetTau(double mass) {
        if (0.077 <= mass && mass < 0.8) {
            return 10000 * math.pow(mass, -2.7) * (1.2 - 0.6 * mass);
        } else if (0.8 <= mass && mass < 1) {
            return 10000 * math.pow(mass, -3) * (2 - mass);
        } else if (mass == 1) {
            return 10000;
        } else if (1 < mass && mass <= 2) {
            return 25000.0 / 3.0 * math.pow(mass, -3);
        } else if (2 < mass && mass <= 10.0 / 3.0) {
            return 12500.0 / 3.0 * math.pow(mass, -2);
        } else if (10.0 / 3.0 < mass && mass <= 10) {
            return 125000.0 / 9.0 * math.pow(mass, -3);
        } else if (10 < mass && mass <= 12) {
            return 50.0 / 3.0 * math.pow(mass / 10.0, -1.542);
        } else if (12 < mass && mass <= 16) {
            return 50.0 / 3.0 * math.pow(mass / 10.0, -1.542) * (0.0375 * mass + 0.55);
        } else if (16 < mass && mass <= 30) {
            return 3714.0 / 575.0 * (27.0 / 280.0 * mass - 11.0 / 28.0) * math.pow(mass / 16.0, -1.93);
        }
        return 0.0;
    }

    public static double GetMainSequenceLifespan(double mass, double metallicity) {
        return GetTau(mass) * math.pow(metallicity / 0.02, 0.19953);
    }

    public static double GetRedGiantLifespan(double mass, double metallicity) {
        // TODO
        return 0.2 * GetMainSequenceLifespan(mass, metallicity);
    }
}
