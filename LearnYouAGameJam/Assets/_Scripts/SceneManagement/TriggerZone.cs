using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common.Physics;
using LYGJ.EntitySystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.SceneManagement {
    [RequireComponent(typeof(Collider))]
    #if UNITY_EDITOR
    [InfoBox("The collider for this trigger zone is not marked as 'Is Trigger'", InfoMessageType.Error, nameof(IsNotTrigger))]
    #endif
    public abstract class TriggerZone : ObjectBase {
        #if UNITY_EDITOR
        bool IsNotTrigger => TryGetComponent(out Collider C) && !C.isTrigger;
        #endif

        protected override void Awake() {
            base.Awake();
            TriggerMonitor Monitor = gameObject.GetTriggerMonitor();
            Monitor.TriggerEntered += TriggerEntered;
            Monitor.TriggerExited  += TriggerExited;
        }

        UniTaskCompletionSource?
            _EnterSource,
            _ExitSource;

        void TriggerEntered( Collider Other ) {
            if (!Other.CompareTag("Player")) { return; }

            if (_EnterSource is not null) {
                _EnterSource.TrySetResult();
                _EnterSource = null;
            }
        }

        void TriggerExited( Collider Other ) {
            if (!Other.CompareTag("Player")) { return; }

            if (_ExitSource is not null) {
                _ExitSource.TrySetResult();
                _ExitSource = null;
            }
        }

        /// <summary> Waits until the player enters the trigger zone. </summary>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> A task that completes when the player enters the trigger zone. </returns>
        public UniTask WaitForEntry( CancellationToken Token = default ) {
            if (_EnterSource is not null) {
                throw new InvalidOperationException("Already waiting for the player to enter the trigger zone!");
            }

            _EnterSource = new();
            return _EnterSource.Task.AttachExternalCancellation(Token).SuppressCancellationThrow();
        }

        /// <summary> Waits until the player exits the trigger zone. </summary>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> A task that completes when the player exits the trigger zone. </returns>
        public UniTask WaitForExit( CancellationToken Token = default ) {
            if (_ExitSource is not null) {
                throw new InvalidOperationException("Already waiting for the player to exit the trigger zone!");
            }

            _ExitSource = new();
            return _ExitSource.Task.AttachExternalCancellation(Token).SuppressCancellationThrow();
        }
    }
}
