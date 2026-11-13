# Runs both frontend variants and rehearses the recall against the configured API.
param([string]$ApiBase = 'http://localhost:5188/api/v1')
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$env:VITE_API_BASE_URL = $ApiBase
$frontPath = Join-Path $root 'front-end/erp-frontend'
$demoPath = Join-Path $root 'front Deploy/erp-frontend'
$node = (Get-Command node).Source
Push-Location $demoPath
try {
    npm run build
    if ($LASTEXITCODE -ne 0) { throw 'Demo build failed' }
} finally { Pop-Location }
$front = Start-Process $node -ArgumentList 'node_modules/vite/bin/vite.js --host 127.0.0.1 --port 5193 --strictPort' -WorkingDirectory $frontPath -WindowStyle Hidden -PassThru
$demo = Start-Process $node -ArgumentList 'node_modules/vite/bin/vite.js preview --host 127.0.0.1 --port 4194 --strictPort' -WorkingDirectory $demoPath -WindowStyle Hidden -PassThru
try {
    foreach ($port in 5193, 4194) {
        $ready = $false
        for ($attempt = 0; $attempt -lt 24; $attempt++) {
            try { $ready = (Invoke-WebRequest "http://127.0.0.1:$port/" -TimeoutSec 2).StatusCode -eq 200 }
            catch { $ready = $false }
            if ($ready) { break }
            Start-Sleep -Milliseconds 250
        }
        if (!$ready) { throw "Frontend on port $port did not start" }
    }
    & $node (Join-Path $PSScriptRoot 'Test-Demo.mjs')
    if ($LASTEXITCODE -ne 0) { throw 'Browser audit failed' }
} finally {
    foreach ($process in @($front, $demo)) {
        if (!$process.HasExited) { Stop-Process -Id $process.Id }
    }
}
