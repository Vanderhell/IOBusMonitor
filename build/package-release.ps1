param(
    [switch]$GenerateChecksum,
    [switch]$IncludeDemoData
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Require-Command {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$HelpText
    )

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw $HelpText
    }

    return $command.Source
}

function Copy-FilteredTree {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,
        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    $excludedDirectories = @("Data", "Logs", "Settings")
    $excludedFilePatterns = @("*.pdb", "*.xml", "*.db", "*.db-journal", "*.log", "*.vshost.*")

    Get-ChildItem -Path $Source -Recurse -Force | ForEach-Object {
        $relativePath = $_.FullName.Substring($Source.Length).TrimStart('\')
        if ([string]::IsNullOrWhiteSpace($relativePath)) {
            return
        }

        $segments = $relativePath.Split('\')
        if (($segments | Where-Object { $excludedDirectories -contains $_ }).Count -gt 0) {
            return
        }

        $destinationPath = Join-Path $Destination $relativePath

        if ($_.PSIsContainer) {
            if (-not (Test-Path -LiteralPath $destinationPath)) {
                New-Item -ItemType Directory -Path $destinationPath | Out-Null
            }

            return
        }

        foreach ($pattern in $excludedFilePatterns) {
            if ($_.Name -like $pattern) {
                return
            }
        }

        $destinationDirectory = Split-Path -Path $destinationPath -Parent
        if (-not (Test-Path -LiteralPath $destinationDirectory)) {
            New-Item -ItemType Directory -Path $destinationDirectory | Out-Null
        }

        Copy-Item -LiteralPath $_.FullName -Destination $destinationPath -Force
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$solutionPath = Join-Path $repoRoot "IOBusMonitor.sln"
$projectOutput = Join-Path $repoRoot "IOBusMonitor\bin\x64\Release"
$distRoot = Join-Path $repoRoot "dist"
$stagingRoot = Join-Path $distRoot "staging"

$nuget = Require-Command -Name "nuget" -HelpText "nuget.exe was not found on PATH. Run the script from a Windows shell with NuGet CLI installed."
$msbuild = Require-Command -Name "msbuild" -HelpText "msbuild.exe was not found on PATH. Run the script from a Visual Studio Developer PowerShell or Build Tools shell."

Write-Host "Restoring NuGet packages..."
& $nuget restore $solutionPath
if ($LASTEXITCODE -ne 0) {
    throw "NuGet restore failed with exit code $LASTEXITCODE."
}

Write-Host "Building Release|x64..."
& $msbuild $solutionPath "/p:Configuration=Release" "/p:Platform=x64"
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE."
}

$appExecutable = Join-Path $projectOutput "IOBusMonitor.exe"
if (-not (Test-Path -LiteralPath $appExecutable)) {
    throw "Expected release executable was not found at $appExecutable."
}

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($appExecutable).ProductVersion
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "0.0.0"
}

$packageName = "IOBusMonitor-$version-win-x64"
$packageRoot = Join-Path $stagingRoot $packageName
$zipPath = Join-Path $distRoot ($packageName + ".zip")
$checksumPath = $zipPath + ".sha256"

if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path -LiteralPath $checksumPath) {
    Remove-Item -LiteralPath $checksumPath -Force
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

Write-Host "Collecting runtime files..."
Copy-FilteredTree -Source $projectOutput -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination (Join-Path $packageRoot "README.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination (Join-Path $packageRoot "LICENSE") -Force

if ($IncludeDemoData) {
    foreach ($folderName in @("Settings", "Data", "Logs")) {
        $sourceFolder = Join-Path $repoRoot $folderName
        if (Test-Path -LiteralPath $sourceFolder) {
            Copy-Item -LiteralPath $sourceFolder -Destination (Join-Path $packageRoot $folderName) -Recurse -Force
        }
    }
}

Write-Host "Creating ZIP package..."
if (-not (Test-Path -LiteralPath $distRoot)) {
    New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
}

Compress-Archive -Path $packageRoot -DestinationPath $zipPath -Force

if ($GenerateChecksum) {
    $hash = Get-FileHash -Path $zipPath -Algorithm SHA256
    "{0} *{1}" -f $hash.Hash.ToLowerInvariant(), (Split-Path -Path $zipPath -Leaf) | Set-Content -Path $checksumPath
}

Write-Host "Package created: $zipPath"
if ($GenerateChecksum) {
    Write-Host "Checksum created: $checksumPath"
}
