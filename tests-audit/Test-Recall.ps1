# Exercises traceability, role boundaries and idempotent stock and pallet operations.
param([string]$Base, [string]$Password, [int]$ProductId, [string[]]$RoleRows)
$ErrorActionPreference = 'Stop'
$script:passed = 0
$roles = @{}
foreach ($row in $RoleRows) {
    $role, $id = $row -split ','
    if ($role -and $id) { $roles[$role.Trim()] = [int]$id.Trim() }
}

function Send-Api($Method, $Path, $Body, $Token) {
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $options = @{ Uri="$Base/api/v1/$Path"; Method=$Method; Headers=$headers; SkipHttpErrorCheck=$true; TimeoutSec=12 }
    if ($null -ne $Body) {
        $options.ContentType = 'application/json'
        $options.Body = ConvertTo-Json -InputObject $Body -Depth 8 -Compress
    }
    Invoke-WebRequest @options
}

function Assert-Api($Method, $Path, $Body, $Token, [int]$Expected = 200) {
    $response = Send-Api $Method $Path $Body $Token
    if ($response.StatusCode -ne $Expected) {
        $code = if ($response.Content -and $response.Headers.'Content-Type' -like '*json*') {
            ($response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue).errorCode
        }
        throw "$Method $Path returned $($response.StatusCode), expected $Expected. Error code $code."
    }
    $script:passed++
    "PASS $Method $Path [$Expected]" | Write-Host
    if ($response.Content) { ($response.Content | ConvertFrom-Json).data }
}

function Assert-Condition([bool]$Condition, [string]$Description) {
    if (!$Condition) { throw $Description }
    $script:passed++
    "PASS $Description"
}

$admin = (Assert-Api POST 'auth/login' @{ username='superadmin'; password=$Password } $null).token
$tokens = @{}
foreach ($role in @('Storekeeper','ProductionManager','LabTech','QualityManager')) {
    $null = Assert-Api POST 'auth/register' @{username=$role; fullName="Test $role"; password=$Password; roleId=$roles[$role]} $admin
    if ($env:ERP_AUDIT_BROWSER -eq '1' -and $role -in @('LabTech', 'QualityManager')) {
        $tokens[$role] = $admin
        continue
    }
    $tokens[$role] = (Assert-Api POST 'auth/login' @{ username=$role; password=$Password } $null).token
}
$store = $tokens.Storekeeper
$production = $tokens.ProductionManager
$lab = $tokens.LabTech
$quality = $tokens.QualityManager
$null = Assert-Api GET 'bottling/products' $null $null 401
$null = Assert-Api GET 'bottling/products' $null $store 403
$products = Assert-Api GET 'bottling/products' $null $production
Assert-Condition ($products[0].id -eq $ProductId) 'Production can read the configured product catalog'
$expiry = (Get-Date).ToUniversalTime().AddMonths(3).ToString('yyyy-MM-dd')
$syrup = Assert-Api POST 'store/add-material' @{materialName='Demo syrup'; supplierName='Demo supplier'; supplierLot='SYRUP-01'; quantity=200; physicalState='Liquid'; expiryDate=$expiry; unit='L'} $store
$caps = Assert-Api POST 'store/add-material' @{materialName='Demo caps'; supplierName='Demo supplier'; supplierLot='CAPS-01'; quantity=400; physicalState='Solid'; expiryDate=$expiry; unit='pcs'} $store
$null = Assert-Api POST 'store/issue-material' @{rawMaterialId=$caps.id; quantityToIssue=1} $store 400
foreach ($lot in @($syrup, $caps)) {
    $null = Assert-Api POST 'lab/submit-result' @{rawMaterialId=$lot.id; inspectionType='IncomingMaterial'; resultDetails='Fixture passed'; isPassed=$true} $lab
}
$batches = @()
foreach ($index in 1..2) {
    $issueBody = @{rawMaterialId=$caps.id; quantityToIssue=100; requestId=[Guid]::NewGuid()}
    $capIssue = Assert-Api POST 'store/issue-material' $issueBody $store
    $duplicate = Assert-Api POST 'store/issue-material' $issueBody $store
    Assert-Condition ($duplicate.issueId -eq $capIssue.issueId -and $duplicate.currentQuantity -eq $capIssue.currentQuantity) 'Replayed issue did not deduct stock twice'
    $syrupIssue = Assert-Api POST 'store/issue-material' @{rawMaterialId=$syrup.id; quantityToIssue=50; requestId=[Guid]::NewGuid()} $store
    $batchBody = @{productId=$ProductId; rawMaterialId=$syrup.id; issuedMaterialQty=50; primaryIssueId=$syrupIssue.issueId; additionalIssueIds=@($capIssue.issueId); requestId=[Guid]::NewGuid()}
    $batch = Assert-Api POST 'production/start-batch' $batchBody $production
    $duplicateBatch = Assert-Api POST 'production/start-batch' $batchBody $production
    Assert-Condition ($batch.id -eq $duplicateBatch.id) 'Replayed start returned the original batch'
    $batchBody.requestId = [Guid]::NewGuid()
    $null = Assert-Api POST 'production/start-batch' $batchBody $production 409
    $batches += $batch
    $null = Assert-Api POST 'production/finish-batch' @{batchId=$batch.id; producedQty=45; returnedMaterialQty=2; expiryDate=$expiry} $production
    $null = Assert-Api POST 'lab/submit-result' @{batchId=$batch.id; inspectionType='FinishedBatch'; resultDetails='Fixture passed'; isPassed=$true} $lab
    $null = Assert-Api POST 'manager/decision' @{batchId=$batch.id; approve=$true} $quality
    $null = Assert-Api POST "bottling/batches/$($batch.id)/pallets" @{code="PALLET-$index-A"; quantity=20} $store
    $null = Assert-Api POST "bottling/batches/$($batch.id)/pallets" @{code="PALLET-$index-B"; quantity=25} $store
}
$shipment = @{requestId=[Guid]::NewGuid(); clientName='Demo distributor'; truckPlates='TEST-001'; palletCodes=@('PALLET-1-A','PALLET-2-A')}
$null = Assert-Api POST 'bottling/dispatch' $shipment $production 403
$null = Assert-Api POST 'bottling/dispatch' $shipment $store
$null = Assert-Api POST 'bottling/dispatch' $shipment $store
$holdPath = "bottling/batches/$($batches[0].id)/hold"
$null = Assert-Api PUT $holdPath @{isHeld=$true; reason='Cap supplier investigation'} $store 403
$null = Assert-Api PUT $holdPath @{isHeld=$true; reason='Cap supplier investigation'} $quality
$null = Assert-Api POST 'bottling/dispatch' @{requestId=[Guid]::NewGuid(); clientName='Demo distributor'; truckPlates='TEST-002'; palletCodes=@('PALLET-1-B','PALLET-2-B')} $store 409
$null = Assert-Api GET "bottling/recall?lotCode=$($caps.lotCode)" $null $production 403
$recall = Assert-Api GET "bottling/recall?lotCode=$($caps.lotCode)" $null $quality
Assert-Condition ($recall.batches.Count -eq 2 -and $recall.stockQuantity -eq 200) 'Recall traces both batches and reconciles remaining cap stock'
$pallets = @($recall.batches | ForEach-Object { $_.pallets })
Assert-Condition (@($pallets | Where-Object state -eq 'Shipped').Count -eq 2) 'Recall identifies the two dispatched pallets'
Assert-Condition (@($pallets | Where-Object state -eq 'Held').Count -eq 1) 'Recall identifies the held pallet and rejected dispatch remained atomic'
Assert-Condition (@($pallets | Where-Object clientName -eq 'Demo distributor').Count -eq 2) 'Recall identifies the affected customer'
"PASS_COUNT=$script:passed"
$env:ERP_BROWSER_LOT = $caps.lotCode
"DEMO_LOT=$($caps.lotCode)"
