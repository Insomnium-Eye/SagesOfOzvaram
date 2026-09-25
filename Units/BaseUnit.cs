using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SagesOfOzvaram.Combat;
using SagesOfOzvaram.Maps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// Base class for all game units (heroes, NPCs, enemies, etc).
    /// </summary>
    public abstract class BaseUnit
    {
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int Speed { get; set; }  // Determines turn order

        /// <summary>% Speed lost to a Slowed effect (e.g. Frost Blast), 0-1. Only meaningful while SpeedReductionTurnsRemaining > 0.</summary>
        public float SpeedReductionPercent { get; private set; }

        /// <summary>How many of this unit's own upcoming turns the Slowed effect still applies for - ticks down once per turn regardless of anything else, same pattern as StunBreakCooldownRemaining.</summary>
        public int SpeedReductionTurnsRemaining { get; private set; }

        /// <summary>Speed after any active Slowed reduction - this is what actually drives turn order and TilesPerAP.</summary>
        public int EffectiveSpeed => SpeedReductionTurnsRemaining > 0
            ? (int)Math.Round(Speed * (1f - SpeedReductionPercent))
            : Speed;

        /// <summary>Apply a Slowed effect: takes the stronger of any existing reduction% and the longer of any existing duration, rather than either shortening or weakening one already active.</summary>
        public void ApplySpeedReduction(float percent, int turns)
        {
            SpeedReductionPercent = Math.Max(SpeedReductionPercent, percent);
            SpeedReductionTurnsRemaining = Math.Max(SpeedReductionTurnsRemaining, turns);
        }

        public void TickSpeedReduction()
        {
            SpeedReductionTurnsRemaining = Math.Max(0, SpeedReductionTurnsRemaining - 1);
            if (SpeedReductionTurnsRemaining == 0)
                SpeedReductionPercent = 0f;
        }

        /// <summary>Tiles this unit can move per AP spent: 1 by default, +1 for every 15 (effective) Speed.</summary>
        public int TilesPerAP => 1 + EffectiveSpeed / 15;

        /// <summary>Current world position - jumps instantly when set directly. To animate a walk across several tiles instead, use SetMovementPath.</summary>
        public Vector2 Position { get; set; }

        private readonly Queue<Vector2> _movementWaypoints = new Queue<Vector2>();
        private Vector2 _currentWaypointStart;
        private float _currentWaypointElapsed;
        private const float SecondsPerTileMove = 0.2f;

        /// <summary>True while this unit is mid-walk (Position is animating toward SetMovementPath's waypoints).</summary>
        public bool IsMoving => _movementWaypoints.Count > 0;

        /// <summary>
        /// Walk toward each of `waypoints` (one world position per tile) in order, animating
        /// Position smoothly instead of jumping - call UpdateMovementAnimation every frame to
        /// advance it. Replaces any walk already in progress.
        /// </summary>
        public void SetMovementPath(IEnumerable<Vector2> waypoints)
        {
            _movementWaypoints.Clear();
            foreach (var waypoint in waypoints)
                _movementWaypoints.Enqueue(waypoint);

            _currentWaypointStart = Position;
            _currentWaypointElapsed = 0f;
        }

        /// <summary>Advance the current walk animation, if any (no-op otherwise). Call once per frame for every unit.</summary>
        public void UpdateMovementAnimation(float deltaTime)
        {
            if (_movementWaypoints.Count == 0)
                return;

            _currentWaypointElapsed += deltaTime;
            float t = Math.Clamp(_currentWaypointElapsed / SecondsPerTileMove, 0f, 1f);
            Vector2 target = _movementWaypoints.Peek();
            Position = Vector2.Lerp(_currentWaypointStart, target, t);

            if (t >= 1f)
            {
                Position = target;
                _movementWaypoints.Dequeue();
                _currentWaypointStart = Position;
                _currentWaypointElapsed = 0f;
            }
        }

        public float Scale { get; set; } = 1.0f;  // Per-unit scale multiplier

        /// <summary>
        /// Which of the 6 hex directions this unit is facing. Used for facing-dependent
        /// effects like Stab's guaranteed backstab crit (see HexGrid.GetDirectionToNeighbor).
        /// Defaults to East; nothing rotates a unit to face its movement yet.
        /// </summary>
        public HexDirection Facing { get; set; } = HexDirection.East;

        // Combat stats (first-pass placeholder defaults; each hero overrides these)
        public int Strength { get; set; } = 10;

        /// <summary>Strength including this unit's racial bonus, if its Race grants one (e.g. Lethios +2) - combat formulas (damage, Knockdown, the Bleeding DEF-gate) read this, not raw Strength.</summary>
        public int EffectiveStrength => Strength + RaceCatalog.GetModifiers(Race).StrengthBonus;

        public int Accuracy { get; set; } = 10;

        /// <summary>Dodge stat - reduces an incoming attack's hit chance by 0.1%/point, the mirror of Accuracy's own +0.1%/point (see Move.GetHitChance).</summary>
        public int Evasion { get; set; } = 10;

        /// <summary>Evasion including this unit's racial bonus, if its Race grants one (e.g. Vectium +5) - Move.GetHitChance reads this, not raw Evasion.</summary>
        public int EffectiveEvasion => Evasion + RaceCatalog.GetModifiers(Race).EvasionBonus;

        /// <summary>Magic damage stat - magical weapon moves (e.g. the Sorcerer's Arcane Missile) scale with this instead of Strength.</summary>
        public int Intelligence { get; set; } = 10;

        /// <summary>Physical damage reduction - raw armor rating, not a direct percentage; see DefenseMitigationPercent for the diminishing-returns curve that converts it.</summary>
        public int Defense { get; set; } = 10;

        /// <summary>Magic damage reduction - raw armor rating, not a direct percentage; see ResistanceMitigationPercent for the diminishing-returns curve that converts it.</summary>
        public int Resistance { get; set; } = 10;

        public Race Race { get; }

        /// <summary>
        /// This unit's class (GDD §4.2). Any unit sharing a class gets that class's weapon
        /// specializations (and eventually spells) via ClassCatalog - shared by class, not by
        /// which specific tier/subclass instance a unit happens to be.
        /// </summary>
        public HeroClass Class { get; }

        /// <summary>Bleed magnitude as a % of MAX HP lost when this unit's turn starts. 0 = not bleeding.</summary>
        public float BleedPercentPerTurn { get; set; } = 0f;

        /// <summary>
        /// How many of this unit's own upcoming turns are still skipped due to Stun - its
        /// "length" is set by whatever inflicted it (Move.StatusDurationTurns), not a severity
        /// rank. Decremented by one each time a turn is skipped - see TurnSystem.OnUnitTurnStart.
        /// </summary>
        public int StunTurnsRemaining { get; private set; }

        public bool IsStunned => StunTurnsRemaining > 0;

        /// <summary>Stun for `turns` of this unit's own turns - takes the longer of this and any Stun already active, rather than shortening an existing one.</summary>
        public void ApplyStun(int turns) => StunTurnsRemaining = Math.Max(StunTurnsRemaining, turns);

        /// <summary>Consume one turn of Stun (called when this unit's turn is skipped because of it).</summary>
        public void ConsumeStunTurn() => StunTurnsRemaining = Math.Max(0, StunTurnsRemaining - 1);

        /// <summary>AP cost to Break Stun (see TryBreakStun).</summary>
        public const int StunBreakAPCost = 3;

        /// <summary>How many of this unit's own turns remain before Break Stun is off cooldown again. Ticks down once per turn regardless of whether this unit is currently stunned - see TurnSystem.OnUnitTurnStart.</summary>
        public int StunBreakCooldownRemaining { get; private set; }

        /// <summary>
        /// True for a "Summoner" unit (has a real HeroClass) - only these can attempt to Break
        /// Stun; summoned creatures (HeroClass.None) can't. Faint bypasses this entirely too
        /// (see IsFainted) - it's a forced, unbreakable version of Stun with no escape at all.
        /// </summary>
        public bool CanBreakStun => Class != HeroClass.None;

        public void TickStunBreakCooldown() => StunBreakCooldownRemaining = Math.Max(0, StunBreakCooldownRemaining - 1);

        /// <summary>
        /// Attempt to Break Stun: spends StunBreakAPCost AP, clears Stun entirely, and starts a
        /// 5-turn cooldown. No-op (returns false, nothing spent) if not stunned, not a Summoner
        /// unit, still on cooldown, or the AP can't be afforded.
        /// </summary>
        public bool TryBreakStun()
        {
            if (!IsStunned || !CanBreakStun || StunBreakCooldownRemaining > 0 || CurrentAP < StunBreakAPCost)
                return false;

            CurrentAP -= StunBreakAPCost;
            StunTurnsRemaining = 0;
            StunBreakCooldownRemaining = 5;
            return true;
        }

        /// <summary>True while Knocked Down (prone): cannot move, and this unit's own accuracy is cut by 75% on any move it attempts (see Move.GetHitChance). Cleared by spending KnockdownStandUpAPCost AP - see TryStandUp.</summary>
        public bool IsKnockedDown { get; private set; }

        /// <summary>AP cost to stand back up, set by whichever hit knocked this unit down (Move.KnockdownStandUpAPCost) rather than a fixed global cost.</summary>
        public int KnockdownStandUpAPCost { get; private set; }

        public void ApplyKnockdown(int standUpApCost)
        {
            IsKnockedDown = true;
            KnockdownStandUpAPCost = standUpApCost;
        }

        /// <summary>Spend KnockdownStandUpAPCost AP to clear Knocked Down. No-op (returns false) if not knocked down or the AP can't be afforded.</summary>
        public bool TryStandUp()
        {
            if (!IsKnockedDown || CurrentAP < KnockdownStandUpAPCost)
                return false;

            CurrentAP -= KnockdownStandUpAPCost;
            IsKnockedDown = false;
            KnockdownStandUpAPCost = 0;
            return true;
        }

        /// <summary>
        /// True once HP drops to 5% of MaxHP or below (recomputed on every HP change, so it
        /// clears again if healed back above the threshold). A forced, unbreakable version of
        /// Stun - unlike Stun, there's no AP granted and no Break Stun option; the unit's turn
        /// is skipped outright every time until HP recovers - see TurnSystem.OnUnitTurnStart.
        /// </summary>
        public bool IsFainted { get; private set; }

        // Action Points
        public int MaxAP { get; set; } = 5;
        public int CurrentAP { get; set; } = 5;

        /// <summary>How many cards this unit has drawn this turn. The AP cost doubles each time (1, 2, 4, 8...). Reset at the start of a new turn.</summary>
        public int CardsDrawnThisTurn { get; private set; }

        public void RecordCardDraw() => CardsDrawnThisTurn++;

        public void ResetCardDrawState() => CardsDrawnThisTurn = 0;

        /// <summary>Refill AP to MaxAP. Called automatically when this unit's turn starts.</summary>
        public void ResetAP() => CurrentAP = MaxAP;

        // Mana Points - unlike AP, this does NOT auto-refill each turn (nothing spends MP yet,
        // since there's no Spell/card execution system; first-pass placeholder pool per class).
        public int MaxMP { get; set; } = 10;
        public int CurrentMP { get; set; } = 10;

        /// <summary>AP cost to Guard. Adjustable - some abilities/weapons may raise or lower this.</summary>
        public int GuardAPCost { get; set; } = 2;

        /// <summary>
        /// True while this unit is in a Guard stance (from the "Guard" action), boosting
        /// Defense/Resistance until the start of its next turn.
        /// </summary>
        public bool IsGuarding { get; private set; }

        public void ApplyGuard() => IsGuarding = true;

        /// <summary>Clear any active Guard stance. Called automatically when this unit's turn starts.</summary>
        public void ClearGuard() => IsGuarding = false;

        /// <summary>
        /// Passive DEF/RES bonus (percentage points) from any shield in Inventory - applies
        /// always, not just while Guarding (e.g. an Iron Shield's own 15%). Takes the best if
        /// somehow carrying more than one. 0 if carrying no shield.
        /// </summary>
        public float GetPassiveShieldBonusPercent()
        {
            float best = 0f;
            foreach (var weapon in Inventory)
            {
                if (weapon.ShieldPassiveDefResBonusPercent.HasValue)
                    best = Math.Max(best, weapon.ShieldPassiveDefResBonusPercent.Value * 100f);
            }
            return best;
        }

        /// <summary>
        /// The ADDITIONAL DEF/RES bonus (percentage points) Guard grants right now, on top of
        /// any passive shield bonus: the base 20%, or a shield's own additional Guard bonus if
        /// one is carried (shields replace the base amount rather than stacking with it - e.g.
        /// an Iron Shield's Guard adds 25% on top of its own 15% passive, for 40% total).
        /// </summary>
        public float GetGuardBonusPercent()
        {
            float best = 20f;
            foreach (var weapon in Inventory)
            {
                if (weapon.ShieldGuardBonusPercent.HasValue)
                    best = Math.Max(best, weapon.ShieldGuardBonusPercent.Value * 100f);
            }
            return best;
        }

        /// <summary>
        /// Diminishing-returns constant for DEF/RES, same "asymptotic armor" family WoW and
        /// Skyrim use: mitigation% = 100 * stat / (stat + K). At stat == K, mitigation is
        /// exactly 50%; no amount of DEF/RES alone ever reaches 100% (unlike a flat clamped
        /// percentage), so stacking it keeps paying off but with steadily smaller returns.
        /// </summary>
        private const float ArmorDiminishingReturnsConstant = 100f;

        /// <summary>Defense including this unit's racial bonus, if its Race grants one (e.g. Lethios +2) - raw armor rating, not yet run through the diminishing curve. Also what the Knockdown formula and Bleeding's DEF-gate read as "Armor."</summary>
        public int TotalDefense => Defense + RaceCatalog.GetModifiers(Race).DefenseBonus;

        /// <summary>Resistance including this unit's racial bonus, if its Race grants one (e.g. Human +7) - raw, not yet run through the diminishing curve.</summary>
        public int TotalResistance => Resistance + RaceCatalog.GetModifiers(Race).ResistanceBonus;

        /// <summary>Physical mitigation % from TotalDefense (raw Defense + racial bonus, diminishing returns) - before any shield/Guard bonus, which are still flat adds layered on top.</summary>
        public float DefenseMitigationPercent => 100f * TotalDefense / (TotalDefense + ArmorDiminishingReturnsConstant);

        /// <summary>Magical mitigation % from TotalResistance (raw Resistance + racial bonus, diminishing returns) - before any shield/Guard bonus, which are still flat adds layered on top.</summary>
        public float ResistanceMitigationPercent => 100f * TotalResistance / (TotalResistance + ArmorDiminishingReturnsConstant);

        /// <summary>Total physical mitigation %: diminishing-returns Defense, plus any passive shield bonus (always), plus the Guard bonus (only while Guarding) - the shield/Guard layers are still flat percentage-point adds, clamped 0-100 overall.</summary>
        public float EffectiveDefense => Math.Clamp(DefenseMitigationPercent + GetPassiveShieldBonusPercent() + (IsGuarding ? GetGuardBonusPercent() : 0f), 0f, 100f);

        /// <summary>Total magical mitigation %: diminishing-returns Resistance, plus any passive shield bonus (always), plus the Guard bonus (only while Guarding) - the shield/Guard layers are still flat percentage-point adds, clamped 0-100 overall.</summary>
        public float EffectiveResistance => Math.Clamp(ResistanceMitigationPercent + GetPassiveShieldBonusPercent() + (IsGuarding ? GetGuardBonusPercent() : 0f), 0f, 100f);

        private readonly List<Weapon> _inventory = new List<Weapon>();

        /// <summary>
        /// Weapons this unit is carrying. Each weapon's attacks are folded into
        /// AvailableMoves alongside the unit's racial move. Add/remove via TryAddToInventory /
        /// RemoveFromInventory so InventoryWeightCapacity is respected and EquippedWeapon stays valid.
        /// </summary>
        public IReadOnlyList<Weapon> Inventory => _inventory;

        /// <summary>Total inventory space every unit has for carrying weapons (GDD-pending; first-pass flat value for every class).</summary>
        public int InventoryWeightCapacity { get; set; } = 20;

        /// <summary>Sum of Weight across everything currently in Inventory.</summary>
        public int CurrentInventoryWeight => Inventory.Sum(w => w.Weight);

        /// <summary>Free inventory space left before hitting InventoryWeightCapacity.</summary>
        public int RemainingInventoryWeight => InventoryWeightCapacity - CurrentInventoryWeight;

        /// <summary>True if this unit has enough remaining capacity to carry the given weapon.</summary>
        public bool CanCarry(Weapon weapon) => weapon != null && weapon.Weight <= RemainingInventoryWeight;

        /// <summary>
        /// Add a weapon to Inventory if there's enough remaining weight capacity for it. Equips
        /// it automatically if this is the unit's first weapon. Returns false (and leaves
        /// Inventory unchanged) if the weapon is too heavy to carry.
        /// </summary>
        public bool TryAddToInventory(Weapon weapon)
        {
            if (!CanCarry(weapon))
                return false;

            _inventory.Add(weapon);
            EquippedWeapon ??= weapon;
            return true;
        }

        /// <summary>
        /// Remove a weapon from Inventory (e.g. Throw Dagger consuming the Dagger). If it was the
        /// EquippedWeapon, falls back to the first remaining weapon, or null if Inventory is now empty.
        /// </summary>
        public bool RemoveFromInventory(Weapon weapon)
        {
            if (!_inventory.Remove(weapon))
                return false;

            if (EquippedWeapon == weapon)
                EquippedWeapon = _inventory.Count > 0 ? _inventory[0] : null;
            return true;
        }

        /// <summary>
        /// The weapon currently in-hand. Attacks from this weapon cost their listed AP; attacks
        /// from any other weapon in Inventory cost +1 AP to cover switching to it (see
        /// GetEffectiveAPCost). Null means no weapon is equipped (only the racial move is "free").
        /// </summary>
        public Weapon EquippedWeapon { get; set; }

        private readonly Dictionary<AmmoType, int> _ammo = new Dictionary<AmmoType, int>();

        /// <summary>How many of `type` this unit is carrying. 0 if none.</summary>
        public int GetAmmo(AmmoType type) => _ammo.TryGetValue(type, out int count) ? count : 0;

        public void AddAmmo(AmmoType type, int amount) => _ammo[type] = GetAmmo(type) + amount;

        /// <summary>Spend `amount` of `type` if there's enough. No-op (returns false) otherwise - a Move requiring ammo it doesn't have is simply unusable, same as unaffordable AP/MP.</summary>
        public bool TryConsumeAmmo(AmmoType type, int amount = 1)
        {
            if (GetAmmo(type) < amount)
                return false;

            _ammo[type] -= amount;
            return true;
        }

        /// <summary>
        /// Weapon types this unit is a specialist in, derived from its Class (e.g. any Hunter
        /// is a Dagger specialist) - not set per-instance, so it stays consistent across every
        /// unit of that class regardless of tier. Grants access to a weapon's SpecialistAttacks;
        /// other units can still carry and use the base Attacks of a type they're not specialized in.
        /// </summary>
        public HashSet<WeaponType> WeaponSpecializations => ClassCatalog.GetWeaponSpecializations(Class);

        /// <summary>
        /// Every attack this unit can currently perform: its race's innate move, plus every
        /// attack (and, if specialized in that weapon's Type, specialist attack) granted by a
        /// weapon in its Inventory. Computed live so picking up/dropping a weapon is reflected
        /// immediately.
        /// </summary>
        public List<Move> AvailableMoves => AvailableMovesWithSource.Select(entry => entry.Move).ToList();

        /// <summary>
        /// Every attack this unit can currently perform, paired with the weapon it came from
        /// (null for the racial move) - lets callers work out weapon-switch AP costs.
        /// </summary>
        public IEnumerable<(Move Move, Weapon SourceWeapon)> AvailableMovesWithSource
        {
            get
            {
                yield return (RaceAttacks.GetInnateMove(Race), null);
                foreach (var weapon in Inventory)
                {
                    foreach (var move in weapon.Attacks)
                        yield return (move, weapon);

                    if (weapon.Type.HasValue && WeaponSpecializations.Contains(weapon.Type.Value))
                    {
                        foreach (var move in weapon.SpecialistAttacks)
                            yield return (move, weapon);
                    }
                }
            }
        }

        /// <summary>
        /// The AP a move actually costs right now: its own APCost, plus 1 if it belongs to a
        /// weapon other than EquippedWeapon (the cost of putting away the current weapon and
        /// drawing that one). Racial moves (sourceWeapon null) never carry this surcharge.
        /// </summary>
        public int GetEffectiveAPCost(Move move, Weapon sourceWeapon)
        {
            bool needsWeaponSwitch = sourceWeapon != null && sourceWeapon != EquippedWeapon;
            return move.APCost + (needsWeaponSwitch ? 1 : 0);
        }

        // Sprite and rendering
        public Texture2D SpriteTexture { get; set; }
        public string SpriteAssetPath { get; set; }

        /// <summary>
        /// Create a new unit.
        /// </summary>
        protected BaseUnit(string name, int maxHP, int speed, Race race, HeroClass heroClass, Vector2 position = default)
        {
            Name = name;
            MaxHP = maxHP;
            HP = maxHP;
            Speed = speed;
            Race = race;
            Class = heroClass;
            Position = position;
            SpriteAssetPath = "";
        }
 
        /// <summary>
        /// Load the unit's sprite texture.
        /// </summary>
        public virtual void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content, Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice)
        {
            if (SpriteTexture != null)
                return; // Already loaded
 
            // Try ContentManager first
            if (!string.IsNullOrEmpty(SpriteAssetPath))
            {
                try
                {
                    SpriteTexture = content.Load<Texture2D>(SpriteAssetPath);
                    return;
                }
                catch
                {
                    // Fall through to file loading
                }
            }
 
            // Fallback: Load directly from disk
            try
            {
                string filePath = Path.Combine("Content", SpriteAssetPath + ".png");
                if (File.Exists(filePath))
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        SpriteTexture = Texture2D.FromStream(graphicsDevice, stream);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Sprite file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load sprite from disk: {ex.Message}");
            }
        }
 
        /// <summary>
        /// Draw the unit at its position.
        /// </summary>
        public virtual void Draw(SpriteBatch spriteBatch, Vector2 screenPos, float scale = 1.0f)
        {
            if (SpriteTexture != null)
            {
                spriteBatch.Draw(SpriteTexture, screenPos, null, Color.White, 0f, 
                               Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
 
        /// <summary>
        /// Take damage, mitigated by Defense (Sharp/Blunt) or Resistance (Magical/Light) - see
        /// EffectiveDefense/EffectiveResistance for the diminishing-returns formula. Applied
        /// BEFORE that mitigation: a racial vulnerability bonus (see RaceCatalog), if this
        /// unit's Race is vulnerable to damageType specifically (e.g. Lethios + Frost) - so DEF/RES
        /// still mitigates a portion of the inflated amount, rather than being tacked on after.
        /// </summary>
        public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Sharp)
        {
            var racialMods = RaceCatalog.GetModifiers(Race);
            float rawDamage = damage;
            if (racialMods.VulnerableTo == damageType)
                rawDamage *= 1f + racialMods.VulnerabilityBonusPercent;

            float mitigation = damageType.IsPhysical() ? EffectiveDefense : EffectiveResistance;
            float mitigated = rawDamage * (1f - mitigation / 100f);

            ApplyRawDamage((int)Math.Round(mitigated));
        }

        /// <summary>
        /// Apply damage that bypasses Defense/Resistance entirely (e.g. bleed ticks).
        /// </summary>
        public void ApplyTrueDamage(int damage)
        {
            ApplyRawDamage(damage);
        }

        private void ApplyRawDamage(int damage)
        {
            HP = Math.Max(0, HP - Math.Max(0, damage));
            UpdateFaintStatus();
        }

        /// <summary>
        /// Heal.
        /// </summary>
        public virtual void Heal(int amount)
        {
            HP = Math.Min(MaxHP, HP + amount);
            UpdateFaintStatus();
        }

        /// <summary>
        /// Apply this turn's bleed damage (a % of MAX HP), if bleeding. Called automatically
        /// when this unit's turn starts (see TurnSystem). Bypasses Defense/Resistance - bleed
        /// is true damage.
        /// </summary>
        public void ApplyBleedTick()
        {
            if (BleedPercentPerTurn <= 0f)
                return;

            int bleedDamage = (int)Math.Round(MaxHP * BleedPercentPerTurn);
            ApplyTrueDamage(bleedDamage);
        }

        private void UpdateFaintStatus()
        {
            IsFainted = MaxHP > 0 && HP <= MaxHP * 0.05f;
        }

        /// <summary>
        /// Check if unit is alive.
        /// </summary>
        public bool IsAlive => HP > 0;
    }
}
