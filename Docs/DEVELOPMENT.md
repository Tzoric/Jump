# Jump development guide

## Current completion rule

A level is complete when the player collects every object tagged `BlueCrystal` or
`BlackBigCrystal`. This explicit rule lets the automated virtual controller produce
a pass/fail result. It can be replaced by a goal object when level exits are added.

## Project conventions

- Put gameplay scripts in `Assets/Scripts` and editor-only tools in `Assets/Editor`.
- Put playable scenes in `Assets/Scenes` and include them in Build Settings in order.
- Build reusable objects as prefabs instead of duplicating configured scene objects.
- Keep tunable values serialized and grouped in the Inspector.
- Commit `.meta` files with their matching Unity assets.
- Do not commit generated folders such as `Library`, `Temp`, `Logs`, or `UserSettings`.
- This project uses Git; the Plastic SCM editor package is intentionally not installed.

## Automated playtest

The virtual controller is intentionally mechanical. It drives horizontal movement
and jump input through `HeroMovement`, targets remaining crystals, and reports a
pass only if it collects them all before the timeout without falling out of bounds.
Its current heuristic clears crystals from the lowest elevation upward and includes
basic stuck recovery. A timeout means the planner failed; it does not by itself mean
the level is impossible. Reliable certification of complex levels will use authored
playtest waypoints or a generated platform-reachability graph.

From Unity, use **Jump > Playtest > Run Virtual Controller**. For unattended runs,
launch Unity with:

```text
-batchmode -projectPath <project> -executeMethod AutomatedPlaytestCommand.Run
-automatedPlaytest -playtestReport <report-file>
```

The unattended command requires all other Unity instances using this project to be
closed because Unity locks an open project.

## Level 1 - The Mines mechanics

The playable scene is `Assets/Scenes/Level1_TheMines.unity`.

- `PlayerHealth` owns damage, temporary invulnerability, healing, and respawning.
- `DamageZone` lets stationary traps and kill volumes damage the player consistently.
- `FallingSpike` warns, drops when the player passes below, and resets for another attempt.
- `MovingPlatform` supports horizontal or vertical travel with configurable speed and pauses.
- `WeightedBreakablePlatform` spends durability according to all riders' apparent weight.
- `PlayerWeight` is the future inventory/power-up integration point.

Apparent weight is calculated as:

```text
(base weight + carried inventory weight) × weight multiplier × gravity multiplier
```

Future inventory code should call `SetCarriedWeight` or `AddCarriedWeight`. A lightweight
power-up should temporarily lower `weightMultiplier`; a low-gravity power-up should lower
`gravityMultiplier`. Always restore modifiers when the effect expires. This keeps inventory
data independent from platform behavior.
