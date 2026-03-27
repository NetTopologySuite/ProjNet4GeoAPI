// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Represents a pass-through transform that leaves all ordinates unchanged.
/// </summary>
[Serializable]
internal sealed class IdentityMathTransform : MathTransform
{
    private readonly int dimension;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityMathTransform"/> class.
    /// </summary>
    /// <param name="dimension">Requested transform dimension; values below 2 are promoted to 2.</param>
    internal IdentityMathTransform(int dimension)
    {
        this.dimension = dimension < 2 ? 2 : dimension;
    }

    /// <inheritdoc/>
    public override int DimSource => this.dimension;

    /// <inheritdoc/>
    public override int DimTarget => this.dimension;

    /// <inheritdoc/>
    public override string WKT => $"PARAM_MT[\"Identity\",PARAMETER[\"dimension\",{this.dimension}]]";

    /// <inheritdoc/>
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool Identity() => true;

    /// <inheritdoc/>
    public override MathTransform Inverse() => this;

    /// <inheritdoc/>
    public override void Invert()
    {
    }

    /// <inheritdoc/>
    public override void Transform(ref double x, ref double y, ref double z)
    {
    }
}
