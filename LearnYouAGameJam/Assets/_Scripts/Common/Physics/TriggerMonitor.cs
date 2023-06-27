using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LYGJ.Common.Physics {
    public sealed class TriggerMonitor : MonoBehaviour, IReadOnlyCollection<Collider> {

        [SerializeField, Tooltip("Triggered when an object enters this trigger.")]
        UnityEvent<Collider> _TriggerEntered = new();

        [SerializeField, Tooltip("Triggered while an object stays within this trigger.")]
        UnityEvent<Collider> _TriggerStays = new();

        [SerializeField, Tooltip("Triggered when an object exits this trigger.")]
        UnityEvent<Collider> _TriggerExited = new();

        readonly HashSet<Collider> _Colliders = new();

        /// <summary> Raised when an object enters this trigger. </summary>
        public event UnityAction<Collider> TriggerEntered {
            add => _TriggerEntered.AddListener(value);
            remove => _TriggerEntered.RemoveListener(value);
        }

        /// <summary> Raised while an object stays within this trigger. </summary>
        public event UnityAction<Collider> TriggerStays {
            add => _TriggerStays.AddListener(value);
            remove => _TriggerStays.RemoveListener(value);
        }

        /// <summary> Raised when an object exits this trigger. </summary>
        public event UnityAction<Collider> TriggerExited {
            add => _TriggerExited.AddListener(value);
            remove => _TriggerExited.RemoveListener(value);
        }

        void OnTriggerEnter( Collider Other ) {
            if (_Colliders.Add(Other)) {
                _TriggerEntered.Invoke(Other);
            }
        }

        void OnTriggerStay( Collider Other ) {
            if (_Colliders.Contains(Other)) {
                _TriggerStays.Invoke(Other);
            } else {
                OnTriggerEnter(Other);
            }
        }

        void OnTriggerExit( Collider Other ) {
            if (_Colliders.Remove(Other)) {
                _TriggerExited.Invoke(Other);
            }
        }

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<Collider> GetEnumerator() {
            foreach (Collider? Collider in _Colliders) {
                if (Collider) { // Lifetime check.
                    yield return Collider;
                }
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Implementation of IReadOnlyCollection<out Collider>

        /// <inheritdoc />
        public int Count => _Colliders.Count;

        #endregion

    }

    public static class TriggerMonitorExtensions {
        /// <summary> Gets the first <see cref="TriggerMonitor"/> component attached to the <paramref name="GameObject"/>, attaching one if none is found. </summary>
        /// <param name="GameObject"> The <see cref="GameObject"/> to get the <see cref="TriggerMonitor"/> component from. </param>
        /// <param name="AddIfNotFound"> Whether to add a <see cref="TriggerMonitor"/> component if none is found. </param>
        /// <returns> The <see cref="TriggerMonitor"/> component attached to the <paramref name="GameObject"/>. </returns>
        public static TriggerMonitor GetTriggerMonitor( this GameObject GameObject, bool AddIfNotFound = true ) {
            if (GameObject.TryGetComponent(out TriggerMonitor TriggerMonitor)) {
                return TriggerMonitor;
            }

            if (AddIfNotFound) {
                return GameObject.AddComponent<TriggerMonitor>();
            }

            throw new MissingComponentException($"No {nameof(TriggerMonitor)} component found on {GameObject.name}.");
        }

        /// <inheritdoc cref="GetTriggerMonitor(UnityEngine.GameObject,bool)"/>
        public static TriggerMonitor GetTriggerMonitor( this Component Component, bool AddIfNotFound = true ) => Component.gameObject.GetTriggerMonitor(AddIfNotFound);
    }

}
