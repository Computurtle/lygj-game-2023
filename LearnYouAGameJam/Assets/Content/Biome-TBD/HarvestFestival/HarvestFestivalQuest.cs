using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.DialogueSystem;
using LYGJ.QuestSystem;
using UnityEngine;

namespace LYGJ.Content.HarvestFestival
{
    public class HarvestFestivalQuest : Quest
    {
        public const string ID = "harvest-festival";

        public override string Key => ID;

        const string
            _Mayor = "mayor-pippins",
            _Florist = "florist-penelope",
            _Blacksmith = "blacksmith-cedric",
            _Farmer = "farmer-eliza",
            _Witch = "witch-evangeline",

            _S000 = "speak-to-pippins",
            _S001 = "speak-to-penelope",
            _S002 = "speak-to-cedric",
            _S100 = "speak-to-witch",
            _S200 = "speak-to-eliza";

        [SerializeField]
        private DialogueChain _MayorDialogue, _FloristDialogue, _BlacksmithDialogue, _WitchDialogue, _FarmerDialogue;

        protected override IEnumerable<(string ID, Func<CancellationToken, UniTask> Stage)> ConstructStages()
        {
            yield return (_S000, EnterTriggerZone(MayorTriggerZone.ID)
                .Then(
                    async Token =>
                    {
                        int result = await _MayorDialogue.Play();
                        CompleteStage(_S000);
                        StartStage(_S001, Token);
                        StartStage(_S002, Token);
                        StartStage(_S100, Token);
                    }
                )
            );

            yield return (_S001, TalkToNPC(_Florist, _FloristDialogue)
                .Then(
                    Token =>
                    {
                        CompleteStage(_S001);
                    }
                )
            );

            yield return (_S002, TalkToNPC(_Blacksmith, _BlacksmithDialogue)
                .Then(
                    Token =>
                    {
                        CompleteStage(_S002);
                    }
                )
            );

            yield return (_S100, TalkToNPC(_Witch, _WitchDialogue)
                .Then(
                    Token =>
                    {
                        CompleteStage(_S100);
                        StartStage(_S200, Token);
                    }
                )
            );

            yield return (_S200, TalkToNPC(_Farmer, _FarmerDialogue)
                .Then(
                    Token =>
                    {
                        CompleteStage(_S200);
                    }
                )
            );
        }
    }
}
