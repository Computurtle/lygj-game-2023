using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.DialogueSystem;
using LYGJ.EntitySystem.NPCSystem;
using LYGJ.QuestSystem;
using LYGJ.SaveManagement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.Content.AridSprings {
    public sealed class WaterCrisis : Quest {

        public const string ID = "water-crisis";

        internal const string
            // ReSharper disable once InconsistentNaming
            _Handyman = "ezekial-the-handyman";

        const string
            _Mayor   = "mayor-layla",
            _Barkeep = "barkeep-bael",

            _Well = Well.ID,

            _S000 = "speak-to-mayor",
            _S100 = "speak-to-barkeep",
            _S101 = "enter-the-grove",
            _S102 = "kill-bandits",
            _S103 = "return-to-barkeep",
            _S104 = "accepted-evil-offer",
            _S200 = "find-the-well",
            _S201 = "find-parts",
            _S202 = "return-to-handyman",
            _S203 = "repaired-the-well",
            _S300 = "return-to-mayor",

            _OutcomeKey = "repaired-well";

        [AssetsOnly, Required, AssetSelector]
        public DialogueChain
            S000_Mayor                  = null!,
            S000_Mayor_After            = null!,
            S100_Barkeep_Intro          = null!,
            S100_Barkeep_Intro_After    = null!,
            S103_Barkeep_DoneTask       = null!,
            S104_Barkeep_ChoseEvilOffer = null!,
            S200_Handyman_Intro         = null!,
            S201_Handyman_AskHelp       = null!,
            S202_Handyman_FoundParts    = null!,
            S203_Handyman_DoneTask      = null!,
            S300_Mayor_Good             = null!,
            S300_Mayor_Evil             = null!;

        public enum Outcome {
            NotCompleted,
            RepairedWell,
            AcceptedEvilOffer,
        }

        static Outcome ThisSave_Outcome {
            get => Saves.Current.GetOrDefault(_OutcomeKey, Outcome.NotCompleted);
            set => Saves.Current.Set(_OutcomeKey, value);
        }

        /// <summary> Whether the player repaired the well or not. </summary>
        public static Outcome QuestOutcome {
            get {
                Outcome O = ThisSave_Outcome;
                if (O is Outcome.NotCompleted) {
                    throw new InvalidOperationException("Quest not completed yet.");
                }
                return O;
            }
        }

        #region Overrides of Quest

        /// <inheritdoc />
        public override string Key => ID;
        /// <inheritdoc />
        protected override IEnumerable<(string ID, Func<CancellationToken, UniTask> Stage)> ConstructStages() {
            yield return (_S000, TalkToNPC(_Mayor, S000_Mayor, S000_Mayor_After)
                .Then(
                    Token => {
                        CompleteStage(_S000);
                        StartStage(_S100, Token);
                        StartStage(_S200, Token);
                    }
                ));

            yield return (_S100, TalkToNPC(_Barkeep, S100_Barkeep_Intro, S100_Barkeep_Intro_After)
                .Then(
                    Token => {
                        CompleteStage(_S100);
                        StartStage(_S101, Token);
                    }
                ));
            yield return (_S101, EnterTriggerZone(Grove.ID)
                .Then(
                    Token => {
                        CompleteStage(_S101);
                        StartStage(_S102, Token);
                    }
                ));
            // TODO: _S102 -- detect when player kills bandits
            yield return (_S103, TalkToNPC(_Barkeep, S103_Barkeep_DoneTask, Condition: C => C switch {
                    1 => (Action)(() => {
                        NPCBase Barkeep = NPCs.Get(_Barkeep);
                        Barkeep.SetAmbientDialogue(S104_Barkeep_ChoseEvilOffer);
                        CompleteStage(_S104);
                        ThisSave_Outcome = Outcome.AcceptedEvilOffer;
                    }),
                    -1 => false,
                    _ => throw new ArgumentOutOfRangeException(nameof(C), C, null)
                })
                .Then(
                    Token => {
                        CompleteStage(_S103);
                        StartStage(_S300, Token);
                    }
                ));

            yield return (_S200, InteractWith(_Well)
                .Then(
                    async Token => {
                        int Result;
                        do {
                            if (Token.IsCancellationRequested) {
                                return;
                            }
                            Result = await S200_Handyman_Intro.Play();
                        } while (Result != 0);
                        NPCBase Handyman = NPCs.Get(_Handyman);
                        Handyman.SetAmbientDialogue(S201_Handyman_AskHelp);
                    }
                )
                .Then(
                    Token => {
                        CompleteStage(_S200);
                        StartStage(_S201, Token);
                    }
                ));
            // TODO: _S201 -- detect when player finds the parts
            yield return (_S202, TalkToNPC(_Handyman, S202_Handyman_FoundParts, S203_Handyman_DoneTask)
                .Then(
                    Token => {
                        CompleteStage(_S202);
                        CompleteStage(_S203);
                        ThisSave_Outcome = Outcome.RepairedWell;
                        StartStage(_S300, Token);
                    }
                ));

            yield return (_S300, async Token => {
                NPCBase Mayor = NPCs.Get(_Mayor);
                if (!Mayor.TryGetDialogueGiver(out NPCDialogueGiver? Giver)) {
                    throw new InvalidOperationException("Mayor dialogue giver is indisposed.");
                }

                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                Giver.Dialogue = ThisSave_Outcome switch {
                    Outcome.RepairedWell      => S300_Mayor_Good,
                    Outcome.AcceptedEvilOffer => S300_Mayor_Evil,
                    _                         => throw new ArgumentOutOfRangeException()
                };
                _ = await Giver.WaitForInteraction(true, Token);
                CompleteStage(_S300);
                Complete();
            });
        }

        #endregion

    }
}
