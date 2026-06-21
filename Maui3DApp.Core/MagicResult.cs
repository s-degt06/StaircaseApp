namespace Maui3DApp.Models;

public record MagicResult(
    double Nh,
    int    G,
    int    V,
    double SL,
    int    Middle,
    int?   Diff
);

public static class StairsCalculator
{
    public static MagicResult Magic(
        double A, double B, double C,
        double h1, double h2, double N)
    {
        // Nh
        double Nh = h1 / N;
        if      (Nh > 180) Nh = 180;
        else if (Nh < 170) Nh = 170;

        // G
        const int G = 250;

        // SL
        double SL = B < 2000 ? B / 2 : 1000;

        // V, Middle, Diff
        int  V      = 0;
        int  Middle = 0;
        int? Diff   = null;

        if      (A >= 2750 && A <= 2900) { Diff = 2; }
        else if (A >= 2500 && A <= 2650) { Diff = 3; Middle = 2; }
        else if (A >= 2250 && A <= 2400) { Diff = 1; Middle = 6; }

        return new MagicResult(Nh, G, V, SL, Middle, Diff);
    }
}