# Project Meeting #2 Submission Summary

## Project

Visual Education System

## Acting Like a Designer

The project has been developed through iteration rather than through one final design made all at once. The first step was defining the memory-palace concept: a user should be able to remember information through spatial structure, room identity, and visual cues. After that, the project moved into a playable greybox prototype in Unity so the design could be tested through actual interaction.

The main visible iterations in the current build are:

- a central Entry Hall that gives the player orientation
- branch rooms with distinct names, colors, signs, and icons
- an in-game editor for renaming and recoloring rooms
- palace renaming and one added branch-room option from the Entry Hall
- JSON save/load so the palace can persist between sessions

These iterations were used to answer practical design questions about clarity, recognition, authorship, and persistence. Team-specific process notes can be added later if they need to be stated more explicitly.

## Design Goals

The final design goals are:

- make each room feel like a distinct memory space
- make palace orientation clear through a strong hub-and-branch layout
- let users personalize the palace through editing actions, sub-rooms, and richer room structure
- let users place memory clues such as images, notes, videos, and files inside rooms
- support active rehearsal through practice or lecturing rooms
- preserve palace identity across sessions through save/load
- grow toward VR headset support for a more immersive memory-palace experience
- improve the experience with stronger UI and visual polish

The current prototype achieves the first production version of these goals, but it still needs more polish, broader room-management options, memory-clue tools, practice features, VR planning, and more formal testing evidence.

## Paper Prototyping Plan

Paper prototyping at this stage is used to check layout and readability before final polish. The most useful paper prototype for this project is a top-down layout of the Entry Hall and connected rooms, along with the basic user flow of create/load, explore, edit, save, and reload.

This low-fidelity approach helps test whether:

- the palace layout is easy to understand
- the rooms feel distinct enough from one another
- the core memory-palace idea is clear before final art is added

A simple layout sketch is included as a supporting artifact in the repo.

## Game Testing

Game testing will focus on how clearly the current prototype communicates the design. The main test tasks are:

- create a palace and enter the Entry Hall
- move between rooms and identify them
- rename and recolor a room
- rename the palace and add the extra branch room
- save, reload, and verify persistence

The main testing focus is orientation clarity, room identity, editing clarity, and save/load reliability. The separate testing-plan document contains the more detailed task list and observation prompts.

## Auto Evaluation Questions And Remaining Steps

Current evaluation questions include:

- Does the Entry Hall orient the player quickly?
- Are room names, colors, and signs distinct enough to support recognition?
- Is the room-editing flow easy to understand?
- Does save/load make the palace feel persistent and personal?
- Does the greybox build already communicate the memory-palace concept clearly?

Remaining steps are:

- improve UI and presentation polish
- strengthen room theming and visual identity
- expand room-management features beyond the current prototype scope, including sub-rooms
- add in-room memory clues such as pictures, notes, videos, and files
- build spaces or tools for practicing memorization
- investigate VR headset support
- run and document more formal playtests
- align the build and documentation for the final submission
