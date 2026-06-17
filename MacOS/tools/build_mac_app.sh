#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_DIR="$ROOT/src/BanWordsFilter"
DIST_DIR="$ROOT/dist/Ban Words Filter"
APP_NAME="Ban Words Filter"
BUNDLE="$DIST_DIR/$APP_NAME.app"

export DOTNET_ROOT="${DOTNET_ROOT:-/opt/homebrew/opt/dotnet@8/libexec}"
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Установите .NET 8: brew install dotnet@8"
  exit 1
fi

echo "==> Сборка приложения (Release, osx-arm64)..."
cd "$APP_DIR"
dotnet publish -c Release -r osx-arm64 --self-contained true

PUBLISH_DIR="$APP_DIR/bin/Release/net8.0/osx-arm64/publish"

echo "==> Упаковка $APP_NAME.app ..."
rm -rf "$DIST_DIR"
mkdir -p "$BUNDLE/Contents/MacOS"
ICON_SRC="$APP_DIR/Assets/AppIcon.icns"
if [ -f "$ICON_SRC" ]; then
  mkdir -p "$BUNDLE/Contents/Resources"
  cp "$ICON_SRC" "$BUNDLE/Contents/Resources/AppIcon.icns"
fi

cp -R "$PUBLISH_DIR/"* "$BUNDLE/Contents/MacOS/"
chmod +x "$BUNDLE/Contents/MacOS/BanWordsFilter"

cat > "$BUNDLE/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>ru</string>
  <key>CFBundleDisplayName</key>
  <string>Ban Words Filter</string>
  <key>CFBundleExecutable</key>
  <string>BanWordsFilter</string>
  <key>CFBundleIdentifier</key>
  <string>local.banwordsfilter.app</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>Ban Words Filter</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>1.0.0</string>
  <key>CFBundleVersion</key>
  <string>1</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

echo ""
echo "Готово: $DIST_DIR"
echo "Пользователь видит: $APP_NAME.app"
echo ""
echo "Установщик:"
echo "  bash tools/build_installer.sh"
echo ""
echo "Запуск:"
echo "  open \"$BUNDLE\""
