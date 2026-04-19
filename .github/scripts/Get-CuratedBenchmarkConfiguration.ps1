function Get-CuratedBenchmarkConfiguration
{
    return @{
        FullRuns = @(
            @{
                Filters = @(
                    'ProjNet.Benchmark.CatalogFirstTransformationLookupBenchmarks.FirstCreateTransformation4326To3857',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseSimpleGeographicCs',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedCs',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseCompoundCs',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseGeodeticWkt2',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedWkt2',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseBoundWkt2'
                )
                Overrides = @()
            },
            @{
                Filters = @(
                    'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchMercator',
                    'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchUtm32N',
                    'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchLambert93',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorOneByOne(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm32NBatched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm31NBatched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Utm31NToWgs84Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToLambert93Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Lambert93ToWgs84Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.WebMercatorToWgs84Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatchedWithNoise(PointCount: 10000)',
                    'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToMercator',
                    'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToUtm32N',
                    'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformUtm32NToLambert93'
                )
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '3')
            }
        )
        SmokeRuns = @(
            @{
                Filters = @('ProjNet.Benchmark.CatalogFirstTransformationLookupBenchmarks.FirstCreateTransformation4326To3857')
                Overrides = @('--launchCount', '1', '--warmupCount', '0', '--iterationCount', '1')
            },
            @{
                Filters = @(
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseSimpleGeographicCs',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedCs',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseCompoundCs',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseGeodeticWkt2',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedWkt2',
                    'ProjNet.Benchmark.WktParsingBenchmarks.ParseBoundWkt2'
                )
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
            },
            @{
                Filters = @(
                    'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchMercator',
                    'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchUtm32N',
                    'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchLambert93',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorOneByOne(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm32NBatched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm31NBatched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Utm31NToWgs84Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToLambert93Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Lambert93ToWgs84Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.WebMercatorToWgs84Batched(PointCount: 10000)',
                    'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatchedWithNoise(PointCount: 10000)',
                    'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToMercator',
                    'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToUtm32N',
                    'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformUtm32NToLambert93'
                )
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
            }
        )
        RequiredDatasetPatterns = @(
            'ProjNet.Benchmark.CatalogFirstTransformationLookupBenchmarks.FirstCreateTransformation4326To3857',
            'ProjNet.Benchmark.WktParsingBenchmarks.ParseSimpleGeographicCs',
            'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedCs',
            'ProjNet.Benchmark.WktParsingBenchmarks.ParseCompoundCs',
            'ProjNet.Benchmark.WktParsingBenchmarks.ParseGeodeticWkt2',
            'ProjNet.Benchmark.WktParsingBenchmarks.ParseProjectedWkt2',
            'ProjNet.Benchmark.WktParsingBenchmarks.ParseBoundWkt2',
            'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchMercator',
            'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchUtm32N',
            'ProjNet.Benchmark.ProjectionTransformBenchmarks.TransformBatchLambert93',
            'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatched(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorOneByOne(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm32NBatched(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToUtm31NBatched(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.Utm31NToWgs84Batched(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToLambert93Batched(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.Lambert93ToWgs84Batched(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.WebMercatorToWgs84Batched(PointCount: 10000)',
            'ProjNet.Benchmark.ProjParityBenchmarks.Wgs84ToWebMercatorBatchedWithNoise(PointCount: 10000)',
            'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToMercator',
            'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformWgs84ToUtm32N',
            'ProjNet.Benchmark.TransformationFactoryBenchmarks.CreateTransformUtm32NToLambert93'
        )
    }
}
