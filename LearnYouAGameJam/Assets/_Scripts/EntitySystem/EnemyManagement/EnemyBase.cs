using System;
using LYGJ.AudioManagement;
using LYGJ.EntitySystem.PlayerManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LYGJ.EntitySystem.EnemyManagement {
    [RequireComponent(typeof(EnemyHealth))]
    public abstract class EnemyBase : MonoBehaviour {
        [SerializeField, Tooltip("The health."), Required, ChildGameObjectsOnly]
        // ReSharper disable once InconsistentNaming
        protected EnemyHealth _Health = null!;

        /// <summary> The health. </summary>
        protected IDamageTaker Health => _Health;

        #if UNITY_EDITOR
        protected virtual void Reset() {
            _Health = GetComponent<EnemyHealth>();
        }
        #endif

        /// <summary> The enemy type. </summary>
        [ShowInInspector, ReadOnly, Tooltip("The enemy type.")]
        public abstract EnemyType Type { get; }

        protected virtual void Awake() {
            Enemies.Add(this);
            _Health.Died        += OnDied;
            _Health.DamageTaken += OnDamageTaken;
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

        [SerializeField, Tooltip("The sound(s) to play when the enemy takes damage.")]
        SFX[] _DamageSounds = Array.Empty<SFX>();
        [SerializeField, Tooltip("The sound to play when the enemy dies.")]
        SFX? _DeathSound = null;
        [SerializeField, Tooltip("The wilhelm scream sound."), HorizontalGroup("Wilhelm")]
        SFX? _WilhelmScream = null;
        [SerializeField, Tooltip("The chance for a wilhelm scream to play when the enemy dies."), Range(0, 1), HorizontalGroup("Wilhelm", 0.3f), LabelText("Chance"), LabelWidth(70f)]
        float _WilhelmScreamChance = 0.01f;

        void OnDamageTaken( float Damage ) {
            Vector3 Pos = transform.position;
            foreach (SFX Sound in _DamageSounds) {
                Sound.Play(Pos);
            }
        }

        void OnDied() {
            if (Random.value < _WilhelmScreamChance) {
                _WilhelmScream.Play(transform.position);
            }else {
                _DeathSound.Play(transform.position);
            }
            Enemies.Remove(this);
            Destroy(gameObject);
        }

    }

}
