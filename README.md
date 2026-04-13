# Enhanced Memory Palace

Unity project for a first-person educational memory-palace prototype.

## Current Project Scope

- persistent palace creation and loading
- entry hall with connected rooms and sub-rooms
- in-room note clues
- in-room image clues
- first-pass ink clues
- room editing and wall placement
- palace map and mini-map
- simulated and webcam-backed hand-tracking foundation
- native macOS webcam recognition bridge scaffold

## Run

1. Open `submitted/VisualEducationSystem` in Unity.
2. Open `Assets/Scenes/Bootstrap.unity`.
3. Press Play.

## Core Controls

- `W A S D`: move
- `Mouse`: look
- `E`: open or close room editor
- `Esc`: close the active panel or return toward the menu flow
- `M`: toggle palace map

## Hand Tracking

- `F10`: toggle simulated / webcam provider
- `F7`: select user
- `F8`: start or resume tracking
- `F9`: pause tracking

Simulation:

- `I J K L`: move simulated right-hand pointer
- `P`: right-hand pinch
- `O`: right-hand point

Ink clues:

- start from the room editor
- right-hand pinch draws
- right-hand fist erases when erase mode is active

## Repository Note

This branch is kept focused on the playable project. Meeting, presentation, and classwork submission files are intentionally excluded from version control.
