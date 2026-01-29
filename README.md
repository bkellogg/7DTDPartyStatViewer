# PartyStatViewer

A 7 Days to Die mod that displays party members' skill book and magazine progression in item tooltips.

## Features

- Shows party members' progress when selecting skill books or crafting magazines
- Supports both Perk Books (19 series × 7 volumes) and Crafting Skill Magazines (23 types)
- Server-authoritative: data comes from server, can't be spoofed
- Real-time updates when party members read books
- Party-only: only shows members of your party, not all players

## Installation

1. Build the project (or download release)
2. Copy `PartyStatViewer.dll` and `ModInfo.xml` to `7 Days to Die/Mods/PartyStatViewer/`

## Build

Requires:
- .NET Framework 4.8
- Game DLLs from `7 Days to Die/7DaysToDie_Data/Managed/`

```powershell
msbuild PartyStatViewer.csproj /p:Configuration=Debug
```

Build auto-deploys to the game's Mods folder.

## Usage

1. Join a multiplayer game and form a party
2. Pick up any skill book or crafting magazine
3. Click on the item in your inventory
4. The item description now shows all party members' progress for that skill

## Display Format

**Perk Books:**
```
--- Pistol Pete Progress ---
PlayerA: 5/7 volumes
PlayerB: 7/7 ✓ COMPLETE
You: 2/7 volumes
```

**Crafting Magazines:**
```
--- Handgun Crafting Skill ---
PlayerA: 67/100
PlayerB: 100/100 ✓ MAX
You: 23/100
```
