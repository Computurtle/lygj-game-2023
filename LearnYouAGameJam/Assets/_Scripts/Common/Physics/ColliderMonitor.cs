using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LYGJ.Common.Physics {
    public sealed class CollisionMonitor : MonoBehaviour, IReadOnlyCollection<Collider> {

        [SerializeField, Tooltip("Triggered when an object enters this collider.")]
        UnityEvent<Collision> _CollisionEntered = new();

        [SerializeField, Tooltip("Triggered while an object stays within this collider.")]
        UnityEvent<Collider> _CollisionStays = new();

        [SerializeField, Tooltip("Triggered when an object exits this collider.")]
        UnityEvent<Collision> _CollisionExited = new();

        readonly HashSet<Collider> _Colliders = new();

        /// <summary> Raised when an object enters this collider. </summary>
        public event UnityAction<Collision> CollisionEntered {
            add => _CollisionEntered.AddListener(value);
            remove => _CollisionEntered.RemoveListener(value);
        }

        /// <summary> Raised while an object stays within this collider. </summary>
        public event UnityAction<Collider> CollisionStays {
            add => _CollisionStays.AddListener(value);
            remove => _CollisionStays.RemoveListener(value);
        }

        /// <summary> Raised when an object exits this collider. </summary>
        public event UnityAction<Collision> CollisionExited {
            add => _CollisionExited.AddListener(value);
            remove => _CollisionExited.RemoveListener(value);
        }

        void OnCollisionEnter( Collision Other ) {
            if (_Colliders.Add(Other.collider)) {
                _CollisionEntered.Invoke(Other);
            }
        }

        void OnCollisionStay( Collision Other ) {
            if (_Colliders.Contains(Other.collider)) {
                _CollisionStays.Invoke(Other.collider);
            } else {
                OnCollisionEnter(Other);
            }
        }

        void OnCollisionExit( Collision Other ) {
            if (_Colliders.Remove(Other.collider)) {
                _CollisionExited.Invoke(Other);
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

    public static class CollisionMonitorExtensions {
        /// <summary> Gets the first <see cref="CollisionMonitor"/> component attached to the <paramref name="GameObject"/>, attaching one if none is found. </summary>
        /// <param name="GameObject"> The <see cref="GameObject"/> to get the <see cref="CollisionMonitor"/> component from. </param>
        /// <param name="AddIfNotFound"> Whether to add a <see cref="CollisionMonitor"/> component if none is found. </param>
        /// <returns> The <see cref="CollisionMonitor"/> component attached to the <paramref name="GameObject"/>. </returns>
        public static CollisionMonitor GetCollisionMonitor( this GameObject GameObject, bool AddIfNotFound = true ) {
            if (GameObject.TryGetComponent(out CollisionMonitor CollisionMonitor)) {
                return CollisionMonitor;
            }

            if (AddIfNotFound) {
                return GameObject.AddComponent<CollisionMonitor>();
            }

            throw new MissingComponentException($"No {nameof(CollisionMonitor)} component found on {GameObject.name}.");
        }
    }

}
