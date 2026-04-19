[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DatasetPath
)

. "$PSScriptRoot/Get-CuratedBenchmarkConfiguration.ps1"

[object[]]$entries = Get-Content -Path $DatasetPath -Raw | ConvertFrom-Json
$entryNames = $entries | ForEach-Object { $_.name }
$configuration = Get-CuratedBenchmarkConfiguration

foreach ($pattern in $configuration.RequiredDatasetPatterns)
{
    if (-not ($entryNames | Where-Object { $_ -like $pattern }))
    {
        throw "Curated benchmark dataset is missing entries that match '$pattern'."
    }
}
