using System.Diagnostics;
using LYGJ.InventoryManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ {
    public sealed class Inventory_Group : MonoBehaviour {

        [SerializeField, Tooltip("The button."), Required, ChildGameObjectsOnly]
        Button _Button = null!;
        [SerializeField, Tooltip("The icon."), Required, ChildGameObjectsOnly]
        Image _Icon = null!;

        #if UNITY_EDITOR
        void Reset() {
            _Button = GetComponentInChildren<Button>();
            _Icon   = GetComponentInChildren<Image>();
        }
        #endif

        /// <summary> The group. </summary>
        public ItemGroupOrNone Group { get; private set; }

        /// <summary> Whether or not this group is selected. </summary>
        public bool Selected { get; private set; }

        /// <summary> Sets the group to be all items. </summary>
        /// <param name="Inventory_UI"> The inventory UI that owns this group button. </param>
        public void SetAll( Inventory_UI Inventory_UI ) => Setup(Inventory_UI, ItemGroupOrNone.None);

        /// <summary> Sets the group to be the given group. </summary>
        /// <param name="Inventory_UI"> The inventory UI that owns this group button. </param>
        /// <param name="Group"> The group to set. </param>
        public void SetGroup( Inventory_UI Inventory_UI, ItemGroup Group ) => Setup(Inventory_UI, Group);

        void Setup( Inventory_UI Inventory_UI, ItemGroupOrNone Group ) {
            this.Group   = Group;
            _Icon.sprite = Inventory_UI.GetGroupIcon(Group);
            _Button.onClick.RemoveAllListeners();
            void Call() => Inventory_UI.OnGroup(Group);
            _Button.onClick.AddListener(Call);
            SetSelected(false);
        }

        /// <summary> Sets whether or not this group is selected. </summary>
        /// <param name="Selected"> Whether or not this group is selected. </param>
        public void SetSelected( bool Selected ) {
            this.Selected        = Selected;
            _Button.interactable = !Selected;
        }
    }
}
