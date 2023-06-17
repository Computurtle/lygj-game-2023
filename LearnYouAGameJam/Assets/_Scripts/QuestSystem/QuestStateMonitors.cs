using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.DialogueSystem;
using LYGJ.EntitySystem;
using LYGJ.EntitySystem.NPCSystem;
using LYGJ.Interactables;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.QuestSystem {
    public sealed class TalkToNPC : QuestStateMonitor {

        readonly string                       _Key;
        readonly DialogueChain                _Chain;
        readonly Func<int, QuestStateResult>? _OnAnswer;

        public TalkToNPC( string Key, DialogueChain Chain, Func<int, QuestStateResult>? OnAnswer = null, Action? OnFinalise = null ) {
            _Key         = Key;
            _Chain      = Chain;
            _OnAnswer   = OnAnswer;
            if (OnFinalise is not null) {
                Finalisers.Add(OnFinalise);
            }
        }

        public TalkToNPC( string Key, DialogueChain Chain, DialogueChain FinaliseChain, Func<int, QuestStateResult>? OnAnswer = null ) : this(
            Key, Chain, OnAnswer, () => {
                NPCBase NPC = NPCs.Get(Key);
                if (NPC.TryGetDialogueGiver(out NPCDialogueGiver? Giver, EvenIfInUse: false)) {
                    Giver.Dialogue = FinaliseChain;
                }
            }
        ) { }

        #region Implementation of IQuestStateMonitor

        /// <inheritdoc />
        protected override QuestStateResult Prerequisite() {
            if (NPCs.Exists(_Key)) {
                // Debug.Log($"NPC with ID {_ID} exists.");
                return QuestStateResult.OK;
            }

            // Debug.LogError($"NPC with ID {_ID} does not exist.");
            return QuestStateResult.RequiresNPC(_Key);
        }

        /// <inheritdoc />
        protected override async UniTask<QuestStateResult> Perform( CancellationToken Token ) {
            NPCBase NPC = NPCs.Get(_Key);
            if (NPC.TryGetComponent(out NPCDialogueGiver Giver) && Giver.IsWaitingForInteraction) {
                Debug.LogError($"NPC with ID {_Key} already has a dialogue giver which is awaiting interaction. Waiting for next scene change.");
                return QuestStateResult.RequiresTaskCompletion(Giver.WaitForExistingInteractionCompletion);
            }

            if (Giver == null) {
                Giver = NPC.gameObject.AddComponent<NPCDialogueGiver>();
            }

            Giver.Dialogue = _Chain;
            // Debug.Log($"NPC with ID {_ID} has been given dialogue ({_Chain}) that is awaiting interaction.");
            int Result = await Giver.WaitForInteraction(Cleanup: false, Token: Token);
            if (Token.IsCancellationRequested) {
                Debug.LogWarning("Quest state was cancelled.");
                return QuestStateResult.RequiresImmediateRestart();
            }

            return _OnAnswer is not null ? _OnAnswer.Invoke(Result) : QuestStateResult.OK;
        }

        #endregion

    }

    public sealed class Finaliser : QuestStateMonitor {

        readonly Action? _Perform;

        /// <summary> Creates a new finaliser which is always invoked. </summary>
        /// <param name="Perform"> The perform action to invoke once and only once in the lifetime of the finaliser. </param>
        /// <param name="Finalise"> The finalisation action. </param>
        public Finaliser( Action Perform, Action Finalise ) {
            _Perform = Perform;
            Finalisers.Add(Finalise);
        }

        /// <inheritdoc cref="Finaliser" />
        public Finaliser( Action Finalise ) : this(null!, Finalise) { }

        #region Implementation of IQuestStateMonitor

        /// <inheritdoc />
        protected override UniTask<QuestStateResult> Perform( CancellationToken Token ) {
            _Perform?.Invoke();
            return UniTask.FromResult(QuestStateResult.OK);
        }

        #endregion

    }

    public sealed class InteractWithObject : QuestStateMonitor {

        readonly string _Key;

        readonly QuestStateResult
            _PrerequisiteFailure,
            _InteractFailure;

        public InteractWithObject( string Key, QuestStateResult? PrerequisiteFailure = null, QuestStateResult? InteractFailure = null ) {
            _Key = Key;
            if (PrerequisiteFailure is not null) {
                if (PrerequisiteFailure == QuestStateResult.OK) {
                    throw new ArgumentException("PrerequisiteFailure cannot be OK.", nameof(PrerequisiteFailure));
                }
                _PrerequisiteFailure = PrerequisiteFailure;
            } else {
                _PrerequisiteFailure = QuestStateResult.RequiresObject(_Key);
            }
            if (InteractFailure is not null) {
                if (InteractFailure == QuestStateResult.OK) {
                    throw new ArgumentException("InteractFailure cannot be OK.", nameof(InteractFailure));
                }
                _InteractFailure = InteractFailure;
            } else {
                _InteractFailure = QuestStateResult.RequiresImmediateRestart();
            }
        }

        #region Overrides of QuestStateMonitor

        /// <inheritdoc />
        protected override QuestStateResult Prerequisite() => Objects.Exists(_Key) ? QuestStateResult.OK : _PrerequisiteFailure;

        /// <inheritdoc />
        protected override async UniTask<QuestStateResult> Perform( CancellationToken Token ) {
            await Objects.Get<InteractableObjectBase>(_Key).WaitForInteraction(Token);
            if (Token.IsCancellationRequested) {
                return _InteractFailure;
            }
            return QuestStateResult.OK;
        }

        #endregion

    }
}
