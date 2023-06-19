using System;

namespace LYGJ.InventoryManagement {
    public enum ItemType { // General fantasy rpg categories (specifically Fable)
        // Weapons
        [ItemGroup(ItemGroup.Weapon)] Melee,
        [ItemGroup(ItemGroup.Weapon)] Ranged,
        // Armour
        [ItemGroup(ItemGroup.Armour)] Head,
        [ItemGroup(ItemGroup.Armour)] Chest,
        [ItemGroup(ItemGroup.Armour)] Hands,
        [ItemGroup(ItemGroup.Armour)] Legs,
        [ItemGroup(ItemGroup.Armour)] Feet,
        // Accessories
        [ItemGroup(ItemGroup.Accessory)] Ring,
        [ItemGroup(ItemGroup.Accessory)] Amulet,
        // Consumables
        [ItemGroup(ItemGroup.Consumable)] Food,
        [ItemGroup(ItemGroup.Consumable)] Potion,
        // Misc
        [ItemGroup(ItemGroup.Misc)] Quest,
        [ItemGroup(ItemGroup.Misc)] Junk,
        // Special
        [ItemGroup(ItemGroup.Special)] Key,
        // Currency
        [ItemGroup(ItemGroup.Currency)] Gold,
        [ItemGroup(ItemGroup.Currency)] Silver,
        [ItemGroup(ItemGroup.Currency)] Copper
    }

    public enum ItemGroup {
        Weapon           = 0,
        Armour           = 1,
        [Obsolete] Armor = Armour,
        Accessory        = 2,
        Consumable       = 3,
        Misc             = 4,
        Special          = 5,
        Currency         = 6
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ItemGroupAttribute : Attribute {
        /// <summary> The group this item belongs to. </summary>
        public ItemGroup Group { get; }

        /// <summary> Defines the group this item belongs to. </summary>
        /// <param name="Group"> The group this item belongs to. </param>
        public ItemGroupAttribute( ItemGroup Group ) => this.Group = Group;
    }
}
