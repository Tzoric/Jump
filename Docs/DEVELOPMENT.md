# Jump development guide

## Current game flow

`Assets/Scenes/DungeonOverview.unity` is first in Build Settings and must always contain an enabled rendering camera. The Bronze Mines overview has eleven interactive mineshafts, one for every playable tunnel. It must never show Unity's `No cameras rendering` message.

Levels unlock sequentially through Level 10. Level 11 unlocks only when Level 10 has been completed and the persistent silver key hidden in Level 10 has been collected. Every successful exit locks control, visibly walks the miner through a supported doorway, records completion, and returns to the overview.

When the player spends the final life, load the Game Over screen rather than `DungeonOverview`. Its **Restart** button restores the starting three lives and then loads the overview; all other persistent progression and economy data remains unchanged.

The overview also contains the earned-currency shop. Current prices are three green crystals for one health potion and twenty-five for one extra life.

## Project conventions

- Put gameplay scripts in `Assets/Scripts` and editor/build tools in `Assets/Editor`.
- Put playable scenes in `Assets/Scenes` and enable the overview followed by Levels 1-11 in progression order.
- Build reusable objects as prefabs instead of duplicating configured scene objects.
- Commit `.meta` files with their matching Unity assets.
- Every ordinary exit door must have a solid platform directly beneath it. Only an explicit level brief may allow a floating door.
- Mine platforms use irregular dark rocks with bronze mineral veins through and between them. Do not render them as bronze-framed rectangles.
- Keep platform visuals and colliders thinner than the initial prototype while preserving stable landings.
- Derive ordinary vertical headroom from the hero collider. For overlapping usable platform spans, require at least `standing collider height + 0.75` world units between the lower top and upper underside on main and optional routes.
- Do not silently waive headroom validation. A deliberately tight section must be named/tagged `Intentional Head-Bump Challenge`, documented in its level brief, visibly readable, and verified not to trap or damage the player invisibly.
- Orient background composition to the shaft: vertical, angled, or horizontal. Level 2 uses angled background art for its lower reset ramp.

## Player rules and presentation

- Horizontal movement is 7.5 units per second, 75% of the original 10.
- Jump force is 12, gravity scale is 5.4, the grounded anticipation is approximately 0.08 seconds, and the held-jump window after takeoff is 0.24 seconds.
- A valid grounded jump press commits the jump immediately but holds the upward impulse until the anticipation squat finishes. A quick tap must still launch; continuing to hold after takeoff controls the additional jump height.
- Required routes must be completable with these defaults; reserve faster movement for a future speed power-up.
- The redesigned miner is approximately 125% of the old character's size and wears a detailed mining outfit.
- The silver helmet and small yellow lamp are integrated into the character art.
- The smaller mining pick is a separate hand-aligned visual that follows hand movement and facing direction.
- The player starts each level with five hearts. A spike hit removes one heart; invulnerability prevents immediate repeated hits.
- A new save starts with three lives, meaning three total attempts. Zero hearts consumes the current attempt and restarts the level only while another life remains.
- Spending the final life loads the Game Over screen. It must not automatically load the overview; only Restart begins a new three-life run and loads the overview while preserving progression, keys, chest state, currency, inventory, and upgrades.
- A potion restores exactly one heart and is consumed with `H`.

### Reusable hero animation and outfits

- Keep one persistent hero identity across every dungeon. The face, proportions, gameplay scale, collider alignment, animation cadence, and attachment-point names must remain compatible between outfits.
- Represent the Bronze Miner, construction worker, astronaut, and future themes as swappable outfit profiles rather than separate player prefabs.
- Every outfit profile supplies clips or frame sets for side walk, side run, side jump/rise, apex, fall, land, front-facing walk toward the camera, and back-facing walk away from the camera.
- The six side idle/jump cells in animation-sheet row 2 use zero-based frame order `0 idle`, `1 grounded squat`, `2 rise`, `3 apex`, `4 fall`, and `5 land`. Frame 1 is anticipation before physics takeoff and must never be selected as the high-velocity airborne pose.
- Side art may mirror for left/right. Outfit-specific asymmetry and hand-held tools may provide dedicated direction variants when mirroring would be visibly incorrect.
- Keep tools such as the mining pick separate from the body frames when they need to follow a hand attachment. The tool rig must inherit facing and animation motion without drifting from the hand, including the lowered squat, rise, apex, fall, and landing poses.
- Gameplay movement gives the queued grounded-squat state priority, then selects side locomotion from horizontal speed and grounded/vertical velocity state. Positive takeoff velocity selects the rise pose even while the ground-check volume briefly overlaps the platform, preventing an idle-frame flash between squat and rise. Door entry explicitly selects the back-facing walk-away state; entrances or reveals may select front-facing walk-toward-camera.
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
| 2 | `Level2_SlidingAscent.unity` | Angled hybrid | Varied horizontal-surfaced platforms rise diagonally over a parallel low-friction ramp with upward-facing one-heart spikes. |
| 3 | `Level3_ChasmRun.unity` | Horizontal | Longer lateral route with visible bottomless gaps and no lethal trigger overlap on playable geometry. |
| 4 | `Level4_CopperColumn.unity` | Vertical | Taller climb and increasing hazard pressure. |
| 5 | `Level5_CrookedIncline.unity` | Angled | Longer diagonal ascent and slide risk. |
| 6 | `Level6_BrokenRail.unity` | Horizontal | Harder bottomless-pit crossings. |
| 7 | `Level7_FurnaceRise.unity` | Vertical | Extended endurance climb with combined hazards. |
| 8 | `Level8_RazorAscent.unity` | Angled | Tighter diagonal spike timing. |
| 9 | `Level9_AbyssRun.unity` | Horizontal | Most difficult horizontal pit route. |
| 10 | `Level10_KeyVault.unity` | Vertical | Long climb plus hard optional silver-key route. |
| 11 | `Level11_TreasureVein.unity` | Angled | Silver-key-gated finale with exceptionally difficult treasure routes. |

Difficulty and length increase through the sequence. The direction cycle is vertical, angled, horizontal and then repeats; Level 2's individual upper surfaces remain horizontal while their centers, art, and parallel recovery ramp form a diagonal ascent.

### Platform headroom contract

- Measure clear vertical headroom from the top of a lower collider to the underside of an overlapping upper collider, using the configured standing hero collider rather than sprite bounds.
- Ordinary clearance is at least the hero collider height plus 0.75 world units across all eleven levels, including optional key, chest, and crystal routes.
- Builder spacing changes must preserve reachable jump distances and camera framing; increasing clearance must not create impossible required jumps.
- Main vertical shafts stagger consecutive ledges laterally as well as vertically. Their required door foundation extends outward beside the last ledge rather than hanging over the final jump.
- The validator may exempt only explicitly named/tagged `Intentional Head-Bump Challenge` geometry. Each exemption needs a documented purpose, a readable approach, and a traversal test proving the player cannot become trapped or take invisible damage.

### Level 2 reset behavior

- The completion route uses individually horizontal platforms whose centers rise diagonally toward the exit.
- Use at least three materially distinct platform widths. Consecutive jumps must vary both horizontal and vertical gap sizes rather than repeating one delta.
- The low-friction ramp stays beneath the gap catch area and runs at 18 degrees, parallel within 2 degrees of the upper route's overall trend. Its downhill end is the retry bottom/start.
- Assign the ramp a dedicated zero- or near-zero-friction `PhysicsMaterial2D`; default platform friction is not acceptable. With movement input released, the miner must not perch and must reach the retry bottom reliably.
- Seat ramp spikes on the slope but keep their tips within 5 degrees of world up; do not inherit the ramp's sideways rotation. They deal one heart each and must be jumped while sliding.
- Falling onto this ramp is recoverable and does not itself consume a life.
- A bottom reset returns the player to the route start without changing persistent key, chest, or crystal state.

### Horizontal bottomless pits

Levels 3, 6, and 9 place separated horizontal platforms above lethal pits. `DamageZone` or the level's lethal-fall component must route these falls through the normal death/life system. Do not reuse Level 2's nonlethal reset behavior for these tunnels.

Level 3 has a dedicated regression contract for the reported invisible-death bug: every lethal trigger must be horizontally contained by a visible pit gap and vertically below its adjacent platform tops. No lethal collider may intersect the spawn, a platform collider, an automated waypoint, a required landing envelope, or the normal jump corridor. The focused automated regression must cross the first visible gap and reach the first authored landing with five hearts and zero respawns; later deaths are valid only after a visibly missed pit jump or hazard hit.

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
2. `GameOver.unity`
3. `Level1_TheMines.unity`
4. `Level2_SlidingAscent.unity`
5. Levels 3-11 in numeric order

Validation should fail if any of the following contracts are broken:

- The overview has no active camera or does not have eleven level nodes.
- A scene or level node is missing from progression order.
- A level lacks a bronze key, chest, supported exit, player, camera, or automated route.
- Ordinary platform overlap provides less than `hero collider height + 0.75` headroom without an explicit, documented `Intentional Head-Bump Challenge` marker.
- Level 11 can unlock without both prerequisites.
- Level 2's upper centers do not rise diagonally, widths and two-axis gaps do not vary, or the 18-degree low-friction ramp is not parallel beneath the route.
- Any Level 2 ramp spike points sideways instead of upward, or the no-input slide does not reach the retry bottom.
- Levels 3, 6, or 9 lack lethal pit zones.
- Any Level 3 lethal trigger overlaps playable geometry or an intended movement corridor, or the focused spawn-to-first-landing regression loses health or a life.
- Final-life depletion bypasses `GameOver.unity`, Restart fails to restore three lives, or Restart clears persistent progress.
- Level 10 lacks the silver key.
- Level 11 does not have exactly five blue value-5 crystals and one purple value-20 crystal.
- Shop prices, heart count, starting lives, damage, or potion healing differ from the design values.
- A grounded jump does not preserve the approximately 0.08-second squat before its upward impulse, the side jump cells do not follow the documented row-2 order, or a quick tap can be discarded before takeoff.

## Automated playtest

The virtual controller drives movement through `HeroMovement`, follows ordered `AutomatedPlaytestWaypoint` objects, and enters the exit. Required routes should remain deterministic enough for unattended verification while optional key and treasure routes are validated separately.

```text
-batchmode -projectPath <project> -executeMethod AutomatedPlaytestCommand.Run
-automatedPlaytest -playtestReport <report-file>
```

Use `-playtestScene <Assets/Scenes/Scene.unity>` to override the default Level 1 scene. `-playtestFailOnRespawn` turns any lost life into an immediate failure, and `-playtestPassAfterWaypoints <count>` supports focused geometry regressions such as Level 3's first-gap check. The controller waits for the spawn landing, runs at normal gameplay time, and accepts only the exit door's configured destination as successful completion; Game Over or any other scene transition is a failure.

The mechanics smoke test must observe the jump transition itself: immediately after a grounded press, the miner remains grounded with approximately zero upward velocity and displays `bronze_miner_2_1`; after roughly 0.08 seconds, upward velocity begins and the visible body advances directly to `bronze_miner_2_2`, without returning to `bronze_miner_2_0`. Coverage includes both a quick tap that still launches and a held jump that reaches the expected higher arc. Route playtests must retain enough observation time for the anticipation delay.

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
- The anticipation squat is visibly held before the miner leaves the ground, transitions directly into the rise pose, and never appears suspended in midair.
- The small pick follows the hand through squat, rise, apex, fall, and landing without retaining an unrelated idle/walk swing or drifting from the grip.
- Door entry keeps the miner visible until the character passes into the doorway.
- Level 2's background and support details reinforce the ramp angle.
- Side locomotion has distinct walk, run, rise, apex, fall, and land states with readable transitions.
- Front-facing and back-facing walk cycles retain the same recognizable face and proportions as the side view.
- Swapping between Bronze Miner, construction worker, and astronaut profiles changes clothing and equipment without changing hero identity, gameplay scale, collider alignment, or attachment names.
- Door entry uses the back-facing walk-away cycle, and separately rigged tools remain aligned throughout every supported state.
