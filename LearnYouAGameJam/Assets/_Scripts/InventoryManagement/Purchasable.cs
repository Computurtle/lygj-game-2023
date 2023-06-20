using System;
using System.Collections.Generic;
using UnityEngine;

namespace LYGJ.InventoryManagement {
    [Serializable]
    public sealed class Purchasable {
        [SerializeField, Tooltip("The item to purchase.")] ItemInstance   _Item = null!;
        [SerializeField, Tooltip("The cost of the item.")] ItemInstance[] _Cost = Array.Empty<ItemInstance>();

        /// <summary> The item to purchase. </summary>
        public ItemInstance Item => _Item;

        /// <summary> The cost of the item. </summary>
        public IReadOnlyCollection<ItemInstance> Cost => _Cost;

        /// <summary> Creates a new purchasable item. </summary>
        /// <param name="Item"> The item to purchase. </param>
        /// <param name="Cost"> The cost of the item. </param>
        public Purchasable( ItemInstance Item, params ItemInstance[] Cost ) {
            _Item = Item;
            _Cost = Cost;
        }

        [Obsolete("This constructor is for serialization only and should not be used.")]
        public Purchasable() { }

        #region Overrides of ValueType

        /// <inheritdoc />
        public override string ToString() => $"{Item} for {string.Join(", ", Cost)}";

        #endregion

        /// <summary> Determines whether the player can afford the item. </summary>
        /// <returns> <see langword="true"/> if the player can afford the item; otherwise, <see langword="false"/>. </returns>
        public bool CanAfford {
            get {
                foreach (ItemInstance Item in Cost) {
                    if (!Inventory.Contains(Item)) {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary> Purchases the item. </summary>
        /// <exception cref="InvalidOperationException"> The player cannot afford the item. </exception>
        public void Purchase() {
            if (!CanAfford) {
                throw new InvalidOperationException("The player cannot afford the item.");
            }

            foreach (ItemInstance Item in Cost) {
                Inventory.Remove(Item);
            }

            Inventory.Add(Item);
        }

    }
}
