using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;
using IInteractable = LYGJ.Interactables.IInteractable;
using Object = UnityEngine.Object;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerInteractor : MonoBehaviour {
        [SerializeField, Tooltip("The layer for raycasting.")]                                  LayerMask               _Layer              = default;
        [SerializeField, Tooltip("The thickness(es) of the raycast(s).")]                       float[]                 _Thicknesses        = { 0.01f, 0.03f, 0.1f, 0.2f };
        [SerializeField, Tooltip("The maximum distance of the raycast(s).")]                    float                   _MaxDistance        = 4f;
        [SerializeField, Tooltip("The interaction with triggers.")]                             QueryTriggerInteraction _TriggerInteraction = QueryTriggerInteraction.UseGlobal;
        [SerializeField, Required, ChildGameObjectsOnly, Tooltip("The origin for raycast(s).")] Transform               _Origin             = null!;

        [Space]
        [SerializeField, Tooltip("The minimum time that must elapse, in seconds, between interactions.")] float _Cooldown = 0.25f;

        float _LastInteractionTime = 0f;

        bool CanInteract_Cooldown()                    => !Interacting             && Time.time - _LastInteractionTime >= _Cooldown;
        bool CanInteract( IInteractable Interactable ) => Interactable.CanInteract && CanInteract_Cooldown();

        /// <summary> Gets whether the hero is currently interacting. </summary>
        [ShowInInspector, ReadOnly, HideInEditorMode, Tooltip("Whether the hero is currently interacting.")] public bool Interacting { get; private set; } = false;

        void Interact( IInteractable Interactable ) {
            if (!CanInteract(Interactable)) {
                Debug.LogWarning($"Cannot interact with {Interactable}", Interactable as Object);
                return;
            }

            async UniTask Perform() {
                Interacting = true;
                try {
                    await Interactable.Interact();
                } catch (Exception Ex) {
                    Debug.LogException(Ex, Interactable as Object);
                }

                Interacting          = false;
                _LastInteractionTime = Time.time;
            }

            Perform().Forget();
        }

        #if UNITY_EDITOR
        void Reset() {
            _Layer  = LayerMask.NameToLayer("Interactable");
            _Origin = transform;
        }
        #endif

        static bool Thickcast( Vector3 Origin, Vector3 Direction, float MaxDistance, out RaycastHit Hit, int Layer, IEnumerable<float> Thicknesses, QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.UseGlobal ) {
            if (Physics.Raycast(Origin, Direction, out Hit, MaxDistance, Layer, TriggerInteraction)) {
                return true;
            }

            foreach (float Thickness in Thicknesses) {
                if (Physics.SphereCast(Origin, Thickness, Direction, out Hit, MaxDistance, Layer, TriggerInteraction)) {
                    return true;
                }
            }

            return false;
        }

        #if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            if (_Origin == null) {
                return;
            }

            static bool IsValidHit( RaycastHit HitInfo ) => TryGetComponentInParent<IInteractable>(HitInfo.collider, out _);

            static void DrawCapsule( Vector3 Start, Vector3 End, float Radius ) {
                Gizmos.DrawWireSphere(Start, Radius);
                Gizmos.DrawWireSphere(End, Radius);

                Gizmos.DrawLine(Start + Vector3.up    * Radius, End + Vector3.up    * Radius);
                Gizmos.DrawLine(Start + Vector3.down  * Radius, End + Vector3.down  * Radius);
                Gizmos.DrawLine(Start + Vector3.left  * Radius, End + Vector3.left  * Radius);
                Gizmos.DrawLine(Start + Vector3.right * Radius, End + Vector3.right * Radius);
            }

            Vector3
                Origin    = _Origin.position,
                Direction = _Origin.forward,
                End       = Origin + Direction * _MaxDistance;

            int  Lyr = _Layer;
            bool Hit = Physics.Raycast(Origin, Direction, out RaycastHit HitInfo, _MaxDistance, Lyr, _TriggerInteraction);
            Gizmos.color = Hit ? IsValidHit(HitInfo) ? Color.green : Color.yellow : Color.red;
            Gizmos.DrawLine(Origin, Hit ? HitInfo.point : End);
            if (Hit) { return; }

            foreach (float Thickness in _Thicknesses) {
                Hit          = Physics.SphereCast(_Origin.position, Thickness, _Origin.forward, out HitInfo, _MaxDistance, Lyr);
                Gizmos.color = Hit ? IsValidHit(HitInfo) ? Color.green : Color.yellow : Color.red;
                DrawCapsule(_Origin.position, Hit ? HitInfo.point : End, Thickness);
                if (Hit) { return; }
            }
        }
        #endif

        (Collider Collider, IInteractable Interactable)? _Last;

        static bool TryGetComponentInParent<TComponent>( Component Source, [NotNullWhen(true)] out TComponent? Component ) {
            Transform? Parent = Source.transform;
            while (Parent != null) {
                if (Parent.TryGetComponent<TComponent>(out Component)) {
                    #pragma warning disable CS8762
                    return true;
                    #pragma warning restore CS8762
                }

                Parent = Parent.parent;
            }

            Component = default;
            return false;
        }

        void Update() {
            if (!CanInteract_Cooldown()) {
                if (_Last is { } L) {
                    ((Component)L.Interactable).gameObject.RemoveOutline();
                }

                _Last = null;
                return;
            }

            Collider? New = Thickcast(_Origin.position, _Origin.forward, _MaxDistance, out RaycastHit Hit, _Layer, _Thicknesses, _TriggerInteraction) ? Hit.collider : null;
            if (New != null) {
                if (_Last is { } L && L.Collider != New) {
                    ((Component)L.Interactable).gameObject.RemoveOutline();
                    _Last = null;
                }

                if (!TryGetComponentInParent(New, out IInteractable? Interactable)) {
                    return;
                }

                if (CanInteract(Interactable)) {
                    Component Cmp = (Component)Interactable;
                    Cmp.gameObject.AddOutline(OutlineStyle.Interactable);
                    _Last = (New, Interactable);
                }
            } else if (_Last is { } L) {
                ((Component)L.Interactable).gameObject.RemoveOutline();
                _Last = null;
            }
        }

        void Start() => PlayerInput.Interact.Pressed += OnInteractPressed;

        void OnInteractPressed() {
            if (_Last is { } L) {
                if (!TryGetComponentInParent(L.Collider, out IInteractable? Interactable)) {
                    return;
                }

                if (Interactable is Component C) {
                    C.gameObject.RemoveOutline();
                }

                _Last = null;
                Interact(Interactable);
            }
        }
    }
}
