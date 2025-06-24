using ProjNet.IO.Wkt.Core;
using System;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktPrimeMeridian property.
    /// </summary>
    public class WktPrimeMeridian : WktBaseObject, IEquatable<WktPrimeMeridian>
    {

        /// <summary>
        /// Name property.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Longitude property.
        /// </summary>
        public double Longitude { get; internal set; }

        /// <summary>
        /// Authority property.
        /// </summary>
        public WktAuthority Authority { get; internal set; }


        /// <summary>
        /// Constructor for the WktPrimeMeridian class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="longitude"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktPrimeMeridian(string name, double longitude, WktAuthority authority,
                                string keyword = "PRIMEM", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            Longitude = longitude;
            Authority = authority;
        }

        /// <summary>
        /// Implements IEquatable.Equals checking the whole tree.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktPrimeMeridian other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Longitude.Equals(other.Longitude) && Equals(Authority, other.Authority);
        }

        /// <summary>
        /// Overriding base Equals method.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktPrimeMeridian) obj);
        }

        /// <summary>
        /// Overriding base GetHashCode method.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Longitude.GetHashCode();
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

        /// <inheritdoc/>
        public override string ToString(IWktOutputFormatter formatter)
        {
            formatter = formatter ?? new DefaultWktOutputFormatter();

            var result = new StringBuilder();

            formatter
                .AppendKeyword(Keyword, result)
                .AppendLeftDelimiter(LeftDelimiter, result)
                .AppendQuotedText(Name, result)
                .AppendSeparator(result, keepInside: true)
                .AppendExtraWhitespace(result)
                .Append(Longitude, result);


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
