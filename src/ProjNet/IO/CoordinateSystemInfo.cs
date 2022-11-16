using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjNet.IO
{
    [Table("srs_data")]
    public class CoordinateSystemInfo
    {
        public string Authority { get; set; }
        [PrimaryKey]
        public int Code { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public bool IsDeprecated { get; set; }
        [Column("type")]
        public string SystemType { get; set; }
        public string WKT { get; set; }

        public CoordinateSystemInfo() 
        { }

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
