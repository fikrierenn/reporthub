# fix-path.ps1 — Windows PATH temizleyici
#
# Kullanım:
#   .\fix-path.ps1                    # User PATH dry-run
#   .\fix-path.ps1 -Apply             # User PATH temizle (yedek alır)
#   .\fix-path.ps1 -Machine           # System PATH dry-run (admin gerek)
#   .\fix-path.ps1 -Machine -Apply    # System PATH temizle (admin)
#
# Yapar:
#   - Boş entry'leri siler
#   - $env:USERNAME literal gibi PowerShell variable expansion yapılmamış girişleri siler
#   - Aynı path'in (case-insensitive, trailing slash normalize) duplicate'lerini kaldırır
#   - Var olmayan klasörleri RAPOR EDER ama otomatik silmez (network drive, dış disk olabilir)
#   - Apply sırasında eski PATH'i ~/path-backup-<scope>-<timestamp>.txt dosyasına yedekler
#
# Restart sonra: cmd/PowerShell pencerelerini kapat-aç. Açık session'lar eski PATH'i tutar.

[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$Machine
)

$scope = if ($Machine) { 'Machine' } else { 'User' }
$current = [Environment]::GetEnvironmentVariable('Path', $scope)

if ($null -eq $current -or $current.Length -eq 0) {
    Write-Host "PATH ($scope) bos veya okunamadi." -ForegroundColor Red
    return
}

Write-Host ""
Write-Host "=== $scope PATH analizi ===" -ForegroundColor Cyan
Write-Host "Mevcut karakter: $($current.Length) (Windows limiti: 8191)"

$entries = $current -split ';' | ForEach-Object { $_.Trim() }
Write-Host "Toplam entry: $($entries.Count)"

# Bilinen-bozuk pattern'ler
function Test-IsBroken([string]$entry) {
    if ([string]::IsNullOrWhiteSpace($entry)) { return $true }
    if ($entry -match '\$env:') { return $true }            # PowerShell literal sızıntısı
    if ($entry -match '\$\{[^}]+\}') { return $true }       # ${var} expansion sizmis
    if ($entry -match '\.exe$') { return $true }            # binary, klasör değil (örn. cloudflared.exe)
    return $false
}

# Duplicate kaldırma — case-insensitive, trailing slash normalize
$seen = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)
$kept = New-Object System.Collections.ArrayList
$dupes = New-Object System.Collections.ArrayList
$broken = New-Object System.Collections.ArrayList

foreach ($e in $entries) {
    if (Test-IsBroken $e) {
        [void]$broken.Add($e)
        continue
    }
    $key = $e.TrimEnd('\').ToLowerInvariant()
    if ($seen.Add($key)) {
        [void]$kept.Add($e)
    } else {
        [void]$dupes.Add($e)
    }
}

# Var olmayan klasörler (raporlama amaçlı — silmiyoruz, network drive ihtimali)
$missing = $kept | Where-Object { -not (Test-Path -LiteralPath $_ -ErrorAction SilentlyContinue) }

$cleaned = $kept -join ';'

Write-Host ""
Write-Host "=== Sonuc ===" -ForegroundColor Yellow
Write-Host "  Korunan      : $($kept.Count) entry, $($cleaned.Length) karakter"
Write-Host "  Atilan dup   : $($dupes.Count)"
Write-Host "  Atilan bozuk : $($broken.Count)"
Write-Host "  Var olmayan  : $($missing.Count) (raporlanir, silinmez)"
Write-Host ""

if ($broken.Count -gt 0) {
    Write-Host "Bozuk girisler (silinecek):" -ForegroundColor Red
    $broken | ForEach-Object {
        $display = if ([string]::IsNullOrWhiteSpace($_)) { '<bos>' } else { $_ }
        Write-Host "  $display"
    }
    Write-Host ""
}

if ($dupes.Count -gt 0) {
    Write-Host "Duplicate girisler (silinecek):" -ForegroundColor DarkYellow
    $dupes | Select-Object -First 30 | ForEach-Object { Write-Host "  $_" }
    if ($dupes.Count -gt 30) { Write-Host "  ... ve $($dupes.Count - 30) tane daha" }
    Write-Host ""
}

if ($missing.Count -gt 0) {
    Write-Host "Var olmayan klasorler (silmiyoruz, kontrol edin):" -ForegroundColor Yellow
    $missing | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
}

if (-not $Apply) {
    Write-Host "DRY-RUN: gercek degisiklik yapilmadi." -ForegroundColor Cyan
    Write-Host "Uygulamak icin: .\fix-path.ps1 -Apply" -ForegroundColor Cyan
    if ($Machine) {
        Write-Host "(System PATH icin admin PowerShell gerekir.)" -ForegroundColor Yellow
    }
    return
}

# Yedek al
$backupDir = $env:USERPROFILE
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = Join-Path $backupDir "path-backup-$scope-$timestamp.txt"
$current | Out-File -FilePath $backup -Encoding utf8
Write-Host "Yedek: $backup" -ForegroundColor Green

# Uygula
try {
    [Environment]::SetEnvironmentVariable('Path', $cleaned, $scope)
    Write-Host "$scope PATH guncellendi." -ForegroundColor Green
    Write-Host "Acik cmd/PowerShell pencerelerini kapat-ac (eski PATH cached'dir)." -ForegroundColor Yellow
} catch {
    Write-Host "HATA: $_" -ForegroundColor Red
    if ($Machine) {
        Write-Host "System PATH yazma yetkisi yok. PowerShell'i 'Run as Administrator' ile ac." -ForegroundColor Yellow
    }
    Write-Host "Yedek korundu: $backup" -ForegroundColor Yellow
}
