using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using LYGJ.Common;
using LYGJ.SaveManagement;
using LYGJ.SceneManagement;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace LYGJ.QuestSystem {
    public sealed class QuestStates : SingletonMB<QuestStates> {

        #region Overrides of SingletonMB<QuestStates>

        /// <inheritdoc />
        protected override void Awake() {
            if (Scenes.IsLoadingScreen) {
                _PerformedOnSceneChanged = true;
            } else {
                _PerformedOnSceneChanged = false;
                foreach (MonitoredQuestState State in _States) {
                    State.Performed = PerformanceType.NotPerformed;
                }
            }

            if (_Instance != null && _Instance != this) {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(this);
            _Instance = this;
        }

        #endregion

        /// <inheritdoc cref="SingletonMB{T}.Instance"/>
        public new static QuestStates Instance {
            get {
                if (_Instance == null) {
                    #if UNITY_EDITOR
                    if (Application.isPlaying) {
                        #endif
                        Debug.LogWarning($"No instance of {typeof(QuestStates).GetNiceName()} was set. Make sure that cross-script dependencies are performed in Start() or later.");
                        #if UNITY_EDITOR
                    }
                    #endif
                    _Instance = FindObjectOfType<QuestStates>();
                    if (_Instance == null) {
                        GameObject GO = new(nameof(QuestStates), typeof(QuestStates));
                        _Instance = GO.GetComponent<QuestStates>();
                    }
                }

                return _Instance;
            }
        }

        public enum PerformanceType {
            NotPerformed,
            PerformedInLoop,
            PerformedOutOfLoop
        }

        sealed class MonitoredQuestState {
            public readonly string             ID;
            public readonly IQuestStateMonitor Monitor;

            public PerformanceType Performed = PerformanceType.NotPerformed;

            public MonitoredQuestState( string ID, IQuestStateMonitor Monitor ) {
                this.ID      = ID;
                this.Monitor = Monitor;
            }

            public StateState State { get; set; } = StateState.NotStarted;

            #region Overrides of Object

            /// <inheritdoc />
            public override string ToString() => $"{ID} ({Monitor.GetType().GetNiceName()}); {Performed.ToString().ConvertNamingConvention(NamingConvention.TitleCase)}";

            #endregion

        }

        [ShowInInspector, ReadOnly, DisplayAsString, PropertyOrder(-1)]
        static readonly List<MonitoredQuestState> _States = new();

        [ExecuteOnReload]
        static void CleanupStates() {
            _States.Clear();
            _States.TrimExcess();
        }

        /// <summary> Adds a quest state to be monitored. </summary>
        /// <param name="ID"> The ID of the state. </param>
        /// <param name="Monitor"> The monitor of the quest state. </param>
        public static void Add( [LocalizationRequired(false)] string ID, IQuestStateMonitor Monitor ) => AddStateInternal(new(ID.ToLowerInvariant(), Monitor));

        /// <inheritdoc cref="Add(string, IQuestStateMonitor)"/>
        /// <param name="QuestID"> The ID of the quest. </param>
        /// <param name="StateID"> The ID of the state. </param>
        /// <param name="Monitor"> The monitor of the quest state. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StateID, IQuestStateMonitor Monitor ) => AddStateInternal(new(GetUniqueID(QuestID, StateID), Monitor));

        static void AddStateInternal( MonitoredQuestState State ) {
            _States.Add(State);
            SetState(State.ID, StateState.NotStarted);
        }

        static CancellationToken SceneChangeToken =>
            Scenes.SceneChangeToken
            #if UNITY_EDITOR
            .Or(Editor_ApplicationExitedToken)
            #endif
            ;

        #if UNITY_EDITOR
        static CancellationTokenSource? _Editor_ApplicationExitedTokenSource = null;
        static CancellationToken Editor_ApplicationExitedToken {
            get {
                if (_Editor_ApplicationExitedTokenSource == null) {
                    _Editor_ApplicationExitedTokenSource = new();
                    void Callback() {
                        _Editor_ApplicationExitedTokenSource?.Dispose();
                        _Editor_ApplicationExitedTokenSource = null;

                        UnityEditor.EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
                    }
                    _Editor_ApplicationExitedTokenSource.Token.Register(Callback);
                    UnityEditor.EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
                }
                return _Editor_ApplicationExitedTokenSource.Token;
            }
        }
        static void OnEditorPlayModeStateChanged( UnityEditor.PlayModeStateChange State ) {
            if (State == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                _Editor_ApplicationExitedTokenSource?.Cancel();
            }
        }
        #endif

        static void PerformStateImmediate( string ID ) => PerformStateImmediate(Find(ID));
        static void PerformStateImmediate( MonitoredQuestState State ) {
            if (Scenes.IsLoadingScreen) {
                Debug.LogWarning($"Performance of quest state {State.ID} is invalid at this time. Can't perform quest states while loading.");
                return;
            }

            if (_PerformedOnSceneChanged) {
                // Debug.Log($"Performing quest state {State.ID} immediately, out-of-loop.");
                PerformState(State, InLoop: false, SceneChangeToken).Forget(Debug.LogException);
            }
        }

        static MonitoredQuestState Find( string ID ) {
            foreach (MonitoredQuestState State in _States) {
                if (State.ID == ID) { return State; }
            }

            throw new KeyNotFoundException($"No quest state with ID {ID} was found.");
        }

        static bool _PerformedOnSceneChanged = false;

        /// <summary> Performs all states that are not yet performed. </summary>
        public static void PerformPending() => PerformPendingInternal();

        static void PerformPendingInternal() {
            // Debug.Log("Performing quest states.");
            CancellationToken Token   = SceneChangeToken;
            List<UniTask>     ToAwait = new(_States.Count);
            foreach (MonitoredQuestState State in _States) {
                if (State.Performed is not PerformanceType.NotPerformed) {
                    // Debug.Log($"Quest state {State.ID} was already performed.");
                    continue;
                }

                if (State.State == StateState.NotStarted) {
                    // Debug.Log($"Quest state {State.ID} was not started.");
                    continue;
                }

                async UniTask ReEvaluate( QuestStateResult Dynamic, CancellationToken Token ) {
                    if (Token.IsCancellationRequested || Dynamic.IsT0) { return; }
                    await UniTask.Yield(); // Wait for the next frame as a bare-minimum.
                    await Dynamic.AsT1.Delay(Token);
                    if (Token.IsCancellationRequested) { return; }
                    await PerformState(State, InLoop: true, Token).ContinueWith(ReEvaluate, Token);
                }
                // Debug.Log($"Will perform quest state {State.ID}.");
                ToAwait.Add(PerformState(State, InLoop: true, Token).ContinueWith(ReEvaluate, Token));
            }

            // Debug.Log($"Performing {ToAwait.Count} quest states.");
            UniTask.WhenAll(ToAwait)
                .ContinueWith(
                    () => {
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (Token.IsCancellationRequested) {
                            Debug.Log("Some quest states were not performed because the scene changed and/or the application exited.");
                        }/* else {
                            Debug.Log("All quest states performed.");
                        }*/
                    }
                )
                .Forget(Debug.LogException);
        }

        void Update() {
            if (!_PerformedOnSceneChanged) {
                _PerformedOnSceneChanged = true;
                PerformPendingInternal();
            }
        }

        public enum StateState {
            NotStarted,
            Skip,
            UseAndFinalise,
            FinaliseOnly
        }

        static async UniTask<QuestStateResult> PerformState( MonitoredQuestState State, bool InLoop, CancellationToken Token ) {
            if (Token.IsCancellationRequested) { return QuestStateResult.Skip; }

            State.Performed = InLoop ? PerformanceType.PerformedInLoop : PerformanceType.PerformedOutOfLoop;
            StateState S = State.State;
            // Debug.Log($"Attempting to perform state {State.ID} (state is {S}) {(InLoop ? "in" : "out of")} loop.");
            if (S is StateState.NotStarted or StateState.Skip) {
                // Debug.Log($"Skipping quest state {State.ID} (state is {S}).");
                return QuestStateResult.Skip;
            }

            QuestStateResult Result = State.Monitor.Prerequisite();
            if (Result.IsT0) {
                if (!UpdateStateFromResultConstant(State, Result.AsT0)) {
                    // Debug.Log($"Skipping quest state {State.ID} (prerequisite not met; got result {Result}).");
                    return Result;
                }
            } else {
                return Result;
            }

            if (S is StateState.UseAndFinalise) {
                // Debug.Log($"Performing quest state execution {State.ID}.");
                Result = await State.Monitor.Perform(Token);
                if (Result.IsT0) {
                    if (!UpdateStateFromResultConstant(State, Result.AsT0)) {
                        Debug.Log($"Skipping quest state {State.ID} (perform failed; got result {Result}).");
                        return Result;
                    }
                } else {
                    return Result;
                }

                // Debug.Log($"Quest state {State.ID} performed.");
                State.State = StateState.FinaliseOnly;
            }

            if (S is StateState.UseAndFinalise or StateState.FinaliseOnly) {
                // Debug.Log($"Finalising quest state {State.ID}.");
                State.Monitor.Finalise();
            }/* else {
                Debug.Log($"Skipping finalisation of quest state {State.ID}.");
            }*/
            return QuestStateResult.OK;
        }

        static bool UpdateStateFromResultConstant( MonitoredQuestState State, QuestStateResultConstant Result ) {
            switch (Result) {
                case QuestStateResultConstant.OK:
                    break;
                case QuestStateResultConstant.Skip:
                    State.State = StateState.Skip;
                    return false;
                case QuestStateResultConstant.RequiresSceneChange:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        /// <summary> Gets the unique ID of a quest stage. </summary>
        /// <param name="QuestID"> The ID of the quest. </param>
        /// <param name="StageID"> The ID of the stage. </param>
        /// <returns> The unique ID of the quest stage. </returns>
        [Pure, MustUseReturnValue]
        [return: LocalizationRequired(false)]
        public static string GetUniqueID( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StageID ) => SaveData.GetName(QuestID, StageID);

        /// <summary> Starts a quest stage. </summary>
        /// <param name="QuestID"> The ID of the quest. </param>
        /// <param name="StageID"> The ID of the stage. </param>
        public static void StartStage( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StageID ) {
            // Debug.Log($"Starting quest stage {StageID} of quest {QuestID}.");
            string ID = GetUniqueID(QuestID, StageID);
            SetState(ID, StateState.UseAndFinalise);
            PerformPendingInternal();
        }

        static bool TryGetState( string ID, [NotNullWhen(true)] out MonitoredQuestState? State ) {
            foreach (MonitoredQuestState S in _States) {
                if (string.Equals(S.ID, ID, StringComparison.OrdinalIgnoreCase)) {
                    State = S;
                    return true;
                }
            }

            State = default;
            return false;
        }

        static void SetState( string ID, StateState State ) {
            if (!TryGetState(ID, out MonitoredQuestState? S)) {
                throw new KeyNotFoundException($"No quest state with ID {ID} was found.");
            }

            S.State = State;
        }
    }
}
