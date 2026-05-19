#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$outFile     = "TUM_PROJE_KODLARI.txt"
$currentPath = (Get-Location).Path

Write-Host "Calisma Dizini : $currentPath" -ForegroundColor Cyan

# --- Uzanti filtresi ---
$extensions = @(
    ".cs", ".py", ".md", ".json", ".sql", ".html",
    ".css", ".js", ".ts", ".xml", ".yaml", ".yml",
    ".txt", ".bat", ".sh", ".dockerfile"
)

# --- Her zaman haric tutulan klasorler ---
$excludeFolders = @(
    # JS / Node
    "node_modules", ".next", ".nuxt", ".svelte-kit", ".turbo",
    # IDE / editor
    ".vs", ".vscode", ".idea",
    # VCS
    ".git", ".github",
    # Python
    "venv", ".venv", "env", "__pycache__", ".tox", ".pytest_cache", ".mypy_cache",
    # .NET
    "bin", "obj",
    # Java / Gradle / Maven
    "target", ".gradle", "gradle",
    # PHP / Go / Ruby
    "vendor",
    # Build & test output
    "dist", "build", "out", "coverage", "artifacts", "test-results",
    # DB migrations
    "migrations"
)

# --- Her zaman haric tutulan dosyalar (tam ad) ---
$excludeFileNames = @(
    # Node / JS
    "package.json", "package-lock.json", "yarn.lock", "pnpm-lock.yaml",
    "next.config.js", "next.config.ts", "next.config.mjs",
    "postcss.config.js", "postcss.config.mjs",
    "tailwind.config.js", "tailwind.config.ts",
    "eslint.config.js", "eslint.config.mjs", ".eslintrc.json", ".eslintrc.js",
    ".prettierrc", ".prettierrc.json",
    "jest.config.js", "jest.config.ts", "vitest.config.ts",
    "tsconfig.json", "tsconfig.node.json",
    # Python
    "requirements.txt", "requirements-dev.txt", "requirements-test.txt",
    "Pipfile", "Pipfile.lock", "poetry.lock",
    "setup.py", "setup.cfg", "pyproject.toml", "MANIFEST.in",
    "tox.ini", ".flake8", "mypy.ini", ".pylintrc", "pytest.ini",
    # .NET
    "global.json", "nuget.config", "packages.lock.json",
    "web.config", "app.config", "launchSettings.json",
    "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props",
    # Java
    "pom.xml", "build.gradle", "settings.gradle", "gradlew", "gradlew.bat",
    # Ruby
    "Gemfile", "Gemfile.lock",
    # PHP
    "composer.json", "composer.lock",
    # Go
    "go.mod", "go.sum",
    # Rust
    "Cargo.toml", "Cargo.lock",
    # Container
    "Dockerfile", "docker-compose.yml", "docker-compose.yaml",
    # Genel
    "README.md", ".gitignore", ".gitattributes", ".editorconfig",
    # Bu script'in kendi ciktisi
    "TUM_PROJE_KODLARI.txt"
)

# --- Wildcard ile haric tutulan dosyalar ---
$excludeWildcards = @(
    "appsettings.*.json",   # .NET ortam config
    "*.log",                # log dosyalari
    "*.tsbuildinfo",        # TypeScript build info
    "*.pem", "*.key", "*.pfx", "*.p12", "*.cer",  # sertifika/key
    ".env", ".env.*",       # environment dosyalari
    ".DS_Store"             # macOS
)

# --- Python venv tespiti (pyvenv.cfg iceren her klasor) ---
$venvDirs = Get-ChildItem -Path $currentPath -Recurse -Filter "pyvenv.cfg" -ErrorAction SilentlyContinue |
            ForEach-Object { $_.Directory.Name }
if ($venvDirs.Count -gt 0) {
    $excludeFolders += $venvDirs
    Write-Host "Python venv tespit edildi: $($venvDirs -join ', ')" -ForegroundColor DarkCyan
}

# --- .gitignore oku, varsa ek pattern ekle ---
$giIgnoreFolders   = @()
$giIgnoreFileNames = @()
$giIgnoreWildcards = @()

$gitignorePath = Join-Path $currentPath ".gitignore"
if (Test-Path $gitignorePath) {
    $lines = Get-Content $gitignorePath |
             Where-Object { -not $_.TrimStart().StartsWith('#') -and $_.Trim() -ne '' }
    foreach ($line in $lines) {
        $line = $line.Trim().TrimStart('/')
        if     ($line.EndsWith('/'))  { $giIgnoreFolders   += $line.TrimEnd('/') }
        elseif ($line.Contains('*')) { $giIgnoreWildcards += $line }
        else                          { $giIgnoreFileNames += $line }
    }
    Write-Host ".gitignore: $($giIgnoreFolders.Count) klasor, $($giIgnoreFileNames.Count) dosya, $($giIgnoreWildcards.Count) wildcard" -ForegroundColor DarkCyan
}

# --- Proje tipi tespiti ---
$isCSharp = [bool](Get-ChildItem $currentPath -Recurse -Depth 4 -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1)
$isPython = (Test-Path (Join-Path $currentPath "requirements.txt")) -or
            (Test-Path (Join-Path $currentPath "pyproject.toml"))    -or
            (Test-Path (Join-Path $currentPath "setup.py"))
$isNode   = Test-Path (Join-Path $currentPath "package.json")
$isJava   = (Test-Path (Join-Path $currentPath "pom.xml")) -or
            (Test-Path (Join-Path $currentPath "build.gradle"))
$isGo     = Test-Path (Join-Path $currentPath "go.mod")
$isRust   = Test-Path (Join-Path $currentPath "Cargo.toml")

$detectedTypes = @()
if ($isCSharp) { $detectedTypes += "C#/.NET" }
if ($isPython) { $detectedTypes += "Python"  }
if ($isNode)   { $detectedTypes += "Node.js" }
if ($isJava)   { $detectedTypes += "Java"    }
if ($isGo)     { $detectedTypes += "Go"      }
if ($isRust)   { $detectedTypes += "Rust"    }

if ($detectedTypes.Count -gt 0) {
    Write-Host "Proje tipi: $($detectedTypes -join ', ')" -ForegroundColor Magenta
} else {
    Write-Host "Proje tipi tespit edilemedi, genel filtreler uygulanacak" -ForegroundColor Yellow
}

# --- Dosya tarama ---
try {
    $allFiles = Get-ChildItem -Path $currentPath -Recurse -File -ErrorAction SilentlyContinue

    $files = $allFiles | Where-Object {
        $f = $_

        # Uzanti kontrolu
        if (-not ($extensions -contains $f.Extension.ToLower())) { return $false }

        # Klasor haric tutma (statik + gitignore)
        $relPath = $f.FullName.Substring($currentPath.Length)
        $parts   = $relPath.Split([System.IO.Path]::DirectorySeparatorChar) |
                   Where-Object { $_ -ne '' }

        # Son eleman dosya adi, ondan oncekiler klasorler
        if ($parts.Count -gt 1) {
            $dirs = $parts[0..($parts.Count - 2)]
            foreach ($d in $dirs) {
                if ($excludeFolders  -contains $d) { return $false }
                if ($giIgnoreFolders -contains $d) { return $false }
            }
        }

        # Tam dosya adi (statik + gitignore)
        if ($excludeFileNames  -contains $f.Name) { return $false }
        if ($giIgnoreFileNames -contains $f.Name) { return $false }

        # Wildcard (statik + gitignore)
        foreach ($w in $excludeWildcards)  { if ($f.Name -like $w) { return $false } }
        foreach ($w in $giIgnoreWildcards) { if ($f.Name -like $w) { return $false } }

        return $true
    }

    Write-Host "$($files.Count) dosya dahil edilecek" -ForegroundColor Yellow
    Write-Host ""

    if (Test-Path $outFile) { Remove-Item $outFile }

    foreach ($f in $files) {
        $rel = $f.FullName.Substring($currentPath.Length)
        Write-Host "  + $rel" -ForegroundColor Green
        Add-Content -Path $outFile -Value ("=" * 80)
        Add-Content -Path $outFile -Value " DOSYA: $rel"
        Add-Content -Path $outFile -Value ("=" * 80)
        try {
            $content = Get-Content -Path $f.FullName -Raw -ErrorAction Stop
            Add-Content -Path $outFile -Value $content
        } catch {
            Add-Content -Path $outFile -Value "[HATA: Dosya okunamadi - $($_.Exception.Message)]"
        }
        Add-Content -Path $outFile -Value ""
    }

    Write-Host ""
    Write-Host "ISLEM TAMAMLANDI >> $outFile" -ForegroundColor Yellow

} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
