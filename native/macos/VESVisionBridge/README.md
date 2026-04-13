# VESVisionBridge

Native macOS plugin scaffold for Unity webcam recognition.

## What exists now

This folder now contains a real plugin scaffold:

- `src/VESVisionBridgeExports.h`
- `src/VESVisionBridge.mm`
- `build-macos-plugin.sh`

The current native implementation is a first-pass bridge:

- starts a native camera session
- runs Vision hand-pose detection
- assigns left/right hands by viewport position
- emits heuristic gesture labels for:
  - `thumbsup`
  - `openpalm`
  - `fist`
  - `pinch`
  - `point`

It is still an early heuristic pass. It does not yet do strong person recognition or robust selected-user ownership on the native side.

## Unity contract

Unity expects a plugin named `VESVisionBridge` loadable from:

- `submitted/VisualEducationSystem/Assets/Plugins/macOS/VESVisionBridge.bundle`

Exported symbols:

- `VESVision_IsBackendAvailable`
- `VESVision_CopyStatusMessage`
- `VESVision_CopyLatestFrameJson`
- `VESVision_FreeCopiedString`

## Expected JSON payload

```json
{
  "hasUser": true,
  "userId": 1001,
  "userConfidence": 0.91,
  "userViewportX": 0.2,
  "userViewportY": 0.08,
  "userViewportWidth": 0.6,
  "userViewportHeight": 0.84,
  "leftTracked": true,
  "leftConfidence": 0.88,
  "leftViewportX": 0.31,
  "leftViewportY": 0.57,
  "leftGesture": "openpalm",
  "rightTracked": true,
  "rightConfidence": 0.9,
  "rightViewportX": 0.71,
  "rightViewportY": 0.56,
  "rightGesture": "thumbsup",
  "timestamp": 12345.67
}
```

## Current behavior

The native bridge now:

- builds a Unity-loadable `.bundle`
- starts a native macOS camera capture session
- reports live backend status strings
- returns hand/user frame JSON payloads to Unity

Current limitations:

- user recognition is inferred from hand presence rather than true person identity
- gesture recognition is heuristic and will need tuning
- selected-user locking is still primarily enforced on the Unity side
- no handwriting-specific native gesture layer yet

## Build

From this folder:

```bash
./build-macos-plugin.sh
```

The script:

1. builds `build/VESVisionBridge.bundle`
2. copies it into `submitted/VisualEducationSystem/Assets/Plugins/macOS/`

## Next native implementation steps

1. Improve user ownership from hand-union fallback to a stronger person/body model.
2. Stabilize left/right hand assignment across rapid motion and occlusion.
3. Tune gesture classification with temporal smoothing.
4. Add selected-user lock persistence on the native side.
5. Add richer debug/status outputs for calibration and troubleshooting.
