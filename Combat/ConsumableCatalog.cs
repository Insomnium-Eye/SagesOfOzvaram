namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Shared consumable item definitions (see ConsumableItem). Each property returns a fresh
    /// instance, same reasoning as WeaponCatalog - so e.g. two separate Conjure Potions casts
    /// don't end up with reference-identical potions sitting in two different units' Consumables.
    /// Numbers are first-pass placeholders, pending a balance pass, same as everywhere else.
    /// </summary>
    public static class ConsumableCatalog
    {
        public static ConsumableItem MinorHPPotion => new ConsumableItem(
            "Minor HP Potion",
            "A small vial of restorative tonic - heals 10 HP the moment it's drunk.",
            healHP: 10);

        public static ConsumableItem MinorMPPotion => new ConsumableItem(
            "Minor MP Potion",
            "A faintly glowing draught - restores 10 MP the moment it's drunk.",
            restoreMP: 10);

        public static ConsumableItem MinorAPPotion => new ConsumableItem(
            "Minor AP Potion",
            "A sharp, bracing tonic - grants 2 AP the moment it's drunk.",
            grantAP: 2);
    }
}
