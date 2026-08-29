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
cp "$ROOT/src/Assets/assistant-icon.png" "$RESOURCES/assistant-icon.png"

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
cp "$RESOURCES/assistant-icon.png" "$APP/Contents/Resources/assistant-icon.png"

ICONSET="$ROOT/dist/AssistantIcon.iconset"
rm -rf "$ICONSET"
mkdir -p "$ICONSET"
sips -z 16 16 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_16x16.png" >/dev/null
sips -z 32 32 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_16x16@2x.png" >/dev/null
sips -z 32 32 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_32x32.png" >/dev/null
sips -z 64 64 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_32x32@2x.png" >/dev/null
sips -z 128 128 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_128x128.png" >/dev/null
sips -z 256 256 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_128x128@2x.png" >/dev/null
sips -z 256 256 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_256x256.png" >/dev/null
sips -z 512 512 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_256x256@2x.png" >/dev/null
sips -z 512 512 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_512x512.png" >/dev/null
sips -z 1024 1024 "$RESOURCES/assistant-icon.png" --out "$ICONSET/icon_512x512@2x.png" >/dev/null
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/AssistantIcon.icns"
rm -rf "$ICONSET"

rm -f "$ZIP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"
echo "Created $ZIP"
