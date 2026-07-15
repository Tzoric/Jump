# Jump

## Game Design Document

**Status:** First dungeon production design
**Version:** 0.4a
**Last updated:** July 14, 2026
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
2. Choose one of the eleven mineshafts on the Bronze Mines overview.
3. Enter an unlocked level.
4. Traverse the tunnel, avoid hazards, and collect optional crystals and keys.
5. Optionally use the level's bronze key to open its reward chest.
6. Reach the supported exit door.
7. Watch the miner visibly walk through the door, complete the level, and return to the overview.
8. Spend crystals in the overview shop or continue to the next unlocked shaft.

Normal levels unlock sequentially. Level 11 requires both completion of Level 10 and possession of the silver key.

### Controls

| Action | Input | Notes |
|---|---|---|
| Move | Arrow keys or `A` / `D` | Base horizontal speed is 7.5 units per second. |
| Jump | Current project jump input | Jump force is 12 with a 0.24-second held-jump window. |
| Use health potion | `H` | Consumes one potion and restores one heart. |
| Open inventory | `E` | Reserved for the broader inventory interface. |

---

## 3. Player presentation and movement

### Miner character

- The playable character is a completely redrawn little miner, approximately 125% of the former character's size.
- The miner wears a dark mining outfit with leather and bronze details.
- A silver mining helmet is integrated into the character sprite and has a small yellow lamp on its front.
- The mining pick is smaller than the original accessory, stays aligned to the hand, and moves with the hand rather than floating beside the body.
- The character, equipment, and animation must match the detail and polish of the cave backgrounds.

### Persistent hero and outfit architecture

- The same recognizable hero identity, face, body proportions, and animation timing persist throughout the game.
- Dungeon themes change a swappable outfit profile rather than replacing the hero. Initial profiles include the Bronze Miner, a construction worker, and an astronaut.
- Each outfit supplies compatible directional sprite sets while preserving the same silhouette, attachment points, collider assumptions, and gameplay scale.
- Hand-held tools may use separate hand-rigged visuals so a pickaxe or later tool can follow the hand without being baked into every body frame.
- The required animation set is side walk, side run, side jump/rise, apex, fall, land, front-facing walk toward the camera, and back-facing walk away from the camera.
- Side animations mirror for left and right unless an outfit or tool requires dedicated directional art.
- Door-entry transitions use the back-facing walk-away animation so the hero visibly walks into the doorway. Front-facing walking supports entrances, reveals, and movement toward the camera.

### Movement tuning

- Base side movement is 7.5 units per second, 75% of the original speed. A future speed power-up may restore or exceed the old speed.
- Gravity scale is 5.4, approximately 60% of the former value, so ascent and falling are readable beside the slower horizontal movement.
- Jump force is 12. This is slightly higher than the first slowed-jump pass while remaining proportionate to the 7.5 side speed.
- The held-jump window is 0.24 seconds.
- Movement values must be playtested against every required route; normal completion must never require a speed power-up.

---

## 4. Health, lives, damage, and failure

- The miner starts every level with five full hearts, plus any future permanent heart-capacity upgrades.
- A spike hit removes exactly one heart and grants brief damage invulnerability.
- A health potion restores exactly one missing heart and cannot exceed the current maximum.
- A new save begins with three lives. A life represents one complete attempt, so the starting balance provides three attempts rather than three respawns after the first attempt.
- Reaching zero hearts consumes one life. If another life remains, restart the level from its beginning; otherwise return to the overview.
- Falling into a bottomless pit is lethal, consumes the current attempt, and follows the normal life/respawn flow.
- Level 2's reset ramp is not a bottomless pit. Falling from its upper route makes the player slide back to the ramp's bottom and restart the traversal without spending a life unless spikes reduce health to zero.

---

## 5. Universal level and art rules

### Doors and completion

- The exit is reached by a grounded player at the end of the intended route.
- Touching the exit does not instantly hide or teleport the character.
- Control is briefly locked and a short cutscene visibly walks the miner into the doorway before the overview loads.
- Every ordinary door must have a solid platform directly beneath it. A floating or unsupported door is allowed only when a level brief explicitly calls for one.

### Platforms

- Mine platforms are formed from irregular chunks of the level's dark rock.
- Bronze veins run through and between the rocks. They should read as mineral veins, not as a flat rectangular bronze frame.
- Rock faces need enough highlights, shadows, and chipped edges to match the surrounding environment.
- Platforms remain thinner vertically than the first prototype while preserving clear collision and safe footing.
- Later dungeons replace the vein material to match their identity; Dungeon 2 uses silver.

### Background and camera

- The world overview always contains an active rendering camera; it must never display a `No cameras rendering` message.
- The level camera follows the miner and frames upcoming landings and hazards.
- Background composition and support art follow each tunnel's direction: vertical shafts emphasize height, angled shafts rise diagonally, and horizontal tunnels emphasize lateral depth.
- Level 2's background is angled to support its rising ramp composition even though its main upper platforms are horizontal.

### Difficulty and fairness

- Present hazards before demanding a difficult reaction.
- Give the player a safe landing or readable recovery opportunity after demanding jumps.
- Optional rewards may be very difficult; the required completion route must remain achievable with default movement and no purchased item.
- Avoid blind jumps, unavoidable damage, and permanent traps.

---

## 6. Bronze Mines progression

Dungeon 1 is the **Bronze Mines** and contains eleven levels, one for each tunnel shown on its world overview. All eleven shafts are level-select nodes rather than decorative `coming soon` entrances.

The principal tunnel direction alternates vertical, angled, and horizontal. Level 2 is classified as the angled entry in the cycle because of its reset ramp and angled composition, while its required upper route is built from horizontal platforms.

| Level | Working name | Direction | Primary challenge |
|---:|---|---|---|
| 1 | Bronze Shaft | Vertical | Short tutorial climb, basic movement, camera tracking, and supported door entry. |
| 2 | Sliding Ascent | Angled hybrid | Horizontal upper platforms separated by gaps over a steep spike-covered reset ramp. |
| 3 | Chasm Run | Horizontal | A longer lateral route that introduces bottomless gaps. |
| 4 | Copper Column | Vertical | A taller climb with less generous spacing and more hazards. |
| 5 | Crooked Incline | Angled | A longer diagonal ascent with increasingly demanding recovery. |
| 6 | Broken Rail | Horizontal | Longer pit crossings and more hazardous optional routes. |
| 7 | Furnace Rise | Vertical | An extended vertical endurance climb with combined hazards. |
| 8 | Razor Ascent | Angled | A long diagonal route with tighter spike timing. |
| 9 | Abyss Run | Horizontal | The hardest bottomless-pit tunnel before the key challenge. |
| 10 | Key Vault | Vertical | A long, difficult climb containing the hidden silver key on a hard optional path. |
| 11 | Treasure Vein | Angled | A silver-key-gated final tunnel with the chapter's hardest optional crystal routes. |

### Length and challenge curve

- Each level after Level 2 is longer and more difficult than the preceding level.
- Vertical levels increase climbing height and landing difficulty.
- Angled levels increase route length, slide risk, and spike combinations.
- Horizontal levels use separated platforms over bottomless pits; falling into a pit is lethal.
- Reused mechanics are combined only after the player has encountered them in a readable form.
- Main-route checkpoints may be added when playtesting shows that repeated early sections become tedious, but they must not bypass keys or one-time chest state.

### Level 1 - Bronze Shaft

- Teach slower movement, jumping, camera tracking, and visible exit entry on wide stationary landings.
- Keep the required route free of damage hazards.
- Hide the level's bronze key and place its reward chest where the system can be learned without blocking completion.

### Level 2 - Sliding Ascent

- The required route moves laterally across thin, flat, horizontal platforms separated by clear gaps.
- A continuous steep ramp runs underneath the upper route and slopes back toward the level start/bottom.
- Falling through an upper gap lands the miner on the ramp. Gravity carries the miner to the bottom, forcing the upper-platform route to be attempted again.
- Spike groups project from the ramp. The miner must jump while sliding to avoid them; every hit costs one heart.
- The ramp is a recoverable reset route, not a bottomless death zone.
- Green crystals introduce collection and the overview shop.
- The cave background, rails, and visual flow are angled to match the ramp.

### Horizontal tunnel rule

Levels 3, 6, and 9 contain bottomless pits beneath separated horizontal platforms. Pit edges must be visually obvious, camera framing must reveal the intended landing, and the required jump distances must fit default movement.

### Level 10 silver-key secret

- The one silver key is hidden in Level 10, which satisfies the requirement that it be hidden in another Bronze Mines level before Level 11.
- Reaching it is deliberately difficult and optional for ordinary Level 10 completion.
- Collection persists after leaving the level, including after a later death.
- The overview clearly distinguishes `complete Level 10` from `find the silver key` when explaining why Level 11 is locked.

### Level 11 - Treasure Vein

- Level 11 unlocks only after Level 10 is completed and the silver key has been collected.
- Its required route is the hardest Bronze Mines completion route, but remains possible with default movement.
- It contains many green crystals plus exactly five blue crystals and one purple crystal.
- Each blue crystal is worth five green crystals.
- The single purple crystal is worth twenty green crystals and occupies the level's most difficult optional route.
- The five blue crystals are also placed on exceptionally difficult optional routes. They are not required to complete the level.

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
- Opening consumes the key's use for that chest and permanently records that the chest was opened.
- A chest can be claimed only once per save; replaying a level cannot farm repeated random rewards.
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

The initial five-heart maximum remains viable without purchases. Upgrade pricing must be set only after the eleven-level balance pass.

---

## 9. Dungeon roadmap

Material names identify whole dungeons, not successive bands inside Dungeon 1.

| Order | Dungeon | Identity | Status |
|---:|---|---|---|
| 1 | Bronze Mines | Bronze-veined dark rock, introductory mine machinery, eleven tunnels | In production |
| 2 | Silver Mines | Silver-veined rock and a more advanced mining chapter | Confirmed next dungeon |
| 3 | Gold Mines | Gold material identity and later-difficulty systems | Provisional |
| 4 | Ruby and Sapphire Mines | Contrasting red/blue crystal regions | Provisional |
| 5 | Diamond Mines | High-value late-game mining challenges | Provisional |
| Later | Surface, atmosphere, moon, planets, sun | Journey beyond the mine sequence | Idea parking lot |

Dungeon 2 starts the silver material theme. Silver is not introduced as an environmental tier inside Bronze Mines Levels 1-11.

---

## 10. Production and validation rules

### Required automated coverage

- The overview renders from an active camera and exposes eleven level nodes.
- Locked nodes cannot load early levels through UI interaction.
- Levels 3-10 unlock sequentially.
- Level 11 remains locked without both Level 10 completion and the saved silver key.
- All eleven exits have foundations and use the visible door-entry transition.
- All eleven levels contain one bronze key and one persisted chest.
- Horizontal levels contain lethal bottomless pit zones.
- Level 2's ramp returns a fallen player toward the start and its spikes deal one heart.
- Level 11 contains exactly five blue crystals and one purple crystal, with values 5 and 20.
- A new save has five hearts per level and three total attempts.
- Shop prices and potion healing match this document.
- The default miner can complete every required route without a purchase or power-up.

### Human playtest focus

- Confirm that the slower side speed and higher jump feel controlled rather than sluggish.
- Confirm that the thin platforms remain readable and their collision matches their artwork.
- Verify the pick stays attached to the moving hand in both facing directions.
- Verify every outfit preserves the hero's recognizable face, scale, collider alignment, and full directional animation contract.
- Verify door entry selects the back-facing walk-away animation rather than mirroring a side-walk cycle.
- Verify that Level 2 falls naturally land on the ramp and cannot bypass the intended restart.
- Verify that bottomless pit deaths are clear and never resemble recoverable Level 2 falls.
- Measure whether later levels are difficult because of mastery rather than excessive repetition.

---

## 11. Reusable level brief

### Identity

- **Dungeon and level:**
- **Name:**
- **Direction:** Vertical / angled / horizontal / explicit hybrid
- **Purpose and new challenge:**
- **Target completion time:**

### Route

- **Spawn and opening orientation:**
- **Required route:**
- **Recovery route or pit behavior:**
- **Final challenge:**
- **Supported exit location:**
- **Camera and background direction:**

### Hazards and rewards

- **Hazards and damage:**
- **Green, blue, and purple crystal placements:**
- **Bronze key hiding place:**
- **Same-level chest location:**
- **Special key or secret:**

### Acceptance checklist

- [ ] Default movement can complete the required route.
- [ ] Required jumps have an appropriate margin for the target difficulty.
- [ ] Hazards are visible before they can cause unavoidable damage.
- [ ] The player cannot become permanently trapped.
- [ ] Horizontal pits are unmistakably lethal; recoverable ramps are unmistakably recoverable.
- [ ] The bronze key and chest are optional, persisted, and belong to this level.
- [ ] The exit has a platform and plays the walk-through cutscene.
- [ ] Crystal colors and values match the economy table.
- [ ] Automated waypoints cover the intended route.

---

## 12. Open design decisions

1. What should the permanent +1-heart upgrade cost after the eleven-level balance pass?
2. Should later heart upgrades use a flat or increasing price?
3. Which new traversal mechanic should distinguish the Silver Mines from the Bronze Mines?
4. Should later dungeons also contain eleven levels, or use a different tunnel count?
5. What are the final pause, inventory, and input-remapping controls?
6. Which platforms and intended player age should guide accessibility and storefront decisions?

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
| 0.4a | July 14, 2026 | Defined a persistent hero identity, swappable dungeon outfit profiles, hand-rigged tools, and the complete side/front/back animation contract used by gameplay and door entry. |
| 0.4 | July 14, 2026 | Defined all eleven Bronze Mines tunnels and their alternating directions; rebuilt Level 2's design around horizontal gaps above an angled spike ramp; added bottomless horizontal pits, bronze keys and one-time chests, the Level 10 silver key and Level 11 gate, exact crystal values and Level 11 rewards, revised shop prices, the redrawn miner and hand-held pick, thinner bronze-veined rock platforms, and the confirmed Silver Mines as Dungeon 2. |
| 0.3 | July 14, 2026 | Added five-heart/three-life rules, green crystals, the overview shop, slower movement, visible door entry, supported-door standards, and the initial miner presentation. |
| 0.2 | July 14, 2026 | Rebuilt Level 1 as Bronze Shaft, made the exit door the completion rule, and added the dungeon overview flow. |
| 0.1 | July 14, 2026 | Organized the original outline and added reusable dungeon and level briefs. |
