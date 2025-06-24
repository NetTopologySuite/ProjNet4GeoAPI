using System;
using System.Globalization;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Temporal extent is an optional attribute which describes a date or time
    /// range over which a CRS or coordinate operation is applicable.
    /// The format for date and time values is defined in ISO 9075-2. Start time is earlier than end time.
    /// </summary>
    /// <remarks>
    /// See 7.3.3.5 Temporal extent in specification document.
    /// </remarks>
    public class WktTemporalExtent : WktExtent
    {
        /// <summary>
        /// StartDateTime
        /// </summary>
        public DateTimeOffset? StartDateTime { get; set; }

        /// <summary>
        /// StartText
        /// </summary>
        public string StartText { get; set; }


        /// <summary>
        /// EndDateTime
        /// </summary>
        public DateTimeOffset? EndDateTime { get; set; }

        /// <summary>
        /// EndText
        /// </summary>
        public string EndText { get; set; }



        /// <summary>
        /// TemporalExtent to WKT.
        /// </summary>
        /// <returns></returns>
        public override string ToWKT()
        {
            var sb = new StringBuilder();

            sb.Append("TIMEEXTENT[");

            if (StartDateTime.HasValue)
                sb.Append(StartDateTime.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            else
            {
                sb.Append(@"""");
                sb.Append(StartText);
                sb.Append(@"""");
            }

            sb.Append(",");

            if (EndDateTime.HasValue)
                sb.Append(EndDateTime.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            else
            {
                sb.Append(@"""");
                sb.Append(EndText);
                sb.Append(@"""");
            }
            sb.Append("]");

            return sb.ToString();
        }
    }
}
