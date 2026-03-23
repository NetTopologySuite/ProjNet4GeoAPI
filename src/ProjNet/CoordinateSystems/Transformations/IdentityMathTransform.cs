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
