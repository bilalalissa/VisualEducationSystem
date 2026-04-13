Drop the native macOS recognition plugin here with the library name:

VESVisionBridge

Native scaffold source now lives at:

submitted/native/macos/VESVisionBridge/

Build it with:

./build-macos-plugin.sh

Expected exported functions:
- VESVision_IsBackendAvailable
- VESVision_CopyStatusMessage
- VESVision_CopyLatestFrameJson
- VESVision_FreeCopiedString

Expected JSON payload fields:
- hasUser
- userId
- userConfidence
- userViewportX
- userViewportY
- userViewportWidth
- userViewportHeight
- leftTracked
- leftConfidence
- leftViewportX
- leftViewportY
- leftGesture
- rightTracked
- rightConfidence
- rightViewportX
- rightViewportY
- rightGesture
- timestamp
