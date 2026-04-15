// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt;

/// <summary>
/// Creates a <see cref="MathTransform"/> from a Well Known Text (WKT) string.
/// </summary>
public static class MathTransformWktReader
{
    /// <summary>
    /// Reads and parses a WKT-formatted projection string.
    /// </summary>
    /// <param name="wkt">String containing WKT.</param>
    /// <returns>Object representation of the WKT.</returns>
    /// <exception cref="ArgumentException">If a token is not recognised.</exception>
    public static MathTransform Parse(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            ArgumentGuard.ThrowArgument("WKT text must not be empty or whitespace.", nameof(wkt));
        }

        var tokenizer = new WktTokenizer(wkt);
        tokenizer.NextToken();
        string objectName = tokenizer.GetStringValue();
        return objectName switch
        {
            "PARAM_MT" => ReadMathTransform(tokenizer),
            "INVERSE_MT" => ReadInverseMathTransform(tokenizer),
            _ => ThrowWktParseException<MathTransform>($"'{objectName}' is not recognized."),
        };
    }

    /// <summary>
    /// Reads a math transform from the current position of the specified tokenizer.
    /// </summary>
    /// <param name="tokenizer">The tokenizer positioned at or before a <c>PARAM_MT</c> token.</param>
    /// <returns>The parsed <see cref="MathTransform"/>.</returns>
    internal static MathTransform ReadMathTransform(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "PARAM_MT")
        {
            tokenizer.ReadToken("PARAM_MT");
        }

        tokenizer.ReadToken("[");
        string transformName = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");

        return transformName.ToUpperInvariant() switch
        {
            "AFFINE" => ReadAffineTransform(tokenizer),
            "IDENTITY" => ReadIdentityTransform(tokenizer),
            _ => ReadProjectionTransform(tokenizer, transformName),
        };
    }

    /// <summary>
    /// Reads a math transform from a parsed <c>PARAM_MT</c> node.
    /// </summary>
    /// <param name="node">The parsed math-transform node.</param>
    /// <returns>The parsed <see cref="MathTransform"/>.</returns>
    internal static MathTransform ReadMathTransform(WktKeywordNode node)
    {
        ArgumentGuard.ThrowIfNull(node, nameof(node));
        if (!string.Equals(node.Keyword, "PARAM_MT", StringComparison.OrdinalIgnoreCase))
        {
            ArgumentGuard.ThrowArgument($"Expected 'PARAM_MT' but found '{node.Keyword}'.", nameof(node));
        }

        string transformName = node.GetString(0);
        return transformName.ToUpperInvariant() switch
        {
            "AFFINE" => ReadAffineTransform(node),
            "IDENTITY" => ReadIdentityTransform(node),
            _ => ReadProjectionTransform(node, transformName),
        };
    }

    /// <summary>
    /// Reads an inverse math transform from the current position of the specified tokenizer.
    /// </summary>
    /// <param name="tokenizer">The tokenizer positioned at or before an <c>INVERSE_MT</c> token.</param>
    /// <returns>The parsed inverse <see cref="MathTransform"/>.</returns>
    internal static MathTransform ReadInverseMathTransform(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "INVERSE_MT")
        {
            tokenizer.ReadToken("INVERSE_MT");
        }

        tokenizer.ReadToken("[");
        tokenizer.NextToken();

        MathTransform transform = tokenizer.GetStringValue() switch
        {
            "PARAM_MT" => ReadMathTransform(tokenizer),
            "INVERSE_MT" => ReadInverseMathTransform(tokenizer),
            _ => throw new NotSupportedException($"Transform not supported '{tokenizer.GetStringValue()}'"),
        };

        if (tokenizer.GetStringValue() != "]")
        {
            tokenizer.ReadToken("]");
        }

        return transform.Inverse();
    }

    /// <summary>
    /// Reads an inverse math transform from a parsed <c>INVERSE_MT</c> node.
    /// </summary>
    /// <param name="node">The parsed inverse-math-transform node.</param>
    /// <returns>The parsed inverse <see cref="MathTransform"/>.</returns>
    internal static MathTransform ReadInverseMathTransform(WktKeywordNode node)
    {
        ArgumentGuard.ThrowIfNull(node, nameof(node));
        if (!string.Equals(node.Keyword, "INVERSE_MT", StringComparison.OrdinalIgnoreCase))
        {
            ArgumentGuard.ThrowArgument($"Expected 'INVERSE_MT' but found '{node.Keyword}'.", nameof(node));
        }

        foreach (WktNode child in node.Children)
        {
            if (child is not WktKeywordNode keywordChild)
            {
                continue;
            }

            MathTransform transform = keywordChild.Keyword switch
            {
                "PARAM_MT" => ReadMathTransform(keywordChild),
                "INVERSE_MT" => ReadInverseMathTransform(keywordChild),
                _ => throw new NotSupportedException($"Transform not supported '{keywordChild.Keyword}'"),
            };

            return transform.Inverse();
        }

        return ThrowWktParseException<MathTransform>("INVERSE_MT does not contain a nested math transform.");
    }

    private static ParameterInfo ReadParameters(WktTokenizer tokenizer)
    {
        var paramList = new List<Parameter>();
        while (tokenizer.GetStringValue() == "PARAMETER")
        {
            tokenizer.ReadToken("[");
            string paramName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double paramValue = tokenizer.GetNumericValue();
            tokenizer.ReadToken("]");

            // test, whether next parameter is delimited by comma
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() != "]")
            {
                tokenizer.NextToken();
            }

            paramList.Add(new Parameter(paramName, paramValue));
        }

        var info = new ParameterInfo() { Parameters = paramList };
        return info;
    }

    private static ParameterInfo ReadParameters(WktKeywordNode node)
    {
        ArgumentGuard.ThrowIfNull(node, nameof(node));

        var paramList = new List<Parameter>();
        foreach (WktNode child in node.Children)
        {
            if (child is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (!string.Equals(keywordChild.Keyword, "PARAMETER", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            paramList.Add(new Parameter(keywordChild.GetString(0), keywordChild.GetNumber(0)));
        }

        return new ParameterInfo() { Parameters = paramList };
    }

    private static AffineTransform ReadAffineTransform(WktTokenizer tokenizer)
    {
        // PARAM_MT[
        //    "Affine",
        //    PARAMETER["num_row",3],
        //    PARAMETER["num_col",3],
        //    PARAMETER["elt_0_0", 0.883485346527455],
        //    PARAMETER["elt_0_1", -0.468458794848877],
        //    PARAMETER["elt_0_2", 3455869.17937689],
        //    PARAMETER["elt_1_0", 0.468458794848877],
        //    PARAMETER["elt_1_1", 0.883485346527455],
        //    PARAMETER["elt_1_2", 5478710.88035753],
        //    PARAMETER["elt_2_2", 1]
        // ]
        // tokenizer stands on the first PARAMETER
        if (tokenizer.GetStringValue() != "PARAMETER")
        {
            tokenizer.ReadToken("PARAMETER");
        }

        ParameterInfo paramInfo = ReadParameters(tokenizer);

        // manage required parameters - row, col
        Parameter? rowParamCandidate = paramInfo.GetParameterByName("num_row");
        Parameter? colParamCandidate = paramInfo.GetParameterByName("num_col");

        if (rowParamCandidate is null)
        {
            ThrowWktParseException("Affine transform does not contain 'num_row' parameter");
        }

        if (colParamCandidate is null)
        {
            ThrowWktParseException("Affine transform does not contain 'num_col' parameter");
        }

        Parameter rowParam = ArgumentGuard.ThrowIfNull(rowParamCandidate, nameof(rowParamCandidate));
        Parameter colParam = ArgumentGuard.ThrowIfNull(colParamCandidate, nameof(colParamCandidate));
        IList<Parameter>? parametersCandidate = paramInfo.Parameters;
        IList<Parameter> parameters = ArgumentGuard.ThrowIfNull(parametersCandidate, nameof(parametersCandidate));

        int rowVal = (int)rowParam.Value;
        int colVal = (int)colParam.Value;

        if (rowVal <= 0)
        {
            ThrowWktParseException("Affine transform contains invalid value of 'num_row' parameter");
        }

        if (colVal <= 0)
        {
            ThrowWktParseException("Affine transform contains invalid value of 'num_col' parameter");
        }

        // creates working matrix;
        double[,] matrix = new double[rowVal, colVal];

        // simply process matrix values - no elt_ROW_COL parsing
        foreach (Parameter? param in parameters)
        {
            if (param is null || param.Name is null)
            {
                continue;
            }

            switch (param.Name)
            {
                case "num_row":
                case "num_col":
                    break;
                case "elt_0_0":
                    matrix[0, 0] = param.Value;
                    break;
                case "elt_0_1":
                    matrix[0, 1] = param.Value;
                    break;
                case "elt_0_2":
                    matrix[0, 2] = param.Value;
                    break;
                case "elt_0_3":
                    matrix[0, 3] = param.Value;
                    break;
                case "elt_1_0":
                    matrix[1, 0] = param.Value;
                    break;
                case "elt_1_1":
                    matrix[1, 1] = param.Value;
                    break;
                case "elt_1_2":
                    matrix[1, 2] = param.Value;
                    break;
                case "elt_1_3":
                    matrix[1, 3] = param.Value;
                    break;
                case "elt_2_0":
                    matrix[2, 0] = param.Value;
                    break;
                case "elt_2_1":
                    matrix[2, 1] = param.Value;
                    break;
                case "elt_2_2":
                    matrix[2, 2] = param.Value;
                    break;
                case "elt_2_3":
                    matrix[2, 3] = param.Value;
                    break;
                case "elt_3_0":
                    matrix[3, 0] = param.Value;
                    break;
                case "elt_3_1":
                    matrix[3, 1] = param.Value;
                    break;
                case "elt_3_2":
                    matrix[3, 2] = param.Value;
                    break;
                case "elt_3_3":
                    matrix[3, 3] = param.Value;
                    break;
            }
        }

        // read rest of WKT
        if (tokenizer.GetStringValue() != "]")
        {
            tokenizer.ReadToken("]");
        }

        // use "matrix" constructor to create transformation matrix
        var affineTransform = new AffineTransform(matrix);
        return affineTransform;
    }

    private static AffineTransform ReadAffineTransform(WktKeywordNode node)
    {
        ParameterInfo paramInfo = ReadParameters(node);

        Parameter? rowParamCandidate = paramInfo.GetParameterByName("num_row");
        Parameter? colParamCandidate = paramInfo.GetParameterByName("num_col");

        if (rowParamCandidate is null)
        {
            ThrowWktParseException("Affine transform does not contain 'num_row' parameter");
        }

        if (colParamCandidate is null)
        {
            ThrowWktParseException("Affine transform does not contain 'num_col' parameter");
        }

        Parameter rowParam = ArgumentGuard.ThrowIfNull(rowParamCandidate, nameof(rowParamCandidate));
        Parameter colParam = ArgumentGuard.ThrowIfNull(colParamCandidate, nameof(colParamCandidate));
        IList<Parameter>? parametersCandidate = paramInfo.Parameters;
        IList<Parameter> parameters = ArgumentGuard.ThrowIfNull(parametersCandidate, nameof(parametersCandidate));

        int rowVal = (int)rowParam.Value;
        int colVal = (int)colParam.Value;

        if (rowVal <= 0)
        {
            ThrowWktParseException("Affine transform contains invalid value of 'num_row' parameter");
        }

        if (colVal <= 0)
        {
            ThrowWktParseException("Affine transform contains invalid value of 'num_col' parameter");
        }

        double[,] matrix = new double[rowVal, colVal];
        foreach (Parameter? param in parameters)
        {
            if (param is null || param.Name is null)
            {
                continue;
            }

            switch (param.Name)
            {
                case "num_row":
                case "num_col":
                    break;
                case "elt_0_0":
                    matrix[0, 0] = param.Value;
                    break;
                case "elt_0_1":
                    matrix[0, 1] = param.Value;
                    break;
                case "elt_0_2":
                    matrix[0, 2] = param.Value;
                    break;
                case "elt_0_3":
                    matrix[0, 3] = param.Value;
                    break;
                case "elt_1_0":
                    matrix[1, 0] = param.Value;
                    break;
                case "elt_1_1":
                    matrix[1, 1] = param.Value;
                    break;
                case "elt_1_2":
                    matrix[1, 2] = param.Value;
                    break;
                case "elt_1_3":
                    matrix[1, 3] = param.Value;
                    break;
                case "elt_2_0":
                    matrix[2, 0] = param.Value;
                    break;
                case "elt_2_1":
                    matrix[2, 1] = param.Value;
                    break;
                case "elt_2_2":
                    matrix[2, 2] = param.Value;
                    break;
                case "elt_2_3":
                    matrix[2, 3] = param.Value;
                    break;
                case "elt_3_0":
                    matrix[3, 0] = param.Value;
                    break;
                case "elt_3_1":
                    matrix[3, 1] = param.Value;
                    break;
                case "elt_3_2":
                    matrix[3, 2] = param.Value;
                    break;
                case "elt_3_3":
                    matrix[3, 3] = param.Value;
                    break;
            }
        }

        return new AffineTransform(matrix);
    }

    private static IdentityMathTransform ReadIdentityTransform(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "PARAMETER")
        {
            tokenizer.ReadToken("PARAMETER");
        }

        ParameterInfo paramInfo = ReadParameters(tokenizer);
        Parameter? dimensionParam = paramInfo.GetParameterByName("dimension");
        if (dimensionParam is null)
        {
            ThrowWktParseException("Identity transform does not contain 'dimension' parameter");
        }

        int dimension = (int)dimensionParam.Value;
        if (dimension <= 0)
        {
            ThrowWktParseException("Identity transform contains invalid value of 'dimension' parameter");
        }

        if (tokenizer.GetStringValue() != "]")
        {
            tokenizer.ReadToken("]");
        }

        return new IdentityMathTransform(dimension);
    }

    private static IdentityMathTransform ReadIdentityTransform(WktKeywordNode node)
    {
        ParameterInfo paramInfo = ReadParameters(node);
        Parameter? dimensionParam = paramInfo.GetParameterByName("dimension");
        if (dimensionParam is null)
        {
            ThrowWktParseException("Identity transform does not contain 'dimension' parameter");
        }

        int dimension = (int)dimensionParam.Value;
        if (dimension <= 0)
        {
            ThrowWktParseException("Identity transform contains invalid value of 'dimension' parameter");
        }

        return new IdentityMathTransform(dimension);
    }

    private static MathTransform ReadProjectionTransform(WktTokenizer tokenizer, string transformName)
    {
        if (tokenizer.GetStringValue() != "PARAMETER")
        {
            tokenizer.ReadToken("PARAMETER");
        }

        ParameterInfo paramInfo = ReadParameters(tokenizer);
        IList<Parameter>? parametersCandidate = paramInfo.Parameters;
        IList<Parameter> parameters = ArgumentGuard.ThrowIfNull(parametersCandidate, nameof(parametersCandidate));
        var projectionParameters = new List<ProjectionParameter>(parameters.Count);

        foreach (Parameter? parameter in parameters)
        {
            if (parameter is null || parameter.Name is null)
            {
                continue;
            }

            projectionParameters.Add(new ProjectionParameter(parameter.Name, parameter.Value));
        }

        if (tokenizer.GetStringValue() != "]")
        {
            tokenizer.ReadToken("]");
        }

        return ProjectionsRegistry.CreateProjection(transformName, projectionParameters);
    }

    private static MathTransform ReadProjectionTransform(WktKeywordNode node, string transformName)
    {
        ParameterInfo paramInfo = ReadParameters(node);
        IList<Parameter>? parametersCandidate = paramInfo.Parameters;
        IList<Parameter> parameters = ArgumentGuard.ThrowIfNull(parametersCandidate, nameof(parametersCandidate));
        var projectionParameters = new List<ProjectionParameter>(parameters.Count);

        foreach (Parameter? parameter in parameters)
        {
            if (parameter is null || parameter.Name is null)
            {
                continue;
            }

            projectionParameters.Add(new ProjectionParameter(parameter.Name, parameter.Value));
        }

        return ProjectionsRegistry.CreateProjection(transformName, projectionParameters);
    }

    [DoesNotReturn]
    private static void ThrowWktParseException(string message)
    {
        throw new WktParseException(message);
    }

    [DoesNotReturn]
    private static T ThrowWktParseException<T>(string message)
    {
        throw new WktParseException(message);
    }
}
