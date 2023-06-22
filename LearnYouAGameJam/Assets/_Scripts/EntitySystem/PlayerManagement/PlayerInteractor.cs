using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        [ShowInInspector, SuffixLabel("°"), PropertyRange(0, 360),
         PropertyTooltip("@\"The maximum horizontal angle, in degrees, between the origin and the target.\\n\\nDegrees: \" + (" + nameof(_MaxAzimuth) + ").ToString(\"N0\") + \"°\\nRadians: \" + (" + nameof(_MaxAzimuth) + " * Mathf.Deg2Rad).ToString(\"N2\") + \"㎭\"")]
        float MaxAzimuth {
            get => _MaxAzimuth * Mathf.Rad2Deg;
            set => _MaxAzimuth = value * Mathf.Deg2Rad;
        }
        [SerializeField, HideInInspector] float _MaxAzimuth = 45f * Mathf.Deg2Rad;

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

        /// <summary> Ranks the colliders and their interactable components by their suitability for interaction. </summary>
        /// <param name="Colliders"> The colliders to rank. </param>
        /// <param name="Count"> The amount of colliders to rank. </param>
        /// <param name="Sorter"> The sorter to use for ranking. Used to avoid memory allocations. </param>
        /// <param name="Origin"> The origin of the interaction detection. </param>
        /// <param name="Forward"> The forward direction of the interaction detection. </param>
        /// <param name="MaxAzimuthRad"> The maximum horizontal angle, in radians, between the origin and the target. </param>
        /// <param name="MaxDist"> The maximum distance, in metres, between the origin and the target. </param>
        /// <returns> The ranked colliders and their interactable components. </returns>
        static IEnumerable<CachedInteractable> Rank( IEnumerable<Collider?> Colliders, int Count, PostSortList<float, CachedInteractable> Sorter, Vector3 Origin, Vector3 Forward, float MaxDist, float MaxAzimuthRad ) {
            switch (Count) {
                case 0: {
                    yield break;
                }
                case 1: {
                    Collider? Option = Colliders.FirstOrDefault();
                    if (Option != null
                        && WithinView(Origin, Forward, Option.transform.position, MaxDist, MaxAzimuthRad, out _, out _)
                        && CachedInteractable.TryGet(Option, out CachedInteractable? Found)
                        && Found.Value.Interactable!.CanInteract) {
                        yield return Found.Value;
                    }
                    break;
                }
                default: {
                    Sorter.Clear();
                    if (Sorter.Capacity < Count) {
                        Sorter.Capacity = Count;
                    }

                    int I = 0;
                    foreach (Collider? Collider in Colliders) {
                        if (I >= Count) {
                            break;
                        }
                        if (Collider == null) {
                            // Debug.LogWarning($"{I} - Null component in {nameof(Colliders)}.");
                            continue;
                        }

                        if (WithinView(Origin, Forward, Collider.transform.position, MaxDist, MaxAzimuthRad, out float HorAngle, out float DistRel)
                            && CachedInteractable.TryGet(Collider, out CachedInteractable? Found)
                            && Found.Value.Interactable!.CanInteract) {
                            float SortingKey = GetSortingKey(HorAngle, DistRel);
                            // Debug.Log($"{I} - SortingKey of '{Collider.name}': {SortingKey} (HorAngle: {HorAngle}, DistRel: {DistRel})");
                            Sorter.Add(SortingKey, Found.Value);
                        } /* else {
                            Debug.Log($"{I} - '{Collider.name}' is not within view.");
                        }*/

                        I++;
                    }

                    // Extract the sorted components from the ItemList
                    foreach ((_, CachedInteractable Interactable) in Sorter) {
                        yield return Interactable;
                    }
                    break;
                }
            }

            static float GetSortingKey( float HorAngle, float DistRel ) => HorAngle + DistRel;

            static bool WithinView( Vector3 Origin, Vector3 Forward, Vector3 TargetPos, float MaxDist, float MaxAzimuthRad, out float Azimuth, out float DistRel ) {
                Vector3 Dir = TargetPos - Origin;

                // Debug.Log($"Distance: {Dir.magnitude}/{MaxDist} (={Dir.magnitude / MaxDist:P0}), Azimuth: {Mathf.Abs(Mathf.Acos(Vector3.Dot(Forward, Dir.normalized)) * Mathf.Rad2Deg)}/{MaxAzimuthRad * Mathf.Rad2Deg} (={Mathf.Abs(Mathf.Acos(Vector3.Dot(Forward, Dir.normalized))) / MaxAzimuthRad:P0})");

                float Dist = Dir.magnitude;
                if (Dist > MaxDist) {
                    Azimuth = 0f;
                    DistRel = 0f;
                    return false;
                }

                Azimuth = Mathf.Acos(Vector3.Dot(Forward, Dir.normalized));
                if (Mathf.Abs(Azimuth) > MaxAzimuthRad) {
                    DistRel = 0f;
                    return false;
                }

                DistRel = Dist / MaxDist;
                return true;
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

            if (Application.isPlaying) {
                Transform Tr = Cameras.Transform;
                Vector3     Pos      = Tr.position;
                const float AxisSize = 1f, PointRadii = 0.1f;

                Gizmos.color = Color.red;
                Vector3 R = (Pos + Tr.right) * AxisSize;
                Gizmos.DrawLine(Pos, R);
                Gizmos.DrawSphere(R, PointRadii);
                Gizmos.color = Color.green;
                Vector3 U = (Pos + Tr.up) * AxisSize;
                Gizmos.DrawLine(Pos, U);
                Gizmos.DrawSphere(U, PointRadii);
                Gizmos.color = Color.blue;
                Vector3 F = (Pos + Tr.forward) * AxisSize;
                Gizmos.DrawLine(Pos, F);
                Gizmos.DrawSphere(F, PointRadii);
            }

            static void DrawFrustum( Vector3 Origin, Vector3 Forward, float Distance, float HorAngleRad, float VerAngleRad ) {
                float HorAngle = Mathf.Tan(HorAngleRad);
                float VerAngle = Mathf.Tan(VerAngleRad);

                Vector3 Right = Vector3.Cross(Vector3.up, Forward).normalized;
                Vector3 Up    = Vector3.Cross(Forward, Right).normalized;

                Vector3 TopLeft     = Origin + Forward * Distance + (Right * HorAngle + Up * VerAngle) * Distance;
                Vector3 TopRight    = Origin + Forward * Distance + (Right * HorAngle - Up * VerAngle) * Distance;
                Vector3 BottomLeft  = Origin + Forward * Distance + (-Right * HorAngle + Up * VerAngle) * Distance;
                Vector3 BottomRight = Origin + Forward * Distance + (-Right * HorAngle - Up * VerAngle) * Distance;

                Gizmos.DrawLine(Origin, TopLeft);
                Gizmos.DrawLine(Origin, TopRight);
                Gizmos.DrawLine(Origin, BottomLeft);
                Gizmos.DrawLine(Origin, BottomRight);

                Gizmos.DrawLine(TopLeft, TopRight);
                Gizmos.DrawLine(TopRight, BottomRight);
                Gizmos.DrawLine(BottomRight, BottomLeft);
                Gizmos.DrawLine(BottomLeft, TopLeft);
            }
            Vector3 Orig = _Origin.position;
            Gizmos.color = Color.cyan;
            DrawFrustum(Orig, _Origin.forward, _Radius, _MaxAzimuth, 90f);
            Gizmos.DrawWireSphere(Orig, _Radius);

            int Count = Scan();
            // Set colour:
            // - Red if no interactable detected,
            // - Yellow if interactable detected but not valid,
            // - Green if interactable detected and valid;
            // - Cyan for non-primary valid interactable,
            // - Magenta for non-primary invalid interactable.
            for (int I = 0; I < Count; I++) {
                Collider? Collider = _Detected[I];
                if (Collider == null) {
                    continue;
                }

                bool Valid = TryGetComponentInParent<IInteractable>(Collider, out _);
                Gizmos.color = Valid ? (I == 0 ? Color.green : Color.cyan) : (I == 0 ? Color.red : Color.magenta);
                Vector3 End = Collider.transform.position;
                Gizmos.DrawWireSphere(End, _Radius);
                Gizmos.DrawLine(Orig, End);
            }
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
            public static bool TryGet( Collider Source, [NotNullWhen(true)] out CachedInteractable? Found ) {
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
                if (CachedInteractable.TryGet(_Detected[I]!, out CachedInteractable? Interactable)) {
                    _Detected[I] = Interactable.Value.Collider;
                } else {
                    _Detected[I] = null;
                }
            }

            return Count;
        }

        readonly PostSortList<float, CachedInteractable> _Ranker = new(_MaxDetected);

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
            IEnumerable<CachedInteractable> Closest  = Rank(_Detected, Count, _Ranker, _Origin.position, Cameras.Forward, _Radius, _MaxAzimuth);
            bool             AnyFound = false;
            foreach (CachedInteractable Interactable in Closest) {
                if (Interactable == _Last) {
                    return;
                }
                AnyFound = true;

                ClearLast();

                Interactable.Highlight();
                _Last = Interactable;
                return;
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
