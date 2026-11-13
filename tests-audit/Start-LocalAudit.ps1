# Starts the API against a fresh SQL Server LocalDB database and supplies disposable fixtures.
param([int]$Port = 5188)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '../back-end/ERP-Backend')
$database = 'ERP_Audit_' + [Guid]::NewGuid().ToString('N')
$server = $env:ERP_AUDIT_SQL_SERVER ?? '(localdb)\MSSQLLocalDB'
$env:ConnectionStrings__DefaultConnection = "Server=$server;Database=$database;Integrated Security=true;TrustServerCertificate=true"
$env:Jwt__Key = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:Seed__SuperAdminPassword = 'Aa1!' + [Guid]::NewGuid().ToString('N')
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AllowedOrigins__0 = 'http://localhost:5173'
$env:AllowedOrigins__1 = 'http://localhost:4173'
$env:AllowedOrigins__2 = 'http://localhost:4174'
$env:AllowedOrigins__3 = 'http://127.0.0.1:5193'
$env:AllowedOrigins__4 = 'http://127.0.0.1:4194'
$env:ASPNETCORE_URLS = "http://localhost:$Port"

$log = Join-Path $PSScriptRoot "../audit-report/logs"
$backend = Start-Process -FilePath 'dotnet' -ArgumentList '"bin/Debug/net10.0/ERP Api.dll"' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $log "api.log") -RedirectStandardError (Join-Path $log "api-error.log")
try {
    $base = "http://localhost:$Port"
    $health = $null
    for ($attempt = 0; $attempt -lt 40 -and !$backend.HasExited; $attempt++) {
        try { $health = Invoke-WebRequest "$base/health" -SkipHttpErrorCheck -TimeoutSec 3 } catch { $health = $null }
        if ($health.StatusCode -eq 200) { break }
        Start-Sleep -Milliseconds 350
    }
    if ($health.StatusCode -ne 200) { throw 'API health check did not return HTTP 200.' }

    $sql = "INSERT INTO dbo.Products (ProductName, SKU) VALUES (N'Demo Cola PET',N'COLA-PET'); SELECT Id FROM dbo.Products WHERE SKU=N'COLA-PET';"
    $product = & sqlcmd -S $server -d $database -E -C -b -h -1 -W -Q "SET NOCOUNT ON; $sql"
    if ($LASTEXITCODE -ne 0) { throw 'Unable to seed demo product.' }
    $roles = & sqlcmd -S $server -d $database -E -C -b -h -1 -W -s ',' -Q 'SET NOCOUNT ON; SELECT RoleName, Id FROM dbo.Roles;'
    if ($LASTEXITCODE -ne 0) { throw 'Unable to load audit roles.' }
    & (Join-Path $PSScriptRoot 'Test-Recall.ps1') -Base $base -Password $env:Seed__SuperAdminPassword -ProductId ([int]$product.Trim()) -RoleRows $roles
    if ($env:ERP_AUDIT_BROWSER -eq '1') {
        $env:ERP_BROWSER_PASSWORD = $env:Seed__SuperAdminPassword
        & (Join-Path $PSScriptRoot 'Start-BrowserAudit.ps1') -ApiBase "$base/api/v1"
    }
    "AUDIT_DATABASE=$database"
    "AUDIT_BASE=$base"
    if ($env:ERP_AUDIT_KEEP_RUNNING -eq '1') { Wait-Process -Id $backend.Id }
} finally {
    if (!$backend.HasExited) { Stop-Process -Id $backend.Id }
}
