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

        /// <summary>Tiles this unit can move per AP spent: 1 by default, +1 for every 15 Speed.</summary>
        public int TilesPerAP => 1 + Speed / 15;
        public Vector2 Position { get; set; }
        public float Scale { get; set; } = 1.0f;  // Per-unit scale multiplier

        /// <summary>
        /// Which of the 6 hex directions this unit is facing. Used for facing-dependent
        /// effects like Stab's guaranteed backstab crit (see HexGrid.GetDirectionToNeighbor).
        /// Defaults to East; nothing rotates a unit to face its movement yet.
        /// </summary>
        public HexDirection Facing { get; set; } = HexDirection.East;

        // Combat stats (first-pass placeholder defaults; each hero overrides these)
        public int Strength { get; set; } = 10;
        public int Accuracy { get; set; } = 10;

        /// <summary>Magic damage stat - magical weapon moves (e.g. the Sorcerer's Arcane Missile) scale with this instead of Strength.</summary>
        public int Intelligence { get; set; } = 10;

        /// <summary>Physical damage reduction, in percentage points (e.g. 20 = -20% physical damage).</summary>
        public int Defense { get; set; } = 10;

        /// <summary>Magic damage reduction, in percentage points (e.g. 20 = -20% magic damage).</summary>
        public int Resistance { get; set; } = 10;

        public Race Race { get; }

        /// <summary>
        /// This unit's class (GDD §4.2). Any unit sharing a class gets that class's weapon
        /// specializations (and eventually spells) via ClassCatalog - shared by class, not by
        /// which specific tier/subclass instance a unit happens to be.
        /// </summary>
        public HeroClass Class { get; }

        /// <summary>Bleed magnitude as a % of CURRENT HP lost when this unit's turn starts. 0 = not bleeding.</summary>
        public float BleedPercentPerTurn { get; set; } = 0f;

        /// <summary>True once HP drops to 5% of MaxHP or below; a fainted unit is unable to move.</summary>
        public bool IsFainted { get; private set; }

        // Action Points
        public int MaxAP { get; set; } = 5;
        public int CurrentAP { get; set; } = 5;

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
        /// The DEF/RES bonus (percentage points) Guard grants right now: the base 20%, or a
        /// shield's own total if a shield is equipped (shields replace the base amount rather
        /// than stacking with it - e.g. an Iron Shield makes Guard worth 40% instead of 20%).
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

        /// <summary>Defense including the Guard bonus, if currently guarding.</summary>
        public float EffectiveDefense => Defense + (IsGuarding ? GetGuardBonusPercent() : 0f);

        /// <summary>Resistance including the Guard bonus, if currently guarding.</summary>
        public float EffectiveResistance => Resistance + (IsGuarding ? GetGuardBonusPercent() : 0f);

        /// <summary>
        /// Weapons this unit is carrying. Each weapon's attacks are folded into
        /// AvailableMoves alongside the unit's racial move.
        /// </summary>
        public List<Weapon> Inventory { get; } = new List<Weapon>();

        /// <summary>
        /// The weapon currently in-hand. Attacks from this weapon cost their listed AP; attacks
        /// from any other weapon in Inventory cost +1 AP to cover switching to it (see
        /// GetEffectiveAPCost). Null means no weapon is equipped (only the racial move is "free").
        /// </summary>
        public Weapon EquippedWeapon { get; set; }

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
        /// Take damage, mitigated by Defense (Physical) or Resistance (Magical) - formula is a
        /// first-pass flat percentage reduction, to be tuned once real combat exists.
        /// </summary>
        public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Physical)
        {
            float mitigation = damageType == DamageType.Physical ? EffectiveDefense : EffectiveResistance;
            float mitigated = damage * (1f - Math.Clamp(mitigation, 0f, 100f) / 100f);
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
        /// Apply this turn's bleed damage (a % of current HP), if bleeding. Called
        /// automatically when this unit's turn starts (see TurnSystem). Bypasses
        /// Defense/Resistance - bleed is true damage.
        /// </summary>
        public void ApplyBleedTick()
        {
            if (BleedPercentPerTurn <= 0f)
                return;

            int bleedDamage = (int)Math.Round(HP * BleedPercentPerTurn);
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
