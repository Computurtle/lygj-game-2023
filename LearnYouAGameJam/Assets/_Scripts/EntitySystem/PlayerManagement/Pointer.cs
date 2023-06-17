using System;
using System.Diagnostics;
using LYGJ.Common;
using LYGJ.Common.Datatypes.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class Pointer : SingletonMB<Pointer> {

        [SerializeField, Tooltip("The lock mode to use when hidden.")] CursorLockMode _LockMode = CursorLockMode.Locked;

        [Space]
        [SerializeField, Tooltip("Whether to hide the pointer on start.")] bool _HideOnStart = true;
        [SerializeField, Tooltip("Whether to show the pointer on focus loss.")]       bool _ShowOnFocusLoss    = true;
        [SerializeField, Tooltip("Whether to re-apply the lock mode on focus gain.")] bool _ReapplyOnFocusGain = true;

        /// <summary> Immediately hides the pointer. </summary>
        [Button, HideInEditorMode, ButtonGroup("ShowHide"), EnableIf("@UnityEngine.Cursor.visible")]
        public void HideNow() {
            Cursor.lockState = _LockMode;
            Cursor.visible = false;
        }

        /// <summary> Immediately shows the pointer. </summary>
        [Button, HideInEditorMode, ButtonGroup("ShowHide"), DisableIf("@UnityEngine.Cursor.visible")]
        public void ShowNow() {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary> Gets whether the pointer is shown or hidden. </summary>
        public static bool Visible {
            get => Cursor.visible;
            private set {
                if (value) {
                    Instance.ShowNow();
                } else {
                    Instance.HideNow();
                }
            }
        }

        readonly PriorityList<PointerPriority, bool> _Visible = new();

        void Start() {
            if (_HideOnStart) {
                HideNow();
            }

            PlayerInput.MousePosition.Changed += OnMousePositionChanged;
            _Visible.ValueChanged           += OnVisibleChanged;
        }

        /// <inheritdoc />
        protected override void OnDestroy() {
            base.OnDestroy();
            _Visible.ValueChanged -= OnVisibleChanged;
        }

        void OnApplicationFocus( bool HasFocus ) {
            // Debug.Log($"Focus {(HasFocus ? "gained" : "lost")}. (ShowOnFocusLoss: {_ShowOnFocusLoss}, ReapplyOnFocusGain: {_ReapplyOnFocusGain})");
            if (HasFocus) {
                if (_ReapplyOnFocusGain) {
                    Visible = _Visible.Value;
                }
            } else {
                if (_ShowOnFocusLoss) {
                    Visible = true;
                }
            }
        }


        static void OnVisibleChanged( bool Visible ) => Pointer.Visible = Visible;

        Vector2 _MousePosition = Vector2.zero;

        void OnMousePositionChanged( Vector2 OldValue, Vector2 NewValue ) => _MousePosition = NewValue;

        /// <summary> Gets the mouse position. </summary>
        public static Vector2 Position => Instance._MousePosition;

        /// <summary> Sets the visibility of the pointer. </summary>
        /// <param name="Priority"> The priority of the pointer. </param>
        /// <param name="Visible"> Whether to show or hide the pointer. </param>
        public static void SetVisible( PointerPriority Priority, bool Visible = true ) => Instance._Visible.AddOverride(Priority, Visible);

        /// <inheritdoc cref="SetVisible(PointerPriority,bool)"/>
        public static void SetInvisible( PointerPriority Priority, bool Invisible = true ) => SetVisible(Priority, !Invisible);

        /// <summary> Clears the visibility of the pointer for the given priority. </summary>
        /// <param name="Priority"> The priority of the pointer. </param>
        public static void ClearVisible( PointerPriority Priority ) => Instance._Visible.RemoveOverride(Priority);

    }

    public enum PointerPriority {
        /// <summary> General gameplay. </summary>
        Gameplay,
        /// <summary> General UI. </summary>
        [Obsolete]
        UI,
        /// <summary> Inventory. </summary>
        Inventory,
        /// <summary> Dialogue. </summary>
        Dialogue,
        /// <summary> Pause Menu. </summary>
        PauseMenu,
    }
}
