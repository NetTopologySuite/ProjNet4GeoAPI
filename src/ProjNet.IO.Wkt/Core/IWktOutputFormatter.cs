using System;
using System.Text;

namespace ProjNet.IO.Wkt.Core
{

    /// <summary>
    /// IWktOutputFormatter interface for customizing ToString(...) support.
    /// </summary>
    public interface IWktOutputFormatter
    {
        /// <summary>
        /// Newline char or empty string. Default empty.
        /// </summary>
        string Newline { get; }

        /// <summary>
        /// LeftDelimiter if empty use original. Default empty.
        /// </summary>
        char? LeftDelimiter { get; }
        /// <summary>
        /// RightDelimiter if empty use original. Default empty.
        /// </summary>
        char? RightDelimiter { get; }

        /// <summary>
        /// Separator to use. Default comma ,
        /// </summary>
        string Separator { get; }

        /// <summary>
        /// ExtraWhitespace. Default empty.
        /// </summary>
        string ExtraWhitespace { get; }


        /// <summary>
        /// Changeable Append method for Keywords. Optional extra indent is written.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="result"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendKeyword(string text, StringBuilder result, bool indent = true);


        /// <summary>
        /// Changeable Append method for Separator. Optional extra newline afterward.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="keepInside"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendSeparator(StringBuilder result, bool keepInside = false);

        /// <summary>
        /// Changeable Append method for string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter Append(string text, StringBuilder result);

        /// <summary>
        /// Changeable Append method for long.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter Append(long l, StringBuilder result);

        /// <summary>
        /// Changeable Append method for double.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter Append(double d, StringBuilder result);


        /// <summary>
        /// Changeable AppendNewline method applying Newline.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendNewline(StringBuilder result);

        /// <summary>
        /// Changeable AppendQuotedText method applying text surrounded by double quotes.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendQuotedText(string text, StringBuilder result);

        /// <summary>
        /// Changeable AppendLeftDelimiter applying LeftDelimiter or original.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendLeftDelimiter(char original, StringBuilder result);

        /// <summary>
        /// Changeable AppendRightDelimiter applying RightDelimiter or original.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendRightDelimiter(char original, StringBuilder result);


        /// <summary>
        /// Changeable AppendExtraWhitespace optionally applying extr whitespace.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendExtraWhitespace(StringBuilder result);

        /// <summary>
        /// IncreaseIndentCounter
        /// </summary>
        /// <returns></returns>
        IWktOutputFormatter IncreaseIndentCounter();

        /// <summary>
        /// DecreaseIndentCounter
        /// </summary>
        /// <returns></returns>
        IWktOutputFormatter DecreaseIndentCounter();


        /// <summary>
        /// AppendIndent
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        IWktOutputFormatter AppendIndent(StringBuilder result);
    }
}
