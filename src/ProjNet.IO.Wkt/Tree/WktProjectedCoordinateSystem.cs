using ProjNet.IO.Wkt.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktProjectedCoordinateSystem class.
    /// </summary>
    public class WktProjectedCoordinateSystem : WktCoordinateSystem, IEquatable<WktProjectedCoordinateSystem>
    {
        /// <summary>
        /// GeographicCoordinateSystem property.
        /// </summary>
        public WktGeographicCoordinateSystem GeographicCoordinateSystem { get; internal set; }

        /// <summary>
        /// Projection property.
        /// </summary>
        public WktProjection Projection { get; internal set; }

        /// <summary>
        /// Parameters property.
        /// </summary>
        public IEnumerable<WktParameter> Parameters { get; internal set; }

        /// <summary>
        /// Unit property.
        /// </summary>
        public WktUnit Unit { get; internal set; }

        /// <summary>
        /// Axes property.
        /// </summary>
        public IEnumerable<WktAxis> Axes { get; internal set; }

        /// <summary>
        /// Authority property.
        /// </summary>
        public WktAuthority Authority { get; internal set; }

        /// <summary>
        /// Extension property.
        /// </summary>
        public WktExtension Extension { get; internal set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="geographicCoordinateSystem"></param>
        /// <param name="projection"></param>
        /// <param name="parameters"></param>
        /// <param name="unit"></param>
        /// <param name="axes"></param>
        /// <param name="extension"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktProjectedCoordinateSystem(string name, WktGeographicCoordinateSystem geographicCoordinateSystem, WktProjection projection,
                                            IEnumerable<WktParameter> parameters, WktUnit unit, IEnumerable<WktAxis> axes,
                                            WktExtension extension, WktAuthority authority,
                                            string keyword = "PROJCS", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(name, keyword, leftDelimiter, rightDelimiter)
        {
            GeographicCoordinateSystem = geographicCoordinateSystem;
            Projection = projection;
            Parameters = parameters ?? new List<WktParameter>();
            Unit = unit;
            Axes = axes;
            Extension = extension;
            Authority = authority;
        }


        /// <summary>
        /// Implentation of IEquatable.Equals.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktProjectedCoordinateSystem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(GeographicCoordinateSystem, other.GeographicCoordinateSystem) &&
                   Equals(Projection, other.Projection) &&
                   Parameters.SequenceEqual(other.Parameters) &&
                   Equals(Unit, other.Unit) &&
                   Axes.SequenceEqual(other.Axes) &&
                   Equals(Authority, other.Authority) &&
                   Equals(Extension, other.Extension);
        }

        /// <summary>
        /// Override basic Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktProjectedCoordinateSystem) obj);
        }

        /// <summary>
        /// Override basic GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (GeographicCoordinateSystem != null ? GeographicCoordinateSystem.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Projection != null ? Projection.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Parameters != null ? Parameters.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Unit != null ? Unit.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Axes != null ? Axes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Authority != null ? Authority.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Extension != null ? Extension.GetHashCode() : 0);
                return hashCode;
            }
        }


        /// <inheritdoc/>
        public override void Traverse(IWktTraverseHandler handler)
        {
            if (GeographicCoordinateSystem!=null)
                GeographicCoordinateSystem.Traverse(handler);
            if (Projection!=null)
                Projection.Traverse(handler);

            if (Parameters != null)
            {
                foreach (var p in Parameters)
                    p.Traverse(handler);
            }

            if (Unit!=null)
                Unit.Traverse(handler);

            if (Axes != null)
            {
                foreach (var axis in Axes)
                    axis.Traverse(handler);
            }

            if (Authority!=null)
                Authority.Traverse(handler);
            if (Extension!=null)
                Extension.Traverse(handler);

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
                .Append(GeographicCoordinateSystem.ToString(formatter), result)

                .AppendSeparator(result)
                .AppendExtraWhitespace(result)
                .Append(Projection.ToString(formatter), result);

            foreach (var p in Parameters)
            {
                formatter
                    .AppendSeparator(result)
                    .AppendExtraWhitespace(result)
                    .Append(p.ToString(formatter), result);
            }

            formatter
                .AppendSeparator(result)
                .AppendExtraWhitespace(result)
                .Append(Unit.ToString(formatter), result);

            foreach (var ax in Axes)
            {
                formatter
                    .AppendSeparator(result)
                    .AppendExtraWhitespace(result)
                    .Append(ax.ToString(formatter), result);
            }

            if (Extension != null)
            {
                formatter
                    .AppendSeparator(result)
                    .AppendExtraWhitespace(result)
                    .Append(Extension.ToString(formatter), result);
            }

            if (Authority != null)
            {
                formatter
                    .AppendSeparator(result)
                    .AppendIndent(result)
                    .Append(Authority.ToString(formatter), result);
            }

            formatter
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }
    }
}
