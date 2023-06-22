using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cinemachine;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ {
    public sealed class Cameras : SingletonMB<Cameras> {
        const int
            _ActivePriority   = 20,
            _InactivePriority = 0;

        CinemachineVirtualCameraBase? _CurrentCamera = null;

        /// <summary> Gets or sets the current camera. </summary>
        public CinemachineVirtualCameraBase? CurrentCamera {
            get => _CurrentCamera;
            set {
                if (_CurrentCamera != null) {
                    _CurrentCamera.Priority = _InactivePriority;
                }

                if (value != null) {
                    value.Priority = _ActivePriority;
                } else {
                    value              = _Fallback;
                    _Fallback.Priority = _ActivePriority;
                }

                _CurrentCamera = value;
            }
        }

        /// <inheritdoc cref="CurrentCamera"/>
        public static CinemachineVirtualCameraBase? Current {
            get => Instance.CurrentCamera;
            set => Instance.CurrentCamera = value;
        }

        [SerializeField, Tooltip("The camera to use by default, on scene start."), Required, SceneObjectsOnly]
        CinemachineVirtualCameraBase _Default = null!;

        /// <summary> Gets the default camera, used on scene start. </summary>
        public static CinemachineVirtualCameraBase SceneDefault => Instance._Default;

        [SerializeField, Tooltip("The fallback camera to use, when none is set."), Required, SceneObjectsOnly]
        CinemachineVirtualCameraBase _Fallback = null!;

        /// <summary> Gets or sets the fallback camera, used when none is set. </summary>
        public static CinemachineVirtualCameraBase Fallback {
            get => Instance._Fallback;
            set => Instance._Fallback = value;
        }

        protected override void Awake() {
            base.Awake();
            foreach (CinemachineVirtualCameraBase Camera in GetComponentsInChildren<CinemachineVirtualCameraBase>()) {
                Camera.Priority = _InactivePriority;
            }
            _Default.Priority = _ActivePriority;
            _CurrentCamera          = _Default;
        }

        readonly List<CinemachineVirtualCameraBase> _CameraStack = new();

        /// <summary> Pushes a camera onto the stack, making it active. </summary>
        /// <param name="Camera"> The camera to push. </param>
        public void PushOntoStack( CinemachineVirtualCameraBase Camera ) {
            if (_CameraStack.Count > 0) {
                _CameraStack[^1].Priority = _InactivePriority;
            }

            Camera.Priority = _ActivePriority;
            _CameraStack.Add(Camera);
            _CurrentCamera = Camera;
        }

        /// <inheritdoc cref="PushOntoStack"/>
        public static void Push( CinemachineVirtualCameraBase Camera ) => Instance.PushOntoStack(Camera);

        /// <summary> Pops a camera from the stack, making the previous one active. </summary>
        public void PopFromStack() {
            switch (_CameraStack.Count) {
                case 0:
                    CurrentCamera = null;
                    break;
                case 1:
                    CurrentCamera = _CameraStack[0];
                    _CameraStack.Clear();
                    break;
                default:
                    CurrentCamera = _CameraStack[^1];
                    _CameraStack.RemoveAt(_CameraStack.Count - 1);
                    break;
            }
        }

        /// <summary> Pops a camera from the stack, making the previous one active. </summary>
        /// <param name="Camera"> The camera to pop. </param>
        public void PopFromStack( CinemachineVirtualCameraBase Camera ) {
            // This works on a fake stack. To prevent visual transition bugs, a new camera must first be pushed before the previous popped. Therefore this method checks the topmost, and second topmost cameras, when removing.
            switch (_CameraStack.Count) {
                case 0:
                    CurrentCamera = null;
                    Debug.LogWarning("Nothing to pop.", this);
                    break;
                case 1:
                    if (_CameraStack[0] == Camera) {
                        CurrentCamera = null;
                        _CameraStack.Clear();
                        // Debug.Log("Popped the last camera from the stack.", this);
                    } else {
                        Debug.LogWarning($"The camera to pop ({Camera.name}#{Camera.GetInstanceID()}) is not on the stack. (Stack: '{_CameraStack[0].name}#{_CameraStack[0].GetInstanceID()}')", this);
                    }
                    break;
                default:
                    int Idx = _CameraStack.FindLastIndex(C => C == Camera);
                    if (Idx == -1) {
                        Debug.LogWarning($"The camera to pop ({Camera.name}#{Camera.GetInstanceID()}) is not on the stack. (Stack: '{string.Join("', '", _CameraStack.Select(C => $"{C.name}#{C.GetInstanceID()}"))}')", this);
                        break;
                    }
                    int Ln = _CameraStack.Count;
                    // Edge-case Idx is at end of list. In this case, set CurrentCamera to the second topmost camera.
                    if (Idx == Ln - 1) {
                        CurrentCamera = _CameraStack[^2];
                    }
                    _CameraStack.RemoveAt(Idx);
                    break;
            }
        }

        /// <inheritdoc cref="PopFromStack()"/>
        public static void Pop() => Instance.PopFromStack();

        /// <inheritdoc cref="PopFromStack(CinemachineVirtualCameraBase)"/>
        public static void Pop( CinemachineVirtualCameraBase Camera ) => Instance.PopFromStack(Camera);

        /// <summary> Gets the transform of the current camera. </summary>
        /// <returns> The transform of the current camera. </returns>
        public static Transform Transform => Instance.CurrentCamera != null ? Instance.CurrentCamera.transform : null!;

        /// <summary> Gets the forward direction of the current camera. </summary>
        /// <returns> The forward direction of the current camera. </returns>
        public static Vector3 Forward => Instance.CurrentCamera != null ? Instance.CurrentCamera.transform.forward : Vector3.forward;

        /// <summary> Gets the right direction of the current camera. </summary>
        /// <returns> The right direction of the current camera. </returns>
        public static Vector3 Right => Instance.CurrentCamera != null ? Instance.CurrentCamera.transform.right : Vector3.right;

        /// <summary> Gets the up direction of the current camera. </summary>
        /// <returns> The up direction of the current camera. </returns>
        public static Vector3 Up => Instance.CurrentCamera != null ? Instance.CurrentCamera.transform.up : Vector3.up;
    }

    public static class CameraExtensions {
        /// <inheritdoc cref="Cameras.Push"/>
        public static void Push( this CinemachineVirtualCameraBase Camera ) => Cameras.Push(Camera);

        /// <inheritdoc cref="Cameras.Pop(CinemachineVirtualCameraBase)"/>
        public static void Pop( this CinemachineVirtualCameraBase Camera ) => Cameras.Pop(Camera);

        /// <inheritdoc cref="Cameras.Current"/>
        public static void SetCurrent( this CinemachineVirtualCameraBase Camera ) => Cameras.Current = Camera;
    }
}
