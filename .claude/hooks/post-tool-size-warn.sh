#!/usr/bin/env bash
# PostToolUse hook — tool ciktisi >50 satir ise journal'a uyari ekle.
# Buyuk MCP/Bash output baglamı hizla doldurur — farkindalik icin log.
# Tetik: settings.json PostToolUse, tum tool'lar (matcher yok).

cd "$(git rev-parse --show-toplevel 2>/dev/null || echo "$PWD")"

input=$(cat)

# JSON parse: jq > node fallback
if command -v jq >/dev/null 2>&1; then
  tool=$(echo "$input" | jq -r '.tool_name // ""')
  line_count=$(echo "$input" | jq -r '.tool_response.output // ""' | wc -l | tr -d ' ')
elif command -v node >/dev/null 2>&1; then
  tool=$(echo "$input" | node -e "
let d=''; process.stdin.on('data',c=>d+=c).on('end',()=>{
  try { const o=JSON.parse(d); process.stdout.write(o.tool_name||''); }
  catch { process.stdout.write(''); }
})" 2>/dev/null || echo "")
  line_count=$(echo "$input" | node -e "
let d=''; process.stdin.on('data',c=>d+=c).on('end',()=>{
  try {
    const o=JSON.parse(d);
    const out=(o.tool_response||{}).output||'';
    process.stdout.write(String((out.match(/\n/g)||[]).length+1));
  } catch { process.stdout.write('0'); }
})" 2>/dev/null || echo "0")
else
  exit 0
fi

[ -z "$line_count" ] && line_count=0
[ "$line_count" -le 50 ] && exit 0

today=$(date +%Y-%m-%d)
journal="docs/journal/$today.md"
mkdir -p docs/journal
ts=$(date +%H:%M)

if [ ! -f "$journal" ]; then
  printf "# Oturum Gunlugu --- %s\n\n" "$today" > "$journal"
fi

if ! grep -q '^## Buyuk Tool Ciktilari' "$journal" 2>/dev/null; then
  printf "\n## Buyuk Tool Ciktilari\n\n" >> "$journal"
fi

printf -- "- %s: \`%s\` → %s satir (>50 esigi)\n" "$ts" "$tool" "$line_count" >> "$journal"

exit 0
