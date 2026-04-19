[CmdletBinding()]
param(
    [ValidateSet('Full', 'Smoke')]
    [string]$Mode = 'Full',

    [string]$ArtifactsPath = 'BenchmarkDotNet.Artifacts',

    [switch]$NoBuild
)

. "$PSScriptRoot/Get-CuratedBenchmarkConfiguration.ps1"

$projectPath = 'src/ProjNet.Benchmark/ProjNet.Benchmark.csproj'
$configuration = Get-CuratedBenchmarkConfiguration

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
    $arguments += '--curated'

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
        foreach ($run in $configuration.FullRuns)
        {
            Invoke-BenchmarkRun -Filters $run.Filters -Overrides $run.Overrides
        }
    }

    'Smoke'
    {
        foreach ($run in $configuration.SmokeRuns)
        {
            Invoke-BenchmarkRun -Filters $run.Filters -Overrides $run.Overrides
        }
    }
}
