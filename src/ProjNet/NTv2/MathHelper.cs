namespace ProjNet.NTv2
{
    using System;

    static class MathHelper
    {
        const double EPSILON = 1e-12;

        public static bool AlmostEqual(double a, double b)
        {
            return (a == b) || Math.Abs(a - b) <= EPSILON * (1 + (Math.Abs(a) + Math.Abs(b)) / 2);
        }

        public static bool AlmostZero(double a)
        {
            return (a == 0.0) || Math.Abs(a) <= EPSILON;
        }

        public static int Round(double a)
        {
            if (a == 0.0) return 0;

            return (a < 0.0) ? (int)(a - 0.5) : (int)(a + 0.5);
        }
    }
}
