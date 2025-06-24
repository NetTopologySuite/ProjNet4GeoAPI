using ProjNet.IO.Wkt.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktGeocentricCoordinateSystem class.
    /// </summary>
    public class WktGeocentricCoordinateSystem : WktCoordinateSystem, IEquatable<WktGeocentricCoordinateSystem>
    {
        /// <summary>
        /// Datum property.
        /// </summary>
        public WktDatum Datum { get; internal set; }

        /// <summary>
        /// PrimeMeridian property.
        /// </summary>
        public WktPrimeMeridian PrimeMeridian { get; internal set; }

        /// <summary>
        /// Unit property.
        /// </summary>
        public WktUnit Unit { get; internal set; }

        /// <summary>
        /// Authority property.
        /// </summary>
        public WktAuthority Authority { get; internal set; }

        /// <summary>
        /// Axes property.
        /// </summary>
        public IEnumerable<WktAxis> Axes { get; internal set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="datum"></param>
        /// <param name="meridian"></param>
        /// <param name="unit"></param>
        /// <param name="axes"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktGeocentricCoordinateSystem(string name, WktDatum datum, WktPrimeMeridian meridian,
                                                WktUnit unit, IEnumerable<WktAxis> axes, WktAuthority authority,
                                                string keyword = "GEOCS", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(name, keyword, leftDelimiter, rightDelimiter)
        {
            Datum = datum;
            PrimeMeridian = meridian;
            Unit = unit;
            Axes = axes;
            Authority = authority;
        }

        /// <summary>
        /// Implementation of IEquatable.Equals.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktGeocentricCoordinateSystem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Datum, other.Datum) && Equals(PrimeMeridian, other.PrimeMeridian) && Equals(Unit, other.Unit) && Equals(Authority, other.Authority) && Axes.SequenceEqual(other.Axes);
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
            return Equals((WktGeocentricCoordinateSystem) obj);
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
                hashCode = (hashCode * 397) ^ (Datum != null ? Datum.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PrimeMeridian != null ? PrimeMeridian.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Unit != null ? Unit.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Authority != null ? Authority.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Axes != null ? Axes.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override void Traverse(IWktTraverseHandler handler)
        {
            Datum.Traverse(handler);
            PrimeMeridian.Traverse(handler);
            Unit.Traverse(handler);

            if (Authority!=null)
                Authority.Traverse(handler);

            foreach (var axis in Axes)
                axis.Traverse(handler);

            handler.Handle(this);
        }

        /// <inheritdoc/>
        public override string ToString(IWktOutputFormatter formatter)
        {
            formatter = formatter ?? new DefaultWktOutputFormatter();

            var result = new StringBuilder();

            formatter
                .AppendKeyword(Keyword, result)
                .AppendLeftDelimiter(LeftDelimiter, result)
                .AppendQuotedText(Name, result)

                .AppendSeparator(result)
                .AppendExtraWhitespace(result)
                .Append(Datum.ToString(formatter), result)

                .AppendSeparator(result)
                .AppendExtraWhitespace(result)
                .Append(PrimeMeridian.ToString(formatter), result)

                .AppendSeparator(result)
                .AppendExtraWhitespace(result)
                .Append(Unit.ToString(formatter), result);

            if (Axes != null && Axes.Any())
            {
                foreach (var axis in Axes)
                {
                    formatter
                        .AppendSeparator(result)
                        .AppendExtraWhitespace(result)
                        .Append(axis.ToString(formatter), result);
                }
            }

            if (Authority != null)
            {
                formatter
                    .AppendSeparator(result, keepInside: true)
                    .AppendExtraWhitespace(result)
                    .Append(Authority.ToString(formatter), result);
            }

            formatter
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }
    }
}
