param(
    [string]$ZipPath = "..\..\..\spec\epsg\EPSG-v12_053-WKT.Zip",
    [string]$ProjDbPath = "..\..\..\spec\PROJ\build-vcpkg\data\proj.db",
    [string]$OutputPath = "..\src\ProjNet\Data\Generated\EpsgGeneratedCatalog.g.cs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-NormalizedPath {
    param([string]$PathValue)
    return [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath $PathValue))
}

$zipFilePath = Resolve-NormalizedPath -PathValue $ZipPath
$projDbFilePath = Resolve-NormalizedPath -PathValue $ProjDbPath
$outputFilePath = Resolve-NormalizedPath -PathValue $OutputPath

if (-not (Test-Path $zipFilePath)) {
    throw "EPSG archive not found: $zipFilePath"
}

if (-not (Test-Path $projDbFilePath)) {
    throw "PROJ database not found: $projDbFilePath"
}

$generatorScript = Join-Path -Path $PSScriptRoot -ChildPath "generate_epsg_catalog.py"
if (-not (Test-Path $generatorScript)) {
    throw "Generator script not found: $generatorScript"
}

python $generatorScript --zip $zipFilePath --proj-db $projDbFilePath --output $outputFilePath
