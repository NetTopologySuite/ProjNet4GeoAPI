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
using ProjNet.CoordinateSystems;

/// <summary>
/// Creates axis-order correction transforms between source and target coordinate systems.
/// </summary>
internal static class AxisOrderHelper
{
    /// <summary>
    /// Tries to create an axis-swap transform that aligns source and target axis orientation.
    /// </summary>
    /// <param name="source">Source coordinate system.</param>
    /// <param name="target">Target coordinate system.</param>
    /// <param name="transform">Created transform when alignment is possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreateAxisSwapTransform(
        CoordinateSystem source,
        CoordinateSystem target,
        out MathTransform transform)
    {
        transform = null;
        if (source is null || target is null)
        {
            return false;
        }

        int dimension = Math.Min(3, Math.Min(source.Dimension, target.Dimension));
        if (dimension < 2)
        {
            return false;
        }

        if (!TryGetRoleByOrientation(source, dimension, out int[] sourceRoles)
            || !TryGetRoleByOrientation(target, dimension, out int[] targetRoles))
        {
            return false;
        }

        int[] sourceIndexByRole = { -1, -1, -1 };
        int[] sourceSignByRole = { 1, 1, 1 };
        int[] targetIndexByRole = { -1, -1, -1 };
        int[] targetSignByRole = { 1, 1, 1 };

        for (int i = 0; i < dimension; i++)
        {
            AxisOrientationEnum sourceOrientation = source.GetAxis(i).Orientation;
            if (!TryMapOrientation(sourceOrientation, out int sourceRole, out int sourceSign))
            {
                return false;
            }

            AxisOrientationEnum targetOrientation = target.GetAxis(i).Orientation;
            if (!TryMapOrientation(targetOrientation, out int targetRole, out int targetSign))
            {
                return false;
            }

            sourceIndexByRole[sourceRole] = i;
            sourceSignByRole[sourceRole] = sourceSign;
            targetIndexByRole[targetRole] = i;
            targetSignByRole[targetRole] = targetSign;
        }

        int[] sourceIndices = { 0, 1, 2 };
        int[] signs = { 1, 1, 1 };
        bool changed = false;

        for (int role = 0; role < 3; role++)
        {
            int targetIndex = targetIndexByRole[role];
            if (targetIndex < 0 || targetIndex > 2)
            {
                continue;
            }

            int sourceIndex = sourceIndexByRole[role];
            if (sourceIndex < 0 || sourceIndex > 2)
            {
                return false;
            }

            sourceIndices[targetIndex] = sourceIndex;
            signs[targetIndex] = sourceSignByRole[role] * targetSignByRole[role];
            if (sourceIndices[targetIndex] != targetIndex || signs[targetIndex] != 1)
            {
                changed = true;
            }
        }

        if (!changed)
        {
            transform = new IdentityMathTransform(Math.Max(source.Dimension, target.Dimension));
            return true;
        }

        transform = new AxisSwapMathTransform(
            dimension,
            sourceIndices[0],
            signs[0],
            sourceIndices[1],
            signs[1],
            sourceIndices[2],
            signs[2]);
        return true;
    }

    private static bool TryGetRoleByOrientation(CoordinateSystem coordinateSystem, int dimension, out int[] roleByAxis)
    {
        roleByAxis = new int[Math.Min(3, dimension)];
        for (int i = 0; i < roleByAxis.Length; i++)
        {
            if (!TryMapOrientation(coordinateSystem.GetAxis(i).Orientation, out int role, out _))
            {
                return false;
            }

            roleByAxis[i] = role;
        }

        return true;
    }

    private static bool TryMapOrientation(AxisOrientationEnum orientation, out int role, out int sign)
    {
        role = -1;
        sign = 1;
        switch (orientation)
        {
            case AxisOrientationEnum.East:
                role = 0;
                sign = 1;
                return true;
            case AxisOrientationEnum.West:
                role = 0;
                sign = -1;
                return true;
            case AxisOrientationEnum.North:
                role = 1;
                sign = 1;
                return true;
            case AxisOrientationEnum.South:
                role = 1;
                sign = -1;
                return true;
            case AxisOrientationEnum.Up:
                role = 2;
                sign = 1;
                return true;
            case AxisOrientationEnum.Down:
                role = 2;
                sign = -1;
                return true;
            default:
                return false;
        }
    }
}
