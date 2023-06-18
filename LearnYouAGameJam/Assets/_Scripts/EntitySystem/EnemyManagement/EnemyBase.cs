using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.EnemyManagement {
    public abstract class EnemyBase : MonoBehaviour {
        /// <summary> The enemy type. </summary>
        [ShowInInspector, ReadOnly, Tooltip("The enemy type.")]
        public abstract EnemyType Type { get; }

        /// <summary> Whether this enemy is killed. </summary>
        [ShowInInspector, ReadOnly, Tooltip("Whether the enemy has been killed."), HideInEditorMode, ToggleLeft]
        public bool IsKilled { get; protected set; } = false;

        /// <summary> Kills this enemy. </summary>
        [Button, HideInEditorMode, DisableIf(nameof(IsKilled))]
        public void Kill() {
            if (IsKilled) {
                Debug.LogWarning($"Tried to kill {this}, but it was already killed.", this);
                return;
            }
            IsKilled = true;
            KillInternal();
            Enemies.MarkKilled(this);
        }

        /// <summary> Called when this enemy is killed. </summary>
        protected virtual void KillInternal() => Destroy(gameObject);

        protected virtual void Awake() => Enemies.Add(this);

        protected virtual void OnDestroy() => Enemies.Remove(this, Silent: IsKilled);
    }

}
