using System;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace LYGJ.EntitySystem.PlayerManagement {
    public abstract class RuntimePlayerEquipment : MonoBehaviour {
        /// <summary> The equipment. </summary>
        [HideInEditorMode] public PlayerEquipment Equipment;

        /// <summary> Initialises the equipment. </summary>
        /// <param name="Equipment"> The equipment. </param>
        public virtual void Init( PlayerEquipment Equipment ) {
            this.Equipment = Equipment;
            OnEquipped();
        }

        /// <summary> Cleans up the equipment. </summary>
        public virtual void Cleanup() {
            OnUnequipped();
            Destroy(gameObject);
        }

        /// <summary> Called when the equipment is equipped. </summary>
        protected virtual void OnEquipped() { }

        /// <summary> Called when the equipment is unequipped. </summary>
        protected virtual void OnUnequipped() { }

        /// <summary> Called when the equipment is 'fired' (swung, shot, etc.). </summary>
        /// <param name="Duration"> The duration of the fire button press, in seconds. </param>
        /// <param name="Multiplier"> The damage multiplier. </param>
        public virtual void InterceptFire( float Duration, float Multiplier = 1f ) { }

        #if UNITY_EDITOR
        [Button("Unequip"), ButtonGroup("EquipUnequip"), HideInEditorMode, EnableIf(nameof(Editor_CanUnequipNow))]
        void Editor_UnequipNow() => PlayerEquipper.Unequip(Equipment);
        bool Editor_CanUnequipNow() => PlayerEquipper.CanUnequip(Equipment);
        #endif
    }

    public abstract class RuntimeMeleeEquipmentBase<TEquipment> : RuntimePlayerEquipment where TEquipment : MeleeEquipment {
        /// <inheritdoc cref="RuntimePlayerEquipment.Equipment"/>
        public new TEquipment Equipment => (TEquipment)base.Equipment;

        #region Overrides of RuntimePlayerEquipment

        /// <inheritdoc />
        public override void Init( PlayerEquipment Equipment ) {
            if (Equipment is not TEquipment Melee) {
                throw new ArgumentException($"Cannot initialise {nameof(TEquipment)} with equipment of type {Equipment.GetType().GetNiceName()}.");
            }

            base.Init(Melee);
        }

        #endregion

    }

    public abstract class RuntimeRangedEquipmentBase<TEquipment> : RuntimePlayerEquipment where TEquipment : RangedEquipment {
        /// <inheritdoc cref="RuntimePlayerEquipment.Equipment"/>
        public new TEquipment Equipment => (TEquipment)base.Equipment;

        #region Overrides of RuntimePlayerEquipment

        /// <inheritdoc />
        public override void Init( PlayerEquipment Equipment ) {
            if (Equipment is not TEquipment Ranged) {
                throw new ArgumentException($"Cannot initialise {nameof(TEquipment)} with equipment of type {Equipment.GetType().GetNiceName()}.");
            }

            base.Init(Ranged);
        }

        #endregion
    }

}
