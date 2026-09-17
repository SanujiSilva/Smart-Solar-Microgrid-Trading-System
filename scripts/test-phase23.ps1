param(
    [switch]$SkipWebE2E,
    [switch]$SkipAndroid
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot

function Invoke-PhaseCheck {
    param(
        [string]$Name,
        [string]$WorkingDirectory,
        [string[]]$Command
    )

    Write-Host ""
    Write-Host "== $Name =="
    Write-Host ($Command -join " ")
    Push-Location $WorkingDirectory
    try {
        $arguments = @($Command | Select-Object -Skip 1)
        & $Command[0] @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$Name failed with exit code $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
}

Invoke-PhaseCheck "Backend tests" $repo @(
    "dotnet", "test", "backend/SmartSolarMicrogrid.sln", "--configuration", "Release", "--no-restore"
)

Invoke-PhaseCheck "Web build" (Join-Path $repo "web") @("npm.cmd", "run", "build")
Invoke-PhaseCheck "Web lint" (Join-Path $repo "web") @("npm.cmd", "run", "lint")

if (-not $SkipWebE2E) {
    Invoke-PhaseCheck "Web Playwright E2E" (Join-Path $repo "web") @("npm.cmd", "run", "test:e2e")
}

if (-not $SkipAndroid) {
    if (-not $env:JAVA_HOME) {
        $studioJdk = "C:/Program Files/Android/Android Studio/jbr"
        if (Test-Path $studioJdk) {
            $env:JAVA_HOME = $studioJdk
        }
    }
    $standardSdk = Join-Path $env:LOCALAPPDATA "Android/Sdk"
    $currentSdkHasPlatforms = $env:ANDROID_HOME -and (Test-Path (Join-Path $env:ANDROID_HOME "platforms"))
    if ((-not $currentSdkHasPlatforms) -and (Test-Path (Join-Path $standardSdk "platforms"))) {
        $env:ANDROID_HOME = $standardSdk
    }
    $env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
    $env:DEBUG = ""
    Invoke-PhaseCheck "Android build, unit tests and lint" $repo @(
        ".\android\gradlew.bat", "-p", "android", "assembleDebug", "testDebugUnitTest", "lintDebug", "--console=plain"
    )
}

Write-Host ""
Write-Host "Phase 23 automated checks completed."
