using System;
using System.Globalization;
using System.Text;

namespace ProjNet.IO.Wkt.Core
{
    /// <summary>
    /// DefaultWktOutputFormatter - Keeping output compact with original delimiters.
    /// </summary>
    public class DefaultWktOutputFormatter : IWktOutputFormatter
    {
        private int indentCounter = 0;

        /// <inheritdoc/>
        public string Newline { get; } = null;
        /// <inheritdoc/>
        public char? LeftDelimiter { get; } = null;
        /// <inheritdoc/>
        public char? RightDelimiter { get; } = null;

        /// <inheritdoc/>
        public string Separator { get; } = null;

        /// <summary>
        /// Indent chars. E.g. tab or spaces. Default null.
        /// </summary>
        public string Indent { get; } = null;

        /// <inheritdoc/>
        public string ExtraWhitespace { get; } = null;


        /// <summary>
        /// Constructor with support for optional overriding the settings.
        /// </summary>
        /// <param name="newline"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        /// <param name="indent"></param>
        /// <param name="extraWhitespace"></param>
        public DefaultWktOutputFormatter(
            string newline = null,
            char? leftDelimiter = null,
            char? rightDelimiter = null,
            string indent = null,
            string extraWhitespace = null)
        {
            Newline = newline;
            LeftDelimiter = leftDelimiter;
            RightDelimiter = rightDelimiter;
            Indent = indent;
            ExtraWhitespace = extraWhitespace;
        }


        /// <inheritdoc/>
        public IWktOutputFormatter AppendKeyword(string text, StringBuilder result, bool indent = true)
        {
            if (indent)
                this.AppendIndent(result);

            result.Append(text);

            this.IncreaseIndentCounter();

            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter AppendSeparator(StringBuilder result, bool keepInside = false)
        {
            string s = Separator ?? ",";
            result.Append(s);
            if (!keepInside)
            {
                this.AppendNewline(result);
            }

            return this;
        }


        /// <inheritdoc/>
        public IWktOutputFormatter Append(string text, StringBuilder result)
        {
            result.Append(text);
            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter Append(long l, StringBuilder result)
        {
            result.Append(l);
            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter Append(double d, StringBuilder result)
        {
            result.Append(d.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter AppendNewline(StringBuilder result)
        {
            if (!string.IsNullOrEmpty(Newline))
            {
                result.Append(Newline);
            }

            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter AppendQuotedText(string text, StringBuilder result)
        {
            result.Append($"\"{text}\"");
            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter AppendLeftDelimiter(char original, StringBuilder result)
        {
            if (LeftDelimiter != null)
                result.Append(LeftDelimiter);
            else
                result.Append(original);
            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter AppendRightDelimiter(char original, StringBuilder result)
        {
            //this.AppendIndent(result);

            if (RightDelimiter != null)
                result.Append(RightDelimiter);
            else
                result.Append(original);

            this.DecreaseIndentCounter();
            //this.AppendNewline(result);

            return this;
        }

        /// <inheritdoc/>
        public IWktOutputFormatter AppendExtraWhitespace(StringBuilder result)
        {
            if (!string.IsNullOrEmpty(ExtraWhitespace))
            {
                result.Append(ExtraWhitespace);
            }

            return this;
        }

        /// <summary>
        /// Increasing the indentCounter.
        /// </summary>
        /// <returns></returns>
        public IWktOutputFormatter IncreaseIndentCounter()
        {
            indentCounter++;
            return this;
        }

        /// <summary>
        /// Decreasing the indentCounter.
        /// </summary>
        /// <returns></returns>
        public IWktOutputFormatter DecreaseIndentCounter()
        {
            indentCounter--;
            return this;
        }


        /// <summary>
        /// AppendIndent repeat Indent according to internal indentCounter.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public IWktOutputFormatter AppendIndent(StringBuilder result)
        {
            if (!string.IsNullOrEmpty(Indent))
            {
                for (int i = 1; i <= indentCounter; i++)
                {
                    result.Append(Indent);
                }
            }

            return this;
        }
    }
}
