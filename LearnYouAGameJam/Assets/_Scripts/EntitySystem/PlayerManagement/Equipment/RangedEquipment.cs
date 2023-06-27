using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.PlayerManagement {
    [CreateAssetMenu(menuName = "LYGJ/Inventory/Equipment/Ranged Equipment", fileName = "Ranged Equipment", order = 200)]
    public class RangedEquipment : PlayerEquipment {
        [SerializeField, Tooltip("The projectile speed, in metres per second."), Min(0), SuffixLabel("m/s")] float      _ProjectileSpeed  = 1f;
        [SerializeField, Tooltip("The projectile prefab."), Required, AssetsOnly]                            GameObject _ProjectilePrefab = null!;

        /// <summary> The projectile speed, in metres per second. </summary>
        public float ProjectileSpeed => _ProjectileSpeed;

        /// <summary> The projectile prefab. </summary>
        public GameObject ProjectilePrefab => _ProjectilePrefab;

        [Space]
        [SerializeField, Tooltip("The maximum ammo capacity."), Min(0)] int _MaxAmmo = 1;
        [SerializeField, Tooltip("The reload time, in seconds."), Min(0), SuffixLabel("s")] float _ReloadTime = 1f;

        /// <summary> The maximum ammo capacity. </summary>
        public int MaxAmmo => _MaxAmmo;

        /// <summary> The reload time, in seconds. </summary>
        public float ReloadTime => _ReloadTime;

    }
}
