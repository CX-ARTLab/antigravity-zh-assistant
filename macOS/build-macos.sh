#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARCH="$(uname -m)"
case "$ARCH" in
  arm64) ARCH_LABEL="apple-silicon" ;;
  x86_64) ARCH_LABEL="intel" ;;
  *) ARCH_LABEL="$ARCH" ;;
esac

RESOURCES="$ROOT/macOS/Resources"
mkdir -p "$RESOURCES"
cp "$ROOT/src/translator.js" "$RESOURCES/translator.js"
cp "$ROOT/translation/translation-pack.json" "$RESOURCES/translation-pack.json"

cd "$ROOT/macOS"
swift build -c release
BIN_PATH="$(swift build --show-bin-path -c release)/AntigravityZhAssistantMac"
APP="$ROOT/dist/AntigravityZhAssistant-macOS-${ARCH_LABEL}.app"
ZIP="$ROOT/dist/AntigravityZhAssistant-macOS-${ARCH_LABEL}.zip"

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$BIN_PATH" "$APP/Contents/MacOS/AntigravityZhAssistantMac"
cp "$ROOT/macOS/Info.plist" "$APP/Contents/Info.plist"
cp "$RESOURCES/translator.js" "$APP/Contents/Resources/translator.js"
cp "$RESOURCES/translation-pack.json" "$APP/Contents/Resources/translation-pack.json"

rm -f "$ZIP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"
echo "Created $ZIP"
