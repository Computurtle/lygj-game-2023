using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace LYGJ.Interactables {
    public interface IInteractable {
        /// <summary> Gets whether the object can currently be interacted with. </summary>
        /// <returns> <see langword="true"/> if the object can currently be interacted with; otherwise, <see langword="false"/>. </returns>
        bool CanInteract { get; }

        /// <summary> Interacts with the given object. </summary>
        /// <returns> The asynchronous task that finishes when interaction is complete. </returns>
        [MustUseReturnValue] UniTask Interact();

        /// <summary> Waits for the object to start being interacted with. </summary>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> The asynchronous task that finishes when interaction starts. </returns>
        [MustUseReturnValue] UniTask WaitForInteractionInitiation( CancellationToken Token = default );

        /// <summary> Waits for the object to finish being interacted with. </summary>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> The asynchronous task that finishes when interaction finishes. </returns>
        [MustUseReturnValue] UniTask WaitForInteraction( CancellationToken Token = default );
    }
}
