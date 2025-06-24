using System;
using ProjNet.IO.Wkt.Core;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktSpheroid class.
    /// </summary>
    public class WktSpheroid : WktEllipsoid, IEquatable<WktSpheroid>
    {
        /// <summary>
        /// Constructor for a WktSpheroid.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="semiMajorAxis"></param>
        /// <param name="inverseFlattening"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktSpheroid(string name, double semiMajorAxis, double inverseFlattening, WktAuthority authority,
                            string keyword = "SPHEROID", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(name, semiMajorAxis, inverseFlattening, authority, keyword, leftDelimiter, rightDelimiter)
        {
        }



        /// <summary>
        /// IEquatable.Equals implementation checking the whole tree.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktSpheroid other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && SemiMajorAxis.Equals(other.SemiMajorAxis) && InverseFlattening.Equals(other.InverseFlattening) && Equals(Authority, other.Authority);
        }

        /// <summary>
        /// Override of basic Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktSpheroid) obj);
        }

        /// <summary>
        /// Override of basic GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SemiMajorAxis.GetHashCode();
                hashCode = (hashCode * 397) ^ InverseFlattening.GetHashCode();
                hashCode = (hashCode * 397) ^ (Authority != null ? Authority.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override void Traverse(IWktTraverseHandler handler)
        {
            if (Authority!=null)
                Authority.Traverse(handler);

            handler.Handle(this);
        }
    }
}
