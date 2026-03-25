// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Converts between geodetic and geocentric latitude.
/// </summary>
[Serializable]
internal sealed class GeocentricLatitudeMathTransform : MathTransform
{
    private readonly double geodeticToGeocentricFactor;
    private bool isInverse;
    private MathTransform inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeocentricLatitudeMathTransform"/> class.
    /// </summary>
    /// <param name="semiMajor">Semi-major axis of the ellipsoid.</param>
    /// <param name="semiMinor">Semi-minor axis of the ellipsoid.</param>
    /// <param name="isInverse">
    /// <see langword="true"/> to convert geocentric latitude to geodetic latitude;
    /// <see langword="false"/> to convert geodetic latitude to geocentric latitude.
    /// </param>
    internal GeocentricLatitudeMathTransform(double semiMajor, double semiMinor, bool isInverse)
    {
        if (semiMajor <= 0d || semiMinor <= 0d || double.IsNaN(semiMajor) || double.IsInfinity(semiMajor) || double.IsNaN(semiMinor) || double.IsInfinity(semiMinor))
        {
            throw new ArgumentOutOfRangeException(nameof(semiMajor), "Semi-major and semi-minor axes must be finite and positive.");
        }

        this.geodeticToGeocentricFactor = (semiMinor * semiMinor) / (semiMajor * semiMajor);
        this.isInverse = isInverse;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeocentricLatitudeMathTransform"/> class
    /// using a precomputed geodetic-to-geocentric scale factor.
    /// </summary>
    /// <param name="geodeticToGeocentricFactor">Scale factor applied to tangent of latitude in forward mode.</param>
    /// <param name="isInverse">
    /// <see langword="true"/> to convert geocentric latitude to geodetic latitude;
    /// <see langword="false"/> to convert geodetic latitude to geocentric latitude.
    /// </param>
    private GeocentricLatitudeMathTransform(double geodeticToGeocentricFactor, bool isInverse)
    {
        if (geodeticToGeocentricFactor <= 0d || double.IsNaN(geodeticToGeocentricFactor) || double.IsInfinity(geodeticToGeocentricFactor))
        {
            throw new ArgumentOutOfRangeException(nameof(geodeticToGeocentricFactor), "Geocentric latitude factor must be finite and positive.");
        }

        this.geodeticToGeocentricFactor = geodeticToGeocentricFactor;
        this.isInverse = isInverse;
    }

    /// <inheritdoc/>
    public override int DimSource => 2;

    /// <inheritdoc/>
    public override int DimTarget => 2;

    /// <inheritdoc/>
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc/>
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool Identity() => this.geodeticToGeocentricFactor.Equals(1d);

    /// <inheritdoc/>
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new GeocentricLatitudeMathTransform(this.geodeticToGeocentricFactor, !this.isInverse);
        }

        return this.inverse;
    }

    /// <inheritdoc/>
    public override void Invert()
    {
        this.isInverse = !this.isInverse;
        this.inverse = null;
    }

    /// <inheritdoc/>
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (double.IsNaN(y))
        {
            return;
        }

        double tangent = Math.Tan(DegreesToRadians(y));
        double factor = this.isInverse
            ? 1d / this.geodeticToGeocentricFactor
            : this.geodeticToGeocentricFactor;
        y = RadiansToDegrees(Math.Atan(factor * tangent));
    }
}
