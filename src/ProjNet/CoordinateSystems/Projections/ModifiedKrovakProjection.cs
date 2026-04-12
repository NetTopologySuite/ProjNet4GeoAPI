// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Projections;

using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Modified Krovak projection variant with the PROJ polynomial correction terms.
/// </summary>
internal sealed class ModifiedKrovakProjection : KrovakProjection
{
    private const double X0 = 1089000.0;
    private const double Y0 = 654000.0;
    private const double C1 = 2.946529277E-02d;
    private const double C2 = 2.515965696E-02d;
    private const double C3 = 1.193845912E-07d;
    private const double C4 = -4.668270147E-07d;
    private const double C5 = 9.233980362E-12d;
    private const double C6 = 1.523735715E-12d;
    private const double C7 = 1.696780024E-18d;
    private const double C8 = 4.408314235E-18d;
    private const double C9 = -8.331083518E-24d;
    private const double C10 = -3.689471323E-24d;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedKrovakProjection"/> class.
    /// </summary>
    /// <param name="parameters">The projection parameters.</param>
    public ModifiedKrovakProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    private ModifiedKrovakProjection(IEnumerable<ProjectionParameter> parameters, KrovakProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Modified Krovak";
    }

    /// <inheritdoc />
    protected override KrovakProjection CreateInverseProjection() => new ModifiedKrovakProjection(this.Parameters.ToProjectionParameter(), this);

    /// <inheritdoc />
    protected override bool TryComputeModifiedDelta(
        double southing,
        double westing,
        out double deltaSouthing,
        out double deltaWesting)
    {
        double reducedSouthing = southing - X0;
        double reducedWesting = westing - Y0;
        double reducedSouthingSquared = reducedSouthing * reducedSouthing;
        double reducedWestingSquared = reducedWesting * reducedWesting;
        double reducedSouthingFourth = reducedSouthingSquared * reducedSouthingSquared;
        double reducedWestingFourth = reducedWestingSquared * reducedWestingSquared;

        deltaSouthing = C1
            + (C3 * reducedSouthing)
            - (C4 * reducedWesting)
            - (2d * C6 * reducedSouthing * reducedWesting)
            + (C5 * (reducedSouthingSquared - reducedWestingSquared))
            + (C7 * reducedSouthing * (reducedSouthingSquared - (3d * reducedWestingSquared)))
            - (C8 * reducedWesting * ((3d * reducedSouthingSquared) - reducedWestingSquared))
            + (4d * C9 * reducedSouthing * reducedWesting * (reducedSouthingSquared - reducedWestingSquared))
            + (C10 * (reducedSouthingFourth + reducedWestingFourth - (6d * reducedSouthingSquared * reducedWestingSquared)));

        deltaWesting = C2
            + (C3 * reducedWesting)
            + (C4 * reducedSouthing)
            + (2d * C5 * reducedSouthing * reducedWesting)
            + (C6 * (reducedSouthingSquared - reducedWestingSquared))
            + (C8 * reducedSouthing * (reducedSouthingSquared - (3d * reducedWestingSquared)))
            + (C7 * reducedWesting * ((3d * reducedSouthingSquared) - reducedWestingSquared))
            - (4d * C10 * reducedSouthing * reducedWesting * (reducedSouthingSquared - reducedWestingSquared))
            + (C9 * (reducedSouthingFourth + reducedWestingFourth - (6d * reducedSouthingSquared * reducedWestingSquared)));

        return true;
    }
}
