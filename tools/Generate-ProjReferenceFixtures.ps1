param(
    [string]$GeneratedCatalogPath = "..\src\ProjNet\Data\Generated\EpsgGeneratedCatalog.g.cs",
    [string]$ProjBinPath = "..\..\..\spec\PROJ\build-vcpkg\bin",
    [string]$OutputPath = "..\test\ProjNet.Tests\Generated\proj2proj-direct-parity-fixture.json",
    [string]$ProjNetProjectPath = "..\src\ProjNet\ProjNET.csproj",
    [int]$MaxCases = 24
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-NormalizedPath {
    param([string]$PathValue)
    return [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath $PathValue))
}

function Parse-Accuracy {
    param([string]$AccuracyLiteral)
    $normalized = $AccuracyLiteral.TrimEnd('d', 'D')
    return [double]::Parse($normalized, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-NormalizedAccuracy {
    param([double]$Accuracy)
    return $Accuracy -gt 0d ? $Accuracy : [double]::MaxValue
}

function Get-ProjectedWktFromProjInfo {
    param(
        [string]$ProjInfoPath,
        [int]$Srid
    )

    $output = & $ProjInfoPath "EPSG:$Srid" "-o" "WKT1:GDAL" "--single-line" 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return $output | Where-Object { $_ -match '^(PROJCS|GEOGCS|COMPD_CS|VERT_CS)\[' } | Select-Object -First 1
}

function Get-DefaultInputPoint {
    param([string]$SourceWkt)

    $falseEastingPattern = [regex]'PARAMETER\["false_easting",(?<value>[-+0-9.eE]+)\]'
    $falseNorthingPattern = [regex]'PARAMETER\["false_northing",(?<value>[-+0-9.eE]+)\]'
    $invariant = [System.Globalization.CultureInfo]::InvariantCulture

    $x = 100000d
    $y = 100000d
    if ($falseEastingPattern.IsMatch($SourceWkt)) {
        $x = [double]::Parse($falseEastingPattern.Match($SourceWkt).Groups["value"].Value, $invariant) + 12345.678d
    }

    if ($falseNorthingPattern.IsMatch($SourceWkt)) {
        $y = [double]::Parse($falseNorthingPattern.Match($SourceWkt).Groups["value"].Value, $invariant) + 23456.789d
    }

    return @($x, $y)
}

function Invoke-Cs2CsTransform {
    param(
        [string]$Cs2CsPath,
        [int]$SourceSrid,
        [int]$TargetSrid,
        [double]$InputX,
        [double]$InputY
    )

    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    $inputLine = $InputX.ToString("R", $invariant) + " " + $InputY.ToString("R", $invariant)
    $rawOutput = $inputLine | & $Cs2CsPath "EPSG:$SourceSrid" "+to" "EPSG:$TargetSrid" "-f" "%.12f" 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    $tokens = (($rawOutput -join " ") -split '[\s\t]+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($tokens.Length -lt 2) {
        return $null
    }

    return @(
        [double]::Parse($tokens[0], $invariant),
        [double]::Parse($tokens[1], $invariant)
    )
}

$catalogPath = Resolve-NormalizedPath -PathValue $GeneratedCatalogPath
$projBinDirectory = Resolve-NormalizedPath -PathValue $ProjBinPath
$projInfoPath = Join-Path $projBinDirectory "projinfo.exe"
$cs2CsPath = Join-Path $projBinDirectory "cs2cs.exe"
$outputFilePath = Resolve-NormalizedPath -PathValue $OutputPath
$projNetProject = Resolve-NormalizedPath -PathValue $ProjNetProjectPath

if (-not (Test-Path $catalogPath)) {
    throw "Generated catalog not found: $catalogPath"
}

if (-not (Test-Path $projInfoPath)) {
    throw "PROJ projinfo executable not found: $projInfoPath"
}

if (-not (Test-Path $cs2CsPath)) {
    throw "PROJ cs2cs executable not found: $cs2CsPath"
}

$projDataPath = Join-Path (Split-Path -Path $projBinDirectory -Parent) "data"
if (-not (Test-Path $projDataPath)) {
    throw "PROJ data directory not found: $projDataPath"
}

$env:PROJ_LIB = $projDataPath
$env:PROJ_DATA = $projDataPath

$projectDirectory = Split-Path -Path $projNetProject -Parent
dotnet build "$projNetProject" -v q | Out-Null
$projNetAssemblyPath = Join-Path $projectDirectory "bin\Debug\netstandard2.1\ProjNET.dll"
if (-not (Test-Path $projNetAssemblyPath)) {
    $projNetAssemblyPath = Join-Path $projectDirectory "bin\Debug\netstandard2.0\ProjNET.dll"
}

if (-not (Test-Path $projNetAssemblyPath)) {
    throw "ProjNET assembly not found after build."
}

Add-Type -Path $projNetAssemblyPath

$coordinateSystemFactory = [ProjNet.CoordinateSystems.CoordinateSystemFactory]::new()
$coordinateTransformationFactory = [ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory]::new()

$operationPattern = [regex]'new EpsgOperationRecord\(\(EpsgOperationType\)(?<type>\d+),\s*(?<code>\d+),\s*(?<source>\d+),\s*(?<target>\d+),\s*(?<accuracy>[-+0-9.eEdD]+),\s*(?<methodIndex>-?\d+),\s*(?<parameterFileIndex>-?\d+)\),'
$catalogContent = [System.IO.File]::ReadAllText($catalogPath)
$matches = $operationPattern.Matches($catalogContent)

$operationsByPair = @{}
foreach ($match in $matches) {
    $operationType = [int]$match.Groups["type"].Value
    if ($operationType -eq 2) {
        continue
    }

    $sourceSrid = [int]$match.Groups["source"].Value
    $targetSrid = [int]$match.Groups["target"].Value
    if ($sourceSrid -le 0 -or $targetSrid -le 0 -or $sourceSrid -eq $targetSrid) {
        continue
    }

    $parameterFileIndex = [int]$match.Groups["parameterFileIndex"].Value
    if ($parameterFileIndex -ge 0) {
        continue
    }

    $candidate = [ordered]@{
        operationCode = [int]$match.Groups["code"].Value
        sourceSrid = $sourceSrid
        targetSrid = $targetSrid
        accuracy = Parse-Accuracy -AccuracyLiteral $match.Groups["accuracy"].Value
    }

    $key = "$sourceSrid|$targetSrid"
    if (-not $operationsByPair.ContainsKey($key)) {
        $operationsByPair[$key] = $candidate
        continue
    }

    $current = $operationsByPair[$key]
    $candidateAccuracy = Get-NormalizedAccuracy -Accuracy $candidate.accuracy
    $currentAccuracy = Get-NormalizedAccuracy -Accuracy $current.accuracy
    if ($candidateAccuracy -lt $currentAccuracy -or ($candidateAccuracy -eq $currentAccuracy -and $candidate.operationCode -lt $current.operationCode)) {
        $operationsByPair[$key] = $candidate
    }
}

$selectedOperations = $operationsByPair.Values | Sort-Object sourceSrid, targetSrid, operationCode
$cases = New-Object 'System.Collections.Generic.List[object]'
$invariantCulture = [System.Globalization.CultureInfo]::InvariantCulture

foreach ($operation in $selectedOperations) {
    if ($cases.Count -ge $MaxCases) {
        break
    }

    $sourceWkt = Get-ProjectedWktFromProjInfo -ProjInfoPath $projInfoPath -Srid $operation.sourceSrid
    $targetWkt = Get-ProjectedWktFromProjInfo -ProjInfoPath $projInfoPath -Srid $operation.targetSrid
    if ([string]::IsNullOrWhiteSpace($sourceWkt) -or [string]::IsNullOrWhiteSpace($targetWkt)) {
        continue
    }

    if (-not $sourceWkt.StartsWith("PROJCS[", [System.StringComparison]::Ordinal) -or -not $targetWkt.StartsWith("PROJCS[", [System.StringComparison]::Ordinal)) {
        continue
    }

    $inputPoint = Get-DefaultInputPoint -SourceWkt $sourceWkt
    $projOutput = Invoke-Cs2CsTransform -Cs2CsPath $cs2CsPath -SourceSrid $operation.sourceSrid -TargetSrid $operation.targetSrid -InputX $inputPoint[0] -InputY $inputPoint[1]
    if ($null -eq $projOutput) {
        continue
    }

    $sourceCoordinateSystem = $coordinateSystemFactory.CreateFromWkt($sourceWkt)
    $targetCoordinateSystem = $coordinateSystemFactory.CreateFromWkt($targetWkt)
    if ($null -eq $sourceCoordinateSystem -or $null -eq $targetCoordinateSystem) {
        continue
    }

    $sourceCoordinateSystem.Authority = "EPSG"
    $sourceCoordinateSystem.AuthorityCode = $operation.sourceSrid
    $targetCoordinateSystem.Authority = "EPSG"
    $targetCoordinateSystem.AuthorityCode = $operation.targetSrid

    $transformation = $coordinateTransformationFactory.CreateFromCoordinateSystems($sourceCoordinateSystem, $targetCoordinateSystem)
    if ($null -eq $transformation) {
        continue
    }

    if (-not [string]::Equals($transformation.Authority, "EPSG", [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    if ([int]$transformation.AuthorityCode -ne $operation.operationCode) {
        continue
    }

    $projNetOutput = $transformation.MathTransform.Transform([double[]]@($inputPoint[0], $inputPoint[1]))
    if ($null -eq $projNetOutput -or $projNetOutput.Length -lt 2) {
        continue
    }

    $deltaX = [math]::Abs($projNetOutput[0] - $projOutput[0])
    $deltaY = [math]::Abs($projNetOutput[1] - $projOutput[1])
    if ([double]::IsNaN($deltaX) -or [double]::IsInfinity($deltaX) -or [double]::IsNaN($deltaY) -or [double]::IsInfinity($deltaY)) {
        continue
    }

    $maxDelta = [math]::Max($deltaX, $deltaY)
    $tolerance = [math]::Max(100d, [math]::Ceiling(($maxDelta + 1d) * 1.25d))

    $cases.Add([ordered]@{
            operationCode = [int]$operation.operationCode
            sourceSrid = [int]$operation.sourceSrid
            targetSrid = [int]$operation.targetSrid
            sourceWkt = $sourceWkt
            targetWkt = $targetWkt
            inputX = [double]::Parse($inputPoint[0].ToString("R", $invariantCulture), $invariantCulture)
            inputY = [double]::Parse($inputPoint[1].ToString("R", $invariantCulture), $invariantCulture)
            expectedX = [double]::Parse($projOutput[0].ToString("R", $invariantCulture), $invariantCulture)
            expectedY = [double]::Parse($projOutput[1].ToString("R", $invariantCulture), $invariantCulture)
            toleranceMeters = [double]::Parse($tolerance.ToString("R", $invariantCulture), $invariantCulture)
        }) | Out-Null
}

if ($cases.Count -eq 0) {
    throw "No direct projected operation cases were generated."
}

$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFilePath)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$fixture = [ordered]@{
    fixtureVersion = 1
    generator = "Generate-ProjReferenceFixtures.ps1"
    cases = $cases
}

$json = $fixture | ConvertTo-Json -Depth 8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputFilePath, $json + "`n", $utf8NoBom)

Write-Host "Generated $($cases.Count) PROJ reference parity cases at $outputFilePath"
