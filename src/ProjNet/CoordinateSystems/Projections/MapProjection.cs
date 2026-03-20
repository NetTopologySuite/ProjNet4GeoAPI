// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

// SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
namespace ProjNet.CoordinateSystems.Projections
{
    // SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
    /*
     *  Copyright (C) 2002 Urban Science Applications, Inc.
     *
     *  This library is free software; you can redistribute it and/or
     *  modify it under the terms of the GNU Lesser General Public
     *  License as published by the Free Software Foundation; either
     *  version 2.1 of the License, or (at your option) any later version.
     *
     *  This library is distributed in the hope that it will be useful,
     *  but WITHOUT ANY WARRANTY; without even the implied warranty of
     *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
     *  Lesser General Public License for more details.
     *
     *  You should have received a copy of the GNU Lesser General Public
     *  License along with this library; if not, write to the Free Software
     *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
     *
     */

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using ProjNet.CoordinateSystems.Transformations;

    /// <summary>
    /// Projections inherit from this abstract class to get access to useful mathematical functions.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Legacy PROJ-compatible API surface is preserved for compatibility.")]
    [Serializable]
    public abstract class MapProjection : MathTransform, IProjection
    {
        /// <summary>
        /// EPS10 => 1e-10.
        /// </summary>
        protected const double EPS10 = 1e-10;

        /// <summary>
        /// EPS7 => 1e-7.
        /// </summary>
        protected const double EPS7 = 1e-7;

        /// <summary>
        /// HUGE_VAL => double.NaN.
        /// </summary>
        protected const double HUGEVAL = double.NaN;

        /// <summary>
        /// PI.
        /// </summary>
        protected const double PI = Math.PI;

        /// <summary>
        /// A fourth of <see cref="Math.PI"/>.
        /// </summary>
        protected const double FORTPI = PI * 0.25;

        /// <summary>
        /// Half of PI.
        /// </summary>
        protected const double HALFPI = PI * 0.5;

        /// <summary>
        /// PI * 2.
        /// </summary>
        protected const double TWOPI = PI * 2.0;

        /// <summary>
        /// EPSLN.
        /// </summary>
        protected const double EPSLN = EPS10;

        /// <summary>
        /// S2R.
        /// </summary>
        protected const double S2R = 4.848136811095359e-6;

        /// <summary>
        /// MAX_VAL.
        /// </summary>
        protected const double MAXVAL = 4;

        /// <summary>
        /// prjMAXLONG.
        /// </summary>
        protected const double prjMAXLONG = 2147483647;

        /// <summary>
        /// DBLLONG.
        /// </summary>
        protected const double DBLLONG = 4.61168601e18;

        // ReSharper disable InconsistentNaming

        /// <summary>
        /// Eccentricity.
        /// </summary>
        protected readonly double e;

        /// <summary>
        /// Eccentricity squared <c>_e * _e</c>.
        /// </summary>
        protected readonly double es;

        /// <summary>
        /// Length of semi major axis of ellipse.
        /// </summary>
        protected readonly double semiMajor;

        /// <summary>
        /// Length of semi minor axis  of ellipse.
        /// </summary>
        protected readonly double semiMinor;

        /// <summary>
        /// Meters per unit.
        /// </summary>
        protected readonly double metersPerUnit;

        /// <summary>
        /// Reciprocal meters per unit <c>1.0 / <see cref="metersPerUnit"/></c>.
        /// </summary>
        protected readonly double reciprocalMetersPerUnit;

        /// <summary>
        /// Scale factor.
        /// </summary>
        protected readonly double scaleFactor; /* scale factor             */

        /// <summary>
        /// Center longitude (projection center).
        /// </summary>
        protected double centralMeridian; /* Center longitude (projection center) */

        /// <summary>
        /// Center latitude.
        /// </summary>
        protected readonly double latOrigin; /* center latitude            */

        /// <summary>
        /// Y offset in meters.
        /// </summary>
        protected readonly double falseNorthing; /* y offset in meters         */

        /// <summary>
        /// X offset in meters.
        /// </summary>
        protected readonly double falseEasting; /* x offset in meters          */

        /// <summary>
        /// Constants for <see cref="Mlfn(double,double,double,double,double)"/>.
        /// </summary>
        protected readonly double en0, en1, en2, en3, en4;

        /// <summary>
        /// A set of projection parameters for this projection.
        /// </summary>
        protected readonly ProjectionParameterSet Parameters;

        /// <summary>
        /// The inverse <see cref="MathTransform"/>.
        /// </summary>
        protected MathTransform inverse;

        private const double C00 = 1.0,
                             C02 = 0.25,
                             C04 = 0.046875,
                             C06 = 0.01953125,
                             C08 = 0.01068115234375,
                             C22 = 0.75,
                             C44 = 0.46875,
                             C46 = 0.01302083333333333333,
                             C48 = 0.00712076822916666666,
                             C66 = 0.36458333333333333333,
                             C68 = 0.00569661458333333333,
                             C88 = 0.3076171875;

        private const double P00 = 0.33333333333333333333; /*   1 /     3 */
        private const double P01 = 0.17222222222222222222; /*  31 /   180 */
        private const double P02 = 0.10257936507936507937; /* 517 /  5040 */
        private const double P10 = 0.06388888888888888888; /*  23 /   360 */
        private const double P11 = 0.06640211640211640212; /* 251 /  3780 */
        private const double P20 = 0.01677689594356261023; /* 761 / 45360 */

        /// <summary>
        /// Initializes a new instance of the <see cref="MapProjection"/> class.
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="parameters">An enumeration of projection parameters.</param>
        /// <param name="inverse">Indicator if this projection is inverse.</param>
        protected MapProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : this(parameters)
        {
            this.inverse = inverse;
            if (this.inverse != null)
            {
                inverse.inverse = this;
                this.IsInverse = !inverse.IsInverse;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapProjection"/> class.
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="parameters">An enumeration of projection parameters.</param>
        protected MapProjection(IEnumerable<ProjectionParameter> parameters)
        {
            this.Parameters = new ProjectionParameterSet(parameters);

            this.semiMajor = this.Parameters.GetParameterValue("semi_major");
            this.semiMinor = this.Parameters.GetParameterValue("semi_minor");

            // _es = 1.0 - (_semiMinor * _semiMinor) / (_semiMajor * _semiMajor);
            this.es = EccentricySquared(this.semiMajor, this.semiMinor);
            this.e = Math.Sqrt(this.es);

            this.scaleFactor = this.Parameters.GetOptionalParameterValue("scale_factor", 1);

            this.centralMeridian = DegreesToRadians(this.Parameters.GetParameterValue("central_meridian", "longitude_of_center"));
            this.latOrigin = DegreesToRadians(this.Parameters.GetOptionalParameterValue("latitude_of_origin", 0d, "latitude_of_center"));

            this.metersPerUnit = this.Parameters.GetParameterValue("unit");
            this.reciprocalMetersPerUnit = 1 / this.metersPerUnit;

            this.falseEasting = this.Parameters.GetOptionalParameterValue("false_easting", 0) * this.metersPerUnit;
            this.falseNorthing = this.Parameters.GetOptionalParameterValue("false_northing", 0) * this.metersPerUnit;

            // TODO: Should really convert to the correct linear units??

            // Compute constants for the mlfn
            double t;
            this.en0 = C00 - (this.es * (C02 + (this.es *
                             (C04 + (this.es * (C06 + (this.es * C08)))))));
            this.en1 = this.es * (C22 - (this.es *
                       (C04 + (this.es * (C06 + (this.es * C08))))));
            this.en2 = (t = this.es * this.es) *
                  (C44 - (this.es * (C46 + (this.es * C48))));
            this.en3 = (t *= this.es) * (C66 - (this.es * C68));
            this.en4 = t * this.es * C88;

        }

        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Gets the projection classification name (e.g. 'Transverse_Mercator').
        /// </summary>
        public string ClassName
        {
            get { return this.Name; }
        }

        /// <summary>
        ///
        /// </summary>
        /// <inheritdoc/>
        public int NumParameters
        {
            get { return this.Parameters.Count; }
        }

        /// <summary>
        /// Gets or sets the abbreviation of the object.
        /// </summary>
        public string Abbreviation { get; set; }

        /// <summary>
        /// Gets or sets the alias of the object.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the authority name for this object, e.g., "EPSG",
        /// is this is a standard object with an authority specific
        /// identity code. Returns "CUSTOM" if this is a custom object.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the authority specific identification code of the object.
        /// </summary>
        public long AuthorityCode { get; set; }

        /// <summary>
        /// Gets or sets the name of the object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the provider-supplied remarks for the object.
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Function to calculate UTM zone number.
        /// </summary>
        /// <param name="lon">The longitudinal value (in Degrees!).</param>
        /// <returns>The UTM zone number.</returns>
        public static long CalcUtmZone(double lon)
        {
            return (long)(((lon + 180.0) / 6.0) + 1.0);
        }

        /// <summary>
        /// Gets the Well-known text for this object
        /// as defined in the simple features specification.
        /// </summary>
        public override string WKT
        {
            get
            {
                var sb = new StringBuilder();
                if (this.IsInverse)
                {
                    sb.Append("INVERSE_MT[");
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, "PARAM_MT[\"{0}\"", this.Name);
                for (int i = 0; i < this.NumParameters; i++)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", {0}", this.GetParameter(i).WKT);
                }

                // if (!string.IsNullOrWhiteSpace(Authority) && AuthorityCode > 0)
                // sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                if (this.IsInverse)
                {
                    sb.Append("]");
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("<CT_MathTransform>");
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    this.IsInverse
                        ? "<CT_InverseTransform Name=\"{0}\">"
                        : "<CT_ParameterizedMathTransform Name=\"{0}\">",
                    this.ClassName);
                for (int i = 0; i < this.NumParameters; i++)
                {
                    sb.Append(this.GetParameter(i).XML);
                }

                sb.Append(this.IsInverse ? "</CT_InverseTransform>" : "</CT_ParameterizedMathTransform>");
                sb.Append("</CT_MathTransform>");
                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public sealed override int DimSource
        {
            get { return 2; }
        }

        /// <inheritdoc/>
        public sealed override int DimTarget
        {
            get { return 2; }
        }

        /// <inheritdoc />
        public sealed override void Transform(ref double x, ref double y, ref double z)
        {
            if (this.IsInverse)
            {
                this.SourceToDegrees(ref x, ref y);
            }
            else
            {
                this.DegreesToTarget(ref x, ref y);
            }
        }

        /// <summary>
        /// Reverses the transformation.
        /// </summary>
        public override void Invert()
        {
            this.IsInverse = !this.IsInverse;
            if (this.inverse != null)
            {
                ((MapProjection)this.inverse).Invert(false);
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj">The obj parameter.</param>
        /// <returns>True if equal.</returns>
        public bool EqualParams(object obj)
        {
            if (!(obj is MapProjection))
            {
                return false;
            }

            var proj = obj as MapProjection;

            if (!this.Parameters.Equals(proj.Parameters))
            {
                return false;
            }

            /*
if (proj.NumParameters != NumParameters)
   return false;

for (var i = 0; i < _Parameters.Count; i++)
{
   var param = _Parameters.Find(par => par.Name.Equals(proj.GetParameter(i).Name, StringComparison.OrdinalIgnoreCase));
   if (param == null)
       return false;
   if (param.Value != proj.GetParameter(i).Value)
       return false;
}
*/
            return this.IsInverse == proj.IsInverse;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index">The index parameter.</param>
        /// <returns>The transformation result.</returns>
        /// <inheritdoc/>
        public ProjectionParameter GetParameter(int index)
        {
            return this.Parameters.GetAtIndex(index);
        }

        /// <summary>
        /// Gets an named parameter of the projection.
        /// </summary>
        /// <remarks>The parameter name is case insensitive.</remarks>
        /// <param name="name">Name of parameter.</param>
        /// <returns>parameter or null if not found.</returns>
        public ProjectionParameter GetParameter(string name)
        {
            return this.Parameters.Find(name);
        }

        /// <summary>
        /// Gets a value indicating whether returns true if this projection is inverted.
        /// Most map projections define forward projection as "from geographic to projection", and backwards
        /// as "from projection to geographic". If this projection is inverted, this will be the other way around.
        /// </summary>
        protected internal bool IsInverse { get; private set; }

        /// <inheritdoc />
        protected sealed override void TransformCore(Span<double> xs, Span<double> ys, Span<double> zs, int strideX, int strideY, int strideZ)
        {
            if (this.IsInverse)
            {
                this.SourceToDegrees(xs, ys, strideX, strideY);
            }
            else
            {
                this.DegreesToTarget(xs, ys, strideX, strideY);
            }
        }

        /// <summary>
        /// Abstract method to convert a point (lon, lat) in radians to (x, y) in meters.
        /// </summary>
        /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        protected abstract void RadiansToMeters(ref double lon, ref double lat);

        /// <summary>
        /// Method to convert a series of points defined by (lon, lat) in radians to (x, y) in meters.
        /// </summary>
        /// <param name="lons">The longitudes of the points in radians when entering, their x-ordinates in meters after exit.</param>
        /// <param name="lats">The latitudes of the points in radians when entering, their y-ordinates in meters after exit.</param>
        /// <param name="strideX">A stride value for longitude-ordinates.</param>
        /// <param name="strideY">A stride value for latitude-ordinates.</param>
        protected virtual void RadiansToMeters(Span<double> lons, Span<double> lats, int strideX, int strideY)
        {
            for (int i = 0, j = 0; i < lons.Length; i += strideX, j += strideY)
            {
                this.RadiansToMeters(ref lons[i], ref lats[j]);
            }
        }

        /// <summary>
        /// Converts a point (lon, lat) in degrees to (x, y) in meters.
        /// </summary>
        /// <param name="lon">The longitude in degree.</param>
        /// <param name="lat">The latitude in degree.</param>
        protected virtual void DegreesToMeters(ref double lon, ref double lat)
        {
            lon = DegreesToRadians(lon);
            lat = DegreesToRadians(lat);
            this.RadiansToMeters(ref lon, ref lat);
        }

        /// <summary>
        /// Converts points (lon, lat) in degrees to (x, y) in meters.
        /// </summary>
        /// <param name="lons">The longitudes of the points in degree when entering, their x-ordinates in meters after exit.</param>
        /// <param name="lats">The latitudes of the points in degree when entering, their y-ordinates in meters after exit.</param>
        /// <param name="strideX">A stride value for longitude-ordinates.</param>
        /// <param name="strideY">A stride value for latitude-ordinates.</param>
        protected virtual void DegreesToMeters(Span<double> lons, Span<double> lats, int strideX, int strideY)
        {
            DegreesToRadians(lons, strideX);
            DegreesToRadians(lats, strideY);
            this.RadiansToMeters(lons, lats, strideX, strideY);
        }

        /// <summary>
        /// Converts a point from degrees to target units.
        /// </summary>
        /// <param name="lon">The longitude in degree.</param>
        /// <param name="lat">The latitude in degree.</param>
        protected virtual void DegreesToTarget(ref double lon, ref double lat)
        {
            this.DegreesToMeters(ref lon, ref lat);
            this.MetersToTarget(ref lon, ref lat);
        }

        /// <summary>
        /// Converts a series of points from degrees to target units to degrees.
        /// </summary>
        /// <param name="lons">A series of x-ordinate values.</param>
        /// <param name="lats">A series of y-ordinate values.</param>
        /// <param name="strideX">A stride value for x-ordinates.</param>
        /// <param name="strideY">A stride value for y-ordinates.</param>
        protected virtual void DegreesToTarget(
            Span<double> lons,
            Span<double> lats,
            int strideX,
            int strideY)
        {
            this.DegreesToMeters(lons, lats, strideX, strideY);
            this.MetersToTarget(lons, lats, strideX, strideY);
        }

        /// <summary>
        /// Transforms point from meters to unit of output coordinate. This is done by
        /// adding <see cref="falseEasting"/> or <see cref="falseNorthing"/> and
        /// multiplying with <see cref="reciprocalMetersPerUnit"/>.
        /// </summary>
        /// <param name="x">A x-ordinate.</param>
        /// <param name="y">A y-ordinate.</param>
        /// <returns>A point.</returns>
        protected void MetersToTarget(ref double x, ref double y)
        {
            x = (x + this.falseEasting) * this.reciprocalMetersPerUnit;
            y = (y + this.falseNorthing) * this.reciprocalMetersPerUnit;
        }

        /// <summary>
        /// Transforms point from meters to unit of output coordinate. This is done by
        /// adding <see cref="falseEasting"/> or <see cref="falseNorthing"/> and
        /// multiplying with <see cref="reciprocalMetersPerUnit"/>.
        /// </summary>
        /// <param name="xs">A x-ordinates.</param>
        /// <param name="ys">A y-ordinates.</param>
        /// <param name="strideX">A stride value for x-ordinates.</param>
        /// <param name="strideY">A stride value for y-ordinates.</param>
        /// <returns>A point.</returns>
        protected void MetersToTarget(Span<double> xs, Span<double> ys, int strideX, int strideY)
        {
            AddThenMultiplyInPlace(xs, strideX, this.falseEasting, this.reciprocalMetersPerUnit);
            AddThenMultiplyInPlace(ys, strideY, this.falseNorthing, this.reciprocalMetersPerUnit);
        }

        /// <summary>
        /// Abstract method to convert a point from meters to radians.
        /// </summary>
        /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
        /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
        protected abstract void MetersToRadians(ref double x, ref double y);

        /// <summary>
        /// Method to convert a series of points defined by (x, y) in meters to (lon, lat) in radians.
        /// </summary>
        /// <param name="xs">The x-ordinates of the points in meters when entering, their longitudes in radians after exit.</param>
        /// <param name="ys">The y-ordinates of the points in meters when entering, their latitudes in radians after exit.</param>
        /// <param name="strideX">A stride value for x-ordinates.</param>
        /// <param name="strideY">A stride value for y-ordinates.</param>
        protected virtual void MetersToRadians(Span<double> xs, Span<double> ys, int strideX, int strideY)
        {
            for (int i = 0, j = 0; i < xs.Length; i += strideX, j += strideY)
            {
                this.MetersToRadians(ref xs[i], ref ys[j]);
            }
        }

        /// <summary>
        /// Method to convert a point from meters to degrees.
        /// </summary>
        /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
        /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
        protected virtual void MetersToDegrees(ref double x, ref double y)
        {
            this.MetersToRadians(ref x, ref y);
            x = RadiansToDegrees(x);
            y = RadiansToDegrees(y);
        }

        /// <summary>
        /// Method to convert a point from meters to degrees.
        /// </summary>
        /// <param name="xs">The x-ordinate values when entering, the longitude values upon exit.</param>
        /// <param name="ys">The y-ordinate values when entering, the latitude values upon exit.</param>
        /// <param name="strideX">The strideX parameter.</param>
        /// <param name="strideY">The strideY parameter.</param>
        protected virtual void MetersToDegrees(Span<double> xs, Span<double> ys, int strideX, int strideY)
        {
            this.MetersToRadians(xs, ys, strideX, strideY);
            RadiansToDegrees(xs, strideX);
            RadiansToDegrees(ys, strideY);
        }

        /// <summary>
        /// Converts a point from source units to degrees.
        /// </summary>
        /// <param name="x">The x-ordinate.</param>
        /// <param name="y">The y-ordinate.</param>
        /// <returns>Converted point.</returns>
        protected virtual void SourceToDegrees(ref double x, ref double y)
        {
            this.SourceToMeters(ref x, ref y);
            this.MetersToDegrees(ref x, ref y);
        }

        /// <summary>
        /// Converts a series of points from source units to degrees.
        /// </summary>
        /// <param name="xs">A series of x-ordinate values.</param>
        /// <param name="ys">A series of y-ordinate values.</param>
        /// <param name="strideX">A stride value for x-ordinates.</param>
        /// <param name="strideY">A stride value for y-ordinates.</param>
        protected virtual void SourceToDegrees(
            Span<double> xs,
            Span<double> ys,
            int strideX,
            int strideY)
        {
            this.SourceToMeters(xs, ys, strideX, strideY);
            this.MetersToDegrees(xs, ys, strideX, strideY);
        }

        /// <summary>
        /// Transforms unit of input coordinates to meters. This is done by multiplying with
        /// <see cref="metersPerUnit"/> and subtracting <see cref="falseEasting"/>
        /// or <see cref="falseNorthing"/>.
        /// </summary>
        /// <param name="xs">A series of x-ordinates.</param>
        /// <param name="ys">A series of y-ordinates.</param>
        /// <param name="strideX">A stride value for x-ordinates.</param>
        /// <param name="strideY">A stride value for y-ordinates.</param>
        protected void SourceToMeters(Span<double> xs, Span<double> ys, int strideX, int strideY)
        {
            MultiplyThenAddInPlace(xs, strideX, this.metersPerUnit, -this.falseEasting);
            MultiplyThenAddInPlace(ys, strideY, this.metersPerUnit, -this.falseNorthing);
        }

        /// <summary>
        /// Transforms unit of input coordinate to meters. This is done by multiplying with
        /// <see cref="metersPerUnit"/> and subtracting <see cref="falseEasting"/>
        /// or <see cref="falseNorthing"/>.
        /// </summary>
        /// <param name="x">A x-ordinate.</param>
        /// <param name="y">A y-ordinate.</param>
        /// <returns>A point.</returns>
        protected void SourceToMeters(ref double x, ref double y)
        {
            x = (x * this.metersPerUnit) - this.falseEasting;
            y = (y * this.metersPerUnit) - this.falseNorthing;
        }

        /// <summary>
        /// Reverses this transformation.
        /// </summary>
        /// <param name="invertInverse">A flag indicating to reverse the <see cref="inverse"/>"/> projection as well.</param>
        protected void Invert(bool invertInverse)
        {
            this.IsInverse = !this.IsInverse;
            if (invertInverse && this.inverse != null)
            {
                ((MapProjection)this.inverse).Invert(false);
            }
        }

        /// <summary>
        /// Gets or sets substitute for <see cref="centralMeridian"/>.
        /// </summary>
        protected double Lon_origin
        {
            get { return this.centralMeridian; }
            set { this.centralMeridian = value; }
        }

        /// <summary>
        /// Gets center latitude (projection center), same as lat_origin.
        /// </summary>
        protected double Central_parallel
        {
            get { return this.latOrigin; }
        }

        /// <summary>
        /// Gets center latitude (projection center), same as lat_origin.
        /// </summary>
        protected double Phi0
        {
            get { return this.latOrigin; }
        }

        /// <summary>
        /// Returns a list of projection "cloned" projection parameters.
        /// </summary>
        /// <param name="projectionParameters">The projectionParameters value.</param>
        /// <returns>The transformation result.</returns>
        protected internal static List<ProjectionParameter> CloneParametersList(
            IEnumerable<ProjectionParameter> projectionParameters)
        {
            var res = new List<ProjectionParameter>();
            foreach (var pp in projectionParameters)
            {
                res.Add(new ProjectionParameter(pp.Name, pp.Value));
            }

            return res;
        }

        /// <summary>
        /// Returns the cube of a number.
        /// </summary>
        /// <param name="x">The x parameter.</param>
        /// <returns>The computed value.</returns>
        protected static double CUBE(double x)
        {
            return Math.Pow(x, 3); /* x^3 */
        }

        /// <summary>
        /// Returns the quad of a number.
        /// </summary>
        /// <param name="x">The x parameter.</param>
        /// <returns>The computed value.</returns>
        protected static double QUAD(double x)
        {
            return Math.Pow(x, 4); /* x^4 */
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="A">The A parameter.</param>
        /// <param name="B">The B parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double GMAX(ref double A, ref double B)
        {
            return Math.Max(A, B); /* assign maximum of a and b */
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="A">The A parameter.</param>
        /// <param name="B">The B parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double GMIN(ref double A, ref double B)
        {
            return A < B ? A : B; /* assign minimum of a and b */
        }

        /// <summary>
        /// IMOD.
        /// </summary>
        /// <param name="A">The A parameter.</param>
        /// <param name="B">The B parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double IMOD(double A, double B)
        {
            return A - ((A / B) * B); /* Integer mod function */

        }

        /// <summary>
        /// Function to return the sign of an argument.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <returns>The computed value.</returns>
        protected static double Sign(double x)
        {
            if (x < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x">The x parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double Adjust_lon(double x)
        {
            long count = 0;
            for (; ; )
            {
                if (Math.Abs(x) <= PI)
                {
                    break;
                }
                else if (((long)Math.Abs(x / Math.PI)) < 2)
                {
                    x = x - (Sign(x) * TWOPI);
                }
                else if (((long)Math.Abs(x / TWOPI)) < prjMAXLONG)
                {
                    x = x - (((long)(x / TWOPI)) * TWOPI);
                }
                else if (((long)Math.Abs(x / (prjMAXLONG * TWOPI))) < prjMAXLONG)
                {
                    x = x - (((long)(x / (prjMAXLONG * TWOPI))) * (TWOPI * prjMAXLONG));
                }
                else if (((long)Math.Abs(x / (DBLLONG * TWOPI))) < prjMAXLONG)
                {
                    x = x - (((long)(x / (DBLLONG * TWOPI))) * (TWOPI * DBLLONG));
                }
                else
                {
                    x = x - (Sign(x) * TWOPI);
                }

                count++;
                if (count > MAXVAL)
                {
                    break;
                }
            }

            return x;
        }

        /// <summary>
        /// Function to compute the constant small m which is the radius of
        /// a parallel of latitude, phi, divided by the semimajor axis.
        /// </summary>
        /// <param name="cosphi">The cosphi value.</param>
        /// <param name="eccent">The eccent value.</param>
        /// <param name="sinphi">The sinphi value.</param>
        /// <returns>The computed value.</returns>
        protected static double Msfnz(double eccent, double sinphi, double cosphi)
        {
            double con;

            con = eccent * sinphi;
            return (cosphi / Math.Sqrt(1.0 - (con * con)));
        }

        /// <summary>
        /// Function to compute constant small q which is the radius of a
        /// parallel of latitude, phi, divided by the semimajor axis.
        /// </summary>
        /// <param name="eccent">The eccent value.</param>
        /// <param name="sinphi">The sinphi value.</param>
        /// <returns>The computed value.</returns>
        protected static double Qsfnz(double sinphi, double eccent)
        {
            if (eccent > 1.0e-7)
            {
                double con = eccent * sinphi;
                return (1.0 - (eccent * eccent)) * ((sinphi / (1.0 - (con * con))) - ((.5 / eccent) *
                                               Math.Log((1.0 - con) / (1.0 + con))));
            }

            return 2.0 * sinphi;
        }

        /// <summary>
        /// Function to compute constant small q which is the radius of a
        /// parallel of latitude, phi, divided by the semimajor axis.
        /// </summary>
        /// <param name="eccent">The eccent value.</param>
        /// <param name="one_es">The one_es value.</param>
        /// <param name="sinphi">The sinphi value.</param>
        /// <returns>The computed value.</returns>
        protected static double Qsfn(double sinphi, double eccent, double one_es)
        {
            if (eccent >= EPS7)
            {
                double con = eccent * sinphi;
                double div1 = 1.0 - (con * con);
                double div2 = 1.0 + con;

                /* avoid zero division, fail gracefully */
                if (div1 == 0.0 || div2 == 0.0)
                {
                    return HUGEVAL;
                }

                return one_es * ((sinphi / div1) - ((.5 / eccent) * Math.Log((1.0 - con) / div2)));
            }
            else
            {
                return sinphi + sinphi;
            }
        }

        /// <summary>
        /// Function to calculate the sine and cosine in one call.  Some computer
        /// systems have implemented this function, resulting in a faster implementation
        /// than calling each function separately.  It is provided here for those
        /// computer systems which don`t implement this function.
        /// </summary>
        /// <param name="cos_val">The cos_val value.</param>
        /// <param name="sin_val">The sin_val value.</param>
        /// <param name="val">The val value.</param>
        protected static void Sincos(double val, out double sin_val, out double cos_val)

        {
            sin_val = Math.Sin(val);
            cos_val = Math.Cos(val);
        }

        /// <summary>
        /// Function to compute the constant small t for use in the forward
        /// computations in the Lambert Conformal Conic and the Polar
        /// Stereographic projections.
        /// </summary>
        /// <param name="eccent">The eccent value.</param>
        /// <param name="phi">The phi value.</param>
        /// <param name="sinphi">The sinphi value.</param>
        /// <returns>The computed value.</returns>
        protected static double Tsfnz(double eccent, double phi, double sinphi)
        {
            double con;
            double com;
            con = eccent * sinphi;
            com = .5 * eccent;
            con = Math.Pow((1.0 - con) / (1.0 + con), com);
            return Math.Tan(.5 * (HALFPI - phi)) / con;
        }

        /// <summary>
        ///
        ///
        /// </summary>
        /// <param name="eccent">The eccent parameter.</param>
        /// <param name="qs">The qs parameter.</param>
        /// <param name="flag">The flag parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double Phi1z(double eccent, double qs, out long flag)
        {
            double eccnts;
            double dphi;
            double con;
            double com;
            double sinpi;
            double cospi;
            double phi;
            flag = 0;

            // double asinz();
            long i;

            phi = Asinz(.5 * qs);
            if (eccent < EPSLN)
            {
                return phi;
            }

            eccnts = eccent * eccent;
            for (i = 1; i <= 25; i++)
            {
                Sincos(phi, out sinpi, out cospi);
                con = eccent * sinpi;
                com = 1.0 - (con * con);
                dphi = .5 * com * com / cospi * ((qs / (1.0 - eccnts)) - (sinpi / com) +
                                         (.5 / eccent * Math.Log((1.0 - con) / (1.0 + con))));
                phi = phi + dphi;
                if (Math.Abs(dphi) <= 1e-7)
                {
                    return phi;
                }
            }

            // p_error ("Convergence error","phi1z-conv");
            // ASSERT(FALSE);
            throw new ArgumentException("Convergence error.");
        }

        /// <summary>
        /// Function to eliminate roundoff errors in asin.
        /// </summary>
        /// <param name="con">The con value.</param>
        /// <returns>The computed value.</returns>
        protected static double Asinz(double con)
        {
            if (Math.Abs(con) > 1.0)
            {
                if (con > 1.0)
                {
                    con = 1.0;
                }
                else
                {
                    con = -1.0;
                }
            }

            return Math.Asin(con);
        }

        /// <summary>
        /// Function to compute the latitude angle, phi2, for the inverse of the
        /// Lambert Conformal Conic and Polar Stereographic projections.
        /// </summary>
        /// <param name="eccent">Spheroid eccentricity.</param>
        /// <param name="ts">Constant value t.</param>
        /// <param name="flag">Error flag number.</param>
        /// <returns>The computed value.</returns>
        protected static double Phi2z(double eccent, double ts, out long flag)
        {
            double con;
            double dphi;
            double sinpi;
            long i;

            flag = 0;
            double eccnth = .5 * eccent;
            double chi = HALFPI - (2 * Math.Atan(ts));
            for (i = 0; i <= 15; i++)
            {
                sinpi = Math.Sin(chi);
                con = eccent * sinpi;
                dphi = HALFPI - (2 * Math.Atan(ts * Math.Pow((1.0 - con) / (1.0 + con), eccnth))) - chi;
                chi += dphi;
                if (Math.Abs(dphi) <= .0000000001)
                {
                    return chi;
                }
            }

            throw new ArgumentException("Convergence error - phi2z-conv");
        }

        /// <summary>
        /// Functions to compute the constants e0, e1, e2, and e3 which are used
        /// in a series for calculating the distance along a meridian.  The
        /// input x represents the eccentricity squared.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <returns>The computed value.</returns>
        protected static double E0fn(double x)
        {
            return 1.0 - (0.25 * x * (1.0 + (x / 16.0 * (3.0 + (1.25 * x)))));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x">The x parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double E1fn(double x)
        {
            return 0.375 * x * (1.0 + (0.25 * x * (1.0 + (0.46875 * x))));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x">The x parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double E2fn(double x)
        {
            return 0.05859375 * x * x * (1.0 + (0.75 * x));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x">The x parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double E3fn(double x)
        {
            return x * x * x * (35.0 / 3072.0);
        }

        /// <summary>
        /// Function to compute the constant e4 from the input of the eccentricity
        /// of the spheroid, x.  This constant is used in the Polar Stereographic
        /// projection.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <returns>The computed value.</returns>
        protected static double E4fn(double x)
        {
            double con;
            double com;
            con = 1.0 + x;
            com = 1.0 - x;
            return Math.Sqrt(Math.Pow(con, con) * Math.Pow(com, com));
        }

        /// <summary>
        /// Function computes the value of M which is the distance along a meridian
        /// from the Equator to latitude phi.
        /// </summary>
        /// <param name="e0">The e0 value.</param>
        /// <param name="e1">The e1 value.</param>
        /// <param name="e2">The e2 value.</param>
        /// <param name="e3">The e3 value.</param>
        /// <param name="phi">The phi value.</param>
        /// <returns>The computed value.</returns>
        protected static double Mlfn(double e0, double e1, double e2, double e3, double phi)
        {
            return (e0 * phi) - (e1 * Math.Sin(2.0 * phi)) + (e2 * Math.Sin(4.0 * phi)) - (e3 * Math.Sin(6.0 * phi));
        }

        /// <summary>
        /// Calculates the meridian distance. This is the distance along the central
        /// meridian from the equator to <paramref name="phi"/>. Accurate to &lt; 1e-5 meters
        /// when used in conjuction with typical major axis values.
        /// </summary>
        /// <param name="phi">The phi parameter.</param>
        /// <param name="sphi">The sphi parameter.</param>
        /// <param name="cphi">The cphi parameter.</param>
        /// <returns>The transformation result.</returns>
        protected double Mlfn(double phi, double sphi, double cphi)
        {
            cphi *= sphi;
            sphi *= sphi;
            return (this.en0 * phi) - (cphi * (this.en1 + (sphi * (this.en2 + (sphi * (this.en3 + (sphi * this.en4)))))));
        }

        /// <summary>
        /// Calculates the latitude (phi) from a meridian distance.
        /// Determines phi to TOL (1e-11) radians, about 1e-6 seconds.
        /// </summary>
        /// <param name="arg">The meridonial distance.</param>
        /// <returns>The latitude of the meridian distance.</returns>
        protected double Inv_mlfn(double arg)
        {
            const double MLFN_TOL = 1E-11;
            const int MAXIMUM_ITERATIONS = 20;
            double s, t, phi, k = 1.0 / (1.0 - this.es);
            int i;
            phi = arg;
            for (i = MAXIMUM_ITERATIONS; /*true*/;)
            {
                // rarely goes over 5 iterations
                if (--i < 0)
                {
                    throw new InvalidOperationException("No convergence");
                }

                s = Math.Sin(phi);
                t = 1.0 - (this.es * s * s);
                t = (this.Mlfn(phi, s, Math.Cos(phi)) - arg) * (t * Math.Sqrt(t)) * k;
                phi -= t;
                if (Math.Abs(t) < MLFN_TOL)
                {
                    return phi;
                }
            }
        }

        /// <summary>
        /// Converts a longitude value in degrees to radians.
        /// </summary>
        /// <param name="x">The value in degrees to convert to radians.</param>
        /// <param name="edge">If true, -180 and +180 are valid, otherwise they are considered out of range.</param>
        /// <returns>The transformation result.</returns>
        protected static double LongitudeToRadians(double x, bool edge)
        {
            if (edge ? (x >= -180 && x <= 180) : (x > -180 && x < 180))
            {
                return DegreesToRadians(x);
            }

            throw new ArgumentOutOfRangeException(
                "x",
                                                  x.ToString(CultureInfo.InvariantCulture) +
                                                  " not a valid longitude in degrees.");
        }

        /// <summary>
        /// Converts a latitude value in degrees to radians.
        /// </summary>
        /// <param name="y">The value in degrees to to radians.</param>
        /// <param name="edge">If true, -90 and +90 are valid, otherwise they are considered out of range.</param>
        /// <returns>The transformation result.</returns>
        protected static double LatitudeToRadians(double y, bool edge)
        {
            if (edge ? (y >= -90 && y <= 90) : (y > -90 && y < 90))
            {
                return DegreesToRadians(y);
            }

            throw new ArgumentOutOfRangeException(
                "y",
                                                  y.ToString(CultureInfo.InvariantCulture) +
                                                  " not a valid latitude in degrees.");
        }

        /// <summary>
        /// authset.
        /// </summary>
        /// <param name="es">The es parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double[] Authset(double es)
        {
            double[] aPA = new double[3];
            aPA[0] = es * P00;
            double t = es * es;
            aPA[0] += t * P01;
            aPA[1] = t * P10;
            t *= es;
            aPA[0] += t * P02;
            aPA[1] += t * P11;
            aPA[2] = t * P20;

            return aPA;
        }

        /// <summary>
        /// authlat.
        /// </summary>
        /// <param name="beta">The beta parameter.</param>
        /// <param name="APA">The APA parameter.</param>
        /// <returns>The transformation result.</returns>
        protected static double Authlat(double beta, double[] APA)
        {
            double t = beta + beta;
            return beta + (APA[0] * Math.Sin(t)) + (APA[1] * Math.Sin(t + t)) + (APA[2] * Math.Sin(t + t + t));
        }

        /// <summary>
        /// Calculates the hypotenuse of a triangle: Sqrt(x*x + y*y).
        /// </summary>
        /// <param name="x">The length of one orthogonal leg of the triangle.</param>
        /// <param name="y">The length of the other orthogonal leg of the triangle.</param>
        /// <returns>The length of the diagonal.</returns>
        protected static double Hypot(double x, double y)
        {
            return Math.Sqrt((x * x) + (y * y));
        }

        /// <summary>
        /// Calculates the flattening factor, (<paramref name="equatorialRadius"/> - <paramref name="polarRadius"/>) / <paramref name="equatorialRadius"/>.
        /// </summary>
        /// <param name="equatorialRadius">The radius of the equator.</param>
        /// <param name="polarRadius">The radius of a circle touching the poles.</param>
        /// <returns>The flattening factor.</returns>
        private static double FlatteningFactor(double equatorialRadius, double polarRadius)
        {
            return (equatorialRadius - polarRadius) / equatorialRadius;
        }

        /// <summary>
        /// Calculates the square of eccentricity according to es = (2f - f^2) where f is the <see cref="FlatteningFactor">flattening factor</see>.
        /// </summary>
        /// <param name="equatorialRadius">The radius of the equator.</param>
        /// <param name="polarRadius">The radius of a circle touching the poles.</param>
        /// <returns>The square of eccentricity.</returns>
        private static double EccentricySquared(double equatorialRadius, double polarRadius)
        {
            double f = FlatteningFactor(equatorialRadius, polarRadius);
            return (2 * f) - (f * f);
        }
    }
}
