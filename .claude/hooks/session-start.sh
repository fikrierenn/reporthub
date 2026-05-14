#!/usr/bin/env bash
# SessionStart hook — Claude'a "son 3 gun ne degisti, aktif TODO'lar, uncommitted sayisi" enjekte eder.
# Ciktisi stdout'a yaziliyor, Claude bunu additionalContext olarak goruyor.

set -e

REPO="${CLAUDE_PROJECT_DIR:-D:/Dev/reporthub}"
cd "$REPO" 2>/dev/null || exit 0

echo "## ReportHub — Oturum Basi Ozet"
echo ""

echo "### Son 3 gun commit'ler"
git log --since='3 days ago' --oneline 2>/dev/null | head -10
echo ""

echo "### Uncommitted dosya sayisi"
count=$(git status --porcelain 2>/dev/null | wc -l)
echo "$count dosya"
if [ "$count" -gt 15 ]; then
    echo ""
    echo "UYARI: 15 dosya esigi asildi. Yeni is baslamadan once commit-split gerek."
fi
echo ""

echo "### Aktif TODO basliklari (ilk 15)"
grep -E '^### |^- \[ \]' TODO.md 2>/dev/null | head -15
echo ""

# Stale-claim detector: post-review hardening commit'i son 7 gunde varsa, TODO
# Code review backlog'unun HIGH'lari muhtemelen kapanmistir. Action almadan once
# canli koddan dogrulama zorunlu. Detay: .claude/rules/todo-verification.md.
hardening_commits=$(git log --since='7 days ago' --grep='hardening\|fix(security)\|post-review' --oneline 2>/dev/null | wc -l | tr -d ' ')
open_high_count=$(grep -cE '^- \[ \] \*\*HIGH-' TODO.md 2>/dev/null | head -1 || true)
[ -z "$open_high_count" ] && open_high_count=0
if [ "$hardening_commits" -gt 0 ] && [ "$open_high_count" -gt 0 ]; then
    echo "### ⚠️ TODO Stale-Claim Uyarisi"
    echo "Son 7 gunde $hardening_commits 'hardening' / 'fix(security)' commit'i var."
    echo "TODO.md'de hala $open_high_count 'HIGH' open isareti — muhtemelen STALE."
    echo "Action almadan once .claude/rules/todo-verification.md Adim 2 (paralel Read/Grep dogrulama) yap."
    echo ""
fi

# Kod sagligi sinyalleri (anlik kod analizi — hook 1-2 sn sumler).
# Amac: oturum basinda "kod ne durumda" sorusunu canli koddan cevaplamak,
# stale TODO/journal'a guvenmemek (feedback_todo_stale_claim.md).
echo "### Kod sagligi sinyalleri"

# C# hard-limit ihlali (csharp-conventions.md: >500 satir = kirmizi cizgi)
csharp_over=$(find Mosaik Mosaik.Core Mosaik.Modules.Circular -name '*.cs' -type f -exec wc -l {} + 2>/dev/null | awk '$1 > 500 && $2 != "total"' | sort -rn)
csharp_count=$(printf '%s\n' "$csharp_over" | grep -c '^[[:space:]]*[0-9]' || true)
if [ "$csharp_count" -gt 0 ]; then
    echo "- C# 500+ satir hard-limit ihlali: $csharp_count dosya"
    printf '%s\n' "$csharp_over" | head -3 | sed 's/^/  /'
else
    echo "- C# hard-limit (500): temiz"
fi

# JS hard-limit ihlali (js-conventions.md: >350 satir = kirmizi cizgi)
js_over=$(find Mosaik/wwwroot/assets/js -name '*.js' -type f -exec wc -l {} + 2>/dev/null | awk '$1 > 350 && $2 != "total"' | sort -rn)
js_count=$(printf '%s\n' "$js_over" | grep -c '^[[:space:]]*[0-9]' || true)
if [ "$js_count" -gt 0 ]; then
    echo "- JS 350+ satir hard-limit ihlali: $js_count dosya"
    printf '%s\n' "$js_over" | head -3 | sed 's/^/  /'
else
    echo "- JS hard-limit (350): temiz"
fi

# Inline style envanter (inline-style-guard.md: sifir tolerans yeni view'da)
inline_count=$(grep -ro 'style="' Mosaik/Views Mosaik.Modules.Circular/Areas 2>/dev/null | wc -l)
if [ "$inline_count" -gt 50 ]; then
    echo "- Inline style attribute (.cshtml): $inline_count olusum (yuksek — Plan 25.1 borc)"
elif [ "$inline_count" -gt 0 ]; then
    echo "- Inline style attribute (.cshtml): $inline_count olusum"
else
    echo "- Inline style: temiz"
fi

# Silent failure (HIGH-a benzeri — _ = ex; yorum atimi, log eksik)
silent_ex=$(grep -rE '_ = ex;' Mosaik/ Mosaik.Core/ Mosaik.Modules.Circular/ 2>/dev/null | wc -l)
if [ "$silent_ex" -gt 0 ]; then
    echo "- Silent catch (\`_ = ex;\`): $silent_ex olusum — log eksik (HIGH-a paterni)"
    grep -rnE '_ = ex;' Mosaik/ Mosaik.Core/ Mosaik.Modules.Circular/ 2>/dev/null | head -3 | sed 's/^/  /'
else
    echo "- Silent catch (\`_ = ex;\`): 0 (temiz)"
fi

# ARCHITECTURE_MAP refresh disiplini (scripts/refresh-arch-map.sh)
if [ -f docs/ARCHITECTURE_MAP.md ]; then
    last=$(stat -c '%Y' docs/ARCHITECTURE_MAP.md 2>/dev/null || echo 0)
    now=$(date +%s)
    days=$(( (now - last) / 86400 ))
    if [ "$days" -gt 3 ]; then
        echo "- ARCHITECTURE_MAP son refresh: $days gun once (3+ gun — scripts/refresh-arch-map.sh calistir)"
    else
        echo "- ARCHITECTURE_MAP son refresh: $days gun once"
    fi
fi

# Plan envanter (plan-first.md disiplini)
active_plans=$(find plans -maxdepth 1 -name '[0-9]*.md' -type f 2>/dev/null | wc -l)
archived_plans=$(find plans/archive -name '*.md' -type f 2>/dev/null | wc -l)
echo "- Plan aktif: $active_plans · arsivlenmis: $archived_plans"

# Plan stale-check (14+ gun dokunulmamis aktif plan). "Plan olum tarihi"
# kurali (analiz onerisi 2026-05-14): 14 gun dokunulmamis plan ya yeniden
# isitilir ya arsive tasinir. Sogutulan plan = sogutulmus is.
stale_plans=$(find plans -maxdepth 1 -name '[0-9]*.md' -type f -mtime +14 2>/dev/null)
if [ -n "$stale_plans" ]; then
    stale_count=$(echo "$stale_plans" | wc -l | tr -d ' ')
    echo "- Plan 14+ gun stale: $stale_count dosya — yeniden isit veya arsive tasi"
    printf '%s\n' "$stale_plans" | head -3 | sed 's/^/  /'
fi

# TODO MEDIUM open
medium_count=$(grep -cE '^- \[ \] \*\*MEDIUM' TODO.md 2>/dev/null | head -1 || true)
[ -z "$medium_count" ] && medium_count=0
echo "- TODO MEDIUM open: $medium_count"

echo ""

echo "### En son journal girdisi"
last_journal=$(ls -t docs/journal/*.md 2>/dev/null | head -1)
if [ -n "$last_journal" ]; then
    journal_mtime=$(stat -c '%Y' "$last_journal" 2>/dev/null || echo 0)
    now=$(date +%s)
    journal_age=$(( (now - journal_mtime) / 86400 ))
    echo "Dosya: $last_journal ($journal_age gun once)"
    if [ "$journal_age" -ge 1 ]; then
        echo ""
        echo "UYARI: Journal $journal_age gun eski. Icerigi STALE olabilir —"
        echo "ozellikle 'ACIL HIGH' / 'Guvenlik Bulgulari' bolumlerindeki iddialar"
        echo "canli koddan dogrulanmadan FIX olarak alinmamali."
        echo "Detay: .claude/rules/todo-verification.md"
    fi
    echo ""
    tail -40 "$last_journal"
fi
echo ""

echo "### Kritik dosyalar / kurallar"
echo "- Baglam yonetimi: docs/CONTEXT_MANAGEMENT.md"
echo "- Mimari: .claude/rules/architecture.md"
echo "- Guvenlik: .claude/rules/security-principles.md"
echo "- Commit: .claude/rules/commit-discipline.md"
echo "- Turkce UI: .claude/rules/turkish-ui.md"
echo "- Bilinen sorunlar: .claude/rules/known-issues.md"

exit 0
