using ProjNet.IO.Wkt.Core;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktFittedCoordinateSystem class.
    /// </summary>
    public class WktFittedCoordinateSystem : WktCoordinateSystem
    {

        /// <summary>
        /// ParameterMathTransform property.
        /// </summary>
        public WktParameterMathTransform ParameterMathTransform { get; internal set; }

        /// <summary>
        /// ProjectedCoordinateSystem property.
        /// </summary>
        public WktProjectedCoordinateSystem ProjectedCoordinateSystem { get; internal set; }

        /// <summary>
        /// Authority property.
        /// </summary>
        public WktAuthority Authority { get; internal set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pmt"></param>
        /// <param name="projcs"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktFittedCoordinateSystem(string name, WktParameterMathTransform pmt, WktProjectedCoordinateSystem projcs, WktAuthority authority,
            string keyword, char leftDelimiter = '[', char rightDelimiter = ']')
            : base(name, keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            ParameterMathTransform = pmt;
            ProjectedCoordinateSystem = projcs;
            Authority = authority;
        }

        /// <summary>
        /// Traverse method for WktFittedCoordinateSystem.
        /// </summary>
        /// <param name="handler"></param>
        public override void Traverse(IWktTraverseHandler handler)
        {
            ParameterMathTransform.Traverse(handler);
            ProjectedCoordinateSystem.Traverse(handler);
            if(Authority!=null)
                Authority.Traverse(handler);
            handler.Handle(this);
        }

        /// <summary>
        /// ToString implementation with optional OutputFormatter support.
        /// </summary>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public override string ToString(IWktOutputFormatter formatter)
        {
            formatter = formatter ?? new DefaultWktOutputFormatter();

            var result = new StringBuilder();

            formatter
                .AppendKeyword(Keyword, result)
                .AppendLeftDelimiter(LeftDelimiter, result)
                .AppendQuotedText(Name, result)
                .AppendSeparator(result)
                .Append(ParameterMathTransform.ToString(formatter), result)
                .AppendSeparator(result)
                .Append(ProjectedCoordinateSystem.ToString(formatter), result);

            if (Authority != null)
            {
                formatter
                    .AppendSeparator(result)
                    .Append(Authority.ToString(formatter), result);
            }

            formatter
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }

    }
}
