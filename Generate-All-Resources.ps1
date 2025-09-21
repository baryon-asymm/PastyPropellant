<#
.SYNOPSIS
    Main resource generation script for PastyPropellant project
.DESCRIPTION
    Invokes Generate-Resources.ps1 from the ReportMaking Resources directory
#>

# Configuration
$projectRoot = $PSScriptRoot  # Use script location directly as project root
$resourcesRelativePath = "src\dotnet\ParametricCombustionModel\src\ParametricCombustionModel.ReportMaking\Resources"
$resourcesDir = Join-Path -Path $projectRoot -ChildPath $resourcesRelativePath
$generateScript = Join-Path -Path $resourcesDir -ChildPath "Generate-Resources.ps1"

Write-Host "Starting resource generation for PastyPropellant project..."
Write-Host "Project root: $projectRoot"
Write-Host "Resources directory: $resourcesDir"

# Verify generate script exists
if (-not (Test-Path -Path $generateScript -PathType Leaf)) {
    Write-Error "Resource generation script not found at: $generateScript"
    Write-Host "Please verify:"
    Write-Host "1. This script should be placed in your project root directory"
    Write-Host "2. The resources directory structure should match: $resourcesRelativePath"
    Write-Host "3. Generate-Resources.ps1 should exist in the resources directory"
    exit 1
}

# Verify resources directory exists
if (-not (Test-Path -Path $resourcesDir -PathType Container)) {
    Write-Error "Resources directory not found: $resourcesDir"
    exit 1
}

# Change to resources directory
Push-Location -Path $resourcesDir

try {
    # Execute generation script
    Write-Host "Executing resource generation script..."
    & $generateScript
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Resource generation failed (exit code: $LASTEXITCODE)"
        exit $LASTEXITCODE
    }
}
catch {
    Write-Error "Script execution error: $_"
    exit 1
}
finally {
    # Return to original directory
    Pop-Location
}

Write-Host "Resource generation completed successfully!" -ForegroundColor Green
exit 0
