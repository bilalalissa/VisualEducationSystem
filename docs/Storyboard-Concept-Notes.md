# Storyboard And Concept Notes

## Project Overview

Visual Education System is a Unity prototype for a memory-palace experience. The project is built around the idea that a learner can remember information more effectively when it is placed inside a meaningful spatial structure. The palace acts as a visual organizer, and each room acts as a memory container.

## Core Concept

The user begins in a central Entry Hall. From there, the user can identify different branches leading to different rooms. Each room has its own name, color, and visual markers. The palace itself also has a distinct identity through a central landmark and palace name.

The concept is not only “walk through rooms.” The intended experience is:

- spatial orientation
- room identity
- memory grouping
- simple authorship by the user
- persistence through save/load

## Storyboard Narrative

### 1. Entering the system

The user launches the project and sees a simple main menu. The system immediately communicates two primary actions: create a new palace or load an existing one.

### 2. Creating a palace

The user enters a palace name. The system rejects duplicate names so palace identity remains clear and no palace list confusion is introduced.

### 3. First recognition moment

When the user enters the palace, they land in the Entry Hall. This space is designed to communicate orientation quickly:

- palace name plaque
- central landmark
- visible room entrances
- distinct room signage and colors

This is the first important recognition moment in the experience.

### 4. Exploring room branches

The user walks through branching room entrances. The HUD updates with the current room name. This supports the idea that the palace is not just a 3D map, but a structure for remembering categorized information.

### 5. Editing the palace

The user presses `E` to open the room editor. In any room, the user can rename the room and change its color. In the Entry Hall, the user can also rename the palace and add one extra room branch.

This is the current core creation/edit loop:

- navigate
- identify
- edit
- save
- reload

### 6. Saving and loading

The user returns to the main menu, loads the palace from the save list, and confirms that names, colors, and room additions persist.

## What Is Finalized For This Milestone

- concept direction
- Entry Hall hub structure
- room naming
- room color editing
- named palace creation
- simple save/load JSON prototype

## What Still Needs Work

- more polished UI
- richer visual art and theming
- broader room-creation system
- more complete testing evidence
- final presentation polish

## Why The Current Prototype Matches The Concept

The current build already demonstrates the core concept in a practical way. The user can create a palace, move through a spatial memory structure, assign distinct room identities, and return to the same personalized state through loading. That is enough to show the concept clearly, even though the prototype is still visually early-stage.
