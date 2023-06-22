using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LYGJ.AudioManagement;
using LYGJ.Common.Attributes;
using LYGJ.Common.Physics;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem.PlayerManagement {
    public class RuntimeMeleeEquipment : RuntimeMeleeEquipmentBase<MeleeEquipment> {
        [Title("Animations")]
        [SerializeField, Tooltip("The animator."), Required, ChildGameObjectsOnly]
        Animator _Anim = null!;
        [SerializeField, Tooltip("The swing trigger."), AnimParam]
        string _SwingTrigger = "Swing";
        [SerializeField, Tooltip("The duration of the swing animation, in seconds."), Min(0), SuffixLabel("s")]
        float _SwingDuration = 1f;

        int _SwingTriggerHash;

        [Title("Audio")]
        [SerializeField, Tooltip("The equip sound.")]
        SFX? _EquipSound;
        [SerializeField, Tooltip("The unequip sound.")]
        SFX? _UnequipSound;
        [SerializeField, Tooltip("The swing sound.")]
        SFX? _SwingSound;
        [SerializeField, Tooltip("The impact sound.")]
        SFX? _ImpactSound;

        [Title("Collision Detection")]
        [SerializeField, Tooltip("The trigger monitor for the weapon's collision detection."), Required, ChildGameObjectsOnly]
        TriggerMonitor _Trigger = null!;

        [Title("Visuals")]
        [SerializeField, Tooltip("The slash effect prefab."), AssetsOnly]
        GameObject? _SlashEffect = null;
        [SerializeField, Tooltip("Where to spawn the slash effect."), Required, ChildGameObjectsOnly, HideIf("@" + nameof(_SlashEffect) + " == null"), LabelText("Spawn Point")]
        Transform _SlashEffectSpawnPoint = null!;

        #if UNITY_EDITOR
        void Reset() {
            _Anim    = GetComponentInChildren<Animator>();
            _Trigger = GetComponentInChildren<TriggerMonitor>();
        }
        #endif

        bool  _Attacking        = false;
        float _DamageMultiplier = 1f;
        bool  _HitAnything      = false;

        readonly HashSet<Collider> _Colliders = new();

        void Awake() {
            _SwingTriggerHash = Animator.StringToHash(_SwingTrigger);

            _Trigger.TriggerEntered += OnTriggerEntered;
        }

        float Damage => Equipment.BaseDamage * _DamageMultiplier;

        void OnTriggerEntered( Collider Other ) {
            if (!_Attacking) { return; }
            if (!_Colliders.Add(Other)) { return; }

            if (Other.TryGetComponent(out IDamageTaker DamageTaker)) {
                float Dmg = Damage;
                Debug.Log($"Dealt {Dmg} damage to {DamageTaker}.", DamageTaker as Object);
                DamageTaker.TakeDamage(Dmg);

                if (!_HitAnything) {
                    _HitAnything = true;
                    _ImpactSound.Play(transform.position);
                }
            }
        }

        /// <summary> Swings the weapon. </summary>
        [Button, HideInEditorMode]
        public void Swing() => StartCoroutine(SwingRoutine());

        IEnumerator SwingRoutine() {
            _Attacking   = true;
            _HitAnything = false;
            _Anim.SetTrigger(_SwingTriggerHash);
            _SwingSound.Play(transform.position);
            if (_SlashEffect) {
                Instantiate(_SlashEffect, _SlashEffectSpawnPoint.position, _SlashEffectSpawnPoint.rotation);
            }
            yield return new WaitForSeconds(_SwingDuration);
            _Colliders.Clear();
            _Attacking = false;
        }

        #region Overrides of RuntimePlayerEquipment

        /// <inheritdoc />
        public override void InterceptFire( float Duration, float Multiplier = 1 ) {
            if (_Attacking) { return; }
            _DamageMultiplier = Multiplier;
            Swing();
        }

        /// <inheritdoc />
        protected override void OnEquipped() {
            _EquipSound.Play(transform.position);
        }

        /// <inheritdoc />
        protected override void OnUnequipped() {
            _UnequipSound.Play(transform.position);
        }

        #endregion

    }
}
