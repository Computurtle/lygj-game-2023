using System;
using System.Collections;
using System.Collections.Generic;
using LYGJ.Common;
using UnityEngine;
using UnityEngine.Events;

namespace LYGJ.InventoryManagement {
    public sealed class Inventory : SingletonMB<Inventory>, IReadOnlyCollection<ItemInstance> {
        readonly Dictionary<Item, uint> _Inventory = new(16);

        [SerializeField, Tooltip("Raised when an item is added to the inventory.")]
        UnityEvent<ItemInstance> _OnItemAdded = new();

        /// <summary> Raised when an item is added to the inventory. </summary>
        public static event UnityAction<ItemInstance> Added {
            add => Instance._OnItemAdded.AddListener(value);
            remove => Instance._OnItemAdded.RemoveListener(value);
        }

        [SerializeField, Tooltip("Raised when an item is removed from the inventory.")]
        UnityEvent<ItemInstance> _OnItemRemoved = new();

        /// <summary> Raised when an item is removed from the inventory. </summary>
        public static event UnityAction<ItemInstance> Removed {
            add => Instance._OnItemRemoved.AddListener(value);
            remove => Instance._OnItemRemoved.RemoveListener(value);
        }

        [SerializeField, Tooltip("Raised when the inventory is cleared.")]
        UnityEvent _OnInventoryCleared = new();

        /// <summary> Raised when the inventory is cleared. </summary>
        public static event UnityAction Cleared {
            add => Instance._OnInventoryCleared.AddListener(value);
            remove => Instance._OnInventoryCleared.RemoveListener(value);
        }

        /// <summary> Adds the specified item to the inventory. </summary>
        /// <param name="Item"> The item to add. </param>
        public static void Add( ItemInstance Item ) => Instance.AddInternal(Item);

        /// <inheritdoc cref="Add(ItemInstance)"/>
        /// <param name="Item"> The item to add. </param>
        /// <param name="Amount"> The amount of the item to add. </param>
        public static void Add( Item Item, uint Amount = 1u ) {
            if (Amount == 0u) {
                Debug.LogWarning("Cannot add zero items.");
                return;
            }
            Instance.AddInternal(new(Item, Amount));
        }

        void AddInternal( ItemInstance Item ) {
            if (_Inventory.ContainsKey(Item.Item)) {
                _Inventory[Item.Item] += Item.Amount;
            } else {
                _Inventory.Add(Item.Item, Item.Amount);
            }
            _OnItemAdded.Invoke(Item);
        }

        /// <summary> Removes the specified item from the inventory. </summary>
        /// <param name="Item"> The item to remove. </param>
        /// <exception cref="KeyNotFoundException"> Thrown if the item is not in the inventory. </exception>
        /// <exception cref="NotEnoughItemsException"> Thrown if there are not enough items to remove. </exception>
        public static void Remove( ItemInstance Item ) => Instance.RemoveInternal(Item.Item, Item.Amount);

        /// <summary> Removes the specified item from the inventory. </summary>
        /// <param name="Item"> The item to remove. </param>
        /// <param name="Amount"> The amount of the item to remove. </param>
        public static void Remove( Item Item, uint Amount = 1u ) => Instance.RemoveInternal(Item, Amount);

        void RemoveInternal(Item Item, uint Amount) {
            if (!_Inventory.TryGetValue(Item, out uint CurrentAmount)) {
                throw new KeyNotFoundException($"Item {Item} is not in the inventory.");
            }
            if (CurrentAmount < Amount) {
                throw new NotEnoughItemsException(Item, Amount, CurrentAmount);
            }
            if (CurrentAmount == Amount) {
                _Inventory.Remove(Item);
            } else {
                _Inventory[Item] -= Amount;
            }
            _OnItemRemoved.Invoke(new(Item, Amount));
        }

        /// <summary> Removes all of a specified item from the inventory. </summary>
        /// <param name="Item"> The item to remove. </param>
        /// <exception cref="KeyNotFoundException"> Thrown if the item is not in the inventory. </exception>
        /// <returns> The amount of the item that was removed. </returns>
        public static uint RemoveAll( Item Item ) => Instance.RemoveAllInternal(Item);

        uint RemoveAllInternal(Item Item) {
            if (!_Inventory.TryGetValue(Item, out uint Amount)) {
                throw new KeyNotFoundException($"Item {Item} is not in the inventory.");
            }
            _Inventory.Remove(Item);
            _OnItemRemoved.Invoke(new(Item, Amount));
            return Amount;
        }

        /// <summary> Checks if the inventory contains the specified item. </summary>
        /// <param name="Item"> The item to check for. </param>
        /// <returns> <see langword="true"/> if the inventory contains the item; otherwise, <see langword="false"/>. </returns>
        public static bool Contains( Item Item ) => Instance._Inventory.ContainsKey(Item);

        /// <summary> Checks if the inventory contains the specified item in the given amount. </summary>
        /// <param name="Item"> The item to check for. </param>
        /// <returns> <see langword="true"/> if the inventory contains an equivalent or greater amount of the item; otherwise, <see langword="false"/>. </returns>
        public static bool Contains( ItemInstance Item ) => Instance._Inventory.TryGetValue(Item.Item, out uint Amount) && Amount >= Item.Amount;

        /// <inheritdoc cref="Contains(Item)"/>
        /// <param name="Item"> The item to check for. </param>
        /// <param name="Amount"> The amount of the item to check for. </param>
        public static bool Contains( Item Item, uint Amount ) {
            if (Amount == 0u) {
                Debug.LogWarning("Should not check for zero items.");
                return true;
            }
            return Instance._Inventory.TryGetValue(Item, out uint CurrentAmount) && CurrentAmount >= Amount;
        }

        /// <summary> Gets the amount of the specified item in the inventory. </summary>
        /// <param name="Item"> The item to get the amount of. </param>
        /// <returns> The amount of the item in the inventory, or <c>0</c> if the item is not in the inventory. </returns>
        public static uint Count( Item Item ) => Instance._Inventory.TryGetValue(Item, out uint Amount) ? Amount : 0u;

        /// <summary> Gets the amount of unique items in the inventory. </summary>
        /// <returns> The amount of unique items in the inventory. </returns>
        public static int UniqueItemCount => Instance._Inventory.Count;

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<ItemInstance> GetEnumerator() {
            foreach (KeyValuePair<Item, uint> Pair in _Inventory) {
                yield return Pair;
            }
        }

        /// <inheritdoc cref="GetEnumerator"/>
        public static IEnumerator<ItemInstance> All => Instance.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Implementation of IReadOnlyCollection<out ItemInstance>

        /// <inheritdoc />
        int IReadOnlyCollection<ItemInstance>.Count => _Inventory.Count;

        #endregion

    }

    public abstract class InventoryException : Exception {
        protected InventoryException( string Message ) : base(Message) { }
    }

    public sealed class NotEnoughItemsException : InventoryException {
        public NotEnoughItemsException( Item Item, uint Requested, uint Available ) : base($"Not enough items. Requested {Requested:N0} {Item}, but only {Available:N0} {Item} are available.") { }
    }
}
