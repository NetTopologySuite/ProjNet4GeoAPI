[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DatasetPath
)

. "$PSScriptRoot/Get-CuratedBenchmarkConfiguration.ps1"

$doubleStyles = [System.Globalization.NumberStyles]::Float -bor [System.Globalization.NumberStyles]::AllowThousands
$culture = [System.Globalization.CultureInfo]::InvariantCulture

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

foreach ($entry in $entries)
{
    if ([string]::IsNullOrWhiteSpace($entry.name))
    {
        throw 'Curated benchmark dataset contains an entry without a valid name.'
    }

    if ($entry.unit -ne 'ns')
    {
        throw "Curated benchmark dataset entry '$($entry.name)' does not use the expected 'ns' unit."
    }

    $value = 0d
    if ($null -eq $entry.value -or -not [double]::TryParse([string]$entry.value, $doubleStyles, $culture, [ref]$value))
    {
        throw "Curated benchmark dataset entry '$($entry.name)' does not contain a valid numeric value."
    }

    if ([double]::IsNaN($value) -or [double]::IsInfinity($value) -or $value -lt 0)
    {
        throw "Curated benchmark dataset entry '$($entry.name)' does not contain a finite non-negative numeric value."
    }

    $range = 0d
    if ($null -eq $entry.range -or -not [double]::TryParse([string]$entry.range, $doubleStyles, $culture, [ref]$range))
    {
        throw "Curated benchmark dataset entry '$($entry.name)' does not contain a valid numeric range."
    }

    if ([double]::IsNaN($range) -or [double]::IsInfinity($range) -or $range -lt 0)
    {
        throw "Curated benchmark dataset entry '$($entry.name)' does not contain a finite non-negative numeric range."
    }

    if ($entry.extra -isnot [string] -or [string]::IsNullOrWhiteSpace($entry.extra))
    {
        throw "Curated benchmark dataset entry '$($entry.name)' does not contain explanatory extra metadata."
    }
}
