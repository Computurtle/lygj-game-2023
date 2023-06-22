using System;
using LYGJ.AudioManagement;
using LYGJ.EntitySystem.PlayerManagement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.EnemyManagement {
    [RequireComponent(typeof(EnemyHealth))]
    public abstract class EnemyBase : MonoBehaviour {
        [SerializeField, Tooltip("The health."), Required, ChildGameObjectsOnly]
        EnemyHealth _Health = null!;

        IDamageTaker Health => _Health;

        /// <summary> The enemy type. </summary>
        [ShowInInspector, ReadOnly, Tooltip("The enemy type.")]
        public abstract EnemyType Type { get; }

        protected virtual void Awake() {
            Enemies.Add(this);
            _Health.Died += OnDied;
        }

        protected virtual void OnDestroy() {
            try {
                if (!Health.IsDead) {
                    Enemies.Remove(this);
                }
            }
            catch (MissingReferenceException) { }
            catch (NullReferenceException) { }
        }

        [SerializeField, Tooltip("The sound to play when the enemy dies.")]
        SFX? _DeathSound = null;

        void OnDied() {
            _DeathSound.Play(transform.position);
            Enemies.Remove(this);
            Destroy(gameObject);
        }

    }

}
