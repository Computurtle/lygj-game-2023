using System;
using LYGJ.Common;
using LYGJ.Common.Datatypes.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerHealth : SingletonMB<PlayerHealth>, IDamageTaker {
        [SerializeField, Tooltip("The player's health."), PropertyRange(0, nameof(_MaxHealth)), HorizontalGroup("H"), HideInPlayMode, PropertyOrder(-3)]
        float _Health = 100f;
        #if UNITY_EDITOR
        [OnValueChanged(nameof(Editor_FixHealth))]
        #endif
        [SerializeField, Tooltip("The player's maximum health."), Min(1), HorizontalGroup("H", 0.3f), LabelText(" /"), LabelWidth(20f), HideInPlayMode, PropertyOrder(-2)]
        float _MaxHealth = 100f;

        #if UNITY_EDITOR
        void Editor_FixHealth() {
            if (_Health >= _MaxHealth) {
                _Health = _MaxHealth;
            }
        }
        #endif

        readonly PriorityList<HealthPriority, bool> _Invulnerable = new(false);

        [Space]
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the player takes damage.")]
        UnityEvent<float> _OnDamageTaken = new();
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the player dies.")]
        UnityEvent _OnDeath = new();
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the player is revived.")]
        UnityEvent _OnRevive = new();
        [SerializeField, FoldoutGroup("Events"), Tooltip("Raised when the player is healed.")]
        UnityEvent<float> _OnHeal = new();

        #if UNITY_EDITOR
        [ShowInInspector, HideInEditorMode, Tooltip("Whether or not the player is invulnerable."), ToggleLeft, LabelText("Is Invulnerable"), PropertyOrder(-1)]
        bool Editor_IsInvulnerable {
            get => _Invulnerable.DefaultValue;
            set => _Invulnerable.DefaultValue = value;
        }
        #endif

        /// <summary> Raised when the player takes damage. </summary>
        public static event UnityAction<float> DamageTaken {
            add => Instance._OnDamageTaken.AddListener(value);
            remove => Instance._OnDamageTaken.RemoveListener(value);
        }

        /// <summary> Raised when the player dies. </summary>
        public static event UnityAction Died {
            add => Instance._OnDeath.AddListener(value);
            remove => Instance._OnDeath.RemoveListener(value);
        }

        /// <summary> Raised when the player is revived. </summary>
        public static event UnityAction Revived {
            add => Instance._OnRevive.AddListener(value);
            remove => Instance._OnRevive.RemoveListener(value);
        }

        /// <summary> Raised when the player is healed. </summary>
        public static event UnityAction<float> Healed {
            add => Instance._OnHeal.AddListener(value);
            remove => Instance._OnHeal.RemoveListener(value);
        }

        /// <summary> Whether or not the player is invulnerable. </summary>
        public static bool IsInvulnerable => Instance._Invulnerable;

        /// <summary> Adds an invulnerability override. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="Value"> The value of the override. </param>
        public static void SetInvulnerable( HealthPriority Priority, bool Value = true ) => Instance._Invulnerable.AddOverride(Priority, Value);

        /// <summary> Adds a vulnerability override. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="Value"> The value of the override. </param>
        /// <remarks> This is the same as <see cref="SetInvulnerable"/> but with the value inverted. </remarks>
        public static void SetVulnerable( HealthPriority Priority, bool Value = true ) => Instance._Invulnerable.AddOverride(Priority, !Value);

        /// <summary> Removes an invulnerability override. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        public static void ClearInvulnerable( HealthPriority Priority ) => Instance._Invulnerable.RemoveOverride(Priority);

        /// <summary> Removes a vulnerability override. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <remarks> This is the same as <see cref="ClearInvulnerable"/>. </remarks>
        public static void ClearVulnerable( HealthPriority Priority ) => Instance._Invulnerable.RemoveOverride(Priority);

        /// <summary> Gets or sets the player's health. </summary>
        [ShowInInspector, Tooltip("The player's health."), PropertyRange(0, nameof(_MaxHealth)), HorizontalGroup("H"), HideInEditorMode, DisableIf(nameof(Editor_IsInvulnerable)), PropertyOrder(-3)]
        public static float Health {
            get => Instance._Health;
            set {
                float Old = Instance._Health;
                if (value == Old) { return; }
                if (value > Old) {
                    Instance.Heal(value - Old);
                } else {
                    Instance.TakeDamage(Old - value);
                }
            }
        }

        /// <summary> Gets or sets the player's maximum health. </summary>
        [ShowInInspector, Tooltip("The player's maximum health."), MinValue(1), HorizontalGroup("H", 0.3f), LabelText("/"), LabelWidth(20f), HideInEditorMode, DisableIf(nameof(Editor_IsInvulnerable)), PropertyOrder(-2)]
        public static float MaxHealth {
            get => Instance._MaxHealth;
            set {
                float Old = Instance._MaxHealth;
                if (value == Old) { return; }
                Instance._MaxHealth = value;
                if (value < Old) {
                    if (Instance._Health > value) {
                        Instance.TakeDamage(Old - value);
                    }
                }
            }
        }

        #region Implementation of IDamageTaker

        /// <inheritdoc />
        public void TakeDamage( float Amount ) {
            if (_Invulnerable) { return; }

            switch (Amount) {
                case < 0f:
                    Debug.LogWarning($"Cannot deal negative damage to {name}. Healing instead.", this);
                    Heal(-Amount);
                    return;
                case 0f:
                    Debug.LogWarning("Dealing 0 damage is pointless. Skipping.", this);
                    return;
            }

            if (Amount > _Health) {
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
        LifeState IDamageTaker.LifeState => _Invulnerable ? LifeState.Invulnerable : _Health <= 0 ? LifeState.Dead : LifeState.Alive;

        #endregion

    }

    public interface IDamageTaker {
        /// <summary> Deals damage to the object. </summary>
        /// <param name="Amount"> The amount of damage to deal. </param>
        void TakeDamage( float Amount );

        /// <summary> Heals the object. </summary>
        /// <param name="Amount"> The amount of health to restore. </param>
        void Heal( float Amount );

        /// <summary> The object's health. </summary>
        float Health { get; }

        /// <summary> The object's maximum health. </summary>
        float MaxHealth { get; }

        /// <summary> The object's health as a percentage. </summary>
        public float HealthPercentage => Health / MaxHealth;

        /// <summary> The object's current life state. </summary>
        LifeState LifeState { get; }

        /// <summary> Whether or not the object is dead. </summary>
        public bool IsDead => LifeState is LifeState.Dead;

        /// <summary> Whether or not the object is invulnerable. </summary>
        public bool IsInvulnerable => LifeState is LifeState.Invulnerable;
    }

    public enum LifeState {
        /// <summary> The object is alive. </summary>
        Alive,

        /// <summary> The object is dead. </summary>
        Dead,

        /// <summary> The object is invulnerable. </summary>
        Invulnerable
    }

    public enum HealthPriority {
        /// <summary> General gameplay. </summary>
        Gameplay,
        /// <summary> A minigame. </summary>
        Minigame,
        /// <summary> General UI. </summary>
        [Obsolete]
        UI,
        /// <summary> Barter. </summary>
        Barter,
        /// <summary> Inventory. </summary>
        Inventory,
        /// <summary> Dialogue. </summary>
        Dialogue,
        /// <summary> Pause Menu. </summary>
        PauseMenu,
    }
}
