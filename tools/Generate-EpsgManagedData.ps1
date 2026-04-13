param(
    # Path to the EPSG WKT ZIP archive (for example EPSG-v12_053-WKT.Zip).
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,
    [string]$OutputPath = "..\src\ProjNet\Data\Generated\EpsgGeneratedCatalog.g.cs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-NormalizedPath {
    param([string]$PathValue)
    return [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath $PathValue))
}

$zipFilePath = Resolve-NormalizedPath -PathValue $ZipPath
$outputFilePath = Resolve-NormalizedPath -PathValue $OutputPath

if (-not (Test-Path $zipFilePath)) {
    throw "EPSG archive not found: $zipFilePath"
}

$generatorScript = Join-Path -Path $PSScriptRoot -ChildPath "generate_epsg_catalog.py"
if (-not (Test-Path $generatorScript)) {
    throw "Generator script not found: $generatorScript"
}

python $generatorScript --zip $zipFilePath --output $outputFilePath
