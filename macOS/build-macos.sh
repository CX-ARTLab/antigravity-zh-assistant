#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARCH="$(uname -m)"
if [[ "$ARCH" != "arm64" ]]; then
  echo "This macOS build supports Apple Silicon (arm64) only; detected: $ARCH" >&2
  exit 1
fi
ARCH_LABEL="apple-silicon"

RESOURCES="$ROOT/macOS/Resources"
mkdir -p "$RESOURCES"
cp "$ROOT/src/translator.js" "$RESOURCES/translator.js"
cp "$ROOT/translation/translation-pack.json" "$RESOURCES/translation-pack.json"
cp "$ROOT/translation/manifest.json" "$RESOURCES/translation-manifest.json"
cp "$ROOT/src/Assets/assistant-icon.png" "$RESOURCES/assistant-icon.png"

cd "$ROOT/macOS"
BUILD_ROOT="$ROOT/macOS/.build"
mkdir -p "$BUILD_ROOT/cache" "$BUILD_ROOT/config" "$BUILD_ROOT/security" "$BUILD_ROOT/module-cache"
export CLANG_MODULE_CACHE_PATH="$BUILD_ROOT/module-cache"
export SWIFTPM_MODULECACHE_OVERRIDE="$BUILD_ROOT/module-cache"

# Command Line Tools upgrades can briefly leave the newest SDK ahead of the
# installed Swift compiler. Prefer the oldest versioned CLT SDK in that case;
# the app only needs macOS 12 APIs. A complete Xcode installation keeps its
# default SDK because its compiler and SDK are shipped together.
DEVELOPER_DIR="$(xcode-select -p 2>/dev/null || true)"
if [[ "$DEVELOPER_DIR" == */CommandLineTools ]]; then
  SDK_CANDIDATES=("$DEVELOPER_DIR"/SDKs/MacOSX[0-9]*.sdk)
  if [[ -d "${SDK_CANDIDATES[0]}" ]]; then
    SDKROOT="$(printf '%s\n' "${SDK_CANDIDATES[@]}" | sort -V | head -n 1)"
    export SDKROOT
  fi
fi

SWIFT_BUILD_ARGS=(
  -c release
  --disable-sandbox
  --scratch-path "$BUILD_ROOT"
  --cache-path "$BUILD_ROOT/cache"
  --config-path "$BUILD_ROOT/config"
  --security-path "$BUILD_ROOT/security"
)
swift build "${SWIFT_BUILD_ARGS[@]}"
BIN_PATH="$(swift build --show-bin-path "${SWIFT_BUILD_ARGS[@]}")/AntigravityZhAssistantMac"
APP="$ROOT/dist/AntigravityZhAssistant-macOS-${ARCH_LABEL}.app"
ZIP="$ROOT/dist/AntigravityZhAssistant-macOS-${ARCH_LABEL}.zip"

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$BIN_PATH" "$APP/Contents/MacOS/AntigravityZhAssistantMac"
cp "$ROOT/macOS/Info.plist" "$APP/Contents/Info.plist"
cp "$RESOURCES/translator.js" "$APP/Contents/Resources/translator.js"
cp "$RESOURCES/translation-pack.json" "$APP/Contents/Resources/translation-pack.json"
cp "$RESOURCES/translation-manifest.json" "$APP/Contents/Resources/translation-manifest.json"
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
if ! iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/AssistantIcon.icns" 2>/dev/null; then
  echo "iconutil rejected the generated iconset; using the compatible ICNS writer."
  # Some Command Line Tools releases reject even iconsets produced by their own
  # iconutil. Build the documented ICNS chunk container from the PNGs instead.
  /usr/bin/python3 - "$ICONSET" "$APP/Contents/Resources/AssistantIcon.icns" <<'PY'
from pathlib import Path
import struct
import sys

iconset = Path(sys.argv[1])
output = Path(sys.argv[2])
entries = [
    (b"ic10", "icon_512x512@2x.png"),
    (b"ic09", "icon_512x512.png"),
    (b"ic08", "icon_256x256.png"),
    (b"ic07", "icon_128x128.png"),
    (b"icp6", "icon_32x32@2x.png"),
    (b"icp5", "icon_32x32.png"),
    (b"icp4", "icon_16x16.png"),
]
chunks = []
for chunk_type, filename in entries:
    data = (iconset / filename).read_bytes()
    chunks.append(chunk_type + struct.pack(">I", len(data) + 8) + data)
payload = b"".join(chunks)
output.write_bytes(b"icns" + struct.pack(">I", len(payload) + 8) + payload)
PY
fi
rm -rf "$ICONSET"

# Bind the hand-assembled bundle metadata and resources to the executable so
# LaunchServices can open the app. This is an ad-hoc local signature, not an
# Apple Developer distribution signature or notarization.
codesign --force --deep --sign - "$APP"

rm -f "$ZIP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"
echo "Created $ZIP"
