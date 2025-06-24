using ProjNet.IO.Wkt.Core;
using System;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktAxis - Simple POCO for Axis info.
    /// </summary>
    public class WktAxis : WktBaseObject, IEquatable<WktAxis>
    {
        /// <summary>
        /// Constructor using string for direction.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktAxis(string name, string direction,
                        string keyword = "AXIS", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            SetDirection(direction);
        }


        /// <summary>
        /// Name of this Axis.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Direction for this Axis.
        /// </summary>
        public WktAxisDirectionEnum Direction { get; set; }





        /// <summary>
        /// Direction Setter method.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public WktAxis SetDirection(string direction)
        {
            if (!string.IsNullOrWhiteSpace(direction))
            {
                Direction = (WktAxisDirectionEnum)Enum.Parse(typeof(WktAxisDirectionEnum), direction, true);
            }

            return this;
        }

        /// <summary>
        /// IEquatable.Equals implementation for checking the whole tree except keywords and delimiters.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktAxis other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Direction == other.Direction;
        }

        /// <summary>
        /// Basic override of Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktAxis) obj);
        }

        /// <summary>
        /// Basic override of GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (int) Direction;
            }
        }


        /// <inheritdoc/>>
        public override void Traverse(IWktTraverseHandler handler)
        {
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
                .Append(Direction.ToString(), result)
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }
    }
}
