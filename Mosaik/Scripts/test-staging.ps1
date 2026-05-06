# BKM Report Panel - Staging Test Script
# Staging ortamını test etmek için PowerShell scripti

param(
    [string]$BaseUrl = "http://staging.bkm.com/ReportPanel_Staging",
    [string]$HealthUrl = "$BaseUrl/health",
    [int]$TimeoutSeconds = 30
)

Write-Host "=== BKM Report Panel Staging Test ===" -ForegroundColor Green
Write-Host "Test başlatılıyor..." -ForegroundColor Yellow

# Test sonuçları
$TestResults = @()

# 1. Health Check Test
Write-Host "`n1. Health Check testi..." -ForegroundColor Cyan
try {
    $Response = Invoke-WebRequest -Uri $HealthUrl -TimeoutSec $TimeoutSeconds
    if ($Response.StatusCode -eq 200) {
        Write-Host "✓ Health Check başarılı" -ForegroundColor Green
        $TestResults += @{Test="Health Check"; Status="PASS"; Details="HTTP 200"}
    } else {
        Write-Host "✗ Health Check başarısız: $($Response.StatusCode)" -ForegroundColor Red
        $TestResults += @{Test="Health Check"; Status="FAIL"; Details="HTTP $($Response.StatusCode)"}
    }
} catch {
    Write-Host "✗ Health Check hatası: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{Test="Health Check"; Status="ERROR"; Details=$_.Exception.Message}
}

# 2. Ana Sayfa Test
Write-Host "`n2. Ana sayfa testi..." -ForegroundColor Cyan
try {
    $Response = Invoke-WebRequest -Uri "$BaseUrl/Dashboard" -TimeoutSec $TimeoutSeconds
    if ($Response.StatusCode -eq 200 -and $Response.Content -like "*BKM Report Panel*") {
        Write-Host "✓ Ana sayfa başarılı" -ForegroundColor Green
        $TestResults += @{Test="Dashboard"; Status="PASS"; Details="Sayfa yüklendi"}
    } else {
        Write-Host "✗ Ana sayfa başarısız" -ForegroundColor Red
        $TestResults += @{Test="Dashboard"; Status="FAIL"; Details="İçerik bulunamadı"}
    }
} catch {
    Write-Host "✗ Ana sayfa hatası: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{Test="Dashboard"; Status="ERROR"; Details=$_.Exception.Message}
}

# 3. Admin Panel Test
Write-Host "`n3. Admin panel testi..." -ForegroundColor Cyan
try {
    $Response = Invoke-WebRequest -Uri "$BaseUrl/Admin" -TimeoutSec $TimeoutSeconds
    if ($Response.StatusCode -eq 200 -and $Response.Content -like "*Yönetim Paneli*") {
        Write-Host "✓ Admin panel başarılı" -ForegroundColor Green
        $TestResults += @{Test="Admin Panel"; Status="PASS"; Details="Panel yüklendi"}
    } else {
        Write-Host "✗ Admin panel başarısız" -ForegroundColor Red
        $TestResults += @{Test="Admin Panel"; Status="FAIL"; Details="İçerik bulunamadı"}
    }
} catch {
    Write-Host "✗ Admin panel hatası: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{Test="Admin Panel"; Status="ERROR"; Details=$_.Exception.Message}
}

# 4. API Test (Veri Kaynakları)
Write-Host "`n4. API testi..." -ForegroundColor Cyan
try {
    $Response = Invoke-WebRequest -Uri "$BaseUrl/Admin?tab=datasources" -TimeoutSec $TimeoutSeconds
    if ($Response.StatusCode -eq 200) {
        Write-Host "✓ API erişimi başarılı" -ForegroundColor Green
        $TestResults += @{Test="API Access"; Status="PASS"; Details="Veri kaynakları yüklendi"}
    } else {
        Write-Host "✗ API erişimi başarısız" -ForegroundColor Red
        $TestResults += @{Test="API Access"; Status="FAIL"; Details="HTTP $($Response.StatusCode)"}
    }
} catch {
    Write-Host "✗ API hatası: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{Test="API Access"; Status="ERROR"; Details=$_.Exception.Message}
}

# 5. Performans Test
Write-Host "`n5. Performans testi..." -ForegroundColor Cyan
try {
    $StartTime = Get-Date
    $Response = Invoke-WebRequest -Uri "$BaseUrl/Dashboard" -TimeoutSec $TimeoutSeconds
    $EndTime = Get-Date
    $Duration = ($EndTime - $StartTime).TotalMilliseconds
    
    if ($Duration -lt 3000) {
        Write-Host "✓ Performans iyi ($([math]::Round($Duration, 0)) ms)" -ForegroundColor Green
        $TestResults += @{Test="Performance"; Status="PASS"; Details="$([math]::Round($Duration, 0)) ms"}
    } else {
        Write-Host "⚠ Performans yavaş ($([math]::Round($Duration, 0)) ms)" -ForegroundColor Yellow
        $TestResults += @{Test="Performance"; Status="WARN"; Details="$([math]::Round($Duration, 0)) ms"}
    }
} catch {
    Write-Host "✗ Performans testi hatası: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{Test="Performance"; Status="ERROR"; Details=$_.Exception.Message}
}

# Test Özeti
Write-Host "`n=== Test Özeti ===" -ForegroundColor Green
$PassCount = ($TestResults | Where-Object {$_.Status -eq "PASS"}).Count
$FailCount = ($TestResults | Where-Object {$_.Status -eq "FAIL"}).Count
$ErrorCount = ($TestResults | Where-Object {$_.Status -eq "ERROR"}).Count
$WarnCount = ($TestResults | Where-Object {$_.Status -eq "WARN"}).Count

Write-Host "Toplam Test: $($TestResults.Count)" -ForegroundColor White
Write-Host "Başarılı: $PassCount" -ForegroundColor Green
Write-Host "Başarısız: $FailCount" -ForegroundColor Red
Write-Host "Hata: $ErrorCount" -ForegroundColor Red
Write-Host "Uyarı: $WarnCount" -ForegroundColor Yellow

# Detaylı sonuçlar
Write-Host "`n=== Detaylı Sonuçlar ===" -ForegroundColor Cyan
foreach ($Result in $TestResults) {
    $Color = switch ($Result.Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "ERROR" { "Red" }
        "WARN" { "Yellow" }
        default { "White" }
    }
    Write-Host "$($Result.Test): $($Result.Status) - $($Result.Details)" -ForegroundColor $Color
}

# Rapor dosyası oluştur
$ReportPath = "staging-test-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$TestResults | ConvertTo-Json -Depth 2 | Out-File -FilePath $ReportPath -Encoding UTF8
Write-Host "`nTest raporu kaydedildi: $ReportPath" -ForegroundColor Cyan

# Sonuç
if ($FailCount -eq 0 -and $ErrorCount -eq 0) {
    Write-Host "`n🎉 Tüm testler başarılı! Staging ortamı hazır." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ Bazı testler başarısız! Lütfen hataları kontrol edin." -ForegroundColor Red
    exit 1
}