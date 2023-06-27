using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem.PlayerManagement {
    public abstract class PlayerEquipment : ScriptableObject {
        [SerializeField, Tooltip("The world-space prefab."), Required, AssetsOnly] RuntimePlayerEquipment _Prefab = null!;

        /// <summary> The world-space prefab. </summary>
        public RuntimePlayerEquipment Prefab => _Prefab;

        [Space]
        [SerializeField, Tooltip("The base damage."), Min(0)] float _BaseDamage = 1f;
        [SerializeField, Tooltip("The attack range, in metres."), Min(0), SuffixLabel("m")] float _AttackRange = 1f;

        /// <summary> The base damage. </summary>
        public float BaseDamage => _BaseDamage;

        /// <summary> The attack range, in metres. </summary>
        public float AttackRange => _AttackRange;

        /// <summary> Equips the equipment. </summary>
        /// <param name="Parent"> The parent transform. </param>
        /// <returns> The runtime equipment. </returns>
        public virtual RuntimePlayerEquipment Equip( Transform Parent ) {
            RuntimePlayerEquipment Runtime = Instantiate(_Prefab, Parent);
            Runtime.Init(this);
            return Runtime;
        }

        /// <summary> Unequips the equipment. </summary>
        /// <param name="Runtime"> The runtime equipment. </param>
        public virtual void Unequip( RuntimePlayerEquipment Runtime ) {
            Debug.Assert(Runtime.Equipment == this);
            Runtime.Cleanup();
        }

        #if UNITY_EDITOR
        [Button("Equip"), ButtonGroup("EquipUnequip"), HideInEditorMode, EnableIf(nameof(Editor_CanEquipNow))]
        void Editor_EquipNow() => PlayerEquipper.Equip(this);
        bool Editor_CanEquipNow() => PlayerEquipper.CanEquip(this);
        [Button("Unequip"), ButtonGroup("EquipUnequip"), HideInEditorMode, EnableIf(nameof(Editor_CanUnequipNow))]
        void Editor_UnequipNow() => PlayerEquipper.Unequip(this);
        bool Editor_CanUnequipNow() => PlayerEquipper.CanUnequip(this);
        #endif
    }
}
