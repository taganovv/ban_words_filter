#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_DIR="$ROOT/src/BanWordsFilter"
PAYLOAD="$ROOT/dist/installer-payload"
APP_DIST="$ROOT/dist/Ban Words Filter Windows/Ban Words Filter"

if [ ! -f "$APP_DIST/BanWordsFilter.exe" ]; then
  echo "Сначала соберите: bash tools/build_windows_app.sh"
  exit 1
fi

echo "==> Подготовка payload для установщика..."
rm -rf "$PAYLOAD"
mkdir -p "$PAYLOAD"
rsync -a "$APP_DIST/" "$PAYLOAD/"

ICON_SRC="$APP_DIR/Assets/app-icon.png"
ICON_DST="$APP_DIR/Assets/app-icon.ico"
python3 "$ROOT/tools/generate_app_icon.py" "$ICON_SRC" "$ICON_DST"
cp "$ICON_DST" "$PAYLOAD/app-icon.ico"

echo "Payload: $PAYLOAD"
