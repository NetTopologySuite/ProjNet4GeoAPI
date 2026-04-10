// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Shared helpers for affine derived coordinate-system serialization and parsing.
/// </summary>
internal static class DerivedCoordinateSystemSupport
{
    internal const string AffineParametricTransformationMethodName = "Affine parametric transformation";
    internal const string DefaultDerivingConversionName = "unnamed";
    private const double AffineMatrixTolerance = 1e-12;

    internal static Projection CreateAffineConversion(MathTransform transform, string conversionName)
    {
        if (!TryGetAffineParameters(transform, out DerivedAffineParameters parameters))
        {
            throw new NotSupportedException("Derived coordinate-system support currently requires a two-dimensional affine transform with a standard homogeneous 3x3 matrix.");
        }

        string name = string.IsNullOrWhiteSpace(conversionName) ? DefaultDerivingConversionName : conversionName;
        return new Projection(
            AffineParametricTransformationMethodName,
            new List<ProjectionParameter>
            {
                new("A0", parameters.A0),
                new("A1", parameters.A1),
                new("A2", parameters.A2),
                new("B0", parameters.B0),
                new("B1", parameters.B1),
                new("B2", parameters.B2),
            },
            name,
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    internal static AffineTransform CreateAffineTransform(IProjection conversion)
    {
        ArgumentGuard.ThrowIfNull(conversion, nameof(conversion));
        if (!string.Equals(conversion.ClassName, AffineParametricTransformationMethodName, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Derived coordinate-system support currently recognizes only '{AffineParametricTransformationMethodName}' deriving conversions.");
        }

        double a0 = GetRequiredParameter(conversion, "A0");
        double a1 = GetRequiredParameter(conversion, "A1");
        double a2 = GetRequiredParameter(conversion, "A2");
        double b0 = GetRequiredParameter(conversion, "B0");
        double b1 = GetRequiredParameter(conversion, "B1");
        double b2 = GetRequiredParameter(conversion, "B2");

        return new AffineTransform(a1, a2, a0, b1, b2, b0);
    }

    private static bool TryGetAffineParameters(MathTransform transform, out DerivedAffineParameters parameters)
    {
        parameters = default;
        if (transform is not AffineTransform affineTransform)
        {
            return false;
        }

        double[,] matrix = affineTransform.GetMatrix();
        if (matrix.GetLength(0) != 3
            || matrix.GetLength(1) != 3
            || !ApproximatelyZero(matrix[2, 0])
            || !ApproximatelyZero(matrix[2, 1])
            || !ApproximatelyEqual(matrix[2, 2], 1d))
        {
            return false;
        }

        parameters = new DerivedAffineParameters(
            matrix[0, 2],
            matrix[0, 0],
            matrix[0, 1],
            matrix[1, 2],
            matrix[1, 0],
            matrix[1, 1]);
        return true;
    }

    private static double GetRequiredParameter(IProjection conversion, string name)
    {
        ProjectionParameter? parameter = conversion.GetParameter(name);
        if (parameter is null)
        {
            throw new NotSupportedException($"Derived affine conversion is missing the required '{name}' parameter.");
        }

        return parameter.Value;
    }

    private static bool ApproximatelyZero(double value) => Math.Abs(value) <= AffineMatrixTolerance;

    private static bool ApproximatelyEqual(double left, double right) => Math.Abs(left - right) <= AffineMatrixTolerance;
}

/// <summary>
/// Captures the 2D affine coefficients used by WKT2/PROJJSON derived conversions.
/// </summary>
/// <param name="A0">Translation term for the first target axis.</param>
/// <param name="A1">Source X scale/shear term for the first target axis.</param>
/// <param name="A2">Source Y scale/shear term for the first target axis.</param>
/// <param name="B0">Translation term for the second target axis.</param>
/// <param name="B1">Source X scale/shear term for the second target axis.</param>
/// <param name="B2">Source Y scale/shear term for the second target axis.</param>
internal readonly record struct DerivedAffineParameters(double A0, double A1, double A2, double B0, double B1, double B2);
