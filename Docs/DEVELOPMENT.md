# Jump development guide

## Current game flow

`Assets/Scenes/DungeonOverview.unity` is first in Build Settings and must always contain an enabled rendering camera. The Bronze Mines overview has twelve interactive mineshafts, one for every playable tunnel. It must never show Unity's `No cameras rendering` message.

Levels unlock sequentially through Level 10. Level 11 unlocks only when Level 10 has been completed and the persistent silver key hidden in Level 10 has been collected. Completing Level 11 unlocks Level 12. Reaching an exit only shows `PRESS X / UP / W TO EXIT LEVEL`; a grounded controller-X, Up, or `W` press in range locks control, visibly walks the miner through the supported doorway, records completion, and returns to the overview.

At any point before the exit or death/respawn sequence begins, Start pauses the current level and Back abandons it to the overview shop. This retreat keeps already-saved rewards and the current life balance, restores normal time, and does not complete the level, unlock another tunnel, or charge a life.

When the player spends the final life, load the Game Over screen rather than `DungeonOverview`. Its **Restart** button clears the previous run's level unlocks, crystals, potions, heart upgrades, silver and bronze keys, and opened-chest state; it then creates a fresh three-life run and loads the overview.

The overview also contains the earned-currency shop. Current prices are three green crystals for one health potion and twenty-five for one extra life.

## Project conventions

- Put gameplay scripts in `Assets/Scripts` and editor/build tools in `Assets/Editor`.
- Put playable scenes in `Assets/Scenes` and enable the overview followed by Levels 1-12 in progression order.
- Build reusable objects as prefabs instead of duplicating configured scene objects.
- Commit `.meta` files with their matching Unity assets.
- Every ordinary exit door must have a solid platform directly beneath it and an enabled proximity trigger. Contact only shows the exit prompt; grounded controller-X or keyboard Up/W starts entry. Only an explicit level brief may allow a floating door.
- Mine platforms use irregular dark rocks with bronze mineral veins through and between them. Do not render them as bronze-framed rectangles.
- Keep platform visuals and colliders thinner than the initial prototype while preserving stable landings.
- Derive ordinary vertical headroom from the hero collider. For overlapping usable platform spans, require at least `standing collider height + 0.75` world units between the lower top and upper underside on main and optional routes.
- Do not silently waive headroom validation. A deliberately tight section must be named/tagged `Intentional Head-Bump Challenge`, documented in its level brief, visibly readable, and verified not to trap or damage the player invisibly.
- Orient background composition to the shaft: vertical, angled, horizontal, or downward. Levels 2, 5, 8, and 11, plus Level 12's angled sections, use dedicated diagonal-mine artwork in its authored orientation; do not simulate a diagonal mine by rotating the ordinary backdrop.

## Player rules and presentation

- Ordinary horizontal movement is 7.5 units per second, 75% of the original 10. Holding controller A or keyboard Shift with a horizontal direction raises movement to the 9-unit run speed.
- The ordinary jump uses force 12, gravity scale 5.4, an approximately 0.08-second grounded anticipation, and a 0.24-second held-jump window after takeoff.
- A directional power jump uses force 14.75 and a 0.26-second held-jump window. It is selected only when Run and a horizontal direction of at least 0.5 magnitude are held at the grounded jump press.
- Power-jump qualification is latched when the squat begins. Do not re-evaluate it during anticipation and downgrade a committed A+B or Shift+Space power jump before takeoff; live horizontal input may still steer the airborne miner.
- A valid grounded jump press commits the jump immediately but holds the upward impulse until the anticipation squat finishes. A quick tap must still launch; continuing to hold after takeoff controls the additional jump height.
- Required routes other than explicitly marked power-jump challenges must remain completable with the ordinary 7.5 movement and 12/.24 jump. Power-jump waypoints must be authored and validated explicitly.
- The hero's solid collider uses a zero-friction 2D physics material so sustained input against platform edges and walls cannot pin or hang the miner. Slope-specific slide behavior remains controlled by the contacted surface material.
- The redesigned miner is approximately 125% of the old character's size and wears a detailed mining outfit.
- The silver helmet and small yellow lamp are integrated into the character art.
- The Bronze Miner carries no pickaxe. Preserve the optional hand-tool attachment/profile architecture for later outfits or explicitly equipped tools, but do not instantiate or render a pick in the Bronze Mines.
- The player starts each level with five hearts. A spike hit removes one heart; invulnerability prevents immediate repeated hits.
- Render missing health with the same supported filled-heart glyph at a dim color or opacity. Do not use the unsupported outline-heart character, which can appear as an empty square in the active font.
- A new save starts with three lives, meaning three total attempts. Zero hearts consumes the current attempt and restarts the level only while another life remains.
- Spending the final life loads the Game Over screen. It must not automatically load the overview; only Restart clears all run progression and economy state, begins a fresh three-life run, and loads the overview.
- A potion restores exactly one heart and is consumed with controller Y or keyboard `H`.

### Centralized input and level-menu contract

All gameplay scripts read player controls through `MineInput`; do not add independent raw controller mappings to movement, health, chests, doors, or level-menu scripts. The semantic mapping is:

| Action | Controller | Keyboard fallback |
|---|---|---|
| Move | Left stick or left D-pad | Arrow keys or `A` / `D` |
| Run | Hold A | Hold either Shift key |
| Jump / parachute | B | Space |
| Interact with chest or door | X | Up Arrow or `W` |
| Use health potion | Y | `H` |
| Pause / resume | Start | Escape or `P` |
| Return to overview shop | Back | Backspace |
| Confirm UI selection | A | Enter, keypad Enter, or Space |

- `MineInput` uses Input System semantic `Gamepad` controls for A/B/X/Y, Start, Back, left stick, and D-pad, with the legacy XInput-index fallback only when no semantic gamepad is available.
- Set a Logitech F310 to **XInput/X mode** before launching the game. This lets Unity expose its physical labels as the semantic A/B/X/Y, Start, and Back controls; DirectInput mode is not the supported mapping target.
- Overview, Game Over, and every level pause menu use exactly one `EventSystem` with `InputSystemUIInputModule` and assigned actions. Do not restore `StandaloneInputModule`; stick and D-pad navigation, A submit, and reliable initial selection depend on the Input System module.
- Opening pause selects **Resume**. Overview and Game Over provide an initial controller-selected button so the menus work without a mouse.
- `MineLevelMenuController` owns level pause and retreat. Start/Escape/`P` toggles its pause panel and `Time.timeScale` between 0 and 1; disabling the controller must also restore normal time.
- Back/Backspace calls `ReturnToOverview`, restores `Time.timeScale` to 1, requests the overview's Shop page through the one-shot `OverviewArrival` state, and loads `DungeonOverview` without calling `GameProgress.CompleteLevel`, consuming a life, or resetting progress. Pause and retreat are disabled after either the exit-door completion sequence or a death/respawn sequence begins. Potion, chest, and door actions also reject a dead or respawning player. An overview entered with zero lives redirects to `GameOver`.

### Reusable hero animation and outfits

- Keep one persistent hero identity across every dungeon. The face, proportions, gameplay scale, collider alignment, animation cadence, and attachment-point names must remain compatible between outfits.
- Represent the Bronze Miner, construction worker, astronaut, and future themes as swappable outfit profiles rather than separate player prefabs.
- Every outfit profile supplies clips or frame sets for side walk, side run, side jump/rise, apex, fall, land, front-facing walk toward the camera, and back-facing walk away from the camera.
- The six side idle/jump cells in animation-sheet row 2 use zero-based frame order `0 idle`, `1 grounded squat`, `2 rise`, `3 apex`, `4 fall`, and `5 land`. Frame 1 is anticipation before physics takeoff and must never be selected as the high-velocity airborne pose.
- Side art may mirror for left/right. Outfit-specific asymmetry and optional hand-held tools may provide dedicated direction variants when mirroring would be visibly incorrect.
- Keep the optional tool attachment point and profile field available for future equipment. When a later outfit supplies a hand tool, its rig must inherit facing and animation motion without drifting from the hand through squat, rise, apex, fall, and landing; a null tool must produce no accessory object or validator failure.
- Gameplay movement gives the queued grounded-squat state priority, then selects side locomotion from horizontal speed and grounded/vertical velocity state. Positive takeoff velocity selects the rise pose even while the ground-check volume briefly overlaps the platform, preventing an idle-frame flash between squat and rise. Door entry explicitly selects the back-facing walk-away state; entrances or reveals may select front-facing walk-toward-camera.
- Outfit changes must not alter movement statistics, collision, health, level reachability, or saved hero identity unless a future design explicitly defines an equipment effect.

## Currency, keys, and chests

- Green crystals are the stored shop currency and save immediately.
- Green crystals are worth 1, blue crystals are worth 5, and purple crystals are worth 20.
- The shop sells a potion for 3 and an extra life for 25. Permanent heart upgrades are planned but their price is not yet fixed.
- Every level contains one level-specific bronze key and one chest.
- A bronze key opens only the chest in its own level. Key collection and opened-chest state persist per level.
- A chest is a one-time reward per save, preventing replay farming.
- Entering chest range never claims a reward automatically. A keyed player must press controller X, Up, or `W` while the HUD shows `PRESS X / UP / W TO OPEN CHEST`.
- Locked chests report that the same-level bronze key is required. Persisted claimed chests retain an enabled proximity trigger, use the distinct open/empty sprite, and report `CHEST ALREADY OPENED` on replay.
- Already-collected bronze keys remain hidden when their level is replayed, and the run-status HUD refreshes from saved key/chest state when the scene starts.
- Chest rewards are 50% blue-crystal value (+5 currency), 45% one potion, and 5% one extra life.
- The one silver key is hidden on a difficult optional path in Level 10 and persists globally for the Level 11 gate.

## Bronze Mines level matrix

Dungeon 1 is the Bronze Mines. Each of its twelve overview tunnels is a level. Dungeon 2 is the Silver Mines; do not shift Bronze Mines wall/platform material to silver within Levels 1-12.

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
| 11 | `Level11_TreasureVein.unity` | Angled | Silver-key-gated treasure tunnel with exceptionally difficult optional reward routes. |
| 12 | `Level12_DeepworksGauntlet.unity` | Mixed | Very long seeded twelve-section gauntlet combining three vertical climbs, three diagonal climbs, three horizontal pit runs, and three parachute descents. |

Difficulty and length increase through the sequence. Levels 1-11 follow the vertical, angled, horizontal cycle; Level 12 is the deliberate mixed-direction capstone. Level 2's individual upper surfaces remain horizontal while their centers, dedicated art, and parallel recovery ramp form a diagonal ascent.

### Platform headroom contract

- Measure clear vertical headroom from the top of a lower collider to the underside of an overlapping upper collider, using the configured standing hero collider rather than sprite bounds.
- Ordinary clearance is at least the hero collider height plus 0.75 world units across all twelve levels, including optional key, chest, and crystal routes.
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

Levels 3, 6, and 9, along with Level 12's horizontal sections, place separated horizontal platforms above localized lethal pits. `DamageZone` or the level's lethal-fall component must route these falls through the normal death/life system. Do not reuse Level 2's nonlethal reset behavior for these tunnels.

Level 3 has a dedicated regression contract for the reported invisible-death bug: every lethal trigger must be horizontally contained by a visible pit gap and vertically below its adjacent platform tops. No lethal collider may intersect the spawn, a platform collider, an automated waypoint, a required landing envelope, or the normal jump corridor. The focused automated regression must cross the first visible gap and reach the first authored landing with five hearts and zero respawns; later deaths are valid only after a visibly missed pit jump or hazard hit.

### Level 10 power-jump and spike-clearance contract

- Every required grounded landing waypoint in Level 10 is marked `UsePowerJump`. The automated controller must therefore use the same directional Run+Jump commitment as a player holding A+B or Shift+Space.
- Route spikes remain dangerous, but the authored safe landing center must not overlap their damage envelope. For a spike at the same landing elevation, horizontal clearance is `abs(waypoint.x - spike-center.x) - hero-half-width - spike-half-width` and must be at least 0.25 world units.
- For Level 10, the builder alternates ledges across `x = -3.1/+3.1`, places each required-route spike 1.25 world units toward that ledge's far wall, and offsets the safe waypoint 0.5 units toward the central jump gap with a 0.3-unit landing radius. The resulting 5.2-unit zigzag remains a power-jump route without forcing the player to recross a spike during the next squat. Preserve or increase the validated clearance when platform, hero-collider, or spike-collider dimensions change. Other vertical levels retain their standard offsets.
- The required exit route must pass the no-damage and no-respawn power-run playtest. The hidden silver-key branch may be harder, but it must remain optional and visibly readable.

### Level 11 content contract

- Gate entry on both Level 10 completion and the saved silver key.
- Include many green crystals.
- Include exactly five blue crystals, each configured for value 5.
- Include exactly one purple crystal, configured for value 20.
- Put all five blue crystals and the purple crystal on extremely difficult optional routes; none are required for exit completion.

### Level 12 mixed-gauntlet contract

- Completing Level 11 is the only Level 12 unlock requirement; Level 11's silver-key gate remains unchanged.
- Build a very long twelve-section route containing exactly three each of vertical-up, angled-up, horizontal, and vertical-down traversal. Use a stable seed so the order looks shuffled while builds, validation, and automated routes remain reproducible.
- Horizontal sections use visible platform gaps with localized lethal triggers contained beneath those gaps.
- Each downward section begins with a readable parachute prompt. Hold Jump to deploy the parachute and slow descent, release Jump to fast-drop, and steer horizontally to pass the authored safe lanes.
- Downward hazards include camouflaged wall spikes and moving hazards. Hidden spikes must reveal and warn before becoming damaging, leave a viable dodge lane, and remove exactly one heart per hit.
- Use a descent-aware camera and airborne automated waypoints so the view and unattended playtest lead the player toward upcoming hazards rather than looking above the miner.
- Compose Level 12 from direction-specific background pieces. Its diagonal sections use the same dedicated, unrotated diagonal-mine artwork as the other angled Bronze Mines levels.

## Build and validation

Generate scenes and assets with **Jump > Level Tools > Build Mines Levels**. Validate them with **Jump > Level Tools > Validate Mines Levels**.

Build Settings order:

1. `DungeonOverview.unity`
2. `GameOver.unity`
3. `Level1_TheMines.unity`
4. `Level2_SlidingAscent.unity`
5. Levels 3-12 in numeric order

Validation should fail if any of the following contracts are broken:

- The overview has no active camera or does not have twelve level nodes.
- A scene or level node is missing from progression order.
- A level lacks a bronze key, chest, supported exit, player, camera, or automated route.
- An exit completes on contact, lacks an enabled X/Up/W proximity trigger, omits the exit prompt, or bypasses the visible walk-in sequence.
- A chest opens merely from contact, lacks an enabled X/Up/W proximity trigger, has no distinct open-state sprite, or silently ignores a replayed already-claimed state.
- `MineInput` no longer maps A run, B jump, X interact, Y potion, Start pause, and Back return-to-shop, or movement omits either the left stick or left D-pad.
- A level lacks its initially hidden `MineLevelMenuController` pause panel, Resume and Return-to-Shop actions, or explanatory Start/Back labels.
- An overview, Game Over, or level UI scene lacks its single action-backed `InputSystemUIInputModule`, uses `StandaloneInputModule`, or omits the required initial controller selection.
- Ordinary platform overlap provides less than `hero collider height + 0.75` headroom without an explicit, documented `Intentional Head-Bump Challenge` marker.
- Level 11 can unlock without both prerequisites.
- Level 12 can unlock before Level 11 is completed.
- Level 2's upper centers do not rise diagonally, widths and two-axis gaps do not vary, or the 18-degree low-friction ramp is not parallel beneath the route.
- Any Level 2 ramp spike points sideways instead of upward, or the no-input slide does not reach the retry bottom.
- Levels 3, 6, or 9 lack lethal pit zones, or Level 12's localized pit triggers extend outside their visible horizontal gaps.
- Any Level 3 lethal trigger overlaps playable geometry or an intended movement corridor, or the focused spawn-to-first-landing regression loses health or a life.
- Final-life depletion bypasses `GameOver.unity`, Restart fails to restore three lives, or any crystals, potions, upgrades, unlocks, keys, or opened-chest state survives Restart.
- Level 10 lacks the silver key.
- A Level 10 required landing is not marked `UsePowerJump`, or a same-height spike leaves less than 0.25 world units of horizontal landing clearance after hero and spike extents are included.
- Level 11 does not have exactly five blue value-5 crystals and one purple value-20 crystal.
- Level 12 does not contain exactly three sections of each required direction, a reproducible seeded order, parachute-enabled descents, airborne route coverage, or fair-reveal one-heart hazards with viable dodge lanes.
- Shop prices, heart count, starting lives, damage, or potion healing differ from the design values.
- Ordinary walk/run tuning differs from 7.5/9, ordinary/power jump tuning differs from 12/.24 and 14.75/.26, a directional Run+Jump commitment is downgraded during anticipation, or a grounded jump does not preserve the approximately 0.08-second squat before its upward impulse.
- The side jump cells do not follow the documented row-2 order, or a quick tap can be discarded before takeoff.
- The hero collider has friction that allows sustained wall/ledge sticking, the HUD uses an unsupported empty-heart glyph, the Bronze Miner renders a pickaxe, or an absent optional tool breaks the outfit contract.
- An angled Bronze Mines level or Level 12 angled section rotates the ordinary backdrop instead of using the dedicated diagonal-mine artwork in its authored orientation.

## Automated playtest

The virtual controller drives movement through `HeroMovement`, follows ordered `AutomatedPlaytestWaypoint` objects, and explicitly activates the exit after reaching its proximity trigger while grounded. A waypoint's `UsePowerJump` flag commits Run+Jump for that required launch; `-playtestPowerRun` forces the same behavior for every eligible route jump. Required routes should remain deterministic enough for unattended verification while optional key and treasure routes are validated separately. Level 12 additionally uses airborne-pass waypoints and automated parachute deployment so all three descent corridors are exercised through their intended safe lanes.

```text
-batchmode -projectPath <project> -executeMethod AutomatedPlaytestCommand.Run
-automatedPlaytest -playtestReport <report-file>
```

Supported focused flags are:

- `-playtestScene <Assets/Scenes/Scene.unity>` overrides the default Level 1 scene.
- `-playtestTimeout <seconds>` overrides the 120-second real-time limit.
- `-playtestFailOnRespawn` fails as soon as the route consumes a life.
- `-playtestFailOnDamage` fails as soon as the miner loses a heart.
- `-playtestPowerRun` holds Run for all eligible jumps; per-waypoint `UsePowerJump` still works without this global flag.
- `-playtestPassAfterWaypoints <count>` ends after a focused waypoint count, such as Level 3's first-gap regression.
- `-playtestReturnHome` skips traversal after the initial grounded spawn, first enters the zero-time-scale pause state, and then invokes `MineLevelMenuController.ReturnToOverview`. It passes only if `DungeonOverview` loads with the Shop page visible, `Time.timeScale` restored to 1, and unlock, crystal, life, and potion values unchanged.
- `-playtestTraceFirstJump` logs the first launch's position, velocity, grounded/preparation state, and target at short intervals for diagnosing ledge-side collisions. The route driver begins its squat about two world units before a support edge, accounting for horizontal travel during the anticipation pose.

The controller waits for the spawn landing, runs at normal gameplay time, and normally accepts only the exit door's configured destination as successful completion; Game Over or any other unexpected scene transition is a failure. Run Level 10 with `-playtestPowerRun -playtestFailOnDamage -playtestFailOnRespawn` to cover its required power-jump route and spike-clear landing centers.

The mechanics smoke test can be run with `-executeMethod MineMechanicsSmokeTestCommand.Run -mineMechanicsSmokeTest -mechanicsReport <report-file>`. It observes the jump transition itself: immediately after a grounded press, the miner remains grounded with approximately zero upward velocity and displays `bronze_miner_2_1`; after roughly 0.08 seconds, upward velocity begins and the visible body advances directly to `bronze_miner_2_2`, without returning to `bronze_miner_2_0`. Coverage includes a quick tap that still launches, a held ordinary jump, 7.5 walk versus 9 run speed and their animation rows, and a committed directional 14.75/.26 power jump that exceeds the ordinary arc and travels horizontally. Route playtests must retain enough observation time for the anticipation delay.

Input and menu smoke coverage verifies the centralized A/B/X/Y/Start/Back contract, toggles `MineLevelMenuController` into and out of a zero-time-scale pause with the panel state synchronized, confirms its home target is `DungeonOverview`, and proves death/respawn rejects pause, retreat, and potion consumption while still consuming exactly one life. The separate `-playtestReturnHome` scene-transition coverage returns from the paused state and proves retreat opens the Shop, restores normal time, preserves progress, and does not complete the abandoned level or spend a life.

Chest smoke coverage verifies that contact alone does not open a chest, an X/Up/W interaction without the key is rejected, a keyed interaction grants exactly one deterministic reward, a second interaction cannot duplicate it, and replay state restores the open sprite, prompt trigger, hidden collected key, and `CHEST ALREADY OPENED` feedback.

Door smoke coverage verifies that contact alone leaves the exit unused, proximity displays `PRESS X / UP / W TO EXIT LEVEL`, and an explicit grounded X/Up/W interaction starts the existing kinematic walk-through transition. The full virtual-controller traversal then verifies that this explicit activation reaches only the configured overview scene.

The unattended command requires other Unity instances using the project to be closed. A timeout means the controller failed to execute the authored route or the door transition did not complete.

## Reusable mechanics

- `DamageZone` handles stationary hazards and lethal pits.
- `FallingSpike` warns, falls when the player passes below, and resets.
- `MovingPlatform` supports horizontal or vertical travel with pauses.
- `WeightedBreakablePlatform` spends durability according to apparent weight.
- `PlayerWeight` is the inventory and power-up integration point.
- The Level 2 reset zone returns a fallen miner to the start without consuming a life.
- Level 12 descent zones suppress ordinary jump launch, map held Jump to parachute deployment, restore normal gravity on exit/reset, and support slower deployed descent plus a released fast-drop.
- Fair-reveal hidden hazards expose a warning before enabling one-heart damage; moving descent hazards preserve a reachable safe lane.
- Bronze-key, chest, silver-key, crystal, and completion state must use persistent progression data rather than scene-only state.

Apparent weight is `(base + carried) x weight multiplier x gravity multiplier`.

## Art acceptance checks

- Platform silhouettes read as separate rocks with branching bronze veins, not smooth bronze edging.
- Platform collision matches the visibly thinner top surface.
- The miner sprite has an integrated silver helmet and yellow lamp at gameplay scale.
- The Bronze Miner has no visible pickaxe, while the optional hand-tool attachment remains available for future profiles.
- The anticipation squat is visibly held before the miner leaves the ground, transitions directly into the rise pose, and never appears suspended in midair.
- Door entry keeps the miner visible until the character passes into the doorway.
- Levels 2, 5, 8, and 11 use dedicated unrotated diagonal-mine artwork, and Level 12 changes background composition with each route section.
- Empty heart slots read as dim filled hearts rather than missing-font squares.
- Sustained movement into a wall or platform edge does not leave the miner friction-locked in place.
- Side locomotion has distinct walk, run, rise, apex, fall, and land states with readable transitions.
- Front-facing and back-facing walk cycles retain the same recognizable face and proportions as the side view.
- Swapping between Bronze Miner, construction worker, and astronaut profiles changes clothing and equipment without changing hero identity, gameplay scale, collider alignment, or attachment names.
- Door entry uses the back-facing walk-away cycle, and any future separately rigged tool remains aligned throughout every supported state.
