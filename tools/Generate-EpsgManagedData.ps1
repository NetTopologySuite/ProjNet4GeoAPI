param(
    [string]$ZipPath = "..\..\..\spec\epsg\EPSG-v12_053-WKT.Zip",
    [string]$OutputPath = "..\src\ProjNet\Data\Generated\EpsgGeneratedCatalog.g.cs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-NormalizedPath {
    param([string]$PathValue)
    return [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath $PathValue))
}

function Escape-CSharpString {
    param([string]$Value)
    if ($null -eq $Value) {
        return ""
    }

    $escapedValue = $Value.Replace("\", "\\")
    $escapedValue = $escapedValue.Replace('"', '\"')
    $escapedValue = $escapedValue.Replace("`r", "\r")
    $escapedValue = $escapedValue.Replace("`n", "\n")
    $escapedValue = $escapedValue.Replace("`t", "\t")
    return $escapedValue
}

$zipFilePath = Resolve-NormalizedPath -PathValue $ZipPath
$outputFilePath = Resolve-NormalizedPath -PathValue $OutputPath
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFilePath)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

if (-not (Test-Path $zipFilePath)) {
    throw "EPSG archive not found: $zipFilePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipFilePath)

try {
    $stringIndexByValue = New-Object 'System.Collections.Generic.Dictionary[string,int]' ([System.StringComparer]::Ordinal)
    $stringPool = New-Object 'System.Collections.Generic.List[string]'
    $coordinateSystemRecords = New-Object 'System.Collections.Generic.List[object]'
    $operationRecords = New-Object 'System.Collections.Generic.List[object]'

    function Add-StringToPool {
        param([string]$Value)
        if ([string]::IsNullOrEmpty($Value)) {
            return -1
        }

        $existingIndex = 0
        if ($stringIndexByValue.TryGetValue($Value, [ref]$existingIndex)) {
            return $existingIndex
        }

        $newIndex = $stringPool.Count
        $stringPool.Add($Value) | Out-Null
        $stringIndexByValue[$Value] = $newIndex
        return $newIndex
    }

    $crsPattern = [regex]'^EPSG-CRS-(\d+)\.wkt$'
    $transformPattern = [regex]'^EPSG-Transformation-(\d+)\.wkt$'
    $concatPattern = [regex]'^EPSG-ConcatenatedOperation-(\d+)\.wkt$'
    $pmoPattern = [regex]'^EPSG-PMO-(\d+)\.wkt$'
    $idPattern = [regex]'ID\["EPSG",(\d+)\]'
    $methodPattern = [regex]'METHOD\["([^"]+)"'
    $accuracyPattern = [regex]'OPERATIONACCURACY\[(\-?\d+(?:\.\d+)?)\]'
    $parameterFilePattern = [regex]'PARAMETERFILE\["[^"]*","([^"]+)"'

    function Get-BracketBlockContent {
        param(
            [string]$Text,
            [string]$TokenWithOpenBracket
        )

        $tokenIndex = $Text.IndexOf($TokenWithOpenBracket, [System.StringComparison]::Ordinal)
        if ($tokenIndex -lt 0) {
            return $null
        }

        $startIndex = $tokenIndex + $TokenWithOpenBracket.Length
        $depth = 1
        $currentIndex = $startIndex
        while ($currentIndex -lt $Text.Length) {
            $ch = $Text[$currentIndex]
            if ($ch -eq '[') {
                $depth++
            }
            elseif ($ch -eq ']') {
                $depth--
                if ($depth -eq 0) {
                    return $Text.Substring($startIndex, $currentIndex - $startIndex)
                }
            }

            $currentIndex++
        }

        return $null
    }

    function Get-LastEpsgIdFromBlock {
        param([string]$BlockContent)
        if ([string]::IsNullOrEmpty($BlockContent)) {
            return $null
        }

        $matches = $idPattern.Matches($BlockContent)
        if ($matches.Count -eq 0) {
            return $null
        }

        return [int]$matches[$matches.Count - 1].Groups[1].Value
    }

    foreach ($entry in $zip.Entries) {
        if (-not $entry.FullName.EndsWith(".wkt", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $entryName = [System.IO.Path]::GetFileName($entry.FullName)
        $streamReader = [System.IO.StreamReader]::new($entry.Open())
        try {
            $wkt = $streamReader.ReadToEnd()
        }
        finally {
            $streamReader.Dispose()
        }

        $crsMatch = $crsPattern.Match($entryName)
        if ($crsMatch.Success) {
            $srid = [int]$crsMatch.Groups[1].Value
            $wktIndex = Add-StringToPool -Value $wkt
            $coordinateSystemRecords.Add([pscustomobject]@{
                    Srid = $srid
                    WktIndex = $wktIndex
                }) | Out-Null
            continue
        }

        $operationType = -1
        $operationCode = -1
        $operationMatch = $transformPattern.Match($entryName)
        if ($operationMatch.Success) {
            $operationType = 0
            $operationCode = [int]$operationMatch.Groups[1].Value
        }
        else {
            $operationMatch = $concatPattern.Match($entryName)
            if ($operationMatch.Success) {
                $operationType = 1
                $operationCode = [int]$operationMatch.Groups[1].Value
            }
            else {
                $operationMatch = $pmoPattern.Match($entryName)
                if ($operationMatch.Success) {
                    $operationType = 2
                    $operationCode = [int]$operationMatch.Groups[1].Value
                }
            }
        }

        if ($operationType -lt 0) {
            continue
        }

        $sourceBlock = Get-BracketBlockContent -Text $wkt -TokenWithOpenBracket "SOURCECRS["
        $targetBlock = Get-BracketBlockContent -Text $wkt -TokenWithOpenBracket "TARGETCRS["
        $sourceSrid = Get-LastEpsgIdFromBlock -BlockContent $sourceBlock
        $targetSrid = Get-LastEpsgIdFromBlock -BlockContent $targetBlock
        if ($null -eq $sourceSrid -or $null -eq $targetSrid) {
            continue
        }
        $methodMatch = $methodPattern.Match($wkt)
        $methodName = if ($methodMatch.Success) { $methodMatch.Groups[1].Value } else { "" }
        $methodIndex = Add-StringToPool -Value $methodName

        $accuracyMatch = $accuracyPattern.Match($wkt)
        $accuracyValue = if ($accuracyMatch.Success) { [double]::Parse($accuracyMatch.Groups[1].Value, [System.Globalization.CultureInfo]::InvariantCulture) } else { [double]::NaN }

        $parameterFileMatches = $parameterFilePattern.Matches($wkt)
        $parameterFileIndex = -1
        if ($parameterFileMatches.Count -gt 0) {
            $parameterFileIndex = Add-StringToPool -Value $parameterFileMatches[0].Groups[1].Value
        }

        $operationRecords.Add([pscustomobject]@{
                OperationType = $operationType
                OperationCode = $operationCode
                SourceSrid = $sourceSrid
                TargetSrid = $targetSrid
                Accuracy = $accuracyValue
                MethodNameIndex = $methodIndex
                ParameterFileIndex = $parameterFileIndex
            }) | Out-Null
    }

    $coordinateSystemRecords = $coordinateSystemRecords | Sort-Object -Property Srid
    $operationRecords = $operationRecords | Sort-Object -Property OperationType, OperationCode

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.AppendLine("// <auto-generated>")
    [void]$builder.AppendLine("// Generated by tools\\Generate-EpsgManagedData.ps1")
    [void]$builder.AppendLine("// Source: " + [System.IO.Path]::GetFileName($zipFilePath))
    [void]$builder.AppendLine("// </auto-generated>")
    [void]$builder.AppendLine("using System;")
    [void]$builder.AppendLine("using System.Collections.Generic;")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("namespace ProjNet.Data.Generated")
    [void]$builder.AppendLine("{")
    [void]$builder.AppendLine("    internal enum EpsgOperationType : byte")
    [void]$builder.AppendLine("    {")
    [void]$builder.AppendLine("        Transformation = 0,")
    [void]$builder.AppendLine("        ConcatenatedOperation = 1,")
    [void]$builder.AppendLine("        PointMotionOperation = 2")
    [void]$builder.AppendLine("    }")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("    internal struct EpsgCoordinateSystemRecord")
    [void]$builder.AppendLine("    {")
    [void]$builder.AppendLine("        internal EpsgCoordinateSystemRecord(int srid, int wktIndex)")
    [void]$builder.AppendLine("        {")
    [void]$builder.AppendLine("            Srid = srid;")
    [void]$builder.AppendLine("            WktIndex = wktIndex;")
    [void]$builder.AppendLine("        }")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("        internal int Srid { get; }")
    [void]$builder.AppendLine("        internal int WktIndex { get; }")
    [void]$builder.AppendLine("    }")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("    internal struct EpsgOperationRecord")
    [void]$builder.AppendLine("    {")
    [void]$builder.AppendLine("        internal EpsgOperationRecord(EpsgOperationType operationType, int operationCode, int sourceSrid, int targetSrid, double accuracy, int methodNameIndex, int parameterFileIndex)")
    [void]$builder.AppendLine("        {")
    [void]$builder.AppendLine("            OperationType = operationType;")
    [void]$builder.AppendLine("            OperationCode = operationCode;")
    [void]$builder.AppendLine("            SourceSrid = sourceSrid;")
    [void]$builder.AppendLine("            TargetSrid = targetSrid;")
    [void]$builder.AppendLine("            Accuracy = accuracy;")
    [void]$builder.AppendLine("            MethodNameIndex = methodNameIndex;")
    [void]$builder.AppendLine("            ParameterFileIndex = parameterFileIndex;")
    [void]$builder.AppendLine("        }")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("        internal EpsgOperationType OperationType { get; }")
    [void]$builder.AppendLine("        internal int OperationCode { get; }")
    [void]$builder.AppendLine("        internal int SourceSrid { get; }")
    [void]$builder.AppendLine("        internal int TargetSrid { get; }")
    [void]$builder.AppendLine("        internal double Accuracy { get; }")
    [void]$builder.AppendLine("        internal int MethodNameIndex { get; }")
    [void]$builder.AppendLine("        internal int ParameterFileIndex { get; }")
    [void]$builder.AppendLine("    }")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("    internal static class EpsgGeneratedCatalog")
    [void]$builder.AppendLine("    {")
    [void]$builder.AppendLine("        internal const string SourceArchive = """ + (Escape-CSharpString -Value ([System.IO.Path]::GetFileName($zipFilePath))) + """;")
    [void]$builder.AppendLine("        internal static readonly string[] StringPool = new string[]")
    [void]$builder.AppendLine("        {")

    foreach ($value in $stringPool) {
        [void]$builder.AppendLine("            """ + (Escape-CSharpString -Value $value) + """,")
    }

    [void]$builder.AppendLine("        };")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("        internal static readonly EpsgCoordinateSystemRecord[] CoordinateSystems = new EpsgCoordinateSystemRecord[]")
    [void]$builder.AppendLine("        {")
    foreach ($record in $coordinateSystemRecords) {
        [void]$builder.AppendLine("            new EpsgCoordinateSystemRecord(" + $record.Srid + ", " + $record.WktIndex + "),")
    }
    [void]$builder.AppendLine("        };")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("        internal static readonly EpsgOperationRecord[] Operations = new EpsgOperationRecord[]")
    [void]$builder.AppendLine("        {")
    foreach ($record in $operationRecords) {
        $accuracyLiteral = if ([double]::IsNaN($record.Accuracy)) { "double.NaN" } else { $record.Accuracy.ToString("R", [System.Globalization.CultureInfo]::InvariantCulture) + "d" }
        [void]$builder.AppendLine("            new EpsgOperationRecord((EpsgOperationType)" + $record.OperationType + ", " + $record.OperationCode + ", " + $record.SourceSrid + ", " + $record.TargetSrid + ", " + $accuracyLiteral + ", " + $record.MethodNameIndex + ", " + $record.ParameterFileIndex + "),")
    }
    [void]$builder.AppendLine("        };")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("        internal static IEnumerable<KeyValuePair<int, string>> GetCoordinateSystemDefinitions()")
    [void]$builder.AppendLine("        {")
    [void]$builder.AppendLine("            for (int i = 0; i < CoordinateSystems.Length; i++)")
    [void]$builder.AppendLine("            {")
    [void]$builder.AppendLine("                var record = CoordinateSystems[i];")
    [void]$builder.AppendLine("                yield return new KeyValuePair<int, string>(record.Srid, StringPool[record.WktIndex]);")
    [void]$builder.AppendLine("            }")
    [void]$builder.AppendLine("        }")
    [void]$builder.AppendLine("    }")
    [void]$builder.AppendLine("}")

    [System.IO.File]::WriteAllText($outputFilePath, $builder.ToString(), [System.Text.Encoding]::UTF8)
    $crsCount = @($coordinateSystemRecords).Count
    $operationCount = @($operationRecords).Count
    Write-Output ("Generated: " + $outputFilePath)
    Write-Output ("CRS records: " + $crsCount)
    Write-Output ("Operation records: " + $operationCount)
    Write-Output ("String pool size: " + $stringPool.Count)
}
finally {
    $zip.Dispose()
}
