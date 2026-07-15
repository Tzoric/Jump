# Jump

## Game Design Document

**Status:** Early design draft  
**Version:** 0.3  
**Last updated:** July 14, 2026  
**Working title:** Jump

This document organizes the ideas from `Jump game outline.docx` into a format that can grow with the project. Items marked **Decision needed** are intentionally unresolved. Items marked **Proposed** are recommendations and can be changed without contradicting the original outline.

---

## 1. Game vision

### One-sentence concept

Jump is a platforming game in which the player travels through increasingly difficult themed dungeons, collects gems, survives environmental hazards, and uses upgrades and power-ups to reach each level's goal.

### Intended player experience

- Movement should be easy to learn and satisfying to master.
- New mechanics should be taught safely before they are combined into difficult challenges.
- Levels should become more demanding without becoming tedious.
- Gems, unlocks, upgrades, skins, and new locations should give players reasons to continue.
- A failed attempt should teach the player something and make another attempt feel worthwhile.

### Project vocabulary

- **Dungeon:** A themed world containing a large collection of levels. Examples include the Mines, the Surface, and the Moon.
- **Level:** One playable stage within a dungeon.
- **Gem:** The main collectible and current working currency.
- **Power-up:** A temporary or consumable effect such as increased speed, a higher jump, flight, or reduced weight.
- **Upgrade:** A longer-lasting improvement purchased or earned by the player.

---

## 2. Player journey

### Game flow

1. Start the game.
2. View the home screen.
3. Select a dungeon.
4. Select an unlocked level.
5. Play the level: move, jump, avoid hazards, and collect gems.
6. Reach the level goal, possibly while carrying or collecting a key.
7. Add collected gems to the player's balance.
8. Unlock the next level, purchase something, or replay for a better result.

### Core gameplay loop

**Choose a level → traverse platforms → avoid hazards → collect rewards → complete the goal → improve or unlock → attempt a harder level**

### Controls

| Action | Keyboard input | Status |
|---|---|---|
| Move left or right | Arrow keys or `A` / `D` | Confirmed |
| Jump | Current project jump input | Implemented; final key should be documented in-game |
| Open or close inventory | `E` | Planned |
| Pause | **Decision needed** | Not specified |
| Use selected item | **Decision needed** | Not specified |

---

## 3. Level rules

### Completion

The long-term level goal is to reach the end of the stage. A key may be required before the exit opens.

**Decision needed:** Decide whether every level requires a key, only selected puzzle levels require one, or the key is unrelated to level completion.

The current build completes a level when the grounded miner reaches its exit. Control is briefly locked while the miner visibly walks into the doorway, then the overview loads. Required crystals are not an exit condition. Every ordinary exit must have a solid platform made from the level's rock and tier material directly underneath it unless the level brief explicitly permits an unsupported door.

### Health and failure

- The player has health and can take damage from hazards.
- Falling out of the level causes damage, death, or a respawn.
- After death, the player restarts from the beginning or the latest checkpoint.
- The outline proposes “3–5 HP per level.” The exact meaning must be confirmed.

**Resolved in version 0.3:** The player has five hearts per level and three persistent starting lives.

### Difficulty principles

- Introduce one major idea at a time.
- Demonstrate a hazard before requiring a difficult reaction to it.
- Give the player a safe landing area after demanding jumps.
- Increase challenge by combining known mechanics, not only by making jumps longer.
- Keep early levels short and replayable.
- Avoid long sections that force the player to repeat easy actions after one difficult obstacle.

---

## 4. Player systems

### Health

The implemented health system uses five base hearts, one-heart hazard damage, temporary invulnerability, potion healing, three starting lives, death, respawning, and permanent heart-capacity upgrades.

### Inventory

Pressing `E` is intended to open the inventory. The inventory may eventually hold consumables, keys, equipment, and other carried objects.

Planned inventory rules:

- Each carried object may add weight.
- Heavy inventory causes weighted platforms to break faster.
- A lightweight power-up reduces the player's effective weight.
- A low-gravity power-up reduces the effective force placed on a weighted platform.
- Inventory weight and temporary power-up modifiers should remain separate so effects are easy to balance.

### Power-ups and purchasable items

Ideas from the original outline:

- Health potion
- Apple or another healing food
- Speed boost
- Flight
- High jump

Additional power-ups already supported by the current design direction:

- Lightweight effect
- Low-gravity effect

For every power-up, define its duration, strength, inventory weight, price or unlock method, stacking rule, and whether it can be used during a level.

### Cosmetics

Skins may be purchased with earned currency. Cosmetics should not affect level reachability or player power.

---

## 5. Economy and progression

### Currency plan

| Currency or item | Earned by | Used for | Status |
|---|---|---|---|
| Gems | Collecting them in levels | Upgrades, skins, optional dungeon unlocks, and possibly level skips | Confirmed concept |
| Keys | **Decision needed** | Level exits, level skips, or both | Ambiguous in the outline |
| Coins | **Decision needed** | Possibly shop purchases | Mentioned once; role not defined |
| Real-world currency | Optional store purchase | Gems or coins | Future concept; platform and audience rules must be reviewed first |

### Gem values

Green crystals are introduced in Dungeon 1, Level 2 and save immediately as the current shop currency. Later levels may contain crystals worth 5, 10, 15, or 20. Different values should have clearly different visuals.

### Unlocking content

- Normal route: complete levels and dungeons in order.
- Optional route: spend earned gems to unlock a later dungeon without completing every earlier dungeon.
- Possible skip: spend a resource to bypass an individual level.

**Decision needed:** The sentence “Key can pay gems to have a skip on any level” could mean that the player pays gems to skip, or that keys are skip tokens. Confirm which resource should be used.

### Scope warning

The target of 100–200 levels per dungeon is a long-term idea. Before committing to that number, complete and measure the time needed to build, test, and polish the first 10–20 levels. Reusable level pieces and reliable automated tests will be important at this scale.

---

## 6. Dungeon roadmap

The current location ideas form a journey from underground to space.

| Order | Dungeon concept | Theme | Status |
|---:|---|---|---|
| 1 | The Mines | Underground tunnels, machinery, crystals, unstable structures | In development |
| 2 | The Surface | Open terrain and the first outdoor environment | Concept |
| 3 | The Atmosphere | Wind, clouds, height, and aerial movement | Concept |
| 4 | The Moon | Low gravity, craters, and space hazards | Concept |
| 5+ | The Planets | A distinct mechanic and visual identity for each selected planet | Concept |
| Later | The Sun | Extreme heat and high-risk endgame challenges | Concept |
| Expansion | Another Galaxy | New worlds after the main journey | Possible future expansion |

**Proposed:** Treat “dungeon” as the game's internal term for a world or chapter, even when the location is outdoors or in space. The player-facing label could be “World” if that feels more natural later.

---

## 7. Dungeon 1 — The Mines

### Dungeon identity

**Fantasy:** Begin deep underground and escape through a dangerous working mine while learning the game's core movement and survival systems.

**Visual ideas:** Dark rock, timber supports, mine carts, rails, lanterns, crystals, ropes, warning signs, dust, and machinery.

**Core mechanics:**

- Basic movement and jumping
- Health, damage, death, and respawning
- Gems
- Moving platforms
- Falling ceiling spikes with a visible warning
- Weighted breakable platforms
- Pits and fall hazards
- Inventory weight and gravity modifiers in later levels

### Material progression

The material visible in mine walls and related platform trim changes as the player advances. Use proportional bands so this progression still works if the final dungeon contains more or fewer than 100 levels.

| Mines progress | Example for 100 levels | Wall material | Visual purpose |
|---|---:|---|---|
| First 20% | 1–20 | Bronze and copper | Establish the early working mine |
| 21–40% | 21–40 | Silver | Show that the player has reached richer, cooler-colored depths |
| 41–60% | 41–60 | Gold | Mark the advanced middle of the dungeon |
| 61–80% | 61–80 | Ruby and sapphire | Create a high-value red-and-blue crystal region |
| Final 20% | 81–100 | Diamond | Signal the most difficult and prestigious Mines levels |

Level 1 is part of the bronze tier. Material tiers are environmental progression markers; their relationship to collectible gem values can be balanced separately.

### Difficulty arc

| Levels | Main purpose | Planned content |
|---|---|---|
| 1 | Tutorial and first completion | A simple vertical climb using only stationary platforms, camera tracking, and the exit door |
| 2 | Introduce rewards | Teach gem collection and show how collected gems enter the player's balance |
| 3–5 | Practice | Slightly longer routes using one mechanic at a time |
| 6–10 | Combine basics | Mix familiar hazards while keeping levels concise |
| 11–15 | Moving-platform focus | More demanding moving platforms and gems worth 5, 10, 15, and 20 |
| 16–20 | First mastery test | Combine moving platforms, spikes, breakable platforms, weight, and limited health |
| 21+ | Future acts | Add mine-specific mechanics only after Levels 1–20 are proven fun and achievable |

**Confirmed Level 1 scope:** Moving platforms, falling spikes, and weighted breakable platforms are reserved for later levels. Level 1 teaches basic movement and completion without those hazards.

### Level 1 — Bronze Shaft

**Purpose:** A short vertical tutorial that introduces the player to the Mines and teaches the complete level flow.

**Difficulty:** 1/5  
**Target first-attempt time:** 1–3 minutes  
**Primary objective:** Climb the shaft, land on the final platform, and walk through the mine exit door.  
**Completion result:** Return to the Mines dungeon overview.

#### What the player learns

1. Move left and right.
2. Jump between stationary platforms.
3. Follow the camera upward through a vertical level.
4. Read the bronze wall deposits as the current Mines material tier.
5. Recognize the exit door as the level goal.
6. Walk through the door to return to the dungeon overview.

#### Level-design rules

- Use wide, stationary platforms in a clear alternating zig-zag.
- Keep every next landing visible and avoid placing a ledge directly over the takeoff position.
- Require a stable landing on the final platform before door entry.
- Keep the route free of damage hazards so failure comes only from missing a jump.
- Do not require an inventory or power-up to finish the level.
- Do not place required gems because gem collection is taught in Level 2.

#### Playtest acceptance criteria

- The level can be completed from the default spawn with the default player abilities.
- No required jump depends on a frame-perfect input.
- The player cannot become permanently trapped.
- Missing the route eventually returns the player to the starting floor or respawns them from the pit.
- The exit loads the dungeon overview only while the player is grounded.
- The automated virtual controller completes all 11 authored landings and reaches the exit door.

### Level 2 — First Pay Dirt

**Purpose:** Introduce gems without adding another major mechanic.

- Place the first gem directly on the required route.
- Use a short visual or interface response to show that it was collected.
- Show the updated gem balance after level completion.
- Place optional gems on slightly more difficult but safe routes.
- Do not require spending gems yet.

### Levels 3–10 — Foundation

- Increase level length gradually.
- Focus each level on one main skill.
- Reuse known mechanics in new arrangements.
- Add optional gem routes for confident players.
- Avoid making levels feel longer only by repeating the same jump.
- Introduce checkpoints only when levels become long enough to need them.

### Levels 11–20 — Mine machinery

- Make moving platforms the main focus.
- Introduce multiple gem values with distinct visuals.
- Combine horizontal and vertical platform movement gradually.
- Use falling spikes near moving platforms only after each is understood separately.
- Add breakable-platform routes that change based on carried weight.
- Balance five-heart difficulty and the three-life economy through playtesting.

---

## 8. Shop plan

The overview shop sells upgrades and consumables. Current prices are 10 green crystals for an extra life, 5 for a health potion, and 25 for a permanent +1-heart upgrade. Press `H` in a level to consume a potion.

### Initial categories

| Category | Examples | Design rule |
|---|---|---|
| Healing | Health potion, apple | Should help recovery without removing all challenge |
| Movement | Speed boost, high jump, flight | Levels must define whether each item is allowed |
| Weight and gravity | Lightweight, low gravity | Must interact predictably with weighted platforms |
| Cosmetics | Player skins | Visual only |
| Progression | Dungeon unlock or level skip | Price carefully so playing normally remains rewarding |

### Purchase safety

Real-money purchases are a future business decision, not a requirement for the first playable version. Before implementing them, define the target platforms, intended player age, parental controls, refund behavior, save recovery, and applicable storefront policies.

---

## 9. Production roadmap

### Milestone 1 — Reliable first level

- Finish and polish Level 1.
- Confirm movement, health, hazards, respawning, and level completion.
- Add clear tutorial prompts and player feedback.
- Verify the level with automated and human playtesting.

### Milestone 2 — Progression foundation

- Build Level 2 and introduce gems.
- Add a saved gem balance.
- Add a level-exit rule.
- Add level selection and unlocking.
- Define checkpoints and completion records.

### Milestone 3 — First Mines chapter

- Build and validate Levels 3–10.
- Establish reusable mine prefabs and level-building standards.
- Track completion time, deaths, and common failure points.

### Milestone 4 — Inventory and power-ups

- Add the inventory opened with `E`.
- Add carried weight.
- Implement and test one power-up at a time.
- Show duration and active effects clearly in the interface.

### Milestone 5 — Shop and extended content

- Add an earned-currency shop.
- Add upgrades and cosmetics.
- Build Levels 11–20 and rebalance the difficulty curve.
- Evaluate production time before choosing the final number of levels per dungeon.

---

## 10. Reusable dungeon brief

Copy this section when planning a new dungeon.

### Identity

- **Dungeon number and name:**
- **One-sentence fantasy:**
- **Location in the journey:**
- **Visual style and color palette:**
- **Music and sound ideas:**
- **Story purpose:**
- **Target number of levels:**

### Gameplay

- **New mechanic introduced:**
- **Existing mechanics expanded:**
- **Signature hazards:**
- **Signature platforms or traversal:**
- **Dungeon-specific collectible:**
- **Power-ups introduced or emphasized:**
- **Inventory or weight interactions:**
- **Final challenge or boss:**

### Progression

- **How the dungeon unlocks:**
- **Level groups and difficulty curve:**
- **Expected completion time:**
- **Rewards for completion:**
- **Optional challenges:**

### Production notes

- **Required art and animation:**
- **Required audio:**
- **Required scripts and systems:**
- **Reusable prefabs:**
- **Major risks or unanswered questions:**

---

## 11. Reusable level brief

Copy this section for every new level.

### Level identity

- **Dungeon and level number:**
- **Level name:**
- **One-sentence purpose:**
- **Difficulty (1–5):**
- **Target completion time:**

### Player goal

- **Required objective:**
- **Optional objective:**
- **Completion condition:**
- **Failure conditions:**
- **Checkpoint locations:**

### Gameplay plan

- **Mechanic introduced:**
- **Mechanics practiced:**
- **Mechanics combined or mastered:**
- **Required player abilities:**
- **Allowed or required power-ups:**
- **Expected inventory weight:**
- **Health or lives available:**

### Level sequence

1. **Opening and orientation:**
2. **Safe teaching moment:**
3. **First real test:**
4. **Combination or escalation:**
5. **Rest or reward moment:**
6. **Final challenge:**
7. **Exit and results:**

### Content

- **Platforms:**
- **Hazards:**
- **Enemies:**
- **Required collectibles:**
- **Optional gems and values:**
- **Secrets:**
- **Tutorial messages:**
- **Visual or audio landmarks:**

### Playtest checklist

- [ ] The default character can complete the level without a purchased item.
- [ ] Required jumps have an appropriate margin for the target difficulty.
- [ ] Hazards are communicated before they cause unavoidable damage.
- [ ] The player cannot become permanently trapped.
- [ ] Checkpoints and resets restore every required object correctly.
- [ ] Optional rewards do not look required.
- [ ] The virtual controller test has a clear route and pass condition.
- [ ] Completion time and deaths are recorded.
- [ ] Known problems and balance changes are documented.

---

## 12. Open design decisions

1. Does a level end by reaching an exit, collecting a key, collecting all required crystals, or a combination of these?
2. Are keys level objectives, skip tokens, or both?
3. Does skipping a level cost gems, a key, or another resource?
4. How quickly should heart upgrades and extra lives increase in price after repeated purchases?
5. Is “coin” a separate currency from gems, or should the game use only gems?
6. Which exact level should first introduce moving platforms within the planned Levels 11–15 machinery section?
7. What key should the player press to jump, pause, and use an inventory item?
8. How many levels should the first dungeon contain after the first 20 are measured and tested?
9. Which planets should receive their own dungeons, and in what order?
10. What is the game's target platform and intended player age?

---

## 13. Idea parking lot

These ideas are intentionally saved for later so they are not lost or implemented too early.

- A dungeon for each selected planet
- The Sun as a late-game dungeon
- A different galaxy as an expansion
- Real-money gem or coin purchases
- Flight power-up
- Skippable levels
- Hundreds of levels per dungeon

---

## 14. Change log

### Implemented rules in version 0.3

- Horizontal movement is 75% of its original speed; jump force is about 73% and gravity is 60% of their original values so side and vertical motion read together without making authored ledges unreachable.
- The miner is 125% of the former size and wears a miner helmet while carrying a pickaxe.
- Players have five base hearts and start a new save with three lives. A spike hit costs one heart. Shop upgrades may add hearts.
- Level 2, **Sliding Ascent**, travels up and right across connected 22-degree ramps. Falling slides the player toward the bottom. Four spike groups and six collectible green crystals teach hazards and currency.
- Each overview mineshaft is a level node. Levels 1 and 2 are playable and shafts 3–5 are represented as locked future levels.
- Platforms visually combine the level's rock with bronze binding in the current material tier.
- The overview provides an earned-currency shop for extra lives, potions, and heart upgrades.

| Version | Date | Change |
|---|---|---|
| 0.3 | July 14, 2026 | Added five-heart/three-life rules, green crystals and shop prices, Sliding Ascent Level 2, slower movement, miner presentation, visible door entry, supported-door standards, and rock/bronze platform art. |
| 0.2 | July 14, 2026 | Rebuilt Level 1 as Bronze Shaft, made the exit door the completion rule, added the dungeon overview flow, and defined bronze-to-diamond Mines material tiers. |
| 0.1 | July 14, 2026 | Organized the original Word outline, incorporated the current Mines mechanics, and added reusable dungeon and level briefs. |
