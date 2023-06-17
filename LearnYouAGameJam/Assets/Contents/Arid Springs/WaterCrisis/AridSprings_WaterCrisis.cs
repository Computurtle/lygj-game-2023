using System.Collections.Generic;
using LYGJ.QuestSystem;
using UnityEngine;

namespace LYGJ.Contents.AridSprings {
    public sealed class WaterCrisis : Quest {

        public const string ID = "water-crisis";

        #region Overrides of Quest

        /// <inheritdoc />
        public override string Key => ID;

        /// <inheritdoc />
        protected override IEnumerable<(string ID, IQuestStateMonitor Monitor)> ConstructStages() {
            yield break;
        }

        #endregion

    }
}
