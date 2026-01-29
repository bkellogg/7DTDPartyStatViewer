# PartyStatViewer

A 7 Days to Die mod that shows party members' skill book and magazine progression in item tooltips.

## Features

- **Perk Book Progress**: When hovering over a perk book (e.g., Sledge Saga, Ranger's Guide), see which volumes each party member has read
- **Crafting Magazine Progress**: When hovering over crafting magazines, see each party member's skill level
- **Duplicate Warning**: Shows a warning when viewing a perk book volume you've already read
- **Compact Display**: Shows progress as "PlayerName: 5/7 (need 1,4)" format
- **Multiplayer Support**: Works in both single player and multiplayer (data synced from server)

## Example Display

When hovering over a Sledge Saga book:

```
--- Sledge Saga Progress ---
Warning: You already have Vol 7!
OtherPlayer: 5/7 (need 2,6)
You: 3/7 (need 1,2,4,6)
```

## Installation

1. Build the mod or download the release
2. Copy the `PartyStatViewer` folder to your `7 Days to Die/Mods/` directory
3. Start the game

## Requirements

- 7 Days to Die (tested on latest version)
- Harmony (included with the game via 0_TFP_Harmony)
