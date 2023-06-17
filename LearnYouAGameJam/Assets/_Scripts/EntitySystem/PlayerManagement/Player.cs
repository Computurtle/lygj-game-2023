using System;
using System.Diagnostics;
using LYGJ.Common;
using LYGJ.Common.Datatypes.Collections;
using UnityEngine;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class Player : SingletonMB<Player> {
        [SerializeField, Tooltip("The renderer(s) that show the player's model.")] Renderer[] _Renderers = Array.Empty<Renderer>();

        #if UNITY_EDITOR
        void Reset() => _Renderers = GetComponentsInChildren<Renderer>();
        #endif

        readonly PriorityList<ModelPriority, bool> _Shown = new(true);

        void OnEnable()  => _Shown.ValueChanged += Shown_ValueChanged;
        void OnDisable() => _Shown.ValueChanged -= Shown_ValueChanged;

        void Shown_ValueChanged( bool Value ) {
            foreach (Renderer Renderer in _Renderers) {
                Renderer.enabled = Value;
            }
        }

        /// <summary> Sets the model to be shown. </summary>
        /// <param name="Priority"> The priority to set. </param>
        /// <param name="Value"> Whether to show the model. </param>
        public static void SetModelVisible( ModelPriority Priority, bool Value ) => Instance._Shown.AddOverride(Priority, Value);

        /// <summary> Clears the model priority. </summary>
        /// <param name="Priority"> The priority to clear. </param>
        public static void ClearModelPriority( ModelPriority Priority ) => Instance._Shown.RemoveOverride(Priority);
    }

    public enum ModelPriority {
        /// <summary> General gameplay. </summary>
        Gameplay,
        /// <summary> A vehicle. </summary>
        Vehicle,
        /// <summary> A minigame. </summary>
        Minigame,
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
