#!/usr/bin/env bash
# PreCompact hook — /compact oncesi aktif TODO basliklar + son commitler journal'a eklenir.
# Baglamdan once ne uzerinde calisildiginin snapshot'i otomatik kaydedilir.
# Tetik: settings.json PreCompact eventi.

set -e
cd "$(git rev-parse --show-toplevel 2>/dev/null || echo "$PWD")"

today=$(date +%Y-%m-%d)
journal="docs/journal/$today.md"
mkdir -p docs/journal

if [ ! -f "$journal" ]; then
  echo "# Oturum Gunlugu --- $today" > "$journal"
  echo "" >> "$journal"
fi

ts=$(date +%H:%M)

{
  echo ""
  echo "## Compact Snapshot --- $ts"
  echo ""
  echo "### Son 5 Commit"
  echo ""
  git log --oneline -5 2>/dev/null | sed 's/^/- /' || echo "- (git log basarisiz)"
  echo ""
  echo "### Uncommitted Dosya Sayisi"
  echo ""
  count=$(git status --porcelain 2>/dev/null | wc -l | tr -d ' ')
  echo "- $count uncommitted dosya"
  echo ""
  echo "### Aktif TODO (ilk 15 acik madde)"
  echo ""
  grep '^\- \[ \]' TODO.md 2>/dev/null | head -15 | sed 's/^/- /' || echo "- (TODO.md okunamadi)"
  echo ""
} >> "$journal"

exit 0
