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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BitMiracle.LibTiff.Classic;

/// <summary>
/// Represents a documented type.
/// </summary>
internal static partial class GeoTiffGridLoader
{
    private const int ModelPixelScaleTag = 33550;
    private const int ModelTiePointTag = 33922;
    private const int ModelTransformationTag = 34264;
    private const int GeoKeyDirectoryTag = 34735;
    private const int GdalMetadataTag = 42112;
    private const int GdalNoDataTag = 42113;
    private const int GeogAngularUnitsGeoKey = 2054;
    private const int GtRasterTypeGeoKey = 1025;
    private const int RasterPixelIsPoint = 2;
#if NET8_0_OR_GREATER
    [GeneratedRegex("<Item(?<attrs>[^>]*)>(?<value>.*?)</Item>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MetadataItemRegex();
#else
    private static readonly Regex MetadataItemRegex = new("<Item(?<attrs>[^>]*)>(?<value>.*?)</Item>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
#endif

    private enum GridMode
    {
        Horizontal,
        Vertical,
        Xyz,
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <returns>The computed value.</returns>
    internal static IReadOnlyList<GeoTiffHGridShiftMathTransform.HorizontalGrid> LoadHorizontal(string path)
    {
        return LoadCore(path, GridMode.Horizontal)
            .Select(page => page.ToHorizontalGrid(path))
            .Where(grid => !(grid is null))
            .ToArray();
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <returns>The computed value.</returns>
    internal static IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid> LoadVertical(string path)
    {
        return LoadCore(path, GridMode.Vertical)
            .Select(page => page.ToVerticalGrid(path))
            .Where(grid => !(grid is null))
            .ToArray();
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <returns>The computed value.</returns>
    internal static IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> LoadXyz(string path)
    {
        return LoadXyz(path, requireMetreUnits: true);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="requireMetreUnits">Whether XYZ samples must be unit=metre.</param>
    /// <returns>The computed value.</returns>
    internal static IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> LoadXyz(string path, bool requireMetreUnits)
    {
        return LoadCore(path, GridMode.Xyz, requireMetreUnits)
            .Select(page => page.ToXyzGrid(path))
            .Where(grid => !(grid is null))
            .ToArray();
    }

    private static List<LoadedPage> LoadCore(string path, GridMode mode, bool requireMetreUnitsForXyz = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var pages = new List<LoadedPage>();
        using (Tiff tiff = Tiff.Open(path, "r"))
        {
            if (tiff is null)
            {
                throw new InvalidDataException("Unable to open GeoTIFF grid.");
            }

            short pageIndex = 0;
            do
            {
                if (!TryReadPage(path, tiff, mode, requireMetreUnitsForXyz, out LoadedPage page))
                {
                    pageIndex++;
                    continue;
                }

                pages.Add(page);
                pageIndex++;
            }
            while (tiff.ReadDirectory());
        }

        return pages;
    }

    private static bool TryReadPage(string path, Tiff tiff, GridMode mode, bool requireMetreUnitsForXyz, out LoadedPage page)
    {
        page = null;
        if (!TryGetIntField(tiff, TiffTag.IMAGEWIDTH, out int width)
            || !TryGetIntField(tiff, TiffTag.IMAGELENGTH, out int height)
            || width <= 1
            || height <= 1)
        {
            return false;
        }

        int samplesPerPixel = 1;
        if (TryGetIntField(tiff, TiffTag.SAMPLESPERPIXEL, out int foundSamples))
        {
            samplesPerPixel = Math.Max(1, foundSamples);
        }

        if (!TryGetSampleEncoding(tiff, out SampleEncoding encoding))
        {
            throw new InvalidDataException("Unsupported GeoTIFF sample encoding.");
        }

        if (!TryGetGeoTransform(tiff, width, height, out GeoTransform transform))
        {
            return false;
        }

        SampleData sampleData = ReadSampleData(tiff, width, height, samplesPerPixel, encoding);
        GeoMetadata metadata = ReadMetadata(tiff, samplesPerPixel);
        sampleData = sampleData.ApplyScaleOffset(metadata.ScaleBySample, metadata.OffsetBySample);
        switch (mode)
        {
            case GridMode.Horizontal:
                if (!TryResolveHorizontalSampleIndices(samplesPerPixel, metadata, out int latitudeSample, out int longitudeSample, out bool positiveWest))
                {
                    return false;
                }

                page = LoadedPage.CreateHorizontal(transform, sampleData, metadata, latitudeSample, longitudeSample, positiveWest);
                return true;
            case GridMode.Vertical:
                if (!TryResolveVerticalSampleIndex(samplesPerPixel, metadata, out int sampleIndex))
                {
                    return false;
                }

                page = LoadedPage.CreateVertical(transform, sampleData, metadata, sampleIndex);
                return true;
            case GridMode.Xyz:
                if (!TryResolveXyzSampleIndices(samplesPerPixel, metadata, requireMetreUnitsForXyz, out int sampleX, out int sampleY, out int sampleZ))
                {
                    return false;
                }

                page = LoadedPage.CreateXyz(transform, sampleData, metadata, sampleX, sampleY, sampleZ);
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported GeoTIFF grid mode.");
        }
    }

    private static bool TryResolveHorizontalSampleIndices(int samplesPerPixel, GeoMetadata metadata, out int latitudeSample, out int longitudeSample, out bool positiveWest)
    {
        latitudeSample = -1;
        longitudeSample = -1;
        positiveWest = false;
        for (int i = 0; i < samplesPerPixel; i++)
        {
            if (!metadata.DescriptionsBySample.TryGetValue(i, out string description))
            {
                continue;
            }

            if (ContainsOrdinalIgnoreCase(description, "latitude_offset"))
            {
                latitudeSample = i;
            }
            else if (ContainsOrdinalIgnoreCase(description, "longitude_offset"))
            {
                longitudeSample = i;
            }
        }

        if (latitudeSample < 0 || longitudeSample < 0)
        {
            if (samplesPerPixel == 2)
            {
                latitudeSample = 0;
                longitudeSample = 1;
            }
            else
            {
                return false;
            }
        }

        if (metadata.PositiveValueBySample.TryGetValue(longitudeSample, out string positiveValue)
            && positiveValue.Equals("west", StringComparison.OrdinalIgnoreCase))
        {
            positiveWest = true;
        }

        return true;
    }

    private static bool TryResolveVerticalSampleIndex(int samplesPerPixel, GeoMetadata metadata, out int sampleIndex)
    {
        for (int i = 0; i < samplesPerPixel; i++)
        {
            if (!metadata.DescriptionsBySample.TryGetValue(i, out string description))
            {
                continue;
            }

            if (ContainsOrdinalIgnoreCase(description, "geoid_undulation")
                || ContainsOrdinalIgnoreCase(description, "vertical_offset"))
            {
                sampleIndex = i;
                return true;
            }
        }

        sampleIndex = 0;
        return samplesPerPixel >= 1;
    }

    private static bool TryResolveXyzSampleIndices(int samplesPerPixel, GeoMetadata metadata, bool requireMetreUnits, out int sampleX, out int sampleY, out int sampleZ)
    {
        sampleX = -1;
        sampleY = -1;
        sampleZ = -1;

        for (int i = 0; i < samplesPerPixel; i++)
        {
            if (!metadata.DescriptionsBySample.TryGetValue(i, out string description))
            {
                continue;
            }

            if (ContainsOrdinalIgnoreCase(description, "x_translation"))
            {
                sampleX = i;
            }
            else if (ContainsOrdinalIgnoreCase(description, "y_translation"))
            {
                sampleY = i;
            }
            else if (ContainsOrdinalIgnoreCase(description, "z_translation"))
            {
                sampleZ = i;
            }
        }

        if (sampleX < 0 || sampleY < 0 || sampleZ < 0)
        {
            if (samplesPerPixel >= 3)
            {
                sampleX = 0;
                sampleY = 1;
                sampleZ = 2;
            }
            else
            {
                return false;
            }
        }

        if (requireMetreUnits
            && (!IsUnitMetreOrEmpty(metadata, sampleX)
            || !IsUnitMetreOrEmpty(metadata, sampleY)
            || !IsUnitMetreOrEmpty(metadata, sampleZ)))
        {
            throw new InvalidDataException("xyzgridshift only supports unit=metre for XYZ samples.");
        }

        return true;
    }

    private static bool IsUnitMetreOrEmpty(GeoMetadata metadata, int sampleIndex)
    {
        if (!metadata.UnitTypeBySample.TryGetValue(sampleIndex, out string unitType))
        {
            return true;
        }

        string unit = unitType?.Trim();
        if (string.IsNullOrWhiteSpace(unit))
        {
            return true;
        }

        return unit.Equals("metre", StringComparison.OrdinalIgnoreCase)
            || unit.Equals("meter", StringComparison.OrdinalIgnoreCase)
            || unit.Equals("metres", StringComparison.OrdinalIgnoreCase)
            || unit.Equals("meters", StringComparison.OrdinalIgnoreCase)
            || unit.Equals("m", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsOrdinalIgnoreCase(string value, string search)
    {
#if NETSTANDARD2_1_OR_GREATER
        return value.Contains(search, StringComparison.OrdinalIgnoreCase);
#else
        return value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
#endif
    }

    private static bool TryGetGeoTransform(Tiff tiff, int width, int height, out GeoTransform transform)
    {
        transform = default;
        bool pixelIsPoint = IsPixelIsPoint(tiff);
        double pixelOffset = pixelIsPoint ? 0d : 0.5d;

        if (TryGetDoubleArrayField(tiff, (TiffTag)ModelTransformationTag, out double[] matrix) && matrix.Length >= 16)
        {
            double a = matrix[0];
            double b = matrix[1];
            double c = matrix[3] + (pixelOffset * matrix[0]) + (pixelOffset * matrix[1]);
            double d = matrix[4];
            double e = matrix[5];
            double f = matrix[7] + (pixelOffset * matrix[4]) + (pixelOffset * matrix[5]);
            if (!TryCreateGeoTransform(width, height, a, b, c, d, e, f, out transform))
            {
                return false;
            }

            return true;
        }

        if (!TryGetDoubleArrayField(tiff, (TiffTag)ModelPixelScaleTag, out double[] pixelScale) || pixelScale.Length < 2)
        {
            return false;
        }

        if (!TryGetDoubleArrayField(tiff, (TiffTag)ModelTiePointTag, out double[] tiePoints) || tiePoints.Length < 6)
        {
            return false;
        }

        double tieI = tiePoints[0];
        double tieJ = tiePoints[1];
        double tieX = tiePoints[3];
        double tieY = tiePoints[4];
        double scaleX = pixelScale[0];
        double scaleY = pixelScale[1];
        if (scaleX == 0d || scaleY == 0d)
        {
            return false;
        }

        double aSimple = scaleX;
        double bSimple = 0d;
        double cSimple = tieX + ((pixelOffset - tieI) * scaleX);
        double dSimple = 0d;
        double eSimple = -scaleY;
        double fSimple = tieY - ((pixelOffset - tieJ) * scaleY);
        return TryCreateGeoTransform(width, height, aSimple, bSimple, cSimple, dSimple, eSimple, fSimple, out transform);
    }

    private static bool TryCreateGeoTransform(int width, int height, double a, double b, double c, double d, double e, double f, out GeoTransform transform)
    {
        transform = default;
        double determinant = (a * e) - (b * d);
        if (Math.Abs(determinant) <= 1e-18d)
        {
            return false;
        }

        (double west, double east, double south, double north, double area, double epsilon) = ComputeBounds(width, height, a, b, c, d, e, f);
        transform = new GeoTransform(width, height, a, b, c, d, e, f, determinant, west, east, south, north, area, epsilon);
        return true;
    }

    private static (double West, double East, double South, double North, double Area, double Epsilon) ComputeBounds(
        int width,
        int height,
        double a,
        double b,
        double c,
        double d,
        double e,
        double f)
    {
        var xs = new[] { 0d, width - 1d, 0d, width - 1d };
        var ys = new[] { 0d, 0d, height - 1d, height - 1d };
        double west = double.PositiveInfinity;
        double east = double.NegativeInfinity;
        double south = double.PositiveInfinity;
        double north = double.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            double lon = (a * xs[i]) + (b * ys[i]) + c;
            double lat = (d * xs[i]) + (e * ys[i]) + f;
            west = Math.Min(west, lon);
            east = Math.Max(east, lon);
            south = Math.Min(south, lat);
            north = Math.Max(north, lat);
        }

        double area = Math.Max(1e-12d, (east - west) * (north - south));
        double resX = Math.Sqrt((a * a) + (d * d));
        double resY = Math.Sqrt((b * b) + (e * e));
        double epsilon = (resX + resY) * 1e-5d;
        return (west, east, south, north, area, epsilon);
    }

    private static SampleData ReadSampleData(Tiff tiff, int width, int height, int samplesPerPixel, SampleEncoding encoding)
    {
        int scanlineSize = tiff.ScanlineSize();
        var sampleValues = new double[samplesPerPixel][];
        for (int i = 0; i < samplesPerPixel; i++)
        {
            sampleValues[i] = new double[width * height];
        }

        PlanarConfig planarConfig = PlanarConfig.CONTIG;
        if (TryGetIntField(tiff, TiffTag.PLANARCONFIG, out int planarConfigValue))
        {
            planarConfig = (PlanarConfig)planarConfigValue;
        }

        if (planarConfig == PlanarConfig.SEPARATE && samplesPerPixel > 1)
        {
            for (int sample = 0; sample < samplesPerPixel; sample++)
            {
                byte[] buffer = new byte[scanlineSize];
                for (int row = 0; row < height; row++)
                {
                    if (!tiff.ReadScanline(buffer, row, (short)sample))
                    {
                        throw new InvalidDataException("Failed to read GeoTIFF scanline.");
                    }

                    for (int column = 0; column < width; column++)
                    {
                        int offset = column * encoding.BytesPerSample;
                        sampleValues[sample][(row * width) + column] = encoding.ReadValue(buffer, offset);
                    }
                }
            }
        }
        else
        {
            byte[] buffer = new byte[scanlineSize];
            for (int row = 0; row < height; row++)
            {
                if (!tiff.ReadScanline(buffer, row))
                {
                    throw new InvalidDataException("Failed to read GeoTIFF scanline.");
                }

                for (int column = 0; column < width; column++)
                {
                    int pixelBase = column * encoding.BytesPerSample * samplesPerPixel;
                    for (int sample = 0; sample < samplesPerPixel; sample++)
                    {
                        int offset = pixelBase + (sample * encoding.BytesPerSample);
                        sampleValues[sample][(row * width) + column] = encoding.ReadValue(buffer, offset);
                    }
                }
            }
        }

        return new SampleData(sampleValues, width: width);
    }

    private static GeoMetadata ReadMetadata(Tiff tiff, int samplesPerPixel)
    {
        var descriptionsBySample = new Dictionary<int, string>();
        var positiveValueBySample = new Dictionary<int, string>();
        var scaleBySample = new Dictionary<int, double>();
        var offsetBySample = new Dictionary<int, double>();
        var unitTypeBySample = new Dictionary<int, string>();

        if (TryGetStringField(tiff, (TiffTag)GdalMetadataTag, out string gdalMetadata) && !string.IsNullOrWhiteSpace(gdalMetadata))
        {
            string sanitizedMetadata = SanitizeXmlMetadata(gdalMetadata);
            ParseMetadataItems(sanitizedMetadata, samplesPerPixel, descriptionsBySample, positiveValueBySample, scaleBySample, offsetBySample, unitTypeBySample);
        }

        double? noDataValue = null;
        if (TryGetStringField(tiff, (TiffTag)GdalNoDataTag, out string noDataText)
            && double.TryParse(
                CleanMetadataValue(noDataText),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out double parsedNoData))
        {
            noDataValue = parsedNoData;
        }

        double angularScaleToDegree = ResolveAngularScaleToDegree(tiff);
        return new GeoMetadata(descriptionsBySample, positiveValueBySample, scaleBySample, offsetBySample, noDataValue, angularScaleToDegree, unitTypeBySample);
    }

    private static string SanitizeXmlMetadata(string metadata)
    {
        if (metadata is null)
        {
            return string.Empty;
        }

        string sanitized = metadata.Trim('\0', '\uFEFF', ' ', '\t', '\r', '\n');
#if NETSTANDARD2_1_OR_GREATER
        int firstTag = sanitized.IndexOf('<', StringComparison.Ordinal);
#else
        int firstTag = sanitized.IndexOf('<');
#endif
        if (firstTag > 0)
        {
            sanitized = sanitized.Substring(firstTag);
        }

        int lastTag = sanitized.LastIndexOf('>');
        if (lastTag >= 0 && lastTag + 1 < sanitized.Length)
        {
            sanitized = sanitized.Substring(0, lastTag + 1);
        }

        return sanitized;
    }

    private static string CleanMetadataValue(string value)
    {
        return value is null ? string.Empty : value.Trim('\0', ' ', '\t', '\r', '\n');
    }

    private static void ParseMetadataItems(
        string metadata,
        int samplesPerPixel,
        Dictionary<int, string> descriptionsBySample,
        Dictionary<int, string> positiveValueBySample,
        Dictionary<int, double> scaleBySample,
        Dictionary<int, double> offsetBySample,
        Dictionary<int, string> unitTypeBySample)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return;
        }

#if NET8_0_OR_GREATER
        MatchCollection matches = MetadataItemRegex().Matches(metadata);
#else
        MatchCollection matches = MetadataItemRegex.Matches(metadata);
#endif
        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            Group attrsGroup = match.Groups["attrs"];
            Group valueGroup = match.Groups["value"];
            if (!attrsGroup.Success || !valueGroup.Success)
            {
                continue;
            }

            string attrs = attrsGroup.Value;
            string name = ExtractAttribute(attrs, "name");
            string sampleValue = ExtractAttribute(attrs, "sample");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sampleValue))
            {
                continue;
            }

            if (!int.TryParse(sampleValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sample)
                || sample < 0
                || sample >= samplesPerPixel)
            {
                continue;
            }

            string value = CleanMetadataValue(valueGroup.Value);
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (name.Equals("DESCRIPTION", StringComparison.OrdinalIgnoreCase))
            {
                descriptionsBySample[sample] = value;
            }
            else if (name.Equals("positive_value", StringComparison.OrdinalIgnoreCase))
            {
                positiveValueBySample[sample] = value;
            }
            else if (name.Equals("SCALE", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double scale))
            {
                scaleBySample[sample] = scale;
            }
            else if (name.Equals("OFFSET", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double offset))
            {
                offsetBySample[sample] = offset;
            }
            else if (name.Equals("UNITTYPE", StringComparison.OrdinalIgnoreCase))
            {
                unitTypeBySample[sample] = value;
            }
        }
    }

    private static string ExtractAttribute(string attrs, string attributeName)
    {
        if (string.IsNullOrWhiteSpace(attrs) || string.IsNullOrWhiteSpace(attributeName))
        {
            return string.Empty;
        }

        Match match = Regex.Match(
            attrs,
            "\\b" + Regex.Escape(attributeName) + "\\s*=\\s*\"(?<value>[^\"]*)\"",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return string.Empty;
        }

        Group valueGroup = match.Groups["value"];
        return valueGroup.Success ? CleanMetadataValue(valueGroup.Value) : string.Empty;
    }

    private static double ResolveAngularScaleToDegree(Tiff tiff)
    {
        if (!TryGetShortArrayField(tiff, (TiffTag)GeoKeyDirectoryTag, out short[] keyDirectory) || keyDirectory.Length < 4)
        {
            return 1d;
        }

        int keyCount = keyDirectory[3];
        for (int i = 0; i < keyCount; i++)
        {
            int entryOffset = 4 + (i * 4);
            if (entryOffset + 3 >= keyDirectory.Length)
            {
                break;
            }

            int keyId = keyDirectory[entryOffset];
            int tiffTagLocation = keyDirectory[entryOffset + 1];
            int valueOffset = keyDirectory[entryOffset + 3];
            if (keyId != GeogAngularUnitsGeoKey)
            {
                continue;
            }

            int angularCode = tiffTagLocation == 0 ? valueOffset : 9102;
            switch (angularCode)
            {
                case 9101:
                    return 180d / Math.PI;
                case 9102:
                    return 1d;
                case 9105:
                    return 0.9d;
                default:
                    return 1d;
            }
        }

        return 1d;
    }

    private static bool IsPixelIsPoint(Tiff tiff)
    {
        if (!TryGetShortArrayField(tiff, (TiffTag)GeoKeyDirectoryTag, out short[] keyDirectory) || keyDirectory.Length < 4)
        {
            return false;
        }

        int keyCount = keyDirectory[3];
        for (int i = 0; i < keyCount; i++)
        {
            int entryOffset = 4 + (i * 4);
            if (entryOffset + 3 >= keyDirectory.Length)
            {
                break;
            }

            int keyId = keyDirectory[entryOffset];
            int tiffTagLocation = keyDirectory[entryOffset + 1];
            int valueOffset = keyDirectory[entryOffset + 3];
            if (keyId == GtRasterTypeGeoKey && tiffTagLocation == 0)
            {
                return valueOffset == RasterPixelIsPoint;
            }
        }

        return false;
    }

    private static bool TryGetSampleEncoding(Tiff tiff, out SampleEncoding encoding)
    {
        encoding = default;
        int bitsPerSample = 0;
        if (!TryGetIntField(tiff, TiffTag.BITSPERSAMPLE, out bitsPerSample))
        {
            return false;
        }

        SampleFormat sampleFormat = SampleFormat.IEEEFP;
        if (TryGetIntField(tiff, TiffTag.SAMPLEFORMAT, out int sampleFormatRaw))
        {
            sampleFormat = (SampleFormat)sampleFormatRaw;
        }

        if (!SampleEncoding.TryCreate(bitsPerSample, sampleFormat, out encoding))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetIntField(Tiff tiff, TiffTag tag, out int value)
    {
        value = 0;
        FieldValue[] field = tiff.GetField(tag);
        if (field is null || field.Length == 0)
        {
            return false;
        }

        value = field[0].ToInt();
        return true;
    }

    private static bool TryGetStringField(Tiff tiff, TiffTag tag, out string value)
    {
        value = null;
        FieldValue[] field = tiff.GetField(tag);
        if (field is null || field.Length == 0)
        {
            return false;
        }

        value = field[field.Length - 1].ToString();
        if (string.IsNullOrEmpty(value) && field.Length > 1)
        {
            value = field[0].ToString();
        }

        return !(value is null);
    }

    private static bool TryGetDoubleArrayField(Tiff tiff, TiffTag tag, out double[] values)
    {
        values = Array.Empty<double>();
        FieldValue[] field = tiff.GetField(tag);
        if (field is null || field.Length == 0)
        {
            return false;
        }

        double[] candidate = field[field.Length - 1].ToDoubleArray();
        if (candidate is null || candidate.Length == 0)
        {
            return false;
        }

        values = candidate;
        return true;
    }

    private static bool TryGetShortArrayField(Tiff tiff, TiffTag tag, out short[] values)
    {
        values = Array.Empty<short>();
        FieldValue[] field = tiff.GetField(tag);
        if (field is null || field.Length == 0)
        {
            return false;
        }

        short[] candidate = field[field.Length - 1].ToShortArray();
        if (candidate is null || candidate.Length == 0)
        {
            return false;
        }

        values = candidate;
        return true;
    }

    private readonly struct GeoTransform
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoTransform"/> struct.
        /// </summary>
        /// <param name="width">The width value.</param>
        /// <param name="height">The height value.</param>
        /// <param name="a">The a value.</param>
        /// <param name="b">The b value.</param>
        /// <param name="c">The c value.</param>
        /// <param name="d">The d value.</param>
        /// <param name="e">The e value.</param>
        /// <param name="f">The f value.</param>
        /// <param name="determinant">The determinant value.</param>
        /// <param name="west">The west value.</param>
        /// <param name="east">The east value.</param>
        /// <param name="south">The south value.</param>
        /// <param name="north">The north value.</param>
        /// <param name="area">The area value.</param>
        /// <param name="epsilon">The epsilon value.</param>
        internal GeoTransform(
            int width,
            int height,
            double a,
            double b,
            double c,
            double d,
            double e,
            double f,
            double determinant,
            double west,
            double east,
            double south,
            double north,
            double area,
            double epsilon)
        {
            this.Width = width;
            this.Height = height;
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
            this.E = e;
            this.F = f;
            this.Determinant = determinant;
            this.West = west;
            this.East = east;
            this.South = south;
            this.North = north;
            this.Area = area;
            this.Epsilon = epsilon;
        }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal int Width { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal int Height { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double A { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double B { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double C { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double D { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double E { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double F { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double Determinant { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double West { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double East { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double South { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double North { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double Area { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double Epsilon { get; }
    }

    private readonly struct GeoMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoMetadata"/> struct.
        /// </summary>
        /// <param name="descriptionsBySample">The descriptionsBySample value.</param>
        /// <param name="positiveValueBySample">The positiveValueBySample value.</param>
        /// <param name="scaleBySample">The scaleBySample value.</param>
        /// <param name="offsetBySample">The offsetBySample value.</param>
        /// <param name="noDataValue">The noDataValue value.</param>
        /// <param name="angularScaleToDegree">The angularScaleToDegree value.</param>
        /// <param name="unitTypeBySample">The unitTypeBySample value.</param>
        internal GeoMetadata(
            IReadOnlyDictionary<int, string> descriptionsBySample,
            IReadOnlyDictionary<int, string> positiveValueBySample,
            IReadOnlyDictionary<int, double> scaleBySample,
            IReadOnlyDictionary<int, double> offsetBySample,
            double? noDataValue,
            double angularScaleToDegree,
            IReadOnlyDictionary<int, string> unitTypeBySample)
        {
            this.DescriptionsBySample = descriptionsBySample;
            this.PositiveValueBySample = positiveValueBySample;
            this.ScaleBySample = scaleBySample;
            this.OffsetBySample = offsetBySample;
            this.NoDataValue = noDataValue;
            this.AngularScaleToDegree = angularScaleToDegree;
            this.UnitTypeBySample = unitTypeBySample;
        }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal IReadOnlyDictionary<int, string> DescriptionsBySample { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal IReadOnlyDictionary<int, string> PositiveValueBySample { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal IReadOnlyDictionary<int, double> ScaleBySample { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal IReadOnlyDictionary<int, double> OffsetBySample { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double? NoDataValue { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double AngularScaleToDegree { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal IReadOnlyDictionary<int, string> UnitTypeBySample { get; }
    }

    private sealed class LoadedPage
    {
        private readonly GeoTransform transform;
        private readonly SampleData sampleData;
        private readonly GeoMetadata metadata;
        private readonly int latitudeSample;
        private readonly int longitudeSample;
        private readonly int verticalSample;
        private readonly int xSample;
        private readonly int ySample;
        private readonly int zSample;
        private readonly bool longitudePositiveWest;
        private readonly GridMode mode;

        private LoadedPage(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int latitudeSample,
            int longitudeSample,
            int verticalSample,
            int xSample,
            int ySample,
            int zSample,
            bool longitudePositiveWest,
            GridMode mode)
        {
            this.transform = transform;
            this.sampleData = sampleData;
            this.metadata = metadata;
            this.latitudeSample = latitudeSample;
            this.longitudeSample = longitudeSample;
            this.verticalSample = verticalSample;
            this.xSample = xSample;
            this.ySample = ySample;
            this.zSample = zSample;
            this.longitudePositiveWest = longitudePositiveWest;
            this.mode = mode;
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="transform">The transform value.</param>
        /// <param name="sampleData">The sampleData value.</param>
        /// <param name="metadata">The metadata value.</param>
        /// <param name="latitudeSample">The latitudeSample value.</param>
        /// <param name="longitudeSample">The longitudeSample value.</param>
        /// <param name="longitudePositiveWest">The longitudePositiveWest value.</param>
        /// <returns>The computed value.</returns>
        internal static LoadedPage CreateHorizontal(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int latitudeSample,
            int longitudeSample,
            bool longitudePositiveWest)
        {
            return new LoadedPage(transform, sampleData, metadata, latitudeSample, longitudeSample, -1, -1, -1, -1, longitudePositiveWest, GridMode.Horizontal);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="transform">The transform value.</param>
        /// <param name="sampleData">The sampleData value.</param>
        /// <param name="metadata">The metadata value.</param>
        /// <param name="verticalSample">The verticalSample value.</param>
        /// <returns>The computed value.</returns>
        internal static LoadedPage CreateVertical(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int verticalSample)
        {
            return new LoadedPage(transform, sampleData, metadata, -1, -1, verticalSample, -1, -1, -1, false, GridMode.Vertical);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="transform">The transform value.</param>
        /// <param name="sampleData">The sampleData value.</param>
        /// <param name="metadata">The metadata value.</param>
        /// <param name="xSample">The xSample value.</param>
        /// <param name="ySample">The ySample value.</param>
        /// <param name="zSample">The zSample value.</param>
        /// <returns>The computed value.</returns>
        internal static LoadedPage CreateXyz(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int xSample,
            int ySample,
            int zSample)
        {
            return new LoadedPage(transform, sampleData, metadata, -1, -1, -1, xSample, ySample, zSample, false, GridMode.Xyz);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="sourcePath">The sourcePath value.</param>
        /// <returns>The computed value.</returns>
        internal GeoTiffHGridShiftMathTransform.HorizontalGrid ToHorizontalGrid(string sourcePath)
        {
            if (this.mode != GridMode.Horizontal)
            {
                return null;
            }

            double latitudeScale = ResolveHorizontalShiftScaleToDegree(this.metadata, this.latitudeSample);
            double longitudeScale = ResolveHorizontalShiftScaleToDegree(this.metadata, this.longitudeSample);
            return new GeoTiffHGridShiftMathTransform.HorizontalGrid(
                sourcePath,
                this.transform.Width,
                this.transform.Height,
                this.transform.Area,
                this.transform.Epsilon,
                this.transform.West,
                this.transform.East,
                this.transform.South,
                this.transform.North,
                this.transform.A,
                this.transform.B,
                this.transform.C,
                this.transform.D,
                this.transform.E,
                this.transform.F,
                this.sampleData,
                this.latitudeSample,
                this.longitudeSample,
                this.longitudePositiveWest,
                latitudeScale,
                longitudeScale);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="sourcePath">The sourcePath value.</param>
        /// <returns>The computed value.</returns>
        internal GeoTiffVGridShiftMathTransform.VerticalGrid ToVerticalGrid(string sourcePath)
        {
            if (this.mode != GridMode.Vertical)
            {
                return null;
            }

            return new GeoTiffVGridShiftMathTransform.VerticalGrid(
                sourcePath,
                this.transform.Width,
                this.transform.Height,
                this.transform.Area,
                this.transform.Epsilon,
                this.transform.West,
                this.transform.East,
                this.transform.South,
                this.transform.North,
                this.transform.A,
                this.transform.B,
                this.transform.C,
                this.transform.D,
                this.transform.E,
                this.transform.F,
                this.sampleData,
                this.verticalSample,
                this.metadata.NoDataValue);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="sourcePath">The sourcePath value.</param>
        /// <returns>The computed value.</returns>
        internal GeoTiffXyzGridShiftMathTransform.XyzGrid ToXyzGrid(string sourcePath)
        {
            if (this.mode != GridMode.Xyz)
            {
                return null;
            }

            return new GeoTiffXyzGridShiftMathTransform.XyzGrid(
                sourcePath,
                this.transform.Width,
                this.transform.Height,
                this.transform.Area,
                this.transform.Epsilon,
                this.transform.West,
                this.transform.East,
                this.transform.South,
                this.transform.North,
                this.transform.A,
                this.transform.B,
                this.transform.C,
                this.transform.D,
                this.transform.E,
                this.transform.F,
                this.sampleData,
                this.xSample,
                this.ySample,
                this.zSample);
        }

        private static double ResolveHorizontalShiftScaleToDegree(GeoMetadata metadata, int sampleIndex)
        {
            if (metadata.UnitTypeBySample.TryGetValue(sampleIndex, out string unitType))
            {
                string unit = unitType.Trim();
                if (unit.Equals("degree", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("degrees", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("deg", StringComparison.OrdinalIgnoreCase))
                {
                    return 1d;
                }

                if (unit.Equals("radian", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("radians", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("rad", StringComparison.OrdinalIgnoreCase))
                {
                    return 180d / Math.PI;
                }

                if (unit.Equals("arc-second", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arc-seconds", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arc_second", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arc_seconds", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arcsecond", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arcseconds", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arcsec", StringComparison.OrdinalIgnoreCase))
                {
                    return 1d / 3600d;
                }
            }

            // PROJ hgridshift defaults to arc-second offsets when unit metadata is absent.
            return 1d / 3600d;
        }
    }
}
