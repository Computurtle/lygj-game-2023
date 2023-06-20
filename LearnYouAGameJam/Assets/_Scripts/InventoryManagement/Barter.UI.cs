using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.InventoryManagement {
    public sealed class Barter_UI : Container_UI<Barter_UI> {

        [SerializeField, Tooltip("The prefab for offers."), Required, AssetsOnly, PropertyOrder(-1)]
        Barter_Slot _SlotPrefab = null!;

        /// <inheritdoc />
        protected override bool CanPrefillEmptySlots => false;

        /// <inheritdoc />
        protected override void ReturnPooledSlots() => Pool<Barter_Slot>.ReturnAll(_SlotParent);

        /// <inheritdoc />
        protected override void InitPooledSlot( RectTransform Parent, ItemInstance Item ) {
            if (!IsShopOpen) {
                Debug.LogError("Cannot initialise a purchasable slot because no shop instance is currently set.", this);
                return;
            }
            if (Item.IsNone) { return; }
            if (!_CurrentShop.TryGetPurchasable(Item, out Purchasable? Purchasable)) {
                Debug.LogError($"Failed to retrieve the purchasable for the given item {Item}", this);
                return;
            }

            Barter_Slot Slot = Pool<Barter_Slot>.Get(Parent, _SlotPrefab);
            Slot.SetItem(this, Purchasable);
        }

        /// <summary> Attempts to purchase the given item. </summary>
        /// <param name="Item"> The item to purchase.</param>
        public void AttemptPurchase( ItemInstance Item ) {
            if (!IsShopOpen) {
                Debug.LogError($"Cannot purchase {Item} because no shop instance is currently set.", this);
                return;
            }

            if (_CurrentShop.TryGetPurchasable(Item, out Purchasable? Purchasable)) {
                if (Purchasable.CanAfford) {
                    Purchasable.Purchase();
                } else {
                    Debug.Log($"Cannot purchase {Item} because the player cannot afford it.", this);
                }
            } else {
                Debug.LogError($"Cannot purchase {Item} because it could not be found in the shop.", this);
            }
        }

        [NonSerialized] Shop? _CurrentShop = null;

        /// <summary> Gets whether a shop is currently open. </summary>
        [MemberNotNullWhen(true, nameof(_CurrentShop))]
        public bool IsShopOpen => _CurrentShop != null;

        /// <inheritdoc />
        protected override IReadOnlyCollection<ItemInstance> GetAllItems() => IsShopOpen ? _CurrentShop.GetAllItems().ToArray(_CurrentShop.UniquePurchasableCount) : Array.Empty<ItemInstance>();

        /// <inheritdoc />
        protected override IReadOnlyCollection<ItemInstance> GetItemsOfGroup( ItemGroup Group ) => IsShopOpen ? _CurrentShop.GetItemsOfGroup(Group).Iterate() : Array.Empty<ItemInstance>();

        /// <inheritdoc />
        protected override uint GetItemCount( Item Item ) => IsShopOpen ? _CurrentShop.GetPurchaseAmount(Item) : 0u;

        /// <inheritdoc cref="OpenInterface"/>
        public static void Open( Shop Shop ) => Instance.OpenInterface(Shop);

        /// <summary> Opens the barter interface with the given shop. </summary>
        /// <param name="Shop"> The shop to open. </param>
        [Button("Open"), DisableIf(nameof(Visible)), HideInEditorMode]
        public void OpenInterface( Shop Shop ) {
            if (_CurrentShop != null) {
                Debug.LogError("A shop is already being displayed.", this);
                return;
            }

            if (!Inventory_UI.Instance.Visible) {
                Inventory_UI.Instance.Visible = true;
            }
            _CurrentShop  = Shop;
            RepaintQueued = true;
            Visible       = true;
        }

        /// <inheritdoc cref="CloseInterface"/>
        [Button("Close"), EnableIf(nameof(Visible)), HideInEditorMode]
        public static void Close() => Instance.CloseInterface();

        /// <summary> Closes the barter interface. </summary>
        public void CloseInterface() {
            if (_CurrentShop == null) {
                Debug.LogWarning("No shop is being displayed at this moment. Interface should already be hidden.", this);
            }
            if (Inventory_UI.Instance.Visible) {
                Inventory_UI.Instance.Visible = false;
            }
            Visible      = false;
            _CurrentShop = null;
        }

        #if UNITY_EDITOR
        public static bool Editor_TryGetCanOpen() => Application.isPlaying && _Instance != null && !_Instance.IsShopOpen;
        public static bool Editor_TryGetCanClose() => Application.isPlaying && _Instance != null && _Instance.IsShopOpen;
        #endif

    }
}
