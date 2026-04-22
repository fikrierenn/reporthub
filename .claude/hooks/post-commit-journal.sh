#!/usr/bin/env bash
# Post-commit journal auto-update — Claude Code PostToolUse hook.
#
# Tetikleyici: settings.json PostToolUse, matcher = "Bash", command ~= "^git commit".
# Commit basarili oldugunda (exit 0) son commit'in ozetini docs/journal/YYYY-MM-DD.md'ye append eder.
#
# Stdin'den JSON okur: { "tool_input": { "command": "..." }, "tool_response": { "exit_code": N } }.
# Commit komutu degilse veya commit basarisizsa hic bir sey yapmaz.

set -e
cd "$(git rev-parse --show-toplevel 2>/dev/null || echo "$PWD")"

input=$(cat)

if command -v jq >/dev/null 2>&1; then
  cmd=$(echo "$input" | jq -r '.tool_input.command // ""')
  exit_code=$(echo "$input" | jq -r '.tool_response.exit_code // 1')
else
  cmd=$(echo "$input" | sed -n 's/.*"command"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)
  exit_code=$(echo "$input" | sed -n 's/.*"exit_code"[[:space:]]*:[[:space:]]*\([0-9]*\).*/\1/p' | head -1)
  [ -z "$exit_code" ] && exit_code=1
fi

# Sadece basarili `git commit` komutlari
if ! echo "$cmd" | grep -qE '(^|[[:space:]&;])git[[:space:]]+commit([[:space:]]|$)'; then
  exit 0
fi
[ "$exit_code" != "0" ] && exit 0

# Son commit bilgisi
hash=$(git rev-parse --short HEAD 2>/dev/null || exit 0)
subject=$(git log -1 --format='%s' 2>/dev/null || exit 0)
files=$(git log -1 --name-only --format='' 2>/dev/null | grep -v '^$' | head -10)
file_count=$(echo "$files" | grep -c '.' || echo 0)

today=$(date +%Y-%m-%d)
journal="docs/journal/$today.md"
mkdir -p docs/journal

# Journal yoksa header ile olustur
if [ ! -f "$journal" ]; then
  echo "# Oturum Gunlugu — $today" > "$journal"
  echo "" >> "$journal"
fi

# "## Commit'ler" bolumu yoksa ekle
if ! grep -q '^## Commit'"'"'ler' "$journal"; then
  echo "" >> "$journal"
  echo "## Commit'ler" >> "$journal"
  echo "" >> "$journal"
fi

# Commit ozetini ekle
{
  echo "### \`$hash\` — $subject"
  echo ""
  echo "Dosyalar ($file_count):"
  echo "$files" | sed 's/^/- /'
  echo ""
} >> "$journal"

exit 0
