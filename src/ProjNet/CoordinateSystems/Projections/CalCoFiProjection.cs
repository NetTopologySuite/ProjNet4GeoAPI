// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the CalCOFI line/station projection (<c>calcofi</c>).
/// </summary>
[Serializable]
internal class CalCoFiProjection : MapProjection
{
    private const double DegToLine = 5d;
    private const double DegToStation = 15d;
    private const double LineToRad = 0.0034906585039886592d;
    private const double StationToRad = 0.0011635528346628863d;
    private const double PointOLine = 80d;
    private const double PointOStation = 60d;
    private const double PointOLambda = -2.1144663887911301d;
    private const double PointOPhi = 0.59602993955606354d;
    private const double RotationAngle = 0.52359877559829882d;

    private readonly bool isEllipsoidal;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalCoFiProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CalCoFiProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalCoFiProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CalCoFiProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(MergeParameters(parameters), inverse)
    {
        this.Name = "Cal_Coop_Ocean_Fish_Invest_Lines_Stations";
        this.isEllipsoidal = this.es != 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new CalCoFiProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        if (Math.Abs(Math.Abs(lat) - HalfPi) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double xMercator = lon;
        double yMercator = this.isEllipsoidal
            ? -Math.Log(Tsfnz(this.e, lat, Math.Sin(lat)))
            : Math.Log(Math.Tan(FortPi + (0.5d * lat)));
        double oYMercator = this.isEllipsoidal
            ? -Math.Log(Tsfnz(this.e, PointOPhi, Math.Sin(PointOPhi)))
            : Math.Log(Math.Tan(FortPi + (0.5d * PointOPhi)));
        double l1 = (yMercator - oYMercator) * Math.Tan(RotationAngle);
        double l2 = -xMercator - l1 + PointOLambda;
        double rYMercator = (l2 * Math.Cos(RotationAngle) * Math.Sin(RotationAngle)) + yMercator;
        double ry;
        if (this.isEllipsoidal)
        {
            ry = Phi2z(this.e, Math.Exp(-rYMercator), out _);
        }
        else
        {
            ry = HalfPi - (2d * Math.Atan(Math.Exp(-rYMercator)));
        }

        lon = PointOLine - (RadiansToDegrees(ry - PointOPhi) * DegToLine / Math.Cos(RotationAngle));
        lat = PointOStation + (RadiansToDegrees(ry - lat) * DegToStation / Math.Sin(RotationAngle));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double ry = PointOPhi - (LineToRad * (x - PointOLine) * Math.Cos(RotationAngle));
        y = ry - (StationToRad * (y - PointOStation) * Math.Sin(RotationAngle));
        double oYMercator = this.isEllipsoidal
            ? -Math.Log(Tsfnz(this.e, PointOPhi, Math.Sin(PointOPhi)))
            : Math.Log(Math.Tan(FortPi + (0.5d * PointOPhi)));
        double rYMercator = this.isEllipsoidal
            ? -Math.Log(Tsfnz(this.e, ry, Math.Sin(ry)))
            : Math.Log(Math.Tan(FortPi + (0.5d * ry)));
        double xYMercator = this.isEllipsoidal
            ? -Math.Log(Tsfnz(this.e, y, Math.Sin(y)))
            : Math.Log(Math.Tan(FortPi + (0.5d * y)));
        double l1 = (xYMercator - oYMercator) * Math.Tan(RotationAngle);
        double l2 = (rYMercator - xYMercator) / (Math.Cos(RotationAngle) * Math.Sin(RotationAngle));
        x = PointOLambda - (l1 + l2);
    }

    private static List<ProjectionParameter> MergeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var merged = CloneParametersList(parameters);
        ReplaceOrAdd(merged, "central_meridian", 0d);
        ReplaceOrAdd(merged, "scale_factor", 1d);
        ReplaceOrAdd(merged, "false_easting", 0d);
        ReplaceOrAdd(merged, "false_northing", 0d);
        ReplaceOrAdd(merged, "unit", 1d);
        return merged;
    }

    private static void ReplaceOrAdd(List<ProjectionParameter> parameters, string name, double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(parameters[i].Name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }
}

