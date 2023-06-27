using System.Diagnostics.CodeAnalysis;
using LYGJ.Common;
using LYGJ.InventoryManagement;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerEquipper : SingletonMB<PlayerEquipper> {
        /// <summary> Whether or not the given item is an equippable type. </summary>
        /// <param name="Item"> The item to check. </param>
        /// <returns> <see langword="true"/> if the item is equippable, <see langword="false"/> otherwise. </returns>
        public static bool IsEquippable( Item Item ) =>
            Item.Type switch {
                ItemType.Melee when Item.Equipment is MeleeEquipment   => true,
                ItemType.Ranged when Item.Equipment is RangedEquipment => true,
                _                                                      => false
            };

        [SerializeField, Tooltip("The parent for the equipped weapons.")] Transform _Parent = null!;

        [Space]
        [ShowInInspector, HideInEditorMode, Tooltip("The equipped melee weapon.")]  RuntimePlayerEquipment?  _Melee;
        [ShowInInspector, HideInEditorMode, Tooltip("The equipped ranged weapon.")] RuntimePlayerEquipment? _Ranged;

        /// <summary> Whether or not the player has a melee weapon equipped. </summary>
        [MemberNotNullWhen(true, nameof(_Melee))]
        public bool IsMeleeItemEquipped => _Melee != null;

        /// <inheritdoc cref="IsMeleeItemEquipped"/>
        public static bool IsMeleeEquipped => Instance.IsMeleeItemEquipped;

        /// <summary> Whether or not the player has a ranged weapon equipped. </summary>
        [MemberNotNullWhen(true, nameof(_Ranged))]
        public bool IsRangedItemEquipped => _Ranged != null;

        /// <inheritdoc cref="IsRangedItemEquipped"/>
        public static bool IsRangedEquipped => Instance.IsRangedItemEquipped;

        /// <summary> Attempts to get the melee item currently equipped. </summary>
        /// <param name="Melee"> The melee item, if any. </param>
        /// <returns> <see langword="true"/> if a melee item is equipped, <see langword="false"/> otherwise. </returns>
        public static bool TryGetMeleeItem( [NotNullWhen(true)] out RuntimePlayerEquipment? Melee ) {
            Melee = Instance._Melee;
            return Melee != null;
        }

        /// <summary> Attempts to get the ranged item currently equipped. </summary>
        /// <param name="Ranged"> The ranged item, if any. </param>
        /// <returns> <see langword="true"/> if a ranged item is equipped, <see langword="false"/> otherwise. </returns>
        public static bool TryGetRangedItem( [NotNullWhen(true)]out RuntimePlayerEquipment? Ranged ) {
            Ranged = Instance._Ranged;
            return Ranged != null;
        }

        void UnequipItem<TRuntimeEquipment>( ref TRuntimeEquipment? RuntimeEquipment ) where TRuntimeEquipment : RuntimePlayerEquipment {
            if (RuntimeEquipment == null) { return; }
            RuntimeEquipment.Equipment.Unequip(RuntimeEquipment);
            RuntimeEquipment = null;
        }

        void EquipItem<TRuntimeEquipment, TEquipment>( ref TRuntimeEquipment? RuntimeEquipment, TEquipment Equipment ) where TRuntimeEquipment : RuntimePlayerEquipment where TEquipment : PlayerEquipment {
            if (RuntimeEquipment != null) {
                UnequipItem(ref RuntimeEquipment);
            }

            RuntimeEquipment = (TRuntimeEquipment)Equipment.Equip(_Parent);
        }

        /// <summary> Equips the given item. </summary>
        /// <param name="Item"> The item to equip. </param>
        public void EquipItem( Item Item ) {
            switch (Item.Type) {
                case ItemType.Melee when Item.Equipment is MeleeEquipment Melee:
                    EquipMeleeItem(Melee);
                    break;
                case ItemType.Ranged when Item.Equipment is RangedEquipment Ranged:
                    EquipRangedItem(Ranged);
                    break;
                default:
                    Debug.LogError($"Cannot equip item of type {Item.Type}.");
                    break;
            }
        }

        void EquipMeleeItem( MeleeEquipment Melee ) => EquipItem(ref _Melee, Melee);

        void EquipRangedItem( RangedEquipment Ranged ) => EquipItem(ref _Ranged, Ranged);

        /// <inheritdoc cref="EquipItem(Item)"/>
        public static void Equip( Item Item ) => Instance.EquipItem(Item);

        /// <inheritdoc cref="EquipItem(Item)"/>
        public static void Equip( PlayerEquipment Equipment ) {
            switch (Equipment) {
                case MeleeEquipment Melee:
                    Instance.EquipMeleeItem(Melee);
                    break;
                case RangedEquipment Ranged:
                    Instance.EquipRangedItem(Ranged);
                    break;
                default:
                    Debug.LogError($"Cannot equip item of type {Equipment.GetType().GetNiceName()}.");
                    break;
            }
        }

        /// <summary> Unequips the given item. </summary>
        /// <param name="Item"> The item to unequip. </param>
        public void UnequipItem( Item Item ) {
            switch (Item.Type) {
                case ItemType.Melee when Item.Equipment is MeleeEquipment:
                    UnequipMelee();
                    break;
                case ItemType.Ranged when Item.Equipment is RangedEquipment:
                    UnequipRanged();
                    break;
                default:
                    Debug.LogError($"Cannot unequip item of type {Item.Type}.");
                    break;
            }
        }

        void UnequipMelee() => UnequipItem(ref _Melee);

        void UnequipRanged() => UnequipItem(ref _Ranged);

        /// <inheritdoc cref="UnequipItem(Item)"/>
        public static void Unequip( Item Item ) => Instance.UnequipItem(Item);

        /// <inheritdoc cref="UnequipItem(Item)"/>
        public static void Unequip( PlayerEquipment Equipment ) {
            switch (Equipment) {
                case MeleeEquipment:
                    Instance.UnequipMelee();
                    break;
                case RangedEquipment:
                    Instance.UnequipRanged();
                    break;
                default:
                    Debug.LogError($"Cannot unequip item of type {Equipment.GetType().GetNiceName()}.");
                    break;
            }
        }

        /// <summary> Gets whether a melee item can currently be equipped. </summary>
        /// <returns> <see langword="true"/> if a melee item can be equipped, <see langword="false"/> otherwise. </returns>
        public static bool CanEquipMelee => !IsMeleeEquipped;

        /// <summary> Gets whether a ranged item can currently be equipped. </summary>
        /// <returns> <see langword="true"/> if a ranged item can be equipped, <see langword="false"/> otherwise. </returns>
        public static bool CanEquipRanged => !IsRangedEquipped;

        /// <summary> Gets whether a specific equipment can currently be equipped. </summary>
        /// <param name="Equipment"> The equipment to check. </param>
        /// <returns> <see langword="true"/> if the equipment item can be equipped, <see langword="false"/> otherwise. </returns>
        public static bool CanEquip( PlayerEquipment Equipment ) =>
            Equipment switch {
                MeleeEquipment  => CanEquipMelee,
                RangedEquipment => CanEquipRanged,
                _               => false
            };

        /// <summary> Gets whether a specific equipment can currently be unequipped. </summary>
        /// <param name="Equipment"> The equipment to check. </param>
        /// <returns> <see langword="true"/> if the equipment item can be unequipped, <see langword="false"/> otherwise. </returns>
        public static bool CanUnequip( PlayerEquipment Equipment ) =>
            Equipment switch {
                MeleeEquipment  => IsMeleeEquipped,
                RangedEquipment => IsRangedEquipped,
                _               => false
            };
    }

}
