#!/usr/bin/env bash
# PostToolUse hook — Edit/Write tool view veya controller'a dokunduysa
# ARCHITECTURE_MAP.md'nin stale olduğunu Claude'a hatırlat.
#
# Tetik: settings.json PostToolUse matcher Edit|Write.
# Çıktı: stderr (Claude görür, kullanıcı görmez).

set +e

input=$(cat)

# JSON'dan file_path çıkar
file_path=""
if command -v jq >/dev/null 2>&1; then
  file_path=$(echo "$input" | jq -r '.tool_input.file_path // ""')
elif command -v node >/dev/null 2>&1; then
  file_path=$(echo "$input" | node -e "
let d=''; process.stdin.on('data',c=>d+=c).on('end',()=>{
  try { const o=JSON.parse(d); process.stdout.write(o.tool_input?.file_path||''); }
  catch { process.stdout.write(''); }
})" 2>/dev/null)
fi

# Hangi dosyalar map'i etkiler?
case "$file_path" in
    */Mosaik/Views/Admin/*.cshtml \
    | */Mosaik/Controllers/AdminController*.cs \
    | */Mosaik/Models/AppModule.cs \
    | */Mosaik/Database/*AppModules*.sql \
    | */Mosaik/Database/*Modules*.sql \
    | */Mosaik/Views/Shared/_AppLayout.cshtml)
        echo "⚠️ Bu dosya ARCHITECTURE_MAP.md'yi etkiliyor: $(basename "$file_path")" >&2
        echo "   Oturum sonunda \`bash scripts/refresh-arch-map.sh\` çalıştır." >&2
        ;;
esac

exit 0
