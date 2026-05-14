#!/usr/bin/env bash
# UTF-8 zorla (Windows Git Bash dotnet çıktısı CP1252'ye düşebilir)
export LC_ALL=C.UTF-8 LANG=C.UTF-8
export DOTNET_CLI_UI_LANGUAGE=en-US
# scripts/refresh-arch-map.sh — ARCHITECTURE_MAP.md'nin "live" bölümlerini günceller.
#
# Marker'lar arasındaki içerik yeniden üretilir:
#   <!-- AUTO:APPMODULES:START --> ... <!-- AUTO:APPMODULES:END -->
#   <!-- AUTO:ADMIN_VIEWS:START --> ... <!-- AUTO:ADMIN_VIEWS:END -->
#   <!-- AUTO:CONTROLLER_ROUTES:START --> ... <!-- AUTO:CONTROLLER_ROUTES:END -->
#   <!-- AUTO:LAST_REFRESH --> 2026-MM-DD <!-- /AUTO:LAST_REFRESH -->
#
# Kullanım: bash scripts/refresh-arch-map.sh
# Çıktı: docs/ARCHITECTURE_MAP.md yerinde güncellenir.

set +e  # sqlcli/dotnet run non-zero exit verirse devam et

REPO="${CLAUDE_PROJECT_DIR:-D:/Dev/reporthub}"
cd "$REPO"

MAP="docs/ARCHITECTURE_MAP.md"
TMP=$(mktemp)

if [ ! -f "$MAP" ]; then
    echo "Hata: $MAP bulunamadı"
    exit 1
fi

# 1. AppModules live state — sqlcli ile çek
appmodules_block() {
    echo "<!-- AUTO:APPMODULES:START -->"
    echo "_Otomatik üretildi — \`scripts/refresh-arch-map.sh\` tarafından. Elle düzenleme yok._"
    echo ""
    echo "| ModuleKey | DisplayName | GroupKey | IsEnabled | ModuleType | AssemblyName |"
    echo "|---|---|---|---|---|---|"

    # sqlcli'yi pre-built dll ile çağır — `dotnet run` MSBuild temp clash veriyor (script subshell'de)
    # Çıktı Windows-1254 (CP1254) — iconv ile UTF-8'e çevir (iki adım: capture, sonra çevir)
    local sqlcli_dll="D:/Dev/sqlcli/bin/Debug/net10.0/sqlcli.dll"
    local raw=""
    if [ -f "$sqlcli_dll" ]; then
        local raw_cp1254
        raw_cp1254=$(dotnet "$sqlcli_dll" query \
            "SELECT ModuleKey, DisplayName, ISNULL(GroupKey,'') AS GroupKey, CAST(IsEnabled AS INT) AS IsEnabled, ModuleType, ISNULL(AssemblyName,'') AS AssemblyName FROM dbo.AppModules ORDER BY SortOrder" \
            --format csv 2>&1)
        if [ -n "$raw_cp1254" ]; then
            # Önce ANSI escape kodlarını sil (banner color codes), sonra iconv UTF-8
            raw=$(printf "%s" "$raw_cp1254" | sed 's/\x1b\[[0-9;]*m//g' | iconv -f CP857 -t UTF-8 2>/dev/null)
            [ -z "$raw" ] && raw=$(printf "%s" "$raw_cp1254" | sed 's/\x1b\[[0-9;]*m//g')
        fi
    fi

    # Banner satırını + boş satırları temizle ( -a: Türkçe UTF-8 binary sayılmasın)
    raw=$(echo "$raw" | grep -av '^=== sqlcli' | grep -av '^$')

    if [ -z "$raw" ] || ! echo "$raw" | grep -aq "^ModuleKey"; then
        echo "| _sqlcli erişilemedi — manuel kontrol: \`dotnet run --project D:/Dev/sqlcli -- query 'SELECT * FROM AppModules' --format csv\`_ | | | | | |"
    else
        # İlk satır CSV başlığı (ModuleKey,DisplayName,...), atla
        echo "$raw" | tail -n +2 | awk -F',' '{
            iseen = ($4 == "1") ? "✓" : "✗"
            printf "| %s | %s | %s | %s | %s | %s |\n", $1, $2, $3, iseen, $5, $6
        }'
    fi
    echo "<!-- AUTO:APPMODULES:END -->"
}

# 2. Admin views — non-partial liste
admin_views_block() {
    echo "<!-- AUTO:ADMIN_VIEWS:START -->"
    echo "_Otomatik üretildi. Mosaik/Views/Admin/*.cshtml (partial hariç)._"
    echo ""
    for f in Mosaik/Views/Admin/*.cshtml; do
        name=$(basename "$f" .cshtml)
        case "$name" in
            _*) continue ;;
        esac
        inline=$(grep -c 'style="' "$f" 2>/dev/null)
        [ -z "$inline" ] && inline=0
        lines=$(wc -l < "$f" | tr -d ' ')
        echo "- \`$name.cshtml\` (lines=$lines, inline-style=$inline)"
    done
    echo "<!-- AUTO:ADMIN_VIEWS:END -->"
}

# 3. AdminController routes — partial'ları topla
controller_routes_block() {
    echo "<!-- AUTO:CONTROLLER_ROUTES:START -->"
    echo "_Otomatik üretildi. AdminController partial'larından çıkarılır._"
    echo ""
    echo '```'
    grep -hnE '^\s*\[Route\("[^"]+"\)\]|^\s*public.*Task<IActionResult>\s+\w+|^\s*public\s+IActionResult\s+\w+' \
        Mosaik/Controllers/AdminController*.cs 2>/dev/null | \
        sed 's/^[[:space:]]*//' | head -120
    echo '```'
    echo "<!-- AUTO:CONTROLLER_ROUTES:END -->"
}

# 4. Marker bazlı replace fonksiyonu
replace_block() {
    local marker_name="$1"
    local content_fn="$2"
    local start_marker="<!-- AUTO:${marker_name}:START -->"
    local end_marker="<!-- AUTO:${marker_name}:END -->"

    # awk ile marker'lar arasındaki bloğu yeni içerikle değiştir
    awk -v start="$start_marker" -v end="$end_marker" -v new="$($content_fn)" '
        $0 ~ start { print new; skip=1; next }
        $0 ~ end   { skip=0; next }
        !skip { print }
    ' "$MAP" > "$TMP"
    mv "$TMP" "$MAP"
}

# 5. Last refresh tarihini güncelle
update_refresh_date() {
    local today
    today=$(date +%Y-%m-%d)
    sed -i.bak "s|<!-- AUTO:LAST_REFRESH -->[^<]*<!-- /AUTO:LAST_REFRESH -->|<!-- AUTO:LAST_REFRESH --> $today <!-- /AUTO:LAST_REFRESH -->|g" "$MAP"
    rm -f "${MAP}.bak"
}

# 6. CLAUDE.md inline marker'larını güncelle (test count + migration range + controllers)
update_claude_md() {
    local CLAUDE="CLAUDE.md"
    [ ! -f "$CLAUDE" ] && return 0

    # Test count: Mosaik.Tests altındaki [Fact] + [Theory] sayısı
    local test_count
    test_count=$(grep -rE '^\s*\[Fact|^\s*\[Theory' Mosaik.Tests/ 2>/dev/null | wc -l | tr -d ' ')
    [ -z "$test_count" ] || [ "$test_count" = "0" ] && test_count="?"

    # Migration range: en küçük + en büyük NN_ prefix
    local first_mig last_mig mig_count
    first_mig=$(ls Mosaik/Database/*.sql 2>/dev/null | xargs -n1 basename | grep -oE '^[0-9]+' | sort -n | head -1)
    last_mig=$(ls Mosaik/Database/*.sql 2>/dev/null | xargs -n1 basename | grep -oE '^[0-9]+' | sort -n | tail -1)
    mig_count=$(ls Mosaik/Database/*.sql 2>/dev/null | xargs -n1 basename | grep -cE '^[0-9]+_')
    [ -z "$first_mig" ] && first_mig="?"
    [ -z "$last_mig" ] && last_mig="?"
    [ -z "$mig_count" ] && mig_count="?"
    local mig_range="${first_mig}_ → ${last_mig}_"

    # Controllers: Mosaik/Controllers/*.cs (partial olmayanlar) — sadece base isim
    local controllers
    controllers=$(ls Mosaik/Controllers/*Controller.cs 2>/dev/null | xargs -n1 basename | \
        sed 's/Controller\.cs$//' | grep -v '\.' | sort -u | \
        awk 'BEGIN{ORS=""} {if(NR>1) printf ", "; printf "`%s`", $0}')
    [ -z "$controllers" ] && controllers="?"

    # sed ile marker arası değiştir (basit string replace, regex değil — '/' içermez)
    sed -i.bak \
        -e "s|<!-- AUTO:TEST_COUNT -->[^<]*<!-- /AUTO:TEST_COUNT -->|<!-- AUTO:TEST_COUNT -->${test_count}<!-- /AUTO:TEST_COUNT -->|g" \
        -e "s|<!-- AUTO:MIGRATION_RANGE -->[^<]*<!-- /AUTO:MIGRATION_RANGE -->|<!-- AUTO:MIGRATION_RANGE -->${mig_range}<!-- /AUTO:MIGRATION_RANGE -->|g" \
        -e "s|<!-- AUTO:MIGRATION_COUNT -->[^<]*<!-- /AUTO:MIGRATION_COUNT -->|<!-- AUTO:MIGRATION_COUNT -->${mig_count}<!-- /AUTO:MIGRATION_COUNT -->|g" \
        -e "s|<!-- AUTO:CONTROLLERS -->[^<]*<!-- /AUTO:CONTROLLERS -->|<!-- AUTO:CONTROLLERS -->${controllers}<!-- /AUTO:CONTROLLERS -->|g" \
        "$CLAUDE"
    rm -f "${CLAUDE}.bak"

    echo "  CLAUDE.md: test=$test_count, migration=$mig_range ($mig_count), controllers=$controllers"
}

# Çalıştır
echo "→ ARCHITECTURE_MAP.md yenileniyor..."
replace_block "APPMODULES" appmodules_block
replace_block "ADMIN_VIEWS" admin_views_block
replace_block "CONTROLLER_ROUTES" controller_routes_block
update_refresh_date

echo "→ CLAUDE.md inline marker'lari yenileniyor..."
update_claude_md

echo "✓ Bitti. Diff:"
git diff --stat "$MAP" CLAUDE.md 2>/dev/null || echo "(diff yok / git dışı)"
