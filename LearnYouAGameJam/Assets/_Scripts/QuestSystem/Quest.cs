using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using LYGJ.Common.Enums;
using LYGJ.DialogueSystem;
using LYGJ.EntitySystem;
using LYGJ.EntitySystem.EnemyManagement;
using LYGJ.EntitySystem.NPCSystem;
using LYGJ.Interactables;
using LYGJ.InventoryManagement;
using LYGJ.SceneManagement;
using OneOf;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;
using QuestStage = System.Func<System.Threading.CancellationToken, Cysharp.Threading.Tasks.UniTask>;

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

        [NonSerialized] protected readonly Dictionary<string, QuestStage> Stages = new();

        [NonSerialized] protected bool Constructed;

        [ExecuteOnReload] void Cleanup() {
            Stages.Clear();
            Constructed = false;
        }

        /// <summary> Constructs the quest. </summary>
        public void Construct() {
            if (Constructed) {
                Debug.LogWarning($"Quest {Key} has already been constructed.", this);
                return;
            }
            foreach ((string ID, QuestStage Stage) in ConstructStages()) {
                Stages.Add(ID, Stage);
            }
            Constructed = true;
        }

        /// <summary> Constructs the stages of the quest. </summary>
        /// <returns> The stages of the quest. </returns>
        protected abstract IEnumerable<(string ID, QuestStage Stage)> ConstructStages();

        /// <summary> Begins the quest. </summary>
        /// <param name="Token"> The cancellation token which can be used to cancel the quest (such as when the scene changes). </param>
        public void StartQuest( CancellationToken Token ) {
            if (!Constructed) {
                Debug.LogWarning($"Quest {Key} has not been constructed. Constructing now.", this);
                Construct();
                return;
            }
            Quests.SetCompletion(Key, Completion.Started);
            if (Stages.Count == 0) {
                Debug.LogWarning($"Quest {Key} has no stages.", this);
                Quests.SetCompletion(Key, Completion.Completed);
                return;
            }
            string FirstStage = Stages.Keys.First();
            StartStage(FirstStage, Token);
        }

        /// <summary> Starts the given stage of the quest. </summary>
        /// <param name="Stage"> The stage to start. </param>
        /// <param name="Token"> The cancellation token which can be used to cancel the quest (such as when the scene changes). </param>
        public void StartStage( string Stage, CancellationToken Token ) {
            if (!Stages.ContainsKey(Stage)) {
                Debug.LogWarning($"Quest {Key} does not have a stage with ID {Stage}.", this);
                return;
            }

            Quests.SetCompletion(Key, Stage, Completion.Started);

            void MarkDone() {
                if (Quests.GetCompletion(Key, Stage) is Completion.Started) {
                    Quests.SetCompletion(Key, Stage, Completion.Completed);
                }
            }
            Stages[Stage](Token).ContinueWith(MarkDone).Forget(Debug.LogException);
        }

        /// <inheritdoc cref="Quests.Complete(string)"/>
        protected void Complete() => Quests.Complete(Key);

        /// <inheritdoc cref="Quests.Fail(string)"/>
        [Obsolete]
        protected void Fail() => Quests.Fail(Key);

        /// <inheritdoc cref="Quests.CompleteStage(string, string)"/>
        protected void CompleteStage( string Stage ) => Quests.CompleteStage(Key, Stage);

        /// <inheritdoc cref="Quests.FailStage(string, string)"/>
        [Obsolete]
        protected void FailStage( string Stage ) => Quests.FailStage(Key, Stage);

        #region Template Stages

        protected sealed class TalkToNPCCondition : OneOfBase<QuestStage, bool> {

            /// <inheritdoc />
            TalkToNPCCondition( OneOf<QuestStage, bool> Input ) : base(Input) { }

            public static implicit operator TalkToNPCCondition( QuestStage Stage ) => new(Stage);

            public static implicit operator TalkToNPCCondition( Func<UniTask> Stage ) => new((QuestStage)(QuestExtensions.QuestStageProxy)Stage);

            public static implicit operator TalkToNPCCondition( Action<CancellationToken> Stage ) => new((QuestStage)(QuestExtensions.QuestStageProxy)Stage);

            public static implicit operator TalkToNPCCondition( Action Stage ) => new((QuestStage)(QuestExtensions.QuestStageProxy)Stage);

            public static implicit operator TalkToNPCCondition( bool Continue ) => new(Continue);
        }

        protected static QuestStage TalkToNPC( string Key, DialogueChain Chain, DialogueChain? After = null, Func<int, TalkToNPCCondition>? Condition = null ) {
            Debug.Assert(Chain != null, "No dialogue chain provided.");
            TalkToNPCCondition DefaultCondition( int Result ) => Result switch {
                0 => true,
                1 => false, // Can't continue until user selects correct option.
                _ => throw new($"Unexpected result {Result} from dialogue chain {(Chain == null ? "<null>" : Chain.name)}.")
            };
            Condition ??= DefaultCondition;

            async UniTask Perform( CancellationToken Token ) {
                NPCBase NPC = NPCs.Get(Key);
                if (!NPC.TryGetDialogueGiver(out NPCDialogueGiver? Giver, Create: true, EvenIfInUse: false)) {
                    throw new($"NPC {Key} is already awaiting some other dialogue.");
                }

                while (true) {
                    Giver.Dialogue = Chain;
                    int Result = await Giver.WaitForInteraction(Cleanup: false, Token);
                    if (Condition is not null) {
                        TalkToNPCCondition ConditionResult = Condition(Result);
                        if (ConditionResult.IsT0) {
                            QuestStage Tk = ConditionResult.AsT0;
                            await Tk(Token);
                            break;
                        }

                        Debug.Assert(ConditionResult.IsT1);
                        bool B = ConditionResult.AsT1;
                        if (B) {
                            break;
                        }
                    }
                }

                if (After != null) {
                    Giver.Dialogue = After;
                } else {
                    Destroy(Giver);
                }
            }

            return Perform;
        }

        protected enum InteractionTiming {
            StartInteraction,
            EndInteraction
        }
        protected static QuestStage InteractWith( string Key, InteractionTiming Timing = InteractionTiming.EndInteraction ) {
            UniTask Perform( CancellationToken Token ) {
                InteractableObjectBase Interactable = Objects.Get<InteractableObjectBase>(Key);
                return Timing == InteractionTiming.StartInteraction
                    ? Interactable.WaitForInteractionInitiation(Token)
                    : Interactable.WaitForInteraction(Token);
            }

            return Perform;
        }

        protected static QuestStage EnterTriggerZone( string Key ) {
            UniTask Perform( CancellationToken Token ) {
                TriggerZone Zone = Objects.Get<TriggerZone>(Key);
                return Zone.WaitForEntry(Token);
            }

            return Perform;
        }
        protected static QuestStage ExitTriggerZone( string Key ) {
            UniTask Perform( CancellationToken Token ) {
                TriggerZone Zone = Objects.Get<TriggerZone>(Key);
                return Zone.WaitForExit(Token);
            }

            return Perform;
        }

        protected static QuestStage AchieveItemAmount( ItemInstance Item, NumericComparison Comparison = NumericComparison.GreaterThanOrEqual ) {
            UniTask Perform(CancellationToken Token) => Inventory.WaitForAmount(Item, Comparison, Token);
            return Perform;
        }

        protected static QuestStage AchieveItemAmount( Item Item, uint Amount, NumericComparison Comparison = NumericComparison.GreaterThanOrEqual ) {
            UniTask Perform(CancellationToken Token) => Inventory.WaitForAmount(Item, Amount, Comparison, Token);
            return Perform;
        }

        protected static QuestStage AchieveRecipe( Recipe Recipe ) {
            UniTask Perform(CancellationToken Token) => Inventory.WaitForRecipe(Recipe, Token);
            return Perform;
        }

        protected static QuestStage KillEnemy( EnemyType Type, uint Amount = 1u ) {
            UniTask Perform( CancellationToken Token ) => Enemies.WaitForKills(Type, Amount, Token);
            return Perform;
        }

        #endregion
    }

    public static class QuestExtensions {
        public sealed class QuestStageProxy : OneOfBase<QuestStage, Func<UniTask>, Action<CancellationToken>, Action> {

            /// <inheritdoc />
            QuestStageProxy( OneOf<QuestStage, Func<UniTask>, Action<CancellationToken>, Action> Input ) : base(Input) { }

            public static implicit operator QuestStageProxy( QuestStage                Stage ) => new(Stage);
            public static implicit operator QuestStageProxy( Func<UniTask>             Stage ) => new(Stage);
            public static implicit operator QuestStageProxy( Action<CancellationToken> Stage ) => new(Stage);
            public static implicit operator QuestStageProxy( Action                    Stage ) => new(Stage);

            public static implicit operator QuestStage( QuestStageProxy Proxy ) => Proxy.Perform;

            async UniTask Perform( CancellationToken Token ) {
                if (TryPickT0(out QuestStage T0, out OneOf<Func<UniTask>, Action<CancellationToken>, Action> R)) {
                    await T0(Token);
                } else if (R.TryPickT0(out Func<UniTask> T1, out OneOf<Action<CancellationToken>, Action> R1)) {
                    await T1();
                } else if (R1.TryPickT0(out Action<CancellationToken> T2, out Action T3)) {
                    T2(Token);
                } else {
                    T3();
                }
            }
        }

        sealed class QuestStageConcat {
            readonly QuestStage[] _Stages;

            public QuestStageConcat( params QuestStage[] Stages ) => _Stages = Stages;

            public static implicit operator QuestStage( QuestStageConcat Concat ) => Concat.Perform;

            async UniTask Perform( CancellationToken Token ) {
                foreach (QuestStage Stage in _Stages) {
                    await Stage(Token);
                }
            }
        }

        /// <summary> Concatenates another stage to the end of the given stage. </summary>
        /// <param name="A"> The first stage. </param>
        /// <param name="B"> The second stage. </param>
        /// <returns> A stage which performs the first stage, then the second stage. </returns>
        public static QuestStage Then( this QuestStageProxy A, QuestStageProxy B ) => new QuestStageConcat(A, B);

        /// <inheritdoc cref="Then(QuestStageProxy, QuestStageProxy)"/>
        public static QuestStage Then( this QuestStage A, QuestStage B_T0 ) => new QuestStageConcat(A, B_T0);

        /// <inheritdoc cref="Then(QuestStageProxy, QuestStageProxy)"/>
        public static QuestStage Then( this QuestStage A, Func<UniTask> B_T1 ) => new QuestStageConcat(A, (QuestStageProxy)B_T1);

        /// <inheritdoc cref="Then(QuestStageProxy, QuestStageProxy)"/>
        public static QuestStage Then( this QuestStage A, Action<CancellationToken> B_T2 ) => new QuestStageConcat(A, (QuestStageProxy)B_T2);

        /// <inheritdoc cref="Then(QuestStageProxy, QuestStageProxy)"/>
        public static QuestStage Then( this QuestStage A, Action B_T3 ) => new QuestStageConcat(A, (QuestStageProxy)B_T3);

        /// <inheritdoc cref="Then(QuestStageProxy, QuestStageProxy)"/>
        public static QuestStage Then( this QuestStage A, QuestStageProxy B ) => new QuestStageConcat(A, B);
    }
}
