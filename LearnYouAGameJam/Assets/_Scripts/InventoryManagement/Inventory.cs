using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using LYGJ.Common.Enums;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace LYGJ.InventoryManagement {
    public sealed class Inventory : SingletonMB<Inventory>, IReadOnlyCollection<ItemInstance> {
        [ShowInInspector, ListDrawerSettings(ShowFoldout = false, IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 10)]
        readonly Dictionary<Item, uint> _Inventory = new(16);

        [SerializeField, Tooltip("Raised when an item is added to the inventory."), FoldoutGroup("Events")]
        UnityEvent<ItemInstance> _OnItemAdded = new();

        /// <summary> Raised when an item is added to the inventory. </summary>
        public static event UnityAction<ItemInstance>? Added {
            add => Instance._OnItemAdded.AddListener(value);
            remove => Instance._OnItemAdded.RemoveListener(value);
        }

        [SerializeField, Tooltip("Raised when an item is removed from the inventory."), FoldoutGroup("Events")]
        UnityEvent<ItemInstance> _OnItemRemoved = new();

        /// <summary> Raised when an item is removed from the inventory. </summary>
        public static event UnityAction<ItemInstance>? Removed {
            add => Instance._OnItemRemoved.AddListener(value);
            remove => Instance._OnItemRemoved.RemoveListener(value);
        }

        [SerializeField, Tooltip("Raised when the inventory is cleared."), FoldoutGroup("Events")]
        UnityEvent _OnInventoryCleared = new();

        /// <summary> Raised when the inventory is cleared. </summary>
        public static event UnityAction? Cleared {
            add => Instance._OnInventoryCleared.AddListener(value);
            remove => Instance._OnInventoryCleared.RemoveListener(value);
        }

        public enum ChangeType {
            Add,
            Remove,
            Clear
        }

        public delegate void InventoryChanged( ChangeType ChangeType, ItemInstance ItemInstance );

        /// <summary> Raised when the inventory is changed. </summary>
        public static event InventoryChanged? Changed;

        protected override void Awake() {
            base.Awake();
            void OnItemAdded( ItemInstance   ItemInstance ) => Changed?.Invoke(ChangeType.Add, ItemInstance);
            void OnItemRemoved( ItemInstance ItemInstance ) => Changed?.Invoke(ChangeType.Remove, ItemInstance);
            void OnInventoryCleared()                       => Changed?.Invoke(ChangeType.Clear, ItemInstance.Empty);
            Added    += OnItemAdded;
            Removed  += OnItemRemoved;
            Cleared  += OnInventoryCleared;
        }

        /// <summary> Adds the specified item to the inventory. </summary>
        /// <param name="Item"> The item to add. </param>
        public static void Add( in ItemInstance Item ) => Instance.AddInternal(Item);

        /// <summary> Adds the specified item to the inventory. </summary>
        /// <param name="Item"> The item to add. </param>
        /// <param name="Amount"> The amount of the item to add. </param>
        [Button, HideInEditorMode]
        public static void Add( Item Item, [MinValue(1)] uint Amount = 1u ) {
            if (Amount == 0u) {
                Debug.LogWarning("Cannot add zero items.");
                return;
            }
            Instance.AddInternal(new(Item, Amount));
        }

        void AddInternal( in ItemInstance Item ) {
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
        [Button, HideInEditorMode]
        public static void Remove( Item Item, [MinValue(1)] uint Amount = 1u ) => Instance.RemoveInternal(Item, Amount);

        void RemoveInternal( Item Item, uint Amount ) {
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
        [Button, HideInEditorMode]
        public static uint RemoveAll( Item Item ) => Instance.RemoveAllInternal(Item);

        uint RemoveAllInternal( Item Item ) {
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
        [Button, HideInEditorMode]
        public static bool Contains( Item Item ) => Instance._Inventory.ContainsKey(Item);

        /// <summary> Checks if the inventory contains the specified item in the given amount. </summary>
        /// <param name="Item"> The item to check for. </param>
        /// <returns> <see langword="true"/> if the inventory contains an equivalent or greater amount of the item; otherwise, <see langword="false"/>. </returns>
        public static bool Contains( in ItemInstance Item ) => Instance._Inventory.TryGetValue(Item.Item, out uint Amount) && Amount >= Item.Amount;

        /// <summary> Checks if the inventory contains the specified item in the given amount. </summary>
        /// <param name="Item"> The item to check for. </param>
        /// <param name="Amount"> The amount of the item to check for. </param>
        /// <returns> <see langword="true"/> if the inventory contains an equivalent or greater amount of the item; otherwise, <see langword="false"/>. </returns>
        [Button, HideInEditorMode]
        public static bool Contains( Item Item, [MinValue(1)] uint Amount ) {
            if (Amount == 0u) {
                Debug.LogWarning("Should not check for zero items.");
                return true;
            }
            return Instance._Inventory.TryGetValue(Item, out uint CurrentAmount) && CurrentAmount >= Amount;
        }

        /// <summary> Checks if the inventory contains the specified items in the given amounts. </summary>
        /// <param name="Recipe"> The recipe to check for. </param>
        /// <returns> <see langword="true"/> if the inventory contains an equivalent or greater amount of the items; otherwise, <see langword="false"/>. </returns>
        public static bool Contains( in Recipe Recipe ) {
            foreach (ItemInstance Ingredient in Recipe.Ingredients) {
                if (!Contains(Ingredient)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary> Gets the amount of the specified item in the inventory. </summary>
        /// <param name="Item"> The item to get the amount of. </param>
        /// <returns> The amount of the item in the inventory, or <c>0</c> if the item is not in the inventory. </returns>
        [Button, HideInEditorMode]
        public static uint Count( Item Item ) => Instance._Inventory.TryGetValue(Item, out uint Amount) ? Amount : 0u;

        /// <summary> Gets the amount of unique items in the inventory. </summary>
        /// <returns> The amount of unique items in the inventory. </returns>
        public static int UniqueItemCount => Instance._Inventory.Count;

        /// <summary> Clears the inventory. </summary>
        [Button, HideInEditorMode]
        public static void Clear() => Instance.ClearInternal();

        void ClearInternal() {
            if (_Inventory.Count == 0) {
                return;
            }
            _Inventory.Clear();
            _OnInventoryCleared.Invoke();
        }

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<ItemInstance> GetEnumerator() {
            foreach (KeyValuePair<Item, uint> Pair in _Inventory) {
                yield return Pair;
            }
        }

        /// <inheritdoc cref="GetEnumerator"/>
        public static IEnumerable<ItemInstance> All => Instance;

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Implementation of IReadOnlyCollection<out ItemInstance>

        /// <inheritdoc />
        int IReadOnlyCollection<ItemInstance>.Count => _Inventory.Count;

        #endregion

        /// <summary> Waits for an item amount to be achieved. </summary>
        /// <remarks> If more than one item is requested, the task will complete when the amount of the item in the inventory is greater than or equal to the requested amount. </remarks>
        /// <param name="Item"> The item to wait for. </param>
        /// <param name="Comparison"> The comparison to use when waiting for the item. </param>
        /// <param name="Token"> The token to cancel the wait. </param>
        /// <returns> A task that completes when the item amount is achieved. </returns>
        public static async UniTask WaitForAmount( ItemInstance Item, NumericComparison Comparison = NumericComparison.GreaterThanOrEqual, CancellationToken Token = default ) {
            if (Contains(Item)) {
                return;
            }

            UniTaskCompletionSource Source = new();

            await using CancellationTokenRegistration Registration = Token.Register(() => Source.TrySetCanceled());
            Instance._OnItemAdded.AddListener(OnItemChanged);
            Instance._OnItemRemoved.AddListener(OnItemChanged);
            await Source.Task;
            Instance._OnItemAdded.RemoveListener(OnItemChanged);
            Instance._OnItemRemoved.RemoveListener(OnItemChanged);

            void OnItemChanged( ItemInstance Changed ) {
                if (Changed.Item == Item && Count(Item).Compare(Item.Amount, Comparison)) {
                    Source.TrySetResult();
                }
            }
        }

        /// <inheritdoc cref="WaitForAmount(ItemInstance,NumericComparison,CancellationToken)"/>
        /// <param name="Item"> The item to wait for. </param>
        /// <param name="Amount"> The amount the inventory must contain before the task completes. </param>
        /// <param name="Comparison"> The comparison to use when waiting for the item. </param>
        /// <param name="Token"> The token to cancel the wait. </param>
        public static UniTask WaitForAmount( Item Item, uint Amount = 1u, NumericComparison Comparison = NumericComparison.GreaterThanOrEqual, CancellationToken Token = default ) => WaitForAmount(new (Item, Amount), Comparison, Token);

        /// <summary> Waits for a recipe to be craftable. </summary>
        /// <param name="Recipe"> The recipe to wait for. </param>
        /// <param name="Token"> The token to cancel the wait. </param>
        /// <returns> A task that completes when the recipe is craftable. </returns>
        public static async UniTask WaitForRecipe( Recipe Recipe, CancellationToken Token = default ) {
            if (Contains(Recipe)) {
                return;
            }

            UniTaskCompletionSource Source = new();

            await using CancellationTokenRegistration Registration = Token.Register(() => Source.TrySetCanceled());
            Instance._OnItemAdded.AddListener(OnItemChanged);
            Instance._OnItemRemoved.AddListener(OnItemChanged);
            await Source.Task;
            Instance._OnItemAdded.RemoveListener(OnItemChanged);
            Instance._OnItemRemoved.RemoveListener(OnItemChanged);

            void OnItemChanged( ItemInstance Changed ) {
                if (Contains(Recipe)) {
                    Source.TrySetResult();
                }
            }
        }

        /// <summary> Gets all items of the given type. </summary>
        /// <param name="Type"> The type of items to get. </param>
        /// <returns> All items of the given type. </returns>
        public static IEnumerable<ItemInstance> OfType( ItemType Type ) {
            foreach (KeyValuePair<Item, uint> Pair in Instance._Inventory) {
                if (Pair.Key.Type == Type) {
                    yield return Pair;
                }
            }
        }

        /// <summary> Gets all items of the given group. </summary>
        /// <param name="Group"> The group of items to get. </param>
        /// <returns> All items of the given group. </returns>
        public static IEnumerable<ItemInstance> OfGroup( ItemGroup Group ) {
            foreach (KeyValuePair<Item, uint> Pair in Instance._Inventory) {
                if (Pair.Key.Group == Group) {
                    yield return Pair;
                }
            }
        }

    }

    public abstract class InventoryException : Exception {
        protected InventoryException( string Message ) : base(Message) { }
    }

    public sealed class NotEnoughItemsException : InventoryException {
        public NotEnoughItemsException( Item Item, uint Requested, uint Available ) : base($"Not enough items. Requested {Requested:N0} {Item}, but only {Available:N0} {Item} are available.") { }
    }
}
