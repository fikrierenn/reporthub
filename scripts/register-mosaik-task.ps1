# Windows Task Scheduler'a "Mosaik-Portal-Sunucu" gorevini kaydeder.
# Sen Windows'a GIRIS YAPINCA (logon) Mosaik portal sunucusu arka planda (gizli) ayaga kalkar.
# http://localhost:5197 (LAN'da http://<bu-pc-ip>:5197 -> telefon/diger cihaz).
#
# Logon tetikleyici: sifre SAKLAMAZ, ag/SQL erisimi sorunsuz, ADMIN GEREKMEZ (port 5197 > 1024).
# ASCII-only (Windows PowerShell 5.1 UTF-8 BOM'suz Turkce'yi yanlis okur -> parse hatasi).
#
# Kullanim (yonetici PowerShell GEREKMEZ):
#   cd D:\Dev\reporthub
#   .\scripts\register-mosaik-task.ps1
#
# Silmek icin:
#   Unregister-ScheduledTask -TaskName "Mosaik-Portal-Sunucu" -Confirm:$false

$taskName = "Mosaik-Portal-Sunucu"
$ps1 = "D:\Dev\reporthub\scripts\start_mosaik.ps1"

if (-not (Test-Path $ps1)) {
    Write-Host "HATA: $ps1 bulunamadi" -ForegroundColor Red
    exit 1
}

# Mevcut gorev varsa kaldir (idempotent)
$existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Mevcut gorev bulundu, kaldiriliyor..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

# Aksiyon: powershell gizli pencerede start_mosaik.ps1 calistirir (dotnet de gizli)
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$ps1`""

# Tetikleyici: bu kullanici giris yapinca
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME

# Ayarlar: surekli calissin (sure limiti yok), idle'da durmasin
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -StartWhenAvailable -DontStopOnIdleEnd `
    -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)

# Mevcut kullanici, en yuksek yetki gerekmez
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger `
    -Settings $settings -Principal $principal `
    -Description "Mosaik kurumsal portal (ASP.NET Core MVC) sunucusu - logon'da gizli ayaga kalkar, http://localhost:5197" | Out-Null

Write-Host "OK: '$taskName' kaydedildi (logon tetikleyici)." -ForegroundColor Green
Write-Host "Simdi baslatiliyor (sonraki girise kadar beklemeden)..." -ForegroundColor Cyan
Start-ScheduledTask -TaskName $taskName
Start-Sleep -Seconds 12

# Saglik kontrolu
try {
    $r = Invoke-WebRequest -Uri "http://localhost:5197" -UseBasicParsing -TimeoutSec 10 -MaximumRedirection 0 -ErrorAction Stop
    Write-Host "Sunucu AYAKTA: http://localhost:5197 (HTTP $($r.StatusCode))" -ForegroundColor Green
} catch {
    $sc = $_.Exception.Response.StatusCode.value__
    if ($sc) { Write-Host "Sunucu AYAKTA: http://localhost:5197 (HTTP $sc - login yonlendirme)" -ForegroundColor Green }
    else { Write-Host "Henuz cevap yok (ilk acilis yukleme surebilir). 20-30 sn sonra http://localhost:5197 dene." -ForegroundColor Yellow }
}

$ip = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
      Where-Object { $_.IPAddress -like '192.168.*' } |
      Select-Object -First 1 -ExpandProperty IPAddress
if ($ip) { Write-Host "LAN erisimi (telefon): http://${ip}:5197" -ForegroundColor Cyan }
