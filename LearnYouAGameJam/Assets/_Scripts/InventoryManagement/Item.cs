using System;
using System.Collections.Generic;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.InventoryManagement {
    public sealed class Item : ScriptableObject, IEquatable<Item>, IComparable<Item>, IComparable {
        /// <summary> The item ID. </summary>
        [field: SerializeField, Tooltip("The item ID.")] public string ID { get; private set; } = string.Empty;

        /// <summary> The item name. </summary>
        [field: SerializeField, Tooltip("The item name.")] public string Name { get; private set; } = string.Empty;

        /// <summary> The item description. </summary>
        [field: SerializeField, Multiline, Tooltip("The item description.")] public string Description { get; private set; } = string.Empty;

        #if UNITY_EDITOR
        void Reset() {
            string Path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string Name = System.IO.Path.GetFileNameWithoutExtension(Path);
            ID        = Name;
            this.Name = Name.ConvertNamingConvention(NamingConvention.TitleCase);
        }
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

    [Serializable]
    public readonly struct ItemInstance {
        /// <summary> The item. </summary>
        [ShowInInspector, HorizontalGroup]
        public readonly Item Item;

        /// <summary> The item amount. </summary>
        [ShowInInspector, HorizontalGroup, LabelText("x"), LabelWidth(20f)]
        public readonly uint Amount;

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
        }

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

    }
}
