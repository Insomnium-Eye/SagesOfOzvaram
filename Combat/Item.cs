namespace SagesOfOzvaram.Combat
{
    /// <summary>Which section of the Items menu an Item belongs to - drives both how it's grouped there and which actions Game1's item-detail view offers for it (Equip/Unequip for a Weapon, Use for a Consumable).</summary>
    public enum ItemCategory
    {
        Weapon,
        Equipment, // not yet implemented - armor/trinkets/etc. with no attacks of their own; no concrete type exists yet, this is just reserved for when one does
        Consumable,
    }

    /// <summary>
    /// Base for anything a unit can carry and see listed in the Items menu - a Weapon, a future
    /// Equipment type, or a ConsumableItem (e.g. a potion). Common ground is just a Name, a
    /// Description, and which Category it belongs to; everything category-specific (a Weapon's
    /// Attacks, a ConsumableItem's Use effect) lives on the subclass. See BaseUnit.Inventory
    /// (Weapon) / BaseUnit.Consumables (ConsumableItem) for where each kind is actually stored,
    /// and Game1.OpenItemsMenu for where they're combined into one list.
    /// </summary>
    public abstract class Item
    {
        public string Name { get; }
        public string Description { get; }
        public abstract ItemCategory Category { get; }

        protected Item(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
