using System.Collections;
using System.Collections.Generic;
using LYGJ.Common;
using LYGJ.EntitySystem.PlayerManagement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.InventoryManagement {
    public sealed class Inventory_UI : Container_UI<Inventory_UI> {

        [SerializeField, Tooltip("The prefab for inventory slots."), Required, AssetsOnly, PropertyOrder(-1)]
        Inventory_Slot _SlotPrefab = null!;

        /// <inheritdoc />
        protected override IReadOnlyCollection<ItemInstance> GetItemsOfGroup( ItemGroup Group ) => Inventory.OfGroup(Group).Iterate();

        /// <inheritdoc />
        protected override IReadOnlyCollection<ItemInstance> GetAllItems() => Inventory.All.ToArray(Inventory.UniqueItemCount);

        /// <inheritdoc />
        protected override uint GetItemCount( Item Item ) => Inventory.Count(Item);

        /// <inheritdoc />
        protected override void Awake() {
            base.Awake();

            _ItemPreviewCanvasGroup.alpha          = 0f;
            _ItemPreviewCanvasGroup.interactable   = false;
            _ItemPreviewCanvasGroup.blocksRaycasts = false;
        }

        /// <inheritdoc />
        protected override void ReturnPooledSlots() => Pool<Inventory_Slot>.ReturnAll(_SlotParent);

        /// <inheritdoc />
        protected override void InitPooledSlot( RectTransform Parent, ItemInstance Item ) {
            Inventory_Slot Slot = Pool<Inventory_Slot>.Get(Parent, _SlotPrefab);
            Slot.SetItem(this, Item);
        }

        /// <inheritdoc />
        protected override void OnMakeVisible() {
            Pointer.SetVisible(PointerPriority.Inventory);
            PlayerMotor.SetCanMove(MotorPriority.Inventory, false);
            PlayerInteractor.SetCanInteract(InteractionPriority.Inventory, false);
        }

        /// <inheritdoc />
        protected override void OnMakeInvisible() {
            Pointer.ClearVisible(PointerPriority.Inventory);
            PlayerMotor.ClearCanMove(MotorPriority.Inventory);
            PlayerInteractor.ClearCanInteract(InteractionPriority.Inventory);
        }

        /// <inheritdoc />
        protected override void RepaintNow() {
            base.RepaintNow();
            Repaint_Preview();
        }

        void Start() {
            Inventory.Changed += UpdateUI;
            void UpdateUI( Inventory.ChangeType Type, ItemInstance Item ) => Repaint();

            PlayerInput.Inventory.Pressed += Toggle;
        }

        #region Item Preview

        [Title("Item Preview")]
        [SerializeField, Tooltip("The canvas group for the item preview UI."), LabelText("Canvas Group"), Required, ChildGameObjectsOnly]
        CanvasGroup _ItemPreviewCanvasGroup = null!;
        [SerializeField, Tooltip("The time, in seconds, to fade in the item preview UI."), LabelText("Fade In Time"), Min(0), SuffixLabel("s")]
        float _ItemPreviewFadeInTime = 0.25f;
        [SerializeField, Tooltip("The time, in seconds, to fade out the item preview UI."), LabelText("Fade Out Time"), Min(0), SuffixLabel("s")]
        float _ItemPreviewFadeOutTime = 0.25f;
        [SerializeField, Tooltip("The item preview UI."), Required, ChildGameObjectsOnly]
        Inventory_ItemPreview _ItemPreview = null!;

        IEnumerator? _ItemPreviewFade;

        void FadeInItemPreview()  => FadeTo(this, ref _ItemPreviewFade, _ItemPreviewCanvasGroup, 1, _ItemPreviewFadeInTime, true);
        void FadeOutItemPreview() => FadeTo(this, ref _ItemPreviewFade, _ItemPreviewCanvasGroup, 0, _ItemPreviewFadeOutTime, false);

        /// <summary> Shows the given item in the item preview UI. </summary>
        /// <param name="Item"> The item to show. </param>
        public void ShowPreview( ItemInstance Item ) {
            _ItemPreview.SetItem(Item);
            FadeInItemPreview();
        }

        /// <summary> Hides the item preview UI. </summary>
        public void HidePreview() {
            FadeOutItemPreview();
        }

        void Repaint_Preview() {
            // If the preview is still open:
            // - Check if the item still exists, if it does, update the preview with the new amount.
            // - If the item doesn't exist anymore, close the preview.
            if (_ItemPreviewCanvasGroup.alpha > 0) {
                ItemInstance Item  = _ItemPreview.Item;
                uint         Count = GetItemCount(Item);
                if (Count > 0u) {
                    _ItemPreview.SetItem(new(Item,Count));
                } else {
                    HidePreview();
                }
            }
        }

        #endregion

        #if UNITY_EDITOR
        [Button("Show"), ButtonGroup("ShowHide"), DisableIf(nameof(Visible)), HideInEditorMode]
        void Editor_Show() => Visible = true;
        [Button("Hide"), ButtonGroup("ShowHide"), EnableIf(nameof(Visible)), HideInEditorMode]
        void Editor_Hide() => Visible = false;
        #endif

        /// <summary> Toggles the visibility of the inventory UI. </summary>
        public void Toggle() => Visible = !Visible;

    }
}
