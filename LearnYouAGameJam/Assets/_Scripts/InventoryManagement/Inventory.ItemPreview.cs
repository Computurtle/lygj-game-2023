using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ.InventoryManagement {
    public class Inventory_ItemPreview : MonoBehaviour {
        /// <summary> The item being displayed. </summary>
        public ItemInstance Item { get; private set; }

        [SerializeField, Tooltip("The icon."), ChildGameObjectsOnly]
        Image? _Icon = null;

        [Space]
        [SerializeField, Tooltip("The TMP_Text component for the name."), ChildGameObjectsOnly]
        TMP_Text? _Name = null;
        [SerializeField, Tooltip("The format string for the name.\n\n{0} = Item Name\n{1} = Quantity"), LabelText("Format"), HideIf("@" + nameof(_Name) + " == null")]
        string _NameFormat = "{0}";

        [Space]
        [SerializeField, Tooltip("The TMP_Text component for the quantity."), ChildGameObjectsOnly]
        TMP_Text? _Quantity = null;
        [SerializeField, Tooltip("The format string for the quantity.\n\n{0} = Quantity"), LabelText("Format"), HideIf("@" + nameof(_Quantity) + " == null")]
        string _QuantityFormat = "x{0:N0}";
        [SerializeField, Tooltip("Whether to hide the quantity if it is 1."), HideIf("@" + nameof(_Quantity) + " == null")]
        bool _HideQuantityIfOne = true;

        [Space]
        [SerializeField, Tooltip("The TMP_Text component for the description."), ChildGameObjectsOnly]
        TMP_Text? _Description = null;
        [SerializeField, Tooltip("The format string for the description.\n\n{0} = Item Description"), LabelText("Format"), HideIf("@" + nameof(_Description) + " == null")]
        string _DescriptionFormat = "{0}";

        [Space]
        [SerializeField, Tooltip("The TMP_Text component for the item type."), ChildGameObjectsOnly]
        TMP_Text? _ItemType = null;
        [SerializeField, Tooltip("The format string for the item type.\n\n{0} = Item Type\n{1} = Item Group"), LabelText("Format"), HideIf("@" + nameof(_ItemType) + " == null")]
        string _ItemTypeFormat = "{0}";

        [Space]
        [SerializeField, Tooltip("The TMP_Text component for the item group."), ChildGameObjectsOnly]
        TMP_Text? _ItemGroup = null;
        [SerializeField, Tooltip("The format string for the item group.\n\n{0} = Item Group"), LabelText("Format"), HideIf("@" + nameof(_ItemGroup) + " == null")]
        string _ItemGroupFormat = "{0}";

        /// <summary> Sets the item to be displayed. </summary>
        /// <param name="Item"> The item to set. </param>
        public void SetItem( ItemInstance Item ) {
            if (Item.IsNone) {
                DisplayNoneItemInternal(Item);
            }else {
                DisplayItemInternal(Item);
            }
        }

        protected void DisplayNoneItemInternal( ItemInstance Item ) {
            this.Item = Item;
            if (_Icon != null) {
                _Icon.enabled = false;
            }

            if (_Name != null) {
                _Name.text = string.Empty;
            }

            if (_Quantity != null) {
                _Quantity.text = string.Empty;
            }

            if (_Description != null) {
                _Description.text = string.Empty;
            }

            if (_ItemType != null) {
                _ItemType.text = string.Empty;
            }

            if (_ItemGroup != null) {
                _ItemGroup.text = string.Empty;
            }
        }

        protected void DisplayItemInternal( ItemInstance Item ) {
            this.Item = Item;
            if (_Icon != null) {
                _Icon.enabled        = true;
                _Icon.overrideSprite = Item.Item.Icon!;
            }

            if (_Name != null) {
                _Name.text = string.Format(_NameFormat, Item.Item.Name, Item.Amount);
            }

            if (_Quantity != null) {
                _Quantity.text    = string.Format(_QuantityFormat, Item.Amount);
                _Quantity.enabled = !_HideQuantityIfOne || Item.Amount != 1;
            }

            if (_Description != null) {
                _Description.text = string.Format(_DescriptionFormat, Item.Item.Description);
            }

            if (_ItemType != null) {
                _ItemType.text = string.Format(_ItemTypeFormat, Item.Item.Type, Item.Item.Group);
            }

            if (_ItemGroup != null) {
                _ItemGroup.text = string.Format(_ItemGroupFormat, Item.Item.Group);
            }
        }

        #if UNITY_EDITOR
        protected virtual void Reset() {
            _Icon = GetComponentInChildren<Image>();
            _Name = GetComponentInChildren<TMP_Text>();
        }
        #endif
    }
}
