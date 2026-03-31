# Project Meeting #2 Submission Summary

## Project

Visual Education System

## Purpose

This writeup is the Project Meeting #2 package for the production and post-production checkpoint. It is grounded in the current Unity prototype and focuses on how the project has been iterated from concept into a playable greybox with editing and persistence.

## Acting Like a Designer

The project has been developed through an iterative design process rather than a single fixed build. The first pass established the core concept: a digital memory palace where the user recognizes topics through spatial layout, room identity, and palace landmarks. From there, the prototype moved into a greybox production phase that tested whether the concept could work as an actual interaction loop.

The main design iterations visible in the repo are:

- concept framing around spatial memory and room-based organization
- a central Entry Hall hub to improve orientation before exploration
- branch rooms with distinct names, colors, signs, and icons
- an in-game editor that supports room rename and recolor actions
- palace rename and branch-room addition from the Entry Hall
- JSON save/load so the design can be tested across sessions

This process shows iteration through implementation. Each step answered a design question:

- can the player understand the palace structure quickly
- do room labels and colors make spaces easier to distinguish
- does a simple edit loop support user authorship
- does persistence make the palace feel like an owned memory structure

Assumption to confirm before final upload: the repo shows the iteration through prototype features, but any team-specific paper sketches, meeting notes, or discarded variations should be added here if they were part of the real process.

## Design Goals

The final design goals are larger than the current prototype feature list. The intended final build should:

- support spatial memory by making each room feel like a distinct memory container
- make palace orientation clear through a strong Entry Hall and readable branch layout
- let users author their own palace through naming, color, room-management, and nested sub-room actions
- support memory clues placed inside rooms, including pictures, handwriting-style notes, videos, and files
- include spaces for practicing memorization, such as rehearsal or lecturing rooms
- preserve continuity between sessions through dependable save/load behavior
- grow toward VR headset support for stronger immersion and spatial recall
- present the experience with clearer UI, stronger visual identity, and better demo polish

The current prototype already reaches the first production version of these goals, but it does not yet represent the final polished design. The project still needs a stronger art pass, better interface presentation, expanded room management, sub-room creation, memory-clue tools, practice features, VR planning, and more formal testing evidence.

## Paper Prototyping Plan

The project has been using low-fidelity prototyping to keep the memory-palace concept understandable before full polish. The most useful paper-prototype structure for this stage is a top-down palace layout that shows:

- the Entry Hall as the orientation hub
- initial branch rooms as separate memory spaces
- room identity through names, color labels, and icon markers
- the user flow of enter, explore, edit, save, and reload

The attached paper-prototype artifact is designed to validate two questions early:

- can a new player understand the palace layout without detailed art
- are the room distinctions clear enough to support the memory-palace idea

This paper-level planning matches the current greybox build because the prototype still depends on layout clarity and room differentiation more than on final visuals.

Supporting artifact:

- [Paper Prototype Layout](./assets/Paper-Prototype-Layout.svg)

## Game Testing

Game testing at this stage should focus on whether the current prototype communicates the design clearly, not on final polish. The planned test flow is:

1. create a new palace
2. enter the Entry Hall
3. identify available branch rooms
4. move between rooms and observe HUD updates
5. rename and recolor a room
6. rename the palace in the Entry Hall
7. add the extra branch room from the Entry Hall
8. save, return to menu, reload, and verify persistence

The main testing goals are:

- orientation clarity in the Entry Hall
- readability of room identity
- clarity of controls and editing workflow
- reliability of save/load persistence

Supporting artifact:

- [Game Testing Plan](./Game-Testing-Plan.md)

## Auto Evaluation Questions And Remaining Steps

Current evaluation questions for the project are:

- Does the Entry Hall orient new players quickly enough?
- Are room names, colors, signs, and icons distinct enough to support recognition?
- Is pressing `E` and editing a room understandable without extra explanation?
- Does save/load feel dependable enough to support the idea of a persistent personal palace?
- Does the current greybox layout communicate the memory-palace concept even before a full art pass?

Remaining steps toward completion are:

- polish the UI so the prototype reads as a designed interface rather than a debug-style tool
- strengthen visual theming so rooms feel more memorable and less placeholder
- expand room-management logic beyond the single added branch room and support sub-rooms
- add memory-clue placement for images, notes, videos, and files inside rooms
- design and implement rehearsal or lecturing rooms for memorization practice
- investigate a VR-ready version of the interaction model and deployment path
- run and document more formal playtesting with observations
- align the playable build, documentation, and presentation materials for the final course submission
