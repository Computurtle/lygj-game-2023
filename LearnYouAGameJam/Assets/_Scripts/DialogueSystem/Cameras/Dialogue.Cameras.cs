using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using LYGJ.Common.Datatypes;
using LYGJ.EntitySystem.NPCSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem.Cameras {
    public sealed class Dialogue_Cameras : MonoBehaviour {

        [SerializeField, Tooltip("The multi-target camera to use."), Required, SceneObjectsOnly] CinemachineVirtualCameraBase _MultiTargetCamera = null!;

        [SerializeField, Tooltip("The prefab for single-target cameras."), Required, AssetsOnly] CinemachineSingleTargetTracker _SingleTargetTrackerPrefab = null!;

        CinemachineMultiTargetTracker? _MultiTargetTracker = null;

        CinemachineMultiTargetTracker MultiTargetTracker {
            get {
                if (_MultiTargetTracker == null) {
                    _MultiTargetTracker = _MultiTargetCamera.GetComponent<CinemachineMultiTargetTracker>();
                }
                return _MultiTargetTracker;
            }
        }

        #if UNITY_EDITOR
        void Reset() {
            foreach (CinemachineVirtualCamera VCam in FindObjectsOfType<CinemachineVirtualCamera>()) {
                if (VCam.TryGetComponent<CinemachineMultiTargetTracker>(out _)) {
                    _MultiTargetCamera = VCam;
                    break;
                }
            }
        }
        #endif

        static bool TryGetComponentInChildren<T>( Transform Transform, [NotNullWhen(true)] out T? Component ) where T : Component {
            Component = Transform.GetComponentInChildren<T>();
            return Component != null;
        }

        CinemachineVirtualCameraBase GetSingleTracker( Transform NPC ) {
            CinemachineVirtualCameraBase? VCam = null;
            if (!TryGetComponentInChildren(NPC, out CinemachineSingleTargetTracker? VCamTracker)) {
                VCamTracker = Instantiate(_SingleTargetTrackerPrefab, NPC);
            }
            VCam = VCamTracker.GetComponent<CinemachineVirtualCameraBase>();
            return VCam;
        }

        public enum CameraStyle {
            /// <summary> The camera will be framed such that the current speaker is visible. </summary>
            SingleTarget = 0,
            /// <summary> The camera will be framed such that all NPCs partaking in the dialogue are visible at the same time. </summary>
            MultiTarget = 1,
        }

        [SerializeField, Tooltip("The camera style to use.")] Observable<CameraStyle> _CameraStyle = CameraStyle.SingleTarget;

        readonly ObservableCollection<Transform> _Speakers       = new();
        readonly Observable<Transform?>          _CurrentSpeaker = new();

        void Start() {
            Dialogue.Started       += Dialogue_Started;
            Dialogue.TextDisplayed += Dialogue_TextDisplayed;
            Dialogue.Ended         += Dialogue_Ended;

            _Speakers.CollectionChanged += Speakers_CollectionChanged;
            _CurrentSpeaker.Changed     += CurrentSpeaker_Changed;
            _CameraStyle.Changed        += CameraStyle_Changed;
        }

        void Dialogue_Started( DialogueChain Chain ) => ClearView();

        void Dialogue_Ended( DialogueChain Chain, int Exit ) => ClearView();

        CinemachineVirtualCameraBase? _LastPushed = null;

        void ClearView() {
            _Speakers.Clear();
            Transform? Current = _CurrentSpeaker.Value;
            _CurrentSpeaker.Value = Current;
            if (_CameraStyle == CameraStyle.MultiTarget) {
                MultiTargetTracker.Targets.Clear();
            }

            PopLast();
        }

        void PopLast() {
            if (_LastPushed != null) {
                _LastPushed.Pop();
            }

            _LastPushed = null;
        }

        void Push( CinemachineVirtualCameraBase VCam ) {
            PopLast();
            VCam.Push();
            _LastPushed = VCam;
        }

        UniTask Dialogue_TextDisplayed( DialogueObject DialogueObject, string Speaker, bool Speakerknown, string Text, CancellationToken Token ) {
            if (NPCs.TryGet(Speaker, out NPCBase? NPC)) {
                // Debug.Log($"Found NPC with name \"{Speaker}\". Setting as current speaker.", this);
                _CurrentSpeaker.Value = NPC.transform;

                if (!_Speakers.Select(S => S.name).Contains(Speaker, StringComparison.OrdinalIgnoreCase)) {
                    // Debug.Log($"Adding NPC with name \"{Speaker}\" to speakers list.", this);
                    _Speakers.Add(NPC.transform);
                }
            } else {
                Debug.LogWarning($"Could not find NPC with name \"{Speaker}\". Unable to frame camera accordingly.", this);
            }
            return Token.IsCancellationRequested ? UniTask.FromCanceled(Token) : UniTask.CompletedTask;
        }

        void Speakers_CollectionChanged( object Sender, NotifyCollectionChangedEventArgs E ) {
            if (_CameraStyle != CameraStyle.MultiTarget) { return; }

            MultiTargetTracker.Targets.Clear();
            MultiTargetTracker.Targets.AddRange(_Speakers);
            Push(_MultiTargetCamera);
        }

        void CurrentSpeaker_Changed( Transform? Value ) {
            if (_CameraStyle != CameraStyle.SingleTarget) { return; }
            if (Value == null) { return; }

            CinemachineVirtualCameraBase VCam = GetSingleTracker(Value);
            Push(VCam);
        }

        void CameraStyle_Changed( CameraStyle Value ) {
            if (!Dialogue.IsRunning) { return; }

            switch (Value) {
                case CameraStyle.SingleTarget:
                    if (_CurrentSpeaker.Value != null) {
                        Push(GetSingleTracker(_CurrentSpeaker.Value));
                    }
                    break;
                case CameraStyle.MultiTarget:
                    MultiTargetTracker.Targets.Clear();
                    MultiTargetTracker.Targets.AddRange(_Speakers);
                    Push(_MultiTargetCamera);
                    break;
            }
        }

    }
}
