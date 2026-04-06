# Engineering Roadmap

## Purpose

This roadmap converts the long-term product goals for Visual Education System into a staged engineering plan. It separates what already exists in the current prototype from the larger features still needed for the intended memory-palace product.

Current implemented foundation:

- named palace creation, loading, and deletion
- Entry Hall hub with connected branch rooms
- room naming and recoloring
- Entry Hall branch expansion
- nested sub-rooms with parent/child navigation
- room-level note and image clues with persistence and preview
- clue movement, wall placement, and scaling
- JSON save/load for palace state

Target product additions:

- richer memory clues such as files and videos
- rehearsal or lecturing spaces for practicing memorization
- VR headset support

## Stage 1: Palace Structure Expansion

### Goal

Replace the current mostly flat room structure with a scalable hierarchy that supports sub-rooms inside rooms.

### Status

Substantially complete as an MVP.

Completed in the current build:

- persistent sub-rooms
- parent/child navigation
- save/load support for room hierarchy
- connector and layout fixes
- HUD and map improvements
- HUD settings and auto-hide behavior

Still remaining for later polish:

- large-scale map and label scaling polish
- stronger generated-room theming and polish

### Main Work

- redesign the room data model so rooms can own child rooms
- update runtime room spawning so a room can create and load nested rooms
- update save/load to persist room hierarchy, not only a flat room list
- extend the room editor so users can add, rename, recolor, and navigate sub-rooms
- define limits for first implementation, such as depth, room count, and placement rules

### Engineering Notes

- current save data stores room identity and color, but not hierarchy
- room placement needs a clear layout rule so sub-rooms do not overlap or become confusing
- start with one predictable layout pattern before adding freeform placement

### Acceptance Criteria

- a user can create at least one sub-room inside a room
- sub-rooms persist through save/load
- HUD and room tracking continue to identify the current room correctly
- the user can still navigate back to higher-level spaces without confusion

Current result: met at MVP level

## Stage 2: Memory Clue System

### Goal

Allow rooms to contain study materials and memory anchors, not only names and colors.

### Status

In progress with a working MVP slice.

Completed in the current build:

- clue data model and clue persistence
- room-level note clues
- room-level image clues
- clue preview for notes and images
- clue movement, wall anchoring, and scaling
- Unity Editor local image browsing for image clues

Still remaining:

- stronger clue-placement polish and direct dragging
- file and video clue types
- richer clue editing and presentation polish
- broader runtime file-browser support outside the Unity Editor

### Main Work

- define a memory-clue model that can store clue type, title, room association, and display settings
- support an initial clue set:
  - image clues
  - note/text clues
  - file reference clues
  - video clues if playback is practical in the current Unity scope
- create room-level placement/edit UI for adding and editing clues
- create in-room visual representations so clues are visible and memorable
- extend save/load to persist clue metadata

### Engineering Notes

- start with text and image clues first; videos and file handling may need stricter scope control
- decide early whether files are embedded, copied, or only referenced by path
- external file references are riskier for portability and submission packaging

### Acceptance Criteria

- a user can add at least text and image clues to a room
- clue data persists through save/load
- clues are readable in the room and support room identity rather than clutter it
- the UI makes it clear which clue belongs to which room

Current result: partially met, with text and image clue MVP implemented

## Stage 3: Practice And Rehearsal Flow

### Goal

Turn the palace into an active memorization tool rather than only a navigation/editor system.

### Main Work

- define a practice mode entered from selected rooms or from dedicated rehearsal rooms
- add a lecturing/rehearsal flow where the user reviews topics in sequence
- support clue-guided recall, such as showing prompts before full answers
- record lightweight progress signals, such as completed practice sessions or room-review order
- define whether practice is solo walkthrough, timed recall, or guided presentation mode

### Engineering Notes

- practice should reuse the existing room structure rather than creating a disconnected system
- first version should be simple: prompt-based walkthrough before adding scores or analytics
- avoid complex assessment logic until the room/clue systems are stable

### Acceptance Criteria

- a user can enter a practice flow tied to palace content
- the flow uses room structure and clues meaningfully
- the user can rehearse information in a repeatable sequence
- practice state can be exited safely without corrupting palace edits

## Stage 4: VR Readiness

### Goal

Prepare the project for a VR headset version that improves immersion and spatial recall.

### Main Work

- choose the first VR target platform and headset
- review scene scale, movement comfort, and interaction patterns for VR
- replace or extend first-person controls with VR locomotion and interaction
- redesign editing interactions for VR-friendly input
- test whether clue viewing and room navigation remain readable in headset

### Engineering Notes

- VR should come after the structure and clue systems are stable
- do not build VR-specific interactions on top of an unstable flat prototype
- begin with VR compatibility planning before full deployment work

### Acceptance Criteria

- the project runs on the chosen VR target with basic locomotion
- the user can navigate palace spaces in VR without major readability issues
- at least a minimal subset of clue interaction works in VR
- the VR branch does not block the desktop version

## Cross-Cutting Work

These tasks support every stage and should continue in parallel:

- improve UI from debug-style panels toward clearer production-ready layouts
- strengthen visual identity and room theming
- maintain reliable save/load migrations as new data types are added
- run structured playtests after each stage
- keep docs, assets, and submission materials aligned with actual implementation

## Recommended Order

1. Sub-room architecture
2. Memory clue system
3. Practice/rehearsal flow
4. VR adaptation

This order matters because later systems depend on earlier structure:

- practice is more useful after rooms can hold richer content
- VR should adapt a stable content model, not define it

## Immediate Next Implementation Targets

The next concrete engineering tasks should be:

1. polish clue placement and direct manipulation
2. add richer clue types such as file and video references
3. prototype practice and rehearsal flow tied to room and clue content
4. continue UI cleanup away from debug-style immediate-mode panels

## Risks

- save-data changes may break compatibility if migrations are not planned
- unrestricted room/sub-room growth may make navigation confusing
- rich media support may create storage and portability problems
- VR scope can expand quickly if target hardware and interaction limits are not defined early

## Success Measure

The project will be closer to its intended vision when a learner can:

- build a multi-level palace
- place meaningful memory clues inside rooms
- rehearse information through guided practice
- eventually experience the palace through VR without losing clarity or usability
