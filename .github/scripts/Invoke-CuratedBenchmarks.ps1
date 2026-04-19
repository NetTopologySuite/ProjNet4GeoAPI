[CmdletBinding()]
param(
    [ValidateSet('Full', 'Smoke')]
    [string]$Mode = 'Full',

    [string]$ArtifactsPath = 'BenchmarkDotNet.Artifacts',

    [switch]$NoBuild
)

$projectPath = 'src/ProjNet.Benchmark/ProjNet.Benchmark.csproj'

$catalogFirstFilter = '*CatalogFirstTransformationLookupBenchmarks*'
$wktParsingFilter = '*WktParsingBenchmarks*'
$projectionTransformFilters = @(
    '*ProjectionTransformBenchmarks.TransformBatchMercator*',
    '*ProjectionTransformBenchmarks.TransformBatchUtm32N*',
    '*ProjectionTransformBenchmarks.TransformBatchLambert93*'
)
$projParityFilter = '*ProjParityBenchmarks*'
$transformationFactoryFilter = '*TransformationFactoryBenchmarks*'

function Invoke-BenchmarkRun
{
    param(
        [string[]]$Filters,
        [string[]]$Overrides
    )

    $arguments = @('run', '-c', 'Release')
    if ($NoBuild)
    {
        $arguments += '--no-build'
    }

    $arguments += '--project', $projectPath, '--', '--artifacts', $ArtifactsPath, '--exporters', 'json'

    if ($Overrides.Count -gt 0)
    {
        $arguments += $Overrides
    }

    $arguments += '--filter'
    $arguments += $Filters

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "BenchmarkDotNet run failed for filters '$($Filters -join ', ')'."
    }
}

Remove-Item -Recurse -Force $ArtifactsPath -ErrorAction SilentlyContinue

switch ($Mode)
{
    'Full'
    {
        Invoke-BenchmarkRun -Filters @($catalogFirstFilter, $wktParsingFilter) -Overrides @()
        Invoke-BenchmarkRun -Filters ($projectionTransformFilters + @($projParityFilter, $transformationFactoryFilter)) -Overrides @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '3')
    }

    'Smoke'
    {
        Invoke-BenchmarkRun -Filters @($catalogFirstFilter) -Overrides @('--launchCount', '1', '--warmupCount', '0', '--iterationCount', '1')
        Invoke-BenchmarkRun -Filters @($wktParsingFilter) -Overrides @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
        Invoke-BenchmarkRun -Filters ($projectionTransformFilters + @($projParityFilter, $transformationFactoryFilter)) -Overrides @('--launchCount', '1', '--warmupCount', '1', '--iterationCount', '1')
    }
}
