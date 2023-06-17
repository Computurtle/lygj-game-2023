using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Cinemachine;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class HeroCameraManager : SingletonMB<HeroCameraManager> {

        [Title("First-Person")]
        [SerializeField, Tooltip("The camera."), Required, ChildGameObjectsOnly, LabelText("Camera")]
        CinemachineVirtualCameraBase? _FirstPersonCamera;

        [Title("Third-Person")]
        [SerializeField, Tooltip("The camera."), Required, ChildGameObjectsOnly, LabelText("Camera")]
        CinemachineVirtualCameraBase? _ThirdPersonCamera;

        [Space]
        [SerializeField, Tooltip("The transform to copy pitch from."), ChildGameObjectsOnly, LabelText("Pitch (Source)")]
        Transform? _PitchSource;
        [SerializeField, Tooltip("The transform to apply pitch to."), ChildGameObjectsOnly, Required, LabelText("Pitch (Target)"), HideIf("@_PitchSource == null")]
        Transform _PitchTarget;

        [Space]
        [SerializeField, Tooltip("The transform to copy yaw from."), ChildGameObjectsOnly, LabelText("Yaw (Source)")]
        Transform? _YawSource;
        [SerializeField, Tooltip("The transform to apply yaw to."), ChildGameObjectsOnly, Required, LabelText("Yaw (Target)"), HideIf("@_YawSource == null")]
        Transform _YawTarget;

        [Title("Runtime"), SerializeField, Tooltip("The mode to start in."), LabelText("Mode"), HideInPlayMode]
        CameraMode _StartMode = CameraMode.ThirdPerson;

        [Title("Runtime"), ShowInInspector, Tooltip("The current camera mode."), LabelText("Mode"), ReadOnly, HideInEditorMode]
        CameraMode _CurrentMode;

        #if UNITY_EDITOR
        void Reset() {
            CinemachineVirtualCameraBase[] Cams = GetComponentsInChildren<CinemachineVirtualCameraBase>();
            foreach (CinemachineVirtualCameraBase Cam in Cams) {
                if (Cam.name.Contains("First", StringComparison.OrdinalIgnoreCase)) {
                    _FirstPersonCamera = Cam;
                } else if (Cam.name.Contains("Third", StringComparison.OrdinalIgnoreCase)) {
                    _ThirdPersonCamera = Cam;
                }
            }
        }
        #endif

        [Pure] static CameraMode Swap( CameraMode Mode ) => Mode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson;

        void Start() {
            PlayerInput.SwitchCamera.Released += OnSwitchCameraReleased;
            _CurrentMode = Swap(_StartMode);
            ChangeMode(_StartMode);
        }

        /// <summary> Switches the camera mode. </summary>
        /// <param name="Mode"> The mode to switch to. </param>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if the mode is not a valid camera mode. </exception>
        public void ChangeMode( CameraMode Mode ) {
            if (_CurrentMode == Mode) { return; }
            _CurrentMode = Mode;
            switch (Mode) {
                case CameraMode.FirstPerson:
                    SwapTo(First: true);
                    break;
                case CameraMode.ThirdPerson:
                    SwapTo(First: false);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null);
            }
        }

        void SwapTo( bool First ) {
            Player.SetModelVisible(ModelPriority.Gameplay, !First);

            CinemachineVirtualCameraBase? Camera = First ? _FirstPersonCamera : _ThirdPersonCamera;
            if (Camera != null) {
                CinemachineVirtualCameraBase? New = First ? _FirstPersonCamera : _ThirdPersonCamera;
                Cameras.Current = New;
                if (New != null) {
                    Cameras.Fallback = New;
                }
            } else {
                Debug.LogWarning($"{(First ? "First" : "Third")}-person camera is null.", this);
            }
        }

        /// <summary> Swaps to the alternate camera mode. </summary>
        public void SwapMode() => ChangeMode(Swap(_CurrentMode));

        /// <inheritdoc cref="ChangeMode(CameraMode)"/>
        public static void SwapTo( CameraMode Mode ) => Instance.ChangeMode(Mode);

        /// <inheritdoc cref="SwapMode"/>
        public static void Swap() => Instance.SwapMode();

        void OnSwitchCameraReleased( float HoldTime ) => SwapMode();

        void Update() {
            if (_PitchSource != null) { _PitchTarget.localRotation = _PitchSource.localRotation; }
            if (_YawSource   != null) { _YawTarget.localRotation   = _YawSource.localRotation; }
        }

    }

    public enum CameraMode {
        FirstPerson = 1,
        ThirdPerson = 3
    }
}
