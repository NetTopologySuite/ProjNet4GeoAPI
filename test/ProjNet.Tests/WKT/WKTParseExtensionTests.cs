using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjNET.Tests.WKT
{
    internal class WKTParseTests_11_17_2022
    {
        private readonly CoordinateSystemFactory _coordinateSystemFactory = new CoordinateSystemFactory();

        private string extensionWkt1 = "PROJCS[\"NAD27/BLM59N(ftUS)\",GEOGCS[\"NAD27\",DATUM[\"North_American_Datum_1927\",SPHEROID[\"Clarke1866\",6378206.4,294.978698213898],EXTENSION[\"PROJ4_GRIDS\",\"NTv2_0.gsb\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4267\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",171],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",1640416.67],PARAMETER[\"false_northing\",0],UNIT[\"USsurveyfoot\",0.304800609601219],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"4399\"]]";
        private string extensionWkt2 = "PROJCS[\"NAD27/BLM60N(ftUS)\",GEOGCS[\"NAD27\",DATUM[\"North_American_Datum_1927\",SPHEROID[\"Clarke1866\",6378206.4,294.978698213898],EXTENSION[\"PROJ4_GRIDS\",\"NTv2_0.gsb\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4267\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",177],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",1640416.67],PARAMETER[\"false_northing\",0],UNIT[\"USsurveyfoot\",0.304800609601219],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"4400\"]]";
        private string extensionWkt3 = "PROJCS[\"WGS84/Pseudo-Mercator\",GEOGCS[\"WGS84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],EXTENSION[\"PROJ4\",\"+proj=merc+a=6378137+b=6378137+lat_ts=0+lon_0=0+x_0=0+y_0=0+k=1+units=m+nadgrids=@null+wktext+no_defs\"],AUTHORITY[\"EPSG\",\"3857\"]]";
        private string extensionWkt4 = "COMPD_CS[\"WGS84/Pseudo-Mercator+EGM2008geoidheight\",PROJCS[\"WGS84/Pseudo-Mercator\",GEOGCS[\"WGS84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],EXTENSION[\"PROJ4\",\"+proj=merc+a=6378137+b=6378137+lat_ts=0+lon_0=0+x_0=0+y_0=0+k=1+units=m+nadgrids=@null+wktext+no_defs\"],AUTHORITY[\"EPSG\",\"3857\"]],VERT_CS[\"EGM2008height\",VERT_DATUM[\"EGM2008geoid\",2005,AUTHORITY[\"EPSG\",\"1027\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Gravity-relatedheight\",UP],AUTHORITY[\"EPSG\",\"3855\"]],AUTHORITY[\"EPSG\",\"6871\"]]";

        [Test]
        public void TestExtensions()
        {
            CoordinateSystem cs = null;
            Assert.That(() => cs = _coordinateSystemFactory.CreateFromWkt(extensionWkt1) as CoordinateSystem, Throws.Nothing);

            cs = null;
            Assert.That(() => cs = _coordinateSystemFactory.CreateFromWkt(extensionWkt2) as CoordinateSystem, Throws.Nothing);

            cs = null;
            Assert.That(() => cs = _coordinateSystemFactory.CreateFromWkt(extensionWkt3) as CoordinateSystem, Throws.Nothing);

            cs = null;
            Assert.That(() => cs = _coordinateSystemFactory.CreateFromWkt(extensionWkt4) as CoordinateSystem, Throws.Nothing);
        }
    }
}
