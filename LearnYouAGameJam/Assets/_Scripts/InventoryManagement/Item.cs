using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LYGJ.Common;
using LYGJ.EntitySystem.PlayerManagement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.InventoryManagement {
    [AssetsOnly, AssetSelector]
    [CreateAssetMenu(fileName = "New Item", menuName = "LYGJ/Inventory/Item")]
    public sealed class Item : ScriptableObject, IEquatable<Item>, IComparable<Item>, IComparable {
        /// <summary> The item ID. </summary>
        [field: SerializeField, Tooltip("The item ID.")]
        public string ID {
            get;
            #if !UNITY_EDITOR
            private
            #endif
            set;
        } = string.Empty;

        /// <summary> The item name. </summary>
        [field: SerializeField, Tooltip("The item name.")]
        public string Name {
            get;
            #if !UNITY_EDITOR
            private
            #endif
            set;
        } = string.Empty;

        /// <summary> The item description. </summary>
        [field: SerializeField, Multiline, Tooltip("The item description.")]
        public string Description { get; private set; } = string.Empty;

        /// <summary> The item type. </summary>
        [field: SerializeField, Tooltip("The item type."), HorizontalGroup("Type")]
        public ItemType Type { get; private set; }

        /// <summary> The item group. </summary>
        [ShowInInspector, ReadOnly, Tooltip("The item group."), HorizontalGroup("Type")]
        public ItemGroup Group => Type.GetAttribute<ItemType, ItemGroupAttribute>().Group;

        /// <summary> The item icon. </summary>
        [field: SerializeField, Tooltip("The item icon.")]
        public Sprite? Icon {
            get;
            #if !UNITY_EDITOR
            private
            #endif
            set;
        } = null;

        #if UNITY_EDITOR
        void Reset() {
            string Path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string FileName = System.IO.Path.GetFileNameWithoutExtension(Path);
            ID   = FileName;
            Name = FileName.ConvertNamingConvention(NamingConvention.TitleCase);
        }
        #endif

        /// <summary> Whether or not the given item type is an equippable type. </summary>
        /// <param name="ItemType"> The item type to check. </param>
        /// <returns> <see langword="true"/> if the item type is equippable, <see langword="false"/> otherwise. </returns>
        public static bool IsEquippableType( ItemType ItemType ) =>
            ItemType switch {
                ItemType.Melee  => true,
                ItemType.Ranged => true,
                _               => false
            };

        /// <summary> The item equipment. </summary>
        [field: SerializeField, Tooltip("The item equipment."), AssetsOnly, ShowIf("@" + nameof(IsEquippableType) + "(" + nameof(Type) + ")")]
        public PlayerEquipment? Equipment {
            get;
            #if !UNITY_EDITOR
            private
            #endif
            set;
        } = null;

        /// <summary> Whether or not the item is equippable. </summary>
        [MemberNotNullWhen(true, nameof(Equipment))]
        [ShowInInspector, ReadOnly, Tooltip("Whether or not the item is equippable."), ToggleLeft]
        #if UNITY_EDITOR
        [ValidateInput(nameof(Editor_MeleeEquipInvalid), "Equipment type was unexpected. Item type is melee, but the equipment does not implement MeleeEquipment.", InfoMessageType.Error)]
        [ValidateInput(nameof(Editor_RangedEquipInvalid), "Equipment type was unexpected. Item type is ranged, but the equipment does not implement RangedEquipment.", InfoMessageType.Error)]
        #endif
        public bool IsEquippable => IsEquippableType(Type) && Equipment != null;

        #if UNITY_EDITOR
        bool Editor_MeleeEquipInvalid()  => Type != ItemType.Melee || Equipment == null || Equipment is MeleeEquipment;
        bool Editor_RangedEquipInvalid() => Type != ItemType.Ranged || Equipment == null || Equipment is RangedEquipment;
        #endif

        #region Overrides of Object

        /// <inheritdoc />
        public override string ToString() => $"{Name} ({ID})";

        #endregion

        #region Equality Members

        /// <inheritdoc />
        public bool Equals( Item? Other ) {
            if (ReferenceEquals(null, Other)) {
                return false;
            }

            if (ReferenceEquals(this, Other)) {
                return true;
            }

            return base.Equals(Other)
                && ID == Other.ID;
        }

        /// <inheritdoc />
        public override bool Equals( object? Obj ) => ReferenceEquals(this, Obj) || Obj is Item Other && Equals(Other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ID);

        public static bool operator ==( Item? Left, Item? Right ) => Equals(Left, Right);
        public static bool operator !=( Item? Left, Item? Right ) => !Equals(Left, Right);

        #endregion

        #region Relational Members

        /// <inheritdoc />
        public int CompareTo( Item? Other ) {
            if (ReferenceEquals(this, Other)) {
                return 0;
            }

            if (ReferenceEquals(null, Other)) {
                return 1;
            }

            return string.Compare(Name, Other.Name, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public int CompareTo( object? Obj ) {
            if (ReferenceEquals(null, Obj)) {
                return 1;
            }

            if (ReferenceEquals(this, Obj)) {
                return 0;
            }

            return Obj is Item Other ? CompareTo(Other) : throw new ArgumentException($"Object must be of type {nameof(Item)}");
        }

        public static bool operator <( Item?  Left, Item? Right ) => Comparer<Item?>.Default.Compare(Left, Right) < 0;
        public static bool operator >( Item?  Left, Item? Right ) => Comparer<Item?>.Default.Compare(Left, Right) > 0;
        public static bool operator <=( Item? Left, Item? Right ) => Comparer<Item?>.Default.Compare(Left, Right) <= 0;
        public static bool operator >=( Item? Left, Item? Right ) => Comparer<Item?>.Default.Compare(Left, Right) >= 0;

        #endregion

        public static ItemInstance operator *( Item Item, in uint Amount ) => new(Item, Amount);

    }

    [Serializable, InlineProperty]
    public sealed class ItemInstance : ISerializationCallbackReceiver {
        /// <summary> The item. </summary>
        [field: SerializeField, HorizontalGroup, HideLabel]
        public Item Item { get; private set; } = null!;

        /// <summary> The item amount. </summary>
        [field: SerializeField, HorizontalGroup(0.3f), LabelText("x"), LabelWidth(20f)]
        public uint Amount { get; private set; } = 1u;

        /// <summary> Whether this is a 'none' item. </summary>
        public readonly bool IsNone = false;

        /// <summary> The item name. </summary>
        /// <param name="Item"> The item. </param>
        /// <param name="Amount"> The item amount. </param>
        /// <exception cref="ArgumentNullException"> Thrown if <paramref name="Item"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="Amount"/> is zero. </exception>
        public ItemInstance( Item Item, in uint Amount = 1u ) {
            if (Item   == null) { throw new ArgumentNullException(nameof(Item), "Item cannot be null."); }
            if (Amount == 0u) { throw new ArgumentOutOfRangeException(nameof(Amount), Amount, "Amount must be greater than zero."); }
            this.Item   = Item;
            this.Amount = Amount;
            IsNone      = false;
        }

        [Obsolete("This constructor is for serialization only and should not be used.")]
        public ItemInstance() { }

        ItemInstance( Item Item, in uint Amount, bool IsNone ) {
            if (!IsNone) {
                if (Item   == null) { throw new ArgumentNullException(nameof(Item), "Item cannot be null."); }
                if (Amount == 0u) { throw new ArgumentOutOfRangeException(nameof(Amount), Amount, "Amount must be greater than zero."); }
            }
            this.Item   = Item;
            this.Amount = Amount;
            this.IsNone = IsNone;
        }

        /// <summary> An empty item instance. </summary>
        public static ItemInstance Empty => new(null!, 0u, true);

        #region Casts

        public static implicit operator Item( in ItemInstance ItemInstance ) => ItemInstance.Item;
        public static implicit operator uint( in ItemInstance ItemInstance ) => ItemInstance.Amount;

        public static implicit operator ItemInstance( in Item Item ) => new(Item, 1u);

        public static implicit operator ItemInstance( KeyValuePair<Item, uint> Pair ) => new(Pair.Key, Pair.Value);

        #endregion

        #region Operators

        public static ItemInstance operator +( in ItemInstance Left, in uint Right ) => new(Left.Item, Left.Amount + Right);
        public static ItemInstance operator +( in ItemInstance Left, in ItemInstance Right ) {
            if (Left.Item != Right.Item) { throw new InvalidOperationException("Cannot add two different items."); }
            return new(Left.Item, Left.Amount + Right.Amount);
        }

        public static ItemInstance operator -( in ItemInstance Left, in uint Right ) => new(Left.Item, Left.Amount - Right);
        public static ItemInstance operator -( in ItemInstance Left, in ItemInstance Right ) {
            if (Left.Item   != Right.Item) { throw new InvalidOperationException("Cannot subtract two different items."); }
            if (Left.Amount < Right.Amount) { throw new InvalidOperationException("Cannot subtract more items than exist."); }
            if (Left.Amount == Right.Amount) { throw new InvalidOperationException("Cannot subtract all items."); }

            return new(Left.Item, Left.Amount - Right.Amount);
        }

        #endregion

        /// <summary> Deconstructs the item instance. </summary>
        /// <param name="Item"> The item. </param>
        /// <param name="Amount"> The item amount. </param>
        public void Deconstruct( out Item Item, out uint Amount ) {
            Item   = this.Item;
            Amount = this.Amount;
        }

        #region Overrides of ValueType

        /// <inheritdoc />
        public override string ToString() => $"{Item} x{Amount:N0}";

        #endregion

        #region Implementation of ISerializationCallbackReceiver

        /// <inheritdoc />
        public void OnBeforeSerialize() {
            if (IsNone) {
                Debug.LogWarning("Should not serialise 'None' items.", this);
            }
        }

        /// <inheritdoc />
        public void OnAfterDeserialize() { }

        #endregion

    }
}
