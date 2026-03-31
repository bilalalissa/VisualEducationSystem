# Game Testing Plan

## Purpose

This test plan supports Project Meeting #2 by documenting how the current prototype will be tested during production and post-production review.

## Current Build Under Test

The playable scope being tested is limited to the current Unity prototype:

- `Bootstrap -> MainMenu -> PrototypePalace` flow
- first-person movement
- room identification through names, colors, signs, and HUD updates
- room rename and recolor through the `E` editor
- palace rename and one extra branch room from the Entry Hall
- JSON save/load through the main menu

## Test Tasks

| Task | Expected Result | What To Watch |
| --- | --- | --- |
| Create a new palace | A unique palace is created and the player enters the palace scene | Is naming clear? Is the transition understandable? |
| Enter the Entry Hall | The player starts in a clear hub space | Is the layout readable immediately? |
| Explore branch rooms | Room HUD updates and entrances remain recognizable | Do players know where they are? |
| Rename a room | The new room name appears consistently | Is the edit action obvious? |
| Recolor a room | The room color changes and remains visible | Does color help differentiate spaces? |
| Rename the palace in Entry Hall | Palace identity updates without duplicate names | Does the system communicate palace ownership clearly? |
| Add the extra branch room | One new room branch becomes available from the Entry Hall | Is the new branch easy to notice and understand? |
| Save and reload | Palace name, room names, room colors, and extra branch state persist | Does persistence feel reliable? |

## Observation Table

| Area | Prompt | Notes During Testing |
| --- | --- | --- |
| Orientation | Did the Entry Hall help the player understand the palace structure quickly? | |
| Room Identity | Were rooms distinct enough to recognize without confusion? | |
| Controls | Did the player understand movement, `E`, and `Esc` without repeated explanation? | |
| Editing | Was rename/recolor flow easy to complete? | |
| Persistence | Did players trust that their changes were saved and restored correctly? | |
| Concept Fit | Did the prototype feel like a memory-palace tool rather than only a room-navigation demo? | |

## Testing Notes

- Use current implemented features only.
- Record confusion points, not just bugs.
- Treat repeated player hesitation as design feedback.
- Use the results to prioritize UI clarity, room differentiation, and persistence confidence.
