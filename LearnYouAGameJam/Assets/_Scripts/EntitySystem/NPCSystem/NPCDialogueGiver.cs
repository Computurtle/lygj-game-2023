using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.DialogueSystem;
using LYGJ.Interactables;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem.NPCSystem {
    public sealed class NPCDialogueGiver : Interactable {
        /// <summary> Gets or sets the dialogue chain to use. </summary>
        [SerializeField, Tooltip("The dialogue chain to use.")] public DialogueChain? Dialogue = null;

        /// <summary> Gets or sets whether this NPC is interactable. </summary>
        [SerializeField, Tooltip("Whether this NPC is interactable.")] public bool Interactable = true;

        /// <summary> Event signature for when the player has spoken to this NPC. </summary>
        /// <param name="ExitCode"> The exit code of the dialogue chain. </param>
        public delegate void SpokenToEventHandler( int ExitCode );

        /// <summary> Raised when the player has spoken to this NPC. </summary>
        public event SpokenToEventHandler? SpokenTo;

        UniTaskCompletionSource<int>? _CTS = null;

        #region Implementation of IInteractable

        /// <inheritdoc />
        protected override bool CanInteract => Interactable && Dialogue != null;

        /// <inheritdoc />
        protected override async UniTask Interact() {
            if (Dialogue == null) {
                Debug.LogWarning("Cannot interact with NPC: No dialogue chain assigned.");
                return;
            }

            int ExitCode = await Dialogue.Play();
            SpokenTo?.Invoke(ExitCode);

            if (_CTS != null) {
                _CTS.TrySetResult(ExitCode);
            } else {
                Debug.LogWarning("UniTaskCompletionSource is null.");
            }
        }

        #endregion

        /// <summary> Waits for the player to interact with this NPC. </summary>
        /// <param name="Cleanup"> Whether to clean up the dialogue chain after waiting. </param>
        /// <param name="Token"> The cancellation token to use. </param>
        /// <returns> The exit code of the dialogue chain. </returns>
        public async UniTask<int> WaitForInteraction( bool Cleanup, CancellationToken Token = default ) {
            Debug.Assert(Dialogue != null, "Dialogue Giver has no dialogue chain assigned, yet WaitForInteraction was called.");
            if (_CTS != null) { throw new InvalidOperationException("Already waiting for interaction."); }

            _CTS = new();
            (bool Cancelled, int Result) = await _CTS.Task.AttachExternalCancellation(Token).SuppressCancellationThrow();
            _CTS = null;

            if (Cleanup) { Dialogue = null; }
            return Cancelled ? -1 : Result;
        }

        /// <summary> Whether an external listener is waiting for interaction. </summary>
        /// <returns> <see langword="true"/> if an external listener is waiting for interaction; otherwise, <see langword="false"/>. </returns>
        public bool IsWaitingForInteraction => _CTS != null;

        /// <summary> Waits for the current ongoing interaction to complete. </summary>
        /// <param name="Token"> The cancellation token to use. </param>
        /// <returns> The asynchronous operation. </returns>
        public async UniTask WaitForExistingInteractionCompletion( CancellationToken Token = default ) {
            if (_CTS == null) { return; }
            await _CTS.Task.AttachExternalCancellation(Token).SuppressCancellationThrow();
        }
    }

    public static class NPCDialogueGiverExtensions {
        /// <summary> Attempts to get the dialogue giver component from the given NPC, creating one if necessary. </summary>
        /// <param name="NPC"> The NPC to get the dialogue giver component from. </param>
        /// <param name="Giver"> [out] The dialogue giver component, if one was found or created. </param>
        /// <param name="Create"> Whether to create a dialogue giver component if one does not exist. </param>
        /// <param name="EvenIfInUse"> Whether to get the dialogue giver component even if it is in use. </param>
        /// <returns> <see langword="true"/> if a dialogue giver component was found or created; otherwise, <see langword="false"/>. </returns>
        public static bool TryGetDialogueGiver( this NPCBase NPC, [NotNullWhen(true)] out NPCDialogueGiver? Giver, bool Create = true, bool EvenIfInUse = true ) {
            Giver = NPC.GetComponent<NPCDialogueGiver>();
            if (Giver != null) {
                if (Giver.IsWaitingForInteraction && !EvenIfInUse) {
                    Giver = null;
                    return false;
                }

                return true;
            }

            if (!Create) { return false; }

            Giver = NPC.gameObject.AddComponent<NPCDialogueGiver>();
            return true;
        }

        /// <summary> Sets the ambient dialogue of the given NPC. </summary>
        /// <remarks> Ambient dialogue is dialogue whose result is ignored, and is purely used as 'filler'. </remarks>
        /// <param name="NPC"> The NPC to set the ambient dialogue of. </param>
        /// <param name="Dialogue"> The dialogue to set. </param>
        /// <param name="FailSilently"> Whether to fail silently if the NPC dialogue giver is already in use. </param>
        public static void SetAmbientDialogue( this NPCBase NPC, DialogueChain Dialogue, bool FailSilently = false ) {
            if (!NPC.TryGetDialogueGiver(out NPCDialogueGiver? Giver, Create: true, EvenIfInUse: false)) {
                if (!FailSilently) { throw new InvalidOperationException("NPC dialogue giver is already in use."); }
                return;
            }
            Giver.Dialogue = Dialogue;
        }
    }
}
