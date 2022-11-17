using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjNet.IO
{
    /// <summary>
    /// Model for coordinate system in the database
    /// </summary>
    [Table("srs_data")]
    public class CoordinateSystemInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string Authority { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [PrimaryKey]
        public int Code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsDeprecated { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Column("type")]
        public string SystemType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string WKT { get; set; }

        /// <summary>
        /// Parameterless construction for initialization
        /// </summary>
        public CoordinateSystemInfo()
        {
        }

        /// <summary>
        /// Holds info about the coordinate system
        /// </summary>
        public CoordinateSystemInfo(string name, string alias, string authority, int code, string systemType, bool isDeprecated, string wkt)
        {
            Authority = authority;
            Code = code;
            Name = name;
            Alias = alias;
            SystemType = systemType;
            IsDeprecated = isDeprecated;
            WKT = wkt;
        }
    }
}
