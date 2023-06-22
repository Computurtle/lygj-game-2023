using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using LYGJ.Common.Datatypes.Collections;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using IInteractable = LYGJ.Interactables.IInteractable;
using Object = UnityEngine.Object;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerInteractor : SingletonMB<PlayerInteractor> {
        [Title("Optimisation")]
        [SerializeField, Tooltip("The layer for interaction detection.")] LayerMask               _Layer              = default;
        [SerializeField, Tooltip("The interaction with triggers.")]       QueryTriggerInteraction _TriggerInteraction = QueryTriggerInteraction.UseGlobal;

        const int _MaxDetected = 10; // Max amount of colliders to consider in the radius.

        [Title("Detection")]
        [SerializeField, Tooltip("The origin for interaction detection scans."), Required, ChildGameObjectsOnly] Transform _Origin = null!;
        [SerializeField, Tooltip("The radius for interaction detection scans."), SuffixLabel("m")] float _Radius = 0.5f;

        [Title("View Angle")]
        [SerializeField, Tooltip("The maximum angle, in degrees, between the origin and the target."), SuffixLabel("°"), Range(0, 360)] float _MaxAngle = 90f;
        // Priority always goes to targets in front of the origin, with second priority being distance.

        [Title("Safety")]
        [SerializeField, Tooltip("The minimum time that must elapse, in seconds, between interactions."), SuffixLabel("s"), MinValue(0)] float _Cooldown = 0.25f;

        readonly PriorityList<InteractionPriority, bool> _CanInteractPriority = new(true);

        /// <summary> Sets whether the player can interact. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="CanMove"> Whether the player can interact. </param>
        public static void SetCanInteract( InteractionPriority Priority, bool CanMove ) => Instance._CanInteractPriority.AddOverride(Priority, CanMove);

        /// <summary> Clears the interact override with the given priority. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        public static void ClearCanInteract( InteractionPriority Priority ) => Instance._CanInteractPriority.RemoveOverride(Priority);

        /// <summary> Ranks the given components by their suitability for interaction. </summary>
        /// <param name="Options"> The options to rank. </param>
        /// <param name="Count"> The amount of options to rank. </param>
        /// <param name="Predicate"> The predicate to use for determining if an option is valid. </param>
        /// <param name="Sorter"> The sorter to use for ranking. </param>
        /// <param name="Source"> The source transform to use for ranking. </param>
        /// <param name="MaxAngle"> The maximum angle, in degrees, between the origin and the target. </param>
        /// <param name="MaxDistance"> The maximum distance, in metres, between the origin and the target. </param>
        /// <typeparam name="TComponent"> The type of component to rank. </typeparam>
        /// <returns> The ranked options. </returns>
        static IList<TComponent> Rank<TComponent>( IEnumerable<TComponent?> Options, int Count, Func<TComponent, bool> Predicate, SortedList<float, TComponent> Sorter, Transform Source, float MaxAngle, float MaxDistance ) where TComponent : Component {
            switch (Count) {
                case 0:
                    return Array.Empty<TComponent>();
                default:
                    Vector3 Origin = Source.position, Direction = Source.forward;
                    Sorter.Clear();
                    // Ensure sorter has enough capacity.
                    if (Sorter.Capacity < Count) {
                        Sorter.Capacity = Count;
                    }

                    foreach (TComponent? Option in Options) {
                        if (!Predicate(Option!)) {
                            continue;
                        }

                        Vector3 ToTarget = Option!.transform.position - Origin;
                        float   Angle    = Vector3.Angle(Direction, ToTarget);
                        if (Angle > MaxAngle) {
                            continue;
                        }

                        float SqrDistance = ToTarget.sqrMagnitude;
                        // Key favours view angle over distance.
                        float Key = Angle + SqrDistance / MaxDistance;
                        Sorter.Add(Key, Option);
                    }
                    return Sorter.Values;
            }
        }

        float _CooldownRemaining = 0f;

        bool CanInteract( IInteractable Interactable ) => Interactable.CanInteract && !Interacting && _CooldownRemaining <= 0f;

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

                Interacting        = false;
                _CooldownRemaining = _Cooldown;
            }

            Perform().Forget();
        }

        #if UNITY_EDITOR
        void Reset() {
            _Layer  = LayerMask.NameToLayer("Interactable");
            _Origin = transform;
        }
        #endif

        #region Overrides of SingletonMB<PlayerInteractor>

        /// <inheritdoc />
        protected override void Awake() {
            base.Awake();
            _CanInteractPriority.DefaultValue = true;
        }

        #endregion

        readonly Collider?[] _Detected = new Collider?[_MaxDetected];

        #if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            if (_Origin == null) {
                return;
            }

            int Count = Scan();
            // Set colour:
            // - Red if no interactable detected,
            // - Yellow if interactable detected but not valid,
            // - Green if interactable detected and valid;
            // - Cyan for non-primary valid interactable,
            // - Magenta for non-primary invalid interactable.
            Vector3 Orig = _Origin.position;
            for (int I = 0; I < Count; I++) {
                Collider? Collider = _Detected[I];
                if (Collider == null) {
                    continue;
                }

                bool Valid = IsValid(Collider);
                Gizmos.color = Valid ? (I == 0 ? Color.green : Color.cyan) : (I == 0 ? Color.red : Color.magenta);
                Vector3 End = Collider.transform.position;
                Gizmos.DrawWireSphere(End, _Radius);
                Gizmos.DrawLine(Orig, End);
            }

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(Orig, _Radius);
        }
        #endif

        public readonly struct CachedInteractable : IEquatable<CachedInteractable> {
            /// <summary> The interactable component. </summary>
            /// <remarks> This is not necessarily on the same transform as the collider. This is the first parent transform with an interactable component. </remarks>
            public readonly IInteractable? Interactable;

            /// <summary> The collider of the interactable component. </summary>
            /// <remarks> This is not necessarily on the same transform as the interactable component. This is the child from which the detection originated. </remarks>
            public readonly Collider? Collider;

            public CachedInteractable( IInteractable Interactable, Collider Collider ) {
                this.Interactable = Interactable;
                this.Collider     = Collider;
            }

            /// <summary> Whether the interactable is in an invalid state (i.e. either the interactable or collider (or both) is null). </summary>
            [MemberNotNullWhen(false, nameof(Interactable), nameof(Collider))]
            public bool InvalidState => Interactable == null || Collider == null;

            /// <summary> Highlights the interactable. </summary>
            public void Highlight() {
                switch (Interactable) {
                    case null:
                        Debug.LogWarning("Cannot highlight null interactable!");
                        return;
                    case Component Component:
                        Component.gameObject.AddOutline(OutlineStyle.Interactable);
                        break;
                    default:
                        Debug.LogWarning($"Cannot highlight interactable of type {Interactable.GetType().GetNiceName()}!", Collider);
                        break;
                }
            }

            /// <summary> Un-highlights the interactable. </summary>
            public void Unhighlight() {
                switch (Interactable) {
                    case null: // Nothing to un-highlight.
                        return;
                    case Component Component:
                        Component.gameObject.RemoveOutline();
                        break;
                    default:
                        Debug.LogWarning($"Cannot un-highlight interactable of type {Interactable.GetType().GetNiceName()}!", Collider);
                        break;
                }
            }

            /// <summary> Attempts to find the interactable component on the collider. </summary>
            /// <param name="Source"> The collider to search. </param>
            /// <param name="Found"> The found interactable, if any. </param>
            /// <returns> <see langword="true"/> if an interactable was found, otherwise <see langword="false"/>. </returns>
            public static bool TryGetInteractable( Collider Source, [NotNullWhen(true)] out CachedInteractable? Found ) {
                if (TryGetComponentInParent(Source, out IInteractable? Interactable)) {
                    Found = new CachedInteractable(Interactable, Source);
                    return true;
                }

                Found = null;
                return false;
            }

            #region Equality Members

            /// <inheritdoc />
            public bool Equals( CachedInteractable Other ) =>
                EqualityComparer<Collider?>.Default.Equals(Collider, Other.Collider)
                || EqualityComparer<IInteractable?>.Default.Equals(Interactable, Other.Interactable);

            /// <inheritdoc />
            public override bool Equals( object? Obj ) => Obj is CachedInteractable Other && Equals(Other);

            /// <inheritdoc />
            public override int GetHashCode() => Collider != null ? Collider.GetHashCode() : 0;

            public static bool operator ==( CachedInteractable Left, CachedInteractable Right ) => Left.Equals(Right);
            public static bool operator !=( CachedInteractable Left, CachedInteractable Right ) => !Left.Equals(Right);

            #endregion

        }

        CachedInteractable? _Last;

        static bool TryGetComponentInParent<TComponent>( Component? Source, [NotNullWhen(true)] out TComponent? Component ) {
            if (Source == null) {
                Component = default;
                return false;
            }

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

        int Scan() {
            int Count = Physics.OverlapSphereNonAlloc(_Origin.position, _Radius, _Detected, _Layer, _TriggerInteraction);
            for (int I = 0; I < Count; I++) {
                if (CachedInteractable.TryGetInteractable(_Detected[I]!, out CachedInteractable? Interactable)) {
                    _Detected[I] = Interactable.Value.Collider;
                } else {
                    _Detected[I] = null;
                }
            }

            return Count;
        }

        readonly SortedList<float, Collider> _Ranker = new(_MaxDetected);

        static bool IsValid( Collider Collider ) {
            if (!TryGetComponentInParent(Collider, out IInteractable? Interactable)) {
                return false;
            }

            return Interactable.CanInteract;
        }

        void Update() {
            if (_CooldownRemaining > 0f) {
                _CooldownRemaining -= Time.deltaTime;
                ClearLast();
                return;
            }

            if (Interacting || !_CanInteractPriority) {
                ClearLast();
                return;
            }

            { // Cleanup _Last if invalid (e.g. any part destroyed)
                if (_Last is { InvalidState: true }) {
                    ClearLast();
                }
            }

            // Scan for interactables
            int Count = Scan();
            if (Count == 0) { // No interactables found, so un-highlight the last interactable (if any)
                ClearLast();
                return;
            }

            // Find the closest interactable. If not the same as _Last, un-highlight _Last and highlight the new interactable.
            // Use the Rank method to determine the closest interactable.
            IEnumerable<Collider> Closest = Rank(_Detected, Count, IsValid, _Ranker, _Origin, _MaxAngle, _Radius);
            bool AnyFound = false;
            foreach (Collider Collider in Closest) {
                if (CachedInteractable.TryGetInteractable(Collider, out CachedInteractable? Interactable)) {
                    if (Interactable == _Last) {
                        return;
                    }
                    AnyFound = true;

                    ClearLast();

                    Interactable.Value.Highlight();
                    _Last = Interactable;
                    return;
                }
            }

            { // If no interactable was found, un-highlight _Last (if any)
                if (!AnyFound) {
                    ClearLast();
                }
            }

        }

        void ClearLast() {
            if (_Last is { } L) {
                L.Unhighlight();
            }

            _Last = null;
        }

        void Start() => PlayerInput.Interact.Pressed += OnInteractPressed;

        void OnInteractPressed() {
            if (_Last is { } L) {
                if (L.InvalidState) {
                    ClearLast();
                    return;
                }

                Interact(L.Interactable);
            }
        }
    }

    public enum InteractionPriority {
        /// <summary> General gameplay. </summary>
        Gameplay,
        /// <summary> A minigame. </summary>
        Minigame,
        /// <summary> General UI. </summary>
        [Obsolete]
        UI,
        /// <summary> Barter. </summary>
        Barter,
        /// <summary> Inventory. </summary>
        Inventory,
        /// <summary> Dialogue. </summary>
        Dialogue,
        /// <summary> Pause Menu. </summary>
        PauseMenu,
    }
}
