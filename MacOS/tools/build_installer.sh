#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DIST_DIR="$ROOT/dist/Ban Words Filter"
APP_NAME="Ban Words Filter"
APP="$DIST_DIR/$APP_NAME.app"
INSTALLER="$ROOT/$APP_NAME Installer.dmg"

if [ ! -d "$APP" ]; then
  echo "Сначала соберите: bash tools/build_mac_app.sh"
  exit 1
fi

STAGING="$(mktemp -d)"
trap 'rm -rf "$STAGING"' EXIT

cp -R "$APP" "$STAGING/"
ln -s /Applications "$STAGING/Applications"

rm -f "$INSTALLER"
hdiutil create \
  -volname "$APP_NAME" \
  -srcfolder "$STAGING" \
  -ov \
  -format UDZO \
  "$INSTALLER" >/dev/null

echo "Installer: $INSTALLER"
