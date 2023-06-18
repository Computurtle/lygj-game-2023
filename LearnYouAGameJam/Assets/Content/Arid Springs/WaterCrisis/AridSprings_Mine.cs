using LYGJ.SceneManagement;
using UnityEngine;

namespace LYGJ.Content.AridSprings {
    public sealed class Mine : TriggerZone {

        public const string ID = "arid-springs.mine";

        #region Overrides of Entity

        /// <inheritdoc />
        public override string Key => ID;

        #endregion

    }
}
