using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using LYGJ.SceneManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace LYGJ.EntitySystem.EnemyManagement {
    public sealed class Enemies : SingletonMB<Enemies> {
        [SerializeField, Tooltip("Raised when an enemy is killed.")]
        UnityEvent<EnemyBase> _OnEnemyKilled = new();

        /// <summary> Raised when an enemy is killed. </summary>
        public static event UnityAction<EnemyBase> Killed {
            add => Instance._OnEnemyKilled.AddListener(value);
            remove => Instance._OnEnemyKilled.RemoveListener(value);
        }

        [ShowInInspector, HideInEditorMode, ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
        readonly HashSet<EnemyBase> _Enemies = new();

        /// <summary> Marks the given enemy as killed. </summary>
        /// <param name="Enemy"> The enemy to mark as killed. </param>
        public static void MarkKilled( EnemyBase Enemy ) {
            #if UNITY_EDITOR
            if (Scenes.IsApplicationExiting) { return; }
            #endif
            Instance.MarkKilledInternal(Enemy);
        }

        void MarkKilledInternal( EnemyBase Enemy ) {
            if (_Enemies.Remove(Enemy)) {
                _OnEnemyKilled.Invoke(Enemy);
            } else {
                Debug.LogWarning($"Tried to mark {Enemy} as killed, but it was not in the list of enemies.", Enemy);
            }
        }

        /// <summary> Adds the given enemy to the list of enemies. </summary>
        /// <param name="Enemy"> The enemy to add. </param>
        public static void Add( EnemyBase Enemy ) {
            #if UNITY_EDITOR
            if (Scenes.IsApplicationExiting) { return; }
            #endif
            Instance.AddInternal(Enemy);
        }

        void AddInternal( EnemyBase Enemy ) {
            if (!_Enemies.Add(Enemy)) {
                Debug.LogWarning($"Tried to add {Enemy} to the list of enemies, but it was already in the list.", Enemy);
            }
        }

        /// <summary> Removes the given enemy from the list of enemies. </summary>
        /// <param name="Enemy"> The enemy to remove. </param>
        /// <param name="Silent"> Whether to suppress warnings if the enemy was not in the list. </param>
        public static void Remove( EnemyBase Enemy, bool Silent = false ) {
            #if UNITY_EDITOR
            if (Scenes.IsApplicationExiting) { return; }
            #endif
            Instance.RemoveInternal(Enemy, Silent);
        }

        void RemoveInternal( EnemyBase Enemy, bool Silent ) {
            if (!_Enemies.Remove(Enemy) && !Silent) {
                Debug.LogWarning($"Tried to remove {Enemy} from the list of enemies, but it was not in the list.", Enemy);
            }
        }

        /// <summary> Waits for an enemy of the given type to be killed. </summary>
        /// <param name="Type"> The type of the enemy to be killed. </param>
        /// <param name="Amount"> The amount of the enemy to be killed before returning. </param>
        /// <param name="Token"> The cancellation token to use. </param>
        /// <returns> The asynchronous operation. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="Amount"/> is zero. </exception>
        public static async UniTask WaitForKills( EnemyType Type, uint Amount = 1u, CancellationToken Token = default ) {
            if (Amount == 0u) {
                throw new ArgumentOutOfRangeException(nameof(Amount), "Amount must be greater than or equal to one.");
            }

            UniTaskCompletionSource Completion = new();

            uint Killed = 0u;

            void OnKilled( EnemyBase Base ) {
                if (Base.Type != Type) { return; }

                Killed++;
                if (Killed >= Amount) {
                    Completion.TrySetResult();
                }
            }

            await using CancellationTokenRegistration Registration = Token.Register(() => Completion.TrySetCanceled());
            Instance._OnEnemyKilled.AddListener(OnKilled);
            await Completion.Task;
            Instance._OnEnemyKilled.RemoveListener(OnKilled);
        }
    }
}
