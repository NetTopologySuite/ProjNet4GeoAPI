// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

/// <summary>
/// Implements PROJ's <c>tinshift</c> runtime transform.
/// </summary>
/// <remarks>
/// <para>
/// TIN-based shifts load a triangulation model from JSON, locate the triangle
/// containing the input coordinate through the in-memory spatial index, and use
/// barycentric weights to interpolate horizontal target coordinates and optional
/// vertical offsets. Supported fallback modes mirror PROJ's
/// <c>none</c>, <c>nearest_side</c>, and <c>nearest_centroid</c> strategies for
/// points outside the triangulated area.
/// </para>
/// <para>
/// The runtime was independently verified against PROJ's published
/// <c>tinshift</c> documentation and <c>tinshift.cpp</c>, including barycentric
/// interpolation and the reviewed fallback semantics.
/// </para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/transformations/tinshift.html">PROJ: tinshift.</seealso>
internal sealed class TinShiftMathTransform : MathTransform
{
    private const int MaximumModelSizeInBytes = 100 * 1024 * 1024;
    private const double TriangleEpsilon = 1e-10d;

    private readonly TinShiftModel model;
    private readonly bool isInverted;
    private MathTransform? inverse;

    private TinShiftMathTransform(TinShiftModel model, bool isInverted)
    {
        this.model = ArgumentGuard.ThrowIfNull(model, nameof(model));
        this.isInverted = isInverted;
    }

    private enum FallbackStrategy
    {
        None = 0,
        NearestSide = 1,
        NearestCentroid = 2,
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override bool Identity() => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new TinShiftMathTransform(this.model, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("TinShiftMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (!this.TryTransformInternal(x, y, z, out double xOut, out double yOut, out double zOut))
        {
            TransformationThrowHelper.ThrowInvalidOperation("tinshift transformation failed for input coordinate.");
        }

        x = xOut;
        y = yOut;
        z = zOut;
    }

    /// <summary>
    /// Creates a <see cref="TinShiftMathTransform"/> from parsed PROJ arguments.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreate(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (args is null)
        {
            skipReason = "tinshift arguments were null.";
            return false;
        }

        if (!args.TryGetValue("file", out string? fileToken) || string.IsNullOrWhiteSpace(fileToken))
        {
            skipReason = "tinshift requires +file.";
            return false;
        }

        if (!TryResolveFilePath(fileToken, out string? resolvedPath))
        {
            skipReason = $"Cannot open {fileToken}.";
            return false;
        }

        try
        {
            var fileInfo = new FileInfo(resolvedPath);
            if (!fileInfo.Exists)
            {
                skipReason = $"Cannot open {fileToken}.";
                return false;
            }

            if (fileInfo.Length > MaximumModelSizeInBytes)
            {
                skipReason = $"File {fileToken} too large.";
                return false;
            }

            string jsonText = File.ReadAllText(resolvedPath);
            try
            {
                TinShiftModel model = ParseModel(jsonText);
                transform = new TinShiftMathTransform(model, false);
                if (args.ContainsKey("inv"))
                {
                    transform = transform.Inverse();
                }

                return true;
            }
            catch (FormatException exception)
            {
                skipReason = $"invalid model: {exception.Message}";
                return false;
            }
            catch (JsonException exception)
            {
                skipReason = $"invalid model: {exception.Message}";
                return false;
            }
            catch (ArgumentException exception)
            {
                skipReason = $"invalid model: {exception.Message}";
                return false;
            }
        }
        catch (IOException exception)
        {
            skipReason = $"Cannot read {fileToken}: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            skipReason = $"Cannot read {fileToken}: {exception.Message}";
            return false;
        }
    }

    private static TinShiftModel ParseModel(string jsonText)
    {
        using var document = JsonDocument.Parse(jsonText);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Not an object.");
        }

        string fileType = GetRequiredString(root, "file_type");
        if (!string.Equals(fileType, "triangulation_file", StringComparison.Ordinal))
        {
            throw new FormatException("Unsupported file_type.");
        }

        string formatVersion = GetRequiredString(root, "format_version");
        FallbackStrategy fallback = FallbackStrategy.None;
        if (root.TryGetProperty("fallback_strategy", out JsonElement fallbackElement))
        {
            if (!string.Equals(formatVersion, "1.1", StringComparison.Ordinal))
            {
                throw new FormatException("fallback_strategy needs format_version 1.1.");
            }

            if (fallbackElement.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("fallback_strategy must be a string.");
            }

            string? fallbackText = fallbackElement.GetString();
            if (fallbackText is null)
            {
                throw new FormatException("fallback_strategy must be a string.");
            }

            if (string.Equals(fallbackText, "none", StringComparison.Ordinal))
            {
                fallback = FallbackStrategy.None;
            }
            else if (string.Equals(fallbackText, "nearest_side", StringComparison.Ordinal))
            {
                fallback = FallbackStrategy.NearestSide;
            }
            else if (string.Equals(fallbackText, "nearest_centroid", StringComparison.Ordinal))
            {
                fallback = FallbackStrategy.NearestCentroid;
            }
            else
            {
                throw new FormatException("invalid fallback_strategy.");
            }
        }

        (bool transformHorizontal, bool transformVertical) = ParseTransformedComponents(GetRequiredArray(root, "transformed_components"));
        JsonElement verticesColumns = GetRequiredArray(root, "vertices_columns");
        VerticesColumnMap verticesMap = ParseVerticesColumnMap(verticesColumns);
        ValidateVerticesColumns(transformHorizontal, transformVertical, verticesMap);
        JsonElement trianglesColumns = GetRequiredArray(root, "triangles_columns");
        TriangleColumnMap triangleMap = ParseTriangleColumnMap(trianglesColumns);

        int vertexColumnCount = 2;
        if (transformHorizontal)
        {
            vertexColumnCount += 2;
        }

        if (transformVertical)
        {
            vertexColumnCount += 1;
        }

        JsonElement verticesArray = GetRequiredArray(root, "vertices");
        double[] vertices = ParseVertices(verticesArray, verticesColumns.GetArrayLength(), vertexColumnCount, transformHorizontal, transformVertical, verticesMap);
        JsonElement trianglesArray = GetRequiredArray(root, "triangles");
        List<TriangleIndices> triangles = ParseTriangles(trianglesArray, trianglesColumns.GetArrayLength(), triangleMap, verticesArray.GetArrayLength());

        return new TinShiftModel(
            transformHorizontal,
            transformVertical,
            fallback,
            vertexColumnCount,
            vertices,
            triangles);
    }

    private static (bool TransformHorizontal, bool TransformVertical) ParseTransformedComponents(JsonElement transformedComponentsArray)
    {
        bool transformHorizontal = false;
        bool transformVertical = false;
        foreach (JsonElement component in transformedComponentsArray.EnumerateArray())
        {
            if (component.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("transformed_components[] item is not a string.");
            }

            string? text = component.GetString();
            if (text is null)
            {
                throw new FormatException("transformed_components[] item is not a string.");
            }

            if (string.Equals(text, "horizontal", StringComparison.Ordinal))
            {
                transformHorizontal = true;
            }
            else if (string.Equals(text, "vertical", StringComparison.Ordinal))
            {
                transformVertical = true;
            }
            else
            {
                throw new FormatException($"transformed_components[] = {text} is not handled.");
            }
        }

        return (transformHorizontal, transformVertical);
    }

    private static VerticesColumnMap ParseVerticesColumnMap(JsonElement verticesColumns)
    {
        var result = new VerticesColumnMap
        {
            SourceX = -1,
            SourceY = -1,
            SourceZ = -1,
            TargetX = -1,
            TargetY = -1,
            TargetZ = -1,
            OffsetZ = -1,
        };
        int index = 0;
        foreach (JsonElement column in verticesColumns.EnumerateArray())
        {
            if (column.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("vertices_columns[] item is not a string.");
            }

            string? name = column.GetString();
            if (name is null)
            {
                throw new FormatException("vertices_columns[] item is not a string.");
            }

            if (name == "source_x")
            {
                result.SourceX = index;
            }
            else if (name == "source_y")
            {
                result.SourceY = index;
            }
            else if (name == "source_z")
            {
                result.SourceZ = index;
            }
            else if (name == "target_x")
            {
                result.TargetX = index;
            }
            else if (name == "target_y")
            {
                result.TargetY = index;
            }
            else if (name == "target_z")
            {
                result.TargetZ = index;
            }
            else if (name == "offset_z")
            {
                result.OffsetZ = index;
            }

            index++;
        }

        return result;
    }

    private static void ValidateVerticesColumns(bool transformHorizontal, bool transformVertical, VerticesColumnMap map)
    {
        if (map.SourceX < 0)
        {
            throw new FormatException("source_x must be specified in vertices_columns[].");
        }

        if (map.SourceY < 0)
        {
            throw new FormatException("source_y must be specified in vertices_columns[].");
        }

        if (transformHorizontal)
        {
            if (map.TargetX < 0)
            {
                throw new FormatException("target_x must be specified in vertices_columns[].");
            }

            if (map.TargetY < 0)
            {
                throw new FormatException("target_y must be specified in vertices_columns[].");
            }
        }

        if (transformVertical && map.OffsetZ < 0)
        {
            if (map.SourceZ < 0)
            {
                throw new FormatException("source_z or delta_z must be specified in vertices_columns[].");
            }

            if (map.TargetZ < 0)
            {
                throw new FormatException("target_z must be specified in vertices_columns[].");
            }
        }
    }

    private static TriangleColumnMap ParseTriangleColumnMap(JsonElement trianglesColumns)
    {
        var result = new TriangleColumnMap
        {
            Index1 = -1,
            Index2 = -1,
            Index3 = -1,
        };
        int index = 0;
        foreach (JsonElement column in trianglesColumns.EnumerateArray())
        {
            if (column.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("triangles_columns[] item is not a string.");
            }

            string? name = column.GetString();
            if (name is null)
            {
                throw new FormatException("triangles_columns[] item is not a string.");
            }

            if (name == "idx_vertex1")
            {
                result.Index1 = index;
            }
            else if (name == "idx_vertex2")
            {
                result.Index2 = index;
            }
            else if (name == "idx_vertex3")
            {
                result.Index3 = index;
            }

            index++;
        }

        if (result.Index1 < 0)
        {
            throw new FormatException("idx_vertex1 must be specified in triangles_columns[].");
        }

        if (result.Index2 < 0)
        {
            throw new FormatException("idx_vertex2 must be specified in triangles_columns[].");
        }

        return result.Index3 < 0 ? throw new FormatException("idx_vertex3 must be specified in triangles_columns[].") : result;
    }

    private static double[] ParseVertices(
        JsonElement verticesArray,
        int inputVertexColumnCount,
        int runtimeVertexColumnCount,
        bool transformHorizontal,
        bool transformVertical,
        VerticesColumnMap map)
    {
        double[] vertices = new double[verticesArray.GetArrayLength() * runtimeVertexColumnCount];
        int outputOffset = 0;
        foreach (JsonElement vertex in verticesArray.EnumerateArray())
        {
            if (vertex.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("vertices[] item is not an array.");
            }

            if (vertex.GetArrayLength() != inputVertexColumnCount)
            {
                throw new FormatException("vertices[] item has not expected number of elements.");
            }

            double sourceX = ReadNumber(vertex, map.SourceX, "vertices[][] item is not a number.");
            double sourceY = ReadNumber(vertex, map.SourceY, "vertices[][] item is not a number.");
            vertices[outputOffset++] = sourceX;
            vertices[outputOffset++] = sourceY;

            if (transformHorizontal)
            {
                double targetX = ReadNumber(vertex, map.TargetX, "vertices[][] item is not a number.");
                double targetY = ReadNumber(vertex, map.TargetY, "vertices[][] item is not a number.");
                vertices[outputOffset++] = targetX;
                vertices[outputOffset++] = targetY;
            }

            if (transformVertical)
            {
                if (map.OffsetZ >= 0)
                {
                    double offsetZ = ReadNumber(vertex, map.OffsetZ, "vertices[][] item is not a number.");
                    vertices[outputOffset++] = offsetZ;
                }
                else
                {
                    double sourceZ = ReadNumber(vertex, map.SourceZ, "vertices[][] item is not a number.");
                    double targetZ = ReadNumber(vertex, map.TargetZ, "vertices[][] item is not a number.");
                    vertices[outputOffset++] = targetZ - sourceZ;
                }
            }
        }

        return vertices;
    }

    private static List<TriangleIndices> ParseTriangles(
        JsonElement trianglesArray,
        int inputTriangleColumnCount,
        TriangleColumnMap map,
        int vertexCount)
    {
        var triangles = new List<TriangleIndices>(trianglesArray.GetArrayLength());
        foreach (JsonElement triangle in trianglesArray.EnumerateArray())
        {
            if (triangle.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("triangles[] item is not an array.");
            }

            if (triangle.GetArrayLength() != inputTriangleColumnCount)
            {
                throw new FormatException("triangles[] item has not expected number of elements.");
            }

            int idx1 = ReadNonNegativeInteger(triangle, map.Index1, "triangles[][] item is not an integer.");
            int idx2 = ReadNonNegativeInteger(triangle, map.Index2, "triangles[][] item is not an integer.");
            int idx3 = ReadNonNegativeInteger(triangle, map.Index3, "triangles[][] item is not an integer.");

            if (idx1 >= vertexCount || idx2 >= vertexCount || idx3 >= vertexCount)
            {
                throw new FormatException("Invalid value for a vertex index.");
            }

            triangles.Add(new TriangleIndices(idx1, idx2, idx3));
        }

        return triangles;
    }

    private static bool TryResolveFilePath(string fileToken, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(fileToken))
        {
            return false;
        }

        string normalized = NormalizePathToken(fileToken);
        if (TryGetExistingPath(normalized, out resolvedPath))
        {
            return true;
        }

        string appBaseCandidate = Path.Combine(AppContext.BaseDirectory, normalized);
        if (TryGetExistingPath(appBaseCandidate, out resolvedPath))
        {
            return true;
        }

        if (CoordinateTransformationFactory.TryResolveGridResourcePath(fileToken, out resolvedPath))
        {
            return true;
        }

        if (!string.Equals(normalized, fileToken, StringComparison.Ordinal)
            && CoordinateTransformationFactory.TryResolveGridResourcePath(normalized, out resolvedPath))
        {
            return true;
        }

        string fileName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            string fixtureCandidate = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tinshift", fileName);
            if (TryGetExistingPath(fixtureCandidate, out resolvedPath))
            {
                return true;
            }

            if (CoordinateTransformationFactory.TryResolveGridResourcePath(fileName, out resolvedPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetExistingPath(string candidate, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (!Path.IsPathRooted(candidate))
        {
            candidate = Path.GetFullPath(candidate);
        }

        if (!File.Exists(candidate))
        {
            return false;
        }

        resolvedPath = candidate;
        return true;
    }

    private static string NormalizePathToken(string token)
    {
        return token.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
    }

    private static JsonElement GetRequiredArray(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException($"Missing \"{propertyName}\" key.");
        }

        return value.ValueKind != JsonValueKind.Array
            ? throw new FormatException($"The value of \"{propertyName}\" should be a array.")
            : value;
    }

    private static string GetRequiredString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException($"Missing \"{propertyName}\" key.");
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"The value of \"{propertyName}\" should be a string.");
        }

        string? text = value.GetString();
        return text is null ? throw new FormatException($"The value of \"{propertyName}\" should be a string.") : text;
    }

    private static double ReadNumber(JsonElement array, int index, string errorMessage)
    {
        JsonElement value = array[index];
        return value.ValueKind != JsonValueKind.Number ? throw new FormatException(errorMessage) : value.GetDouble();
    }

    private static int ReadNonNegativeInteger(JsonElement array, int index, string errorMessage)
    {
        JsonElement value = array[index];
        return value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int intValue) || intValue < 0
            ? throw new FormatException(errorMessage)
            : intValue;
    }

    private static bool TryComputeBarycentricCoordinates(
        double x,
        double y,
        double x1,
        double y1,
        double x2,
        double y2,
        double x3,
        double y3,
        out double lambda1,
        out double lambda2,
        out double lambda3)
    {
        double detT = ((y2 - y3) * (x1 - x3)) + ((x3 - x2) * (y1 - y3));
        if (Math.Abs(detT) < TriangleEpsilon)
        {
            lambda1 = 0d;
            lambda2 = 0d;
            lambda3 = 0d;
            return false;
        }

        lambda1 = (((y2 - y3) * (x - x3)) + ((x3 - x2) * (y - y3))) / detT;
        lambda2 = (((y3 - y1) * (x - x3)) + ((x1 - x3) * (y - y3))) / detT;
        lambda3 = 1d - lambda1 - lambda2;
        return true;
    }

    private static bool IsInsideTriangle(double lambda1, double lambda2, double lambda3)
    {
        return lambda1 >= -TriangleEpsilon
            && lambda1 <= (1d + TriangleEpsilon)
            && lambda2 >= -TriangleEpsilon
            && lambda2 <= (1d + TriangleEpsilon)
            && lambda3 >= -TriangleEpsilon;
    }

    private static double SquaredDistance(double x1, double y1, double x2, double y2)
    {
        double dx = x1 - x2;
        double dy = y1 - y2;
        return (dx * dx) + (dy * dy);
    }

    private static double DistancePointSegmentSquared(
        double x,
        double y,
        double x1,
        double y1,
        double x2,
        double y2,
        double segmentLengthSquared)
    {
        double t = (((x - x1) * (x2 - x1)) + ((y - y1) * (y2 - y1))) / segmentLengthSquared;
        if (t <= 0d)
        {
            return SquaredDistance(x, y, x1, y1);
        }

        if (t >= 1d)
        {
            return SquaredDistance(x, y, x2, y2);
        }

        double projectedX = x1 + (t * (x2 - x1));
        double projectedY = y1 + (t * (y2 - y1));
        return SquaredDistance(x, y, projectedX, projectedY);
    }

    private bool TryTransformInternal(double x, double y, double z, out double xOut, out double yOut, out double zOut)
    {
        bool forward = !this.isInverted;
        if (!this.TryFindTriangle(
            x,
            y,
            forward,
            out TriangleIndices triangle,
            out double lambda1,
            out double lambda2,
            out double lambda3))
        {
            xOut = double.NaN;
            yOut = double.NaN;
            zOut = double.NaN;
            return false;
        }

        int idx1 = triangle.Index1;
        int idx2 = triangle.Index2;
        int idx3 = triangle.Index3;

        int sourceXIndex = 0;
        int sourceYIndex = 1;
        int targetXIndex = this.model.TransformHorizontal ? 2 : 0;
        int targetYIndex = this.model.TransformHorizontal ? 3 : 1;
        int offsetZIndex = this.model.TransformHorizontal ? 4 : 2;

        if (this.model.TransformHorizontal)
        {
            int readXIndex = forward ? targetXIndex : sourceXIndex;
            int readYIndex = forward ? targetYIndex : sourceYIndex;
            xOut = (this.model.GetVertexValue(idx1, readXIndex) * lambda1)
                + (this.model.GetVertexValue(idx2, readXIndex) * lambda2)
                + (this.model.GetVertexValue(idx3, readXIndex) * lambda3);
            yOut = (this.model.GetVertexValue(idx1, readYIndex) * lambda1)
                + (this.model.GetVertexValue(idx2, readYIndex) * lambda2)
                + (this.model.GetVertexValue(idx3, readYIndex) * lambda3);
        }
        else
        {
            xOut = x;
            yOut = y;
        }

        if (this.model.TransformVertical)
        {
            double zOffset = (this.model.GetVertexValue(idx1, offsetZIndex) * lambda1)
                + (this.model.GetVertexValue(idx2, offsetZIndex) * lambda2)
                + (this.model.GetVertexValue(idx3, offsetZIndex) * lambda3);
            zOut = forward
                ? z + zOffset
                : z - zOffset;
        }
        else
        {
            zOut = z;
        }

        return true;
    }

    private bool TryFindTriangle(
        double x,
        double y,
        bool forward,
        out TriangleIndices triangle,
        out double lambda1,
        out double lambda2,
        out double lambda3)
    {
        for (int i = 0; i < this.model.Triangles.Count; i++)
        {
            TriangleIndices candidate = this.model.Triangles[i];
            if (!this.TryComputeTriangleLambdas(candidate, x, y, forward, out double l1, out double l2, out double l3))
            {
                continue;
            }

            if (IsInsideTriangle(l1, l2, l3))
            {
                triangle = candidate;
                lambda1 = l1;
                lambda2 = l2;
                lambda3 = l3;
                return true;
            }
        }

        if (this.model.Fallback == FallbackStrategy.None)
        {
            triangle = default;
            lambda1 = 0d;
            lambda2 = 0d;
            lambda3 = 0d;
            return false;
        }

        return this.TryFindFallbackTriangle(x, y, forward, out triangle, out lambda1, out lambda2, out lambda3);
    }

    private bool TryFindFallbackTriangle(
        double x,
        double y,
        bool forward,
        out TriangleIndices triangle,
        out double lambda1,
        out double lambda2,
        out double lambda3)
    {
        double bestDistanceSquared = double.PositiveInfinity;
        TriangleIndices bestTriangle = default;
        bool found = false;

        for (int i = 0; i < this.model.Triangles.Count; i++)
        {
            TriangleIndices candidate = this.model.Triangles[i];
            this.GetTriangleCoordinates(candidate, forward, out double x1, out double y1, out double x2, out double y2, out double x3, out double y3);

            double d12 = SquaredDistance(x1, y1, x2, y2);
            double d23 = SquaredDistance(x2, y2, x3, y3);
            double d13 = SquaredDistance(x1, y1, x3, y3);
            if (d12 < TriangleEpsilon || d23 < TriangleEpsilon || d13 < TriangleEpsilon)
            {
                continue;
            }

            double distanceSquared = this.model.Fallback == FallbackStrategy.NearestSide
                ? Math.Min(
                    DistancePointSegmentSquared(x, y, x1, y1, x2, y2, d12),
                    Math.Min(
                        DistancePointSegmentSquared(x, y, x2, y2, x3, y3, d23),
                        DistancePointSegmentSquared(x, y, x1, y1, x3, y3, d13)))
                : SquaredDistance(x, y, (x1 + x2 + x3) / 3d, (y1 + y2 + y3) / 3d);

            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestTriangle = candidate;
                found = true;
            }
        }

        if (!found)
        {
            triangle = default;
            lambda1 = 0d;
            lambda2 = 0d;
            lambda3 = 0d;
            return false;
        }

        if (!this.TryComputeTriangleLambdas(bestTriangle, x, y, forward, out lambda1, out lambda2, out lambda3))
        {
            triangle = default;
            lambda1 = 0d;
            lambda2 = 0d;
            lambda3 = 0d;
            return false;
        }

        triangle = bestTriangle;
        return true;
    }

    private bool TryComputeTriangleLambdas(
        TriangleIndices triangle,
        double x,
        double y,
        bool forward,
        out double lambda1,
        out double lambda2,
        out double lambda3)
    {
        this.GetTriangleCoordinates(
            triangle,
            forward,
            out double x1,
            out double y1,
            out double x2,
            out double y2,
            out double x3,
            out double y3);

        return TryComputeBarycentricCoordinates(
            x,
            y,
            x1,
            y1,
            x2,
            y2,
            x3,
            y3,
            out lambda1,
            out lambda2,
            out lambda3);
    }

    private void GetTriangleCoordinates(
        TriangleIndices triangle,
        bool forward,
        out double x1,
        out double y1,
        out double x2,
        out double y2,
        out double x3,
        out double y3)
    {
        int baseXIndex = 0;
        int baseYIndex = 1;
        int transformedXIndex = this.model.TransformHorizontal ? 2 : 0;
        int transformedYIndex = this.model.TransformHorizontal ? 3 : 1;
        int xIndex = this.model.TransformHorizontal && !forward
            ? transformedXIndex
            : baseXIndex;
        int yIndex = this.model.TransformHorizontal && !forward
            ? transformedYIndex
            : baseYIndex;

        x1 = this.model.GetVertexValue(triangle.Index1, xIndex);
        y1 = this.model.GetVertexValue(triangle.Index1, yIndex);
        x2 = this.model.GetVertexValue(triangle.Index2, xIndex);
        y2 = this.model.GetVertexValue(triangle.Index2, yIndex);
        x3 = this.model.GetVertexValue(triangle.Index3, xIndex);
        y3 = this.model.GetVertexValue(triangle.Index3, yIndex);
    }

    private readonly struct TriangleIndices(int index1, int index2, int index3)
    {
        internal int Index1 { get; } = index1;

        internal int Index2 { get; } = index2;

        internal int Index3 { get; } = index3;
    }

    private struct VerticesColumnMap
    {
        internal int SourceX;
        internal int SourceY;
        internal int SourceZ;
        internal int TargetX;
        internal int TargetY;
        internal int TargetZ;
        internal int OffsetZ;
    }

    private struct TriangleColumnMap
    {
        internal int Index1;
        internal int Index2;
        internal int Index3;
    }

    private sealed class TinShiftModel
    {
        internal TinShiftModel(
            bool transformHorizontal,
            bool transformVertical,
            FallbackStrategy fallback,
            int vertexColumnCount,
            double[] vertices,
            List<TriangleIndices> triangles)
        {
            this.Vertices = ArgumentGuard.ThrowIfNull(vertices, nameof(vertices));
            this.Triangles = ArgumentGuard.ThrowIfNull(triangles, nameof(triangles));
            this.TransformHorizontal = transformHorizontal;
            this.TransformVertical = transformVertical;
            this.Fallback = fallback;
            this.VertexColumnCount = vertexColumnCount;
        }

        internal bool TransformHorizontal { get; }

        internal bool TransformVertical { get; }

        internal FallbackStrategy Fallback { get; }

        internal int VertexColumnCount { get; }

        internal double[] Vertices { get; }

        internal List<TriangleIndices> Triangles { get; }

        internal double GetVertexValue(int vertexIndex, int columnIndex)
        {
            int baseOffset = vertexIndex * this.VertexColumnCount;
            return this.Vertices[baseOffset + columnIndex];
        }
    }
}
