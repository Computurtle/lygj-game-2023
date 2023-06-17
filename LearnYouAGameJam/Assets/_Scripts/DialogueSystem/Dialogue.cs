using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem {
    public static partial class Dialogue {

        /// <summary> Event signature for the <see cref="Started"/> event. </summary>
        /// <param name="Chain"> The chain that was started. </param>
        public delegate void StartedEventHandler( DialogueChain Chain );

        /// <summary> Raised when a dialogue chain is started. </summary>
        public static event StartedEventHandler? Started;

        /// <summary> Event signature for the <see cref="Ended"/> event. </summary>
        /// <param name="Chain"> The chain that was ended. </param>
        /// <param name="Exit"> The exit code. <c>0</c> is the default when none is specified. </param>
        public delegate void EndedEventHandler( DialogueChain Chain, int Exit );

        /// <summary> Raised when a dialogue chain is ended. </summary>
        public static event EndedEventHandler? Ended;

        /// <summary> Event signature for the <see cref="TextDisplayed"/> event. </summary>
        /// <param name="Object"> The object that is being displayed. </param>
        /// <param name="Speaker"> The speaker of the object. </param>
        /// <param name="SpeakerKnown"> Whether the speaker is known. </param>
        /// <param name="Text"> The text to display. </param>
        /// <param name="Token"> The cancellation token. </param>
        public delegate UniTask TextDisplayedEventHandler( DialogueObject Object, string Speaker, bool SpeakerKnown, string Text, CancellationToken Token );

        /// <summary> Raised when some text is spoken by someone. </summary>
        public static event TextDisplayedEventHandler? TextDisplayed;

        /// <summary> Event signature for the <see cref="TextCleared"/> event. </summary>
        /// <param name="Object"> The object that is being displayed. </param>
        /// <param name="Speaker"> The speaker of the object. </param>
        public delegate UniTask TextClearedEventHandler( DialogueObject Object, string Speaker );

        /// <summary> Raised when someone stops speaking. </summary>
        public static event TextClearedEventHandler? TextCleared;

        /// <summary> Clears the currently displayed text. </summary>
        /// <param name="Object"> The object that is being displayed. </param>
        /// <param name="Speaker"> The speaker of the object. </param>
        internal static UniTask ClearText( DialogueObject Object, string Speaker ) => TextCleared?.RunAll(Object, Speaker) ?? UniTask.CompletedTask;

        static readonly StringBuilder _SB = new();

        /// <summary> Displays the given text excerpt. </summary>
        /// <param name="Object"> The object that is being displayed. </param>
        /// <param name="Speaker"> The speaker of the object. </param>
        /// <param name="SpeakerKnown"> Whether the speaker is known. </param>
        /// <param name="Line"> The line to display. </param>
        internal static async UniTask DisplayText( DialogueObject Object, string Speaker, bool SpeakerKnown, string Line ) {
            if (_TypeAnimSkip is not null) {
                _TypeAnimSkip.Cancel();
                _TypeAnimSkip = null;
            }
            _TypeAnimSkip = new();
            CancellationToken Token = _TypeAnimSkip.Token;

            _SB.Clear();
            _SB.Append(Line);
            if (TextDisplayed is not null) {
                await TextDisplayed.RunAll(Object, Speaker, SpeakerKnown, _SB.ToString(), Token);
            } else {
                Debug.LogWarning("No text display handler is registered!");
            }

            if (_TypeAnimSkip is not null) {
                _TypeAnimSkip.Cancel();
                _TypeAnimSkip = null;
            }
        }

        /// <summary> Event signature for the <see cref="ChoicesDisplayed"/> event. </summary>
        /// <param name="Choices"> The choices to display. </param>
        /// <param name="Chosen"> The chosen choice. </param>
        /// <returns> The chosen choice. </returns>
        public delegate UniTask ChoicesDisplayedEventHandler( IReadOnlyList<DialogueChoiceOption> Choices, DynamicTaskReturn<int> Chosen );

        /// <summary> Raised when choices are displayed. </summary>
        public static event ChoicesDisplayedEventHandler? ChoicesDisplayed;

        /// <summary> Event signature for the <see cref="ChoicesCleared"/> event. </summary>
        public delegate UniTask ChoicesClearedEventHandler();

        /// <summary> Raised when choices are cleared. </summary>
        public static event ChoicesClearedEventHandler? ChoicesCleared;

        [ExecuteOnReload]
        static void ClearEvents() {
            Started          = null;
            Ended            = null;
            TextDisplayed    = null;
            TextCleared      = null;
            ChoicesDisplayed = null;
            ChoicesCleared   = null;
        }

        /// <summary> Displays the given choices. </summary>
        /// <param name="Choices"> The choices to display. </param>
        /// <returns> The chosen choice. </returns>
        internal static async UniTask<DialogueChoiceOption> DisplayChoices( IReadOnlyList<DialogueChoiceOption> Choices ) {
            if (ChoicesDisplayed is null) {
                Debug.LogWarning("No choice display handler is registered!");
                return Choices.First();
            }

            DynamicTaskReturn<int> Chosen = new(0);
            await ChoicesDisplayed.RunAll(Choices, Chosen);

            Chosen.EnsureLocked();

            DialogueChoiceOption Result = Choices[Chosen.Value];
            if (ChoicesCleared is not null) {
                await ChoicesCleared.RunAll();
            }
            return Result;
        }

        [ClearOnReload(null!)]
        static CancellationTokenSource? _TypeAnimSkip = null;

        /// <summary> Skips the current typing animation. </summary>
        /// <returns> <see langword="true"/> if the animation was skipped, <see langword="false"/> if there was no animation to skip. </returns>
        public static bool SkipAnimation() {
            if (_TypeAnimSkip is null) {
                return false;
            }

            _TypeAnimSkip.Cancel();
            _TypeAnimSkip = null;
            return true;
        }

        [ClearOnReload(null!)]
        static UniTaskCompletionSource? _ContinueSource = null;

        /// <summary> Continues the current dialogue. </summary>
        /// <returns> <see langword="true"/> if the dialogue was continued, <see langword="false"/> if there was no dialogue to continue. </returns>
        public static bool Continue() {
            if (_ContinueSource is null) {
                return false;
            }

            _ContinueSource.TrySetResult();
            _ContinueSource = null!;
            return true;
        }

        /// <summary> Waits for the user to continue the dialogue. </summary>
        /// <returns> The task to await. </returns>
        public static UniTask WaitForInput() {
            if (_ContinueSource is not null) {
                Debug.LogError($"{nameof(Dialogue)}: Cannot wait for the user to continue while another dialogue is still waiting.");
                return UniTask.CompletedTask;
            }

            _ContinueSource = new();
            return _ContinueSource.Task;
        }

        static readonly Queue<UniTask> _ForcedDelays = new();

        /// <summary> Gets or sets the currently speaking character. </summary>
        /// <returns> The currently speaking character. <see langword="null"/> if no character is speaking. </returns>
        public static string? CurrentSpeaker { get; set; } = null;

        /// <summary> Gets whether any dialogue is currently running. </summary>
        /// <returns> <see langword="true"/> if any dialogue is currently running, <see langword="false"/> otherwise. </returns>
        [field: ClearOnReload(valueToAssign: false)]
        public static bool IsRunning { get; private set; } = false;

        static async UniTask<int> DisplayDialogue( DialogueChain Chain ) {
            if (IsRunning) {
                throw new($"{nameof(Dialogue)}: Cannot display a dialogue while another dialogue is still running.");
            }
            IsRunning = true;

            CurrentSpeaker = null;
            int CurrentIndex = Chain.FirstIndex;
            int ExitCode     = 0;
            Started?.RunAll(Chain);

            while (Chain[CurrentIndex] is { } Current) {
                while (_ForcedDelays.Count > 0) {
                    Debug.Log("Waiting for forced delay...");
                    await _ForcedDelays.Dequeue();
                }

                DialogueInstruction Instruction = await Current.Display(Chain);
                if (Instruction.PauseForInput) {
                    await WaitForInput();
                }
                switch (Instruction.Type) {
                    case DialogueInstructionType.Continue:
                        CurrentIndex++;
                        break;
                    case DialogueInstructionType.Goto:
                        CurrentIndex = Instruction.Index;
                        break;
                    case DialogueInstructionType.Exit:
                        ExitCode = Instruction.ExitCode;
                        goto End;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            while (_ForcedDelays.Count > 0) {
                Debug.Log("Waiting for final forced delay...");
                await _ForcedDelays.Dequeue();
            }

            End:
            Ended?.RunAll(Chain, ExitCode);
            CurrentSpeaker = null;
            IsRunning    = false;
            return ExitCode;
        }

        /// <summary> Inserts a forced delay between this instruction and the next. </summary>
        /// <param name="Delay"> The delay to insert. </param>
        [Obsolete]
        public static void InsertForcedDelay( UniTask Delay ) => _ForcedDelays.Enqueue(Delay);

        /// <inheritdoc cref="InsertForcedDelay(UniTask)"/>
        [Obsolete]
        public static void InsertForcedDelay( IEnumerator Routine ) => InsertForcedDelay(Routine.ToUniTask());

        /// <summary> Displays the given dialogue chain. </summary>
        /// <param name="Chain"> The dialogue chain to display. </param>
        /// <returns> The exit code. </returns>
        public static UniTask<int> Display( DialogueChain Chain ) => DisplayDialogue(Chain);
    }
}
