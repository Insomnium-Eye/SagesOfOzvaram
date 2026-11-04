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

        /// <summary>Dropped by a slain Wild beast (see Game1's death sweep/DroppedItem), picked up via the "Pick Up" Action - unlike the 3 potions above, this one actually takes item-box space (Weight 1), since it's something found in the field rather than freely granted by a spell.</summary>
        public static ConsumableItem Meat => new ConsumableItem(
            "Meat",
            "Fresh meat from a slain beast - heals 10 HP, restores 10 MP, and grants 3 AP the moment it's eaten.",
            healHP: 10, restoreMP: 10, grantAP: 3, weight: 1);

        /// <summary>Dropped by an Aetherfluff Beetle's Consume (on request - see Combat.RaceAttacks' AetherfluffBeetle entry, which eats the grass object off its own tile), picked up via the "Pick Up" Action same as Meat - takes item-box space (Weight 1), since it's found in the field rather than freely granted.</summary>
        public static ConsumableItem ManaPowder => new ConsumableItem(
            "Mana Powder",
            "A fine, faintly shimmering dust ground from consumed grass - restores 10 MP the moment it's used.",
            restoreMP: 10, weight: 1);
    }
}
