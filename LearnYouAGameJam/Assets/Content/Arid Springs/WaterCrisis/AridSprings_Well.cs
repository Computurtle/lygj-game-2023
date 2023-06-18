using Cysharp.Threading.Tasks;
using LYGJ.EntitySystem.NPCSystem;
using LYGJ.EntitySystem.PlayerManagement;
using LYGJ.Interactables;
using UnityEngine;

namespace LYGJ.Content.AridSprings {
    public sealed class Well : InteractableObjectBase {

        public const string ID = "arid-springs.well";

        [SerializeField, Tooltip("How many units away from the player the handyman should be when interacting with the well")]
        float _HandymanDistance = 2f;

        // ReSharper disable once InconsistentNaming
        static string _Handyman => WaterCrisis._Handyman;

        #region Overrides of Entity

        /// <inheritdoc />
        public override string Key => ID;

        #endregion

        #region Overrides of InteractableObjectBase

        /// <inheritdoc />
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        protected override bool CanInteract => _Interactable;

        /// <inheritdoc />
        protected override async UniTask Interact() {
            NPCBase Handyman = NPCs.Get(_Handyman);
            // await Handyman.MoveTowards(Player.Position);
            Vector3 Dest = Player.Position;
            Vector3 Orig = Handyman.Position;

            // Update Dest to be 'n' unit away from the player in the direction of the handyman
            Dest = Vector3.MoveTowards(Dest, Orig, _HandymanDistance);

            await Handyman.MoveTowards(Dest);
        }

        #endregion

        bool _Interactable = false;

        public void SetInteractability( bool B ) => _Interactable = B;
    }
}
