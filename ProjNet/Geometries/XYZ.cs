using System.Runtime.InteropServices;

namespace ProjNet.Geometries
{
    /// <summary>
    /// A tuple of X-, Y- and Z-ordinate values, laid out in that order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XYZ
    {
        /// <summary>
        /// The X-ordinate value
        /// </summary>
        public double X;

        /// <summary>
        /// The Y-ordinate value
        /// </summary>
        public double Y;

        /// <summary>
        /// The Z-ordinate value
        /// </summary>
        public double Z;
    }
}
