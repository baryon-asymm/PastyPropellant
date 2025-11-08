<#
.SYNOPSIS
    Generates C# resource classes from .resx files using ResGen.exe
.DESCRIPTION
    Processes all specified resource files and generates corresponding C# classes
.NOTES
    File Name: Generate-Resources.ps1
    Requires: .NET SDK (for ResGen.exe)
#>

function Find-ResGenPath {
    # Try common ResGen.exe locations
    $paths = @(
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ResGen.exe",
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\ResGen.exe",
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\ResGen.exe",
        "${env:ProgramFiles}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ResGen.exe"
    )
    
    foreach ($path in $paths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    # Try via where command (checks PATH)
    $whereResGen = where.exe ResGen.exe 2>$null
    if ($whereResGen) {
        return $whereResGen
    }
    
    throw "ResGen.exe not found. Please install .NET SDK or specify full path to ResGen.exe"
}

# Configuration
try {
    $resgenPath = Find-ResGenPath
    Write-Host "Found ResGen.exe at: $resgenPath" -ForegroundColor Cyan
} catch {
    Write-Error $_.Exception.Message
    exit 1
}

$namespace = "ParametricCombustionModel.ReportMaking.Resources"
$outputDir = ".\Generated"
$resourceFiles = @(
    "CombustionSolverParamsReportResources",
    "ConstraintPenaltyEvaluatorReportResources",
    "DifferentialEvolutionSettingsReportResources",
    "FitnessFunctionEvaluatorReportResources",
    "ParametricConstraintReportResources",
    "ProblemContextReportResources",
    "PropellantReportResources",
    "PerformanceMeterReportResources",
    "PressureTablesReportResources"
)

# Create output directory if it doesn't exist
if (-not (Test-Path -Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# Process each resource file - only generate C# classes for default culture
foreach ($resourceBaseName in $resourceFiles) {
    $resxFile = "$resourceBaseName.resx"
    $outputFile = "$outputDir\$resourceBaseName.cs"
    
    Write-Host "`nProcessing $resxFile -> $outputFile"
    
    # Verify .resx file exists
    if (-not (Test-Path $resxFile -PathType Leaf)) {
        Write-Warning "Base resource file not found: $resxFile"
        continue
    }
    
    # Build the ResGen command
    $arguments = @(
        "/str:csharp,$namespace,$resourceBaseName,$outputFile",
        "/compile",
        $resxFile
    )
    
    # Execute ResGen with error handling
    try {
        $process = Start-Process -FilePath $resgenPath -ArgumentList $arguments -NoNewWindow -Wait -PassThru
        
        if ($process.ExitCode -ne 0) {
            Write-Error "Failed to generate resources for $resxFile (Exit Code: $($process.ExitCode))"
        } else {
            Write-Host "Successfully generated" -ForegroundColor Green
            if (Test-Path $outputFile) {
                Write-Host "Output file size: $((Get-Item $outputFile).Length) bytes"
            }
        }
    } catch {
        Write-Error "Error executing ResGen for ${resxFile}: $_"
    }
}

# Validate that satellite resource files exist for other cultures
$supportedCultures = @("en-US", "fr-FR")
foreach ($culture in $supportedCultures) {
    Write-Host "`nValidating satellite resources for culture: $culture" -ForegroundColor Yellow
    foreach ($resourceBaseName in $resourceFiles) {
        $cultureResxFile = "$resourceBaseName.$culture.resx"
        if (Test-Path $cultureResxFile -PathType Leaf) {
            Write-Host "✓ Found: $cultureResxFile" -ForegroundColor Green
        } else {
            Write-Warning "✗ Missing: $cultureResxFile"
        }
    }
}

Write-Host "`nResource generation complete for all cultures!" -ForegroundColor Cyan
