function Get-CuratedBenchmarkConfiguration
{
    $catalogFirstBenchmarks = @(
        'ProjNet.Benchmark.CatalogFirstTransformationLookupBenchmarks.FirstCreateTransformation4326To3857'
    )

    $wktParsingBenchmarks = @(
        'ProjNet.Benchmark.WktParsingBenchmarks.ParseSimpleGeographicCs',
        'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedCs',
        'ProjNet.Benchmark.WktParsingBenchmarks.ParseCompoundCs',
        'ProjNet.Benchmark.WktParsingBenchmarks.ParseGeodeticWkt2',
        'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedWkt2',
        'ProjNet.Benchmark.WktParsingBenchmarks.ParseBoundWkt2'
    )

    $projectionTransformBenchmarks = @(
        'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchMercator',
        'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchUtm32N',
        'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchLambert93'
    )

    $projParityBenchmarks = @(
        'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatched(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorOneByOne(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm32NBatched(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm31NBatched(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.Utm31NToWgs84Batched(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToLambert93Batched(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.Lambert93ToWgs84Batched(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.WebMercatorToWgs84Batched(PointCount: 10000)',
        'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatchedWithNoise(PointCount: 10000)'
    )

    $transformationFactoryBenchmarks = @(
        'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToMercator',
        'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToUtm32N',
        'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformUtm32NToLambert93'
    )

    $allCuratedBenchmarks =
        $catalogFirstBenchmarks +
        $wktParsingBenchmarks +
        $projectionTransformBenchmarks +
        $projParityBenchmarks +
        $transformationFactoryBenchmarks

    return @{
        FullRuns = @(
            @{
                Filters = $catalogFirstBenchmarks + $wktParsingBenchmarks
                Overrides = @()
            },
            @{
                Filters = $projectionTransformBenchmarks + $projParityBenchmarks + $transformationFactoryBenchmarks
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '3')
            }
        )
        SmokeRuns = @(
            @{
                Filters = $catalogFirstBenchmarks
                Overrides = @('--launchCount', '1', '--warmupCount', '0', '--iterationCount', '1')
            },
            @{
                Filters = $wktParsingBenchmarks
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
            },
            @{
                Filters = $projectionTransformBenchmarks + $projParityBenchmarks + $transformationFactoryBenchmarks
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
            }
        )
        RequiredDatasetPatterns = $allCuratedBenchmarks
    }
}
