# Jump development guide

## Current game flow

`Assets/Scenes/DungeonOverview.unity` is first in Build Settings and contains a real camera, eliminating Unity's “No cameras rendering” message. Each visible mineshaft is represented by a level node. Levels 1 and 2 are playable; shafts 3–5 are visible and locked. The overview also contains the earned-currency shop.

Every exit plays a short walk-into-the-door transition before returning to the overview. Required crystals are not an exit condition.

## Project conventions

- Put gameplay scripts in `Assets/Scripts` and editor tools in `Assets/Editor`.
- Put playable scenes in `Assets/Scenes` and enable them in progression order.
- Build reusable objects as prefabs instead of duplicating configured scene objects.
- Commit `.meta` files with their matching Unity assets.
- Every ordinary door must have a solid platform directly under it. Only an explicit level-detail exception may create a floating or unsupported door.
- Mine platforms use the level's rock material with tier material holding and trimming the rocks; current platforms use dark mine rock and bronze binding.

## Player and economy rules

- Horizontal movement is 7.5 units/second, 75% of the original 10.
- Jump force is 11 (about 73% of the former value) and gravity scale is 5.4 (60% of the former value). This preserves reach while making ascent and falling read more naturally beside 75% horizontal speed. The held-jump window is 0.24 seconds.
- The miner is 125% of the former size and has a helmet and carried pickaxe.
- A level starts with five hearts plus permanent purchased heart upgrades.
- A new save starts with three lives. Reaching zero hearts consumes a life and respawns; dying with no lives returns to the overview.
- Green crystals save immediately. The shop sells an extra life for 10 crystals, a health potion for 5, and a permanent +1-heart upgrade for 25.
- Press `H` during a level to consume a potion and refill all hearts.

## Mines levels

Level 1, `Assets/Scenes/Level1_TheMines.unity`, is the Bronze Shaft tutorial. Its 11 wide stationary landings teach slower movement, camera tracking, and visible door entry without crystals or spikes.

Level 2, `Assets/Scenes/Level2_SlidingAscent.unity`, rises up and right through six connected 22-degree bronze ramps. Gravity makes a failed climb slide back toward the bottom. Four spike groups each deal one heart, and six green crystals teach collection and fund the shop. Both exits use supported rock-and-bronze foundations.

Generate scenes and assets with **Jump > Level Tools > Build Mines Levels**. Validate them with **Jump > Level Tools > Validate Mines Levels**.

## Automated playtest

The virtual controller drives movement through `HeroMovement` and follows ordered `AutomatedPlaytestWaypoint` objects before entering the door. Level 1 has 11 route waypoints; Level 2 has 6.

```text
-batchmode -projectPath <project> -executeMethod AutomatedPlaytestCommand.Run
-automatedPlaytest -playtestReport <report-file>
```

The unattended command requires other Unity instances using the project to be closed. A timeout means the controller failed to execute the authored route.

## Reusable mechanics

- `DamageZone` handles stationary hazards and pits.
- `FallingSpike` warns, falls when the player passes below, and resets.
- `MovingPlatform` supports horizontal or vertical travel with pauses.
- `WeightedBreakablePlatform` spends durability according to apparent weight.
- `PlayerWeight` is the inventory and power-up integration point.

Apparent weight is `(base + carried) × weight multiplier × gravity multiplier`.

## Mines material tiers

Wall deposits and platform binding progress through bronze/copper, silver, gold, ruby/sapphire, and diamond in five equal portions of the final Mines level count.
