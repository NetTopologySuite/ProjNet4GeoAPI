// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Diagnostics.CodeAnalysis;

// Legacy backward-compatible aliases are isolated in this partial file for easy removal in a future major version.
#pragma warning disable CS1591 // Legacy alias members intentionally omit duplicate XML docs; canonical members remain documented on the main partial.
#pragma warning disable CA1707 // Legacy alias names intentionally preserve underscores for backward compatibility.
#pragma warning disable SA1300 // Legacy alias properties intentionally preserve historic lower_snake_case names.
#pragma warning disable SA1303 // Legacy alias constants intentionally preserve historic all-caps naming.
#pragma warning disable SA1307 // Legacy alias constants intentionally preserve historic naming rather than the current style.
#pragma warning disable SA1310 // Legacy alias names intentionally preserve underscores for backward compatibility.
#pragma warning disable IDE1006 // Legacy alias members intentionally preserve historic naming rather than the current style.
#pragma warning disable SA1600 // Legacy alias members intentionally omit duplicate XML docs; canonical members remain documented on the main partial.
#pragma warning disable SA1601 // Legacy alias partial intentionally keeps documentation on the canonical main partial declaration.

[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Obsolete compatibility aliases intentionally preserve legacy all-caps names alongside PascalCase names.")]
public abstract partial class MapProjection
{
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
}

#pragma warning restore SA1600
#pragma warning restore SA1601
#pragma warning restore IDE1006
#pragma warning restore SA1310
#pragma warning restore SA1307
#pragma warning restore SA1303
#pragma warning restore SA1300
#pragma warning restore CA1707
#pragma warning restore CS1591
