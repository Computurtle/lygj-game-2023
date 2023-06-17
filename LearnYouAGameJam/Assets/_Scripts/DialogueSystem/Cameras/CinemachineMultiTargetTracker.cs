using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cinemachine;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem.Cameras {
    [AddComponentMenu("Cinemachine/MultiTargetTracker")]
    public sealed class CinemachineMultiTargetTracker : CinemachineExtension {
        /// <summary> The targets to track. </summary>
        [Tooltip("The targets to track."), SceneObjectsOnly, ValidateInput("@$value.Count > 0", "Without any targets, the camera will not be able to track anything.")]
        public List<Transform> Targets = new();

        /// <summary> The minimum/maximum distance to use when calculating the ideal distance. </summary>
        [SerializeField, Tooltip("The minimum/maximum distance to use when calculating the ideal distance."), MinMaxSlider(0, 100, true), SuffixLabel("m")]
        public Vector2 MinMaxDistance = new(5, 20);

        /// <summary> The minimum/maximum field of view to use when calculating the ideal field of view. </summary>
        [SerializeField, Tooltip("The minimum/maximum field of view to use when calculating the ideal field of view."), MinMaxSlider(0, 100, true), SuffixLabel("°")]
        public Vector2 MinMaxFOV = new(30, 60);

        /// <inheritdoc />
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase Vcam,
            CinemachineCore.Stage        Stage,
            ref CameraState              State,
            float                        DeltaTime
        ) {
            if (_FramingTransposer == null) { return; }

            if (Stage == CinemachineCore.Stage.Body) {
                switch (Targets.Count) {
                    case 0:
                        return;
                }

                Bounds Bounds = CalculateBounds(Targets);
                (float MinDistance, float MaxDistance) = MinMaxDistance;
                float Distance = _FramingTransposer.m_CameraDistance = CalculateDistance(Bounds, MinDistance, MaxDistance);
                (float MinFOV, float MaxFOV) = MinMaxFOV;
                float FOV = CalculateFOV(Bounds, Distance, MinFOV, MaxFOV);
                State.Lens.FieldOfView = FOV;

                Vector3 CentrePoint = Bounds.center;
                Vector3 NewPosition = CentrePoint - GetForward(State.RawOrientation) * Distance;
                State.RawPosition = NewPosition;
            }
        }

        /// <summary> Gets the forward vector of the given quaternion. </summary>
        /// <param name="Quaternion"> The quaternion to get the forward vector of. </param>
        /// <returns> The vector pointing forward in the direction of the given quaternion. </returns>
        static Vector3 GetForward( Quaternion Quaternion ) => Quaternion * Vector3.forward;

        [SerializeField, Tooltip("The framing transposer."), Space, HideIf("@$value != null")]
        CinemachineFramingTransposer? _FramingTransposer;

        void Reset() {
            CinemachineVirtualCamera? VCam = VirtualCamera as CinemachineVirtualCamera;
            if (VCam != null) {
                _FramingTransposer = VCam.GetCinemachineComponent<CinemachineFramingTransposer>();
                if (_FramingTransposer == null) {
                    _FramingTransposer = VCam.AddCinemachineComponent<CinemachineFramingTransposer>();
                    #if UNITY_EDITOR
                    UnityEditor.Undo.RegisterCreatedObjectUndo(_FramingTransposer, "Create Framing Transposer");
                    #endif
                }
            } else {
                _FramingTransposer = null;
                Debug.LogWarning("Cannot get framing transposer from virtual camera.", this);
            }
        }

        /// <summary> Gets the framing position of the given target. </summary>
        /// <param name="Target"> The target to get the framing position of. </param>
        /// <returns> The framing position of the given target. </returns>
        static Vector3 CalculateFramingPosition( Transform? Target ) {
            if (Target == null) {
                Debug.LogWarning("Cannot get framing position of null target.");
                return Vector3.zero;
            }

            return Target.position;
        }

        /// <summary> Gets the bounding box of the given targets. </summary>
        /// <param name="Targets"> The targets to get the bounding box of. </param>
        /// <returns> The bounding box of the given targets. </returns>
        static Bounds CalculateBounds( IReadOnlyList<Transform> Targets ) {
            Bounds Bounds = new(CalculateFramingPosition(Targets[0]), Vector3.zero);
            foreach (Transform Target in Targets.Skip(1)) {
                Bounds.Encapsulate(CalculateFramingPosition(Target));
            }

            return Bounds;
        }

        /// <summary> Gets the centre point of the given targets. </summary>
        /// <param name="Targets"> The targets to get the centre point of. </param>
        /// <returns> The centre point of the given targets. </returns>
        /// <exception cref="InvalidOperationException"> Thrown if the given targets list is empty. </exception>
        static Vector3 CalculateCentrePoint( IReadOnlyList<Transform> Targets ) {
            switch (Targets.Count) {
                case 0: throw new InvalidOperationException("Cannot get centre point of zero targets.");
                case 1: return CalculateFramingPosition(Targets[0]);
            }

            Bounds Bounds = CalculateBounds(Targets);

            return Bounds.center;
        }

        /// <summary> Gets the ideal distance for the given bounding box to ensure it is all visible. </summary>
        /// <param name="Bounds"> The bounding box of the given targets. </param>
        /// <param name="MinDistance"> The minimum distance to use. </param>
        /// <param name="MaxDistance"> The maximum distance to use. </param>
        /// <returns> The ideal distance for the given bounding box. </returns>
        static float CalculateDistance( Bounds Bounds, float MinDistance, float MaxDistance ) {
            float Distance = Bounds.extents.magnitude;
            return Mathf.Clamp(Distance, MinDistance, MaxDistance);
        }

        /// <summary> Gets the ideal FOV angle for the given distance and bounding box to ensure it is all visible. </summary>
        /// <param name="Bounds"> The bounding box of the given targets. </param>
        /// <param name="Distance"> The distance from the camera to the bounding box. </param>
        /// <param name="MinFOV"> The minimum FOV to use. </param>
        /// <param name="MaxFOV"> The maximum FOV to use. </param>
        /// <returns> The ideal FOV angle for the given distance and bounding box. </returns>
        static float CalculateFOV( Bounds Bounds, float Distance, float MinFOV, float MaxFOV ) {
            float FOV = 2.0f * Mathf.Atan(Bounds.extents.magnitude / Distance) * Mathf.Rad2Deg;
            return Mathf.Clamp(FOV, MinFOV, MaxFOV);
        }

        void OnDrawGizmosSelected() {
            if (Targets.Count == 0) { return; }
            Transform Tr = transform;

            const float PointRadius    = 0.3f;
            const float SegmentLength  = 0.3f;
            Color       PointColour    = Color.yellow; // Each framing point
            Color       BoundsColour   = Color.red;    // The bounding box
            Color       DistanceColour = Color.green;  // The distance line
            Color       FrustumColour  = Color.blue;   // The FOV cone

            static void DrawSegmentedLine( Vector3 Start, Vector3 End, float SegmentLength ) {
                Vector3 Direction = (End - Start).normalized;
                float   Length    = (End - Start).magnitude;
                int     Segments  = Mathf.CeilToInt(Length / SegmentLength);
                for (int I = 0; I < Segments; I++) {
                    if (I % 2 == 0) { continue; }

                    float SegmentStart = I * SegmentLength;
                    float SegmentEnd   = (I + 1) * SegmentLength;
                    if (SegmentEnd > Length) { SegmentEnd = Length; }

                    Vector3 SegmentStartPoint = Start + Direction * SegmentStart;
                    Vector3 SegmentEndPoint   = Start + Direction * SegmentEnd;
                    Gizmos.DrawLine(SegmentStartPoint, SegmentEndPoint);
                }
            }

            Gizmos.color = PointColour;
            if (Targets.Count == 1) {
                Transform Target = Targets[0];
                Gizmos.DrawSphere(CalculateFramingPosition(Target), PointRadius);
                DrawSegmentedLine(Tr.position, Target.position, SegmentLength);
                return;
            }
            Bounds Bounds = CalculateBounds(Targets);
            Gizmos.color = BoundsColour;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

            (float MinDistance, float MaxDistance) = MinMaxDistance;
            float Distance = CalculateDistance(Bounds, MinDistance, MaxDistance);
            Gizmos.color = DistanceColour;
            Gizmos.DrawLine(Bounds.center, Bounds.center - Tr.forward * Distance);

            Gizmos.color                 = FrustumColour;
            (float MinFOV, float MaxFOV) = MinMaxFOV;
            float             FOV    = CalculateFOV(Bounds, Distance, MinFOV, MaxFOV);
            CinemachineBrain? Brain  = CinemachineCore.Instance.FindPotentialTargetBrain(VirtualCamera);
            float             Aspect = Brain != null ? Brain.OutputCamera.aspect : 1.0f;
            Matrix4x4         Temp   = Gizmos.matrix;

            Vector3 Centre = CalculateCentrePoint(Targets);
            Centre        -= Tr.forward * Distance;
            Gizmos.matrix =  Matrix4x4.TRS(Centre, Tr.rotation, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, FOV, Distance, Brain != null ? Brain.OutputCamera.nearClipPlane : 0.01f, Aspect);
            Gizmos.matrix = Temp;

            Gizmos.color = PointColour;
            foreach (Transform Target in Targets) {
                Gizmos.DrawSphere(CalculateFramingPosition(Target), PointRadius);
                DrawSegmentedLine(Centre, CalculateFramingPosition(Target), SegmentLength);
            }
        }
    }
}
