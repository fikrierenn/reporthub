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

echo "### En son journal girdisi"
last_journal=$(ls -t docs/journal/*.md 2>/dev/null | head -1)
if [ -n "$last_journal" ]; then
    echo "Dosya: $last_journal"
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
