using System.Collections.Generic;
using System.Linq;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Resolves every SpellCard whose entire effect is "pay AP/MP, then apply something to the
    /// caster (and/or its adjacent allies) with no further targeting step" - Mana Shield, Deep
    /// Sleep, Meditate, and the generic self/ally stat-buff or AP-grant utility spells (Brace,
    /// Rally Cry, Steady Hands, Second Wind) - entirely off Move's own data
    /// (IsManaShield/IsSleepSpell/IsMeditateSpell/the *Buff fields/GrantedAP). This has no
    /// MonoGame/UI dependency at all, so it's fully unit-testable and none of it has to live in
    /// Game1. Spells that need an extra targeting STEP first (Blink's destination tile, Arcane
    /// Shield's ally pick, a damage spell's target, a Summon's placement tile) still go through
    /// Game1's own targeting-mode state machine, since picking a tile/unit on the hex grid is
    /// inherently a UI concern - but even those apply their actual effect through
    /// BaseUnit/StatusEffect the same way this class does, not through ad hoc fields of their own.
    /// </summary>
    public static class SpellCaster
    {
        /// <summary>The outcome of a Cast call - what message to show, and whether the card should actually be removed from the caster's hand (most cards: yes; Mana Shield/Meditate's toggle-off, and Meditate's own "stays in hand" start: no).</summary>
        public readonly struct CastResult
        {
            public bool Success { get; }
            public string Message { get; }
            public bool ConsumesCard { get; }

            private CastResult(bool success, string message, bool consumesCard)
            {
                Success = success;
                Message = message;
                ConsumesCard = consumesCard;
            }

            public static CastResult Fail(string message) => new CastResult(false, message, false);
            public static CastResult Ok(string message, bool consumesCard = true) => new CastResult(true, message, consumesCard);
        }

        /// <summary>Whether `move` is a self/ally utility effect (a stat buff and/or a next-turn AP grant) that CastSelfUtility can resolve - aimed either at the caster alone (Range 0, not HitsAllAdjacent) or every adjacent ally (HitsAllAdjacent + TargetsAllies). A card with InflictsStatusEffect set but none of the stat-buff fields (e.g. Meditate before it gets its own IsMeditateSpell handling) describes a mechanic this can't fully resolve and is deliberately excluded - applying only part of a card's effect would misrepresent what it does.</summary>
        public static bool IsSelfUtility(Move move)
        {
            bool hasEffect = move.StrengthBuff > 0 || move.AccuracyBuff > 0 || move.DefenseBuff > 0
                || move.ResistanceBuff > 0 || move.IntelligenceBuff > 0 || move.GrantedAP > 0;
            if (!hasEffect || move.BaseDamage > 0 || move.IsBlink || move.IsManaShield || move.IsAllyShield || move.IsSleepSpell || move.IsMeditateSpell)
                return false;
            return (move.Range == 0 && !move.HitsAllAdjacent) || (move.HitsAllAdjacent && move.TargetsAllies);
        }

        /// <summary>Whether Cast can resolve `move` entirely on its own, with no extra targeting step from Game1.</summary>
        public static bool CanResolveInstantly(Move move) =>
            move.IsManaShield || move.IsSleepSpell || move.IsMeditateSpell || move.IsConjurePotions || IsSelfUtility(move);

        /// <summary>
        /// Cast one of the instant effects CanResolveInstantly recognizes. `adjacentAllies` is
        /// every living unit on the caster's own team within Move.Range of it, EXCLUDING the
        /// caster itself (only actually read for a HitsAllAdjacent utility spell like Rally Cry -
        /// pass an empty list for anything self-only).
        /// </summary>
        public static CastResult Cast(SpellCard card, BaseUnit caster, List<BaseUnit> adjacentAllies)
        {
            var move = card.Effect;

            if (move.IsManaShield) return CastManaShield(caster);
            if (move.IsSleepSpell) return CastSleepSpell(card, caster);
            if (move.IsMeditateSpell) return CastMeditateSpell(card, caster);
            if (move.IsConjurePotions) return CastConjurePotions(card, caster);
            if (IsSelfUtility(move)) return CastSelfUtility(card, caster, adjacentAllies);

            return CastResult.Fail($"{card.Name} can't be cast yet - no execution system for that effect.");
        }

        private static bool CanAfford(Move move, BaseUnit caster) =>
            caster.GetEffectiveAPCost(move, null) <= caster.CurrentAP && move.MPCost <= caster.CurrentMP;

        private static void PayCost(Move move, BaseUnit caster)
        {
            caster.CurrentAP = System.Math.Max(0, caster.CurrentAP - caster.GetEffectiveAPCost(move, null));
            caster.CurrentMP = System.Math.Max(0, caster.CurrentMP - move.MPCost);
        }

        /// <summary>Mana Shield: pays its own AP/MP, then grants a shield equal to whatever MP remains after that (BaseUnit.ApplyShield, ShieldDrainsMana true so the shield and MP drain together as it absorbs hits). Casting it again while already mana-shielded toggles the shield off instead of re-paying, matching the original "can be turned off at will" design.</summary>
        private static CastResult CastManaShield(BaseUnit caster)
        {
            if (caster.ShieldPoints > 0 && caster.ShieldDrainsMana)
            {
                caster.ClearShield();
                return CastResult.Ok($"{caster.Name} drops their Mana Shield.", consumesCard: false);
            }

            var move = SpellCatalog.ManaShield.Effect;
            if (!CanAfford(move, caster))
                return CastResult.Fail($"Not enough AP/MP for {move.Name}.");

            PayCost(move, caster);
            caster.ApplyShield(caster.CurrentMP, drainsMana: true);
            return CastResult.Ok($"{caster.Name} raises a Mana Shield ({caster.ShieldPoints} points).");
        }

        /// <summary>Deep Sleep (e.g. Forced Sleep): pays AP/MP, heals Move.HealPercentMaxHP of max HP immediately, then applies a "Deep Sleep" StatusEffect - RestrictsActions, "Wake Up" for 1 AP to end it, healing that same % again at the start of every one of the caster's own turns while it stays active (BaseUnit.TickStatusEffects). No fixed duration - it persists until Wake Up actually clears it. The immediate heal is what makes "cast, Wake Up right back up" a real quick-heal combo - without it the first heal wouldn't land until TickStatusEffects next ran at the start of the caster's NEXT turn, which never happens if Wake Up is used the same turn it was cast (the same gap Meditate's stacking had, fixed the same way).</summary>
        private static CastResult CastSleepSpell(SpellCard card, BaseUnit caster)
        {
            var move = card.Effect;
            if (!CanAfford(move, caster))
                return CastResult.Fail($"Not enough AP/MP for {move.Name}.");

            PayCost(move, caster);
            caster.Heal((int)System.Math.Round(caster.MaxHP * move.HealPercentMaxHP));
            caster.ApplyStatusEffect(new StatusEffect
            {
                Name = "Deep Sleep",
                Type = StatusEffectType.Buff,
                RestrictsActions = true,
                EndEffectLabel = "Wake Up",
                EndEffectAPCost = 1,
                HealPercentMaxHPPerTurn = move.HealPercentMaxHP,
            });

            return CastResult.Ok($"{caster.Name} falls into a Deep Sleep, healing {move.HealPercentMaxHP:P0} of their max HP.");
        }

        /// <summary>Conjure Potions: pays AP/MP, then grants the caster one each of a Minor HP/MP/AP Potion (ConsumableCatalog) straight into its Consumables - see BaseUnit.AddConsumable. No targeting needed (always self), consumed from hand like any other card.</summary>
        private static CastResult CastConjurePotions(SpellCard card, BaseUnit caster)
        {
            var move = card.Effect;
            if (!CanAfford(move, caster))
                return CastResult.Fail($"Not enough AP/MP for {move.Name}.");

            PayCost(move, caster);
            caster.AddConsumable(ConsumableCatalog.MinorHPPotion);
            caster.AddConsumable(ConsumableCatalog.MinorMPPotion);
            caster.AddConsumable(ConsumableCatalog.MinorAPPotion);

            return CastResult.Ok($"{caster.Name} conjures a Minor HP Potion, a Minor MP Potion, and a Minor AP Potion.");
        }

        // Meditate's bonus to every stat at once for 1/2/3 stacks (+2/+5/+8 - not a flat
        // per-stack multiple, rebalanced down from an earlier flat +5/stack that made 3 stacks
        // of Meditate (+15 to everything) too strong) and how many stacks it caps at - first-pass
        // placeholders, pending a further balance pass. Meditate is the only spell that stacks
        // like this, so these live here rather than as more fields on Move.
        private static readonly int[] MeditateStackBonuses = { 2, 5, 8 };
        private const int MeditateMaxStacks = 3;

        /// <summary>Meditate: if not already meditating, pays AP/MP and starts it - a "Meditating" StatusEffect (RestrictsActions, GrowsOverTime up to MeditateMaxStacks, interrupted outright by a hit for 10%+ of max HP). The card stays in hand (ConsumesCard: false) since it doubles as the off switch too: casting it again while already meditating ends it instead, no AP/MP spent (same "recast to toggle off" convention as Mana Shield), locking the current stack bonus in as a real, fixed-duration buff via BaseUnit.TryEndRestrictingEffect.</summary>
        private static CastResult CastMeditateSpell(SpellCard card, BaseUnit caster)
        {
            var move = card.Effect;

            if (caster.IsMeditating)
            {
                caster.TryEndRestrictingEffect();
                return CastResult.Ok($"{caster.Name} ends their meditation, locking in the bonus.", consumesCard: false);
            }

            if (!CanAfford(move, caster))
                return CastResult.Fail($"Not enough AP/MP for {move.Name}.");

            PayCost(move, caster);
            caster.ApplyStatusEffect(new StatusEffect
            {
                Name = move.InflictsStatusEffect ?? move.Name,
                Type = StatusEffectType.Buff,
                RestrictsActions = true,
                EndEffectLabel = "End Meditation",
                EndEffectAPCost = 1,
                GrowsOverTime = true,
                // Starts at 1 stack, not 0 - TickStatusEffects only grows Stacks at the start of
                // the caster's OWN next turn, which hasn't happened yet on the very turn Meditate
                // is cast. Without this, the bonus fields below would have nothing to show until
                // then despite the "begins meditating" message - the stat display should reflect
                // the buff immediately. StackBonusTable is what TickStatusEffects reads to update
                // these same fields as Stacks grows further (see StatusEffect.StackBonusTable).
                Stacks = 1,
                MaxStacks = MeditateMaxStacks,
                StackBonusTable = MeditateStackBonuses,
                StrengthBonus = MeditateStackBonuses[0],
                AccuracyBonus = MeditateStackBonuses[0],
                DefenseBonus = MeditateStackBonuses[0],
                ResistanceBonus = MeditateStackBonuses[0],
                IntelligenceBonus = MeditateStackBonuses[0],
                LockInDurationTurns = move.StatusDurationTurns,
                InterruptDamagePercentMaxHP = 0.10f,
            });

            return CastResult.Ok($"{caster.Name} begins meditating.", consumesCard: false);
        }

        /// <summary>Brace/Rally Cry/Steady Hands/Second Wind: pays AP/MP, then immediately applies a Strength/Accuracy/Defense/Resistance/Intelligence buff (BaseUnit.ApplyStatusEffect) and/or a next-turn AP grant (BaseUnit.BonusAPNextTurn) to the caster alone, or to every adjacent ally for a HitsAllAdjacent one.</summary>
        private static CastResult CastSelfUtility(SpellCard card, BaseUnit caster, List<BaseUnit> adjacentAllies)
        {
            var move = card.Effect;
            if (!CanAfford(move, caster))
                return CastResult.Fail($"Not enough AP/MP for {move.Name}.");

            List<BaseUnit> targets;
            if (move.HitsAllAdjacent)
            {
                if (adjacentAllies == null || adjacentAllies.Count == 0)
                    return CastResult.Fail($"No one nearby to use {move.Name} on.");
                targets = adjacentAllies;
            }
            else
            {
                targets = new List<BaseUnit> { caster };
            }

            PayCost(move, caster);

            bool hasStatBuff = move.StrengthBuff > 0 || move.AccuracyBuff > 0 || move.DefenseBuff > 0
                || move.ResistanceBuff > 0 || move.IntelligenceBuff > 0;
            string buffName = move.InflictsStatusEffect ?? move.Name;
            foreach (var target in targets)
            {
                if (hasStatBuff)
                {
                    target.ApplyStatusEffect(new StatusEffect
                    {
                        Name = buffName,
                        Type = StatusEffectType.Buff,
                        TurnsRemaining = move.StatusDurationTurns,
                        StrengthBonus = move.StrengthBuff,
                        AccuracyBonus = move.AccuracyBuff,
                        DefenseBonus = move.DefenseBuff,
                        ResistanceBonus = move.ResistanceBuff,
                        IntelligenceBonus = move.IntelligenceBuff,
                    });
                }
                if (move.GrantedAP > 0)
                    target.BonusAPNextTurn += move.GrantedAP;
            }

            string message = move.HitsAllAdjacent
                ? $"{caster.Name} uses {move.Name} on {targets.Count} {(targets.Count == 1 ? "ally" : "allies")}."
                : $"{caster.Name} uses {move.Name} on themself.";
            return CastResult.Ok(message);
        }
    }
}
