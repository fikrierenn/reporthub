#!/usr/bin/env bash
# Inline Style Guard — pre-commit hook taslağı.
#
# Amaç: Yeni eklenen veya düzenlenen .cshtml dosyalarda yeni `style="..."`
# attribute oluşumlarını tespit edip commit'i blokla. Mevcut inline style'lar
# borç olarak kalır (Plan 25.1 Faz 1 refactor sonrası enable).
#
# Aktif etmek için: .claude/settings.json içinde pre-commit hook listesine ekle
# veya .git/hooks/pre-commit'ten symlink/source.
#
# Karar 2026-05-10: kullanıcı "satır için style kullanımı minimum yasak
# seviyesinde olmalı bunuınla iligi önlemlerini al" — bu hook o önlemin
# enforcement'ı. ui-patterns.md §0'da kural belgeli.
#
# Şimdilik DISABLED — Plan 25.1 Faz 1 refactor (HIGH 181 oluşum) bitince
# enable edilir (`if true; then` veya hook listesine ekleme).

set -e

# Plan 25.1 Faz 1 tamamlandı (2026-05-11) — hook aktif
DISABLED=false
if [ "$DISABLED" = "true" ]; then
    exit 0
fi

# Sadece staged .cshtml dosyaları
staged_cshtml=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cshtml$' || true)
[ -z "$staged_cshtml" ] && exit 0

# Bilinen istisnalar:
# - Print.cshtml (A4 print template, view-local <style>)
# - display:contents (form-as-grid pattern)
# - --w: var(...) veri-driven CSS variable
violations=0
for f in $staged_cshtml; do
    [ -f "$f" ] || continue

    # Bu PR'da eklenen yeni satırları al ('+' prefix, header'ları '++' ile filtrele)
    added=$(git diff --cached -U0 "$f" | grep -E '^\+' | grep -v '^\+\+\+')

    # Inline style attribute eklenmiş mi?
    new_inline=$(echo "$added" | grep -E 'style="' || true)

    # İstisnaları çıkar
    new_inline=$(echo "$new_inline" \
        | grep -v 'display:contents' \
        | grep -v -- '--w:' \
        || true)

    if [ -n "$new_inline" ]; then
        echo "[inline-style-guard] Yeni inline style tespit edildi: $f"
        echo "$new_inline" | head -5 | sed 's/^/    /'
        violations=$((violations + 1))
    fi
done

if [ "$violations" -gt 0 ]; then
    echo ""
    echo "[inline-style-guard] $violations dosyada yeni inline style var."
    echo "Çözüm: components.css'e utility class ekle, view'da kullan."
    echo "Detay: .claude/rules/ui-patterns.md §0 (Inline Style YASAĞI)"
    exit 1
fi

exit 0
