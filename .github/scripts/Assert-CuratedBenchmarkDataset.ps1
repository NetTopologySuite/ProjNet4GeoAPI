[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DatasetPath
)

. "$PSScriptRoot/Get-CuratedBenchmarkConfiguration.ps1"

[object[]]$entries = Get-Content -Path $DatasetPath -Raw | ConvertFrom-Json
$entryNames = @($entries | ForEach-Object { $_.name } | Sort-Object)
$configuration = Get-CuratedBenchmarkConfiguration
$expectedNames = @($configuration.RequiredDatasetPatterns | Sort-Object)

if ($entryNames.Count -ne $expectedNames.Count)
{
    throw "Curated benchmark dataset contains $($entryNames.Count) entries but expected $($expectedNames.Count)."
}

$missingEntries = $expectedNames | Where-Object { $_ -notin $entryNames }
if ($missingEntries.Count -gt 0)
{
    throw "Curated benchmark dataset is missing expected entries: $($missingEntries -join ', ')."
}

$unexpectedEntries = $entryNames | Where-Object { $_ -notin $expectedNames }
if ($unexpectedEntries.Count -gt 0)
{
    throw "Curated benchmark dataset contains unexpected entries: $($unexpectedEntries -join ', ')."
}
