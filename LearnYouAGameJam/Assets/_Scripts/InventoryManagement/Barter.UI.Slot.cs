using System.Diagnostics;
using LYGJ.Common;
using LYGJ.InventoryManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ {
    public class Barter_Slot : Inventory_ItemPreview {
        [Title("Interaction")]
        [SerializeField, Tooltip("The button component."), Required, ChildGameObjectsOnly]
        Button _Button = null!;

        [Title("Requirements")]
        [SerializeField, Tooltip("The parent for requirements."), Required, ChildGameObjectsOnly]
        RectTransform _RequirementParent = null!;
        [SerializeField, Tooltip("The prefab for requirements."), Required, AssetsOnly]
        Inventory_ItemPreview _RequirementPrefab = null!;

        #if UNITY_EDITOR
        protected override void Reset() {
            base.Reset();
            _Button = GetComponentInChildren<Button>();
        }
        #endif

        /// <inheritdoc cref="Inventory_ItemPreview.SetItem"/>
        public void SetItem( Barter_UI Barter_UI, Purchasable Purchasable ) {
            ItemInstance Item = Purchasable.Item;
            base.SetItem(Item);
            _Button.onClick.RemoveAllListeners();
            if (!Item.IsNone) {
                void Call() => Barter_UI.AttemptPurchase(Item);
                _Button.onClick.AddListener(Call);
            }

            Repaint_Requirements(Purchasable);
        }

        void Repaint_Requirements( Purchasable Purchasable ) {
            Pool<Inventory_ItemPreview>.ReturnAll(_RequirementParent);
            foreach (ItemInstance Cost in Purchasable.Cost) {
                Inventory_ItemPreview Requirement = Pool<Inventory_ItemPreview>.Get(_RequirementParent, _RequirementPrefab);
                Requirement.SetItem(Cost);
            }
        }

    }
}
