// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

/// <summary>
/// Represents a coordinate operation catalog definition.
/// </summary>
internal sealed class CoordinateOperationDefinition(
    CoordinateOperationKind operationKind,
    int operationCode,
    int sourceSrid,
    int targetSrid,
    double accuracy,
    string methodName,
    string parameterFileName,
    double areaSouthLatitude,
    double areaNorthLatitude,
    double areaWestLongitude,
    double areaEastLongitude)
{
    /// <summary>
    /// Gets the expected operation accuracy.
    /// </summary>
    internal double Accuracy { get; } = accuracy;

    /// <summary>
    /// Gets the operation method name.
    /// </summary>
    internal string MethodName { get; } = methodName;

    /// <summary>
    /// Gets the operation code.
    /// </summary>
    internal int OperationCode { get; } = operationCode;

    /// <summary>
    /// Gets the operation kind.
    /// </summary>
    internal CoordinateOperationKind OperationKind { get; } = operationKind;

    /// <summary>
    /// Gets the optional parameter file name.
    /// </summary>
    internal string ParameterFileName { get; } = parameterFileName;

    /// <summary>
    /// Gets the source SRID.
    /// </summary>
    internal int SourceSrid { get; } = sourceSrid;

    /// <summary>
    /// Gets the target SRID.
    /// </summary>
    internal int TargetSrid { get; } = targetSrid;

    /// <summary>
    /// Gets the approximate south latitude bound of the operation area of use.
    /// </summary>
    internal double AreaSouthLatitude { get; } = areaSouthLatitude;

    /// <summary>
    /// Gets the approximate north latitude bound of the operation area of use.
    /// </summary>
    internal double AreaNorthLatitude { get; } = areaNorthLatitude;

    /// <summary>
    /// Gets the approximate west longitude bound of the operation area of use.
    /// </summary>
    internal double AreaWestLongitude { get; } = areaWestLongitude;

    /// <summary>
    /// Gets the approximate east longitude bound of the operation area of use.
    /// </summary>
    internal double AreaEastLongitude { get; } = areaEastLongitude;

    /// <summary>
    /// Computes a rough coverage area in degree-squared based on the operation bounds.
    /// </summary>
    /// <returns>The approximate area coverage, or <see cref="double.MaxValue"/> when unavailable.</returns>
    internal double GetApproximateAreaOfUseCoverage()
    {
        if (double.IsNaN(this.AreaSouthLatitude)
            || double.IsNaN(this.AreaNorthLatitude)
            || double.IsNaN(this.AreaWestLongitude)
            || double.IsNaN(this.AreaEastLongitude))
        {
            return double.MaxValue;
        }

        if (this.AreaSouthLatitude < -90d || this.AreaSouthLatitude > 90d || this.AreaNorthLatitude < -90d || this.AreaNorthLatitude > 90d)
        {
            return double.MaxValue;
        }

        if (this.AreaSouthLatitude > this.AreaNorthLatitude)
        {
            return double.MaxValue;
        }

        if (this.AreaWestLongitude < -180d || this.AreaWestLongitude > 180d || this.AreaEastLongitude < -180d || this.AreaEastLongitude > 180d)
        {
            return double.MaxValue;
        }

        double latitudeSpan = this.AreaNorthLatitude - this.AreaSouthLatitude;
        double longitudeSpan = this.AreaEastLongitude >= this.AreaWestLongitude
            ? this.AreaEastLongitude - this.AreaWestLongitude
            : 360d - (this.AreaWestLongitude - this.AreaEastLongitude);
        if (longitudeSpan < 0d)
        {
            return double.MaxValue;
        }

        return latitudeSpan * longitudeSpan;
    }
}
