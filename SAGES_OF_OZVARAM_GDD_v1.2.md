# SAGES OF OZVARAM
## Game Design Document v1.2

**Studio:** Insomnium-Eye  
**Website:** https://insomnium-eye.com  
**Target Release:** December 2028  
**Platform:** PC (Windows/Linux/macOS), Mobile (later)  
**Genre:** Tactical Fantasy RPG  
**Target Audience:** Fans of Fire Emblem, XCOM, Tactics Ogre, The Elder Scrolls: Skyrim

---

## 1. PROJECT OVERVIEW

### Pitch
A turn-based tactical RPG set on the planet Ozvaram, a world split by ancient cataclysm. Players command a party of heroes through strategic grid-based combat, navigating political intrigue, magic systems, and branching narratives across multiple factions.

### Core Vision
- **Strategic Gameplay**: Every move matters. Grid-based hex combat with rich mechanics.
- **Deep Lore**: Richly realized world with three playable factions and compelling character arcs.
- **Player Agency**: Meaningful choices affecting story, units, and world state.
- **Visual Polish**: 90's anime-inspired pixel art aesthetics with modern game feel; expressive character animation.

---

## 2. WORLD & LORE

### Planet Ozvaram
- **Geography**: Resembles a partially eaten apple; cataclysm split the planet into two hemispheres
- **North Hemisphere**: Sunny, temperate, diverse ecosystems (forests, grasslands, deserts, mountains)
- **South Hemisphere**: Perpetually cold and dark; harsh, alien terrain

### Races

#### Humans
- Versatile, adaptable, no inherent strengths/weaknesses
- Arrived ~900 years ago via space arks
- Dominant on both hemispheres
- Wide range of classes and professions

### Design Philosophy: Anti-Anthropomorphism
Unlike many sci-fi and fantasy series that anthropomorphize non-human species (giving them human-like facial expressions, body language, and emotional displays), Sages of Ozvaram commits to portraying Vectium and Lethios as truly non-human:

- **Visual Authenticity**: Character designs respect biological constraints; no forced human smiles on animal faces
- **Emotional Expression**: Each race communicates through genuine, species-appropriate channels (scale color, ear position, vocal patterns)
- **Player Challenge**: Understanding NPC emotional states requires paying attention to subtle cues, creating deeper immersion
- **Cultural Differentiation**: Natural expression differences reinforce that these are alien cultures, not just "humans with animal ears"
- **Future Races**: As new species join (giant corvids, etc.), each will have distinct, non-human ways of communicating emotional depth

This approach creates a richer, more believable world where cultural misunderstandings can arise from genuine differences in expression.

#### Vectium
- Small, feline beings; cat-like or fox-like in form
- **Anti-Anthro Philosophy**: Despite animal-like appearance, Vectium do NOT express human emotions visibly
  - Always appear to be smiling; this is their natural resting state, not an expression of joy
  - Express pain, sadness, and distress through ear position, tail movement, and body posture
  - Vocal expressions (yowls, chirps, hisses) communicate emotional states humans miss
  - Eye contact and whisker position indicate attention and mood shifts
- Native to Ardalis (northern region)
- Naturally attuned to magic and high evasion
- Graceful, nimble fighters; excellent spellcasters

#### Lethios
- Large, draconic beings with reptilian features
- **Anti-Anthro Philosophy**: Lethios express emotions through subtle physical cues, not facial expressions
  - Always appear stoic or angry; facial expressions are limited by biology
  - Communicate emotional depth through scale color changes (blushing, paling), body tension, and tail positioning
  - Vocal rumbles, growls, and breathing patterns indicate emotional states
  - Eye narrowing and pupil dilation are primary visible emotional indicators
- Physically powerful and durable
- Natural warriors and tanks
- Slower but immensely strong

### Future Races (Post-Launch Content)

#### Giant Corvids (Planned Expansion 1)
- Massive, intelligent avian species standing 7-10 feet tall; corvid-like (ravens, crows, magpies) but scaled to giant size
- Naturally high Strength, Intelligence, and Accuracy; medium HP
- **Emotional Expression**: Communicate through complex vocalizations (caws, clicks, warbles), dramatic wing displays, and subtle plumage color changes
- Excel at both physical combat (talons, wings) and magical/tactical thinking
- Introduce flight mechanics for large creatures and intelligence-based dialogue branches
- Will challenge players with both brute force and cunning strategies

#### Others TBD
- Each new race will bring:
  - Unique class archetypes aligned with biology
  - Distinct emotional/communication styles
  - New gameplay mechanics (burrowing, telepathy, etc.)
  - Expanded faction dynamics and political intrigue

### Factions

#### Sun and Scale Alliance
- **Members**: Humans, Lethios, Vectium (cooperating)
- **Ideology**: Peace through cooperation and mutual respect
- **Strength**: Balanced forces, diplomatic support
- **Leader**: Council of representatives

#### Draconics
- **Members**: Lethios + Vectium (unified)
- **Ideology**: Resist human encroachment; preserve native ways
- **Strength**: Indigenous knowledge, unified command
- **Leader**: High Draconic Council

#### Human Supremacists
- **Members**: Extreme humans from southern hemisphere
- **Ideology**: Human domination and racial purity
- **Strength**: Ruthless, organized, well-funded
- **Leader**: Shane Valcrane (currently sealed; escaping)

---

## 3. MAIN CHARACTERS

### Vicent the 38th (Protagonist)
- **Race**: Vectium
- **Role**: Prince of Ardalis; Sorcerer/Cleric hybrid
- **Arc**: Hero's journey; political intrigue, personal growth
- **Personality**: Curious, compassionate, often conflicted
- **Goal**: Unite the fractured world; uncover truths about the cataclysm

### The Retainer (Player-Controlled)
- **Role**: Guardian sworn to protect Vicent
- **Customization**: Player creates appearance, name, personality
- **Mechanics**: Controls dialogue choices and unit deployment
- **Relationship**: Develops bond with Vicent throughout story

### Sandor
- **Race**: Human
- **Role**: First human Sage of Ozvaram; mentor figure
- **Secret**: Optional superboss if player discovers hidden truth
- **Personality**: Wise, secretive, carries burden of old knowledge
- **Significance**: Key to understanding the cataclysm

### Frudry
- **Race**: Lethios (disguised as human woman via illusion)
- **Role**: Assassin for Human Supremacists
- **Alignment**: IRREDEEMABLE villain
- **Goal**: Harvesting 1000+ souls for Shane Valcrane's resurrection ritual
- **Personality**: Charming facade; utterly amoral beneath

### Shane Valcrane
- **Race**: Human
- **Role**: Leader of Human Supremacists
- **Status**: Currently sealed away; weakening seals allow escape
- **Power**: Arcane master; nearly immortal via dark rituals
- **Threat**: Ultimate antagonist; seeks total human supremacy

### The Seven Sages
- **Count**: 7 legendary mages who shaped world history
- **Status**: 5 of 7 undefined; Sandor and Vicent are two
- **Role**: Major quest givers and story drivers

### Chronus
- **Status**: Character details TBD
- **Hint**: Related to time/prophecy mechanics

---

## 4. GAMEPLAY MECHANICS

### 4.1 Combat System

#### Grid & Movement
- **Grid Type**: Pointy-top hex grid
- **Map Size**: Variable; typically 20-30 tiles wide, 15-25 tiles tall
- **Tiles per AP**: 1 tile per AP by default; +1 additional tile per AP for every 15 points of Speed (15 Speed = 2 tiles/AP, 30 Speed = 3 tiles/AP, etc.)
- **Terrain**: Mountain is the only impassable terrain for now (blocking objects like trees and other units' tiles are impassable too); Water is passable but costs +1 AP per water tile crossed, on top of the normal tile cost
- **Pathfinding**: Dijkstra over the hex grid (weighted, not plain BFS, so it correctly minimizes true AP cost - it'll take a longer land route over a shorter one through water when the land route is actually cheaper) - always routes around impassable tiles, occupied tiles, and each other unit's position, never through them
- **Range Display**: selecting "Move" overlays every nearby tile's AP cost as text - tiles keep their normal terrain color (no fill/tint), white text if affordable with current AP, red text (shown beyond the affordable range too, so the "just out of reach" area is visible) if not
- **Live Path Preview**: hovering a tile draws a line from the unit through the path to it, growing/shrinking as the cursor moves, ending in an arrowhead; hovering an unreachable tile (off-grid, blocked, occupied) instead draws the trail to the nearest tile that IS reachable and marks the hovered tile with a red X instead of an arrowhead
- **Clicking Moves Toward, Not Just To**: clicking any tile - even one further than the unit can currently afford - moves it as far along that path as its AP allows, rather than refusing the click outright. A tile with no path at all (impassable/occupied/unreachable) is simply ignored
- **Animated Movement**: a unit no longer jumps straight to its destination - it visibly walks the path tile-by-tile, animating smoothly through each hex in turn (0.2s per tile) rather than teleporting
- **Camera Follows the Mover**: the camera continuously tracks whichever unit is acting (not just once when its turn starts), so it stays locked on as that unit walks mid-turn. Move mode and free-look ("View Map") both fully own the camera instead - opening either does a one-time snap (Move mode snaps to whatever tile the cursor is already over, so clicking "Move" behaves as if you'd hovered that tile first; View Map snaps back to the acting unit on exit) and WASD/scroll panning takes over from there with no further auto-recentering while that mode is open
- **Turn-Start Gate**: the existing "TURN X START" announcement (2 seconds, every turn - not just the match's first) now genuinely blocks all turn logic while it's showing, not just its own display - no AI movement, no AI attacks, no auto-advance, and no player input processes until it clears. Previously only the camera's own transition-lerp gated anything, which was shorter than the announcement, so a unit (AI or player) could act while "Turn X - Go!" was still on screen
- **Current Implementation**: Move and Attack are both fully functional (targeting, resolution, everything below); Spells/Items are still unimplemented when selected

#### Elevation Tiers & Height Mechanics
- **Tier System**: Ground(0), Elevated(1), High(2), Extreme(3)
- **Movement Cost**: 
  - **Ascending**: +1 AP per tier climbed (moving from Ground to Elevated costs 2 AP for 1-tile move instead of 1 AP)
  - **Descending**: Normal AP cost (no penalty for moving downward)
- **Combat Advantages/Disadvantages**:
  - **Melee Attack Range**: Blocked entirely if target 2+ tiers higher
  - **Accuracy Penalty**: -20% accuracy when attacking target 1 tier higher
  - **Ranged Advantage**: Ranged attacks gain +10% accuracy per tier advantage
  - **Damage Modifier**: Ranged attacks deal +5% damage per tier advantage
- **Flying Units**: 
  - Variable altitude with ceiling/floor limits per map
  - Can see over walls and terrain if not under roof (roofed areas block flying vision advantage)
  - Flying movement uses different pathing (ignore ground obstacles)
  - Can't land on water or hazardous terrain (unless aquatic/specialized)
- **Falling Damage**: 
  - 2+ tier descent = damage taken (formula TBD via testing)
  - Example: Falling from High (2) to Ground (0) = 2 tiers = takes fall damage
  - Damage scales with distance and unit weight class
  - Heavily armored units take more fall damage; light units less
- **Environmental Height**:
  - Bridges, cliffs, towers, balconies create elevation variation
  - Map design rewards vertical strategy and positioning
  - Knockback effects can combine with height for dramatic damage/positioning

#### Turn Order
- **Mechanic**: Based on EFFECTIVE Speed (raw Speed, minus any active Slowed reduction - see Status Effects) - rebuilt fresh at the start of every Turn, so a unit that got Slowed mid-Turn acts later starting the next one
- **Base Order** *(unmodified Speed)*: Hunter (8) → Sorcerer (6) → Cleric (5) → Warrior (4)
- **Cycling**: After all units act, turn increments; cycle repeats
- **UI**: Turn number displayed; "TURN X START" announcement at turn begins
- **Match Format**: currently a 4-player free-for-all - all 4 units are mutually hostile (no parties/teams yet); the player controls one, the other three are AI-controlled (see AI below)

#### Action Points (AP)
- **Per Turn**: 5 AP
- **Actions**: Movement, attacks, spells, summons, items, guard, card draw
- **Current Implementation**: every attack (racial or weapon) costs a flat 1 AP, regardless of the move - switching to a weapon that isn't currently equipped costs +1 AP on top to cover the draw (so 2 AP total for a switch); Guard costs 2 AP; standing up from Knocked Down and breaking Stun have their own separate AP costs (see Status Effects); Movement is fully implemented (see Grid & Movement above). Move, Attack, and Guard/End are all live; Spells/Items are not
- **Mana (MP)**: regenerates a flat, Class-tiered amount every turn instead of fully refilling (see Stats below). Nothing costs MP except Spell/Summon Cards and the Sorcerer's actual MAGIC weapon attacks - Frost Blast (Staff) and Arcane Missile (Wand) specifically, not their free base attacks Bonk/Whack
- **UI**: the player's current AP and MP are both displayed on-screen during their unit's turn; a move or card whose AP/MP cost the player can't currently afford shows in red instead of white/black

#### Race-Based Attacks
- **Innate Move**: Every unit has at least one attack, granted for free based on their race - always available regardless of equipment
- **Weapon Attacks**: Equipped weapons grant additional attacks on top of the racial one
- **Cost Variance**: Attacks can vary in AP cost, MP cost, and other costs
- **Accuracy Stat Formula**: A unit's Accuracy stat adds +0.1% hit chance per point, on top of a move's base accuracy; the target's Evasion stat then subtracts at the same rate (+0.1%/point) - the mirror of Accuracy, working the other way (see Stats below)
- **Damage Types**: every move deals one of 7 types - **Sharp** and **Blunt** are the two physical flavors (mitigated by DEF); **Magical**, **Light**, **Electric**, **Poison**, and **Frost** are magical/elemental (mitigated by RES). The last three currently exist specifically as racial vulnerability targets (see Race Perks below) - nothing deals them yet. Purely a label for now beyond mitigation and vulnerability - nothing gates which status effects a given type can inflict, though every move so far happens to follow a natural pattern (Sharp things draw blood, Blunt things Stun/Knock Down)
- **Current Race Moves** *(range 1, 1 AP; damage/accuracy formulas, pending a further balance pass)*:
  - **Human - Punch**: `1 + STR/5` Blunt damage, 95% base accuracy, 1% crit chance (3x damage), knocks the target back 1 tile (+1 tile per 5 damage dealt), and can inflict Knocked Down (see Status Effects) - 80% chance if the attacker's Strength beats the target's Strength + Defense, 2% otherwise
  - **Lethios - Bite**: `12 + STR/2` Sharp damage, 40% base accuracy (hard to land), 25% crit chance (3x damage), inflicts Bleeding (Moderate rank: 3% of MAX HP per turn) - devastating when it connects, unreliable otherwise
  - **Vectium - Swipe**: `3 + STR/1.5` Sharp damage, 80% base accuracy (far more reliable than a Bite), 40% crit chance (3x damage), inflicts Bleeding (Minor rank: 2% of MAX HP per turn) - trades raw damage for precision and crit
  - **Roachlin - Roachlin Slash** *(summoned creature, not a playable race)*: `3 + STR/5` Sharp damage, 85% base accuracy - summon attacks are scoped for a separate design pass (every one of a summon's moves counts as an "attack," even spell-like ones, unlike a hero's kit)
- **Bleeding's Armor Gate**: a Sharp move's Bleeding chance drops to 0% outright if the target's (Total, racial-bonus-inclusive) Defense beats the attacker's (Effective, racial-bonus-inclusive) Strength - tough enough hide/armor shrugs off a hit that would otherwise draw blood. Doesn't affect other statuses
- **Damage Scaling**: physical (Sharp/Blunt) weapon moves scale with STR; magical (Magical/Light) ones scale with INT instead. Flintlock Pistol and Crossbow's Bolt Shot are the physical exception on guns specifically - no specialization exists for them, so Pistol Shot doesn't scale with either stat
- **Per-Weapon Power (AttackPower)**: flat damage is moving from being baked into each Move to living on the Weapon instead - the point being multiple weapons of the same kind (e.g. a future Rusty vs. Steel Dagger) can share identical moves but hit for different amounts. Migrated so far: Dagger (7), Iron Sword (9), Iron Shield (4), Light Mace (8) - Staff, Wand, Bow, and Pistol still carry damage on the move itself until each is revisited. Dagger vs. Sword is deliberately asymmetric: Dagger's AttackPower and every one of its STR divisors are lower/weaker than Sword's - daggers should always hit for less and benefit less from Strength than swords, as a standing rule for any future weapon in either classification
- **Notable Weapon Moves** *(range 1 unless noted, 1 AP; pending a further balance pass)*:
  - **Iron Sword - Sword Slash**: `9(AttackPower) + STR/4` Sharp, 88% accuracy, 6% crit (3x), 15% chance to inflict Bleeding (Weak) - the "optimized" single-target technique, best STR scaling of the sword's three moves
  - **Iron Sword - Sword Pierce**: range 2; `9 + STR/5` Sharp, same accuracy/Bleed as Slash, deals 1.5x damage against a Guarding target, advances the attacker 1 tile toward the target on use (blocked if occupied)
  - **Iron Sword - Sword Spin** *(Warrior specialist only)*: `9 + STR/5` Sharp to every adjacent enemy at once, 85% accuracy - a notch behind Slash on raw scaling; its value is hitting everyone, not out-damaging the focused technique too
  - **Dagger - Stab**: 100% accuracy, `7(AttackPower) + STR/6` Sharp (slightly more than Swipe, clearly less than Sword Slash), 5% crit chance (3x) plus a guaranteed critical on a backstab (see Facing below), 35% chance to inflict Bleeding (Weak)
  - **Dagger - Throw Dagger** *(Hunter specialist only)*: range 8; `7 + STR/10` Sharp (slightly less than Stab - a thrown strike doesn't harness Strength as efficiently as a direct thrust), decent (80%) accuracy up close that falls off 8%/tile beyond range 1 (~24% by range 8), consumes the dagger on use, requires no ammo
  - **Rhinewood Staff - Bonk**: available to anyone with a Staff; `2 + STR/4` Blunt, low (55%) accuracy, 20% chance to inflict Stunned for 1 turn. No MP cost - it's just swinging a stick
  - **Rhinewood Staff - Frost Blast** *(Sorcerer specialist only)*: a cone-shaped ice blast (see Skill Shots), range 4; `5 + INT/5` Magical, 75% accuracy, low damage but always inflicts Slowed (-40% Speed, 2 turns) on a hit. Costs 10 MP - the actual magic attack
  - **Willow Wand - Whack**: available to anyone with a Wand; `1 + STR/6` Blunt, high (95%) accuracy, very little damage. No MP cost
  - **Willow Wand - Arcane Missile** *(Sorcerer specialist only)*: range 3; `14 + INT/3` Magical, 80% accuracy, 8% crit chance (3x damage). Costs 10 MP - the Sorcerer's actual spellcasting tool (not the Staff)
  - **Iron Shield - Shield Bash** *(Warrior specialist only)*: `4(AttackPower) + STR/6` Blunt, 90% accuracy (decent), low damage, but a guaranteed (100%) chance to inflict Stunned for 1 turn. Non-specialists (e.g. the Cleric) carrying an Iron Shield get no attack from it at all, just its passive/Guard DEF+RES bonus (see Guard Command)
  - **Light Mace - Mace Bash**: `8(AttackPower) + STR/4` Blunt, 70% accuracy (mediocre), 15% crit chance (3x, decent), 20% chance to inflict Stunned for 1 turn. A Cleric specialist gets +20% accuracy, +1 Range (the head detaches on a chain), and an additional +3 Light damage on every hit - mitigated separately by RES, since it's a different damage type than the mace's own Blunt hit
  - **Longbow - Arrow Shot**: range 6; `9 + STR/5` Sharp, a non-monotonic accuracy curve rather than a flat falloff - 20% at range 1-2 (too close to draw properly), 65% at range 3-4 (its intended range), 45% at range 5-6 (falls off again, but less severely than up close) - 10% chance to inflict Bleeding (Weak), requires an Arrow
  - **Longbow - Bow Whack**: available to anyone with a Longbow; `1 + STR/6` Blunt, 85% accuracy, very little damage, no ammo required - a melee fallback with the bow itself
  - **Flintlock Pistol - Pistol Shot**: range 3; `12` Sharp (flat, no STR/INT scaling - nobody specializes in guns), low (35%) accuracy point-blank then a flat, reliable 75% beyond that, 8% crit (3x), 10% chance to inflict Bleeding (Weak), knocks back 1 tile, requires a Bullet - the more accurate, weaker-hitting of the two guns
  - **Crossbow - Bolt Shot**: range 4; `13 + STR/5` Sharp, low (30%) accuracy point-blank then a flat 55% beyond that, 20% crit (3x, higher than the Pistol's), 10% chance to inflict Bleeding (Weak), requires a Bolt - less accurate but hits harder and crits more than the Pistol; a generalist weapon, gives no one a bonus
- **Ammo**: Longbow, Flintlock Pistol, and Crossbow each require a specific ammo type (Arrow/Bullet/Bolt respectively), tracked as separate per-unit counts, 1 consumed per use. Out of ammo = the move is simply unselectable, same treatment as unaffordable AP/MP

#### Weapon Specialization
- **Concept**: certain classes are specialists in a weapon type and get bonuses/bonus attacks for it that non-specialists can't use, even if they carry the same weapon - every weapon that HAS a specialist follows the same base+specialist pattern (a base Attack anyone can use, plus a stronger SpecialistAttack gated to the specialist class)
- **Tied to Class, not Tier**: specializations (and eventually spells) come from a unit's Class (Sorcerer/Warrior/Cleric/Hunter - see Class System §4.2), not hardcoded per unit - any unit whose Class is Hunter gets every Hunter specialization and spell automatically, including as it evolves through the Journeyman/Expert/Master/Transcendent tiers, and any future non-Apprentice Hunter would too
- **One-Handed Sword - Warrior**: unlocks Sword Spin (see above); no accuracy/damage bonus on the base Sword Slash/Pierce. Distinct type from a future Two-Handed Sword - being a one-handed specialist wouldn't automatically extend to two-handed weapons
- **Shield - Warrior**: unlocks Shield Bash; a non-specialist carrying a shield (e.g. the Cleric) gets only its passive/Guard bonus, no attack at all
- **Dagger - Hunter**: unlocks Throw Dagger; no bonus on the base Stab
- **Bow - Hunter**: no bonus attack, but +15% accuracy on Arrow Shot (bows are otherwise unreliable for anyone else); does NOT extend to the Crossbow, which is its own distinct, generalist weapon type
- **Staff - Sorcerer**: unlocks Frost Blast; no bonus on the base Bonk
- **Wand - Sorcerer**: unlocks Arcane Missile; no bonus on the base Whack. The Sorcerer specializes in both Staff and Wand
- **Mace - Cleric**: +20% accuracy, +1 Range, and +3 flat Light damage on every hit of Mace Bash
- **Pistol/Crossbow - none**: both are deliberately generalist weapons; nobody specializes in either, and Pistol Shot's damage never scales with STR or INT for anyone

#### Facing & Backstabs
- **Facing**: every unit faces one of the 6 hex directions (matches the grid's cube-coordinate axes); all units currently spawn facing right (East) and nothing yet rotates a unit to face the direction it moves
- **Backstab**: an attacker standing on the tile directly behind a target (opposite the direction the target is Facing) is attacking from behind; Dagger's Stab always crits against one
- **Visual Indicator**: since unit sprites don't have directional artwork yet, a small cyan arrow is drawn inside each unit's hex tile pointing at whichever neighboring tile it's facing - a placeholder until real facing sprites exist

#### Guard Command
- **Access**: the turn menu's "End" option relabels itself to "Guard" whenever the unit can afford its Guard AP cost (2 by default); selecting it spends that AP, applies the Guard bonus, and still ends the turn. If the unit can't afford it, the slot just shows "End" and ends the turn with no bonus
- **Adjustable Cost**: Guard's AP cost isn't fixed at 2 - it's a per-unit value some future abilities/weapons can raise or lower
- **Base Bonus**: +20% DEF and RES until the start of the unit's next turn
- **Shields Split Into Two Layers**: a shield now grants a passive DEF/RES bonus that applies always, just from being carried, PLUS an additional bonus on top of that specifically while Guarding (replacing the base 20% rather than stacking with it) - Iron Shield: 15% passive, +25% more while Guarding, 40% total; Glass Shield: 10% passive, +15% more while Guarding, 25% total
- **Status Effect Ranks**: general 5-step severity scale (Weak, Minor, Moderate, Major, Severe) used to grade status effects like Bleeding by strength - see Status Effects below

#### Equipped Weapon & Switching
- **One Active Weapon**: a unit has a single EquippedWeapon at a time (defaults to the first weapon added to its inventory, e.g. the Hunter starts with her Longbow equipped)
- **Switch Cost**: attacking with a weapon that isn't equipped costs its listed AP +1 (so 2 AP total, since every move now costs 1 AP on its own), covering the switch (put away the old weapon, draw the new one); the racial move never carries this surcharge
- **Display**: the attack menu shows each move's actual AP cost live, and its remaining ammo count if it requires any - e.g. "Arrow Shot (1 AP, 7 Arrows)"

#### Inventory & Weight
- **Capacity**: every unit has 20 units of inventory weight to carry weapons in (flat for now across all classes - GDD-pending a per-class or per-level variant)
- **Per-Weapon Weight**: small one-handed weapons (Dagger, Flintlock Pistol) weigh 2; everything larger (Sword, Mace, Staff, Wand, Bow, Crossbow, Shield) weighs 3
- **Adding a Weapon**: fails (and leaves the inventory unchanged) if it would exceed the remaining capacity; the first weapon successfully added becomes the unit's EquippedWeapon automatically

#### Card/Deck System
- **Deck Size**: 15-30 cards per class deck, randomized for each hero
- **Starting Hand**: every unit starts with 3 cards in hand at the beginning of a battle; those cards are removed from the draw pile and remain visible on the bottom of the UI while the unit is acting
- **Mechanics**: units do not draw automatically at turn start; instead, they manually spend AP from the Cards menu to draw from their class deck
- **Additional Draws**: from the Cards menu, a unit may draw more cards beyond its first this turn - each additional draw doubles the AP cost of the last one (1 AP, then 2, then 4, then 8, etc.); the draw count (and its cost) resets at the start of the unit's next turn
- **Depletion**: if the deck is exhausted, the draw action cannot continue until a future design pass adds a proper reshuffle rule; for now, the deck simply runs out once the class deck list is empty
- **Strategy**: Deck composition affects unit playstyle, and class decks are built from that class's spells plus generic summon cards so each unit's deck is tailored but still variable
- **Current Implementation**: `SpellCard` and `SummonCard` exist as presentation/stat data, and the turn menu's "Cards" option opens the current unit's actual hand. The hand sits along the bottom of the screen, with the selected card zoomed in and the rest of the hand slightly smaller. A/D browse the hand, Space casts the selected card, F draws a card from the matched class deck at the current doubled AP cost, and E closes the menu. Hovering the cursor over any numeric field on the selected card shows a tooltip explaining what it represents (`GetHoveredCardTooltip`, checked against the exact same fractional regions the numbers are drawn into)
- **Casting**: a spell whose effect is a real, targetable attack (deals damage to a chosen target, or to every adjacent enemy) casts for real through `CastSpellAsAttack` - the exact same AP/MP check, target-picking, and `AttackResolver.Resolve` pipeline a weapon attack uses (`sourceWeapon` is just null, same as a racial move), so it rolls hit/crit, applies damage and any status effect AttackResolver already recognizes (Bleeding/Stunned/Slowed), and removes the card from hand only once it actually resolves - cancelling target selection (E) returns to the hand with the card untouched. Place Trap/Explosive Trap are deliberately excluded despite having a damage formula, since casting them as an immediate hit would misrepresent their real "place now, triggers later" design, which needs a trap system that doesn't exist yet. Every other card - utility/buff/heal spells, and all Summon Cards (no summon-to-battlefield system exists) - reports "can't be cast yet" rather than silently doing nothing or crashing
- **Teleport Spells**: `Move.IsBlink` (currently just Blink and Disengage) marks a spell as an instant self-teleport rather than an attack - `CastBlinkSpell` pays its AP/MP up front, then opens a dedicated teleport targeting mode highlighting every passable, unoccupied tile within Range of the caster (purple, brighter where the cursor is); clicking one moves the caster there directly, no pathfinding involved, and consumes the card. Cancelling (E) refunds nothing since nothing is spent until a destination is actually picked - same pattern as attack targeting
- **Ally-Shield Spells**: `Move.IsAllyShield` (currently just Arcane Shield) marks a spell as granting a health shield to a chosen unit rather than attacking one - `CastAllyShield` pays AP/MP up front, then opens ally-targeting mode listing every living unit within Range INCLUDING the caster itself (green highlight, brighter under the cursor - distinct from attack targeting's red, which deliberately excludes the caster). The game has no real team/ally system yet (still FFA, see Match Format), so "friendly" isn't filtered - any unit in range is a valid target, same as how TargetsAllies already works everywhere else in the spell data; `GetDamage(caster)` with no target/weapon supplies the shield's magnitude, reusing the exact same BaseDamage/StrengthDivisor/IntelligenceDivisor formula fields attack spells use for damage, just applied via ApplyShield instead of TakeDamage
- **Dev Console Testing**: `~` opens the dev console (`DevConsole`, pre-existing for map/tile/object commands) - typing into it requires MonoGame's `Window.TextInput` event (subscribed in `Initialize()`), since `KeyboardState` polling alone only reports which keys are held, not what character they produce; this was broken (no typed input reached the console at all) until fixed. `SpellCard.Id`/`SummonCard.Id` are stable integers assigned once by each catalog's static constructor, in declaration order - spell cards 1-N, summon cards 1001+ (offset so the two ranges are visually distinguishable at a glance)
  - **Scrolling**: PageUp/PageDown (a full `ConsoleVisibleLines` page, 12) and the mouse wheel (a few lines per notch) scroll back through the console's full output history, not just the last 12 lines - `_consoleScrollOffset` tracks how far up from the bottom the view is, clamped against the actual output length in `DrawConsole` every frame so it's safe even right after a `clear`. Up/Down stay bound to command history (the conventional binding for a command-line prompt, same as a shell) rather than scrolling, to avoid fighting over the same keys; submitting a command (Enter) snaps the view back to the bottom automatically, and a yellow "scrolled up N lines" notice replaces the help text whenever it isn't already there
  - `list cards all` (or bare `list cards`) prints every card's ID and name; `list cards <ClassID>` restricts the Spell Cards section to exactly one bucket - `0`=Generic (the 8 class-less spells - see Spell Cards' Design Note on Throw Grit/Trip for why it's 8, not 10), `1`=Sorcerer, `2`=Warrior, `3`=Cleric, `4`=Hunter. Unlike the real Cards menu (`GetSpellsForClass`, which mixes a class's own spells with the Generic ones), 1-4 here show ONLY that class's own cards (`RequiredClass == heroClass`) - Generic is its own bucket (`0`), never folded into 1-4, since the point of asking for "Sorcerer cards" is the Sorcerer-specific roster. The five buckets partition the full 69-card spell catalog exactly (8 Generic + 15 Sorcerer + 15 Warrior + 16 Cleric + 15 Hunter, no overlap or gaps - verified). Summon Cards print in full regardless of which bucket is requested, since they aren't class-gated
  - `add card <CardID>` (integer) adds that card straight into the player's current hand for testing, bypassing the deck entirely (not a real draw - doesn't touch `_classDecks`). As of this writing: Mana Shield = 10, Blink = 11, Arcane Shield = 18 - but always check `list cards all` rather than hardcoding these, since adding/removing/reordering a card shifts every ID after it (these three shifted down by 2 when Throw Grit/Trip were removed - a live example of exactly that)
- **Summoned Units Have No Cards of Their Own**: unlike heroes, a summoned creature doesn't draw cards - it fights with a fixed, set list of attacks (currently just its race's innate move, e.g. Roachlin Slash for the Dusk Roachlin - see Race-Based Attacks). The SummonCard's bottom-left number range (e.g. "3-12") represents the least and most damage that attack list can deal, not a per-hit random roll - it's informational, shown so a player can gauge a summon's damage output before summoning it

#### Spell Cards
- **Rendering**: cards are composited at runtime from `Content/imgs/Cards/Spells/SpellCard.png` (the shared template) plus the card's own art texture and its dynamic text/values - nothing about a specific card is a separate baked image, so any new `SpellCard` renders correctly through the same code
- **Dynamic Fields**: name (top bar), MP cost (top-right circle), class line "Spell - <Class>" (bar under the art, or "Spell - Generic" for a class-less spell), description (teal box), and the card's "headline number" in the bottom-right circle (with a placeholder wand glyph behind it pending real iconography) - `GetSpellHeadlineNumber` shows whichever of the move's non-zero effects applies (damage, flat heal, heal %, or granted AP, checked in that priority order) and draws nothing at all for a pure status/utility spell with no single number to show (e.g. Meditate, Conjure Potions) rather than a misleading "0"
- **Card Art**: the art window on `SpellCard.png` isn't a plain rectangle - it has a curved notch cut into its top-right corner (clearing the cost circle), which every fractional-rectangle measurement attempt missed, always leaving a seam somewhere along an edge. The fix (`BuildSpellCardArtMask`) flood-fills the template's actual near-white pixels outward from a seed point to capture the window's exact pixel shape (cached once), and `GetCardComposite` resamples the card's art directly into the template's own pixel buffer wherever that mask is true, producing one finished texture per card - not two layers drawn at runtime - so there's no draw-order, blend-state, or region-measurement step left that could reintroduce a gap
- **Placeholder Art**: there's no image-generation model available in this environment, so every spell's art except Arcane Bolt's (`Content/imgs/Cards/Spells/*_CardArt.png` - the 8 Generic Spells, 14 of the 15 Sorcerer Spells, and all 15 Warrior and Hunter Spells) is programmatically composed instead of illustrated - a gradient background, a simple geometric icon evoking the spell, and a name label, with a tiled "AI PLACEHOLDER" watermark - clearly not final art, swap out per card whenever real art arrives (only `ArtAssetPath` needs to change, nothing else). Each class gets its own rough color palette (Sorcerer purples/blues, Warrior reds/steel, Hunter forest greens/browns) to stay visually distinct even as placeholders
- **Current Cards** *(first-pass numbers, pending a balance pass)*:
  - **Sorcerer Spells** *(15 total - burst arcane/elemental damage plus magical utility, the class's stated identity)*:
    - **Arcane Bolt**: `1 + INT/2` Magical damage, range 5, 85% accuracy, 2 AP / 2 MP. The only card with real (non-placeholder) art
    - **Magic Missile**: `3 + INT/4` Magical, range 4, 95% accuracy, 1 AP / 1 MP - cheap and reliable, rarely misses
    - **Mana Burn**: `4 + INT/4` Magical, range 4, 85% accuracy, 2 AP / 3 MP - especially effective against other casters (fluff only, nothing currently reads a target's MP to bonus off it)
    - **Ice Spike**: `7 + INT/2.5` Magical, range 4, 85% accuracy, 2 AP / 3 MP, 25% chance to inflict Frostbite
    - **Teleport Strike**: `5 + INT/3` Magical, range 3, 85% accuracy, 2 AP / 4 MP, advances the caster 3 tiles toward the target on use
    - **Curse**: range 4, 85% accuracy, 2 AP / 3 MP, 80% chance to inflict Cursed (lowers STR/ACC) for 3 turns - no damage
    - **Frost Nova**: `5 + INT/3` Magical to every adjacent enemy, 90% accuracy, 3 AP / 4 MP, 30% chance to inflict Frostbite
    - **Flames**: `6 + INT/3` Magical to every adjacent enemy (arc), 85% accuracy, 3 AP / 4 MP, 20% chance to inflict Burning
    - **Chain Lightning**: `6 + INT/3` Magical, range 4, 80% accuracy, 3 AP / 5 MP, 15% chance to inflict Shocked - jumps to up to 2 more nearby enemies (fluff only, no multi-target chain resolution exists yet)
    - **Fireball**: `12 + INT/2` Magical, range 5, 80% accuracy, 4 AP / 6 MP, 25% chance to inflict Burning - the signature nuke
    - **Arcane Orb**: `16 + INT/1.5` Magical, range 5, 75% accuracy, 15% crit chance (2.5x), 4 AP / 7 MP - slow and expensive, hits hardest of any Sorcerer spell
    - **Mana Shield**: 1 AP / 5 MP, self. `Move.IsManaShield` routes it through `Game1.CastManaShield` instead of the attack pipeline - pays its own cost, then grants a real `ShieldPoints` shield equal to whatever MP remains (see Stats' Health Shields note), draining MP in lockstep as it absorbs hits. Casting it again while already shielded toggles it off instead of re-paying, matching the original "can be turned off at will" design - fully implemented, not fluff
    - **Arcane Shield**: 1 AP / 3 MP, range 3. `Move.IsAllyShield` lets the caster pick itself OR any other unit in range (an actual target choice, not self-only) and grants that unit a real `ShieldPoints` health shield worth `8 + INT/3` - see Stats' Health Shields note and the Teleport Spells note below for the matching ally-targeting UI (green highlight, includes the caster's own tile)
    - **Blink**: 1 AP / 3 MP, self, range 3 (teleport distance) - pure mobility, no attack
    - **Enchant Weapon**: 1 AP / 4 MP, range 1 (self or adjacent ally) - grants the target's weapon bonus magic damage scaled to the wielder's INT (fluff only, nothing currently applies an enchant bonus to weapon damage rolls)
  - **Warrior Spells** *(15 total - buffs for self/allies, summoned swords/shields/armor, and special weapon techniques)*:
    - **Mighty Slash**: `4 + STR/2` Physical, range 1, 90% accuracy, 2 AP / 3 MP - "150% of the equipped weapon's damage" in spirit; a flat STR-scaled approximation until spell damage can reference the actual equipped weapon's Move
    - **Whirlwind**: `6 + STR/3` Physical to every adjacent enemy, 85% accuracy, 3 AP / 3 MP
    - **Execute**: `5 + STR/3` Physical, range 1, 85% accuracy, 2 AP / 3 MP - double damage below 25% target HP (fluff only, nothing currently checks target HP% for a damage multiplier)
    - **Charge**: `5 + STR/3` Physical, range 3, 85% accuracy, 2 AP / 3 MP, advances the caster 3 tiles toward the target on use
    - **Disarm**: range 1, 75% accuracy, 2 AP / 2 MP, 70% chance to inflict Disarmed (lowers damage/Accuracy) for 2 turns - no damage
    - **Taunt**: range 3 (all enemies in range), 1 AP / 2 MP - sharply raises aggression toward the warrior (fluff only - see Aggro System; no aggro tracking is implemented yet)
    - **Bodyguard**: range 3, 2 AP / 3 MP - for 1 turn, redirects damage aimed at any ally in range onto the warrior instead
    - **Shield Wall**: range 1 (all adjacent allies), 2 AP / 3 MP - +DEF for 2 turns to every adjacent ally
    - **Rage**: 2 AP / 3 MP, self, 3 turns - every hit taken while active grants +2 damage and +2 DEF, stacking
    - **Battle Cry**: 1 AP / 2 MP, self, 2 turns - +STR
    - **Iron Will**: 1 AP / 2 MP, self, 3 turns - +RES, and resists Stun while active (fluff only, no status-resistance system exists yet)
    - **Counter Stance**: 1 AP / 3 MP, self, 1 turn - the first hit taken this turn is reflected back at its attacker for a portion of the damage dealt (fluff only, no damage-reflection system exists yet)
    - **Summon Blade**: 2 AP / 4 MP, self - conjures a spectral blade for the encounter, granting a bonus STR-scaled attack even unarmed (fluff only, doesn't add a real Move to AvailableMoves yet)
    - **Summon Shield**: 2 AP / 4 MP, self - conjures a spectral shield, granting a Guard bonus as if wielding a real shield (fluff only, doesn't affect GetGuardBonusPercent yet)
    - **Conjure Armor**: 3 AP / 5 MP, self, 3 turns - +DEF/+RES; a placeholder for the real armor system planned later
  - **Hunter Spells** *(15 total - ranged damage, traps, and beast-taming, the class's stated identity)*:
    - **Place Trap**: range 3 (placement), `6 + STR/4` Physical, 85% chance to inflict Snared for 2 turns - invisible, triggers when any enemy/neutral unit passes through or lands on the tile; flying units immune (fluff only, no trap-placement/trigger system exists yet)
    - **Tame Beast**: range 3, 70% accuracy, 3 AP / 5 MP - attempts to tame a neutral beast of the hunter's level or lower, placing it under the hunter's control (fluff only, no neutral-unit/taming system exists yet)
    - **Eagle Eye**: 1 AP / 2 MP, self, 3 turns - +10 Accuracy
    - **Multi-Shot**: `5 + STR/4` Physical, range 5, 80% accuracy, 2 AP / 3 MP - strikes the primary target plus up to 2 more nearby enemies (fluff only, no multi-target resolution exists yet)
    - **Piercing Shot**: `8 + STR/3.5` Physical, range 5, 85% accuracy, 2 AP / 3 MP - punches through the target and anyone behind them in a line (fluff only, no line-piercing targeting exists yet)
    - **Explosive Trap**: range 3 (placement), `10 + STR/3` Physical, 3 AP / 4 MP - detonates on trigger, damaging the triggering unit and anyone adjacent (fluff only, same caveat as Place Trap, no AoE-at-trigger-point resolution)
    - **Camouflage**: 1 AP / 2 MP, self, 3 turns - +Evasion
    - **Hunter's Mark**: range 5, 90% accuracy, 1 AP / 2 MP, 90% chance to inflict Marked for 3 turns - bonus damage taken from all sources (fluff only, nothing currently reads Marked to boost incoming damage)
    - **Disengage**: 1 AP / 2 MP, self, range 3 (retreat distance) - leaps backward out of melee range without provoking (fluff only, no opportunity-attack system exists to avoid)
    - **Beast Bond**: range 3, 1 AP / 3 MP, 3 turns - +STR/+Speed to a tamed beast companion; requires an active Tame Beast
    - **Rain of Arrows**: `6 + STR/4` Physical, range 5, 75% accuracy, 3 AP / 4 MP - an arrow volley over a target area (fluff only, no AoE-at-range resolution exists yet)
    - **Poison Shot**: `4 + STR/5` Physical, range 5, 85% accuracy, 2 AP / 2 MP, 65% chance to inflict Poisoned for 3 turns
    - **Net Shot**: `2 + STR/6` Physical, range 4, 85% accuracy, 2 AP / 2 MP, 80% chance to inflict Snared for 2 turns - minimal damage, built to root instead
    - **Track**: 1 AP / 2 MP, self, 3 turns - reveals every enemy within a wide radius, through fog of war and Camouflage alike (fluff only, no vision-reveal system exists yet - see Fog of War)
    - **Vital Shot**: `6 + STR/3` Physical, range 5, 80% accuracy, 30% crit chance (2.5x), 2 AP / 3 MP - a called shot, unremarkable on a graze but brutal on a solid hit
  - **Generic Spells** *(RequiredClass null - every class gets these, see Card/Deck System above - 8 total, not 10; see Design Note below)*:
    - **Meditate**: 2 AP / 3 MP. Each turn while active, gain a stack (up to 3) of +RES/+DEF/+INT/+ACC/+STR. Taking a single hit for 10%+ of max HP interrupts it and clears all stacks. Ending it voluntarily at the start of your turn locks in the current buff for 3 more turns
    - **Forced Sleep**: 2 AP / 4 MP. Immobilized, heals 20% max HP each turn; any single hit of 10+ damage wakes the caster
    - **Conjure Potions**: 3 AP / 5 MP. Adds 1 minor HP potion, 1 minor MP potion, and 1 minor AP potion to the caster's inventory
    - **Bandage Wound**: 2 AP / 2 MP. Heals 8 flat HP and cures Bleeding
    - **Second Wind**: 1 AP / 3 MP. Grants +2 AP on the caster's next turn
    - **Brace**: 1 AP / 2 MP. +DEF/+RES for 1 turn
    - **Rally Cry**: range 1 (all adjacent allies), 2 AP / 3 MP. +STR/+ACC for 2 turns to every adjacent ally
    - **Steady Hands**: 1 AP / 2 MP. +Accuracy for 2 turns
  - **Design Note - Removed Throw Grit and Trip**: both were offensive techniques that directly targeted and debuffed/stunned an enemy - mechanically indistinguishable from a weapon attack (the same shape as e.g. Shield Bash), just without a weapon requirement. That's a different problem than "not magical enough": every Generic spell is intentionally mundane by design (things "anyone could learn without studying magic"), so Bandage Wound/Brace/Second Wind etc. are correctly kept despite being just as physical/non-magical - the actual issue was redundancy with the Attack system, not a lack of magic. Removing a card shifts every later card's `Id` (see Dev Console Testing below) - both their art files (`ThrowGrit_CardArt.png`, `Trip_CardArt.png`) were deleted too, nothing orphaned
  - Several of these describe mechanics with no execution system behind them yet (Meditate's stacking/interrupt, Forced Sleep's wake condition, Conjure Potions' item creation, ...) - `Move` gained a small set of non-attack fields (`HealFlat`, `HealPercentMaxHP`, `GrantedAP`, `StatusDurationTurns`, `TargetsAllies`) to carry each spell's headline number as real data, but the more elaborate per-spell state machines are still prose-only in each card's Description until a real status-effect system exists to run them

#### Summon Cards
- **Current Implementation**: `SummonCard` holds a creature's presentation data and full stat block (mana cost, unit type, description, ATT/INT/DEF/RES/Accuracy/Evasion/Speed, min-max damage, HP, art). Every card in `SummonCatalog` is included in the "Cards" hand (see Card/Deck System above) alongside spell cards, rendered dynamically through `DrawSummonCard` - same viewer-only stage the Spell cards started at: no deck/draw system and no actual summon-to-battlefield mechanics exist yet, and summons aren't class-gated the way spells are
- **Rendering**: composited at runtime from `Content/imgs/Cards/Summons/SummonCard.png` (the shared template - a per-theme template, "ground" for now, more to come) plus the card's art and dynamic text/values, using the same flood-fill masking approach as Spell Cards (see above) - the Summon template's art window is a flat maroon fill (matched by color distance from a seed pixel rather than a "near white" test) with seven notches down its left edge clearing the stat-icon bars, which a rectangular region would have overlapped
- **Dynamic Fields**: name (top bar), mana cost (top-right white "star" circle - the top-left stone medallion is pure decoration and gets no text), the seven stat values placed into their icon-labelled bars (fist=Attack, book=Intelligence, shield=Defense, meditating figure=Resistance, crosshair=Accuracy, feather=Evasion, lightning bolt=Speed - the icons themselves are baked into the template), unit type (bottom bar), description (cream box), damage range as "min-max" (bottom-left bar, next to a sword badge baked into the template), and HP (bottom-right circle over a heart badge)
- **Description Fit**: `DrawWrappedText` shrinks its font scale in steps (re-wrapping at each step, since fewer/longer lines fit at a smaller scale) until the whole wrapped block fits inside its box's height, rather than just wrapping to width and letting a long description spill out the bottom
- **Current Cards** *(first-pass numbers, pending a balance pass)* - all 10 now have a matching `Units.Summons.*` class off `BaseUnit`, each using its own summon-only `Race` (not playable, grants no Race Perk bonus/vulnerability) and `HeroClass.None` (no weapon specializations), with stats copied straight from the card. None are spawned into the active session yet - no summon-to-battlefield system exists (see Portal System below for the mana/placement rules that system will need to honor once built). Only Dusk Roachlin has unique art (`DuskRoachlin_CardArt1`); the other 9 share a placeholder (`GenericSummon_CardArt`) until real art exists, and none have sprite art yet either (`SpriteAssetPath` left unset - `BaseUnit` already no-ops gracefully on a missing sprite file):
  - **Dusk Roachlin** - 5 mana, ATT 8 / INT 3 / DEF 6 / RES 4 / Accuracy 7 / Evasion 6 / Speed 9, 3-12 damage, 12 HP. `Race.Roachlin`, innate move **Roachlin Slash** (Sharp, STR/5)
  - **Aetherfluff Beetle** - 1 mana, ATT 0 / INT 2 / DEF 4 / RES 4 / Accuracy 5 / Evasion 8 / Speed 5, 0 damage, 10 HP. A pure support creature - generates 5 mana for every allied unit within 4 tiles at the start of each turn (not implemented yet, no per-turn aura/mana-generation system exists). `Race.AetherfluffBeetle`, innate move **Nudge** (0 damage - it isn't built to fight)
  - **Dusk Roachlin Priest** - 15 mana, ATT 4 / INT 11 / DEF 5 / RES 10 / Accuracy 7 / Evasion 6 / Speed 5, 3-9 damage, 18 HP. Heals a random ally at the start of its turn (double for Roachlin targets) - not implemented, no AI-side heal-cast behavior exists yet. `Race.DuskRoachlinPriest` (its own Race, not shared with Dusk Roachlin, since `RaceAttacks` only holds one Move per Race and this unit needs a distinct ranged magic attack), innate move **Shadow Weave** (Magical, range 3, INT/3.5)
  - **Bearat** - 8 mana, ATT 12 / INT 2 / DEF 9 / RES 4 / Accuracy 7 / Evasion 5 / Speed 6, 6-16 damage, 24 HP. `Race.Bearat`, innate move **Maul** (Blunt, STR/2.4)
  - **Mireback Slogger** - 7 mana, ATT 10 / INT 2 / DEF 11 / RES 4 / Accuracy 6 / Evasion 3 / Speed 4, 5-14 damage, 26 HP. `Race.MirebackSlogger`, innate move **Shove** (Blunt, STR/2.2, knockback 1 base + 3/tile)
  - **Lanternmoth Cinderwing** - 6 mana, ATT 3 / INT 8 / DEF 4 / RES 8 / Accuracy 7 / Evasion 7 / Speed 7, 2-7 damage, 12 HP. Spreads small mana/calm bursts to nearby allies (not implemented, same aura gap as Aetherfluff Beetle). `Race.LanternmothCinderwing`, innate move **Cinder Dust** (Magical, range 3, INT/3.2)
  - **Brambleboar** - 6 mana, ATT 11 / INT 2 / DEF 7 / RES 4 / Accuracy 7 / Evasion 5 / Speed 6, 5-15 damage, 20 HP. `Race.Brambleboar`, innate move **Gore** (Sharp, STR/2.2, 35% chance to inflict Minor Bleeding)
  - **Siltfin Mawpike** - 5 mana, ATT 9 / INT 3 / DEF 6 / RES 5 / Accuracy 8 / Evasion 6 / Speed 8, 4-12 damage, 16 HP. `Race.SiltfinMawpike`, innate move **Snap Bite** (Sharp, STR/2.25)
  - **Gloamraven Ossuary** - 7 mana, ATT 8 / INT 6 / DEF 5 / RES 6 / Accuracy 10 / Evasion 8 / Speed 9, 3-11 damage, 14 HP. `Race.GloamravenOssuary`, innate move **Precision Strike** (Sharp, STR/2, elevated 30% crit chance)
  - **Rootmoss Stonegloom** - 9 mana, ATT 9 / INT 3 / DEF 12 / RES 7 / Accuracy 6 / Evasion 3 / Speed 3, 5-13 damage, 30 HP. `Race.RootmossStonegloom`, innate move **Stone Slam** (Blunt, STR/2.25)

#### Portal System *(design only - not implemented in code yet)*
- **Concept**: every summon requires an active portal on the field before it can be summoned; portals themselves cost mana and a placement tile, adding a strategic layer (an active portal can be dispelled, denying further summons from it until a new one is made)
- **Generic Portal**: covers all 10 cards in `SummonCatalog` above, since none of them have a special requirement. Any class can open one - not gated by HeroClass, same as the summons themselves
  - The first time a caster plays ANY generic summon card, before the summon resolves they must first pick a tile to place their Generic Portal on (their own targeting/placement step, separate from choosing the summon's own arrival tile)
  - That first cast costs the summon's own mana cost **plus a one-time +5 mana** for the portal itself - e.g. summoning a Dusk Roachlin (5 mana) the very first time costs 5 + 5 = 10 mana total
  - Once a caster's Generic Portal is active, every subsequent generic summon from that caster costs only its own listed mana cost - no repeated +5 charge - as long as that portal survives
  - If the portal is dispelled, the next generic summon cast re-triggers the whole placement-and-+5-mana sequence (a new portal has to be opened again)
- **Special Portals** *(future work, design only)*: some summons will require a specific portal type instead of (or in addition to) the Generic Portal - e.g. a **Dark Aethereal Portal** or **Light Aethereal Portal** - each granted by its own dedicated Spell Card rather than being automatic like the Generic Portal, and each may carry its own cast requirements (e.g. class, stat threshold, or location). No such cards, creatures, or portal types are defined yet - this is a placeholder for a later pass once the Generic Portal mechanic above actually exists in code
- **Not yet implemented**: none of this is wired up - there's no portal entity/tile-ownership tracking, no placement-targeting mode (would mirror the existing teleport/ally-shield targeting-mode pattern in `Game1`), no dispel mechanic, and no summon-to-battlefield spawning at all yet. This section exists purely so the mana/placement rules are written down before any of it is built

#### Aggro System
- **Range**: 0-20 aggro per enemy
- **Mechanics**:
  - Attack = +5 aggro on target
  - Take damage = +2 aggro on attacker
  - Heal ally = +3 aggro on healed
- **Forced Targeting**: At ≥10 aggro, must target that enemy (unless blocked by mechanics)
- **Reset**: Aggro resets between map sections/chapters

#### Stats
- **HP**: Health points. Every living unit now shows an actual HP bar above its sprite (there was previously only plain "Name: HP/MaxHP" text, which stays too) - a dark background sized to MaxHP, filled HP on top shaded red-to-green as it drops
  - **Health Shields**: `BaseUnit.ShieldPoints` is a generic damage-absorption pool any "shield" effect can grant via `ApplyShield(amount, drainsMana)` (currently just Mana Shield) - `ApplyRawDamage` drains ShieldPoints before touching HP at all, so a shielded unit takes zero HP loss until the shield itself runs out, and only the remainder spills over to HP. The HP bar reflects this directly: the shield doesn't overlap the HP portion, it extends the bar past the MaxHP mark - a unit at 50/100 HP with a 20-point shield draws as [50 filled][50 dark/missing][20 blue], and a full-HP unit with the same shield just appends the 20 blue points past the already-full bar. `ApplyShield` takes the larger of any existing shield and the new amount (doesn't downgrade a bigger one already active), and `ShieldDrainsMana` (true for Mana Shield specifically) drains CurrentMP in lockstep as the shield absorbs hits, clearing itself automatically once the shield breaks - verified against all of this with a 19-assertion test run through the compiled assembly directly (full-HP absorb, partial-HP spillover, mana lockstep + auto-break, manual toggle-off via ClearShield, and the no-downgrade rule) before calling it done
- **MP**: Mana/magic points; implemented on BaseUnit (MaxMP/CurrentMP) as a pool per class. Unlike AP (full refill every turn), MP regenerates a flat amount per turn via `BaseUnit.RegenMana()` (called from `TurnSystem.OnUnitTurnStart`, same place AP resets), tiered by Class through `ClassCatalog.GetManaRegenPerTurn` rather than set per-instance - **Sorcerer 4/turn** (primary spellcaster) > **Cleric 3/turn** (support caster) > **Warrior/Hunter 1/turn each** (not casters, tied lowest) > **None 0/turn** (summoned creatures - see Dusk Roachlin). First-pass placeholder numbers, pending a balance pass
  - **What Spends It**: every Spell/Summon Card's MP cost (see Card/Deck System), plus only the actual MAGIC weapon attacks - Frost Blast (Staff) and Arcane Missile (Wand), both Sorcerer-only SpecialistAttacks. Their base counterparts, Bonk and Whack, cost no MP at all (anyone can swing a stick for free) - nothing else in the game costs MP
  - **Gating**: both the Attack menu and the Cards hand show a move/card's MP cost in red whenever the player unit can't currently afford it (same red/white convention the movement range already used for AP). A weapon attack always deducts AP/MP on use; a spell card does too, but only for the subset that actually casts right now - see Card/Deck System's Casting note for which
  - **HUD**: current/max MP is shown under the AP counter whenever it's the player's turn
- **AP**: Action points per turn
- **Speed**: Turn order priority (higher = earlier) - see EFFECTIVE Speed under Turn Order; a Slowed unit's effective Speed is reduced
- **Strength**: Adds to physical attack damage (additive per-move bonus, e.g. Punch = 1 + STR/5 - see Race-Based Attacks). What combat math actually reads is EFFECTIVE Strength - raw Strength plus this unit's racial bonus, if its Race grants one (see Race Perks)
- **Intelligence**: Adds to magic attack damage (additive per-move bonus, e.g. Arcane Missile = 14 + INT/3 - see Race-Based Attacks); magical weapon moves scale with this instead of Strength
- **Defense (DEF) / Resistance (RES)**: physical/magical damage reduction. Both now use a diminishing-returns curve - the same "asymptotic armor" family World of Warcraft and Skyrim use - rather than a flat clamped percentage: `Mitigation% = 100 * Stat / (Stat + 100)`. At Stat == 100, mitigation is exactly 50%; no amount of DEF/RES alone ever reaches 100% (unlike the old flat-percentage system), so stacking it keeps paying off but with steadily smaller returns, and there's real headroom for gear/leveling to grow into later. The raw stat fed into this curve already includes this unit's racial DEF/RES bonus, if its Race grants one (see Race Perks). A shield's passive/Guard bonus (see Guard Command) is a separate, flat percentage-point layer added AFTER this curve, not run through it
- **Aggression**: How easily unit draws enemy attention
- **Accuracy**: Adds +0.1% hit chance per point, on top of a move's base accuracy; total hit chance is allowed to exceed 100% (no upper cap) - that headroom is intentional, since accuracy-lowering effects (e.g. Blind) are planned
- **Evasion**: fully wired in now - subtracts 0.1% from the attacker's hit chance per point, the exact mirror of Accuracy. What combat math reads is EFFECTIVE Evasion - raw Evasion plus this unit's racial bonus, if any. Current per-hero values: Sorcerer 4, Warrior 1, Hunter 6, Cleric 4
- **Vision Range**: How far unit can see (5-7 tiles typical)

#### Race Perks
- **Concept**: each playable race grants a small stat bonus (or two) alongside a specific elemental vulnerability - applied automatically to EVERY unit of that race, not hand-set per hero, so a future unit of the same race inherits both without extra work
- **Vectium**: +5 Evasion; takes +2% more damage from Electric attacks
- **Human**: +7 Resistance; takes +4% more damage from Poison
- **Lethios**: +2 Strength, +2 Defense; takes +10% more damage from Frost
- **Vulnerability Timing**: the bonus damage is applied BEFORE normal DEF/RES mitigation - so a vulnerable unit's own armor still mitigates a portion of the inflated amount, rather than the bonus being tacked on untouched after

#### Status Effects
- **Ranks**: Every status effect is graded on a 5-step severity scale: Weak, Minor, Moderate, Major, Severe - currently only Bleeding actually uses this; Stun/Knockdown/Slowed have their own "length" set by whatever inflicted them instead (see below), not a severity rank
- **Bleeding**: Damage over time as a % of MAX HP (changed from current HP), scaling with rank (Weak 1% -> Severe 5%); inflicted by Bite (Moderate), Swipe (Minor), and most Sharp weapon moves (Weak, small chance). Blocked outright (0% chance) if the target's Defense beats the attacker's Strength - see Race-Based Attacks
- **Faint**: Triggered automatically once HP drops to 5% of max HP or below (clears again if healed back above it). A forced, unbreakable version of Stun - no AP is granted and there's no way to break out of it; the unit's turn is skipped outright every time until HP recovers. If all of a player's units are Fainted, that player is eliminated (see Victory Condition)
- **Stunned**: skips the unit's own upcoming turns - how many is set by whatever inflicted it (e.g. Shield Bash: 1 turn), not a severity rank. Unlike Faint, a Stunned (not Fainted) unit still gets AP on its turn, but the only choices available are attempting to **Break Stun** (3 AP, clears it early and lets the unit act normally with whatever AP remains) or ending the turn without acting. Break Stun is restricted to "Summoner" units (anyone with a real Class - the 4 hero classes today, more later) and has its own 5-turn cooldown after use; summoned creatures can't break out at all
- **Knocked Down**: cannot move, and the unit's own accuracy is cut by 75% on anything it attempts while down. Cleared by spending AP to stand back up - the cost is set by whatever knocked it down (Punch: 1 AP), not a fixed global amount. Chance to inflict: 80% if the attacker's Strength beats the target's Strength + Defense ("Armor"), 2% otherwise (currently only Punch can cause this)
- **Slowed**: reduces EFFECTIVE Speed by a %, for a set number of turns - both set by whatever inflicted it (Frost Blast: -40%, 2 turns). Lowers both turn-order priority (a Slowed unit acts later starting the following Turn) and tiles-per-AP
- **Rooted**: Cannot move; can still attack/cast
- **Frozen**: Reduced movement speed; slower attacks
- **Silenced**: Cannot cast spells; can still use skills
- **Bound**: Reduced action speed
- **Cursed**: Damage output reduced
- **Fear**: Reduced accuracy; chance to skip turn
- **Blind**: Cannot see beyond 2 tiles; reduced accuracy

#### Fog of War
- **Vision Ranges**: 
  - Sorcerer: 5 tiles
  - Cleric: 5 tiles
  - Hunter: 7 tiles
  - Warrior: 4 tiles
- **Silhouette Zone**: Base range +1 to +4 tiles (see vague outline)
- **Revelation**: Hit by attack/spell = fully visible for 1 turn
- **Visualization**: Full color (known) → Greyscale (explored) → Black (unknown)

#### Skill Shots
- **Mechanics**: Directional abilities; no line-of-sight required
- **Friendly Fire**: ENABLED (careful with AoE)
- **Detection**: Hitting any unit reveals it for 1 turn
- **Cone AOE - First Implementation (Frost Blast)**: aimed at a direction rather than a specific unit - click any hex within range to set the direction and fire. The cone is a real 60-degree hex sector (narrow at range 1, naturally widening with distance) rather than an approximation, and hits every living unit inside it out to the move's range. A live preview highlights the exact hexes that would be hit as the mouse moves, before committing

#### Attack Targeting & Preview
- **Range Indicator**: selecting an attack closes the menu and shows a "select a target, or press E to return" prompt; every hex within the move's actual range (respecting any specialist range bonus, e.g. the Cleric's mace) lights up faintly, with actual valid targets highlighted in red on top of that
- **Live Preview Panel**: hovering a valid target shows its exact Damage, Hit%, Crit% (or "100% (backstab)" if guaranteed), and any status effect/Knockdown chance the move could inflict - computed from the same formulas the attack actually resolves with, not a separate estimate
- **No Silent Failures**: picking a move that's unaffordable (AP/MP), out of ammo, or has nothing in range shows a specific message explaining why, rather than the menu just doing nothing

#### Victory Condition
- **Format**: 4-player free-for-all - each player is treated as a "team" of one (no real team/faction system exists yet)
- **Elimination**: a unit is eliminated once Fainted (not necessarily dead - HP can still be above 0); the match ends the moment only one player's unit is left un-Fainted, and that player wins (a simultaneous last-two-standing situation is a Draw)

#### AI (Placeholder)
- **Current Behavior**: every non-player-controlled unit now attacks as well as moves. Each turn, it first checks for an attack (skipping the walk-toward-player step entirely if one's already available), then moves toward the player if nothing was in range, then checks for an attack again afterward (in case it just closed into range) - so it gets at most one attack attempt before moving and one after, never a multi-attack chain even with AP left over
- **Attack Selection**: among every move the unit can currently afford (AP, MP, and ammo all checked) with the player within that move's effective range, it picks whichever deals the most raw damage - no weighting for accuracy, status effects, or positioning yet, and `HitsAllAdjacent`/cone moves are handled the same way a player's are (aimed straight at the player's direction for a cone)
- **Still Player-Only Targeting**: even though this is a true 4-player FFA, the AI only ever targets the player specifically - it doesn't yet attack other AI units, so AI-vs-AI combat doesn't happen on its own
- **Also Handles**: a Stunned AI unit always attempts to Break Stun; a Knocked Down one always stands back up; movement animates the same way a player's does; a resolved AI attack plays the same slash/shooting effect and combat log message a player's attack would (see Weapon Attack Animations and Attack Targeting & Preview)
- **Not Yet Implemented**: target prioritization/threat assessment, attacking anyone other than the player, weighing anything besides raw damage, and any deeper tactical behavior (positioning, retreating, focus-fire) - still a separate, later pass

#### Weapon Attack Animations
- **Concept**: every move plays one of two simple visual effects on resolution, purely presentational (`AttackAnimationType`, set per-move) - it doesn't affect resolution at all
- **Slash**: a short swipe drawn near the target, used for melee weapon/racial moves (Punch, Bite, Swipe, Sword Slash/Pierce/Spin, Dagger Stab, Shield Bash, Mace Bash, Bonk, Whack, ...)
- **Shooting**: a projectile drawn traveling from the attacker's position to the target, used for anything ranged (Arrow Shot, Bolt Shot, Pistol Shot, Throw Dagger, Arcane Missile, Frost Blast) - travel duration scales slightly with distance rather than being fixed
- **Death Visuals**: a dead unit's sprite is drawn with a flat gray color tint (an approximation, not true desaturation - the project has no shader/Effect pipeline yet) and its name/HP label switches to "`{Name}: DEAD`" in dark red, distinct from a merely-Fainted unit (which keeps its normal sprite and shows a gray HP label instead)

### 4.2 Class System

#### Base Classes (4)
- **Sorcerer**: High Intelligence, low Armor; ranged magic dealer
- **Warrior**: High Strength, high Armor; melee tank
- **Cleric**: Balanced stats; healer/support
- **Hunter**: High Speed, high Evasion; ranged physical dealer

#### Class Evolution Tiers (5 total)
- **Tier 1 (Lvl 1-19)**: Apprentice classes
  - ApprenticeSorcerer (45 HP, Speed 6)
  - ApprenticeWarrior (60 HP, Speed 4)
  - ApprenticeCleric (50 HP, Speed 5)
  - ApprenticeHunter (55 HP, Speed 8)
- **Tier 2 (Lvl 20-49)**: Journeyman classes (stats increase)
- **Tier 3 (Lvl 50-74)**: Expert classes (special abilities unlock)
- **Tier 4 (Lvl 75-99)**: Master classes (ultimate skills available)
- **Tier 5 (Lvl 100)**: Transcendent/Sage classes (legendary power)

#### Hero Units (~30 total)
- **Unique Units**: Named characters with custom stats, abilities, storylines
- **Some Playable**: Can be recruited and controlled
- **Some Mini-Bosses**: Story encounters; defeatable for rewards
- **Some Unnamed Templates**: Generic units for player customization

#### Multi-Classing
- **Mechanic**: Units can combine abilities from 2 classes (Vicent = Sorcerer/Cleric)
- **Balance**: Stats adjusted; benefits and limitations
- **Strategy**: Allows creative team compositions

### 4.3 Equipment & Progression
- **Equipment Slots**: Weapon, Armor, Accessory (at minimum) - "Armor" as a real equipment slot doesn't exist yet; the STR-vs-(STR+DEF) Knockdown formula uses raw Defense as a stand-in for it until it does
- **Weapon Inventory**: fully implemented now - see Inventory & Weight under Combat System §4.1 (20 weight capacity, per-weapon weights, ammo for ranged weapons)
- **Leveling**: Experience from combat; stat growth per level
- **Abilities**: Learned via class progression or equipment bonuses
- **Cards**: Gained from treasure chests, enemy drops, crafting

### 4.4 Map Features
- **Terrain Types**: Grass, Water, Mountain, Forest, Desert, Stone, Dirt
- **Passability**: Different unit types traverse differently. Currently implemented: Mountain is impassable, Water is passable but costs +1 AP to cross, everything else (Grass, Forest, Desert, Stone, Dirt) is normal cost - see Grid & Movement
- **Roofed Areas**: Blocks flying units' vision advantage
- **Objects**: Trees, buildings, NPCs, treasure chests
- **Environmental Hazards**: Lava, ice, unstable ground (TBD specifics)

---

## 5. STORY STRUCTURE

### Acts
- **Act 1**: Introduction; meet Vicent and Retainer; uncover first conspiracy
- **Act 2**: Gather allies; explore factions; moral choices emerge
- **Act 3**: Confrontation; truth about cataclysm revealed
- **Act 4**: Final confrontation with Shane; world's fate decided
- **Epilogue**: Resolution based on player choices

### Branching Narrative
- **Faction Choices**: Align with Sun & Scale, Draconics, or remain neutral
- **Character Decisions**: Dialogue choices affect NPC relationships and recruitment
- **Endings**: Multiple endings based on:
  - Which faction player supported
  - Which characters recruited
  - Which Sages found
  - Whether Sandor becomes ally or boss fight

### AI Dialogue System & AI-Controlled NPCs

#### Real-Time NPC Dialogue Generation
- **Technology**: Claude API real-time generation
- **NPC Personality**: Mood system (😊😐😕😠🤐) reflected in dialogue tone and behavior
- **Context**: Character background + game state + conversation history (last 10 exchanges) + relationship level
- **Output**: Capped at 150 words per response for pacing
- **Storage**: Persistent JSON relationships per save file (affects NPC reactions in future encounters)

#### AI-Controlled NPCs & Reactive World
- **NPC Autonomy**: NPCs have independent goals, routines, and emotional states
- **Dynamic Reactions**: NPC dialogue and behavior change based on:
  - Previous player interactions (trust/reputation level)
  - Player faction choice (supports same faction or actively opposes)
  - In-game events (NPC witnessed player actions, consequences)
  - Current emotional state (mood system influences patience, generosity, hostility)
  - Relationship level (stranger → acquaintance → ally → best friend or enemy → hated foe)
  - NPC goals vs. player goals (aligned = friendly; conflicting = hostile)
- **Replay Value**: NPCs remember player choices; subsequent playthroughs feature different NPC attitudes, availability, and quest offerings
- **Conflict & Alliance**: NPCs can refuse quests, leave party, or become enemies based on player morality/choices
- **Multi-Path Quests**: Same NPC might offer different quests based on relationship and prior interactions

#### Player Dialogue Input (Text & Voice)
- **Text Input**: 
  - Player types dialogue responses directly
  - System parses intent and generates context-aware NPC reactions
  - Multiple valid response paths; no "wrong" answer (but consequences vary)
  
- **Voice Input** (Phase 2+): 
  - Optional voice-to-text (Vosk integration planned)
  - Player speaks dialogue aloud; AI transcribes and generates NPC response
  - Adds immersion and accessibility
  - Optional toggle; text input always available
  
- **Dialogue Tone Detection**:
  - **Friendly tone** → NPC becomes more open, offers better deals, shares secrets
  - **Hostile tone** → NPC becomes defensive, may call for guards, refuse to trade
  - **Deceptive tone** → NPC makes perception check; if failed, believes lie; if successful, becomes suspicious
  - **Insults/Disrespect** → Reputation hit, NPC refuses to trade/quest, or confronts player
  - **Sincere apology** → Can repair damaged relationships (with effort and time)
  
- **Consequence System**:
  - Player dialogue choices create branching paths
  - Harsh words to one NPC might impress another
  - Recruiting certain NPCs may offend rival factions
  - Dialogue choices affect hidden "alignment" score (good/neutral/evil)

---

## 6. TECHNICAL STACK

### Engine & Language
- **Language**: C# (.NET 8.0.424)
- **Framework**: MonoGame 3.8.5.1
- **Database**: SQLite + JSON
- **Platform Targets**: PC (Windows/Linux/macOS), Mobile (later)

### Development Environment
- **OS**: Ubuntu 26.04 Linux
- **IDE**: Visual Studio Code / JetBrains Rider
- **.NET Version**: 8.0.424 (installed via dotnet-install.sh, not snap)
- **SDL2 Libraries**: libsdl2-dev, libsdl2-image-2.0-0, libsdl2-mixer-2.0-0, libsdl2-ttf-2.0-0

### File Organization
```
SagesOfOzvaram/
├── Units/
│   ├── BaseUnit.cs
│   ├── Race.cs
│   ├── HeroClass.cs
│   ├── Heroes/
│   │   ├── ApprenticeSorcerer.cs
│   │   ├── ApprenticeWarrior.cs
│   │   ├── ApprenticeCleric.cs
│   │   └── ApprenticeHunter.cs
│   └── Summons/
│       └── DuskRoachlin.cs
├── Combat/
│   ├── TurnSystem.cs
│   ├── Move.cs
│   ├── Weapon.cs
│   ├── WeaponCatalog.cs
│   ├── RaceAttacks.cs
│   ├── RaceCatalog.cs       (per-race stat bonus + elemental vulnerability)
│   ├── ClassCatalog.cs
│   ├── AttackResolver.cs    (targeting → hit/crit/damage/status/knockback resolution)
│   ├── AmmoType.cs
│   ├── AttackAnimationType.cs (Slash/Shooting - see Weapon Attack Animations)
│   ├── SpellCard.cs
│   ├── SpellCatalog.cs
│   ├── SummonCard.cs
│   ├── SummonCatalog.cs
│   ├── WeaponType.cs
│   ├── StatusRank.cs
│   ├── BleedEffect.cs
│   └── DamageType.cs
├── Maps/
│   ├── HexGrid.cs
│   ├── HexDirection.cs
│   ├── HexGridRenderer.cs
│   ├── Pathfinder.cs
│   ├── MapData.cs
│   ├── MapGenerator.cs
│   ├── MapSerializer.cs
│   └── DevConsole.cs
├── Content/
│   ├── imgs/sprites/units/
│   │   ├── ApprenticeSorcerer/Sorcerer_Sprite_1.png
│   │   ├── ApprenticeWarrior/Warrior_Sprite_1.png
│   │   ├── ApprenticeCleric/Cleric_Sprite_1.png
│   │   └── ApprenticeHunter/Hunter_Sprite_1.png
│   ├── imgs/Avatar/
│   │   ├── ApprenticeSorc_Avatar.png
│   │   ├── ApprenticeWarrior_Avatar.png
│   │   ├── ApprenticeCleric_Avatar.png
│   │   └── ApprenticeHunter_Avatar.png
│   ├── imgs/Cards/Spells/
│   │   ├── SpellCard.png       (shared template - see Spell Cards §4.1)
│   │   ├── ArcaneBolt_CardArt.png
│   │   └── *_CardArt.png       (54x - one per Generic/Sorcerer/Warrior/Hunter spell except Arcane Bolt - programmatic "AI PLACEHOLDER"-watermarked art, see Spell Cards §4.1)
│   ├── imgs/Cards/Summons/
│   │   ├── SummonCard.png      (shared template - see Summon Cards §4.1)
│   │   └── DuskRoachlin_CardArt1.png
│   ├── Fonts/DefaultFont.spritefont  (Liberation Sans, Arial-metric-compatible; built via Content.mgcb)
│   └── Content.mgcb
├── Game1.cs
└── Program.cs
```

### Asset References
- **Sprites**: PNG format, path-based registry
- **3D Models**: FBX format (for later expansion)
- **Maps**: JSON serialization (save/load)
- **Fonts**: MonoGame .spritefont XML files

### Key APIs & Libraries
- **AI Dialogue**: Claude API via HttpClient
- **Procedural Generation**: Perlin noise (custom implementation)
- **Hex Grid Math**: Custom HexGrid class with pointy-top odd-row offset

---

## 7. DEVELOPMENT STATUS

### Phase 1: Foundation (Current - In Progress)
**Goal**: Playable prototype with core systems

#### ✅ COMPLETED
- Project structure and folder organization
- HexGrid system (pointy-top, odd-row offset)
- Procedural map generation (Perlin noise with FBM)
- Base tile rendering (grass, water, mountain, forest, desert, stone, dirt)
- Unit class hierarchy (BaseUnit + 4 apprentice heroes)
- Sprite loading from disk
- Unit spawning in corners (avoiding water/mountains)
- Sprite scaling per-unit
- Turn order system (Speed-based, now off EFFECTIVE Speed - see Status Effects/Slowed)
- Camera pan/zoom to units per turn, now continuously following (not just once per turn) including mid-walk and mouse-hover during Move mode
- Auto-advance between units every 3 seconds
- WASD camera controls, deltaTime-scaled (frame-rate independent - fixed a jitter bug where panning speed was tied to frame rate)
- Map Editor Dev Console (basic tile/object commands)
- Font system (text renders throughout the UI - AP/HP display, menus, combat log, tooltips)
- Turn announcement UI ("TURN X START" with fade animation)
- Movement: click-to-move with pathfinding, AP cost display, live path preview, and now real tile-by-tile walk animation (not an instant teleport)
- **Full attack system**: targeting (single-target click, HitsAllAdjacent, and a real cone-AOE aim-by-direction mode), hit/miss rolls, crit, damage (including per-weapon AttackPower, DEF/RES diminishing-returns mitigation, Evasion), knockback/thrust movement, weapon switching, ammo consumption, range indicator + live damage/hit%/crit/affliction preview panel, a combat log, and a simple per-move Slash/Shooting visual effect (see Weapon Attack Animations)
- Status effects: Bleeding (now off MAX HP), Stunned (with Break Stun + cooldown), Knocked Down, Slowed, and Faint (forced unbreakable stun) - all fully resolved, not just data
- Inventory weight system (20 capacity, per-weapon weights) and a per-weapon ammo system (Arrow/Bullet/Bolt)
- Race Perks (per-race stat bonus + elemental vulnerability, applied automatically)
- AI now attacks as well as moves (highest-damage affordable in-range move, before and after moving each turn - still player-targeting only, see AI (Placeholder))
- Turn-start announcement now genuinely blocks all turn logic (AI and player alike) until it clears, every turn - not just cosmetic, and not just Turn 1
- Death visuals: a dead unit's sprite tints gray and its label reads "DEAD," distinct from Fainted
- FFA victory condition (last player with a non-Fainted unit wins)
- Health display on units (name + HP/MaxHP text)

#### 🔄 IN PROGRESS
- UI polish (turn display, current unit info)
- AI: attacks and moves now, but still only targets the player and only weighs raw damage - target prioritization and any deeper tactical behavior is still ahead

#### ⏳ TODO (Phase 1 remaining)
- [ ] Pixelated grass sprites/textures on tiles
- [ ] AI targeting other AI units (true FFA behavior, not just player-focused)
- [ ] Sound effects & music (basic)

### Phase 2: Gameplay Core (Next)
- [ ] Spell/card execution (currently presentation/deck-state only - see Card/Deck System)
- [ ] Status effects still unimplemented: Rooted, Frozen, Silenced, Bound, Cursed, Fear, Blind
- [ ] Aggro system implementation
- [ ] Fog of War visualization
- [ ] Map Editor UI (palette, inspector, toolbar)
- [ ] More unit classes & progression
- [ ] Save/Load game state
- [ ] Story intro & tutorial
- [ ] Real teams/parties (combat is currently a 4-player FFA, no grouping)

### Phase 3: Content & Polish (Planned)
- [ ] Full story campaign (Acts 1-4)
- [ ] NPC dialogue system (Claude API integration)
- [ ] Character recruitment & relationships
- [ ] Multiple maps & environments
- [ ] Balanced difficulty progression
- [ ] Visual & audio polish
- [ ] Performance optimization

### Phase 4: Expansion (Post-Launch)
- [ ] Mobile platform (iOS/Android)
- [ ] Multiplayer/PvP modes
- [ ] DLC/expansions
- [ ] Voice acting & cinematics
- [ ] Mod support

### Development Schedule
- **Current**: Part-time (10 hrs/week) until Nov 2026
- **Nov 2026 onward**: Full-time development
- **Target Launch**: December 2028

---

## 8. DEV JOURNAL

### Session 1: Foundation & GDD (Date: Initial)
**Duration**: ~4 hours  
**Goals**: Establish project structure, create GDD, begin tech stack setup

**Accomplishments**:
- [x] Created comprehensive Game Design Document
- [x] Established core lore: Ozvaram world, 3 races, 3 factions
- [x] Defined protagonist (Vicent) and key characters
- [x] Documented game mechanics: grid combat, turn order, deck system, aggro
- [x] Set up .NET 8.0.424 environment on Ubuntu 26.04
- [x] Installed SDL2 libraries for MonoGame
- [x] Created MonoGame project structure
- [x] Folder organization: Units/, Combat/, Maps/, Content/

**Challenges**:
- SDL2 sandbox issues with snap .NET (switched to dotnet-install.sh)
- MonoGame template setup required specific version (3.8.5.1)

**Notes**: 
- Strong foundation laid; team ready to begin implementation
- GDD serves as north star for all future decisions

---

### Session 2: Map System & Hex Grid (Date: Following)
**Duration**: ~6 hours  
**Goals**: Implement hex grid rendering, procedural map generation

**Accomplishments**:
- [x] Implemented HexGrid class
  - Pointy-top orientation
  - Odd-row offset coordinate system
  - HexToWorld & WorldToHex conversions
  - Vertex calculation for hex rendering
- [x] Implemented HexGridRenderer
  - Filled hex rendering with outline
  - Camera pan & zoom controls (WASD + Scroll)
  - Screen-to-world coordinate mapping
- [x] Created MapData classes (Tile, MapObject, Map, AssetRegistry)
- [x] Implemented MapGenerator with Perlin noise
  - Fractional Brownian Motion (FBM) for natural terrain
  - Threshold-based biome assignment
  - Seeded random generation
- [x] Added DevConsole for testing (tile/object commands)
- [x] Tile coloring based on terrain type
- [x] Map save/load serialization (JSON)

**Technical Details**:
- Hex grid math: `_hexWidth = sqrt(3) * TileSize`; `_hexHeight = 2 * TileSize`
- Row offset: `_hexVerticalOffset = _hexHeight * 0.75f`
- Vertex angles: `Math.PI / 3f * i + Math.PI / 6f` (30° offset for pointy-top)
- Perlin noise implementation: Custom class with permutation table

**Challenges**:
- Initial SDL2 rendering issues (resolved via proper initialization order)
- Hex coordinate math required careful testing & iteration
- Perlin noise permutation table indexing had out-of-bounds issues (fixed with & 255 masking)

**Testing**:
- [x] Hex grid renders correctly with proper spacing
- [x] Camera controls responsive and smooth
- [x] Procedural maps generate consistently with seeded randomness
- [x] Tile colors display as expected

---

### Session 3: Unit System & Sprite Integration (Date: Following)
**Duration**: ~5 hours  
**Goals**: Create unit classes, load sprite assets, position units on map

**Accomplishments**:
- [x] Created BaseUnit abstract class
  - Properties: Name, HP, MaxHP, Speed, Position, Scale
  - Methods: LoadContent, Draw, TakeDamage, Heal, IsAlive check
  - Sprite loading from disk (fallback if ContentManager fails)
- [x] Implemented 4 Apprentice unit classes
  - ApprenticeSorcerer (45 HP, Speed 6)
  - ApprenticeWarrior (60 HP, Speed 4)
  - ApprenticeCleric (50 HP, Speed 5)
  - ApprenticeHunter (55 HP, Speed 8)
  - Each with correct sprite path mapping
- [x] Created unit spawning system
  - Spawn in 4 corners of map
  - Avoid water & mountain tiles
  - Expanding search radius for valid spawn positions
- [x] Sprite rendering
  - Direct file loading from Content/ folder
  - Per-unit scale multiplier (0.15x base for proper sizing)
  - Centered positioning on hex tiles

**Technical Details**:
- Sprite paths: `Content/imgs/sprites/units/[UnitName]/[ClassName]_Sprite_1.png`
- Base scale: 0.15f; adjusted per-unit via Scale property
- File-based loading: `Texture2D.FromStream(graphicsDevice, stream)`

**Challenges**:
- Initial sprites were too large (1.5x scale)
- Sprite filenames didn't match unit class names (fixed naming)
- Different artists' sprites have varied dimensions (solved with per-unit scale)

**Testing**:
- [x] 4 units spawn in correct corners
- [x] Units avoid water/mountains reliably
- [x] Sprites render at appropriate sizes
- [x] Sprite scaling maintains visual hierarchy (Sorcerer larger than others)

---

### Session 4: Turn System & Camera Animation (Date: Latest)
**Duration**: ~4 hours  
**Goals**: Implement turn order, auto-cycling, camera animations, announcements

**Accomplishments**:
- [x] Created TurnSystem class
  - Speed-based turn order (Hunter → Sorcerer → Cleric → Warrior)
  - Turn counter incremented after all units cycle
  - Unit timer tracks time per unit (3 seconds default)
  - Turn start timer for announcements
  - GetTurnOrderText() for UI display
- [x] Integrated turn system into Game1
  - Auto-advance every 3 seconds (single trigger per unit)
  - Smooth camera pan/zoom to current unit
  - Easing function for smooth interpolation (smoothstep)
  - Camera zoom in (2.5x) on active unit
- [x] UI announcements
  - "TURN X START" announcement at turn boundaries
  - Fade in (0.3s) → visible (1.4s) → fade out (0.3s)
  - Semi-transparent black background box
  - White text, centered near top of screen
- [x] Fixed WASD controls
  - W = up, S = down, A = left, D = right (was reversed)

**Technical Details**:
- Turn order: `OrderByDescending(u => u.Speed)`
- Camera easing: smoothstep formula `progress * progress * (3 - 2 * progress)`
- Fade timing: Lerp with MathHelper.Clamp for smooth animations
- Auto-advance flag prevents multiple NextUnit() calls per unit

**Challenges**:
- Auto-advance was triggering every frame (fixed with boolean flag)
- TurnStartElapsed resetting incorrectly (fixed to only reset on new turn)
- Arial font not loading from Content (resolved with .spritefont setup)
- Text rendering issues (SpriteBatch.Begin/End brace balance)

**Known Issues**:
- [ ] Font rendering not displaying (Arial.spritefont integration in progress)
- [ ] Turn announcements not visible on screen (font loading needed)
- [ ] UI text missing (same font issue)

**Next Steps**:
1. Resolve font loading (add Arial.spritefont to Content.mgcb)
2. Verify turn announcement renders correctly
3. Add persistent UI elements (current turn, turn order, unit info)
4. Test turn cycling through multiple complete cycles

---

### Session 5: The Attack System, End to End
**Goals**: Take combat from data-only to a fully playable 4-player FFA - targeting, resolution, every weapon's real numbers, and the systems those numbers needed to mean anything

**Accomplishments** *(see Combat System §4.1 for exact current numbers - this is the "what got built," not the "what it's tuned to")*:
- Inventory weight system (20 capacity, per-weapon weights) and a real ammo system (Arrow/Bullet/Bolt, consumed per shot)
- Full attack resolution pipeline: targeting (single-target, HitsAllAdjacent, and a real cone-AOE aim-by-direction mode with live preview), hit/miss, crit (incl. guaranteed backstab crits), damage, knockback/thrust, weapon-switch handling, ammo spend - plus a range indicator and a live damage/hit%/crit/affliction preview panel so nothing is a guess
- Simple move-toward-player AI (movement only, no attacks yet), with real tile-by-tile walk animation for every unit (not a teleport) and a camera that continuously follows whoever's acting
- New status effects, all fully resolved (not just data): Knocked Down, Stunned (with a Break Stun mechanic + cooldown), Faint (a forced, unbreakable version of Stun), Slowed
- FFA victory condition (last player with a non-Fainted unit wins)
- A full weapon-by-weapon balance pass: every racial move and all 9 weapons got real numbers, several new mechanics along the way (distance-banded accuracy for Longbow/Pistol/Crossbow, per-weapon `AttackPower` so multiple tiers of the same weapon can share moves, a Wand introduced alongside the Staff, Shield Bash restricted to a Warrior specialist, a Cleric-only mace upgrade with its own range/damage-type bonus)
- 7-type damage system (Sharp/Blunt/Magical/Light/Electric/Poison/Frost) replacing the old Physical/Magical split
- DEF/RES switched to a diminishing-returns curve (the same family WoW/Skyrim use) instead of a flat clamped percentage
- Evasion actually wired into the hit-chance formula for the first time
- Race Perks: a stat bonus + elemental vulnerability per race, applied automatically to any unit of that race
- Every attack's AP cost unified to 1; the Sorcerer's actual magic attacks (Frost Blast, Arcane Missile) cost 10 MP on top - their free base counterparts (Bonk, Whack) and every other move in the game stay MP-free

**Fixed Along the Way**:
- Combat menu selections that failed silently (unaffordable AP/MP, no ammo, nothing in range) now always show why
- Camera didn't follow a unit that moved mid-turn (only re-centered once per turn) - now follows continuously, including during Move-mode's tile-hover
- View Map's WASD panning was frame-rate dependent (flat per-frame step, no deltaTime scaling) - the actual cause of reported jitter/lag, now scaled properly
- `HexGrid.GetHexesInRadius` was completely broken (ignored its center parameter) - found while building the range indicator, which needed correct radius math

**Notes**:
- AI attacking (not just moving) is the natural next milestone
- Spell/card execution is still a separate, unstarted system - the deck/hand UI works, but nothing a card does actually happens yet

---

### Session 6: AI Attacks, Turn-Start Gating, Weapon Animations, Death Visuals
**Goals**: Close the biggest gaps left after Session 5 - AI that only moved, a Move-mode camera bug, no visual feedback on hits, and units that could act before their own turn had visibly started

**Accomplishments**:
- **Weapon Attack Animations**: every move now plays a simple Slash (melee) or Shooting (ranged, travels attacker→target) effect on resolution, purely visual - see Weapon Attack Animations under Combat System §4.1
- **Move-Mode Camera Fix**: replaced a divergent per-frame camera-chase bug (see Known Issues) with a one-time snap on opening Move mode, to whatever tile the cursor is already over - clicking "Move" now behaves as if the cursor had hovered that tile first, matching the requested behavior exactly
- **Turn-Start Gate**: the "TURN X START" announcement (2s, every turn) now genuinely blocks all turn logic - no AI movement, no AI attacks, no auto-advance, no player input - until it clears, rather than only gating the camera's cosmetic transition (which was shorter, letting units act while the banner was still up)
- **AI Attacks**: AI units now attack as well as move - one attempt before moving (skip walking if already in range) and one after (in case it just closed distance), each picking the highest-damage affordable in-range move it has. Still targets the player specifically, not other AI units - see AI (Placeholder)
- **Death Visuals**: a dead unit's sprite tints gray (color-multiply approximation - no shader/Effect pipeline exists yet for true desaturation) and its label switches to "DEAD" in dark red, distinct from the existing gray-label treatment for a merely-Fainted unit

**Notes**:
- Every feature this session was verified with `dotnet build` (compiles clean) plus a short smoke-test launch (process stays alive) - this environment has no way to drive mouse/keyboard input, so none of it has been verified by an actual interactive playthrough yet
- AI targeting other AI units (not just the player) is the natural next AI milestone, now that attacking at all is in place

---

## 9. UPCOMING MILESTONES

### Immediate (Next 1-2 sessions)
- [ ] **AI Target Prioritization**: AI now attacks, but only ever targets the player specifically - extending it to attack other AI units (true FFA behavior) and weighing more than raw damage is the natural next step
- [ ] **Roachlin Slash / Summon Attacks**: summoned creatures' move lists work differently from a hero's kit (every move counts as an "attack," even spell-like ones) - still a separate, unstarted design pass
- [ ] **Remaining Weapons**: any future weapon variants (e.g. a second Dagger/Sword tier, now that `Weapon.AttackPower` supports it) and finishing the AttackPower migration for Staff/Wand/Bow/Pistol

### Short-term (Next 3-5 sessions)
- [ ] **Spell/Card Execution**: the deck/hand UI is fully built (draw, browse, tooltips), but no spell or summon actually does anything yet when played
- [ ] **Remaining Status Effects**: Rooted, Frozen, Silenced, Bound, Cursed, Fear, Blind are still just data/prose, unlike Bleeding/Stunned/Knocked Down/Slowed/Faint
- [ ] **Sound Effects**: Basic UI sounds, attack sounds, music loop

### Medium-term (Phase 1 completion)
- [ ] **Pixelated Sprites**: Add grass/tile textures
- [ ] **Map Editor UI**: Proper palette, inspector, toolbar
- [ ] **Save/Load**: Full game state persistence
- [ ] **Story Intro**: First scene, tutorial
- [ ] **Real Teams**: combat is currently a 4-player FFA (no party/faction grouping) - needed before the story's multi-hero party structure can exist in-engine

---

## 10. DESIGN DECISIONS & RATIONALE

### Hex Grid (vs Square Grid)
- **Reason**: Smoother movement, more strategic positioning, classic tactical RPG feel
- **Cost**: More complex coordinate math, requires custom rendering
- **Benefit**: Iconic look, better for range-based tactics

### Pointy-Top Orientation (vs Flat-Top)
- **Reason**: Matches Fire Emblem & Tactics Ogre (genre expectations)
- **Visual**: More natural for humanoid movement
- **Alternative**: Flat-top common in 4X games

### Speed-Based Turn Order (vs Simultaneous)
- **Reason**: Classic tactical RPG expectation; allows speed stat to matter
- **Strategic**: Fast units get more actions per "round"
- **Contrast**: Could add simultaneous mode later as variant

### Auto-Cycling with Camera Pan (vs Player Input)
- **Reason**: Keeps pacing fast, dramatic camera movements, cinematic feel
- **Future**: Can add "skip unit" button for experienced players
- **Balance**: 3-second wait prevents feeling rushed

### 5 AP per Turn (vs Fixed Actions)
- **Reason**: Movement + action possible, but forces tough choices
- **5 AP Breakdown**: Move 1 tile = 1 AP; typical attack = 2 AP; spell = 2-4 AP
- **Design**: Encourages varied playstyles (tank = move less, attack more)

### Card Deck System (vs Traditional Spells)
- **Reason**: Adds deckbuilding layer; RNG creates exciting moments
- **Mechanic**: Draw 1 card = 1 AP; reshuffle = +1 MP penalty discourages spam
- **Strategy**: Deck composition = core character differentiation

---

## 11. ASSET REQUIREMENTS

### Visual Style: 90's Anime Aesthetic
- **Sprite Art**: Pixel-based with smooth character animation (8-16 frames per animation)
- **Color Palette**: Vibrant but not oversaturated; classic anime color theory
- **Anti-Aliasing**: Minimal; maintain sharp pixel edges while using subtle shading
- **Animation Style**: Expressive movement; emphasis on character emotion through body language
- **UI Design**: Retro-futuristic panels with anime-inspired typography and iconography
- **Backgrounds**: Detailed environmental pixel art with parallax scrolling effects
- **Visual Effects**: Bold, colorful spell effects and impact animations (screen shake, flash, trails)

### Graphics Assets
- [ ] Sprite sheets for all unit classes (in progress)
  - Idle, walking, attacking, defending, casting, knocked down, dead animations
  - 90's anime style: exaggerated expressions through subtle sprite changes
- [ ] Tile textures (grass, water, mountain, etc.)
  - Pixel art with natural variation; avoid monotone appearance
- [ ] Environmental objects (trees, buildings, terrain features)
  - Proportioned for 90's anime visual style (exaggerated perspective)
- [ ] UI elements (buttons, panels, icons)
  - Anime-inspired neon/gradient borders and designs
- [ ] Particle effects (attacks, spells, environmental)
  - Bold colors; dramatic impact for satisfying feedback
- [ ] Character portraits (for dialogue)
  - Anime-style head shots for conversations
  - Mood variations matching NPC emotion system

### Audio
- [ ] Background music (map theme, battle theme, boss theme)
- [ ] Sound effects (UI, attacks, spells, status effects, footsteps)
- [ ] Voice acting (main characters; optional)
- [ ] Ambient sounds (birds, wind, water)

### Writing
- [ ] Dialogue scripts (NPC conversations)
- [ ] Story text (cutscenes, character bios)
- [ ] Tutorial text
- [ ] Item descriptions, ability flavor text

---

## 12. MONETIZATION

### Base Game
- **Price**: $20 USD
- **Platform**: PC (Steam, itch.io, GOG)
- **Content**: Full campaign, 30+ characters, 4 acts, multiple endings

### Early Access
- **Pricing Tier**: $5 → $10 → $15 → $20 (increases as content added)
- **Duration**: ~12 months before full launch
- **Benefits to Players**: Direct dev feedback, influence on design

### Post-Launch
- **Annual Expansions**: Paid DLC (~$10 each; 2-3 per year)
- **Free Updates**: Multiplayer modes, balance patches, cosmetics
- **No Ads**: Claude products philosophy—ad-free experience

---

## 13. KNOWN ISSUES & TECHNICAL DEBT

### Current Blockers
- [x] **Font Rendering**: Arial.spritefont not loading in Content pipeline
  - **Status**: Resolved - text renders throughout the UI now (AP/HP display, all menus, combat log, tooltips)

- [ ] **Perlin Noise Indexing**: Previous out-of-bounds errors (FIXED with & 255 masking)
  - **Status**: Resolved; monitor for future edge cases

- [ ] **Camera Pan Speed Not Frame-Rate Independent**: `HandleMapControls`'s WASD pan speed was a flat per-FRAME step (`panSpeed = 5f`, applied once per Update() call with no deltaTime scaling) instead of a per-SECOND rate - any variance in frame timing showed up directly as visible stutter
  - **Impact**: reported as "View Map feels laggy and jittery"
  - **Status**: Resolved - converted to `400 pixels/second * deltaTime`

- [ ] **HexGrid.GetHexesInRadius Bug**: iterated `x`/`y` as absolute coordinates near the map origin instead of relative to `centerCol`/`centerRow`, and added `(x, y)` instead of `(centerCol + x, centerRow + y)` - returned hexes near (0,0) regardless of what center was passed in
  - **Impact**: unused by anything at the time (dead code), so no observed symptom - found and fixed while building the Move-mode range indicator, which needed a correct version of this exact functionality
  - **Status**: Resolved

- [ ] **HexGrid Odd-R/Cube Conversion**: `OddRToCube`/`CubeToOddR` used the wrong offset formula (matched "odd-q" column-offset instead of the "odd-r" row-offset scheme the rest of HexGrid uses) - about 1 in 7 neighbor pairs resolved to a cube delta of the wrong distance, and opposite screen-directions didn't map to opposite cube deltas
  - **Impact**: `GetDistance` was subtly wrong in some tile configurations; discovered while implementing Facing/backstab detection, which depends on direction math being correct
  - **Status**: Resolved - corrected to the standard odd-r formula; verified with a brute-force check (every tile 0-19,0-14: round-trip + all 6 neighbors at distance 1, 2100/2100 checks passing; the old formula failed 300 of them)

- [ ] **HexGrid WorldToHex Pixel Formula**: a second, separate bug (not fixed by the one above) - `WorldToHex`'s pixel-to-axial formula didn't actually match `HexToWorld`'s tile placement at all (looked like a leftover from a different hex orientation); clicking/hovering a tile could resolve to a completely different, unrelated tile
  - **Impact**: mouse hex selection, hover highlighting, and movement-mode's click-to-move/path-preview (all built on `WorldToHex`) could target the wrong tile - reported as "cursor on one tile, a tile elsewhere gets highlighted"
  - **Status**: Resolved - derived the correct inverse of the axial-to-pixel formula `HexToWorld`/`OddRToCube` actually imply and swapped it in; verified with a brute-force check (every tile center, plus 500 randomly-jittered click points within each tile's interior, 800/800 passing; the old formula failed 799/800)

- [ ] **Move-Mode Camera Divergent Feedback Loop**: `OpenMovementMode`'s camera-follow recomputed "world position under the cursor" every frame using the CURRENT camera position, then snapped the camera straight to it - since the mouse doesn't move but the camera does, each frame's screen→world conversion pointed further away than the last, with nothing bounding it
  - **Impact**: reported as "the camera moves extremely fast way beyond the bounds of the map" as soon as Move mode opened
  - **Status**: Resolved - removed the per-frame chase entirely; `OpenMovementMode` now does a single one-time snap to the cursor's tile using `Mouse.GetState()` at the moment Move is selected, and Update()'s camera-follow is fully suppressed for the rest of Move mode (WASD/scroll pan freely from there, same treatment as View Map) - see Camera Follows the Mover under Combat System §4.1

### Technical Debt
- [x] DevConsole expanded with `add card <CardID>` for spell testing (see Card/Deck System below) - more commands/better parsing still welcome
- [x] **Fixed**: the console accepted no typed input at all - `HandleConsoleInput` only ever polled `KeyboardState` (which keys are held, not what character they produce), so letters/digits/symbols never reached `_consoleInput`; only Backspace/Enter/Up/Down worked, because those are handled as control keys rather than typed text. Fixed by subscribing to MonoGame's `Window.TextInput` event (keyboard-layout-aware, fires once per keystroke) in `Initialize()`, filtering out `` ` ``/`~` (toggles the console, never typed) and Enter/Backspace/newline (still handled via the existing KeyboardState polling, so they don't double up as literal characters). Also fixed Backspace being un-edge-triggered, which deleted a character every single frame it was held (effectively instant) rather than once per press
- [ ] AssetRegistry could support more asset types (models, sounds)
- [ ] Camera zoom limits should be adjustable
- [ ] No input validation on tile/object commands

### Performance
- **Current**: Single map of 20x15 tiles renders smoothly
- **Scaling**: Monitor performance as maps grow to 50x50+
- **Optimization**: Consider object pooling for repeated entities

---

## 14. CONTACT & CREDITS

**Lead Developer**: Dave (David G. Piper)  
**Email**: ebm22david@gmail.com  
**Phone**: 951 290 9332 (Oaxaca, Mexico)  
**LinkedIn**: linkedin.com/in/davidgpiper1  

**Studio**: Insomnium-Eye  
**Website**: https://insomnium-eye.com  

**Artists**: [TBD - commissions from multiple artists]  
**Musicians**: [TBD]  
**Writers**: [TBD]  

---

## 15. REVISION HISTORY

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | Initial | Dave | GDD foundation, lore, mechanics, tech stack |
| 1.1 | Following | Dave | Map system details, HexGrid math, procedural generation |
| 1.2 | Latest | Dave | Unit system, turn system, dev journal, status update |
| 1.2 (Session 5 update) | September 2026 | Dave | Full attack system (targeting/resolution/AI/status effects), complete weapon-by-weapon balance pass, damage types, ammo, DEF/RES diminishing returns, Evasion, Race Perks, movement animation + camera follow, View Map jitter fix - see Combat System §4.1 and Dev Journal Session 5 |
| 1.2 (Session 6 update) | September 2026 | Dave | AI attacks (highest-damage affordable in-range move, before/after moving), turn-start gate blocking all turn logic during the announcement, Slash/Shooting weapon attack animations, Move-mode camera bug fix (one-time cursor snap, no more divergent chase), death visuals (gray tint + "DEAD" label) - see Combat System §4.1, Known Issues, and Dev Journal Session 6 |

---

**END OF DOCUMENT**

Last Updated: September 2026 (Session 6)  
Next Review: After Phase 1 completion or major design changes
