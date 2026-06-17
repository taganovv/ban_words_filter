#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_DIR="$ROOT/src/BanWordsFilter"
DIST_DIR="$ROOT/dist/Ban Words Filter Windows"
APP_NAME="Ban Words Filter"
PACKAGE_DIR="$DIST_DIR/$APP_NAME"

export DOTNET_ROOT="${DOTNET_ROOT:-/opt/homebrew/opt/dotnet@8/libexec}"
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Установите .NET 8: brew install dotnet@8"
  exit 1
fi

python3 "$ROOT/tools/generate_app_icon.py" \
  "$APP_DIR/Assets/app-icon.png" \
  "$APP_DIR/Assets/app-icon.ico"

echo "==> Сборка приложения (Release, win-x64)..."
cd "$APP_DIR"
dotnet publish -c Release -r win-x64 --self-contained true

PUBLISH_DIR="$APP_DIR/bin/Release/net8.0/win-x64/publish"

echo "==> Упаковка $APP_NAME (Windows) ..."
rm -rf "$DIST_DIR"
mkdir -p "$PACKAGE_DIR"
cp -R "$PUBLISH_DIR/"* "$PACKAGE_DIR/"

INSTALLER="$DIST_DIR/$APP_NAME Installer.zip"
rm -f "$INSTALLER"
(
  cd "$DIST_DIR"
  zip -rq "$INSTALLER" "$APP_NAME"
)

echo ""
echo "Готово: $DIST_DIR"
echo "Запуск на Windows: $PACKAGE_DIR/BanWordsFilter.exe"
echo "Архив: $INSTALLER"
