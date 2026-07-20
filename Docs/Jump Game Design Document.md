# Jump

## Game Design Document

**Status:** Bronze production design and Silver Mines Level 1: Silver Lode proving build
**Version:** 0.9
**Last updated:** July 19, 2026
**Working title:** Jump

This document is the current design authority for the playable Bronze Mines chapter and Silver Mines Level 1: **Silver Lode**. Later-dungeon ideas remain provisional unless they are explicitly marked confirmed.

---

## 1. Game vision

Jump is a platforming game in which a little miner travels through increasingly difficult mine dungeons, collects valuable crystals, survives environmental hazards, opens hidden-key chests, and spends rewards on additional survival resources.

### Intended player experience

- Movement is easy to learn and satisfying to master.
- Each level teaches or escalates a readable traversal challenge.
- Difficulty rises through longer routes, tighter timing, hazards, and optional reward paths rather than repetition alone.
- Valuable collectibles tempt the player into clearly optional risks.
- A failed attempt teaches the player something and makes another attempt feel worthwhile.

### Project vocabulary

- **Dungeon:** A themed world or chapter. Dungeon 1 is the Bronze Mines; Dungeon 2 is the Silver Mines.
- **World overview:** The level-selection scene for a dungeon. Every visible mineshaft represents one playable level.
- **Level:** One playable mine tunnel within a dungeon.
- **Crystal:** The collectible currency. Crystal colors identify their green-crystal value.
- **Life:** One complete level attempt. A new save begins with three lives.
- **Heart:** One point of health during a level. The base maximum is seven hearts.
- **Dungeon key:** A counted, persisted collectible scoped to one dungeon and level. Opening a chest in that scope consumes one key.
- **Bronze key:** The single dungeon key used by an existing Bronze level's reward chest.
- **Silver key:** A special progression key hidden on a difficult route in Level 10 and required to enter Level 11.

---

## 2. Core game flow

1. Start at the main screen or dungeon overview.
2. Choose one of the twelve mineshafts on the Bronze Mines overview.
3. Enter an unlocked level through its supported start door and watch the miner walk out toward the camera before control is enabled.
4. Traverse the tunnel, avoid hazards, and collect optional crystals and keys.
5. Optionally use the level's bronze key to open its reward chest.
6. Reach the supported exit door and press the mapped Interact button (default `X`), Up Arrow, or `W`.
7. Watch the miner visibly walk through the door, complete the level, and return to the overview.
8. Spend crystals in the overview shop, adjust controller mappings on the Controls page, or continue to the next unlocked shaft.

Normal levels unlock sequentially. Level 11 requires both completion of Level 10 and possession of the silver key. Completing Level 11 unlocks Level 12.

During an active level, the player may press mapped Shop (default Back) or Backspace to open a modal shop without unloading the level. Time pauses and the miner, hazards, camera, and collectible state remain exactly where they were. Closing the shop resumes in place; only its explicit **Return to Overview** button abandons the attempt. Pause, shop, potion, chest, and door actions are locked once a death/respawn transition begins so the final-life Game Over flow cannot be bypassed.

### Controls

| Action | Default controller / fixed keyboard input | Notes |
|---|---|---|
| Move | Controller left stick or D-pad; Arrow keys or `A` / `D` | Both Logitech movement controls are supported. Base horizontal speed is 7.5 units per second. |
| Run | Controller `A`; Left or Right Shift | The controller button is remappable. Hold while moving to run at 9 units per second. |
| Jump | Controller `B`; Space | The controller button is remappable. A grounded press shows an approximately 0.08-second squat before the ordinary force-12 impulse; holding after takeoff extends height for up to 0.24 seconds. |
| Power jump | Hold a direction + mapped Run + Jump; hold a direction + Shift + Space | A directional run-jump commits at jump press, uses force 14.75, and supports a 0.26-second held-jump window. |
| Hang glider | Press mapped Interact (`X` by default) while airborne anywhere | Press once to deploy the smaller, correctly oriented glider and again to stow it. Neutral input glides, Up/W holds altitude, and Down/S descends somewhat faster. Hover intentionally uses the front-on wing with the miner facing the camera; Float, Dive, and left/right travel use side-profile art with left travel mirrored from the authored right bank. The top wing flexes/flaps subtly in every deployed state. Jump remains independent. |
| Open chest | Controller `X`; Up Arrow or `W` | Interact is remappable. Works only while standing in a chest's interaction range; contact alone never claims the reward. |
| Enter exit door | Controller `X`; Up Arrow or `W` | Interact is remappable. Works while grounded in the exit door's interaction range; contact alone only displays the exit prompt. |
| Use health potion | Controller `Y`; `H` | The controller button is remappable. Consumes one potion and restores one heart. |
| Pause / resume | Start; Escape or `P` | The controller button is remappable. Toggles the in-level pause overlay without changing progress. |
| Shop | Back; Backspace | The controller button is remappable. Opens or closes the in-level paused shop without moving or reloading the miner. |
| Open inventory | `E` | Reserved for the broader inventory interface. |

The overview has three pages: Levels, Shop, and Controls. The Controls page can remap Run, Jump, Interact/Hang Glider, Health Potion, Pause, and Shop. It displays the active controller and current assignments, waits for the button used to select a row to be released, captures the next controller button, saves a versioned profile per controller model, and offers **Restore Defaults** for that model. Assigning a button already used by another action swaps the two assignments so every action remains reachable. Controller movement, keyboard controls, and UI navigation/submit remain fixed; Game Over does not erase controller preferences.

A hidden playtest easter egg is available only on the Levels page. Typing `MINER`, or entering `Up, Up, Down, Down, Left, Right, Left, Right, Down, Up` with neutral releases on the left stick/D-pad, toggles the **Foreman's Master Key**. While active, all twelve Bronze Mines levels are selectable and the overview shows a persistent master-key banner. The key is session-only: it neither advances the saved highest level nor grants the silver key. Entering a tunnel starts an in-memory copy of progression so gems, lives, potions, upgrades, dungeon keys, chests, and exits can be tested normally; returning to the overview or restarting after a test Game Over discards that copy. In an active MINER sandbox level, typing `HEALTH` restores maximum health and typing `LIFE` adds ten lives. These commands do nothing in story play. Entering the master-key code again restores the normal story locks.

For the pictured Logitech F310, the rear input-mode switch's **X** position is recommended. XInput provides the stable `A`/`B`/`X`/`Y`, Start, and Back defaults shown above. Other controller layouts can use the mapping screen. The game binds both the left stick and D-pad to movement; the F310 Mode button may swap those two hardware controls, but either remains a supported movement source. When more than one USB controller interface is present, selection follows meaningful stick, D-pad, or button activity and prefers Unity's semantic `Gamepad` device over a simultaneously reported noisy generic `Joystick`; a generic DirectInput controller still receives usable Run, Jump, Interact, Potion, Pause, and Return defaults until remapped.

---

## 3. Player presentation and movement

### Miner character

- The playable character is a completely redrawn little miner, approximately 125% of the former character's size.
- The miner wears a dark mining outfit with leather and bronze details.
- A silver mining helmet is integrated into the character sprite and has a small yellow lamp on its front.
- The Bronze Miner carries no pickaxe. The uncluttered silhouette, clothing, and animation must match the detail and polish of the cave backgrounds.

### Persistent hero and outfit architecture

- The same recognizable hero identity, face, body proportions, and animation timing persist throughout the game.
- Dungeon themes change a swappable outfit profile rather than replacing the hero. Initial profiles include the Bronze Miner, a construction worker, and an astronaut.
- Each outfit supplies compatible directional sprite sets while preserving the same silhouette, attachment points, collider assumptions, and gameplay scale.
- The outfit system retains a nullable hand-tool attachment for future equipment without forcing a tool into the Bronze Miner profile. When a later profile supplies a tool, its separate hand-rigged visual follows the authored hand position through squat, rise, apex, fall, and landing.
- The required animation set is side walk, side run, side jump/rise, apex, fall, land, front-facing walk toward the camera, and back-facing walk away from the camera. The current sheet's zero-based row 2 is fixed as frame 0 idle, frame 1 grounded squat, frame 2 rise, frame 3 apex, frame 4 fall, and frame 5 land.
- Side animations mirror for left and right unless an outfit or tool requires dedicated directional art.
- Door-entry transitions use the back-facing walk-away animation so the hero visibly walks into the doorway. Every level intro uses the front-facing walk-toward-camera animation as the miner emerges from its start door before player control begins.
- The deployed hang glider temporarily overrides the miner pose: Hover uses the front-facing/toward-camera view, Float and directional glide use the side apex pose, and Dive uses the side fall pose. Stowing or resetting the glider clears this override and returns animation control to ordinary movement.

### Movement tuning

- Base side movement is 7.5 units per second, 75% of the original speed. A future speed power-up may restore or exceed the old speed.
- Holding mapped Run (default controller `A`) or either Shift key while moving raises horizontal speed to 9 units per second and selects the run animation.
- Gravity scale is 5.4, approximately 60% of the former value, so ascent and falling are readable beside the slower horizontal movement.
- An ordinary mapped Jump (default controller `B`) or Space jump uses force 12 and a 0.24-second held-jump window. This is slightly higher than the first slowed-jump pass while remaining proportionate to the 7.5 side speed.
- Pressing Jump while a direction and Run are already held commits a power jump before the anticipation squat. The committed jump uses force 14.75 and a 0.26-second held-jump window; releasing Run during the squat does not downgrade it to an ordinary jump.
- A valid grounded jump press first commits to an approximately 0.08-second anticipation squat; the upward impulse occurs only after that readable pose has appeared.
- Releasing the button during anticipation does not cancel the committed jump. A quick tap still receives the initial impulse, while continued input after takeoff controls additional height through the ordinary 0.24-second or committed-power 0.26-second held-jump window.
- The visual sequence transitions directly from grounded squat to rise. The standing pose must not flash after the press, and the squat must not appear for the first time after the miner is airborne.
- The hero's solid collider uses a zero-friction 2D physics material. Holding toward a wall, platform side, or ledge must not friction-lock the character; contacted slope materials may still provide their explicitly authored slide behavior.
- Movement values must be playtested against every required route. Level 10 deliberately requires the built-in run and power-jump controls, but normal completion must never require a purchased or collectible speed power-up.

---

## 4. Health, lives, damage, and failure

- The miner starts every level with seven full hearts, plus purchased permanent heart-capacity upgrades.
- Filled and missing heart slots use the same supported filled-heart glyph. Missing hearts are shown with a dim color or reduced opacity rather than an outline-heart character that the active font may render as an empty square.
- A spike hit removes exactly one heart and grants brief damage invulnerability.
- The miner may flash translucent during damage invulnerability, but must always return to full opacity afterward. Repeated damage, respawning, leaving a scene, and the door-entry fade cannot leave the character partially transparent.
- Spike damage geometry follows the visible teeth rather than a rectangular sprite bound. Each three-tooth Bronze Mines spike uses three inset triangular trigger paths; transparent valleys beside and between the teeth cannot remove health. Rotation and non-uniform scale must transform the art and hitbox together.
- A health potion restores exactly one missing heart and cannot exceed the current maximum.
- A new save begins with three lives. A life represents one complete attempt, so the starting balance provides three attempts rather than three respawns after the first attempt.
- Reaching zero hearts consumes one life. If another life remains, restart the level from its beginning.
- When the life balance reaches zero, open the dedicated Game Over screen instead of loading the overview. The screen must keep the failed state visible and offer a clear **Restart** button.
- If a zero-life state ever reaches the overview through stale or externally modified save data, redirect it to Game Over rather than allowing another shaft or Shop purchase.
- Restart clears the failed run and then returns to the Bronze Mines overview with three lives. Level unlocks, collected silver and bronze keys, opened chests, crystals, potions, and purchased heart upgrades all return to their new-save defaults.
- Falling into a bottomless pit is immediately lethal, consumes the current attempt, and follows the normal life/respawn flow. Fatal-fall handling bypasses ordinary one-heart damage invulnerability so a recently damaged miner cannot survive a pit or walk on unintended lower geometry.
- Level 2's reset ramp is not a bottomless pit. Falling from its upper route makes the player slide back to the ramp's bottom and restart the traversal without spending a life unless spikes reduce health to zero.

---

## 5. Universal level and art rules

### Doors and completion

- Every level starts at one supported entrance door aligned over a solid start platform. At scene load, control is locked while the door opens and the miner moves from inside the doorway to the authored gameplay start using the front-facing walk-toward-camera animation; the door closes and normal physics, collision, input, and the starting checkpoint are restored only after the intro completes.
- A start door is an arrival transition rather than an interactable exit. It must not complete, abandon, or reload the level when touched after the intro.
- The exit is reached by a grounded player at the end of the intended route.
- Touching the exit only displays the mapped Interact button plus `UP / W`; it does not complete the level, hide the miner, or teleport the character.
- Pressing mapped Interact (default controller `X`), Up Arrow, or `W` while grounded in the door's interaction range locks control, opens the door, and starts a short cutscene that visibly walks the miner into the doorway before the overview loads.
- Pressing mapped Shop (default Back) or Backspace opens the in-level shop without moving the miner. The shop's separate **Return to Overview** button abandons the level without playing the completion cutscene, unlocking the next level, or consuming a life; saved crystals, keys, opened chests, potions, lives, upgrades, and controller mappings remain intact.
- Every ordinary entrance and exit door must have a solid platform directly beneath it. A floating or unsupported door is allowed only when a level brief explicitly calls for one.

### Platforms

- Mine platforms and walls use a reusable rock fill, repeated whole-rock exposed edges, and rotated/mirrored corner caps. A silhouette must never look like rocks were sliced in half at an arbitrary texture boundary.
- Metal inclusions are generated from a dungeon theme rather than baked into separate rock geometry. Bronze uses bronze flakes and Silver uses silver flakes; changing the theme palette, density, or seed is sufficient to retheme shared walls and platforms.
- Rock faces need enough highlights, shadows, and chipped edges to match the surrounding environment.
- Platforms remain thinner vertically than the first prototype while preserving clear collision and safe footing.
- Across every level, ordinary stacked platforms need generous vertical headroom. Where their usable horizontal spans overlap, the default clear distance from the lower platform top to the upper platform underside is at least the standing hero collider height plus 0.75 world units.
- Apply the headroom rule to main routes and optional key, chest, and gem routes. A platform underside must not unexpectedly block the intended jump arc.
- Vertical shafts also stagger consecutive ledges laterally, and place each supported exit outward beside the final ledge so its foundation never blocks the last climbing jump.
- An intentionally tight head-bump challenge may use less clearance only when it is explicitly identified in that level's brief and authored object name. It must be visually readable, independently playtested, and unable to trap or invisibly damage the player.
- Later dungeons replace the vein material to match their identity; Dungeon 2 uses silver.

### Background and camera

- The world overview always contains an active rendering camera; it must never display a `No cameras rendering` message.
- The level camera follows the miner and frames upcoming landings and hazards.
- Background composition and support art follow each tunnel's direction: vertical shafts emphasize height, angled shafts rise diagonally, horizontal tunnels emphasize lateral depth, and downward shafts reveal the pit and upcoming dodge lanes.
- Levels 2, 5, 8, and 11 use dedicated diagonal-mine artwork in its authored, unrotated orientation. Level 12 uses that same dedicated art in each angled section. Rotating the ordinary vertical/horizontal backdrop is not an acceptable substitute.
- Long or mixed routes use modular, route-aware background pieces for horizontal tunnels, vertical climbs, angled climbs, descents, and transitions. Tiles preserve their authored aspect ratio and uniform scale instead of stretching one painting to fit a section, overlap outside the camera frame, and extend far enough left, right, and below the playable route that camera overscan never reveals an empty void.
- Background-only beams, rails, and rock silhouettes must be subdued enough that they do not read as collidable gameplay objects. Clearly defined shaft walls and bronze-veined foreground rock show the actual traversal boundaries.
- Player-supplied stick-figure route sketches are accepted as level-composition references. Translate each sketch into an explicit route/waypoint plan, direction changes, hazards, camera framing, collision walls, and modular background assignments before scene construction; do not force gameplay collision to follow incidental detail in a single scenic painting.

### Difficulty and fairness

- Present hazards before demanding a difficult reaction.
- Give the player a safe landing or readable recovery opportunity after demanding jumps.
- Optional rewards may be very difficult; the required completion route must remain achievable with default movement and no purchased item.
- Avoid blind jumps, unavoidable damage, and permanent traps.

---

## 6. Bronze Mines progression

Dungeon 1 is the **Bronze Mines** and contains twelve levels, one for each tunnel shown on its world overview. All twelve shafts are level-select nodes rather than decorative `coming soon` entrances.

Levels 1-11 principally alternate vertical, angled, and horizontal. Level 2 is classified as the angled entry in the cycle because its horizontal-surfaced platforms form a diagonally rising path with a parallel reset ramp. Level 12 deliberately breaks the cycle as a mixed-direction capstone.

| Level | Working name | Direction | Primary challenge |
|---:|---|---|---|
| 1 | Bronze Shaft | Vertical | Short tutorial climb, basic movement, camera tracking, and supported door entry. |
| 2 | Sliding Ascent | Angled hybrid | Varied horizontal-surfaced platforms rise diagonally over a parallel spike-covered reset ramp. |
| 3 | Chasm Drop | Descent | An introductory hang-glider chute with alternating safe lanes, hazards, a gem trail, and a short landing tunnel to the exit. |
| 4 | Copper Column | Vertical | A taller climb with less generous spacing and more hazards. |
| 5 | Crooked Incline | Angled | A longer diagonal ascent with increasingly demanding recovery. |
| 6 | Broken Rail | Horizontal | Longer pit crossings and more hazardous optional routes. |
| 7 | Furnace Rise | Vertical | An extended vertical endurance climb with combined hazards. |
| 8 | Razor Ascent | Angled | A long diagonal route with tighter spike timing. |
| 9 | Abyss Run | Horizontal | The hardest bottomless-pit tunnel before the key challenge. |
| 10 | Key Vault | Vertical | A long, difficult climb with required power-jump spike crossings and the hidden silver key on a hard optional path. |
| 11 | Treasure Vein | Angled | A silver-key-gated treasure tunnel with the chapter's hardest optional crystal routes. |
| 12 | Deepworks Gauntlet | Mixed | A very long seeded twelve-section capstone with three vertical climbs, three diagonal climbs, three horizontal pit runs, and three hang-glider descents. |

### Length and challenge curve

- Each level after Level 2 is longer and more difficult than the preceding level.
- Vertical levels increase climbing height and landing difficulty.
- Angled levels increase route length, slide risk, and spike combinations.
- Horizontal levels use separated platforms over bottomless pits; falling into a pit is lethal.
- Level 12 mixes every established direction and introduces controlled chute descents while the globally available hang glider remains optional during ordinary airborne traversal.
- Reused mechanics are combined only after the player has encountered them in a readable form.
- Main-route checkpoints may be added when playtesting shows that repeated early sections become tedious, but they must not bypass keys or one-time chest state.

### Level 1 - Bronze Shaft

- Teach slower movement, jumping, camera tracking, and visible exit entry on wide stationary landings.
- Keep the required route free of damage hazards.
- Hide the level's bronze key and place its reward chest where the system can be learned without blocking completion.

### Level 2 - Sliding Ascent

- Each required upper platform has a flat horizontal surface, but the sequence rises diagonally toward the exit.
- Platform widths vary, and successive jumps vary both their horizontal distance and vertical rise. The route must not read as equal-width ledges placed along one uniform step pattern.
- A continuous 18-degree ramp runs underneath and parallel to the overall rising path. Its downhill direction leads to the retry bottom/start.
- The ramp uses a dedicated zero- or near-zero-friction 2D physics material. The miner must not be able to perch on it and must slide reliably to the retry bottom when movement input is released.
- Falling through an upper gap lands the miner on the ramp. Gravity carries the miner to the bottom, forcing the upper-platform route to be attempted again.
- Spike groups sit on the ramp with their tips pointing upward in world space rather than rotating sideways with the slope. The miner must jump while sliding to avoid them; every hit costs one heart.
- The ramp is a recoverable reset route, not a bottomless death zone.
- Green crystals introduce collection and the overview shop.
- The cave background, rails, and visual flow are angled to match the ramp.

### Horizontal tunnel rule

Levels 6 and 9 contain bottomless pits beneath separated horizontal platforms, as do Level 12's horizontal sections. Pit edges must be visually obvious, camera framing must reveal the intended landing, and each lethal trigger must remain localized beneath its visible gap. Level 3 is instead an introductory hang-glider descent with gems tracing each safe lane and a safely low abyss beneath its landing route.

### Level 10 silver-key secret

- The one silver key is hidden in Level 10, which satisfies the requirement that it be hidden in another Bronze Mines level before Level 11.
- Reaching it is deliberately difficult and optional for ordinary Level 10 completion.
- Level 10's required route teaches and then requires directional power jumps: hold a direction and Run, then press Jump. Its spike crossings are balanced for the 9-unit run and committed force-14.75 jump rather than for an external speed upgrade.
- Every required Level 10 power-jump landing provides at least 0.25 world units of horizontal safe clearance between the accepted landing area and the nearest damaging spike boundary.
- Collection persists after leaving the level, including after a later death.
- The overview clearly distinguishes `complete Level 10` from `find the silver key` when explaining why Level 11 is locked.

### Level 11 - Treasure Vein

- Level 11 unlocks only after Level 10 is completed and the silver key has been collected.
- Its required route is the hardest conventional single-direction Bronze Mines route, but remains possible with default movement.
- It contains many green crystals plus exactly five blue crystals and one purple crystal.
- Each blue crystal is worth five green crystals.
- The single purple crystal is worth twenty green crystals and occupies the level's most difficult optional route.
- The five blue crystals are also placed on exceptionally difficult optional routes. They are not required to complete the level.

### Level 12 - Deepworks Gauntlet

- Completing Level 11 unlocks Level 12. There is no second key gate after the player has passed Level 11's silver-key requirement.
- The route is a very long twelve-section combination containing exactly three each of vertical-up, angled-up, horizontal, and vertical-down traversal.
- Section order is shuffled with a stable seed and saved into the built scene. It should feel random and varied while remaining identical across rebuilds, validation, and automated playtests.
- Horizontal sections retain localized bottomless pits under visible gaps. Their fatal-fall triggers never extend into another section or a required landing corridor, and a safely low global abyss catches any off-route fall before the miner can stand on unintended lower geometry.
- A horizontal-to-descent transition ends on a broad, visible launch shelf rather than an ambiguous pit. The shaft's clearly defined opening is beyond the shelf's right edge, with enough room to approach, stop, and intentionally step off.
- Chute launch areas do not display instructional panels, signs, or status-message tutorials. The existing Controls page remains the reference for mapped Interact/X and hang-glider steering.
- Chute triggers control descent-camera framing, not permission to deploy. Hang-glider physics begins only after the miner toggles it while airborne, can be used anywhere, and resets on landing; walking through a trigger does not consume Jump or deploy it.
- Green gems trace the alternating safe lane through every authored chute descent in Levels 3 and 12.
- Downward sections use camouflaged one-heart wall spikes and moving hazards. A hidden spike shows a visible crack, glint, or reveal warning before its damaging collider activates; every hazard arrangement leaves a reachable safe lane.
- The camera begins at the miner, uses a vertical dead zone and velocity look-ahead outside descents, and smoothly blends toward shaft-centered downward framing only during an active airborne descent. It must not bounce between thresholds at the approach or snap when the glider is deployed; framing retains the smaller wing while revealing hazards with sufficient reaction time.
- Every chute trigger ends above the landing stance and outgoing ledges. Nonterminal shafts provide a clearly illustrated lower-right breakout: the matching wall collider ends above the first two outgoing ledges, a flat transition shelf begins beyond the landing shelf and the miner's head-clearance corridor, and the final fixed spike stays on the opposite wall. The angled climb resumes after that safe run-jump landing.
- Direction-specific modular background pieces change with the route and retain uniform scale rather than being stretched across each section. Tiled overscan covers the camera left, right, and below every route. Downward sections use a cool-dark dedicated shaft layer above neighboring backdrops plus visible bronze-veined rock walls aligned with collision. Diagonal sections use the dedicated unrotated diagonal-mine art instead of a rotated ordinary backdrop.
- Level 12 still contains its own optional bronze key and one-time chest, neither of which is required for the exit.

---

## 7. Keys and reward chests

Every Bronze Mines level contains one hidden bronze key and one reward chest.

Silver levels may contain several keys and chests. `GameProgress` stores counted keys by dungeon ID and level number, while unique collectible/chest IDs persist exactly which objects were collected or opened. This prevents identically numbered levels in different dungeons from sharing state.

### Bronze key rules

- A bronze key belongs to the level in which it is found.
- It opens only that same level's chest.
- Collection is saved so leaving or dying does not recreate an already collected key.
- The bronze key and chest are optional and never block the exit.

### Chest rules

- A chest remains locked until at least one key exists in its dungeon-and-level inventory.
- Entering interaction range displays either the locked message, the current mapped Interact button plus `UP / W`, or `CHEST ALREADY OPENED`; the reward is granted only on a fresh mapped-Interact, Up Arrow, or `W` press.
- Opening atomically removes one matching key, plays the metal-bound chest's unlock/open animation, grants its reward, permanently records the chest as opened, and leaves the open final frame visible.
- A chest can be claimed only once per save; replaying a level cannot farm repeated random rewards.
- A replayed claimed chest uses a clearly open/empty sprite and keeps its prompt trigger enabled so it explains its state instead of looking like an unresponsive closed chest. Already-collected bronze keys do not reappear.
- The reward roll is:

| Chance | Reward | Value or effect |
|---:|---|---|
| 50% | Blue crystal reward | Adds five green-crystal currency. |
| 45% | Health potion | Adds one potion to inventory. |
| 5% | Extra life | Adds one life. |

Chest rewards and placed blue crystals both use the same five-green-crystal value.

---

## 8. Crystal economy and shop

Green-crystal value is the single earned currency used by the current shop. There is no separate coin currency in the Bronze Mines design.

### Crystal values

| Crystal | Currency value | Placement rule |
|---|---:|---|
| Green | 1 | Common along normal and optional routes. |
| Blue | 5 | Rare, on difficult optional routes or awarded by a chest. |
| Purple | 20 | Exceptional secret reward; Level 11 contains one extremely difficult purple crystal. |

Collected currency saves immediately.

### Overview and in-level shop

The shop is available from the main/overview screen and as a paused modal overlay inside every level. The in-level version preserves the live scene and displays the same current crystal, life, potion, and heart information. Closing it resumes at the same place; returning to an overview requires its explicit button.

| Item | Price | Effect |
|---|---:|---|
| Health potion | 3 green crystals | Adds one potion; using it restores one heart. |
| Extra life | 25 green crystals | Adds one life. |
| Heart-capacity upgrade | To be balanced | Permanently increases the maximum hearts available in every level by one. |

The initial seven-heart maximum remains viable without purchases. Upgrade pricing must be set only after the balance pass.

---

## 9. Dungeon roadmap

Material names identify whole dungeons, not successive bands inside Dungeon 1.

| Order | Dungeon | Identity | Status |
|---:|---|---|---|
| 1 | Bronze Mines | Bronze-veined dark rock, introductory mine machinery, twelve tunnels | In production |
| 2 | Silver Mines | Silver-flaked rock, hang-glider traversal, moving machinery, and hidden rooms | Level 1 proving build |
| 3 | Gold Mines | Gold material identity and later-difficulty systems | Provisional |
| 4 | Ruby and Sapphire Mines | Contrasting red/blue crystal regions | Provisional |
| 5 | Diamond Mines | High-value late-game mining challenges | Provisional |
| Later | Surface, atmosphere, moon, planets, sun | Journey beyond the mine sequence | Idea parking lot |

Dungeon 2 starts the silver material theme. Silver is not introduced as an environmental tier inside Bronze Mines Levels 1-12.

### Silver Mines — Level 1: Silver Lode proving build

`SilverDungeonOverview.unity` exposes `SilverLevel1_SilverLode.unity` for focused testing before the remaining Silver chapter or any Bronze layout revision. The route follows the supplied map after rotating it counter-clockwise into gameplay orientation: entrance at lower left, a long left climb, a major chute, central platforms, a right-side climb, a terminal chute, and exit at lower right. Double-headed arrows become moving platforms or hazards along the drawn axis.

- Both drawn chute regions receive descent-aware camera zones, but the hang glider can be deployed during any airborne moment in the game.
- The former canopy is replaced by a smaller hang glider. Its front-on image is intentionally reserved for Hover, with the wing above the miner, the control bar below, and the miner facing the camera. Neutral Float, Dive, and left/right travel use side-profile art; left travel mirrors the authored right-facing bank, and the top wing flexes/flaps subtly in every deployed state. Neutral input glides, Up/W can wait in the air for moving machinery, and Down/S accelerates descent moderately.
- Multiple Silver keys and multiple chests validate counted inventory and one-key-per-chest consumption.
- A fake wall visually matches the surrounding rock yet remains walk-through, revealing an optional room of blue and purple gemstones.
- Shaded regions are filled with reusable tiled rock and Silver-theme flakes. Exposed surfaces use whole-rock edge and corner pieces instead of clipped fill art.
- Gems are faceted, animated cut stones at 60% of the former size. Bronze-metal spikes are polished, animated, and 50% of the former size.
- Chests retain their useful base design but add metal binding, a readable front keyhole, and an opening animation that holds on the open state. Doors open before either entrance or exit traversal begins.

The same shared rock construction and theme hooks are used for Bronze, substituting bronze flakes through `Assets/Art/Generated/BronzeDungeonTheme.asset`. Silver uses `Assets/Art/Silver/SilverDungeonTheme.asset`. This first level is the approval target before revisiting Bronze level compositions.

---

## 10. Production and validation rules

### Required automated coverage

- The overview renders from an active camera and exposes twelve level nodes.
- Locked nodes cannot load early levels through UI interaction.
- Levels 3-10 unlock sequentially.
- Level 11 remains locked without both Level 10 completion and the saved silver key.
- Level 12 remains locked until Level 11 is completed.
- All twelve levels have one supported entrance that locks input for a front-facing walk-out intro and restores the authored gameplay start/checkpoint afterward. All twelve exits have foundations, remain inactive on contact alone, require mapped Interact or Up/W, and use the visible door-entry transition.
- All twelve Bronze levels contain one bronze key and one persisted chest; Silver Level 1 contains multiple uniquely identified keys and chests under the Silver dungeon scope.
- All chests require mapped Interact or Up/W, remain closed on contact alone, consume exactly one scoped key, animate open, and restore an unmistakable open state after being claimed.
- Horizontal levels contain fatal bottomless pit zones that remain lethal during ordinary damage invulnerability.
- Ordinary stacked platforms satisfy the collider-based headroom rule; every smaller clearance is an explicitly named head-bump challenge.
- Level 2's varied upper platforms rise diagonally, its ramp remains parallel beneath them, and its upward-facing spikes deal one heart.
- Every Bronze Mines spike uses three inset polygon paths aligned to its visible teeth. Automated overlap checks hit each tooth center and reject both transparent valleys, including on rotated and scaled variants.
- Level 3 contains one deep hang-glider chute, four or more safe-lane gems, a safe landing, and a two-step exit tunnel without instructional hang-glider text.
- Level 10's required route contains validated directional power jumps, and each accepted landing retains at least 0.25 world units of safe horizontal clearance from damaging spikes.
- Level 11 contains exactly five blue crystals and one purple crystal, with values 5 and 20.
- Level 12 contains exactly three sections of each required direction in a reproducible seeded order; its horizontal pits are localized, its safely low global abyss catches off-route falls, and its three descents support independent Jump, global hang-glider control, smoothly blended downward camera framing, airborne waypoints, readable launch/landing transitions, and fair-reveal one-heart hazards.
- A new save has seven hearts per level and three total attempts.
- Spending the final life loads the Game Over screen; only its Restart action clears all progression and economy data, begins a new three-life run, and returns to the overview.
- Shop prices and potion healing match this document.
- The default miner can complete every required route without a purchase or power-up.
- Input validation confirms fixed left-stick/D-pad and keyboard controls; six stable default controller bindings; meaningful-activity device selection when multiple USB interfaces are attached; semantic-`Gamepad` preference over a noisy duplicate `Joystick`; usable generic DirectInput defaults; the three-page Levels/Shop/Controls overview; six wired mapping rows; per-model persistence; conflict swapping; and Restore Defaults without coupling gameplay overrides to UI navigation. Interact toggles the hang glider anywhere airborne without arming it on the ground, while Up/W and Down/S select hover and faster-descent physics. Presentation validation covers the intentional front-facing Hover, side-profile Float and Dive, mirrored left/right banks, matching miner-facing overrides, visible top-wing flex, and a clean return to the stowed pose.
- A grounded ordinary or power jump holds row-2 frame 1 for approximately 0.08 seconds before upward velocity begins, then advances directly to frame 2; a quick tap still launches, a held input produces the expected higher arc, and a power jump remains committed through its squat.
- Pausing and opening the mid-level shop freeze and resume simulation cleanly. Shop purchases leave scene state and miner position intact; the shop's explicit Return-to-Overview action restores normal time, preserves saved progress, and grants neither completion nor a life penalty.
- Silver Level 1 validates the lower-left-to-lower-right route, two chute zones, multiple scoped key/chest pairs, the non-solid fake wall and blue/purple secret room, animated doors/chests, edge-aware rock construction, and Silver-theme metal flakes.
- The hero collider is zero-friction and cannot remain pinned to a wall or platform edge under sustained movement input.
- Missing hearts render as dim filled-heart glyphs rather than unsupported outline glyphs, the Bronze Miner renders no pickaxe, and a null optional tool remains valid.
- Levels 2, 5, 8, and 11 plus Level 12's angled sections use dedicated unrotated diagonal-mine artwork. Level 12 background tiles retain uniform scale and cover every camera overscan region without exposing black void.

### Current automated verification record — July 19, 2026

These reports were produced from the final regenerated scenes and current scripts. Human visual approval of the Silver environment and hang-glider animation remains pending.

- `Logs/SilverFinalValidation.log` records a structural pass across Bronze Levels 1-12 and the available Silver content, including seven hearts, counted scoped keys/chests, the in-level shop, global hang-glider systems, themed rock/metal presentation, doors, hazards, and route contracts.
- `Logs/SilverFinalMechanicsSmokeTest.json` records every check passing, including exact art selection for the five deployed glider presentation states, front/side miner poses, left/right mirroring, grip-anchor continuity, animated wing flex, scoped key consumption, chest/door interaction, the modal shop, and MINER-only `HEALTH`/`LIFE`.
- `Logs/SilverFinalShopPlaytest.json` records the modal shop opening and closing in the final 49-waypoint scene without unloading or resetting the live level.
- `Logs/SilverFinalFullRoutePlaytest.json` records all 49 waypoints completed, the configured exit reached, no respawn, global hang-glider use observed, and real Up-hover plus Down-dive route input exercised in `SilverLevel1_SilverLode.unity`.

### Human playtest focus

- Confirm that the slower side speed and higher jump feel controlled rather than sluggish.
- Confirm the 9-unit run is visibly faster than the 7.5-unit walk and selects the run animation, and that a directional power jump is distinct without replacing the ordinary jump.
- On a Logitech F310 in rear-switch X/XInput mode, complete a controller-only pass with both the left stick and D-pad; verify the defaults match the table, remap all six actions, change two assignments through conflict swapping, reload the game to confirm persistence, and restore defaults.
- With the Logitech and a generic/retro USB controller attached together, confirm that actual activity selects the intended device, stick/D-pad-only noise does not strand gameplay on a movement-only interface, and every controller can Jump, Interact/deploy the hang glider, use a potion, Pause, and open the Shop either from defaults or after mapping.
- On the overview Levels page, activate the Foreman's Master Key once with `MINER` and once with the ten-direction controller sequence. Confirm all twelve named tunnels become selectable, Level 11 does not award a silver key, the active banner is unmistakable, and returning from a mutated test run leaves the real save byte-for-byte/logically unchanged. Inside that test run, type `HEALTH` to restore seven/current-maximum hearts and `LIFE` to add exactly ten lives; verify neither command works outside the sandbox.
- Confirm the brief squat is readable without making jump input feel delayed, no standing frame flashes between squat and rise, and the squat never appears after takeoff.
- Confirm that the thin platforms remain readable and their collision matches their artwork.
- Brush through the transparent sides and valleys of upright, angled, wall-mounted, and scaled spikes; health must change only when the miner overlaps a visible tooth.
- Confirm ordinary jumps have noticeably more overhead clearance, while every deliberate head-bump challenge is clearly telegraphed and documented.
- Verify the Bronze Miner has no visible pickaxe and that removing the tool does not disturb the body animation or future attachment point.
- Verify every outfit preserves the hero's recognizable face, scale, collider alignment, and full directional animation contract.
- Start every level from the entrance door. Confirm the supported door remains aligned to its start platform, the miner visibly walks toward the camera, movement is locked during the intro, and physics, collision, input, and checkpoint state are restored at the authored start.
- Verify door entry selects the back-facing walk-away animation rather than mirroring a side-walk cycle.
- Verify that Level 2 falls naturally land on the ramp and cannot bypass the intended restart.
- Verify Level 2 platform widths and both axes of jump spacing vary while the path still reads as a diagonal ascent; confirm every ramp spike points upward.
- Traverse Level 3 from its launch shelf through every gem-marked chute lane, land safely, and use the two-step exit tunnel without any tutorial text appearing.
- Verify that bottomless pit deaths are clear and never resemble recoverable Level 2 falls.
- Verify sustained input against both sides of walls and platform edges cannot friction-lock the miner.
- Damage the miner and confirm missing health appears as dim filled hearts, never empty font-missing squares.
- Take damage again as soon as invulnerability expires, then lose a life and respawn; after both sequences the miner must return to full opacity rather than retaining a flash frame.
- Approach each chest with and without its bronze key; verify contact does not open it, mapped Interact or Up/W opens it exactly once when keyed, and replay shows the open sprite plus `CHEST ALREADY OPENED` without recreating the key.
- Approach every exit and verify contact only shows the current Interact mapping plus Up/W; while grounded, press mapped Interact or Up/W and confirm that the back-facing walk-through begins exactly once before the overview loads.
- Clear every required Level 10 spike crossing with directional power jumps, verify each accepted landing has at least 0.25 world units of safe clearance, and confirm the ordinary jump cannot accidentally masquerade as a validated power jump.
- Pause and resume with Start, Escape, and `P`. From live play, use Back or Backspace and verify the modal Shop opens over the unchanged level, purchases update the same economy, closing resumes at the same miner position, and its explicit overview button abandons without completion or life loss.
- Deploy and stow the hang glider with mapped Interact/X in an ordinary airborne jump outside any chute, then repeat through both Silver and all three Level 12 chute zones. Verify grounded X never pre-arms it; Hover intentionally shows the front-on glider and front-facing miner; neutral Float, Down/S Dive, and left/right travel switch to readable side profiles; the left bank mirrors correctly; and the top wing flexes/flaps subtly without clipping the miner or hiding hazards. Confirm neutral input glides, Up/W can hold for a moving platform, Down/S descends faster, Jump remains independent, landing resets the glider, and the camera blends without threshold bounce.
- Traverse Silver Level 1 from the lower-left entrance through both major chutes to the lower-right exit. Collect several keys before opening several chests and verify each animation consumes one key and persists open. Walk through the visually matching fake wall, collect the blue/purple secret gems, inspect whole-rock wall edges/corners and Silver flakes, and verify entrance/exit doors open before the miner crosses them.
- Spend the final life, verify that no overview load occurs before Game Over is displayed, then verify Restart restores three lives while clearing level unlocks, all keys, chest state, crystals, potions, and heart upgrades.
- Measure whether later levels are difficult because of mastery rather than excessive repetition.

---

## 11. Reusable level brief

### Identity

- **Dungeon and level:**
- **Name:**
- **Direction:** Vertical up / angled up / horizontal / vertical down / explicit hybrid
- **Purpose and new challenge:**
- **Target completion time:**

### Route

- **Route sketch or composition reference:**
- **Supported entrance, intro path, and gameplay start:**
- **Spawn and opening orientation:**
- **Required route:**
- **Recovery route or pit behavior:**
- **Final challenge:**
- **Supported exit location:**
- **Camera and background direction:**
- **Ordinary minimum headroom and any explicitly named head-bump exception:**

### Hazards and rewards

- **Hazards and damage:**
- **Green, blue, and purple crystal placements:**
- **Bronze key hiding place:**
- **Same-level chest location:**
- **Special key or secret:**

### Acceptance checklist

- [ ] Default movement can complete the required route.
- [ ] The supported entrance plays the front-facing walk-out intro, locks control during transition, and restores normal play at the authored start.
- [ ] Required jumps have an appropriate margin for the target difficulty.
- [ ] Ordinary stacked platforms provide the required hero-collider headroom; any tighter head-bump challenge is explicitly named and documented.
- [ ] Hazards are visible before they can cause unavoidable damage.
- [ ] Any descent defines hang-glider hover/glide/faster-descent behavior, downward camera framing, airborne safe-lane waypoints, front-hover/side-flight presentation, left/right banking, and top-wing flex.
- [ ] Modular route-aware backgrounds cover camera overscan at uniform scale and visually distinguish scenery from collision.
- [ ] The player cannot become permanently trapped.
- [ ] Horizontal pits are unmistakably lethal; recoverable ramps are unmistakably recoverable.
- [ ] The bronze key and chest are optional, persisted, and belong to this level.
- [ ] The exit has a platform, requires mapped Interact or Up/W while grounded in range, and then plays the walk-through cutscene.
- [ ] Crystal colors and values match the economy table.
- [ ] Automated waypoints cover the intended route.

---

## 12. Open design decisions

1. What should the permanent +1-heart upgrade cost after the twelve-level balance pass?
2. Should later heart upgrades use a flat or increasing price?
3. Should later dungeons also contain twelve levels, or use a different tunnel count?
4. Which platforms and intended player age should guide accessibility and storefront decisions?

---

## 13. Idea parking lot

- Speed, high-jump, flight, lightweight, and low-gravity power-ups
- Moving platforms and weighted breakable platforms in later content
- Cosmetics and miner skins
- Optional level skips after their resource and pricing are designed
- Surface, atmosphere, moon, planet, sun, and another-galaxy chapters
- Real-money purchases, subject to platform, age, save-recovery, and refund requirements

---

## 14. Change log

| Version | Date | Change |
|---|---|---|
| 0.9a | July 19, 2026 | Reworked the hang glider into five deployed visual states: an intentional front-on Hover with the miner facing the camera, side-profile Float and Dive, mirrored left/right banks, state-specific miner pose overrides, and subtle top-wing flex/flap animation; corrected the Silver Level 1 scene identity to Silver Lode and recorded the current provisional automated verification status. |
| 0.9 | July 19, 2026 | Added the Silver Mines Level 1 proving build and overview from the supplied route map; introduced globally available airborne hang-glider hover/glide/descend control, seven base hearts, a true in-level shop, MINER-only HEALTH/LIFE commands, counted dungeon-scoped keys and consuming animated chests, opening doors, a fake-wall gemstone room, reusable themed metal flakes, edge-aware shared rock tiles, and polished smaller gems/spikes. |
| 0.8 | July 16, 2026 | Added a supported start door and front-facing miner walk-out intro to every level; made multi-controller selection follow meaningful activity with semantic Gamepad preference and generic DirectInput defaults; clarified that Level 12 chute prompts never suppress Jump and only activate descent behavior while airborne and falling; smoothed descent-camera transitions; added safe shaft launch shelves, invulnerability-proof fatal falls, and a global off-route abyss; and adopted uniformly scaled modular route-aware backgrounds with overscan coverage and player route sketches as composition references. |
| 0.7c | July 16, 2026 | Stabilized Level 12 camera initialization and vertical framing; separated parachute deployment onto contextual Interact/Up/W; enlarged and lowered the canopy; added dedicated cool-dark descent layers and visible bronze-veined shaft walls; ended chute triggers above landings; and opened the lower-right post-descent routes by shortening matching walls and relocating final spikes. |
| 0.7b | July 16, 2026 | Added the hidden, session-only Foreman's Master Key for rapid level testing through `MINER` or a face-button-free controller sequence; all twelve tunnels open through a visible playtest override while an in-memory progression sandbox protects the real save. |
| 0.7a | July 16, 2026 | Made damage flashing a single owned visual state so boundary-timed repeat hits, respawn, shutdown, and door entry always restore the miner's authored full opacity; added runtime regression coverage. |
| 0.7 | July 16, 2026 | Replaced rectangular spike damage bounds with three inset visible-tooth polygon paths; added a third overview Controls page; made Run, Jump, Interact/Parachute, Potion, Pause, and Return-to-Shop controller buttons remappable; added per-controller-model persistence, conflict swapping, fixed keyboard/UI navigation, dynamic prompts/HUD labels, and Restore Defaults. |
| 0.6 | July 15, 2026 | Added the complete keyboard and Logitech/XInput control layout; introduced 9-unit running and committed force-14.75 directional power jumps; assigned controller actions for interaction, potions, pause, and return to the overview Shop; documented progress-preserving mid-level return; and made Level 10 power jumps and 0.25-unit spike-landing clearance part of validation. |
| 0.5b | July 15, 2026 | Changed exit doors to the same explicit proximity interaction as chests; contact now shows an exit prompt without completing the level, and automated validation proves explicit input starts the existing walk-through sequence. |
| 0.5a | July 15, 2026 | Changed reward chests to explicit proximity interaction; added locked, ready, and already-opened HUD feedback; added a distinct persistent open-chest visual; hid collected bronze keys on replay; and added one-time reward regression coverage. |
| 0.5 | July 15, 2026 | Expanded the Bronze Mines to twelve tunnels; defined Level 12 as a long, reproducibly shuffled twelve-section mixed gauntlet with parachute descents, localized pits, fair-reveal wall spikes, moving hazards, descent-aware cameras, and airborne validation; required dedicated unrotated diagonal artwork; removed the Bronze Miner's pick while retaining optional tool architecture; eliminated wall/ledge sticking with zero-friction hero collision; replaced unsupported empty-heart glyphs with dim filled hearts; and confirmed that Game Over Restart clears all run progress. |
| 0.4c | July 15, 2026 | Defined the grounded jump-anticipation squat, committed quick-tap behavior, row-2 idle/squat/rise/apex/fall/land order, hand-tool pose tracking, and automated transition checks. |
| 0.4b | July 15, 2026 | Increased default platform headroom with explicit-only head-bump exceptions, changed Level 2 to a varied diagonally rising upper route over a parallel ramp with upward-facing spikes, added the Level 3 invisible-death regression contract, and routed zero lives through a Restart-capable Game Over screen. |
| 0.4a | July 14, 2026 | Defined a persistent hero identity, swappable dungeon outfit profiles, hand-rigged tools, and the complete side/front/back animation contract used by gameplay and door entry. |
| 0.4 | July 14, 2026 | Defined all eleven Bronze Mines tunnels and their alternating directions; rebuilt Level 2's design around horizontal gaps above an angled spike ramp; added bottomless horizontal pits, bronze keys and one-time chests, the Level 10 silver key and Level 11 gate, exact crystal values and Level 11 rewards, revised shop prices, the redrawn miner and hand-held pick, thinner bronze-veined rock platforms, and the confirmed Silver Mines as Dungeon 2. |
| 0.3 | July 14, 2026 | Added five-heart/three-life rules, green crystals, the overview shop, slower movement, visible door entry, supported-door standards, and the initial miner presentation. |
| 0.2 | July 14, 2026 | Rebuilt Level 1 as Bronze Shaft, made the exit door the completion rule, and added the dungeon overview flow. |
| 0.1 | July 14, 2026 | Organized the original outline and added reusable dungeon and level briefs. |
