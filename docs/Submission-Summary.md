# Submission Summary

## Project

Visual Education System

## Submission Focus

This submission demonstrates the concept/story milestone state of the project. The prototype currently supports:

- finalized memory-palace direction
- central palace hub with branch rooms
- room naming
- room recoloring
- room creation/edit loop from the Entry Hall
- basic palace creation/load UI
- simple JSON save/load prototype

## Functional Evidence

### Palace creation and selection

The main menu includes:

- `New Palace`
- `Load Palace`
- delete saved palace option

Duplicate palace names are prevented.

### Room editing

Inside the palace, the user can:

- walk into rooms
- press `E` to open the editor
- rename the current room
- recolor the current room

In the Entry Hall, the user can also:

- rename the palace
- add one new branch room

Duplicate room names are prevented.

### Save/load prototype

The project writes palace data to JSON and can load it again through the main menu. Current persisted values include:

- palace name
- room names
- room colors
- whether the extra branch room exists

## Concept Fit

The prototype expresses the intended concept clearly:

- the Entry Hall acts as the orientation space
- branching rooms represent separable memory spaces
- naming, color, icons, and palace landmark support recognition
- save/load supports continuity between sessions

## Current Limitations

- visual art is still mostly greybox
- UI is functional, not final
- room expansion is currently limited
- editing system is still prototype-scale rather than full production logic

## Included Documentation

- [README](../README.md)
- [Storyboard Slides PDF](./Storyboard-Concept-Slides.pdf)
- [Presentation Notes](./Storyboard-Concept-Notes.md)
- [Gantt Plan](./assets/Gantt-Plan.png)
