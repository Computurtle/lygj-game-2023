using LYGJ.EntitySystem.PlayerManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace LYGJ.EntitySystem.EnemyManagement {
    public sealed class EnemyHealth : MonoBehaviour, IDamageTaker {
        [SerializeField, Tooltip("The enemy's health."), PropertyRange(0, nameof(_MaxHealth)), HorizontalGroup("H"), HideInPlayMode, PropertyOrder(-2)]
        float _Health = 100f;
        #if UNITY_EDITOR
        [OnValueChanged(nameof(Editor_FixHealth))]
        #endif
        [SerializeField, Tooltip("The enemy's maximum health."), Min(1), HorizontalGroup("H", 0.3f), LabelText(" /"), LabelWidth(20f), HideInPlayMode, PropertyOrder(-1)]
        float _MaxHealth = 100f;
        [SerializeField, Tooltip("Whether or not the enemy is invulnerable."), ToggleLeft]
        bool _IsInvulnerable = false;

        #if UNITY_EDITOR
        void Editor_FixHealth() {
            if (_Health >= _MaxHealth) {
                _Health = _MaxHealth;
            }
        }
        #endif

        [Space]
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the enemy takes damage.")]
        UnityEvent<float> _OnDamageTaken = new();
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the enemy dies.")]
        UnityEvent _OnDeath = new();
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the enemy is revived.")]
        UnityEvent _OnRevive = new();
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the enemy is healed.")]
        UnityEvent<float> _OnHeal = new();

        /// <summary> Raised when the enemy takes damage. </summary>
        public event UnityAction<float> DamageTaken {
            add => _OnDamageTaken.AddListener(value);
            remove => _OnDamageTaken.RemoveListener(value);
        }

        /// <summary> Raised when the enemy dies. </summary>
        public event UnityAction Died {
            add => _OnDeath.AddListener(value);
            remove => _OnDeath.RemoveListener(value);
        }

        /// <summary> Raised when the enemy is revived. </summary>
        public event UnityAction Revived {
            add => _OnRevive.AddListener(value);
            remove => _OnRevive.RemoveListener(value);
        }

        /// <summary> Raised when the enemy is healed. </summary>
        public event UnityAction<float> Healed {
            add => _OnHeal.AddListener(value);
            remove => _OnHeal.RemoveListener(value);
        }

        /// <summary> Whether or not the enemy is invulnerable. </summary>
        public bool IsInvulnerable => _IsInvulnerable;

        /// <summary> Gets or sets the enemy's health. </summary>
        [ShowInInspector, Tooltip("The enemy's health."), PropertyRange(0, nameof(_MaxHealth)), HorizontalGroup("H"), HideInEditorMode, DisableIf(nameof(_IsInvulnerable)), PropertyOrder(-2)]
        public float Health {
            get => _Health;
            set {
                float Old = _Health;
                if (value == Old) { return; }
                if (value > Old) {
                    Heal(value - Old);
                } else {
                    TakeDamage(Old - value);
                }
            }
        }

        /// <summary> Gets or sets the enemy's maximum health. </summary>
        [ShowInInspector, Tooltip("The enemy's maximum health."), MinValue(1), HorizontalGroup("H", 0.3f), LabelText("/"), LabelWidth(20f), HideInEditorMode, DisableIf(nameof(_IsInvulnerable)), PropertyOrder(-1)]
        public float MaxHealth {
            get => _MaxHealth;
            set {
                float Old = _MaxHealth;
                if (value == Old) { return; }
                _MaxHealth = value;
                if (value < Old) {
                    if (_Health > value) {
                        TakeDamage(Old - value);
                    }
                }
            }
        }

        /// <summary> Kills the enemy. </summary>
        [Button, DisableIf("@" + nameof(_IsInvulnerable) + " || " + nameof(_Health) + " <= 0f"), HorizontalGroup("H2"), HideInEditorMode, PropertyOrder(-3)]
        public void Kill() => Health = 0f;

        /// <summary> Revives the enemy. </summary>
        [Button, DisableIf("@" + nameof(_IsInvulnerable) + " || " + nameof(_Health) + " > 0f"), HorizontalGroup("H2"), HideInEditorMode, PropertyOrder(-3)]
        public void Revive() => Health = _MaxHealth;

        #region Implementation of IDamageTaker

        /// <inheritdoc />
        public void TakeDamage( float Amount ) {
            if (_IsInvulnerable) { return; }

            switch (Amount) {
                case < 0f:
                    Debug.LogWarning($"Cannot deal negative damage to {name}. Healing instead.", this);
                    Heal(-Amount);
                    return;
                case 0f:
                    Debug.LogWarning("Dealing 0 damage is pointless. Skipping.", this);
                    return;
            }

            if (Amount >= _Health) {
                _Health = 0f;
                _OnDamageTaken.Invoke(Amount);
                _OnDeath.Invoke();
            } else {
                _Health -= Amount;
                _OnDamageTaken.Invoke(Amount);
            }
        }

        /// <inheritdoc />
        public void Heal( float Amount ) {
            switch (Amount) {
                case < 0f:
                    Debug.LogWarning($"Cannot heal negative damage to {name}. Dealing damage instead.", this);
                    TakeDamage(-Amount);
                    return;
                case 0f:
                    Debug.LogWarning("Healing 0 damage is pointless. Skipping.", this);
                    return;
            }

            bool WasDead = _Health <= 0f;
            if (_Health + Amount > _MaxHealth) {
                _Health = _MaxHealth;
                _OnHeal.Invoke(Amount);
            } else {
                _Health += Amount;
                _OnHeal.Invoke(Amount);
            }
            if (WasDead) { _OnRevive.Invoke(); }
        }

        /// <inheritdoc />
        float IDamageTaker.Health => _Health;

        /// <inheritdoc />
        float IDamageTaker.MaxHealth => _MaxHealth;

        /// <inheritdoc />
        LifeState IDamageTaker.LifeState => _IsInvulnerable ? LifeState.Invulnerable : _Health <= 0 ? LifeState.Dead : LifeState.Alive;

        #endregion

    }
}
