using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjNet.Sqlite.Extensions
{
    /// <summary>
    /// Extension class for search functions
    /// </summary>
    public static class SearchExtension
    {
        /// <summary>
        /// Extension to find string in another string
        /// </summary>
        /// <param name="toSearch"></param>
        /// <param name="toFind"></param>
        /// <returns></returns>
        public static bool Like(this string toSearch, string toFind)
        {
            return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(toSearch);
        }
    }
}
