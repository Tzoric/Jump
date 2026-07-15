# Jump development guide

## Current game flow

The first scene in Build Settings is `Assets/Scenes/DungeonOverview.unity`. The
player selects Level 1 from that overview, climbs the Mines level, and completes it
by entering the grounded exit-door trigger. The door loads the dungeon overview
again.

Required crystals are no longer the Level 1 completion condition. Crystals can be
introduced as collectibles beginning in Level 2 without blocking the Level 1 exit.

## Project conventions

- Put gameplay scripts in `Assets/Scripts` and editor-only tools in `Assets/Editor`.
- Put playable scenes in `Assets/Scenes` and include them in Build Settings in order.
- Build reusable objects as prefabs instead of duplicating configured scene objects.
- Keep tunable values serialized and grouped in the Inspector.
- Commit `.meta` files with their matching Unity assets.
- Do not commit generated folders such as `Library`, `Temp`, `Logs`, or `UserSettings`.
- This project uses Git; the Plastic SCM editor package is intentionally not installed.

## Level 1 — Bronze Shaft

The playable scene is `Assets/Scenes/Level1_TheMines.unity`.

- The route contains 11 wide, stationary landings in an alternating vertical zig-zag.
- `VerticalCameraFollow` keeps the player centered while respecting level bounds.
- `LevelExitDoor` requires the player to be grounded before loading `DungeonOverview`.
- `PlayerHealth` and `PlayerWeight` remain on the hero for future levels.
- A pit below the starting floor respawns the player.
- Level 1 intentionally has no required gems, moving platforms, falling spikes, or
  breakable platforms. It teaches movement, jumping, and completion first.
- The walls use bronze and copper deposits because Level 1 belongs to the first Mines
  material tier.

The scene is generated reproducibly with **Jump > Level Tools > Build Level 1 - The
Mines**. The builder also updates the overview scene, reusable Hero prefab, imported
art settings, and Build Settings.

## Automated playtest

The virtual controller drives horizontal movement and jump input through
`HeroMovement`. Level 1 contains ordered `AutomatedPlaytestWaypoint` objects, so the
controller follows the authored route, confirms grounded landings, and then walks
through the exit door. The pass condition is loading the destination scene.

The controller also supports the older required-crystal rule for future or legacy
scenes that do not contain a `LevelExitDoor`.

From Unity, use **Jump > Playtest > Run Virtual Controller**. For unattended runs,
launch Unity with:

```text
-batchmode -projectPath <project> -executeMethod AutomatedPlaytestCommand.Run
-automatedPlaytest -playtestReport <report-file>
```

The unattended command requires all other Unity instances using this project to be
closed because Unity locks an open project. A timeout means the controller failed to
find or execute the route; it does not by itself prove that a level is impossible.

## Reusable later-level mechanics

- `DamageZone` provides consistent stationary-trap and pit damage.
- `FallingSpike` warns, falls when the player passes below, and resets.
- `MovingPlatform` supports horizontal or vertical travel with configurable pauses.
- `WeightedBreakablePlatform` spends durability according to riders' apparent weight.
- `PlayerWeight` is the inventory and power-up integration point.

Apparent weight is calculated as:

```text
(base weight + carried inventory weight) × weight multiplier × gravity multiplier
```

Future inventory code should call `SetCarriedWeight` or `AddCarriedWeight`. A
lightweight power-up should temporarily lower `weightMultiplier`; a low-gravity
power-up should lower `gravityMultiplier`. Restore modifiers when the effect expires.

## Mines material tiers

Wall deposits and related trim progress through five equal portions of the final
Mines level count:

1. Bronze and copper
2. Silver
3. Gold
4. Ruby and sapphire
5. Diamond

For a 100-level Mines dungeon, these correspond to Levels 1–20, 21–40, 41–60,
61–80, and 81–100. Scale the ranges proportionally if the final level count changes.
