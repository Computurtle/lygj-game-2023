using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.QuestSystem {
    public abstract class Quest : ScriptableObject {
        /// <summary> The ID of the quest. </summary>
        #if UNITY_EDITOR
        [ValidateInput(nameof(IDMatchesName), "The ID of the quest does not match the name of the object.", ContinuousValidationCheck = true)]
        #endif
        [ShowInInspector, ReadOnly, Tooltip("The ID of the quest."), HorizontalGroup("ID"), PropertyOrder(-2)]
        public abstract string Key { get; }

        /// <summary> The name of the quest. </summary>
        [field: SerializeField, Tooltip("The name of the quest."), PropertyOrder(-1), Space]
        public string Name { get; private set; } = string.Empty;

        #if UNITY_EDITOR
        protected virtual void Reset() {
            name = Key;
            Name = Key.ConvertNamingConvention(NamingConvention.TitleCase);
        }
        [Button("Fix"), HorizontalGroup("ID", 50f), HideIf(nameof(IDMatchesName)), PropertySpace(30f)]
        void FixID() {
            UnityEditor.Undo.RecordObject(this, "Fix ID");
            UnityEditor.AssetDatabase.RenameAsset(UnityEditor.AssetDatabase.GetAssetPath(this), Key);
        }
        bool IDMatchesName() => name == Key;
        #endif

        /// <summary> Constructs the stages of the quest. </summary>
        /// <returns> The stages of the quest. </returns>
        protected abstract IEnumerable<(string ID, IQuestStateMonitor Monitor)> ConstructStages();

        // [ClearOnReload(null!)] // Can't be used in abstract classes.
        Dictionary<string, IQuestStateMonitor>? _Stages = null;

        /// <summary> The first stage of the quest. </summary>
        protected virtual string FirstStage => string.Empty;

        public void ConstructInternal() {
            if (_Stages is not null) {
                throw new InvalidOperationException("Quest has already been constructed.");
            }

            _Stages = new();
            foreach ((string ID, IQuestStateMonitor Monitor) in ConstructStages()) {
                _Stages.Add(ID, Monitor);
                QuestStates.Add(this.Key, ID, Monitor);
            }

            #if UNITY_EDITOR
            void OnEditorApplicationOnplayModeStateChanged( UnityEditor.PlayModeStateChange State ) {
                if (State == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                    UnityEditor.EditorApplication.playModeStateChanged -= OnEditorApplicationOnplayModeStateChanged;
                    _Stages = null;
                }
            }
            UnityEditor.EditorApplication.playModeStateChanged += OnEditorApplicationOnplayModeStateChanged;
            #endif
        }

        /// <summary> Starts the first stage of the quest. </summary>
        /// <exception cref="InvalidOperationException"> Quest has not been constructed. </exception>
        public void StartFirstStageInternal() {
            if (_Stages is null) {
                throw new InvalidOperationException("Quest has not been constructed.");
            }

            string F = FirstStage;
            StartStageInternal(!string.IsNullOrEmpty(F) ? F : _Stages.Keys.First());
        }

        /// <summary> Starts the specified stage of the quest. </summary>
        /// <param name="StageID"> The ID of the stage to start. </param>
        public void StartStageInternal( string StageID ) => QuestStates.StartStage(Key, StageID);
    }

    public abstract class QuestStateMonitor : IQuestStateMonitor {

        protected readonly List<Func<QuestStateResult>> Prerequisites = new();
        protected readonly List<Func<CancellationToken, UniTask<QuestStateResult>>> Performers = new();

        protected readonly List<Action> Finalisers = new();
        protected readonly List<Action> Completers = new();

        protected bool IsComplete { get; private set; }

        /// <inheritdoc cref="IQuestStateMonitor.Prerequisite"/>
        protected virtual QuestStateResult Prerequisite() => QuestStateResult.OK;

        /// <inheritdoc />
        QuestStateResult IQuestStateMonitor.Prerequisite() {
            QuestStateResult Result = Prerequisite();
            if (Result != QuestStateResult.OK) {
                return Result;
            }

            return OtherPrerequisites();
        }

        /// <summary> Checks if the additional prerequisites for the state are met, returning the first non-OK result. </summary>
        /// <returns> The first non-OK result, or <see cref="QuestStateResult.OK"/> if all prerequisites are met. </returns>
        protected QuestStateResult OtherPrerequisites() {
            foreach (Func<QuestStateResult> Prerequisite in Prerequisites) {
                QuestStateResult Result = Prerequisite();
                if (Result != QuestStateResult.OK) {
                    return Result;
                }
            }

            return QuestStateResult.OK;
        }

        /// <inheritdoc />
        public IQuestStateMonitor AddPrerequisite( Func<QuestStateResult> Prerequisite ) {
            Prerequisites.Add(Prerequisite);
            return this;
        }

        /// <inheritdoc cref="IQuestStateMonitor.Perform"/>
        protected abstract UniTask<QuestStateResult> Perform( CancellationToken Token );

        /// <inheritdoc />
        async UniTask<QuestStateResult> IQuestStateMonitor.Perform( CancellationToken Token ) {
            QuestStateResult Result = await Perform(Token);
            if (Result != QuestStateResult.OK) {
                return Result;
            }

            return await OtherPerformers(Token);
        }

        /// <summary> Checks if the additional performers for the state are met, returning the first non-OK result. </summary>
        /// <param name="Token"> The cancellation token to use. </param>
        /// <returns> The asynchronous operation which returns the first non-OK result, or <see cref="QuestStateResult.OK"/> if all performers are met. </returns>
        protected async UniTask<QuestStateResult> OtherPerformers( CancellationToken Token ) {
            foreach (Func<CancellationToken, UniTask<QuestStateResult>> Performer in Performers) {
                if (Token.IsCancellationRequested) { return QuestStateResult.RequiresImmediateRestart(); } // Despite being an 'immediate' restart, scene change still takes precedence.

                QuestStateResult Result = await Performer(Token);
                if (Result != QuestStateResult.OK) {
                    return Result;
                }
            }

            return QuestStateResult.OK;
        }

        /// <inheritdoc />
        public IQuestStateMonitor AddPerformer( Func<CancellationToken, UniTask<QuestStateResult>> Performer ) {
            Performers.Add(Performer);
            return this;
        }

        /// <inheritdoc />
        void IQuestStateMonitor.Finalise() {
            Finalise();
            foreach (Action Finaliser in Finalisers) {
                Finaliser();
            }

            if (!IsComplete) {
                IsComplete = true;
                Complete();
                foreach (Action Completer in Completers) {
                    Completer();
                }
            }
        }

        /// <inheritdoc />
        public IQuestStateMonitor AddFinaliser( Action Finaliser ) {
            Finalisers.Add(Finaliser);
            return this;
        }

        /// <inheritdoc />
        public IQuestStateMonitor AddCompleter( Action Completer ) {
            Completers.Add(Completer);
            return this;
        }

        /// <inheritdoc cref="IQuestStateMonitor.Finalise"/>
        protected virtual void Finalise() { }

        /// <summary> Called when the state is completed. </summary>
        protected virtual void Complete() { }
    }

    public interface IQuestStateMonitor {

        /// <summary> Determines if the prerequisite for the state is met. </summary>
        /// <returns> <see langword="true"/> if the prerequisite for the state is met; otherwise, <see langword="false"/>. </returns>
        QuestStateResult Prerequisite();

        /// <summary> Performs the state. </summary>
        /// <param name="Token"> The cancellation token. Cancelled when the scene changes. </param>
        /// <returns> The asynchronous operation that completes when the state is finished. </returns>
        UniTask<QuestStateResult> Perform( CancellationToken Token );

        /// <summary> The finaliser to call whenever revisiting the state. </summary>
        void Finalise();

        /// <summary> Adds another prerequisite to the state. </summary>
        /// <param name="Prerequisite"> The prerequisite to add. </param>
        /// <returns> The monitor. </returns>
        IQuestStateMonitor AddPrerequisite( Func<QuestStateResult> Prerequisite );

        /// <summary> Adds another performer to the state. </summary>
        /// <param name="Performer"> The performer to add. </param>
        /// <returns> The monitor. </returns>
        IQuestStateMonitor AddPerformer( Func<CancellationToken, UniTask<QuestStateResult>> Performer );

        /// <summary> Adds a finaliser to the state. </summary>
        /// <param name="Finaliser"> The finaliser to add. </param>
        /// <returns> The monitor. </returns>
        IQuestStateMonitor AddFinaliser( Action Finaliser );

        /// <summary> Adds a completer to the state. </summary>
        /// <param name="Completer"> The completer to add. </param>
        /// <returns> The monitor. </returns>
        IQuestStateMonitor AddCompleter( Action Completer );

    }
}
