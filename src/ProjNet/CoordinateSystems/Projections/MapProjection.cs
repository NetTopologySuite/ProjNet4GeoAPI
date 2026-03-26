// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Abstract base class for all map projections, providing shared mathematical utilities and
/// coordinate transformation infrastructure.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Legacy PROJ-compatible API surface is preserved for compatibility.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Legacy PROJ-compatible API surface is preserved for compatibility.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Obsolete compatibility aliases intentionally preserve legacy all-caps names alongside PascalCase names.")]
[Serializable]
public abstract class MapProjection : MathTransform, IProjection
{
    /// <summary>
    /// Tolerance constant equal to 1e-10, used for near-zero comparisons in projection formulas.
    /// </summary>
    protected const double Eps10 = 1e-10;

    /// <summary>
    /// Tolerance constant equal to 1e-7, used for near-zero comparisons in projection formulas.
    /// </summary>
    protected const double Eps7 = 1e-7;

    /// <summary>
    /// Sentinel value equal to <see cref="double.NaN"/>, used to signal an undefined or out-of-range projection result.
    /// </summary>
    protected const double HugeVal = double.NaN;

    /// <summary>
    /// The constant pi, equal to <see cref="Math.PI"/>.
    /// </summary>
    protected const double PI = Math.PI;

    /// <summary>
    /// A fourth of <see cref="Math.PI"/>.
    /// </summary>
    protected const double FortPi = PI * 0.25;

    /// <summary>
    /// Half of PI.
    /// </summary>
    protected const double HalfPi = PI * 0.5;

    /// <summary>
    /// PI * 2.
    /// </summary>
    protected const double TwoPi = PI * 2.0;

    /// <summary>
    /// Tolerance threshold for near-zero comparisons; equal to <see cref="Eps10"/>.
    /// </summary>
    protected const double Epsln = Eps10;

    /// <summary>
    /// Conversion factor from arc-seconds to radians (pi / 648000 ~= 4.848e-6).
    /// </summary>
    protected const double S2R = 4.848136811095359e-6;

    /// <summary>
    /// Maximum iteration count used in longitude normalisation loops.
    /// </summary>
    protected const double MaxVal = 4;

    /// <summary>
    /// Maximum 32-bit integer value (2 147 483 647) used as a scale threshold in longitude normalisation.
    /// </summary>
    protected const double prjMAXLONG = 2147483647;

    /// <summary>
    /// Large double constant used as an upper-bound threshold in longitude normalisation.
    /// </summary>
    protected const double DblLong = 4.61168601e18;

    // Backward-compatible aliases for legacy public API names.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1303 // Const field names should begin with upper-case letter
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1600 // Elements should be documented
    [Obsolete("Use FortPi instead.")]
    protected const double FORTPI = FortPi;
    [Obsolete("Use HalfPi instead.")]
    protected const double HALFPI = HalfPi;
    [Obsolete("Use HugeVal instead.")]
    protected const double HUGEVAL = HugeVal;
    [Obsolete("Use MaxVal instead.")]
    protected const double MAXVAL = MaxVal;
    [Obsolete("Use TwoPi instead.")]
    protected const double TWOPI = TwoPi;
    [Obsolete("Use Eps10 instead.")]
    protected const double EPS10 = Eps10;
    [Obsolete("Use Eps7 instead.")]
    protected const double EPS7 = Eps7;
    [Obsolete("Use Epsln instead.")]
    protected const double EPSLN = Epsln;
    [Obsolete("Use DblLong instead.")]
    protected const double DBLLONG = DblLong;
    [Obsolete("Use FortPi instead.")]
    protected const double FORT_PI = FortPi;
    [Obsolete("Use HalfPi instead.")]
    protected const double HALF_PI = HalfPi;
    [Obsolete("Use HugeVal instead.")]
    protected const double HUGE_VAL = HugeVal;
    [Obsolete("Use MaxVal instead.")]
    protected const double MAX_VAL = MaxVal;
    [Obsolete("Use TwoPi instead.")]
    protected const double TWO_PI = TwoPi;
#pragma warning restore SA1310
#pragma warning restore SA1307
#pragma warning restore SA1303
#pragma warning restore SA1300
#pragma warning restore CA1707
#pragma warning restore CS1591
#pragma warning restore SA1600

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
    protected readonly double scaleFactor;

    /// <summary>
    /// Center latitude.
    /// </summary>
    protected readonly double latOrigin;

    /// <summary>
    /// Y offset in meters.
    /// </summary>
    protected readonly double falseNorthing;

    /// <summary>
    /// X offset in meters.
    /// </summary>
    protected readonly double falseEasting;

    /// <summary>
    /// Coefficient 0 for <see cref="Mlfn(double,double,double,double,double)"/>.
    /// </summary>
    protected readonly double en0;

    /// <summary>
    /// Coefficient 1 for <see cref="Mlfn(double,double,double,double,double)"/>.
    /// </summary>
    protected readonly double en1;

    /// <summary>
    /// Coefficient 2 for <see cref="Mlfn(double,double,double,double,double)"/>.
    /// </summary>
    protected readonly double en2;

    /// <summary>
    /// Coefficient 3 for <see cref="Mlfn(double,double,double,double,double)"/>.
    /// </summary>
    protected readonly double en3;

    /// <summary>
    /// Coefficient 4 for <see cref="Mlfn(double,double,double,double,double)"/>.
    /// </summary>
    protected readonly double en4;

    /// <summary>
    /// A set of projection parameters for this projection.
    /// </summary>
    protected readonly ProjectionParameterSet Parameters;

    /// <summary>
    /// The inverse <see cref="MathTransform"/>.
    /// </summary>
    protected MathTransform inverse;

    /// <summary>
    /// Center longitude (projection center).
    /// </summary>
    protected double centralMeridian;

    private const double C00 = 1.0;
    private const double C02 = 0.25;
    private const double C04 = 0.046875;
    private const double C06 = 0.01953125;
    private const double C08 = 0.01068115234375;
    private const double C22 = 0.75;
    private const double C44 = 0.46875;
    private const double C46 = 0.01302083333333333333;
    private const double C48 = 0.00712076822916666666;
    private const double C66 = 0.36458333333333333333;
    private const double C68 = 0.00569661458333333333;
    private const double C88 = 0.3076171875;

    /// <summary>
    /// Fraction constant 1/3 used in inverse meridional distance series.
    /// </summary>
    private const double P00 = 0.33333333333333333333;

    /// <summary>
    /// Fraction constant 31/180 used in inverse meridional distance series.
    /// </summary>
    private const double P01 = 0.17222222222222222222;

    /// <summary>
    /// Fraction constant 517/5040 used in inverse meridional distance series.
    /// </summary>
    private const double P02 = 0.10257936507936507937;

    /// <summary>
    /// Fraction constant 23/360 used in inverse meridional distance series.
    /// </summary>
    private const double P10 = 0.06388888888888888888;

    /// <summary>
    /// Fraction constant 251/3780 used in inverse meridional distance series.
    /// </summary>
    private const double P11 = 0.06640211640211640212;

    /// <summary>
    /// Fraction constant 761/45360 used in inverse meridional distance series.
    /// </summary>
    private const double P20 = 0.01677689594356261023;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapProjection"/> class with a paired inverse projection.
    /// </summary>
    /// <param name="parameters">An enumeration of projection parameters.</param>
    /// <param name="inverse">The paired inverse projection, or <see langword="null"/> if not yet available.</param>
    protected MapProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : this(parameters)
    {
        this.inverse = inverse;
        if (inverse is not null)
        {
            inverse.inverse = this;
            this.IsInverse = !inverse.IsInverse;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapProjection"/> class from a set of projection parameters.
    /// </summary>
    /// <param name="parameters">An enumeration of projection parameters.</param>
    protected MapProjection(IEnumerable<ProjectionParameter> parameters)
    {
        this.Parameters = new ProjectionParameterSet(parameters);

        this.semiMajor = this.Parameters.GetParameterValue("semi_major");
        this.semiMinor = this.Parameters.GetParameterValue("semi_minor");

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
    public string ClassName => this.Name;

    /// <summary>
    /// Gets the number of projection parameters.
    /// </summary>
    public int NumParameters => this.Parameters.Count;

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
    /// Calculates the UTM zone number for the given longitude.
    /// </summary>
    /// <param name="lon">The longitude in decimal degrees.</param>
    /// <returns>The UTM zone number (1-60).</returns>
    public static long CalcUtmZone(double lon) => (long)(((lon + 180.0) / 6.0) + 1.0);

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

            sb.Append(']');
            if (this.IsInverse)
            {
                sb.Append(']');
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
                this.IsInverse ? "<CT_InverseTransform Name=\"{0}\">" : "<CT_ParameterizedMathTransform Name=\"{0}\">",
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
    public sealed override int DimSource => 2;

    /// <inheritdoc/>
    public sealed override int DimTarget => 2;

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
        if (this.inverse is not null)
        {
            ((MapProjection)this.inverse).Invert(false);
        }
    }

    /// <summary>
    /// Determines whether this projection is equal to another projection by comparing only the
    /// coordinate-system parameters.
    /// </summary>
    /// <remarks>
    /// Name, abbreviation, authority, alias, and remarks are ignored in the comparison.
    /// </remarks>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if the projection parameters and direction are equal; otherwise <see langword="false"/>.</returns>
    public bool EqualParams(object obj)
    {
        if (obj is not MapProjection projection)
        {
            return false;
        }

        if (!this.Parameters.Equals(projection.Parameters))
        {
            return false;
        }

        return this.IsInverse == projection.IsInverse;
    }

    /// <summary>
    /// Returns the projection parameter at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the parameter.</param>
    /// <returns>The <see cref="ProjectionParameter"/> at <paramref name="index"/>.</returns>
    public ProjectionParameter GetParameter(int index) => this.Parameters.GetAtIndex(index);

    /// <summary>
    /// Gets a named parameter of the projection.
    /// </summary>
    /// <remarks>The parameter name is case insensitive.</remarks>
    /// <param name="name">Name of parameter.</param>
    /// <returns>The named <see cref="ProjectionParameter"/>, or <see langword="null"/> if not found.</returns>
    public ProjectionParameter GetParameter(string name) => this.Parameters.Find(name);

    /// <summary>
    /// Gets a value indicating whether this projection operates in the inverse direction.
    /// </summary>
    /// <remarks>
    /// Most map projections define the forward direction as geographic-to-projection (lon/lat -> x/y)
    /// and the inverse direction as projection-to-geographic (x/y -> lon/lat).
    /// </remarks>
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
    /// Converts a series of points from geographic degrees to target projection units.
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
    /// <param name="xs">The x-ordinates.</param>
    /// <param name="ys">The y-ordinates.</param>
    /// <param name="strideX">A stride value for x-ordinates.</param>
    /// <param name="strideY">A stride value for y-ordinates.</param>
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
    /// <param name="strideX">A stride value for x-ordinates.</param>
    /// <param name="strideY">A stride value for y-ordinates.</param>
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
    /// Transforms the unit of input coordinates to meters by multiplying by
    /// <see cref="metersPerUnit"/> and subtracting <see cref="falseEasting"/>
    /// or <see cref="falseNorthing"/>.
    /// </summary>
    /// <param name="xs">The x-ordinates.</param>
    /// <param name="ys">The y-ordinates.</param>
    /// <param name="strideX">A stride value for x-ordinates.</param>
    /// <param name="strideY">A stride value for y-ordinates.</param>
    protected void SourceToMeters(Span<double> xs, Span<double> ys, int strideX, int strideY)
    {
        MultiplyThenAddInPlace(xs, strideX, this.metersPerUnit, -this.falseEasting);
        MultiplyThenAddInPlace(ys, strideY, this.metersPerUnit, -this.falseNorthing);
    }

    /// <summary>
    /// Transforms the unit of the input coordinate to meters by multiplying by
    /// <see cref="metersPerUnit"/> and subtracting <see cref="falseEasting"/>
    /// or <see cref="falseNorthing"/>.
    /// </summary>
    /// <param name="x">A x-ordinate.</param>
    /// <param name="y">A y-ordinate.</param>
    protected void SourceToMeters(ref double x, ref double y)
    {
        x = (x * this.metersPerUnit) - this.falseEasting;
        y = (y * this.metersPerUnit) - this.falseNorthing;
    }

    /// <summary>
    /// Reverses this transformation.
    /// </summary>
    /// <param name="invertInverse">If <see langword="true"/>, also inverts the paired <see cref="inverse"/> projection.</param>
    protected void Invert(bool invertInverse)
    {
        this.IsInverse = !this.IsInverse;
        if (invertInverse && this.inverse is not null)
        {
            ((MapProjection)this.inverse).Invert(false);
        }
    }

    /// <summary>
    /// Gets or sets the central meridian (projection centre longitude) in radians; an alias for <see cref="centralMeridian"/>.
    /// </summary>
    protected double Lon_origin
    {
        get => this.centralMeridian;
        set => this.centralMeridian = value;
    }

    // Backward-compatible aliases for legacy field names.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1600 // Elements should be documented
    [Obsolete("Use centralMeridian instead.")]
    protected double central_meridian
    {
        get => this.centralMeridian;
        set => this.centralMeridian = value;
    }

    [Obsolete("Use falseEasting instead.")]
    protected double false_easting => this.falseEasting;

    [Obsolete("Use falseNorthing instead.")]
    protected double false_northing => this.falseNorthing;

    [Obsolete("Use latOrigin instead.")]
    protected double lat_origin => this.latOrigin;

    [Obsolete("Use scaleFactor instead.")]
    protected double scale_factor => this.scaleFactor;
#pragma warning restore SA1300
#pragma warning restore CA1707
#pragma warning restore CS1591
#pragma warning restore SA1600

    /// <summary>
    /// Gets the central parallel (projection centre latitude) in radians; an alias for <see cref="latOrigin"/>.
    /// </summary>
    protected double Central_parallel => this.latOrigin;

    /// <summary>
    /// Gets the origin latitude phi0 in radians; an alias for <see cref="latOrigin"/>.
    /// </summary>
    protected double Phi0 => this.latOrigin;

    /// <summary>
    /// Returns a list of cloned projection parameters.
    /// </summary>
    /// <param name="projectionParameters">The projection parameters to clone.</param>
    /// <returns>A new list containing a copy of each <see cref="ProjectionParameter"/>.</returns>
    protected internal static List<ProjectionParameter> CloneParametersList(
        IEnumerable<ProjectionParameter> projectionParameters)
    {
        ArgumentGuard.ThrowIfNull(projectionParameters, nameof(projectionParameters));

        int capacity = projectionParameters is ICollection<ProjectionParameter> collection
            ? collection.Count
            : 0;
        var res = capacity > 0
            ? new List<ProjectionParameter>(capacity)
            : new List<ProjectionParameter>();
        foreach (var pp in projectionParameters)
        {
            res.Add(new ProjectionParameter(pp.Name, pp.Value));
        }

        return res;
    }

    /// <summary>
    /// Returns the cube of a number.
    /// </summary>
    /// <param name="x">The value to cube.</param>
    /// <returns>The cube of <paramref name="x"/> (x^3).</returns>
    protected static double CUBE(double x)
    {
        return x * x * x; // x^3
    }

    /// <summary>
    /// Returns the fourth power of a number.
    /// </summary>
    /// <param name="x">The value to raise to the fourth power.</param>
    /// <returns>The fourth power of <paramref name="x"/> (x^4).</returns>
    protected static double QUAD(double x)
    {
        double squared = x * x;
        return squared * squared; // x^4
    }

    /// <summary>
    /// Returns the greater value of two inputs.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The greater of <paramref name="a"/> and <paramref name="b"/>.</returns>
    protected static double GMAX(ref double a, ref double b)
    {
        return Math.Max(a, b); // assign maximum of a and b
    }

    /// <summary>
    /// Returns the smaller value of two inputs.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The lesser of <paramref name="a"/> and <paramref name="b"/>.</returns>
    protected static double GMIN(ref double a, ref double b)
    {
        return a < b ? a : b; // assign minimum of a and b
    }

    /// <summary>
    /// Computes the floating-point integer modulus of <paramref name="a"/> divided by <paramref name="b"/>.
    /// </summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The remainder of the integer division of <paramref name="a"/> by <paramref name="b"/>.</returns>
    protected static double IMOD(double a, double b)
    {
        return a - ((a / b) * b); // Integer mod function
    }

    /// <summary>
    /// Returns the sign of an argument.
    /// </summary>
    /// <param name="x">The value to evaluate.</param>
    /// <returns>1 if <paramref name="x"/> is non-negative; otherwise -1.</returns>
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
    /// Normalises a longitude angle into the canonical interval [-pi, pi].
    /// </summary>
    /// <param name="x">The longitude in radians to normalise.</param>
    /// <returns>The normalised longitude in radians, within [-pi, pi].</returns>
    protected static double Adjust_lon(double x)
    {
        long count = 0;
        while (true)
        {
            if (Math.Abs(x) <= PI)
            {
                break;
            }
            else if (((long)Math.Abs(x / Math.PI)) < 2)
            {
                x = x - (Sign(x) * TwoPi);
            }
            else if (((long)Math.Abs(x / TwoPi)) < prjMAXLONG)
            {
                x = x - (((long)(x / TwoPi)) * TwoPi);
            }
            else if (((long)Math.Abs(x / (prjMAXLONG * TwoPi))) < prjMAXLONG)
            {
                x = x - (((long)(x / (prjMAXLONG * TwoPi))) * (TwoPi * prjMAXLONG));
            }
            else if (((long)Math.Abs(x / (DblLong * TwoPi))) < prjMAXLONG)
            {
                x = x - (((long)(x / (DblLong * TwoPi))) * (TwoPi * DblLong));
            }
            else
            {
                x = x - (Sign(x) * TwoPi);
            }

            count++;
            if (count > MaxVal)
            {
                break;
            }
        }

        return x;
    }

    /// <summary>
    /// Computes the small m function: the radius of a parallel of latitude phi divided by the semi-major axis.
    /// </summary>
    /// <param name="eccent">The ellipsoid eccentricity.</param>
    /// <param name="sinphi">The sine of the latitude angle phi.</param>
    /// <param name="cosphi">The cosine of the latitude angle phi.</param>
    /// <returns>The value of the small m function for latitude phi.</returns>
    protected static double Msfnz(double eccent, double sinphi, double cosphi)
    {
        double con;

        con = eccent * sinphi;
        return cosphi / Math.Sqrt(1.0 - (con * con));
    }

    /// <summary>
    /// Computes the small q function: the authalic latitude weighting used in equal-area projections.
    /// </summary>
    /// <param name="sinphi">The sine of the latitude angle phi.</param>
    /// <param name="eccent">The ellipsoid eccentricity.</param>
    /// <returns>The value of the small q function for latitude phi.</returns>
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
    /// Computes the small q function with an explicit (1 - e^2) factor supplied by the caller.
    /// </summary>
    /// <param name="sinphi">The sine of the latitude angle phi.</param>
    /// <param name="eccent">The ellipsoid eccentricity.</param>
    /// <param name="one_es">One minus the square of the eccentricity (1 - e^2).</param>
    /// <returns>The value of the small q function for latitude phi, or <see cref="HugeVal"/> on a singularity.</returns>
    protected static double Qsfn(double sinphi, double eccent, double one_es)
    {
        if (eccent >= Eps7)
        {
            double con = eccent * sinphi;
            double div1 = 1.0 - (con * con);
            double div2 = 1.0 + con;

            // avoid zero division, fail gracefully
            if (div1 == 0.0 || div2 == 0.0)
            {
                return HugeVal;
            }

            return one_es * ((sinphi / div1) - ((.5 / eccent) * Math.Log((1.0 - con) / div2)));
        }
        else
        {
            return sinphi + sinphi;
        }
    }

    /// <summary>
    /// Computes the sine and cosine of an angle in a single call.
    /// </summary>
    /// <param name="val">The angle in radians.</param>
    /// <param name="sin_val">The sine of <paramref name="val"/>.</param>
    /// <param name="cos_val">The cosine of <paramref name="val"/>.</param>
    protected static void Sincos(double val, out double sin_val, out double cos_val)
    {
        sin_val = Math.Sin(val);
        cos_val = Math.Cos(val);
    }

    /// <summary>
    /// Computes the small t value used in forward Lambert Conformal Conic and Polar Stereographic projections.
    /// </summary>
    /// <param name="eccent">The ellipsoid eccentricity.</param>
    /// <param name="phi">The latitude angle in radians.</param>
    /// <param name="sinphi">The sine of <paramref name="phi"/>.</param>
    /// <returns>The small t value for the given latitude.</returns>
    protected static double Tsfnz(double eccent, double phi, double sinphi)
    {
        double con;
        double com;
        con = eccent * sinphi;
        com = .5 * eccent;
        con = Math.Pow((1.0 - con) / (1.0 + con), com);
        return Math.Tan(.5 * (HalfPi - phi)) / con;
    }

    /// <summary>
    /// Computes latitude from the Snyder q-function using an iterative solution.
    /// </summary>
    /// <param name="eccent">The ellipsoid eccentricity.</param>
    /// <param name="qs">The value of the Snyder q-function.</param>
    /// <param name="flag">Set to a non-zero value on error; otherwise 0.</param>
    /// <returns>The latitude in radians corresponding to the given q value.</returns>
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

        long i;

        phi = Asinz(.5 * qs);
        if (eccent < Epsln)
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

        ArgumentGuard.ThrowArgument("Convergence error.");
        return 0d;
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
    /// Computes the latitude angle phi2 for the inverse of the Lambert Conformal Conic and Polar Stereographic projections.
    /// </summary>
    /// <param name="eccent">Spheroid eccentricity.</param>
    /// <param name="ts">The small t value from <see cref="Tsfnz"/>.</param>
    /// <param name="flag">Set to a non-zero value on error; otherwise 0.</param>
    /// <returns>The latitude phi2 in radians.</returns>
    protected static double Phi2z(double eccent, double ts, out long flag)
    {
        double con;
        double dphi;
        double sinpi;
        long i;

        flag = 0;
        double eccnth = .5 * eccent;
        double chi = HalfPi - (2 * Math.Atan(ts));
        for (i = 0; i <= 15; i++)
        {
            sinpi = Math.Sin(chi);
            con = eccent * sinpi;
            dphi = HalfPi - (2 * Math.Atan(ts * Math.Pow((1.0 - con) / (1.0 + con), eccnth))) - chi;
            chi += dphi;
            if (Math.Abs(dphi) <= .0000000001)
            {
                return chi;
            }
        }

        ArgumentGuard.ThrowArgument("Convergence error - phi2z-conv");
        return 0d;
    }

    /// <summary>
    /// Computes the zeroth meridional arc series coefficient e0 from the squared eccentricity.
    /// </summary>
    /// <param name="x">The squared eccentricity (e^2) of the ellipsoid.</param>
    /// <returns>The coefficient e0.</returns>
    protected static double E0fn(double x) => 1.0 - (0.25 * x * (1.0 + (x / 16.0 * (3.0 + (1.25 * x)))));

    /// <summary>
    /// Computes the first meridional distance series coefficient e1 from the squared eccentricity.
    /// </summary>
    /// <param name="x">The squared eccentricity (e^2) of the ellipsoid.</param>
    /// <returns>The coefficient e1.</returns>
    protected static double E1fn(double x) => 0.375 * x * (1.0 + (0.25 * x * (1.0 + (0.46875 * x))));

    /// <summary>
    /// Computes the second meridional distance series coefficient e2 from the squared eccentricity.
    /// </summary>
    /// <param name="x">The squared eccentricity (e^2) of the ellipsoid.</param>
    /// <returns>The coefficient e2.</returns>
    protected static double E2fn(double x) => 0.05859375 * x * x * (1.0 + (0.75 * x));

    /// <summary>
    /// Computes the third meridional distance series coefficient e3 from the squared eccentricity.
    /// </summary>
    /// <param name="x">The squared eccentricity (e^2) of the ellipsoid.</param>
    /// <returns>The coefficient e3.</returns>
    protected static double E3fn(double x) => x * x * x * (35.0 / 3072.0);

    /// <summary>
    /// Computes the coefficient e4 used in the Polar Stereographic projection from the ellipsoid eccentricity.
    /// </summary>
    /// <param name="x">The eccentricity of the ellipsoid.</param>
    /// <returns>The coefficient e4.</returns>
    protected static double E4fn(double x)
    {
        double con;
        double com;
        con = 1.0 + x;
        com = 1.0 - x;
        return Math.Sqrt(Math.Pow(con, con) * Math.Pow(com, com));
    }

    /// <summary>
    /// Computes the meridian distance M from the equator to latitude phi using a four-coefficient series.
    /// </summary>
    /// <param name="e0">The meridional arc coefficient e0.</param>
    /// <param name="e1">The meridional arc coefficient e1.</param>
    /// <param name="e2">The meridional arc coefficient e2.</param>
    /// <param name="e3">The meridional arc coefficient e3.</param>
    /// <param name="phi">The latitude in radians.</param>
    /// <returns>The meridian distance M from the equator to latitude <paramref name="phi"/>.</returns>
    protected static double Mlfn(double e0, double e1, double e2, double e3, double phi) => (e0 * phi) - (e1 * Math.Sin(2.0 * phi)) + (e2 * Math.Sin(4.0 * phi)) - (e3 * Math.Sin(6.0 * phi));

    /// <summary>
    /// Calculates the meridian distance. This is the distance along the central
    /// meridian from the equator to <paramref name="phi"/>. Accurate to &lt; 1e-5 meters
    /// when used in conjuction with typical major axis values.
    /// </summary>
    /// <param name="phi">The latitude in radians.</param>
    /// <param name="sphi">The sine of <paramref name="phi"/>.</param>
    /// <param name="cphi">The cosine of <paramref name="phi"/>.</param>
    /// <returns>The meridian distance M from the equator to latitude <paramref name="phi"/>.</returns>
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
    /// <param name="arg">The meridional distance.</param>
    /// <returns>The latitude in radians corresponding to meridian distance <paramref name="arg"/>.</returns>
    protected double Inv_mlfn(double arg)
    {
        const double MLFN_TOL = 1E-11;
        const int MAXIMUM_ITERATIONS = 20;
        double s, t, phi, k = 1.0 / (1.0 - this.es);
        phi = arg;
        int i = MAXIMUM_ITERATIONS;
        while (true)
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
    /// <returns>The longitude converted to radians.</returns>
    protected static double LongitudeToRadians(double x, bool edge)
    {
        if (edge ? (x >= -180 && x <= 180) : (x > -180 && x < 180))
        {
            return DegreesToRadians(x);
        }

        string longitudeMessage = x.ToString(CultureInfo.InvariantCulture) + " not a valid longitude in degrees.";
        ArgumentGuard.ThrowArgumentOutOfRange(nameof(x), longitudeMessage);
        return 0d;
    }

    /// <summary>
    /// Converts a latitude value in degrees to radians.
    /// </summary>
    /// <param name="y">The value in degrees to convert to radians.</param>
    /// <param name="edge">If true, -90 and +90 are valid, otherwise they are considered out of range.</param>
    /// <returns>The latitude converted to radians.</returns>
    protected static double LatitudeToRadians(double y, bool edge)
    {
        if (edge ? (y >= -90 && y <= 90) : (y > -90 && y < 90))
        {
            return DegreesToRadians(y);
        }

        string latitudeMessage = y.ToString(CultureInfo.InvariantCulture) + " not a valid latitude in degrees.";
        ArgumentGuard.ThrowArgumentOutOfRange(nameof(y), latitudeMessage);
        return 0d;
    }

    /// <summary>
    /// Computes the series coefficients used by <see cref="Authlat"/> for authalic latitude conversion.
    /// </summary>
    /// <param name="es">The squared eccentricity (e^2) of the ellipsoid.</param>
    /// <returns>An array of three series coefficients for the authalic latitude series.</returns>
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
    /// Converts an authalic latitude to a geodetic latitude using a series approximation.
    /// </summary>
    /// <param name="beta">The authalic latitude in radians.</param>
    /// <param name="apa">The series coefficients from <see cref="Authset"/>.</param>
    /// <returns>The geodetic latitude in radians.</returns>
    protected static double Authlat(double beta, double[] apa)
    {
        ArgumentGuard.ThrowIfNull(apa, nameof(apa));

        double t = beta + beta;
        return beta + (apa[0] * Math.Sin(t)) + (apa[1] * Math.Sin(t + t)) + (apa[2] * Math.Sin(t + t + t));
    }

    /// <summary>
    /// Calculates the hypotenuse of a triangle: Sqrt(x*x + y*y).
    /// </summary>
    /// <param name="x">The length of one orthogonal leg of the triangle.</param>
    /// <param name="y">The length of the other orthogonal leg of the triangle.</param>
    /// <returns>The length of the diagonal.</returns>
    protected static double Hypot(double x, double y) => Math.Sqrt((x * x) + (y * y));

    /// <summary>
    /// Calculates the flattening factor, (<paramref name="equatorialRadius"/> - <paramref name="polarRadius"/>) / <paramref name="equatorialRadius"/>.
    /// </summary>
    /// <param name="equatorialRadius">The radius of the equator.</param>
    /// <param name="polarRadius">The radius of a circle touching the poles.</param>
    /// <returns>The flattening factor.</returns>
    private static double FlatteningFactor(double equatorialRadius, double polarRadius) => (equatorialRadius - polarRadius) / equatorialRadius;

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
