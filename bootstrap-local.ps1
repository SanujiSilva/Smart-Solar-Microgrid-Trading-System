$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$config = Get-Content -LiteralPath (Join-Path $PSScriptRoot '.env.local.json') -Raw | ConvertFrom-Json
$variableNames = @($config.PSObject.Properties.Name) + @('Bootstrap__FullName', 'Bootstrap__Email', 'Bootstrap__Phone', 'Bootstrap__Password')
$previousValues = @{}
foreach ($name in $variableNames) { $previousValues[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    foreach ($property in $config.PSObject.Properties) {
        [Environment]::SetEnvironmentVariable($property.Name, [string]$property.Value, 'Process')
    }
    $env:Bootstrap__FullName = Read-Host 'Backoffice full name'
    $env:Bootstrap__Email = Read-Host 'Backoffice email'
    $env:Bootstrap__Phone = Read-Host 'Backoffice phone'
    $securePassword = Read-Host 'Password (12-128 characters)' -AsSecureString
    $env:Bootstrap__Password = [Net.NetworkCredential]::new('', $securePassword).Password
    $dotnet = Join-Path (Split-Path $PSScriptRoot -Parent) '.local-tools/dotnet/dotnet.exe'
    if (!(Test-Path $dotnet)) { $dotnet = 'dotnet' }
    & $dotnet run --no-build --project backend/src/SmartSolarMicrogrid.Api --launch-profile http -- --bootstrap-backoffice
    if ($LASTEXITCODE -ne 0) { throw 'Backoffice creation failed. See the error above.' }
    Write-Host 'Backoffice created. Sign in at http://localhost:5173 with the email and password you entered.' -ForegroundColor Green
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
} finally {
    foreach ($name in $variableNames) { [Environment]::SetEnvironmentVariable($name, $previousValues[$name], 'Process') }
    Remove-Variable securePassword, config -ErrorAction SilentlyContinue
}
Read-Host 'Press Enter to close'
