// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

using BenchmarkDotNet.Attributes;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Geometries;

/// <summary>
/// Benchmarks in-place coordinate transformation throughput across structure-of-arrays and array-of-struct layouts.
/// </summary>
/// <remarks>
/// These scenarios focus on memory-layout effects and transform invocation styles. They complement
/// <see cref="ProjParityBenchmarks"/> which focuses on EPSG-pipeline parity with PROJ's benchmark scenarios.
/// </remarks>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for explicit invocation from Program and benchmark tooling stability.")]
public class PerformanceTests
{
    private static readonly MathTransform WGS84ToWebMercator = CreateWgs84ToWebMercator();

    private int cnt;

    private double[] xs = [];

    private double[] ys = [];

    private XY[] xys = [];

    private XYZ[] xyzs = [];

    private double[] xsCopy = [];

    private double[] ysCopy = [];

    private XY[] xysCopy = [];

    private XYZ[] xyzsCopy = [];

    /// <summary>
    /// Executes all benchmark entry points once and verifies numerical consistency across variants.
    /// </summary>
    public static void Validate()
    {
        var instance = new PerformanceTests();
        instance.GlobalSetup();

        instance.SoAOneByOne();
        (double X, double Y)[] firstOutput = [.. instance.xsCopy.Zip(instance.ysCopy, (x, y) => (X: x, Y: y))];

        for (int i = 0; i < firstOutput.Length; i++)
        {
            if (firstOutput[i].Equals((instance.xys[i].X, instance.xys[i].Y)))
            {
                throw new InvalidOperationException("Validation failure: transformer isn't actually transforming.");
            }
        }

        instance.SoABatched();
        Validate(instance.xsCopy.Zip(instance.ysCopy, (x, y) => (X: x, Y: y)).ToArray());

        instance.TightAoSOneByOne();
        Validate(Array.ConvertAll(instance.xysCopy, xy => (xy.X, xy.Y)));

        instance.TightAoSBatched();
        Validate(Array.ConvertAll(instance.xysCopy, xy => (xy.X, xy.Y)));

        instance.LooserAoSOneByOne();
        Validate(Array.ConvertAll(instance.xyzsCopy, xyz => (xyz.X, xyz.Y)));

        instance.LooserAoSBatched();
        Validate(Array.ConvertAll(instance.xyzsCopy, xyz => (xyz.X, xyz.Y)));

        void Validate(ReadOnlySpan<(double X, double Y)> nextOutput)
        {
            if (!nextOutput.SequenceEqual(firstOutput))
            {
                throw new InvalidOperationException("Validation failure: some transform method is giving different results than another.");
            }
        }
    }

    /// <summary>
    /// Loads benchmark coordinate data and prepares mutable working buffers.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        string? currentFolderPathCandidate = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (currentFolderPathCandidate is null)
        {
            throw new InvalidOperationException("Unable to resolve benchmark assembly directory.");
        }

        string currentFolderPath = currentFolderPathCandidate;
        string fullPathToData = Path.Combine(currentFolderPath, "coords.dat.gz");
        using var reader = new BinaryReader(new GZipStream(File.OpenRead(fullPathToData), CompressionMode.Decompress));
        this.cnt = reader.ReadInt32();

        this.xs = new double[this.cnt];
        this.ys = new double[this.cnt];
        this.xys = new XY[this.cnt];
        this.xyzs = new XYZ[this.cnt];

        for (int i = 0; i < this.cnt; i++)
        {
            this.xs[i] = this.xys[i].X = this.xyzs[i].X = reader.ReadDouble();
        }

        for (int i = 0; i < this.cnt; i++)
        {
            this.ys[i] = this.xys[i].Y = this.xyzs[i].Y = reader.ReadDouble();
        }

        // transforms happen in-place, so at the start of every iteration, we copy the source
        // coordinate data to these throwaway arrays in order to be able to repeat the test
        // without allocating anything.  this slightly hurts accuracy, but the effect appears to
        // be less than 5% of the total test's time, and [IterationSetup] / [IterationCleanup]
        // aren't designed for the kinds of benchmarks we're running here.
        this.xsCopy = new double[this.cnt];
        this.ysCopy = new double[this.cnt];
        this.xysCopy = new XY[this.cnt];
        this.xyzsCopy = new XYZ[this.cnt];
    }

    /// <summary>
    /// Measures one-by-one transforms for separate X/Y arrays (structure-of-arrays layout).
    /// </summary>
    [Benchmark]
    public void SoAOneByOne()
    {
        this.xs.CopyTo(this.xsCopy.AsSpan());
        this.ys.CopyTo(this.ysCopy.AsSpan());
        for (int i = 0; i < this.cnt; i++)
        {
            WGS84ToWebMercator.Transform(ref this.xsCopy[i], ref this.ysCopy[i]);
        }
    }

    /// <summary>
    /// Measures batched transforms for separate X/Y arrays (structure-of-arrays layout).
    /// </summary>
    [Benchmark]
    public void SoABatched()
    {
        this.xs.CopyTo(this.xsCopy.AsSpan());
        this.ys.CopyTo(this.ysCopy.AsSpan());
        WGS84ToWebMercator.Transform(this.xsCopy, this.ysCopy);
    }

    /// <summary>
    /// Measures one-by-one transforms for tightly packed XY structs (array-of-struct layout).
    /// </summary>
    [Benchmark]
    public void TightAoSOneByOne()
    {
        this.xys.CopyTo(this.xysCopy.AsSpan());
        for (int i = 0; i < this.cnt; i++)
        {
            WGS84ToWebMercator.Transform(ref this.xysCopy[i].X, ref this.xysCopy[i].Y);
        }
    }

    /// <summary>
    /// Measures batched transforms for tightly packed XY structs (array-of-struct layout).
    /// </summary>
    [Benchmark]
    public void TightAoSBatched()
    {
        this.xys.CopyTo(this.xysCopy.AsSpan());
        WGS84ToWebMercator.Transform(this.xysCopy);
    }

    /// <summary>
    /// Measures one-by-one transforms for looser XYZ structs when only X/Y are transformed.
    /// </summary>
    [Benchmark]
    public void LooserAoSOneByOne()
    {
        this.xyzs.CopyTo(this.xyzsCopy.AsSpan());
        for (int i = 0; i < this.cnt; i++)
        {
            WGS84ToWebMercator.Transform(ref this.xyzsCopy[i].X, ref this.xyzsCopy[i].Y);
        }
    }

    /// <summary>
    /// Measures batched transforms for looser XYZ structs when only X/Y are transformed.
    /// </summary>
    [Benchmark]
    public void LooserAoSBatched()
    {
        this.xyzs.CopyTo(this.xyzsCopy.AsSpan());
        WGS84ToWebMercator.Transform(this.xyzsCopy);
    }

    private static MathTransform CreateWgs84ToWebMercator()
    {
        ICoordinateTransformation transformation = new CoordinateTransformationFactory()
            .CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator);
        return transformation.MathTransform;
    }
}
