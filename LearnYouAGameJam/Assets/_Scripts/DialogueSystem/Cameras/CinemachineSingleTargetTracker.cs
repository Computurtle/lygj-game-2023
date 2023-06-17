using System.Diagnostics;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.DialogueSystem.Cameras {
    [AddComponentMenu("Cinemachine/SingleTargetTracker")]
    public sealed class CinemachineSingleTargetTracker : CinemachineExtension {

        /// <summary> The currently speaking character. </summary>
        [Tooltip("The currently speaking character."), SceneObjectsOnly, ValidateInput("@$value != null", "Without a current speaker, the camera will not be able to track anything.")]
        public Transform CurrentSpeaker;

        /// <inheritdoc />
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase Vcam,
            CinemachineCore.Stage        Stage,
            ref CameraState              State,
            float                        DeltaTime
        ) {
            if (Stage == CinemachineCore.Stage.Body) {
                if (CurrentSpeaker != null) {
                    // Directly position the camera looking at the CurrentSpeaker
                    Vector3 Position = CurrentSpeaker.position;
                    Vector3 Dir      = Position - State.RawPosition;
                    Vector3 NewPos   = Position - Dir.normalized * Vcam.State.Lens.OrthographicSize;
                    State.RawPosition = NewPos;
                }
            }
        }
    }
}
