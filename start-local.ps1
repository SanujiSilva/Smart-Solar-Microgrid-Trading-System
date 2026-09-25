param([ValidateSet('All', 'Api', 'Web')][string]$Service = 'All')

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if ($Service -eq 'All') {
    foreach ($item in @(@{ Name = 'Api'; Port = 5080 }, @{ Name = 'Web'; Port = 5173 })) {
        if (Get-NetTCPConnection -State Listen -LocalPort $item.Port -ErrorAction SilentlyContinue) {
            Write-Host "$($item.Name): port $($item.Port) is already in use."
            continue
        }
        $process = Start-Process powershell.exe -WindowStyle Hidden -PassThru -WorkingDirectory $PSScriptRoot `
            -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Service $($item.Name)" `
            -RedirectStandardOutput "$PSScriptRoot/$($item.Name).local.log" `
            -RedirectStandardError "$PSScriptRoot/$($item.Name).error.log"
        Write-Host "$($item.Name) starting (PID $($process.Id)); see $($item.Name).local.log and $($item.Name).error.log."
    }
    Write-Host 'Website: http://localhost:5173'
    Write-Host 'API readiness: http://localhost:5080/api/health/ready'
    exit
}

if ($Service -eq 'Api') {
    $configPath = Join-Path $PSScriptRoot '.env.local.json'
    if (!(Test-Path $configPath)) { throw 'Missing backend configuration: .env.local.json' }
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    foreach ($property in $config.PSObject.Properties) {
        [Environment]::SetEnvironmentVariable($property.Name, [string]$property.Value, 'Process')
    }
    $dotnet = Join-Path (Split-Path $PSScriptRoot -Parent) '.local-tools/dotnet/dotnet.exe'
    if (!(Test-Path $dotnet)) { $dotnet = 'dotnet' }
    & $dotnet run --project backend/src/SmartSolarMicrogrid.Api --launch-profile http
    exit $LASTEXITCODE
}

Set-Location (Join-Path $PSScriptRoot 'web')
& npm.cmd run dev -- --host localhost --port 5173 --strictPort
exit $LASTEXITCODE
