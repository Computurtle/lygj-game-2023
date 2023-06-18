using LYGJ.SceneManagement;
using UnityEngine;

namespace LYGJ.Content.AridSprings {
    public sealed class Grove : TriggerZone {

        public const string ID = "arid-springs.grove";

        #region Overrides of Entity

        /// <inheritdoc />
        public override string Key => ID;

        #endregion

    }
}
