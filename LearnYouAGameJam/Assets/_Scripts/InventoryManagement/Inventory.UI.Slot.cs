using System.Diagnostics;
using LYGJ.InventoryManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ {
    public sealed class Inventory_Slot : Inventory_ItemPreview {
        [Title("Interaction")]
        [SerializeField, Tooltip("The button component."), Required, ChildGameObjectsOnly]
        Button _Button = null!;

        #if UNITY_EDITOR
        protected override void Reset() {
            base.Reset();
            _Button = GetComponentInChildren<Button>();
        }
        #endif

        #region Overrides of Inventory_ItemPreview

        /// <inheritdoc />
        public override void SetItem( Inventory_UI Inventory_UI, ItemInstance Item ) {
            base.SetItem(Inventory_UI, Item);
            _Button.onClick.RemoveAllListeners();
            void Call() => Inventory_UI.ShowPreview(Item);
            _Button.onClick.AddListener(Call);
        }

        #endregion

    }
}
