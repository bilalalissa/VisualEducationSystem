#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
BUNDLE_DIR="$BUILD_DIR/VESVisionBridge.bundle"
CONTENTS_DIR="$BUNDLE_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
OUTPUT_BINARY="$MACOS_DIR/VESVisionBridge"
UNITY_PLUGIN_DIR="$REPO_ROOT/VisualEducationSystem/Assets/Plugins/macOS"
UNITY_BUNDLE_DIR="$UNITY_PLUGIN_DIR/VESVisionBridge.bundle"

mkdir -p "$MACOS_DIR" "$UNITY_PLUGIN_DIR"

cat > "$CONTENTS_DIR/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleExecutable</key>
  <string>VESVisionBridge</string>
  <key>CFBundleIdentifier</key>
  <string>com.visualeducationsystem.vesvisionbridge</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>VESVisionBridge</string>
  <key>CFBundlePackageType</key>
  <string>BNDL</string>
  <key>CFBundleShortVersionString</key>
  <string>0.1.0</string>
  <key>CFBundleVersion</key>
  <string>1</string>
</dict>
</plist>
PLIST

xcrun clang++ \
  -std=c++17 \
  -fobjc-arc \
  -bundle \
  -O2 \
  -framework Foundation \
  -framework AVFoundation \
  -framework CoreGraphics \
  -framework CoreMedia \
  -framework CoreVideo \
  -framework Vision \
  "$SCRIPT_DIR/src/VESVisionBridge.mm" \
  -o "$OUTPUT_BINARY"

rm -rf "$UNITY_BUNDLE_DIR"
cp -R "$BUNDLE_DIR" "$UNITY_BUNDLE_DIR"

echo "Built $UNITY_BUNDLE_DIR"
