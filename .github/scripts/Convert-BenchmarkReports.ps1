param(
    [Parameter(Mandatory = $true)]
    [string]$InputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$reportFiles = Get-ChildItem -Path $InputDirectory -Filter '*-report-full-compressed.json' -File | Sort-Object Name
if ($reportFiles.Count -eq 0)
{
    throw "No BenchmarkDotNet JSON reports were found in '$InputDirectory'."
}

$entries = foreach ($reportFile in $reportFiles)
{
    $report = Get-Content -Path $reportFile.FullName -Raw | ConvertFrom-Json
    foreach ($benchmark in @($report.Benchmarks))
    {
        if ($null -eq $benchmark.Statistics)
        {
            throw "Benchmark '$($benchmark.FullName)' did not produce statistics."
        }

        $allocatedMetric = @($benchmark.Metrics) |
            Where-Object { $_.Descriptor.Id -eq 'Allocated Memory' } |
            Select-Object -First 1

        $extraLines = @(
            "Type: $($benchmark.Type)",
            "Runtime: $($report.HostEnvironmentInfo.RuntimeVersion)",
            "Mean: $([double]$benchmark.Statistics.Mean) ns",
            "StdDev: $([double]$benchmark.Statistics.StandardDeviation) ns"
        )

        if ($null -ne $allocatedMetric)
        {
            $extraLines += "Allocated: $([double]$allocatedMetric.Value) B/op"
        }

        [pscustomobject]@{
            name = $benchmark.FullName
            unit = 'ns'
            value = [double]$benchmark.Statistics.Mean
            range = [string]([math]::Round([double]$benchmark.Statistics.StandardDeviation, 4))
            extra = $extraLines -join [Environment]::NewLine
        }
    }
}

$outputDirectory = Split-Path -Parent $OutputFile
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory))
{
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$sortedEntries = @($entries | Sort-Object name)

$sortedEntries |
    ConvertTo-Json -Depth 5 -AsArray |
    Set-Content -Path $OutputFile -Encoding utf8NoBOM

Write-Host "Wrote $($sortedEntries.Count) benchmark entries to '$OutputFile'."
