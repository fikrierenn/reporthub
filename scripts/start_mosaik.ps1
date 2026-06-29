# Mosaik portal sunucusunu baslatir + sistem tepsisinde (tray) ikon gosterir.
# Task Scheduler "Mosaik-Portal-Sunucu" (logon) bunu powershell -WindowStyle Hidden ile cagirir.
# Manuel test: powershell -ExecutionPolicy Bypass -File scripts\start_mosaik.ps1
#
# Tray ikonu: cift-tik = portali ac. Sag-tik menu = Portali Ac / Yeniden Baslat / Yeniden Derle ve Baslat / Durdur ve Cik.
# Cokerse 5sn sonra otomatik yeniden baslar (5 hizli cokme = durur, balon uyari).
#
# Not: ASPNETCORE_ENVIRONMENT=Development -> appsettings.Development.json (lokal SA sifresi, gitignored)
#      yuklenir; Mosaik DB baglantisi bu yuzden Development sart (Production'da connection string kopar).
# DOSYA UTF-8 BOM ile kaydedilmeli (menu Turkce metni icin) - PS 5.1 BOM'suz UTF-8'i bozar.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Konsol penceresini TAMAMEN KOPAR (FreeConsole) - sadece gizlemek yetmez.
try {
    $sig = '[DllImport("kernel32.dll")] public static extern bool FreeConsole();'
    $t = Add-Type -MemberDefinition $sig -Name 'KernelCon' -Namespace 'Win32Con' -PassThru -ErrorAction Stop
    [void]$t::FreeConsole()
} catch { }

$proj    = "D:\Dev\reporthub\Mosaik\Mosaik.csproj"
$dll     = "D:\Dev\reporthub\Mosaik\bin\Release\net10.0\Mosaik.dll"
$workDir = "D:\Dev\reporthub\Mosaik"   # content root: wwwroot + App_Data + appsettings goreli yollari
$iconIco = "D:\Dev\reporthub\Mosaik\wwwroot\favicon.ico"
$panelUrl = "http://localhost:5197"

if (-not (Test-Path $dll)) {
    dotnet build $proj -c Release | Out-Null
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://0.0.0.0:5197"

# --- Durum ---
$script:running   = $true
$script:fastFails = 0
$script:proc      = $null
$script:startAt   = Get-Date

function Start-Mosaik {
    $script:startAt = Get-Date
    $script:proc = Start-Process -FilePath "dotnet" -ArgumentList "`"$dll`"" `
        -WorkingDirectory $workDir -WindowStyle Hidden -PassThru
}

function Stop-Mosaik {
    if ($script:proc -and -not $script:proc.HasExited) {
        try { $script:proc.Kill() } catch {}
    }
}

# Release'i yeniden derle + yeniden baslat (kod degisince taze surum).
function Rebuild-Mosaik {
    $ni.ShowBalloonTip(3000, "Mosaik Portal", "Derleniyor... (15-30 sn)", [System.Windows.Forms.ToolTipIcon]::Info)
    Stop-Mosaik
    Start-Sleep -Milliseconds 900   # DLL kilidini birak
    $ok = $true
    try {
        & dotnet build $proj -c Release 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { $ok = $false }
    } catch { $ok = $false }
    $script:fastFails = 0
    $script:running   = $true
    Start-Mosaik
    if ($ok) {
        $ni.ShowBalloonTip(3000, "Mosaik Portal", "Yeni surum derlendi ve baslatildi.", [System.Windows.Forms.ToolTipIcon]::Info)
    } else {
        $ni.ShowBalloonTip(5000, "Mosaik Portal", "Derleme BASARISIZ - eski surumle baslatildi.", [System.Windows.Forms.ToolTipIcon]::Warning)
    }
}

# --- Tray ikonu ---
$ni = New-Object System.Windows.Forms.NotifyIcon
try {
    $ni.Icon = New-Object System.Drawing.Icon($iconIco)
} catch {
    $ni.Icon = [System.Drawing.SystemIcons]::Application
}
$ni.Text = "Mosaik Portal - $panelUrl"
$ni.Visible = $true

$menu = New-Object System.Windows.Forms.ContextMenuStrip
$miAc = $menu.Items.Add("Portali Ac")
$miAc.Add_Click({ Start-Process $panelUrl })
$miYeniden = $menu.Items.Add("Yeniden Baslat")
$miYeniden.Add_Click({
    Stop-Mosaik
    $script:fastFails = 0
    $script:running = $true
    Start-Mosaik
    $ni.ShowBalloonTip(3000, "Mosaik Portal", "Yeniden baslatildi.", [System.Windows.Forms.ToolTipIcon]::Info)
})
$miDerle = $menu.Items.Add("Yeniden Derle ve Baslat")
$miDerle.Add_Click({ Rebuild-Mosaik })
$menu.Items.Add((New-Object System.Windows.Forms.ToolStripSeparator)) | Out-Null
$miCik = $menu.Items.Add("Durdur ve Cik")
$miCik.Add_Click({
    $script:running = $false
    Stop-Mosaik
    $ni.Visible = $false
    $ni.Dispose()
    [System.Windows.Forms.Application]::Exit()
})
$ni.ContextMenuStrip = $menu
$ni.Add_DoubleClick({ Start-Process $panelUrl })

# --- Izleme/yeniden-baslat dongusu (timer) ---
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 5000
$timer.Add_Tick({
    if (-not $script:running) { return }
    if ($script:proc -and $script:proc.HasExited) {
        $up = ((Get-Date) - $script:startAt).TotalSeconds
        if ($up -lt 15) { $script:fastFails++ } else { $script:fastFails = 0 }
        if ($script:fastFails -ge 5) {
            $script:running = $false
            $ni.ShowBalloonTip(5000, "Mosaik Portal", "Surekli cokuyor - otomatik yeniden baslatma durduruldu. Sag-tik > Yeniden Baslat.", [System.Windows.Forms.ToolTipIcon]::Error)
            return
        }
        Start-Mosaik
    }
})

Start-Mosaik
$timer.Start()
$ni.ShowBalloonTip(3000, "Mosaik Portal", "Sunucu calisiyor: $panelUrl", [System.Windows.Forms.ToolTipIcon]::Info)

# Mesaj dongusu (tray canli kalsin). Cik menusu Application.Exit cagirir.
$ctx = New-Object System.Windows.Forms.ApplicationContext
[System.Windows.Forms.Application]::Run($ctx)
