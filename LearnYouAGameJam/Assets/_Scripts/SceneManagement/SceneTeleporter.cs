using System.Diagnostics;
using LYGJ.Common;
using LYGJ.Common.Attributes;
using LYGJ.EntitySystem.PlayerManagement;
using LYGJ.SceneManagement.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.SceneManagement {
    public sealed class SceneTeleporter : MonoBehaviour {
        [Space]
        [SerializeField, Tooltip("The scene to change to. Leave blank to use the current scene."), Scene, LabelText("Scene")]
        string _Scene = string.Empty;

        #if UNITY_EDITOR
        [OnValueChanged(nameof(FixMarkerName))]
        #endif
        [SerializeField, Tooltip("The marker to teleport to. Leave blank to use the current position."), TeleportMarker, LabelText("Marker")]
        string _Marker = string.Empty;

        #if UNITY_EDITOR
        void FixMarkerName() => _Marker = _Marker.ConvertNamingConvention(NamingConvention.KebabCase);
        #endif

        bool _Teleporting = false;

        /// <summary> Performs the teleportation. </summary>
        public void Teleport() {
            if (_Teleporting) {
                Debug.LogError("Teleportation already in progress.");
                return;
            }
            _Teleporting = true;

            void Callback() {
                _Teleporting = false;
                if (!string.IsNullOrEmpty(_Marker)) {
                    if (Teleports.TryGet(_Marker, out Transform? Marker)) {
                        PlayerMotor.TeleportTo(Marker);
                    } else {
                        Debug.LogError($"Marker {_Marker} not found.");
                    }
                }
            }

            if (!string.IsNullOrEmpty(_Scene)) {
                // Debug.Log($"Loading scene {_Scene}...");
                Scenes.Load(_Scene, Callback);
            } else {
                Callback();
            }
        }
    }
}
