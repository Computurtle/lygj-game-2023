using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.EntitySystem;
using UnityEngine;

namespace LYGJ.Interactables {
    public abstract class Interactable : MonoBehaviour, IInteractable {

        UniTaskCompletionSource? _StartTask, _EndTask = null;

        #region Implementation of IInteractable

        /// <inheritdoc cref="IInteractable.CanInteract" />
        protected virtual bool CanInteract => true;

        /// <inheritdoc />
        bool IInteractable.CanInteract => isActiveAndEnabled && CanInteract;

        /// <inheritdoc cref="IInteractable.Interact" />
        protected abstract UniTask Interact();

        /// <inheritdoc />
        public UniTask WaitForInteractionInitiation( CancellationToken Token ) {
            _StartTask ??= new();
            return _StartTask.Task.AttachExternalCancellation(Token);
        }

        /// <inheritdoc />
        public UniTask WaitForInteraction( CancellationToken Token ) {
            _EndTask ??= new();
            return _EndTask.Task.AttachExternalCancellation(Token);
        }

        /// <inheritdoc />
        async UniTask IInteractable.Interact() {
            if (_StartTask is not null) {
                _StartTask.TrySetResult();
                _StartTask = null;
            }
            await Interact();
            if (_EndTask is not null) {
                _EndTask.TrySetResult();
                _EndTask = null;
            }
        }

        #endregion

    }

    public abstract class InteractableObjectBase : ObjectBase, IInteractable {

        UniTaskCompletionSource? _StartTask, _EndTask = null;

        #region Implementation of IInteractable

        /// <inheritdoc cref="IInteractable.CanInteract" />
        protected virtual bool CanInteract => true;

        /// <inheritdoc />
        bool IInteractable.CanInteract => isActiveAndEnabled && CanInteract;

        /// <inheritdoc cref="IInteractable.Interact" />
        protected abstract UniTask Interact();

        /// <inheritdoc />
        public UniTask WaitForInteractionInitiation( CancellationToken Token ) {
            _StartTask ??= new();
            return _StartTask.Task.AttachExternalCancellation(Token);
        }

        /// <inheritdoc />
        public UniTask WaitForInteraction( CancellationToken Token ) {
            _EndTask ??= new();
            return _EndTask.Task.AttachExternalCancellation(Token);
        }

        /// <inheritdoc />
        async UniTask IInteractable.Interact() {
            if (_StartTask is not null) {
                _StartTask.TrySetResult();
                _StartTask = null;
            }
            await Interact();
            if (_EndTask is not null) {
                _EndTask.TrySetResult();
                _EndTask = null;
            }
        }

        #endregion

    }
}
