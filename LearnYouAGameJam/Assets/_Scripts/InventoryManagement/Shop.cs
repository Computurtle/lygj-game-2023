using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.InventoryManagement {
    [CreateAssetMenu(fileName = "New Shop", menuName = "LYGJ/Inventory/Shop")]
    public sealed class Shop : ScriptableObject {
        [SerializeField, Tooltip("The items available for purchase."), LabelText("Purchasables"), ListDrawerSettings(ShowFoldout = false)]
        Purchasable[] _Items = Array.Empty<Purchasable>();

        /// <summary> The items available for purchase. </summary>
        public IReadOnlyCollection<Purchasable> Items => _Items;

        /// <summary> The number of items available for purchase. </summary>
        public int UniquePurchasableCount => _Items.Length;

        /// <summary> Gets all items that can be purchased. </summary>
        /// <returns> All items that can be purchased. </returns>
        public IEnumerable<ItemInstance> GetAllItems() {
            foreach (Purchasable Purchasable in _Items) {
                yield return Purchasable.Item;
            }
        }

        /// <summary> Gets the count of the given item that can be purchased at a time. </summary>
        /// <param name="Item"> The item to check. </param>
        /// <returns> The number of items that are purchased at a time. </returns>
        public uint GetPurchaseAmount( Item Item ) {
            foreach (Purchasable Purchasable in _Items) {
                if (Purchasable.Item == Item) {
                    return Purchasable.Item.Amount;
                }
            }

            return 0u;
        }

        /// <summary> Gets the purchasable items of the given group. </summary>
        /// <param name="Group"> The group to check. </param>
        /// <returns> The purchasable items of the given group. </returns>
        public IEnumerable<ItemInstance> GetItemsOfGroup( ItemGroup Group ) {
            foreach (Purchasable Purchasable in _Items) {
                if (Purchasable.Item.Item.Group == Group) {
                    yield return Purchasable.Item;
                }
            }
        }

        /// <summary> Attempts to get the purchasable item from the given item. </summary>
        /// <param name="Item"> The item to check. </param>
        /// <param name="Purchasable"> [out] The purchasable item, if found. </param>
        /// <returns> <see langword="true"/> if the item was found; otherwise, <see langword="false"/>. </returns>
        public bool TryGetPurchasable( ItemInstance Item, [NotNullWhen(true)] out Purchasable? Purchasable ) {
            foreach (Purchasable PurchasableItem in _Items) {
                if (PurchasableItem.Item == Item) {
                    Purchasable = PurchasableItem;
                    return true;
                }
            }

            Purchasable = default;
            return false;
        }

        /// <inheritdoc cref="Purchasable.CanAfford"/>
        /// <param name="Item"> The item to check. </param>
        /// <exception cref="InvalidOperationException"> Thrown if the purchasable item was not found. </exception>
        public bool CanAfford( ItemInstance Item ) {
            if (TryGetPurchasable(Item, out Purchasable? Purchasable)) {
                return Purchasable.CanAfford;
            }

            throw new InvalidOperationException($"The item {Item} is not purchasable.");
        }

        /// <inheritdoc cref="Purchasable.Purchase"/>
        /// <param name="Item"> The item to purchase. </param>
        /// <exception cref="InvalidOperationException"> Thrown if the purchasable item was not found, or if the item could not be purchased. </exception>
        public void Purchase( ItemInstance Item ) {
            if (TryGetPurchasable(Item, out Purchasable? Purchasable)) {
                Purchasable.Purchase();
                return;
            }

            throw new InvalidOperationException($"The item {Item} is not purchasable.");
        }

        /// <inheritdoc cref="Barter_UI.OpenInterface"/>
        #if UNITY_EDITOR
        [Button, ButtonGroup("OpenClose"), HideInEditorMode, EnableIf("@" + nameof(Barter_UI) + "." + nameof(Barter_UI.Editor_TryGetCanOpen) + "()")]
        #endif
        public void Open() => Barter_UI.Open(this);

        /// <inheritdoc cref="Barter_UI.CloseInterface"/>
        #if UNITY_EDITOR
        [Button, ButtonGroup("OpenClose"), HideInEditorMode, EnableIf("@" + nameof(Barter_UI) + "." + nameof(Barter_UI.Editor_TryGetCanClose) + "()")]
        #endif
        public void Close() => Barter_UI.Close();
    }
}
