using Cysharp.Threading.Tasks;
using LYGJ.EntitySystem.NPCSystem;
using LYGJ.EntitySystem.PlayerManagement;
using LYGJ.Interactables;

namespace LYGJ.Content.AridSprings {
    public sealed class Well : InteractableObjectBase {

        public const string ID = "arid-springs.well";

        // ReSharper disable once InconsistentNaming
        static string _Handyman => WaterCrisis._Handyman;

        #region Overrides of Entity

        /// <inheritdoc />
        public override string Key => ID;

        #endregion

        #region Overrides of InteractableObjectBase

        /// <inheritdoc />
        protected override async UniTask Interact() {
            NPCBase Handyman = NPCs.Get(_Handyman);
            await Handyman.MoveTowards(Player.Position);
        }

        #endregion

    }
}
