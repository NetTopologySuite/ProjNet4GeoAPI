// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures single-point array-transform throughput for projection paths touched by the GIE failure-coverage fixes.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
public class ProjectionSinglePointBenchmarks
{
    private MathTransform guyouForward = null!;
    private MathTransform peirceForward = null!;
    private MathTransform adamsHemisphereForward = null!;
    private MathTransform obliqueMercatorForward = null!;
    private MathTransform obliqueMercatorNoRotationForward = null!;
    private MathTransform krovakForward = null!;
    private MathTransform iseaPolarForward = null!;
    private MathTransform lagrangeForward = null!;
    private MathTransform loximuthalForward = null!;
    private MathTransform mercatorForward = null!;
    private MathTransform nzmgForward = null!;
    private MathTransform orthographicLocalForward = null!;
    private MathTransform s2Forward = null!;
    private MathTransform healpixRotatedForward = null!;
    private MathTransform rhealpixForward = null!;
    private MathTransform transverseMercatorExactForward = null!;
    private MathTransform transverseMercatorApproxForward = null!;
    private MathTransform stereographicPolarForward = null!;
    private MathTransform stereographicObliqueForward = null!;
    private MathTransform stereographicEquatorialForward = null!;
    private MathTransform vanDerGrintenOverForward = null!;
    private MathTransform laeaInverse = null!;
    private MathTransform iseaPolarInverse = null!;
    private MathTransform orthographicInverse = null!;
    private MathTransform robinsonInverse = null!;
    private MathTransform krovakInverse = null!;
    private MathTransform lagrangeInverse = null!;
    private MathTransform loximuthalInverse = null!;
    private MathTransform mercatorInverse = null!;
    private MathTransform nzmgInverse = null!;
    private MathTransform s2Inverse = null!;
    private MathTransform healpixRotatedInverse = null!;
    private MathTransform rhealpixInverse = null!;
    private MathTransform transverseMercatorExactInverse = null!;
    private MathTransform transverseMercatorApproxInverse = null!;
    private MathTransform stereographicPolarInverse = null!;
    private MathTransform stereographicObliqueInverse = null!;
    private MathTransform stereographicEquatorialInverse = null!;
    private MathTransform vanDerGrintenOverInverse = null!;

    private double[] guyouInput = null!;
    private double[] peirceInput = null!;
    private double[] adamsHemisphereInput = null!;
    private double[] obliqueMercatorInput = null!;
    private double[] krovakInput = null!;
    private double[] iseaPolarInput = null!;
    private double[] lagrangeInput = null!;
    private double[] loximuthalInput = null!;
    private double[] mercatorInput = null!;
    private double[] nzmgInput = null!;
    private double[] orthographicLocalInput = null!;
    private double[] s2Input = null!;
    private double[] healpixRotatedInput = null!;
    private double[] rhealpixInput = null!;
    private double[] transverseMercatorInput = null!;
    private double[] stereographicPolarInput = null!;
    private double[] stereographicObliqueInput = null!;
    private double[] stereographicEquatorialInput = null!;
    private double[] vanDerGrintenOverInput = null!;
    private double[] iseaPolarInverseInput = null!;
    private double[] krovakInverseInput = null!;
    private double[] lagrangeInverseInput = null!;
    private double[] laeaInput = null!;
    private double[] loximuthalInverseInput = null!;
    private double[] orthographicInput = null!;
    private double[] robinsonInput = null!;
    private double[] mercatorInverseInput = null!;
    private double[] nzmgInverseInput = null!;
    private double[] s2InverseInput = null!;
    private double[] healpixRotatedInverseInput = null!;
    private double[] rhealpixInverseInput = null!;
    private double[] transverseMercatorExactInverseInput = null!;
    private double[] transverseMercatorApproxInverseInput = null!;
    private double[] stereographicPolarInverseInput = null!;
    private double[] stereographicObliqueInverseInput = null!;
    private double[] stereographicEquatorialInverseInput = null!;
    private double[] vanDerGrintenOverInverseInput = null!;

    /// <summary>
    /// Executes one pass of every single-point projection benchmark and validates that the results stay finite.
    /// </summary>
    public static void Validate()
    {
        var benchmark = new ProjectionSinglePointBenchmarks();
        benchmark.GlobalSetup();

        EnsureFinite(benchmark.TransformGuyouSinglePoint());
        EnsureFinite(benchmark.TransformPeirceQuincuncialSinglePoint());
        EnsureFinite(benchmark.TransformAdamsHemisphereSinglePoint());
        EnsureFinite(benchmark.TransformObliqueMercatorSinglePoint());
        EnsureFinite(benchmark.TransformObliqueMercatorNoRotationSinglePoint());
        EnsureFinite(benchmark.TransformKrovakSinglePoint());
        EnsureFinite(benchmark.TransformIseaPolarSinglePoint());
        EnsureFinite(benchmark.TransformLagrangeSinglePoint());
        EnsureFinite(benchmark.TransformLoximuthalSinglePoint());
        EnsureFinite(benchmark.TransformMercatorSinglePoint());
        EnsureFinite(benchmark.TransformNzmgSinglePoint());
        EnsureFinite(benchmark.TransformOrthographicLocalSinglePoint());
        EnsureFinite(benchmark.TransformS2SinglePoint());
        EnsureFinite(benchmark.TransformHealpixRotatedSinglePoint());
        EnsureFinite(benchmark.TransformRhealpixSinglePoint());
        EnsureFinite(benchmark.TransformTransverseMercatorExactSinglePoint());
        EnsureFinite(benchmark.TransformTransverseMercatorApproxSinglePoint());
        EnsureFinite(benchmark.TransformStereographicPolarSinglePoint());
        EnsureFinite(benchmark.TransformStereographicObliqueSinglePoint());
        EnsureFinite(benchmark.TransformStereographicEquatorialSinglePoint());
        EnsureFinite(benchmark.TransformVanDerGrintenOverSinglePoint());
        EnsureFinite(benchmark.TransformLambertAzimuthalEqualAreaInverseSinglePoint());
        EnsureFinite(benchmark.TransformIseaPolarInverseSinglePoint());
        EnsureFinite(benchmark.TransformKrovakInverseSinglePoint());
        EnsureFinite(benchmark.TransformLagrangeInverseSinglePoint());
        EnsureFinite(benchmark.TransformLoximuthalInverseSinglePoint());
        EnsureFinite(benchmark.TransformOrthographicInverseSinglePoint());
        EnsureFinite(benchmark.TransformRobinsonInverseSinglePoint());
        EnsureFinite(benchmark.TransformMercatorInverseSinglePoint());
        EnsureFinite(benchmark.TransformNzmgInverseSinglePoint());
        EnsureFinite(benchmark.TransformS2InverseSinglePoint());
        EnsureFinite(benchmark.TransformHealpixRotatedInverseSinglePoint());
        EnsureFinite(benchmark.TransformRhealpixInverseSinglePoint());
        EnsureFinite(benchmark.TransformTransverseMercatorExactInverseSinglePoint());
        EnsureFinite(benchmark.TransformTransverseMercatorApproxInverseSinglePoint());
        EnsureFinite(benchmark.TransformStereographicPolarInverseSinglePoint());
        EnsureFinite(benchmark.TransformStereographicObliqueInverseSinglePoint());
        EnsureFinite(benchmark.TransformStereographicEquatorialInverseSinglePoint());
        EnsureFinite(benchmark.TransformVanDerGrintenOverInverseSinglePoint());
    }

    /// <summary>
    /// Creates the projection transforms and representative valid source coordinates used by the benchmarks.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.guyouForward = BenchmarkPipelineTransformFactory.Create("+proj=guyou");
        this.peirceForward = BenchmarkPipelineTransformFactory.Create("+proj=peirce_q +shape=square");
        this.adamsHemisphereForward = BenchmarkPipelineTransformFactory.Create("+proj=adams_hemi");
        this.obliqueMercatorForward = BenchmarkPipelineTransformFactory.Create("+proj=omerc +ellps=GRS80 +lat_1=0.5 +lat_2=2");
        this.obliqueMercatorNoRotationForward = BenchmarkPipelineTransformFactory.Create("+proj=omerc +ellps=GRS80 +lat_1=0.5 +lat_2=2 +no_rot");
        this.krovakForward = BenchmarkPipelineTransformFactory.Create("+proj=krovak +ellps=GRS80");
        this.iseaPolarForward = BenchmarkPipelineTransformFactory.Create("+proj=isea +R=6371007.18091875 +orient=pole");
        this.lagrangeForward = BenchmarkPipelineTransformFactory.Create("+proj=lagrng +a=6400000 +W=2 +lat_1=0.5");
        this.loximuthalForward = BenchmarkPipelineTransformFactory.Create("+proj=loxim +a=6400000 +lat_1=0.5 +lat_2=2");
        this.mercatorForward = BenchmarkPipelineTransformFactory.Create("+proj=merc +ellps=GRS80");
        this.nzmgForward = BenchmarkPipelineTransformFactory.Create("+proj=nzmg +ellps=GRS80");
        this.orthographicLocalForward = BenchmarkPipelineTransformFactory.Create("+proj=ortho +lat_0=37.628969166666664 +lon_0=-122.39394166666668 +k_0=0.9999968 +alpha=27.7927777777777 +ellps=GRS80");
        this.s2Forward = BenchmarkPipelineTransformFactory.Create("+proj=s2 +ellps=WGS84 +lat_0=90 +UVtoST=tangent");
        this.healpixRotatedForward = BenchmarkPipelineTransformFactory.Create("+proj=healpix +R=6400000 +rot_xy=42");
        this.rhealpixForward = BenchmarkPipelineTransformFactory.Create("+proj=rhealpix +south_square=2 +north_square=3 +ellps=WGS84");
        this.transverseMercatorExactForward = BenchmarkPipelineTransformFactory.Create("+proj=tmerc +ellps=GRS80");
        this.transverseMercatorApproxForward = BenchmarkPipelineTransformFactory.Create("+proj=tmerc +ellps=GRS80 +approx");
        this.stereographicPolarForward = BenchmarkPipelineTransformFactory.Create("+proj=stere +ellps=GRS80 +lat_0=90 +lat_ts=70");
        this.stereographicObliqueForward = BenchmarkPipelineTransformFactory.Create("+proj=stere +ellps=GRS80 +lat_0=45");
        this.stereographicEquatorialForward = BenchmarkPipelineTransformFactory.Create("+proj=stere +ellps=GRS80 +lat_0=0");
        this.vanDerGrintenOverForward = BenchmarkPipelineTransformFactory.Create("+proj=vandg +a=6400000 +over");

        MathTransform laeaForward = BenchmarkPipelineTransformFactory.Create("+proj=laea +R=6371000 +lat_0=45");
        MathTransform orthographicForward = BenchmarkPipelineTransformFactory.Create("+proj=ortho +ellps=WGS84 +lat_0=30");
        MathTransform robinsonForward = BenchmarkPipelineTransformFactory.Create("+proj=robin +a=6400000");

        this.laeaInverse = laeaForward.Inverse();
        this.iseaPolarInverse = this.iseaPolarForward.Inverse();
        this.krovakInverse = this.krovakForward.Inverse();
        this.lagrangeInverse = this.lagrangeForward.Inverse();
        this.loximuthalInverse = this.loximuthalForward.Inverse();
        this.orthographicInverse = orthographicForward.Inverse();
        this.robinsonInverse = robinsonForward.Inverse();
        this.mercatorInverse = this.mercatorForward.Inverse();
        this.nzmgInverse = this.nzmgForward.Inverse();
        this.s2Inverse = this.s2Forward.Inverse();
        this.healpixRotatedInverse = this.healpixRotatedForward.Inverse();
        this.rhealpixInverse = this.rhealpixForward.Inverse();
        this.transverseMercatorExactInverse = this.transverseMercatorExactForward.Inverse();
        this.transverseMercatorApproxInverse = this.transverseMercatorApproxForward.Inverse();
        this.stereographicPolarInverse = this.stereographicPolarForward.Inverse();
        this.stereographicObliqueInverse = this.stereographicObliqueForward.Inverse();
        this.stereographicEquatorialInverse = this.stereographicEquatorialForward.Inverse();
        this.vanDerGrintenOverInverse = this.vanDerGrintenOverForward.Inverse();

        this.guyouInput = [12d, 25d];
        this.peirceInput = [-15d, 35d];
        this.adamsHemisphereInput = [40d, 30d];
        this.obliqueMercatorInput = [2d, 1d];
        this.krovakInput = [2d, 1d];
        this.iseaPolarInput = [0d, 45d];
        this.lagrangeInput = [2d, 1d];
        this.loximuthalInput = [2d, 1d];
        this.mercatorInput = [18d, -85d];
        this.nzmgInput = [2d, 1d];
        this.orthographicLocalInput = [-122.3846388888889d, 37.62607694444444d];
        this.s2Input = [20d, 70.12337013762532d];
        this.healpixRotatedInput = [2d, 1d];
        this.rhealpixInput = [45d, 50d];
        this.transverseMercatorInput = [44.69d, 35.37d];
        this.stereographicPolarInput = [15d, 80d];
        this.stereographicObliqueInput = [12d, 50d];
        this.stereographicEquatorialInput = [18d, 25d];
        this.vanDerGrintenOverInput = [180.1d, 50d];
        this.iseaPolarInverseInput = this.iseaPolarForward.Transform(this.iseaPolarInput);
        this.krovakInverseInput = [200d, 100d];
        this.lagrangeInverseInput = this.lagrangeForward.Transform(this.lagrangeInput);
        this.laeaInput = laeaForward.Transform([15d, 20d]);
        this.loximuthalInverseInput = [200d, 100d];
        this.orthographicInput = orthographicForward.Transform([20d, 40d]);
        this.robinsonInput = robinsonForward.Transform([30d, 12d]);
        this.mercatorInverseInput = this.mercatorForward.Transform(this.mercatorInput);
        this.nzmgInverseInput = [200000d, 100000d];
        this.s2InverseInput = [0.29020309743436806d, 0.4211558922141421d];
        this.healpixRotatedInverseInput = this.healpixRotatedForward.Transform(this.healpixRotatedInput);
        this.rhealpixInverseInput = this.rhealpixForward.Transform(this.rhealpixInput);
        this.transverseMercatorExactInverseInput = this.transverseMercatorExactForward.Transform(this.transverseMercatorInput);
        this.transverseMercatorApproxInverseInput = this.transverseMercatorApproxForward.Transform(this.transverseMercatorInput);
        this.stereographicPolarInverseInput = this.stereographicPolarForward.Transform(this.stereographicPolarInput);
        this.stereographicObliqueInverseInput = this.stereographicObliqueForward.Transform(this.stereographicObliqueInput);
        this.stereographicEquatorialInverseInput = this.stereographicEquatorialForward.Transform(this.stereographicEquatorialInput);
        this.vanDerGrintenOverInverseInput = this.vanDerGrintenOverForward.Transform(this.vanDerGrintenOverInput);
    }

    /// <summary>
    /// Measures single-point forward throughput for the Guyou projection.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark(Baseline = true)]
    public double[] TransformGuyouSinglePoint() => this.guyouForward.Transform(this.guyouInput);

    /// <summary>
    /// Measures single-point forward throughput for the Peirce quincuncial projection.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformPeirceQuincuncialSinglePoint() => this.peirceForward.Transform(this.peirceInput);

    /// <summary>
    /// Measures single-point forward throughput for the Adams hemisphere-in-a-square projection.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformAdamsHemisphereSinglePoint() => this.adamsHemisphereForward.Transform(this.adamsHemisphereInput);

    /// <summary>
    /// Measures single-point forward throughput for Hotine oblique Mercator in the default rotated-grid mode.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformObliqueMercatorSinglePoint() => this.obliqueMercatorForward.Transform(this.obliqueMercatorInput);

    /// <summary>
    /// Measures single-point forward throughput for Hotine oblique Mercator with <c>+no_rot</c>.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformObliqueMercatorNoRotationSinglePoint() => this.obliqueMercatorNoRotationForward.Transform(this.obliqueMercatorInput);

    /// <summary>
    /// Measures single-point forward throughput for default-parameter Krovak.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformKrovakSinglePoint() => this.krovakForward.Transform(this.krovakInput);

    /// <summary>
    /// Measures single-point forward throughput for polar-oriented ISEA.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformIseaPolarSinglePoint() => this.iseaPolarForward.Transform(this.iseaPolarInput);

    /// <summary>
    /// Measures single-point forward throughput for spherical Lagrange using PROJ <c>lat_1</c> and <c>W</c>.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformLagrangeSinglePoint() => this.lagrangeForward.Transform(this.lagrangeInput);

    /// <summary>
    /// Measures single-point forward throughput for Loximuthal using the PROJ <c>lat_1</c> binding.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformLoximuthalSinglePoint() => this.loximuthalForward.Transform(this.loximuthalInput);

    /// <summary>
    /// Measures single-point forward throughput for ellipsoidal Mercator on a high-latitude input.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformMercatorSinglePoint() => this.mercatorForward.Transform(this.mercatorInput);

    /// <summary>
    /// Measures single-point forward throughput for New Zealand Map Grid with PROJ default origin and offsets.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformNzmgSinglePoint() => this.nzmgForward.Transform(this.nzmgInput);

    /// <summary>
    /// Measures single-point forward throughput for local ellipsoidal Orthographic with <c>alpha</c>.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformOrthographicLocalSinglePoint() => this.orthographicLocalForward.Transform(this.orthographicLocalInput);

    /// <summary>
    /// Measures single-point forward throughput for <c>s2</c> using tangent UV-to-ST mapping.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformS2SinglePoint() => this.s2Forward.Transform(this.s2Input);

    /// <summary>
    /// Measures single-point forward throughput for rotated spherical HEALPix.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformHealpixRotatedSinglePoint() => this.healpixRotatedForward.Transform(this.healpixRotatedInput);

    /// <summary>
    /// Measures single-point forward throughput for ellipsoidal rHEALPix with explicit polar-square placement.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformRhealpixSinglePoint() => this.rhealpixForward.Transform(this.rhealpixInput);

    /// <summary>
    /// Measures single-point forward throughput for exact ellipsoidal transverse Mercator.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformTransverseMercatorExactSinglePoint() => this.transverseMercatorExactForward.Transform(this.transverseMercatorInput);

    /// <summary>
    /// Measures single-point forward throughput for the Snyder approximate transverse Mercator path.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformTransverseMercatorApproxSinglePoint() => this.transverseMercatorApproxForward.Transform(this.transverseMercatorInput);

    /// <summary>
    /// Measures single-point forward throughput for ellipsoidal polar stereographic.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformStereographicPolarSinglePoint() => this.stereographicPolarForward.Transform(this.stereographicPolarInput);

    /// <summary>
    /// Measures single-point forward throughput for ellipsoidal oblique stereographic.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformStereographicObliqueSinglePoint() => this.stereographicObliqueForward.Transform(this.stereographicObliqueInput);

    /// <summary>
    /// Measures single-point forward throughput for ellipsoidal equatorial stereographic.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformStereographicEquatorialSinglePoint() => this.stereographicEquatorialForward.Transform(this.stereographicEquatorialInput);

    /// <summary>
    /// Measures single-point forward throughput for van der Grinten with <c>+over</c>.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformVanDerGrintenOverSinglePoint() => this.vanDerGrintenOverForward.Transform(this.vanDerGrintenOverInput);

    /// <summary>
    /// Measures single-point inverse throughput for spherical Lambert azimuthal equal area.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformLambertAzimuthalEqualAreaInverseSinglePoint() => this.laeaInverse.Transform(this.laeaInput);

    /// <summary>
    /// Measures single-point inverse throughput for polar-oriented ISEA.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformIseaPolarInverseSinglePoint() => this.iseaPolarInverse.Transform(this.iseaPolarInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for default-parameter Krovak.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformKrovakInverseSinglePoint() => this.krovakInverse.Transform(this.krovakInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for spherical Lagrange using PROJ <c>lat_1</c> and <c>W</c>.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformLagrangeInverseSinglePoint() => this.lagrangeInverse.Transform(this.lagrangeInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for Loximuthal using the PROJ <c>lat_1</c> binding.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformLoximuthalInverseSinglePoint() => this.loximuthalInverse.Transform(this.loximuthalInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for oblique ellipsoidal Orthographic.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformOrthographicInverseSinglePoint() => this.orthographicInverse.Transform(this.orthographicInput);

    /// <summary>
    /// Measures single-point inverse throughput for Robinson.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformRobinsonInverseSinglePoint() => this.robinsonInverse.Transform(this.robinsonInput);

    /// <summary>
    /// Measures single-point inverse throughput for ellipsoidal Mercator on a high-latitude input.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformMercatorInverseSinglePoint() => this.mercatorInverse.Transform(this.mercatorInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for New Zealand Map Grid with PROJ default origin and offsets.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformNzmgInverseSinglePoint() => this.nzmgInverse.Transform(this.nzmgInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for <c>s2</c> using tangent UV-to-ST mapping.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformS2InverseSinglePoint() => this.s2Inverse.Transform(this.s2InverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for rotated spherical HEALPix.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformHealpixRotatedInverseSinglePoint() => this.healpixRotatedInverse.Transform(this.healpixRotatedInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for ellipsoidal rHEALPix with explicit polar-square placement.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformRhealpixInverseSinglePoint() => this.rhealpixInverse.Transform(this.rhealpixInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for exact ellipsoidal transverse Mercator.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformTransverseMercatorExactInverseSinglePoint() => this.transverseMercatorExactInverse.Transform(this.transverseMercatorExactInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for the Snyder approximate transverse Mercator path.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformTransverseMercatorApproxInverseSinglePoint() => this.transverseMercatorApproxInverse.Transform(this.transverseMercatorApproxInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for ellipsoidal polar stereographic.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformStereographicPolarInverseSinglePoint() => this.stereographicPolarInverse.Transform(this.stereographicPolarInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for ellipsoidal oblique stereographic.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformStereographicObliqueInverseSinglePoint() => this.stereographicObliqueInverse.Transform(this.stereographicObliqueInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for ellipsoidal equatorial stereographic.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformStereographicEquatorialInverseSinglePoint() => this.stereographicEquatorialInverse.Transform(this.stereographicEquatorialInverseInput);

    /// <summary>
    /// Measures single-point inverse throughput for van der Grinten with <c>+over</c>.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformVanDerGrintenOverInverseSinglePoint() => this.vanDerGrintenOverInverse.Transform(this.vanDerGrintenOverInverseInput);

    private static void EnsureFinite(double[] coordinates)
    {
        for (int i = 0; i < coordinates.Length; i++)
        {
            if (double.IsNaN(coordinates[i]) || double.IsInfinity(coordinates[i]))
            {
                throw new InvalidOperationException("Benchmark validation failed: transform produced non-finite values.");
            }
        }
    }
}
