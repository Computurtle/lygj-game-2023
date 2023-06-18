using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace LYGJ.EntitySystem.NPCSystem {
    [RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
    public sealed class NPCMotor : MonoBehaviour {

        #region Fields / Properties

        #region Movement

        [Title("Movement")]
        [SerializeField, Tooltip("The rigidbody."), Required, ChildGameObjectsOnly, LabelText("Rigidbody")] Rigidbody _Rb = null!;
        [SerializeField, Tooltip("The navmesh agent."), Required, ChildGameObjectsOnly, LabelText("NavMesh Agent")] NavMeshAgent _Agent = null!;

        public enum MovementStyle {
            MovePosition,
            RbPosition,
            Transform,
            Agent
        }

        #if UNITY_EDITOR
        [OnValueChanged(nameof(RecalcMovementStyle))]
        #endif
        [SerializeField, Tooltip("The movement style."), LabelText("Style")] MovementStyle _MovementStyle = MovementStyle.MovePosition;

        #if UNITY_EDITOR
        void RecalcMovementStyle() {
            if (!Application.isPlaying) { return; }
            _Agent.updatePosition = _MovementStyle == MovementStyle.Agent;
        }
        #endif

        #endregion

        #region Animations

        [Title("Animations")]
        [SerializeField, Tooltip("The animator."), ChildGameObjectsOnly] Animator? _Anim = null;

        [MemberNotNullWhen(true, nameof(_Anim))]
        bool HasAnim => _Anim != null;

        #if UNITY_EDITOR
        [OnValueChanged(nameof(RecalcWalkAnimParam)), ShowIf(nameof(HasAnim))]
        #endif
        [SerializeField, Tooltip("The walking animation boolean parameter."), AnimParam] string _WalkAnimParam = "Walking";

        int _WalkAnimParamHash;

        #if UNITY_EDITOR
        void RecalcWalkAnimParam() {
            if (Application.isPlaying) { _WalkAnimParamHash = Animator.StringToHash(_WalkAnimParam); }
        }
        #endif

        [SerializeField, HideInInspector] float _WalkAnimThreshold = 0.1f;

        #if UNITY_EDITOR
        [ShowIf(nameof(HasAnim))]
        #endif
        [ShowInInspector, Tooltip("The minimum square velocity required to play the walking animation."), MinValue(0), LabelText("Walking Threshold")]
        float WalkAnimSqrThreshold {
            get => _WalkAnimThreshold * _WalkAnimThreshold;
            set => _WalkAnimThreshold = Mathf.Sqrt(value);
        }

        #if UNITY_EDITOR
        [OnValueChanged(nameof(RecalcRunAnimParam)), ShowIf(nameof(HasAnim))]
        #endif
        [SerializeField, Tooltip("The running animation boolean parameter."), AnimParam] string _RunAnimParam = "Running";

        int _RunAnimParamHash;

        #if UNITY_EDITOR
        void RecalcRunAnimParam() {
            if (Application.isPlaying) { _RunAnimParamHash = Animator.StringToHash(_RunAnimParam); }
        }
        #endif

        [SerializeField, HideInInspector] float _RunAnimThreshold = 0.1f;

        #if UNITY_EDITOR
        [ShowIf(nameof(HasAnim))]
        #endif
        [ShowInInspector, Tooltip("The minimum square velocity required to play the running animation."), MinValue(0), LabelText("Running Threshold")]
        float RunAnimSqrThreshold {
            get => _RunAnimThreshold * _RunAnimThreshold;
            set => _RunAnimThreshold = Mathf.Sqrt(value);
        }

        #endregion

        #endregion

        #if UNITY_EDITOR
        void Reset() {
            _Rb    = GetComponent<Rigidbody>();
            _Agent = GetComponent<NavMeshAgent>();
            _Anim  = GetComponentInChildren<Animator>();
        }
        #endif

        void Awake() {
            _WalkAnimParamHash = Animator.StringToHash(_WalkAnimParam);
            _RunAnimParamHash  = Animator.StringToHash(_RunAnimParam);

            // See: https://discussions.unity.com/t/navmesh-agent-velocity-vs-physics/134891/2
            if (_MovementStyle != MovementStyle.Agent) {
                _Agent.updatePosition = false; // We will handle the position updates ourselves.
            }
        }

        void FixedUpdate() {
            // Update the position according to the navmesh agent.
            switch (_MovementStyle) {
                case MovementStyle.MovePosition:
                    _Rb.MovePosition(_Agent.nextPosition);
                    break;
                case MovementStyle.RbPosition:
                    _Rb.position = _Agent.nextPosition;
                    break;
                case MovementStyle.Transform:
                    transform.position = _Agent.nextPosition;
                    break;
                case MovementStyle.Agent:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void Update() {
            if (!HasAnim) { return; }
            // Update the animations according to current velocity.
            float SqrMag = _Agent.velocity.sqrMagnitude;
            if (SqrMag > float.Epsilon) {
                Debug.Log(SqrMag, this);
            }

            _Anim.SetBool(_WalkAnimParamHash, SqrMag >= WalkAnimSqrThreshold);
            _Anim.SetBool(_RunAnimParamHash,  SqrMag >= RunAnimSqrThreshold);
        }

        #if UNITY_EDITOR
        [Button("Path to Position")]
        void Editor_PathToPosition( Vector3 Position ) => _Agent.SetDestination(Position);
        #endif

        /// <summary> Moves the NPC towards the given target. </summary>
        /// <param name="Target"> The target position. </param>
        /// <param name="Token"> The cancellation token. </param>
        /// <exception cref="InvalidOperationException"> The NPC is already pathing somewhere else. </exception>
        public async UniTask MoveTowards( Vector3 Target, CancellationToken Token = default ) {
            if (_Agent.hasPath) {
                throw new InvalidOperationException("NPC is already pathing somewhere else.");
            }

            _Agent.SetDestination(Target);
            while (_Agent.remainingDistance < _Agent.stoppingDistance && !Token.IsCancellationRequested) {
                await UniTask.Yield();
            }
        }
    }

    public static class NPCMotorExtensions {

        /// <inheritdoc cref="NPCMotor.MoveTowards"/>
        public static UniTask MoveTowards( this NPCBase NPC, Vector3 Target, CancellationToken Token = default ) {
            if (!NPC.TryGetComponent(out NPCMotor Motor)) {
                throw new InvalidOperationException("NPC does not have a motor.");
            }

            return Motor.MoveTowards(Target, Token);
        }
    }
}
