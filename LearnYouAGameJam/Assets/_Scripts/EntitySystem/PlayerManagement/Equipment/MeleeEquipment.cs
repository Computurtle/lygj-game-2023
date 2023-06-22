using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.PlayerManagement {
    [CreateAssetMenu(menuName = "LYGJ/Inventory/Equipment/Melee Equipment", fileName = "Melee Equipment", order = 100)]
    public class MeleeEquipment : PlayerEquipment {
        [SerializeField, Tooltip("The swing speed, in seconds."), Min(0), SuffixLabel("s")] float _SwingSpeed = 1f;

        /// <summary> The swing speed, in seconds. </summary>
        public float SwingSpeed => _SwingSpeed;

    }
}
