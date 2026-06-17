#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

if ! command -v makensis >/dev/null 2>&1; then
  echo "Установите NSIS: brew install makensis"
  exit 1
fi

python3 "$ROOT/tools/generate_app_icon.py" \
  "$ROOT/src/BanWordsFilter/Assets/app-icon.png" \
  "$ROOT/src/BanWordsFilter/Assets/app-icon.ico"
bash "$ROOT/tools/build_windows_app.sh"
bash "$ROOT/tools/prepare_windows_installer.sh"

echo "==> Сборка Ban Words Filter Setup.exe ..."
makensis "$ROOT/tools/windows/installer.nsi"

echo ""
echo "Готово: $ROOT/dist/Ban Words Filter Windows/Ban Words Filter Setup.exe"
