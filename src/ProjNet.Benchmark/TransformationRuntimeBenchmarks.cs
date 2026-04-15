// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures runtime throughput for representative non-projection transformation implementations touched by the M104 exception audit.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
public class TransformationRuntimeBenchmarks
{
    private const int PointCount = 10_000;
    private const string MolodenskyOperation = "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149 +abridged";
    private const string HornerOperation = "+proj=horner +ellps=intl +range=10000000 +fwd_origin=4.94690026817276e+05,6.13342113183056e+06 +deg=3 +fwd_c=6.13258562111350e+06,6.19480105709997e+05,9.99378966275206e-01,-2.82153291753490e-02,-2.27089979140026e-10,-1.77019590701470e-09,1.08522286274070e-14,2.11430298751604e-15";

    private static readonly MethodInfo CreatePipelineTransformMethod = ResolveCreatePipelineTransformMethod();

    private MathTransform molodenskyTransform = null!;
    private MathTransform hornerTransform = null!;
    private MathTransform horizontalGridShiftTransform = null!;
    private MathTransform verticalGridShiftTransform = null!;

    private double[] molodenskyXs = [];
    private double[] molodenskyYs = [];
    private double[] molodenskyZs = [];
    private double[] molodenskyWorkXs = [];
    private double[] molodenskyWorkYs = [];
    private double[] molodenskyWorkZs = [];

    private double[] hornerXs = [];
    private double[] hornerYs = [];
    private double[] hornerZs = [];
    private double[] hornerWorkXs = [];
    private double[] hornerWorkYs = [];
    private double[] hornerWorkZs = [];

    private double[] horizontalGridShiftXs = [];
    private double[] horizontalGridShiftYs = [];
    private double[] horizontalGridShiftZs = [];
    private double[] horizontalGridShiftWorkXs = [];
    private double[] horizontalGridShiftWorkYs = [];
    private double[] horizontalGridShiftWorkZs = [];

    private double[] verticalGridShiftXs = [];
    private double[] verticalGridShiftYs = [];
    private double[] verticalGridShiftZs = [];
    private double[] verticalGridShiftWorkXs = [];
    private double[] verticalGridShiftWorkYs = [];
    private double[] verticalGridShiftWorkZs = [];

    /// <summary>
    /// Creates representative transforms and deterministic benchmark inputs.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.molodenskyTransform = CreatePipelineTransform(MolodenskyOperation);
        this.hornerTransform = CreatePipelineTransform(HornerOperation);
        this.horizontalGridShiftTransform = CreatePipelineTransform(FormattableString.Invariant($"+proj=hgridshift +grids={BenchmarkFixtureResolver.ResolveGridPath("test_hgrid_little_endian.gsb")}"));
        this.verticalGridShiftTransform = CreatePipelineTransform(FormattableString.Invariant($"+proj=vgridshift +grids={BenchmarkFixtureResolver.ResolveGridPath("egm96_15.gtx")}"));

        (this.molodenskyXs, this.molodenskyYs, this.molodenskyZs) = CreateMolodenskySource();
        (this.hornerXs, this.hornerYs, this.hornerZs) = CreateHornerSource();
        (this.horizontalGridShiftXs, this.horizontalGridShiftYs, this.horizontalGridShiftZs) = CreateConstantSource(4.5d, 52.5d, 0d);
        (this.verticalGridShiftXs, this.verticalGridShiftYs, this.verticalGridShiftZs) = CreateConstantSource(12d, 56d, 0d);

        this.molodenskyWorkXs = new double[PointCount];
        this.molodenskyWorkYs = new double[PointCount];
        this.molodenskyWorkZs = new double[PointCount];

        this.hornerWorkXs = new double[PointCount];
        this.hornerWorkYs = new double[PointCount];
        this.hornerWorkZs = new double[PointCount];

        this.horizontalGridShiftWorkXs = new double[PointCount];
        this.horizontalGridShiftWorkYs = new double[PointCount];
        this.horizontalGridShiftWorkZs = new double[PointCount];

        this.verticalGridShiftWorkXs = new double[PointCount];
        this.verticalGridShiftWorkYs = new double[PointCount];
        this.verticalGridShiftWorkZs = new double[PointCount];
    }

    /// <summary>
    /// Measures batched Molodensky runtime throughput.
    /// </summary>
    [Benchmark]
    public void TransformBatchMolodensky()
    {
        PrepareInput(this.molodenskyXs, this.molodenskyYs, this.molodenskyZs, this.molodenskyWorkXs, this.molodenskyWorkYs, this.molodenskyWorkZs);
        this.molodenskyTransform.Transform(this.molodenskyWorkXs, this.molodenskyWorkYs, this.molodenskyWorkZs);
    }

    /// <summary>
    /// Measures batched Horner runtime throughput.
    /// </summary>
    [Benchmark]
    public void TransformBatchHorner()
    {
        PrepareInput(this.hornerXs, this.hornerYs, this.hornerZs, this.hornerWorkXs, this.hornerWorkYs, this.hornerWorkZs);
        this.hornerTransform.Transform(this.hornerWorkXs, this.hornerWorkYs, this.hornerWorkZs);
    }

    /// <summary>
    /// Measures batched NTv2 horizontal grid-shift throughput.
    /// </summary>
    [Benchmark]
    public void TransformBatchHorizontalGridShift()
    {
        PrepareInput(
            this.horizontalGridShiftXs,
            this.horizontalGridShiftYs,
            this.horizontalGridShiftZs,
            this.horizontalGridShiftWorkXs,
            this.horizontalGridShiftWorkYs,
            this.horizontalGridShiftWorkZs);
        this.horizontalGridShiftTransform.Transform(
            this.horizontalGridShiftWorkXs,
            this.horizontalGridShiftWorkYs,
            this.horizontalGridShiftWorkZs);
    }

    /// <summary>
    /// Measures batched GTX vertical grid-shift throughput.
    /// </summary>
    [Benchmark]
    public void TransformBatchVerticalGridShift()
    {
        PrepareInput(
            this.verticalGridShiftXs,
            this.verticalGridShiftYs,
            this.verticalGridShiftZs,
            this.verticalGridShiftWorkXs,
            this.verticalGridShiftWorkYs,
            this.verticalGridShiftWorkZs);
        this.verticalGridShiftTransform.Transform(
            this.verticalGridShiftWorkXs,
            this.verticalGridShiftWorkYs,
            this.verticalGridShiftWorkZs);
    }

    private static void PrepareInput(
        double[] sourceXs,
        double[] sourceYs,
        double[] sourceZs,
        double[] workXs,
        double[] workYs,
        double[] workZs)
    {
        sourceXs.CopyTo(workXs.AsSpan());
        sourceYs.CopyTo(workYs.AsSpan());
        sourceZs.CopyTo(workZs.AsSpan());
    }

    private static (double[] Xs, double[] Ys, double[] Zs) CreateMolodenskySource()
    {
        double[] xs = new double[PointCount];
        double[] ys = new double[PointCount];
        double[] zs = new double[PointCount];
        for (int i = 0; i < PointCount; i++)
        {
            xs[i] = 144.75d + ((i % 128) * 1e-3d);
            ys[i] = -37.95d + ((i % 96) * 1e-3d);
            zs[i] = 25d + (i % 32);
        }

        return (xs, ys, zs);
    }

    private static (double[] Xs, double[] Ys, double[] Zs) CreateHornerSource()
    {
        double[] xs = new double[PointCount];
        double[] ys = new double[PointCount];
        double[] zs = new double[PointCount];
        for (int i = 0; i < PointCount; i++)
        {
            xs[i] = 495000d + (i % 512);
            ys[i] = 6130500d + ((i % 512) * 0.5d);
            zs[i] = 0d;
        }

        return (xs, ys, zs);
    }

    private static (double[] Xs, double[] Ys, double[] Zs) CreateConstantSource(double x, double y, double z)
    {
        double[] xs = new double[PointCount];
        double[] ys = new double[PointCount];
        double[] zs = new double[PointCount];
        Array.Fill(xs, x);
        Array.Fill(ys, y);
        Array.Fill(zs, z);
        return (xs, ys, zs);
    }

    private static MathTransform CreatePipelineTransform(string operation)
    {
        object?[] arguments = [operation, null, null];
        bool ok = (bool)(CreatePipelineTransformMethod.Invoke(null, arguments) ?? false);
        if (!ok)
        {
            throw new InvalidOperationException(arguments[2] as string ?? "Pipeline transform creation failed.");
        }

        return arguments[1] as MathTransform
            ?? throw new InvalidOperationException("Pipeline transform factory returned null transform.");
    }

    private static MethodInfo ResolveCreatePipelineTransformMethod()
    {
        Type pipelineFactoryType = typeof(MathTransform).Assembly.GetType(
            "ProjNet.CoordinateSystems.Transformations.ProjPipelineMathTransformFactory",
            throwOnError: true)
            ?? throw new InvalidOperationException("Unable to resolve ProjPipelineMathTransformFactory type.");

        return pipelineFactoryType.GetMethod(
            "TryCreateMathTransform",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(string),
                typeof(MathTransform).MakeByRefType(),
                typeof(string).MakeByRefType(),
            ],
            modifiers: null)
            ?? throw new InvalidOperationException("Unable to resolve ProjPipelineMathTransformFactory.TryCreateMathTransform.");
    }
}
