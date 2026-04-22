#!/usr/bin/env bash
# Pre-commit antipattern scan — Claude Code PreToolUse hook.
#
# Tetikleyici: settings.json PreToolUse, matcher = "Bash", command ~= "^git commit".
# Bu script stdin'den JSON okur: { "tool_input": { "command": "..." } }.
# Script `git commit` komutu tespit ederse staged .cs/.cshtml dosyalarinda
# kritik antipattern'leri arar ve exit 2 ile commit'i bloklar.
#
# Cikis kodlari:
#   0 -> commit devam etsin (check passed veya git commit komutu degil)
#   2 -> commit BLOKLA (antipattern bulundu, stdout'taki mesaj user'a gider)

set -e
cd "$(git rev-parse --show-toplevel 2>/dev/null || echo "$PWD")"

input=$(cat)

# Tool command'u cek. jq yoksa sed ile.
if command -v jq >/dev/null 2>&1; then
  cmd=$(echo "$input" | jq -r '.tool_input.command // ""')
else
  cmd=$(echo "$input" | sed -n 's/.*"command"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)
fi

# Sadece `git commit` komutlarini hedefle (`git commit -m ...`, `git commit --amend`, vb.)
if ! echo "$cmd" | grep -qE '(^|[[:space:]&;])git[[:space:]]+commit([[:space:]]|$)'; then
  exit 0
fi

# Staged dosyalari al
staged=$(git diff --cached --name-only --diff-filter=ACM 2>/dev/null || true)
[ -z "$staged" ] && exit 0

# Sadece C# + Razor + Config dosyalari
targets=$(echo "$staged" | grep -E '\.(cs|cshtml|json|ps1)$' || true)
[ -z "$targets" ] && exit 0

found_issues=()

for f in $targets; do
  [ -f "$f" ] || continue

  # C# antipattern'leri
  if [[ "$f" == *.cs ]]; then
    if grep -Hn 'DateTime\.Now\b' "$f" 2>/dev/null | grep -v '^\s*//'; then
      found_issues+=("$f: DateTime.Now -> DateTime.UtcNow kullan")
    fi
    if grep -Hn 'async void\b' "$f" 2>/dev/null | grep -v 'event' | grep -v '^\s*//'; then
      found_issues+=("$f: async void (event handler harici yasak)")
    fi
    if grep -Hn 'new HttpClient()' "$f" 2>/dev/null | grep -v '^\s*//'; then
      found_issues+=("$f: new HttpClient() -> IHttpClientFactory")
    fi
    # ex.Message user'a gidiyor mu (TempData/ViewBag/Json)
    if grep -HnE '(TempData\[.*\]|ViewBag\.|Json\(\s*new\s*\{[^}]*message).*ex\.Message' "$f" 2>/dev/null; then
      found_issues+=("$f: ex.Message user'a sizintili (logger'a yaz, user'a generic mesaj)")
    fi
  fi

  # Hardcoded sifre (tum dosya tipleri)
  if grep -HnE 'Password=[A-Za-z0-9!@#$%^&*+._-]{4,}' "$f" 2>/dev/null | grep -vE 'Password=["'\'']?(\s|$|;|")' | grep -v 'Password=\$'; then
    found_issues+=("$f: hardcoded sifre tespit (env var / User Secrets kullan)")
  fi
done

if [ ${#found_issues[@]} -gt 0 ]; then
  echo "=== PRE-COMMIT ANTIPATTERN SCAN: BLOKLANDI ===" >&2
  for issue in "${found_issues[@]}"; do
    echo "  X $issue" >&2
  done
  echo "" >&2
  echo "Commit iptal edildi. Once issue'lari duzelt, sonra tekrar commit'le." >&2
  echo "Override gerekiyorsa: git commit --no-verify (kural ihlali, journal'a yaz)." >&2
  exit 2
fi

exit 0
