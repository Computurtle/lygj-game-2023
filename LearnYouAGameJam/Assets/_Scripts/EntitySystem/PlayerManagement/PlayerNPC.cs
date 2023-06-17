using System.Diagnostics;
using LYGJ.EntitySystem.NPCSystem;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerNPC : NPCBase {

        #region Overrides of NPCBase

        /// <inheritdoc />
        public override string Key => "player";

        #endregion

    }
}
