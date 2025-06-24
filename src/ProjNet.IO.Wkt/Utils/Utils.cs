using System;
using System.Globalization;

namespace ProjNet.IO.Wkt.Utils
{
    /// <summary>
    /// Helper functions for WKT Parser(s).
    /// </summary>
    public class Utils
    {
        internal static double CalcAsFractionOf(uint i, string fraction)
        {
            int fractionDigits = fraction.Length;

            double d = i;
            double f = double.Parse(fraction, NumberStyles.Any, CultureInfo.InvariantCulture);

            // Calculate the fractional part from f based on the number of fractional digits
            double divisor = Math.Pow(10, fractionDigits);
            double fractionPart = f / divisor;

            // Sum i and the fractional part
            return d + fractionPart;
        }
    }
}
