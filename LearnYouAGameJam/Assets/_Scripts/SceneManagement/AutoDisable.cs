using System.Collections.Generic;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.SceneManagement {
    public sealed class AutoDisable : MonoBehaviour {
        public enum Time {
            Awake,
            Start,
            FirstUpdate
        }

        [SerializeField, Tooltip("When to disable this object."), HideLabel, EnumToggleButtons] Time _Time = Time.Awake;

        /// <summary> Immediately disables the object. </summary>
        public void Disable() => gameObject.SetActive(false);

        /// <summary> Immediately enables the object. </summary>
        public void Enable() => gameObject.SetActive(true);

        void Awake()  { if (_Time == Time.Awake) { Disable(); } }
        void Start()  { if (_Time == Time.Start) { Disable(); } }
        void Update() { if (_Time == Time.FirstUpdate) { Disable(); } }
    }
}
