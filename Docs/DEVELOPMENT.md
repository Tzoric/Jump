# Jump development guide

## Current game flow

`Assets/Scenes/DungeonOverview.unity` is first in Build Settings and must always contain an enabled rendering camera. The Bronze Mines overview has twelve interactive mineshafts, one for every playable tunnel. It must never show Unity's `No cameras rendering` message.

Levels unlock sequentially through Level 10. Level 11 unlocks only when Level 10 has been completed and the persistent silver key hidden in Level 10 has been collected. Completing Level 11 unlocks Level 12. Every level begins at a supported entrance door: `LevelEntranceDoor` locks control, walks the miner toward the camera from inside the doorway, then restores physics, collision, input, and the authored starting checkpoint. Reaching an exit only shows the current Interact binding plus Up/W; a grounded mapped-Interact, Up, or `W` press in range locks control, visibly walks the miner through the supported doorway, records completion, and returns to the overview.

At any point before the exit or death/respawn sequence begins, mapped Pause (default Start) pauses the current level and mapped Shop (default Back, keyboard Backspace) opens the shop over the current level. The modal shop pauses time and preserves the miner, camera, hazards, collectibles, and level state in place. Closing it resumes at the same location; its explicit **Return to Overview** button remains the only shop path that abandons the attempt.

When the player spends the final life, load the Game Over screen rather than `DungeonOverview`. Its **Restart** button clears the previous run's level unlocks, crystals, potions, heart upgrades, silver and bronze keys, and opened-chest state; it then creates a fresh three-life run and loads the overview.

The overview has three mutually exclusive pages: twelve-tunnel Levels, the earned-currency Shop, and controller Controls. Current prices are three green crystals for one health potion and twenty-five for one extra life.

For rapid testing, the Levels page contains a hidden, session-only **Foreman's Master Key**. Type `MINER`, or enter `Up, Up, Down, Down, Left, Right, Left, Right, Down, Up` with the selected controller's D-pad/left stick, returning to neutral between directions. The active banner must clearly say that all twelve tunnels are open. This override does not change the saved highest level or grant the silver key. Selecting a level while it is active copies progression into an in-memory sandbox; crystals, lives, potions, upgrades, keys, chests, and completion behave normally during the test but are discarded when the overview returns. During that sandbox run only, typing `HEALTH` restores the miner to maximum health and typing `LIFE` grants ten lives. Enter the master-key code again to restore story locks, or end the play session.

## Project conventions

- The current editor baseline is Unity 6.3 LTS, `6000.3.20f1`. Open, build, serialize, and validate the project with that version unless a later migration deliberately updates `ProjectSettings/ProjectVersion.txt` and this guide together.
- Unity's Visual Studio integration now generates the SDK-style `Jump.slnx`. Install the x64 .NET 9 SDK `9.0.200` or newer before opening it in VS Code; this workstation was verified with `9.0.316`. The runtime alone is not sufficient. After an SDK update, restart VS Code and confirm `dotnet sln .\Jump.slnx list` succeeds.
- Put gameplay scripts in `Assets/Scripts` and editor/build tools in `Assets/Editor`.
- Put playable scenes in `Assets/Scenes`. Build Settings begin with the Bronze overview and Levels 1-12 in progression order, followed by `SilverDungeonOverview.unity` and `SilverLevel1_SilverLode.unity`.
- Build reusable objects as prefabs instead of duplicating configured scene objects.
- Commit `.meta` files with their matching Unity assets.
- Every level must contain exactly one `LevelEntranceDoor` and one exit door, both supported by solid platforms. The entrance owns the automatic front-facing walk-out intro and is not interactable after arrival. The exit retains its enabled proximity trigger; contact only shows the prompt and grounded mapped Interact or keyboard Up/W starts entry. Only an explicit level brief may allow a floating door.
- Bronze spike damage uses `SpikeHitboxGeometry`: one `PolygonCollider2D` trigger with three inset triangular paths matching the visible teeth. Never replace it with a sprite-sized box or add damage across the transparent side/valley pixels; transforms apply rotation and non-uniform scale to art and collider together.
- Mine platforms use the shared fill/edge/corner rock tiles so exposed surfaces end in whole stones instead of cropped texture edges. `DungeonVisualTheme` supplies the dungeon's metal palette and flake density: Bronze uses bronze inclusions and Silver uses silver inclusions without duplicating the underlying stone geometry.
- Keep platform visuals and colliders thinner than the initial prototype while preserving stable landings.
- Derive ordinary vertical headroom from the hero collider. For overlapping usable platform spans, require at least `standing collider height + 0.75` world units between the lower top and upper underside on main and optional routes.
- Do not silently waive headroom validation. A deliberately tight section must be named/tagged `Intentional Head-Bump Challenge`, documented in its level brief, visibly readable, and verified not to trap or damage the player invisibly.
- Orient background composition to the shaft: vertical, angled, horizontal, or downward. Levels 2, 5, 8, and 11, plus Level 12's angled sections, use dedicated diagonal-mine artwork in its authored orientation; do not simulate a diagonal mine by rotating the ordinary backdrop.
- Construct long and mixed levels from modular direction and transition pieces at uniform scale. Tile them beyond every camera edge and beneath fall corridors instead of non-uniformly stretching a single backdrop. Keep background-only beams and rails subdued; visible foreground rock and shaft-wall art must agree with actual collision.
- Treat player-supplied stick-figure route sketches as source briefs. First convert a sketch into section bounds, ordered waypoints, collision walls, launch/landing shelves, hazards, camera framing, and background-module assignments; then build scenery around that plan. Incidental detail in a scenic painting does not define collision.

## Player rules and presentation

- Ordinary horizontal movement is 7.5 units per second, 75% of the original 10. Holding mapped Run (default controller A) or keyboard Shift with a horizontal direction raises movement to the 9-unit run speed.
- The ordinary jump uses force 12, gravity scale 5.4, an approximately 0.08-second grounded anticipation, and a 0.24-second held-jump window after takeoff.
- A directional power jump uses force 14.75 and a 0.26-second held-jump window. It is selected only when Run and a horizontal direction of at least 0.5 magnitude are held at the grounded jump press.
- Power-jump qualification is latched when the squat begins. Do not re-evaluate it during anticipation and downgrade a committed A+B or Shift+Space power jump before takeoff; live horizontal input may still steer the airborne miner.
- A valid grounded jump press commits the jump immediately but holds the upward impulse until the anticipation squat finishes. A quick tap must still launch; continuing to hold after takeoff controls the additional jump height.
- Required routes other than explicitly marked power-jump challenges must remain completable with the ordinary 7.5 movement and 12/.24 jump. Power-jump waypoints must be authored and validated explicitly.
- The hero's solid collider uses a zero-friction 2D physics material so sustained input against platform edges and walls cannot pin or hang the miner. Slope-specific slide behavior remains controlled by the contacted surface material.
- The redesigned miner is approximately 125% of the old character's size and wears a detailed mining outfit.
- The silver helmet and small yellow lamp are integrated into the character art.
- The Bronze Miner carries no pickaxe. Preserve the optional hand-tool attachment/profile architecture for later outfits or explicitly equipped tools, but do not instantiate or render a pick in the Bronze Mines.
- The player starts each level with seven hearts. A spike hit removes one heart; invulnerability prevents immediate repeated hits.
- The damage flash is temporary presentation state. A new hit, respawn, component shutdown, or door entry must cancel any older flash and restore the miner body's authored color and full opacity before another system takes control.
- Render missing health with the same supported filled-heart glyph at a dim color or opacity. Do not use the unsupported outline-heart character, which can appear as an empty square in the active font.
- A new save starts with three lives, meaning three total attempts. Zero hearts consumes the current attempt and restarts the level only while another life remains.
- Spending the final life loads the Game Over screen. It must not automatically load the overview; only Restart clears all run progression and economy state, begins a fresh three-life run, and loads the overview.
- A potion restores exactly one heart and is consumed with mapped Health Potion (default controller Y) or keyboard `H`.

### Centralized input and level-menu contract

All gameplay scripts read player controls through `MineInput`; do not add independent raw controller mappings to movement, health, chests, doors, or level-menu scripts. The shipped controller defaults are:

| Action | Default controller | Fixed keyboard fallback |
|---|---|---|
| Move | Left stick or left D-pad | Arrow keys or `A` / `D` |
| Run | Hold A | Hold either Shift key |
| Jump | B | Space |
| Interact with chest/door or toggle hang glider while airborne | X | Up Arrow or `W` |
| Use health potion | Y | `H` |
| Pause / resume | Start | Escape or `P` |
| Open / close in-level shop | Back | Backspace |
| Confirm UI selection | A | Enter, keypad Enter, or Space |

- `Assets/Resources/MineControllerActions.inputactions` is the controller-only six-button action asset. Its stable action/binding GUIDs are part of the save contract; do not recreate these bindings with random runtime IDs.
- `MineInput` instantiates that asset, selects one active `Gamepad` or `Joystick` from meaningful stick, D-pad, or button activity, loads a versioned PlayerPrefs override profile derived from the controller model, and exposes the existing static input facade. When Unity reports a semantic `Gamepad` and a noisy generic `Joystick` at the same time, prefer the semantic device until the player deliberately activates another controller. Generic DirectInput joysticks receive common Run/Jump/Interact/Potion/Home/Pause defaults and remain remappable. Movement, keyboard fallbacks, and UI Submit are intentionally outside those overrides.
- `MineControlsController` owns the overview Controls page. It waits until the UI Submit press has been released, temporarily disables `InputSystemUIInputModule`, captures one button from the selected controller, saves automatically, and always disposes the rebind operation on completion, cancellation, timeout, page close, or device loss.
- Run, Jump, Interact/Hang Glider, Potion, Pause, and Shop are remappable. Interact leaves Jump independent: while airborne anywhere in a level, press it once to deploy the smaller, correctly oriented hang glider and press it again to stow it. With the glider open, neutral input glides, Up/W holds altitude, and Down/S descends somewhat faster. `HangGliderVisualController` maps that flight to five deployed presentation states: Hover intentionally uses the front-on wing with the miner facing the camera; Float, Dive, Glide Left, and Glide Right use side-profile art, with the authored right-facing bank mirrored for left travel. Every deployed state applies a subtle top-wing flex/flap, and stowing restores the normal miner pose. Grounded Interact remains available to chests and doors and never pre-arms the glider. Selecting a button already assigned elsewhere swaps the two paths to prevent duplicate actions. **Restore Defaults** removes only the current controller-model profile. `GameProgress.RestartAfterGameOver` must not erase controller preferences.
- Left stick/D-pad movement, all keyboard keys, and UI navigation/A-submit remain fixed so the mapping screen and Game Over can never become unreachable. Do not feed gameplay binding overrides into `InputSystemUIInputModule`.
- `MineShopController` owns the overview-only `MINER` / ten-direction playtest easter egg. Ignore it away from the Levels page and during rebinding. Direction steps require a neutral release so one held stick direction cannot fill the sequence; no face button may be part of the controller code because fixed UI Submit could launch a level early. `PlaytestCheatController` separately recognizes `HEALTH` and `LIFE` only while a MINER sandbox level is active; it suppresses the leading `H` potion press so the health command cannot spend an item.
- A Logitech F310 is most predictable in **XInput/X mode**, where Unity exposes semantic A/B/X/Y, Start, and Back defaults. Other supported `Gamepad`/`Joystick` layouts can be captured on the Controls page; keep X mode as the recommended test configuration.
- Overview, Game Over, and every level pause menu use exactly one `EventSystem` with `InputSystemUIInputModule` and assigned actions. Do not restore `StandaloneInputModule`; stick and D-pad navigation, A submit, and reliable initial selection depend on the Input System module.
- Opening pause selects **Resume**. Overview and Game Over provide an initial controller-selected button so the menus work without a mouse.
- `MineControlHintDisplay` keeps the in-level HUD and pause overlay synchronized with the active controller profile. Chest and door proximity prompts query the current Interact display string instead of serializing `X`.
- `MineLevelMenuController` owns level pause and modal-shop state. Mapped Pause or Escape/`P` toggles its pause panel; mapped Shop or Backspace toggles `MidLevelShopController`. Either overlay sets `Time.timeScale` to zero, they remain mutually exclusive, and disabling the controller must restore normal time.
- Buying from `MidLevelShopController` uses the same persisted economy as the overview without unloading the level. Its explicit **Return to Overview** action restores `Time.timeScale` to 1, requests the overview Shop page through the one-shot `OverviewArrival` state, and loads the appropriate overview without calling `GameProgress.CompleteLevel`, consuming a life, or resetting progress. Pause and shop actions are disabled after either the exit-door completion sequence or a death/respawn sequence begins. Potion, chest, and door actions also reject a dead or respawning player. An overview entered with zero lives redirects to `GameOver`.

### Reusable hero animation and outfits

- Keep one persistent hero identity across every dungeon. The face, proportions, gameplay scale, collider alignment, animation cadence, and attachment-point names must remain compatible between outfits.
- Represent the Bronze Miner, construction worker, astronaut, and future themes as swappable outfit profiles rather than separate player prefabs.
- Every outfit profile supplies clips or frame sets for side walk, side run, side jump/rise, apex, fall, land, front-facing walk toward the camera, and back-facing walk away from the camera.
- The six side idle/jump cells in animation-sheet row 2 use zero-based frame order `0 idle`, `1 grounded squat`, `2 rise`, `3 apex`, `4 fall`, and `5 land`. Frame 1 is anticipation before physics takeoff and must never be selected as the high-velocity airborne pose.
- Side art may mirror for left/right. Outfit-specific asymmetry and optional hand-held tools may provide dedicated direction variants when mirroring would be visibly incorrect.
- Keep the optional tool attachment point and profile field available for future equipment. When a later outfit supplies a hand tool, its rig must inherit facing and animation motion without drifting from the hand through squat, rise, apex, fall, and landing; a null tool must produce no accessory object or validator failure.
- Gameplay movement gives the queued grounded-squat state priority, then selects side locomotion from horizontal speed and grounded/vertical velocity state. Positive takeoff velocity selects the rise pose even while the ground-check volume briefly overlaps the platform, preventing an idle-frame flash between squat and rise. Door entry explicitly selects the back-facing walk-away state; entrances or reveals may select front-facing walk-toward-camera.
- While the glider is deployed, its visual controller temporarily owns the flight pose: Hover selects the front-facing/toward-camera miner, Float and directional glide use the side apex pose, and Dive uses the side fall pose. Stowing or resetting the glider clears that override and returns pose selection to ordinary movement.
- Outfit changes must not alter movement statistics, collision, health, level reachability, or saved hero identity unless a future design explicitly defines an equipment effect.

## Currency, keys, and chests

- Green crystals are the stored shop currency and save immediately.
- Green crystals are worth 1, blue crystals are worth 5, and purple crystals are worth 20.
- The shop sells a potion for 3 and an extra life for 25. Permanent heart upgrades are planned but their price is not yet fixed.
- Bronze levels contain one level-specific bronze key and one chest. Silver levels may contain multiple keys and multiple chests.
- Keys are counted and scoped by dungeon ID and level number. Each collectible and chest also has a unique persistent ID, preventing scene-name collisions between Bronze Level 1 and Silver Level 1.
- Opening a chest atomically consumes one key from that dungeon/level inventory, grants the reward once, records the chest as open, and leaves its animated open state visible. A key from another level or dungeon cannot open it.
- A chest is a one-time reward per save, preventing replay farming.
- Entering chest range never claims a reward automatically. A keyed player must press mapped Interact, Up, or `W` while the HUD shows the current Interact binding plus the fixed keyboard alternatives.
- Locked chests report that a same-dungeon, same-level key is required. Persisted claimed chests retain an enabled proximity trigger, use the final open/empty animation frame, and report `CHEST ALREADY OPENED` on replay.
- Already-collected keys remain hidden when their level is replayed, and the run-status HUD shows the current counted key inventory and refreshes from saved key/chest state when the scene starts.
- Chest rewards are 50% blue-crystal value (+5 currency), 45% one potion, and 5% one extra life.
- The one silver key is hidden on a difficult optional path in Level 10 and persists globally for the Level 11 gate.

## Bronze Mines level matrix

Dungeon 1 is the Bronze Mines. Each of its twelve overview tunnels is a level. Dungeon 2 is the Silver Mines; do not shift Bronze Mines wall/platform material to silver within Levels 1-12.

| Level | Scene | Direction | Required implementation |
|---:|---|---|---|
| 1 | `Level1_TheMines.unity` | Vertical | Tutorial climb with stationary landings and supported exit. |
| 2 | `Level2_SlidingAscent.unity` | Angled hybrid | Varied horizontal-surfaced platforms rise diagonally over a parallel low-friction ramp with upward-facing one-heart spikes. |
| 3 | `Level3_ChasmRun.unity` | Descent | Introductory hang-glider chute with alternating hazards, four safe-lane gems, a safe landing, and a short exit tunnel. |
| 4 | `Level4_CopperColumn.unity` | Vertical | Taller climb and increasing hazard pressure. |
| 5 | `Level5_CrookedIncline.unity` | Angled | Longer diagonal ascent and slide risk. |
| 6 | `Level6_BrokenRail.unity` | Horizontal | Harder bottomless-pit crossings. |
| 7 | `Level7_FurnaceRise.unity` | Vertical | Extended endurance climb with combined hazards. |
| 8 | `Level8_RazorAscent.unity` | Angled | Tighter diagonal spike timing. |
| 9 | `Level9_AbyssRun.unity` | Horizontal | Most difficult horizontal pit route. |
| 10 | `Level10_KeyVault.unity` | Vertical | Long climb plus hard optional silver-key route. |
| 11 | `Level11_TreasureVein.unity` | Angled | Silver-key-gated treasure tunnel with exceptionally difficult optional reward routes. |
| 12 | `Level12_DeepworksGauntlet.unity` | Mixed | Very long seeded twelve-section gauntlet combining three vertical climbs, three diagonal climbs, three horizontal pit runs, and three hang-glider descents. |

Difficulty and length increase through the sequence. Level 3 deliberately introduces hang-glider descent before Levels 4-11 resume the vertical, angled, horizontal progression; Level 12 is the mixed-direction capstone. Level 2's individual upper surfaces remain horizontal while their centers, dedicated art, and parallel recovery ramp form a diagonal ascent.

## Silver Mines — Level 1: Silver Lode

`Assets/Scenes/SilverDungeonOverview.unity` currently exposes the first Silver Mines test scene, Level 1: **Silver Lode**, at `Assets/Scenes/SilverLevel1_SilverLode.unity`. Its route follows the supplied map from the lower-left entrance, climbs the left shaft, crosses the first broad chute, traverses the central ledges, climbs the right side, descends the terminal chute, and reaches the lower-right exit. Double-headed map arrows become moving platforms or hazards along the indicated axis.

- Two major chute zones tune camera framing, but the hang glider remains deployable during any airborne section of the level.
- The front-on glider image is intentionally the Hover state, where the miner faces the camera. Neutral Float, Dive, and left/right travel switch to side-profile art; left travel mirrors the authored right-facing bank, and the top wing flexes/flaps subtly in every deployed state.
- Several independently persisted Silver keys and metal-bound reward chests exercise counted key inventory and one-key-per-chest consumption.
- A rock-matching non-solid fake wall hides the optional blue/purple gem room. `FakeWallReveal` preserves the stone silhouette at rest and fades it only while the miner passes through.
- Doors visibly open before entrance/exit traversal begins. Chests play their lock/open sequence and remain open after their persistent reward is claimed.
- Silver rock masses use the shared stone fill plus repeated edge pieces and rotated/mirrored corner caps. `ThemedMetalFlakes` reads the Silver theme rather than baking silver into the geometry, so a dungeon's `DungeonVisualTheme` asset is the single place to change flake color, highlight, density, and deterministic seed.
- The polished cut-gem render is authored at 60% of the former gameplay size. Bronze spike art is authored at 50%, uses visible-tooth damage geometry, and receives a timed glint through `SpriteShineAnimator`.

The Silver scene is the current art/gameplay proving ground. Reuse its shared tile and theme system in Bronze, but do not revise the existing Bronze level layouts until the Silver test has been reviewed.

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

Levels 6 and 9, along with Level 12's horizontal sections, place separated horizontal platforms above localized lethal pits. Use `FatalFallZone`, not `DamageZone`, so a pit immediately routes through the normal death/life system even if ordinary hit invulnerability is active. Do not reuse Level 2's nonlethal reset behavior for these tunnels.

Level 3 instead has a dedicated hang-glider chute contract: one deep shaft, a silent launch trigger, four gem-marked safe lanes, a safe landing, and a two-step exit tunnel. Its abyss remains well below the landing route.

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
- Horizontal sections use visible platform gaps with localized `FatalFallZone` triggers contained beneath those gaps. Add one safely low global `FatalFallZone` below the complete mixed route so an off-route fall cannot survive through damage invulnerability or reach unintended lower geometry.
- When a horizontal section feeds a descent, omit an ambiguous final local pit and bridge to a broad launch shelf. Place the shaft opening beyond the shelf's right edge so the player can approach, stop, read the drop, and intentionally step off.
- Chute launch areas are silent: do not create tutorial panels, signs, or status-message instructions. The overview Controls page remains the control reference.
- `ParachuteDescentController` owns globally available hang-glider physics; the descent zones only contribute camera/framing context. Grounded approaches retain ordinary Interact, deployment is accepted only while airborne, and landing or reset stows the glider.
- Place at least four green gems through the alternating safe lane of every Level 3 and Level 12 descent.
- Downward hazards include camouflaged wall spikes and moving hazards. Hidden spikes must reveal and warn before becoming damaging, leave a viable dodge lane, and remove exactly one heart per hit.
- Initialize the mixed camera at the miner. Outside descent mode use a vertical dead zone with smoothed velocity look-ahead; during an active airborne descent, blend rather than snap toward the shaft center and glider-visible downward offset. Do not switch framing merely because the trigger is entered or ordinary jump velocity crosses a threshold.
- End every chute trigger above the landing stance and any outgoing ledge. For a nonterminal descent, keep the lower-right exit clear by ending its visible/collision wall above the first two outgoing ledges and placing the final fixed spike on the left wall.
- Compose Level 12 from direction-specific modular background tiles at uniform scale, with enough overlap and horizontal/vertical overscan that no camera position exposes black outside the art. Downward sections use a dedicated cool-dark layer rendered above adjacent section backdrops and visible bronze-veined rock faces aligned to every shaft-wall collider. Its diagonal sections use the same dedicated, unrotated diagonal-mine artwork as the other angled Bronze Mines levels. Dim background-only structural detail so it does not read as collision.

## Build and validation

Generate scenes and assets with **Jump > Level Tools > Build Mines Levels**. Validate them with **Jump > Level Tools > Validate Mines Levels**.

Build Settings order:

1. `DungeonOverview.unity`
2. `GameOver.unity`
3. `Level1_TheMines.unity`
4. `Level2_SlidingAscent.unity`
5. Levels 3-12 in numeric order
6. `SilverDungeonOverview.unity`
7. `SilverLevel1_SilverLode.unity`

Validation should fail if any of the following contracts are broken:

- The overview has no active camera or does not have twelve level nodes.
- A scene or level node is missing from progression order.
- A Bronze level lacks its bronze key/chest pair, or any level lacks exactly one supported entrance, supported exit, player, camera, or automated route.
- An entrance is unsupported, misaligned with the authored start, omits `LevelEntranceDoor`, fails to use the front-facing walk-out state, or does not restore normal collider, physics, input, and checkpoint state after the intro.
- An exit completes on contact, lacks an enabled mapped-Interact/Up/W proximity trigger, omits the dynamic exit prompt, or bypasses the visible walk-in sequence.
- A chest opens merely from contact, lacks an enabled mapped-Interact/Up/W proximity trigger, has no distinct open-state sprite, or silently ignores a replayed already-claimed state.
- The controller action asset loses any of its six stable defaults/GUIDs, `MineInput` stops loading per-model overrides, movement omits either the left stick or left D-pad, meaningful activity cannot select among attached devices, a noisy duplicate joystick steals control from an active semantic gamepad, or a generic DirectInput controller has no usable button defaults.
- The overview lacks distinct Levels/Shop/Controls pages, six wired mapping rows, active-controller/status labels, per-model help, conflict-safe capture, or Restore Defaults.
- Gameplay overrides alter keyboard controls or `InputSystemUIInputModule`, duplicate two actions instead of swapping, survive Restore Defaults, or are erased by Game Over Restart.
- A level lacks its initially hidden `MineLevelMenuController` pause panel and `MidLevelShopController` shop panel, dynamic `MineControlHintDisplay`, Resume action, shop-close action, or explicit Return-to-Overview action.
- An overview, Game Over, or level UI scene lacks its single action-backed `InputSystemUIInputModule`, uses `StandaloneInputModule`, or omits the required initial controller selection.
- Ordinary platform overlap provides less than `hero collider height + 0.75` headroom without an explicit, documented `Intentional Head-Bump Challenge` marker.
- Level 11 can unlock without both prerequisites.
- Level 12 can unlock before Level 11 is completed.
- Level 2's upper centers do not rise diagonally, widths and two-axis gaps do not vary, or the 18-degree low-friction ramp is not parallel beneath the route.
- Any Level 2 ramp spike points sideways instead of upward, or the no-input slide does not reach the retry bottom.
- Any spike uses a rectangular damage collider, differs from the three inset triangle paths, misses a visible tooth center, or overlaps either transparent valley after its authored transform.
- Levels 6 or 9 lack `FatalFallZone` pit zones, Level 12's localized fatal triggers extend outside their visible horizontal gaps, or its global off-route abyss is missing.
- Level 3 lacks its deep hang-glider chute trigger, silent launch, four-gem safe-lane trail, safe landing, or two-step exit tunnel.
- Final-life depletion bypasses `GameOver.unity`, Restart fails to restore three lives, or any crystals, potions, upgrades, unlocks, keys, or opened-chest state survives Restart.
- Level 10 lacks the silver key.
- A Level 10 required landing is not marked `UsePowerJump`, or a same-height spike leaves less than 0.25 world units of horizontal landing clearance after hero and spike extents are included.
- Level 11 does not have exactly five blue value-5 crystals and one purple value-20 crystal.
- Level 12 does not contain exactly three sections of each required direction, a reproducible seeded order, silent launch shelves, gem-marked descent lanes, independent Jump throughout chute triggers, airborne-only descent activation, smoothly blended shaft framing, modular overscan background coverage, airborne route coverage, or fair-reveal one-heart hazards with viable dodge lanes.
- Silver Level 1 lacks its two chute zones, multiple scoped keys/chests, fake-wall gem room, lower-left-to-lower-right route, animated entrance/exit doors, shared edge-aware rock set, Silver theme, or theme-driven silver flakes.
- Any hero lacks the hidden-until-deployed Hover, Float, Dive, Glide Left, and Glide Right presentation, uses the front-on Hover image for side travel, fails to mirror directional bank art, omits the miner-facing override, or has no visible top-wing flex/flap.
- Shop prices, heart count, starting lives, damage, or potion healing differ from the design values.
- Ordinary walk/run tuning differs from 7.5/9, ordinary/power jump tuning differs from 12/.24 and 14.75/.26, a directional Run+Jump commitment is downgraded during anticipation, or a grounded jump does not preserve the approximately 0.08-second squat before its upward impulse.
- The side jump cells do not follow the documented row-2 order, or a quick tap can be discarded before takeoff.
- The hero collider has friction that allows sustained wall/ledge sticking, the HUD uses an unsupported empty-heart glyph, the Bronze Miner renders a pickaxe, or an absent optional tool breaks the outfit contract.
- An angled Bronze Mines level or Level 12 angled section rotates the ordinary backdrop instead of using the dedicated diagonal-mine artwork in its authored orientation, or a Level 12 background module is non-uniformly stretched or leaves a camera-visible void.

## Automated playtest

Regenerate all authored scenes and visual-theme assets with `MineLevelBuilder`, then run structural validation before route testing. Use the editor version named in Project conventions and close other Unity instances that have the project open.

```text
-batchmode -nographics -quit -projectPath <project> -executeMethod MineLevelBuilder.Build -logFile <build-log>
-batchmode -nographics -quit -projectPath <project> -executeMethod MineLevelValidator.Validate -logFile <validation-log>
```

The builder has successfully generated the current Silver overview/level and theme assets. Validator and automated playtest results must be recorded separately; a successful build is not evidence that either later stage passed.

### Current automated verification record — July 19, 2026

The following reports were produced from the final regenerated scenes and current scripts. Human visual approval of the Silver art and hang-glider animation remains pending.

- `Logs/SilverFinalValidation.log` records a structural pass covering Bronze Levels 1-12 plus the available Silver content, including seven-heart health, counted keys/chests, global hang-glider mechanics, the shop, themed visuals, hazards, and route contracts.
- `Logs/SilverFinalMechanicsSmokeTest.json` records every smoke check passing, including scoped counted inventory, chest and door interaction, mid-level shop behavior, MINER-only `HEALTH`/`LIFE`, global glider input, exact art selection for all five deployed visual states, front/side miner poses, left/right mirroring, grip-anchor continuity, wing flex, and clean stowing.
- `Logs/SilverFinalShopPlaytest.json` records the modal shop opening and closing in the final 49-waypoint scene without unloading or resetting the level; the miner remains at seven health with no respawn.
- `Logs/SilverFinalFullRoutePlaytest.json` records a 49-of-49 waypoint pass through `SilverLevel1_SilverLode.unity`, reaches the configured exit with no respawn, and reports `globalGliderObserved`, `routeGliderHoverExercised`, and `routeGliderDiveExercised` as true.

The virtual controller drives movement through `HeroMovement`, follows ordered `AutomatedPlaytestWaypoint` objects, and explicitly activates the exit after reaching its proximity trigger while grounded. A waypoint's `UsePowerJump` flag commits Run+Jump for that required launch; `-playtestPowerRun` forces the same behavior for every eligible route jump. Required routes should remain deterministic enough for unattended verification while optional key and treasure routes are validated separately. Level 12 additionally uses airborne-pass waypoints and automated hang-glider deployment so all three descent corridors are exercised through their intended safe lanes.

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
- `-playtestStartAfterWaypoint <order>` places the miner on that grounded waypoint and begins with the next route goal. This test-only shortcut isolates later regressions without changing the playable scene. For Level 12, starting after waypoint 12 and passing after waypoint 13 isolates the landing-to-breakout run-jump; running from the scene start with power-run enabled and passing after waypoint 13 covers the opening route, first chute, landing, and corrected flat transition shelf.
- `-playtestReturnHome` skips traversal after the initial grounded spawn, first enters the zero-time-scale pause state, and then invokes `MineLevelMenuController.ReturnToOverview`. It passes only if `DungeonOverview` loads with the Shop page visible, `Time.timeScale` restored to 1, and unlock, crystal, life, and potion values unchanged.
- `-playtestTraceFirstJump` logs the first launch's position, velocity, grounded/preparation state, and target at short intervals for diagnosing ledge-side collisions. The route driver begins its squat about two world units before a support edge, accounting for horizontal travel during the anticipation pose.

The controller waits for the spawn landing, runs at normal gameplay time, and normally accepts only the exit door's configured destination as successful completion; Game Over or any other unexpected scene transition is a failure. Run Level 10 with `-playtestPowerRun -playtestFailOnDamage -playtestFailOnRespawn` to cover its required power-jump route and spike-clear landing centers.

The mechanics smoke test can be run with `-executeMethod MineMechanicsSmokeTestCommand.Run -mineMechanicsSmokeTest -mechanicsReport <report-file>`. It observes the jump transition itself: immediately after a grounded press, the miner remains grounded with approximately zero upward velocity and displays `bronze_miner_2_1`; after roughly 0.08 seconds, upward velocity begins and the visible body advances directly to `bronze_miner_2_2`, without returning to `bronze_miner_2_0`. Coverage includes a quick tap that still launches, a held ordinary jump, 7.5 walk versus 9 run speed and their animation rows, and a committed directional 14.75/.26 power jump that exceeds the ordinary arc and travels horizontally. It also drives the hang glider through Hover, Float, Dive, Glide Left, and Glide Right; verifies the intentional front-hover and side-flight miner poses, mirrored left bank, animated wing flex, and complete stow reset; and checks the corresponding hover/glide/faster-descent physics. Route playtests must retain enough observation time for the anticipation delay.

Input and menu smoke coverage verifies the six stable default binding paths plus the runtime three-tooth collider shape, toggles `MineLevelMenuController` into and out of a zero-time-scale pause with the panel state synchronized, confirms its home target is `DungeonOverview`, and proves death/respawn rejects pause, retreat, and potion consumption while still consuming exactly one life. Scene validation independently checks all Controls-page rows/listeners, stable action GUIDs, dynamic HUD/prompt components, and both transparent spike valleys. The separate `-playtestReturnHome` scene-transition coverage returns from the paused state and proves retreat opens the Shop, restores normal time, preserves progress, and does not complete the abandoned level or spend a life.

The same mechanics smoke test enters both Foreman's Master Key codes, confirms all Levels 1-12 open without changing the saved highest-level or silver-key gates, exercises economy/key/chest/completion mutations inside a test-run sandbox, and proves both overview return and test-run Game Over discard every sandbox mutation while leaving the session master key usable.

Chest smoke coverage verifies that contact alone does not open a chest, a mapped-Interact/Up/W interaction without the key is rejected, a keyed interaction grants exactly one deterministic reward, a second interaction cannot duplicate it, and replay state restores the open sprite, prompt trigger, hidden collected key, and `CHEST ALREADY OPENED` feedback.

Entrance validation verifies exactly one supported start door per level, its alignment with the authored gameplay start, and a sufficiently long front-facing walk-out transition. Door smoke coverage verifies that contact alone leaves the exit unused, proximity displays the current Interact binding plus Up/W, and an explicit grounded mapped-Interact/Up/W interaction starts the existing kinematic walk-through transition. The full virtual-controller traversal then verifies that this explicit activation reaches only the configured overview scene.

The unattended command requires other Unity instances using the project to be closed. A timeout means the controller failed to execute the authored route or the door transition did not complete.

## Reusable mechanics

- `DamageZone` handles ordinary heart-damage hazards such as spikes. It is not the bottomless-pit implementation.
- `FatalFallZone` calls the player's fatal-fall path on trigger enter/stay, bypassing ordinary hit invulnerability and routing directly through life, respawn, and Game Over handling. Use it for every bottomless gap and the safely low Level 12 off-route abyss.
- `LevelEntranceDoor` owns the automatic supported-door arrival: it locks input, opens the door, disables normal collision/physics during the short front-facing walk-out, closes the door, and restores the authored gameplay position and checkpoint afterward. `LevelExitDoor` likewise waits for `MineDoorAnimator` to open before its back-facing walk-through begins.
- `SpikeHitboxGeometry` authors the only valid Bronze spike trigger: three disconnected inset triangular paths, with no damaging base strip or valley air.
- `FallingSpike` warns, falls when the player passes below, and resets.
- `MovingPlatform` supports horizontal or vertical travel with pauses.
- `WeightedBreakablePlatform` spends durability according to apparent weight.
- `PlayerWeight` is the inventory and power-up integration point.
- The Level 2 reset zone returns a fallen miner to the start without consuming a life.
- Descent zones never suppress Jump and now tune camera/framing only. A discrete Interact press toggles the hang glider anywhere while the miner is airborne; landing, respawn, or door traversal stows it. With the glider open, Up/W holds altitude and selects the front-facing Hover presentation, neutral input provides the normal side-profile Float or directional bank, and Down/S selects the side-profile Dive plus the moderately faster descent rate. `HangGliderVisualController` mirrors the bank for left travel and adds state-specific top-wing flex without changing flight physics.
- Fair-reveal hidden hazards expose a warning before enabling one-heart damage; moving descent hazards preserve a reachable safe lane.
- Bronze-key, chest, silver-key, crystal, and completion state must use persistent progression data rather than scene-only state.

Apparent weight is `(base + carried) x weight multiplier x gravity multiplier`.

## Art acceptance checks

### Shared generated art and theme sources

- Neutral cave fill: `Assets/Art/Silver/SharedCaveRockTile.png`
- Whole-rock exposed edge and corner cap: `Assets/Art/Silver/RockEdgeTile.png`, `Assets/Art/Silver/RockCornerTile.png`
- Cut gem, bronze spikes, keyed chest pair, key, and open mine door: `Assets/Art/Silver/TintableCutGem.png`, `PolishedBronzeSpikes.png`, `SilverBoundChestClosed.png`, `SilverBoundChestOpen.png`, `SilverChestKey.png`, and `MineDoorOpen.png`
- Hang-glider states: `Assets/Art/Silver/MinerHangGlider.png` is the intentional front-on Hover image; `HangGliderFloatRight.png`, `HangGliderDiveRight.png`, and `HangGliderBankRight.png` provide the side-profile Float, Dive, and directional bank art, with the bank mirrored for left travel.
- Theme assets: `Assets/Art/Generated/BronzeDungeonTheme.asset` and `Assets/Art/Silver/SilverDungeonTheme.asset`
- Runtime sources: `Assets/Scripts/DungeonVisualTheme.cs`, `ThemedMetalFlakes.cs`, `SpriteShineAnimator.cs`, `MineDoorAnimator.cs`, `FakeWallReveal.cs`, `MidLevelShopController.cs`, `PlaytestCheatController.cs`, and `HangGliderVisualController.cs`; scene/theme generation remains in `Assets/Editor/MineLevelBuilder.cs`.

The shared bitmap sources supply shape and texture. Do not recolor or clone rock sheets just to change ore: edit the appropriate `DungeonVisualTheme` palette/density and let `ThemedMetalFlakes` generate dungeon-specific inclusions.

- Platform and wall silhouettes use whole-rock exposed edges and corner caps, never a fill texture abruptly cut at the collider boundary.
- Bronze and Silver variants share the same rock construction while their theme-driven metal flakes read clearly as bronze or silver.
- Gems read as faceted cut gemstones with a moving highlight at 60% scale; bronze spikes read as polished metal with a moving shine at 50% scale.
- Chests have visible metal binding and a clear front keyhole, animate only after a valid key is consumed, and hold the open frame afterward.
- The hang glider's front-on image is used only for Hover, with its broad wing above the miner, its control bar below, and the miner facing the camera. Float, Dive, Glide Left, and Glide Right use readable side-profile art; left travel mirrors the authored right bank. The top wing flexes/flaps subtly in every deployed state without clipping the miner or obscuring nearby hazards, and stowing hides the glider and clears the flight-pose override.
- Platform collision matches the visibly thinner top surface.
- Spike collision stays inside visible teeth on upright, rotated, wall-mounted, and non-uniformly scaled hazards; transparent valleys never damage.
- The miner sprite has an integrated silver helmet and yellow lamp at gameplay scale.
- The Bronze Miner has no visible pickaxe, while the optional hand-tool attachment remains available for future profiles.
- The anticipation squat is visibly held before the miner leaves the ground, transitions directly into the rise pose, and never appears suspended in midair.
- Door entry keeps the miner visible until the character passes into the doorway.
- Every entrance keeps the miner visible while the front-facing walk cycle emerges from the supported doorway, with no controllable or collidable half-transition frame.
- Levels 2, 5, 8, and 11 use dedicated unrotated diagonal-mine artwork, and Level 12 changes background composition with uniformly scaled modular tiles for each route section. Camera overscan never reveals missing art, and decorative background beams do not masquerade as solid obstacles.
- Empty heart slots read as dim filled hearts rather than missing-font squares.
- Sustained movement into a wall or platform edge does not leave the miner friction-locked in place.
- Side locomotion has distinct walk, run, rise, apex, fall, and land states with readable transitions.
- Front-facing and back-facing walk cycles retain the same recognizable face and proportions as the side view.
- Swapping between Bronze Miner, construction worker, and astronaut profiles changes clothing and equipment without changing hero identity, gameplay scale, collider alignment, or attachment names.
- Door entry uses the back-facing walk-away cycle, and any future separately rigged tool remains aligned throughout every supported state.
