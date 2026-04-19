function Get-CuratedBenchmarkConfiguration
{
    return @{
        FullRuns = @(
            @{
                Filters = @(
                    '*CatalogFirstTransformationLookupBenchmarks*',
                    '*WktParsingBenchmarks*'
                )
                Overrides = @()
            },
            @{
                Filters = @(
                    '*ProjectionTransformBenchmarks.TransformBatchMercator*',
                    '*ProjectionTransformBenchmarks.TransformBatchUtm32N*',
                    '*ProjectionTransformBenchmarks.TransformBatchLambert93*',
                    '*ProjParityBenchmarks*',
                    '*TransformationFactoryBenchmarks*'
                )
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '3')
            }
        )
        SmokeRuns = @(
            @{
                Filters = @('*CatalogFirstTransformationLookupBenchmarks*')
                Overrides = @('--launchCount', '1', '--warmupCount', '0', '--iterationCount', '1')
            },
            @{
                Filters = @('*WktParsingBenchmarks*')
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
            },
            @{
                Filters = @(
                    '*ProjectionTransformBenchmarks.TransformBatchMercator*',
                    '*ProjectionTransformBenchmarks.TransformBatchUtm32N*',
                    '*ProjectionTransformBenchmarks.TransformBatchLambert93*',
                    '*ProjParityBenchmarks*',
                    '*TransformationFactoryBenchmarks*'
                )
                Overrides = @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
            }
        )
        RequiredDatasetPatterns = @(
            '*CatalogFirstTransformationLookupBenchmarks.FirstCreateTransformation4326To3857*',
            '*WktParsingBenchmarks*',
            '*ProjectionTransformBenchmarks.TransformBatchMercator*',
            '*ProjectionTransformBenchmarks.TransformBatchUtm32N*',
            '*ProjectionTransformBenchmarks.TransformBatchLambert93*',
            '*ProjParityBenchmarks*',
            '*TransformationFactoryBenchmarks*'
        )
    }
}
