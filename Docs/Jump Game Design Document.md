# Jump

## Game Design Document

**Status:** First dungeon production design
**Version:** 0.7a
**Last updated:** July 16, 2026
**Working title:** Jump

This document is the current design authority for the playable Bronze Mines chapter. Later-dungeon ideas remain provisional unless they are explicitly marked confirmed.

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
- **Heart:** One point of health during a level. The base maximum is five hearts.
- **Bronze key:** A level-specific collectible used to open that same level's reward chest.
- **Silver key:** A special progression key hidden on a difficult route in Level 10 and required to enter Level 11.

---

## 2. Core game flow

1. Start at the main screen or dungeon overview.
2. Choose one of the twelve mineshafts on the Bronze Mines overview.
3. Enter an unlocked level.
4. Traverse the tunnel, avoid hazards, and collect optional crystals and keys.
5. Optionally use the level's bronze key to open its reward chest.
6. Reach the supported exit door and press the mapped Interact button (default `X`), Up Arrow, or `W`.
7. Watch the miner visibly walk through the door, complete the level, and return to the overview.
8. Spend crystals in the overview shop, adjust controller mappings on the Controls page, or continue to the next unlocked shaft.

Normal levels unlock sequentially. Level 11 requires both completion of Level 10 and possession of the silver key. Completing Level 11 unlocks Level 12.

During an active level, the player may press the mapped Return-to-Shop button (default Back) or Backspace to abandon the attempt and return directly to the `DungeonOverview` Shop. This convenience exit does not complete the level or consume a life, and it preserves all progress that has already been saved. Pause, potion, chest, door, and abandon actions are locked once a death/respawn transition begins so the final-life Game Over flow cannot be bypassed.

### Controls

| Action | Default controller / fixed keyboard input | Notes |
|---|---|---|
| Move | Controller left stick or D-pad; Arrow keys or `A` / `D` | Both Logitech movement controls are supported. Base horizontal speed is 7.5 units per second. |
| Run | Controller `A`; Left or Right Shift | The controller button is remappable. Hold while moving to run at 9 units per second. |
| Jump | Controller `B`; Space | The controller button is remappable. A grounded press shows an approximately 0.08-second squat before the ordinary force-12 impulse; holding after takeoff extends height for up to 0.24 seconds. |
| Power jump | Hold a direction + mapped Run + Jump; hold a direction + Shift + Space | A directional run-jump commits at jump press, uses force 14.75, and supports a 0.26-second held-jump window. |
| Parachute | Hold mapped Jump or Space during a Level 12 descent | Deploys the parachute for a slower, steerable fall; release Jump to fast-drop. Ordinary jump launch is suppressed inside a descent zone. |
| Open chest | Controller `X`; Up Arrow or `W` | Interact is remappable. Works only while standing in a chest's interaction range; contact alone never claims the reward. |
| Enter exit door | Controller `X`; Up Arrow or `W` | Interact is remappable. Works while grounded in the exit door's interaction range; contact alone only displays the exit prompt. |
| Use health potion | Controller `Y`; `H` | The controller button is remappable. Consumes one potion and restores one heart. |
| Pause / resume | Start; Escape or `P` | The controller button is remappable. Toggles the in-level pause overlay without changing progress. |
| Return to shop | Back; Backspace | The controller button is remappable. Immediately returns to the `DungeonOverview` Shop without completing the level or consuming a life. |
| Open inventory | `E` | Reserved for the broader inventory interface. |

The overview has three pages: Levels, Shop, and Controls. The Controls page can remap Run, Jump/Parachute, Interact, Health Potion, Pause, and Return to Shop. It displays the active controller and current assignments, waits for the button used to select a row to be released, captures the next controller button, saves a versioned profile per controller model, and offers **Restore Defaults** for that model. Assigning a button already used by another action swaps the two assignments so every action remains reachable. Controller movement, keyboard controls, and UI navigation/submit remain fixed; Game Over does not erase controller preferences.

For the pictured Logitech F310, the rear input-mode switch's **X** position is recommended. XInput provides the stable `A`/`B`/`X`/`Y`, Start, and Back defaults shown above. Other controller layouts can use the mapping screen. The game binds both the left stick and D-pad to movement; the F310 Mode button may swap those two hardware controls, but either remains a supported movement source.

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
- Door-entry transitions use the back-facing walk-away animation so the hero visibly walks into the doorway. Front-facing walking supports entrances, reveals, and movement toward the camera.

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

- The miner starts every level with five full hearts, plus any future permanent heart-capacity upgrades.
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
- Falling into a bottomless pit is lethal, consumes the current attempt, and follows the normal life/respawn flow.
- Level 2's reset ramp is not a bottomless pit. Falling from its upper route makes the player slide back to the ramp's bottom and restart the traversal without spending a life unless spikes reduce health to zero.

---

## 5. Universal level and art rules

### Doors and completion

- The exit is reached by a grounded player at the end of the intended route.
- Touching the exit only displays the mapped Interact button plus `UP / W`; it does not complete the level, hide the miner, or teleport the character.
- Pressing mapped Interact (default controller `X`), Up Arrow, or `W` while grounded in the door's interaction range locks control and starts a short cutscene that visibly walks the miner into the doorway before the overview loads.
- Pressing mapped Return to Shop (default Back) or Backspace is a separate abandon-level action. It returns directly to the `DungeonOverview` Shop without playing the completion cutscene, unlocking the next level, or consuming a life; saved crystals, keys, opened chests, potions, lives, upgrades, and controller mappings remain intact.
- Every ordinary door must have a solid platform directly beneath it. A floating or unsupported door is allowed only when a level brief explicitly calls for one.

### Platforms

- Mine platforms are formed from irregular chunks of the level's dark rock.
- Bronze veins run through and between the rocks. They should read as mineral veins, not as a flat rectangular bronze frame.
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
| 3 | Chasm Run | Horizontal | A longer lateral route that introduces bottomless gaps. |
| 4 | Copper Column | Vertical | A taller climb with less generous spacing and more hazards. |
| 5 | Crooked Incline | Angled | A longer diagonal ascent with increasingly demanding recovery. |
| 6 | Broken Rail | Horizontal | Longer pit crossings and more hazardous optional routes. |
| 7 | Furnace Rise | Vertical | An extended vertical endurance climb with combined hazards. |
| 8 | Razor Ascent | Angled | A long diagonal route with tighter spike timing. |
| 9 | Abyss Run | Horizontal | The hardest bottomless-pit tunnel before the key challenge. |
| 10 | Key Vault | Vertical | A long, difficult climb with required power-jump spike crossings and the hidden silver key on a hard optional path. |
| 11 | Treasure Vein | Angled | A silver-key-gated treasure tunnel with the chapter's hardest optional crystal routes. |
| 12 | Deepworks Gauntlet | Mixed | A very long seeded twelve-section capstone with three vertical climbs, three diagonal climbs, three horizontal pit runs, and three parachute descents. |

### Length and challenge curve

- Each level after Level 2 is longer and more difficult than the preceding level.
- Vertical levels increase climbing height and landing difficulty.
- Angled levels increase route length, slide risk, and spike combinations.
- Horizontal levels use separated platforms over bottomless pits; falling into a pit is lethal.
- Level 12 mixes every established direction and introduces controlled parachute descents without replacing the default platforming controls outside descent zones.
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

Levels 3, 6, and 9 contain bottomless pits beneath separated horizontal platforms, as do Level 12's horizontal sections. Pit edges must be visually obvious, camera framing must reveal the intended landing, and each lethal trigger must remain localized beneath its visible gap. In Level 3 specifically, lethal triggers exist only inside and below visible gaps: standing, landing, or jumping on the authored route must never cause an invisible death.

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
- Horizontal sections retain localized bottomless pits under visible gaps. Their lethal triggers never extend into another section or a required landing corridor.
- Each downward section clearly teaches the same rule before danger begins: hold Jump to deploy the parachute for a slower fall, release Jump to fast-drop, and steer horizontally to dodge.
- Downward sections use camouflaged one-heart wall spikes and moving hazards. A hidden spike shows a visible crack, glint, or reveal warning before its damaging collider activates; every hazard arrangement leaves a reachable safe lane.
- Camera look-ahead shifts downward before and during a drop so hazards are shown with sufficient reaction time. Airborne route waypoints cover the intended dodge lanes and a grounded waypoint confirms each descent landing.
- Direction-specific background pieces change with the route. Diagonal sections use the dedicated unrotated diagonal-mine art instead of a rotated ordinary backdrop.
- Level 12 still contains its own optional bronze key and one-time chest, neither of which is required for the exit.

---

## 7. Keys and reward chests

Every Bronze Mines level contains one hidden bronze key and one reward chest.

### Bronze key rules

- A bronze key belongs to the level in which it is found.
- It opens only that same level's chest.
- Collection is saved so leaving or dying does not recreate an already collected key.
- The bronze key and chest are optional and never block the exit.

### Chest rules

- A chest remains locked until its same-level bronze key has been collected.
- Entering interaction range displays either the locked message, the current mapped Interact button plus `UP / W`, or `CHEST ALREADY OPENED`; the reward is granted only on a fresh mapped-Interact, Up Arrow, or `W` press.
- Opening consumes the key's use for that chest and permanently records that the chest was opened.
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

### Overview shop

The shop is available from the main/overview screen and displays the player's current crystal, life, potion, and heart information.

| Item | Price | Effect |
|---|---:|---|
| Health potion | 3 green crystals | Adds one potion; using it restores one heart. |
| Extra life | 25 green crystals | Adds one life. |
| Heart-capacity upgrade | To be balanced | Permanently increases the maximum hearts available in every level by one. |

The initial five-heart maximum remains viable without purchases. Upgrade pricing must be set only after the twelve-level balance pass.

---

## 9. Dungeon roadmap

Material names identify whole dungeons, not successive bands inside Dungeon 1.

| Order | Dungeon | Identity | Status |
|---:|---|---|---|
| 1 | Bronze Mines | Bronze-veined dark rock, introductory mine machinery, twelve tunnels | In production |
| 2 | Silver Mines | Silver-veined rock and a more advanced mining chapter | Confirmed next dungeon |
| 3 | Gold Mines | Gold material identity and later-difficulty systems | Provisional |
| 4 | Ruby and Sapphire Mines | Contrasting red/blue crystal regions | Provisional |
| 5 | Diamond Mines | High-value late-game mining challenges | Provisional |
| Later | Surface, atmosphere, moon, planets, sun | Journey beyond the mine sequence | Idea parking lot |

Dungeon 2 starts the silver material theme. Silver is not introduced as an environmental tier inside Bronze Mines Levels 1-12.

---

## 10. Production and validation rules

### Required automated coverage

- The overview renders from an active camera and exposes twelve level nodes.
- Locked nodes cannot load early levels through UI interaction.
- Levels 3-10 unlock sequentially.
- Level 11 remains locked without both Level 10 completion and the saved silver key.
- Level 12 remains locked until Level 11 is completed.
- All twelve exits have foundations, remain inactive on contact alone, require mapped Interact or Up/W, and use the visible door-entry transition.
- All twelve levels contain one bronze key and one persisted chest.
- All chests require mapped Interact or Up/W, remain closed on contact alone, and restore an unmistakable open state after being claimed.
- Horizontal levels contain lethal bottomless pit zones.
- Ordinary stacked platforms satisfy the collider-based headroom rule; every smaller clearance is an explicitly named head-bump challenge.
- Level 2's varied upper platforms rise diagonally, its ramp remains parallel beneath them, and its upward-facing spikes deal one heart.
- Every Bronze Mines spike uses three inset polygon paths aligned to its visible teeth. Automated overlap checks hit each tooth center and reject both transparent valleys, including on rotated and scaled variants.
- Level 3 has no lethal trigger intersecting a platform, spawn, required landing, or normal jump corridor.
- Level 10's required route contains validated directional power jumps, and each accepted landing retains at least 0.25 world units of safe horizontal clearance from damaging spikes.
- Level 11 contains exactly five blue crystals and one purple crystal, with values 5 and 20.
- Level 12 contains exactly three sections of each required direction in a reproducible seeded order; its horizontal pits are localized, and its three descents support parachute control, downward camera framing, airborne waypoints, and fair-reveal one-heart hazards.
- A new save has five hearts per level and three total attempts.
- Spending the final life loads the Game Over screen; only its Restart action clears all progression and economy data, begins a new three-life run, and returns to the overview.
- Shop prices and potion healing match this document.
- The default miner can complete every required route without a purchase or power-up.
- Input validation confirms fixed left-stick/D-pad and keyboard controls; six stable default controller bindings; the three-page Levels/Shop/Controls overview; six wired mapping rows; per-model persistence; conflict swapping; and Restore Defaults without coupling gameplay overrides to UI navigation.
- A grounded ordinary or power jump holds row-2 frame 1 for approximately 0.08 seconds before upward velocity begins, then advances directly to frame 2; a quick tap still launches, a held input produces the expected higher arc, and a power jump remains committed through its squat.
- Pausing freezes and resumes level simulation cleanly. Returning home from either running or paused play restores normal time, loads the `DungeonOverview` Shop, preserves saved progress, and grants neither completion nor a life penalty.
- The hero collider is zero-friction and cannot remain pinned to a wall or platform edge under sustained movement input.
- Missing hearts render as dim filled-heart glyphs rather than unsupported outline glyphs, the Bronze Miner renders no pickaxe, and a null optional tool remains valid.
- Levels 2, 5, 8, and 11 plus Level 12's angled sections use dedicated unrotated diagonal-mine artwork.

### Human playtest focus

- Confirm that the slower side speed and higher jump feel controlled rather than sluggish.
- Confirm the 9-unit run is visibly faster than the 7.5-unit walk and selects the run animation, and that a directional power jump is distinct without replacing the ordinary jump.
- On a Logitech F310 in rear-switch X/XInput mode, complete a controller-only pass with both the left stick and D-pad; verify the defaults match the table, remap all six actions, change two assignments through conflict swapping, reload the game to confirm persistence, and restore defaults.
- Confirm the brief squat is readable without making jump input feel delayed, no standing frame flashes between squat and rise, and the squat never appears after takeoff.
- Confirm that the thin platforms remain readable and their collision matches their artwork.
- Brush through the transparent sides and valleys of upright, angled, wall-mounted, and scaled spikes; health must change only when the miner overlaps a visible tooth.
- Confirm ordinary jumps have noticeably more overhead clearance, while every deliberate head-bump challenge is clearly telegraphed and documented.
- Verify the Bronze Miner has no visible pickaxe and that removing the tool does not disturb the body animation or future attachment point.
- Verify every outfit preserves the hero's recognizable face, scale, collider alignment, and full directional animation contract.
- Verify door entry selects the back-facing walk-away animation rather than mirroring a side-walk cycle.
- Verify that Level 2 falls naturally land on the ramp and cannot bypass the intended restart.
- Verify Level 2 platform widths and both axes of jump spacing vary while the path still reads as a diagonal ascent; confirm every ramp spike points upward.
- Traverse Level 3 from spawn to exit and land near both edges of every platform to prove no invisible death trigger overlaps the playable route.
- Verify that bottomless pit deaths are clear and never resemble recoverable Level 2 falls.
- Verify sustained input against both sides of walls and platform edges cannot friction-lock the miner.
- Damage the miner and confirm missing health appears as dim filled hearts, never empty font-missing squares.
- Take damage again as soon as invulnerability expires, then lose a life and respawn; after both sequences the miner must return to full opacity rather than retaining a flash frame.
- Approach each chest with and without its bronze key; verify contact does not open it, mapped Interact or Up/W opens it exactly once when keyed, and replay shows the open sprite plus `CHEST ALREADY OPENED` without recreating the key.
- Approach every exit and verify contact only shows the current Interact mapping plus Up/W; while grounded, press mapped Interact or Up/W and confirm that the back-facing walk-through begins exactly once before the overview loads.
- Clear every required Level 10 spike crossing with directional power jumps, verify each accepted landing has at least 0.25 world units of safe clearance, and confirm the ordinary jump cannot accidentally masquerade as a validated power jump.
- Pause and resume with Start, Escape, and `P`. From both live and paused play, use Back or Backspace and verify the Shop opens without completing the level or consuming a life and that all saved progress remains available.
- Play all three Level 12 descents using both deployed and released parachute states; verify the downward camera reveals every warning, hidden hazards never damage before revealing, moving hazards leave a viable lane, and localized pits do not overlap another section.
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
- [ ] Required jumps have an appropriate margin for the target difficulty.
- [ ] Ordinary stacked platforms provide the required hero-collider headroom; any tighter head-bump challenge is explicitly named and documented.
- [ ] Hazards are visible before they can cause unavoidable damage.
- [ ] Any descent defines parachute/fast-drop behavior, downward camera framing, and airborne safe-lane waypoints.
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
3. Which new traversal mechanic should distinguish the Silver Mines from the Bronze Mines?
4. Should later dungeons also contain twelve levels, or use a different tunnel count?
5. Which platforms and intended player age should guide accessibility and storefront decisions?

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
| 0.7a | July 16, 2026 | Made damage flashing a single owned visual state so boundary-timed repeat hits, respawn, shutdown, and door entry always restore the miner's authored full opacity; added runtime regression coverage. |
| 0.7 | July 16, 2026 | Replaced rectangular spike damage bounds with three inset visible-tooth polygon paths; added a third overview Controls page; made Run, Jump/Parachute, Interact, Potion, Pause, and Return-to-Shop controller buttons remappable; added per-controller-model persistence, conflict swapping, fixed keyboard/UI navigation, dynamic prompts/HUD labels, and Restore Defaults. |
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
