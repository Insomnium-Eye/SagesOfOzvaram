namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// A single-use Item that applies an instant effect to whoever uses it, then is gone (e.g.
    /// the Minor HP/MP/AP Potions Conjure Potions grants - see ConsumableCatalog). A brand new
    /// consumable just needs values for whichever of HealHP/RestoreMP/GrantAP it actually uses;
    /// 0 means that part of Use is skipped. See BaseUnit.Consumables for where these are stored,
    /// and Game1's item-detail "Use (1 AP)" action for how one actually gets consumed (Use
    /// applies the effect; the caller is the one that spends the AP and removes it from
    /// Consumables - same split SpellCaster keeps between resolving an effect and the UI-side
    /// bookkeeping around it).
    /// </summary>
    public class ConsumableItem : Item
    {
        public override ItemCategory Category => ItemCategory.Consumable;

        public int HealHP { get; }
        public int RestoreMP { get; }
        public int GrantAP { get; }

        public ConsumableItem(string name, string description, int healHP = 0, int restoreMP = 0, int grantAP = 0)
            : base(name, description)
        {
            HealHP = healHP;
            RestoreMP = restoreMP;
            GrantAP = grantAP;
        }

        /// <summary>Apply this item's effect to `unit` and return a message describing what happened. Does NOT spend AP or remove itself from the unit's Consumables - the caller handles both.</summary>
        public string Use(SagesOfOzvaram.Units.BaseUnit unit)
        {
            if (HealHP > 0)
                unit.Heal(HealHP);
            if (RestoreMP > 0)
                unit.CurrentMP = System.Math.Min(unit.MaxMP, unit.CurrentMP + RestoreMP);
            if (GrantAP > 0)
                unit.CurrentAP = System.Math.Min(unit.MaxAP, unit.CurrentAP + GrantAP);

            return $"{unit.Name} uses {Name}.";
        }
    }
}
