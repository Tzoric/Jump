# Jump development guide

## Current game flow

`Assets/Scenes/DungeonOverview.unity` is first in Build Settings and must always contain an enabled rendering camera. The Bronze Mines overview has eleven interactive mineshafts, one for every playable tunnel. It must never show Unity's `No cameras rendering` message.

Levels unlock sequentially through Level 10. Level 11 unlocks only when Level 10 has been completed and the persistent silver key hidden in Level 10 has been collected. Every successful exit locks control, visibly walks the miner through a supported doorway, records completion, and returns to the overview.

The overview also contains the earned-currency shop. Current prices are three green crystals for one health potion and twenty-five for one extra life.

## Project conventions

- Put gameplay scripts in `Assets/Scripts` and editor/build tools in `Assets/Editor`.
- Put playable scenes in `Assets/Scenes` and enable the overview followed by Levels 1-11 in progression order.
- Build reusable objects as prefabs instead of duplicating configured scene objects.
- Commit `.meta` files with their matching Unity assets.
- Every ordinary exit door must have a solid platform directly beneath it. Only an explicit level brief may allow a floating door.
- Mine platforms use irregular dark rocks with bronze mineral veins through and between them. Do not render them as bronze-framed rectangles.
- Keep platform visuals and colliders thinner than the initial prototype while preserving stable landings.
- Orient background composition to the shaft: vertical, angled, or horizontal. Level 2 uses angled background art for its lower reset ramp.

## Player rules and presentation

- Horizontal movement is 7.5 units per second, 75% of the original 10.
- Jump force is 12, gravity scale is 5.4, and the held-jump window is 0.24 seconds.
- Required routes must be completable with these defaults; reserve faster movement for a future speed power-up.
- The redesigned miner is approximately 125% of the old character's size and wears a detailed mining outfit.
- The silver helmet and small yellow lamp are integrated into the character art.
- The smaller mining pick is a separate hand-aligned visual that follows hand movement and facing direction.
- The player starts each level with five hearts. A spike hit removes one heart; invulnerability prevents immediate repeated hits.
- A new save starts with three lives, meaning three total attempts. Zero hearts consumes the current attempt and either restarts the level or returns to the overview when no life remains.
- A potion restores exactly one heart and is consumed with `H`.

### Reusable hero animation and outfits

- Keep one persistent hero identity across every dungeon. The face, proportions, gameplay scale, collider alignment, animation cadence, and attachment-point names must remain compatible between outfits.
- Represent the Bronze Miner, construction worker, astronaut, and future themes as swappable outfit profiles rather than separate player prefabs.
- Every outfit profile supplies clips or frame sets for side walk, side run, side jump/rise, apex, fall, land, front-facing walk toward the camera, and back-facing walk away from the camera.
- Side art may mirror for left/right. Outfit-specific asymmetry and hand-held tools may provide dedicated direction variants when mirroring would be visibly incorrect.
- Keep tools such as the mining pick separate from the body frames when they need to follow a hand attachment. The tool rig must inherit facing and animation motion without drifting from the hand.
- Gameplay movement selects side locomotion states from horizontal speed and grounded/vertical velocity state. Door entry explicitly selects the back-facing walk-away state; entrances or reveals may select front-facing walk-toward-camera.
- Outfit changes must not alter movement statistics, collision, health, level reachability, or saved hero identity unless a future design explicitly defines an equipment effect.

## Currency, keys, and chests

- Green crystals are the stored shop currency and save immediately.
- Green crystals are worth 1, blue crystals are worth 5, and purple crystals are worth 20.
- The shop sells a potion for 3 and an extra life for 25. Permanent heart upgrades are planned but their price is not yet fixed.
- Every level contains one level-specific bronze key and one chest.
- A bronze key opens only the chest in its own level. Key collection and opened-chest state persist per level.
- A chest is a one-time reward per save, preventing replay farming.
- Chest rewards are 50% blue-crystal value (+5 currency), 45% one potion, and 5% one extra life.
- The one silver key is hidden on a difficult optional path in Level 10 and persists globally for the Level 11 gate.

## Bronze Mines level matrix

Dungeon 1 is the Bronze Mines. Each of its eleven overview tunnels is a level. Dungeon 2 is the Silver Mines; do not shift Bronze Mines wall/platform material to silver within Levels 1-11.

| Level | Scene | Direction | Required implementation |
|---:|---|---|---|
| 1 | `Level1_TheMines.unity` | Vertical | Tutorial climb with stationary landings and supported exit. |
| 2 | `Level2_SlidingAscent.unity` | Angled hybrid | Flat horizontal upper platforms with gaps over a steep angled reset ramp and one-heart spikes. |
| 3 | `Level3_ChasmRun.unity` | Horizontal | Longer lateral route with bottomless gaps. |
| 4 | `Level4_CopperColumn.unity` | Vertical | Taller climb and increasing hazard pressure. |
| 5 | `Level5_CrookedIncline.unity` | Angled | Longer diagonal ascent and slide risk. |
| 6 | `Level6_BrokenRail.unity` | Horizontal | Harder bottomless-pit crossings. |
| 7 | `Level7_FurnaceRise.unity` | Vertical | Extended endurance climb with combined hazards. |
| 8 | `Level8_RazorAscent.unity` | Angled | Tighter diagonal spike timing. |
| 9 | `Level9_AbyssRun.unity` | Horizontal | Most difficult horizontal pit route. |
| 10 | `Level10_KeyVault.unity` | Vertical | Long climb plus hard optional silver-key route. |
| 11 | `Level11_TreasureVein.unity` | Angled | Silver-key-gated finale with exceptionally difficult treasure routes. |

Difficulty and length increase through the sequence. The direction cycle is vertical, angled, horizontal and then repeats; Level 2's horizontal upper surfaces remain the angled entry because its recovery motion, art, and lower ramp are diagonal.

### Level 2 reset behavior

- The completion route consists of thin horizontal platforms separated by gaps.
- The ramp under the route descends 18 degrees, catches missed jumps, and slides the miner back toward the bottom/start.
- Assign the ramp a dedicated zero- or near-zero-friction `PhysicsMaterial2D`; default platform friction is not acceptable. With movement input released, the miner must not perch and must reach the retry bottom reliably.
- Ramp spikes project upward and deal one heart each. Players must jump over them while sliding.
- Falling onto this ramp is recoverable and does not itself consume a life.
- A bottom reset returns the player to the route start without changing persistent key, chest, or crystal state.

### Horizontal bottomless pits

Levels 3, 6, and 9 place separated horizontal platforms above lethal pits. `DamageZone` or the level's lethal-fall component must route these falls through the normal death/life system. Do not reuse Level 2's nonlethal reset behavior for these tunnels.

### Level 11 content contract

- Gate entry on both Level 10 completion and the saved silver key.
- Include many green crystals.
- Include exactly five blue crystals, each configured for value 5.
- Include exactly one purple crystal, configured for value 20.
- Put all five blue crystals and the purple crystal on extremely difficult optional routes; none are required for exit completion.

## Build and validation

Generate scenes and assets with **Jump > Level Tools > Build Mines Levels**. Validate them with **Jump > Level Tools > Validate Mines Levels**.

Build Settings order:

1. `DungeonOverview.unity`
2. `Level1_TheMines.unity`
3. `Level2_SlidingAscent.unity`
4. Levels 3-11 in numeric order

Validation should fail if any of the following contracts are broken:

- The overview has no active camera or does not have eleven level nodes.
- A scene or level node is missing from progression order.
- A level lacks a bronze key, chest, supported exit, player, camera, or automated route.
- Level 11 can unlock without both prerequisites.
- Level 2 lacks its upper gaps, 18-degree low-friction reset ramp, reliable no-input slide, or spike hazards.
- Levels 3, 6, or 9 lack lethal pit zones.
- Level 10 lacks the silver key.
- Level 11 does not have exactly five blue value-5 crystals and one purple value-20 crystal.
- Shop prices, heart count, starting lives, damage, or potion healing differ from the design values.

## Automated playtest

The virtual controller drives movement through `HeroMovement`, follows ordered `AutomatedPlaytestWaypoint` objects, and enters the exit. Required routes should remain deterministic enough for unattended verification while optional key and treasure routes are validated separately.

```text
-batchmode -projectPath <project> -executeMethod AutomatedPlaytestCommand.Run
-automatedPlaytest -playtestReport <report-file>
```

The unattended command requires other Unity instances using the project to be closed. A timeout means the controller failed to execute the authored route or the door transition did not complete.

## Reusable mechanics

- `DamageZone` handles stationary hazards and lethal pits.
- `FallingSpike` warns, falls when the player passes below, and resets.
- `MovingPlatform` supports horizontal or vertical travel with pauses.
- `WeightedBreakablePlatform` spends durability according to apparent weight.
- `PlayerWeight` is the inventory and power-up integration point.
- The Level 2 reset zone returns a fallen miner to the start without consuming a life.
- Bronze-key, chest, silver-key, crystal, and completion state must use persistent progression data rather than scene-only state.

Apparent weight is `(base + carried) x weight multiplier x gravity multiplier`.

## Art acceptance checks

- Platform silhouettes read as separate rocks with branching bronze veins, not smooth bronze edging.
- Platform collision matches the visibly thinner top surface.
- The miner sprite has an integrated silver helmet and yellow lamp at gameplay scale.
- The small pick touches the hand and follows its movement and mirroring.
- Door entry keeps the miner visible until the character passes into the doorway.
- Level 2's background and support details reinforce the ramp angle.
- Side locomotion has distinct walk, run, rise, apex, fall, and land states with readable transitions.
- Front-facing and back-facing walk cycles retain the same recognizable face and proportions as the side view.
- Swapping between Bronze Miner, construction worker, and astronaut profiles changes clothing and equipment without changing hero identity, gameplay scale, collider alignment, or attachment names.
- Door entry uses the back-facing walk-away cycle, and separately rigged tools remain aligned throughout every supported state.
