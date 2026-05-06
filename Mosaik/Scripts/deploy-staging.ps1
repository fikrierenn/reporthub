# BKM Report Panel - Staging Deployment Script
# Staging ortamına deployment için PowerShell scripti

param(
    [string]$ServerName = "staging-server\SQLEXPRESS",
    [string]$DatabaseName = "ReportPanel_Staging",
    [string]$Username = "staging_user",
    [Parameter(Mandatory = $true)]
    [string]$Password,
    [string]$AppPath = "C:\inetpub\wwwroot\ReportPanel_Staging",
    [switch]$SkipDatabase,
    [switch]$SkipApp,
    [switch]$Force
)

Write-Host "=== BKM Report Panel Staging Deployment ===" -ForegroundColor Green
Write-Host "Deployment başlatılıyor..." -ForegroundColor Yellow

# Değişkenler
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectPath = Split-Path -Parent $ScriptPath
$DatabasePath = Join-Path $ProjectPath "Database"
$BackupPath = "C:\Backup\ReportPanel_Staging"

# Backup klasörü oluştur
if (!(Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force
    Write-Host "Backup klasörü oluşturuldu: $BackupPath" -ForegroundColor Green
}

# 1. Veritabanı Deployment
if (!$SkipDatabase) {
    Write-Host "`n1. Veritabanı deployment başlatılıyor..." -ForegroundColor Cyan
    
    try {
        # Mevcut veritabanını yedekle
        if ($Force -or (Read-Host "Mevcut veritabanını yedeklemek istiyor musunuz? (y/n)") -eq 'y') {
            $BackupFile = Join-Path $BackupPath "ReportPanel_Staging_$(Get-Date -Format 'yyyyMMdd_HHmmss').bak"
            Write-Host "Veritabanı yedekleniyor: $BackupFile" -ForegroundColor Yellow
            
            sqlcmd -S $ServerName -U $Username -P $Password -Q "BACKUP DATABASE [$DatabaseName] TO DISK = '$BackupFile'"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Veritabanı başarıyla yedeklendi!" -ForegroundColor Green
            }
        }
        
        # SQL scriptlerini çalıştır
        $SqlFiles = @(
            "01_CreateDatabase.sql",
            "02_CreateTables.sql", 
            "03_SeedData.sql"
        )
        
        foreach ($SqlFile in $SqlFiles) {
            $SqlPath = Join-Path $DatabasePath $SqlFile
            if (Test-Path $SqlPath) {
                Write-Host "Çalıştırılıyor: $SqlFile" -ForegroundColor Yellow
                sqlcmd -S $ServerName -U $Username -P $Password -i $SqlPath
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ $SqlFile başarıyla çalıştırıldı" -ForegroundColor Green
                } else {
                    Write-Host "✗ $SqlFile çalıştırılırken hata oluştu!" -ForegroundColor Red
                    exit 1
                }
            }
        }
        
        Write-Host "Veritabanı deployment tamamlandı!" -ForegroundColor Green
        
    } catch {
        Write-Host "Veritabanı deployment hatası: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# 2. Uygulama Deployment
if (!$SkipApp) {
    Write-Host "`n2. Uygulama deployment başlatılıyor..." -ForegroundColor Cyan
    
    try {
        # Uygulamayı durdur (IIS)
        Write-Host "IIS uygulaması durduruluyor..." -ForegroundColor Yellow
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        if (Get-WebApplication -Name "ReportPanel_Staging" -ErrorAction SilentlyContinue) {
            Stop-WebApplication -Name "ReportPanel_Staging"
        }
        
        # Mevcut uygulamayı yedekle
        if (Test-Path $AppPath) {
            $AppBackupPath = Join-Path $BackupPath "App_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
            Write-Host "Mevcut uygulama yedekleniyor: $AppBackupPath" -ForegroundColor Yellow
            Copy-Item -Path $AppPath -Destination $AppBackupPath -Recurse -Force
        }
        
        # Uygulamayı publish et
        Write-Host "Uygulama publish ediliyor..." -ForegroundColor Yellow
        Set-Location $ProjectPath
        dotnet publish -c Release -o $AppPath --runtime win-x64 --self-contained false
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Uygulama başarıyla publish edildi" -ForegroundColor Green
        } else {
            Write-Host "✗ Uygulama publish edilirken hata oluştu!" -ForegroundColor Red
            exit 1
        }
        
        # Staging konfigürasyonunu kopyala
        $StagingConfig = Join-Path $ProjectPath "appsettings.Staging.json"
        $TargetConfig = Join-Path $AppPath "appsettings.Production.json"
        if (Test-Path $StagingConfig) {
            Copy-Item -Path $StagingConfig -Destination $TargetConfig -Force
            Write-Host "✓ Staging konfigürasyonu kopyalandı" -ForegroundColor Green
        }
        
        # IIS uygulamasını başlat
        Write-Host "IIS uygulaması başlatılıyor..." -ForegroundColor Yellow
        if (Get-WebApplication -Name "ReportPanel_Staging" -ErrorAction SilentlyContinue) {
            Start-WebApplication -Name "ReportPanel_Staging"
        } else {
            # Yeni IIS uygulaması oluştur
            New-WebApplication -Name "ReportPanel_Staging" -Site "Default Web Site" -PhysicalPath $AppPath
        }
        
        Write-Host "Uygulama deployment tamamlandı!" -ForegroundColor Green
        
    } catch {
        Write-Host "Uygulama deployment hatası: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# 3. Health Check
Write-Host "`n3. Health check yapılıyor..." -ForegroundColor Cyan

try {
    # Veritabanı bağlantısını test et
    $TestQuery = "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
    $Result = sqlcmd -S $ServerName -U $Username -P $Password -d $DatabaseName -Q $TestQuery -h -1
    
    if ($Result -gt 0) {
        Write-Host "✓ Veritabanı bağlantısı başarılı ($Result tablo)" -ForegroundColor Green
    }
    
    # Uygulama health check
    if (Test-Path $AppPath) {
        $AppSize = (Get-ChildItem $AppPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
        Write-Host "✓ Uygulama dosyaları hazır ($([math]::Round($AppSize, 2)) MB)" -ForegroundColor Green
    }
    
} catch {
    Write-Host "Health check hatası: $($_.Exception.Message)" -ForegroundColor Red
}

# Özet
Write-Host "`n=== Deployment Özeti ===" -ForegroundColor Green
Write-Host "Sunucu: $ServerName" -ForegroundColor White
Write-Host "Veritabanı: $DatabaseName" -ForegroundColor White
Write-Host "Uygulama Yolu: $AppPath" -ForegroundColor White
Write-Host "Backup Yolu: $BackupPath" -ForegroundColor White
Write-Host "Deployment Zamanı: $(Get-Date)" -ForegroundColor White

Write-Host "`nStaging deployment tamamlandı! 🚀" -ForegroundColor Green
Write-Host "Test URL: http://staging.bkm.com/ReportPanel_Staging" -ForegroundColor Cyan