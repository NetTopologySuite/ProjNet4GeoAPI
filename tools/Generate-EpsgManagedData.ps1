param(
    # Path to the EPSG WKT ZIP archive (for example EPSG-v12_054-WKT.Zip).
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,
    # Path to the EPSG PostgreSQL ZIP archive (for example EPSG-v12_054-PostgreSQL.zip).
    [Parameter(Mandatory = $true)]
    [string]$PgZipPath,
    [string]$OutputPath = "..\src\ProjNet\Data\Generated\EpsgGeneratedCatalog.g.cs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-NormalizedPath {
    param([string]$PathValue)
    return [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath $PathValue))
}

$zipFilePath = Resolve-NormalizedPath -PathValue $ZipPath
$pgZipFilePath = Resolve-NormalizedPath -PathValue $PgZipPath
$outputFilePath = Resolve-NormalizedPath -PathValue $OutputPath

if (-not (Test-Path $zipFilePath)) {
    throw "EPSG archive not found: $zipFilePath"
}

if (-not (Test-Path $pgZipFilePath)) {
    throw "EPSG PostgreSQL archive not found: $pgZipFilePath"
}

$generatorScript = Join-Path -Path $PSScriptRoot -ChildPath "generate_epsg_catalog.py"
if (-not (Test-Path $generatorScript)) {
    throw "Generator script not found: $generatorScript"
}

python $generatorScript --zip $zipFilePath --pg-zip $pgZipFilePath --output $outputFilePath
