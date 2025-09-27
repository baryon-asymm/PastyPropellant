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
    "FitnessFunctionEvaluatorReportResources",
    "ParametricConstraintReportResources",
    "ProblemContextReportResources",
    "PropellantReportResources"
)

# Create output directory if it doesn't exist
if (-not (Test-Path -Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# Process each resource file
foreach ($resourceBaseName in $resourceFiles) {
    $resxFile = "$resourceBaseName.resx"
    $outputFile = "$outputDir\$resourceBaseName.cs"
    
    Write-Host "`nProcessing $resxFile -> $outputFile"
    
    # Verify .resx file exists
    if (-not (Test-Path $resxFile -PathType Leaf)) {
        Write-Warning "Resource file not found: $resxFile"
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
            Write-Error "Failed to generate resources (Exit Code: $($process.ExitCode))"
        } else {
            Write-Host "Successfully generated" -ForegroundColor Green
            if (Test-Path $outputFile) {
                Write-Host "Output file size: $((Get-Item $outputFile).Length) bytes"
            }
        }
    } catch {
        Write-Error "Error executing ResGen: $_"
    }
}

Write-Host "`nResource generation complete!" -ForegroundColor Cyan
