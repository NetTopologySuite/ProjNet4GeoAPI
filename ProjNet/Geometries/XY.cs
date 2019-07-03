using System.Runtime.InteropServices;

namespace ProjNet.Geometries
{
    /// <summary>
    /// A pair of X- and Y-ordinates, laid out in that order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XY
    {
        /// <summary>
        /// The x-ordinate value
        /// </summary>
        public double X;

        /// <summary>
        /// The y-ordinate value
        /// </summary>
        public double Y;
    }
}
