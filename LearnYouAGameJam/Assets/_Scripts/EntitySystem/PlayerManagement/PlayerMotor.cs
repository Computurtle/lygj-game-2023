using System;
using System.Diagnostics;
using Cinemachine;
using LYGJ.Common;
using LYGJ.Common.Attributes;
using LYGJ.Common.Datatypes.Collections;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerMotor : SingletonMB<PlayerMotor> {

        [SerializeField, Tooltip("The rigidbody component."), LabelText("Rigidbody"), Required, ChildGameObjectsOnly] Rigidbody       _Rb       = null!;
        [SerializeField, Tooltip("The collider component."), Required, ChildGameObjectsOnly]                          CapsuleCollider _Collider = null!;

        #region Properties / Movement

        [SerializeField, ToggleGroup(nameof(CanMove), "Movement"), Tooltip("The walk speed of the player, in game units per second.")]
        float _WalkSpeed = 2f;
        [SerializeField, ToggleGroup(nameof(CanMove), "Movement"), Tooltip("The multiplier upon strafing movement, relative to forward movement.")]
        float _StrafeMultiplier = 0.85f;
        [SerializeField, ToggleGroup(nameof(CanMove), "Movement"), Tooltip("The multiplier upon backpedaling movement, relative to forward movement.")]
        float _BackpedalMultiplier = 0.65f;

        readonly PriorityList<MotorPriority, bool> _CanMovePriority = new();

        [SerializeField, HideInEditorMode] bool _CanMove = true;
        [ShowInInspector, ToggleGroup(nameof(CanMove), "Movement")]
        bool CanMove {
            #if UNITY_EDITOR
            get => Application.isPlaying ? _CanMove && _CanMovePriority : _CanMove;
            set {
                if (Application.isPlaying) { _CanMovePriority.DefaultValue = value; } else { _CanMove = value; }
            }
            #else
            get => _CanMove && _CanMovePriority;
            #endif
        }

        /// <summary> Sets whether the player can move. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="CanMove"> Whether the player can move. </param>
        public static void SetCanMove( MotorPriority Priority, bool CanMove ) => Instance._CanMovePriority.AddOverride(Priority, CanMove);

        /// <summary> Clears the move override with the given priority. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        public static void ClearCanMove( MotorPriority Priority ) => Instance._CanMovePriority.RemoveOverride(Priority);

        #endregion

        #region Properties / Running

        [SerializeField, ToggleGroup(nameof(CanRun), "Running"), Tooltip("The running speed of the player, in game units per second.")]
        float _RunSpeed = 4f;

        readonly PriorityList<MotorPriority, bool> _CanRunPriority = new();

        [SerializeField, HideInEditorMode] bool _CanRun = true;
        [ShowInInspector, ToggleGroup(nameof(CanRun), "Running")]
        bool CanRun {
            #if UNITY_EDITOR
            get => Application.isPlaying ? CanMove && _CanRun && _CanRunPriority : _CanRun;
            set {
                if (Application.isPlaying) { _CanRunPriority.DefaultValue = value; } else { _CanRun = value; }
            }
            #else
            get => CanMove && _CanRun && _CanRunPriority;
            #endif
        }

        /// <summary> Sets whether the player can run. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="CanRun"> Whether the player can run. </param>
        public static void SetCanRun( MotorPriority Priority, bool CanRun ) => Instance._CanRunPriority.AddOverride(Priority, CanRun);

        /// <summary> Clears the run override with the given priority. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        public static void ClearCanRun( MotorPriority Priority ) => Instance._CanRunPriority.RemoveOverride(Priority);

        #endregion

        #region Properties / Jumping

        [SerializeField, ToggleGroup(nameof(CanJump), "Jumping"), Tooltip("The jump height of the player, in game units.")]
        float _JumpHeight = 1f;
        [SerializeField, ToggleGroup(nameof(CanJump), "Jumping"), Tooltip("The multiplier to apply to general movement velocity when jumping.")]
        float _JumpForwardMultiplier = 1f;

        readonly PriorityList<MotorPriority, bool> _CanJumpPriority = new();

        [SerializeField, HideInEditorMode] bool _CanJump = true;
        [ShowInInspector, ToggleGroup(nameof(CanJump), "Jumping")]
        bool CanJump {
            #if UNITY_EDITOR
            get => Application.isPlaying ? CanMove && _CanJump && _CanJumpPriority : _CanJump;
            set {
                if (Application.isPlaying) { _CanJumpPriority.DefaultValue = value; } else { _CanJump = value; }
            }
            #else
            get => CanMove && _CanJump && _CanJumpPriority;
            #endif
        }

        /// <summary> Sets whether the player can jump. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="CanJump"> Whether the player can jump. </param>
        public static void SetCanJump( MotorPriority Priority, bool CanJump ) => Instance._CanJumpPriority.AddOverride(Priority, CanJump);

        /// <summary> Clears the jump override with the given priority. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        public static void ClearCanJump( MotorPriority Priority ) => Instance._CanJumpPriority.RemoveOverride(Priority);

        #endregion

        #region Properties / Ground Checking

        [SerializeField, FoldoutGroup("Ground Checking"), Tooltip("The layer mask used to determine what is considered 'ground'.")]
        LayerMask _GroundLayer = default;
        [SerializeField, FoldoutGroup("Ground Checking"), Tooltip("The radius from the player's origin to check for ground.")]
        float _GroundRadius = 0.5f;
        [SerializeField, FoldoutGroup("Ground Checking"), Tooltip("The player's origin."), Required, ChildGameObjectsOnly]
        Transform _Origin = null!;

        #endregion

        #region Properties / Collision Checking

        [SerializeField, FoldoutGroup("Collision Checking"), Tooltip("The radius multiplier to use when checking for obstacles."), MinValue(0f), LabelText("Radius Multiplier")]
        float _ObsRadiusMult = 1.05f;
        [SerializeField, FoldoutGroup("Collision Checking"), Tooltip("The height multiplier to use when checking for obstacles."), MinValue(0f), LabelText("Height Multiplier")]
        float _ObsHeightMult = 0.95f;
        [SerializeField, FoldoutGroup("Collision Checking"), Tooltip("The lookahead time, in seconds, to use when checking for obstacles."), MinValue(0f), LabelText("Lookahead Time"), SuffixLabel("s")]
        float _ObsLookahead = 0.1f;

        #endregion

        #region Properties / Looking

        [SerializeField, ToggleGroup(nameof(CanLook), "Looking"), Tooltip("The sensitivity of the player's mouse."), MinValue(0f)]
        Vector2 _MouseSensitivity = new(1f, 1f);
        [SerializeField, ToggleGroup(nameof(CanLook), "Looking"), Tooltip("The player's body."), Required, ChildGameObjectsOnly]
        Transform _Body = null!;
        [SerializeField, ToggleGroup(nameof(CanLook), "Looking"), Tooltip("The minimum/maximum angle of the player's head."), MinMaxSlider(-90f, 90f, true)]
        Vector2 _HeadAngle = new(-90f, 90f);
        [SerializeField, ToggleGroup(nameof(CanLook), "Looking"), Tooltip("The player's head."), Required, ChildGameObjectsOnly]
        Transform _Head = null!;
        [SerializeField, ToggleGroup(nameof(CanLook), "Looking"), Tooltip("Whether to invert the x/y look axis.")]
        bool2 _LookInvert = new(false, true);

        readonly PriorityList<MotorPriority, bool> _CanLookPriority = new();

        [SerializeField, HideInEditorMode] bool _CanLook = true;
        [ShowInInspector, ToggleGroup(nameof(CanLook), "Looking")]
        bool CanLook {
            #if UNITY_EDITOR
            get => Application.isPlaying ? _CanLook && _CanLookPriority : _CanLook;
            set {
                if (Application.isPlaying) { _CanLookPriority.DefaultValue = value; } else { _CanLook = value; }
            }
            #else
            get => _CanLook && _CanLookPriority;
            #endif
        }

        /// <summary> Sets whether the player can look. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="CanLook"> Whether the player can look. </param>
        public static void SetCanLook( MotorPriority Priority, bool CanLook ) => Instance._CanLookPriority.AddOverride(Priority, CanLook);

        /// <summary> Clears the look override with the given priority. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        public static void ClearCanLook( MotorPriority Priority ) => Instance._CanLookPriority.RemoveOverride(Priority);

        #endregion

        #region Properties / Crouching

        [SerializeField, ToggleGroup(nameof(CanCrouch), "Crouching"), Tooltip("The multiplier to apply to general movement velocity when crouching.")]
        float _CrouchSpeedMultiplier = 0.6f;

        readonly PriorityList<MotorPriority, bool> _CanCrouchPriority = new();

        [SerializeField, HideInEditorMode] bool _CanCrouch = true;
        [ShowInInspector, ToggleGroup(nameof(CanCrouch), "Crouching")]
        bool CanCrouch {
            #if UNITY_EDITOR
            get => Application.isPlaying ? CanMove && _CanCrouch && _CanCrouchPriority : _CanCrouch;
            set {
                if (Application.isPlaying) { _CanCrouchPriority.DefaultValue = value; } else { _CanCrouch = value; }
            }
            #else
            get => CanMove && _CanCrouch && _CanCrouchPriority;
            #endif
        }

        /// <summary> Sets whether the player can crouch. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="CanCrouch"> Whether the player can crouch. </param>
        public static void SetCanCrouch( MotorPriority Priority, bool CanCrouch ) => Instance._CanLookPriority.AddOverride(Priority, CanCrouch);

        /// <summary> Clears the crouch override with the given priority. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        public static void ClearCanCrouch( MotorPriority Priority ) => Instance._CanLookPriority.RemoveOverride(Priority);

        #endregion

        #region Properties / Visuals

        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The cinemachine virtual camera(s) to control."), Required, ChildGameObjectsOnly]
        CinemachineVirtualCamera[] _Cameras = Array.Empty<CinemachineVirtualCamera>();

        [Space]
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The base FOV when walking."), MinValue(0f)]                 float _WalkFOV = 50f;
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The FOV to use when the player is running."), MinValue(0f)] float _RunFOV  = 60f;

        [Space]
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The increase in FOV when the player jumps."), MinValue(0f)] float _JumpFOV = 10f;
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The intensity over time for jump FOV increases.")]     AnimationCurve _JumpFOVIntensity = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The duration, in seconds, of the jump FOV increase.")] float          _JumpFOVDuration  = 1f;

        [Space]
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The interpolation rate to use when lerping the FOV."), MinValue(0f)] float _FOVLerp = 10f;

        [Space]
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The model."), Required, ChildGameObjectsOnly] Transform _Model = null!;
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("The interpolation rate to use when lerping the model towards the forward direction."), MinValue(0f)]
        float _ModelLerp = 10f;
        [SerializeField, FoldoutGroup("Visuals"), Tooltip("Whether to rotate the model towards the forward direction when moving backwards.")]
        bool _RotateModelBackwards = true;

        #endregion

        #region Properties / Animations

        [SerializeField, FoldoutGroup("Animations"), Tooltip("The animator component."), Required, ChildGameObjectsOnly] Animator _Anim = null!;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The 'animation' parameter name."), AnimParam] string _AnimParam = "animation";

        int _AnimParamHash;

        [Space]
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'idle' animation.")] int _IdleAnimID = 34;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'walk' animation.")]            int _WalkAnimID         = 21;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'walk left' animation.")]       int _WalkLeftAnimID     = 22;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'walk right' animation.")]      int _WalkRightAnimID    = 23;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'run' animation.")]             int _RunAnimID          = 18;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'run left' animation.")]        int _RunLeftAnimID      = 19;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'run right' animation.")]       int _RunRightAnimID     = 20;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'jump' animation.")]            int _JumpAnimID         = 50;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'idle (crouched)' animation.")] int _IdleCrouchedAnimID = 44;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'walk (crouched)' animation.")] int _WalkCrouchedAnimID = 48;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The ID of the 'run (crouched)' animation.")]  int _RunCrouchedAnimID  = 47;

        [Space]
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The time, in seconds, to allow the 'jump' animation to play before transitioning out.")] float _JumpGracePeriod = 0.5f;
        [SerializeField, FoldoutGroup("Animations"), Tooltip("The minimum offset from origin for horizontal movement to be considered 'walking' in a given direction."), MinValue(0f)] float _SidewalkThreshold = 0.1f;

        #endregion

        float _TimeOfLastJump = 0f;
        void Update_FOV() {
            float FOV = _Running ? _RunFOV : _WalkFOV;

            float Now               = Time.time;
            float TimeSinceLastJump = Now - _TimeOfLastJump;
            if (TimeSinceLastJump < _JumpFOVDuration) {
                float Intensity = _JumpFOVIntensity.Evaluate(TimeSinceLastJump / _JumpFOVDuration);
                FOV += _JumpFOV * Intensity;
            }

            foreach (CinemachineVirtualCamera Cam in _Cameras) {
                Cam.m_Lens.FieldOfView = Mathf.MoveTowards(Cam.m_Lens.FieldOfView, FOV, _FOVLerp * Time.deltaTime);
            }
        }

        void Update_Model() {
            Vector3 ModelForward = _Model.forward;
            Vector3 Forward      = _Body.forward;

            // Intensity of interpolation is relative to speed (i.e. not moving = no rotation, moving fast = fast rotation)
            Vector2 M =
                _RotateModelBackwards
                    ? _Move
                    : Vector3.Dot(ModelForward, Forward) < 0f ? Vector2.zero : _Move;
                    // : new(_Move.x, Mathf.Max(0f, _Move.y));

            float LerpIntensity = GetSpeed() * M.magnitude;
            if (LerpIntensity <= float.Epsilon) { return; }

            Vector3 Target = Vector3.Slerp(ModelForward, Forward, _ModelLerp * LerpIntensity * Time.deltaTime);
            _Model.forward = Target;
        }

        readonly Collider[] _GroundHits = new Collider[1];

        #if UNITY_EDITOR
        void Reset() {
            _Rb       = GetComponentInChildren<Rigidbody>();
            _Collider = GetComponentInChildren<CapsuleCollider>();
            _Origin   = transform;
        }

        void OnDrawGizmosSelected() {
            OnDrawGizmosSelected_Grounded();
            OnDrawGizmosSelected_Movement();
        }

        void OnDrawGizmosSelected_Grounded() {
            if (_Origin == null) { return; }

            bool Grounded = CheckIsGrounded();
            Gizmos.color = Grounded ? Color.green : Color.red;
            Vector3 Origin = _Origin.position;
            Gizmos.DrawWireSphere(Origin, _GroundRadius);
        }
        #endif

        #region Overrides of SingletonMB<HeroMotor>

        /// <inheritdoc />
        protected override void Awake() {
            base.Awake();
            _AnimParamHash  = Animator.StringToHash(_AnimParam);
            _TimeOfLastJump = Time.time - _JumpGracePeriod;
        }

        #endregion

        void Start() {
            PlayerInput.Move.Changed    += OnMoveChanged;
            PlayerInput.Run.Pressed     += OnRunPressed;
            PlayerInput.Run.Released    += OnRunReleased;
            PlayerInput.Jump.Pressed    += OnJumpPressed;
            PlayerInput.Look.Changed    += OnLookChanged;
            PlayerInput.Crouch.Pressed  += OnCrouchPressed;
            PlayerInput.Crouch.Released += OnCrouchReleased;

            _CanMovePriority.DefaultValue   = _CanMove;
            _CanRunPriority.DefaultValue    = _CanRun;
            _CanJumpPriority.DefaultValue   = _CanJump;
            _CanLookPriority.DefaultValue   = _CanLook;
            _CanCrouchPriority.DefaultValue = _CanCrouch;
        }

        bool CheckIsGrounded() => Physics.OverlapSphereNonAlloc(_Origin.position, _GroundRadius, _GroundHits, _GroundLayer) > 0;

        [ShowInInspector, ReadOnly, HideInEditorMode, ToggleLeft, FoldoutGroup("Debug")] bool _IsGrounded;

        void OnJumpPressed() {
            _IsGrounded = CheckIsGrounded();
            if (!CanJump || !_IsGrounded) {
                return;
            }

            _IsGrounded = false;
            _Rb.AddForce(Vector3.up * Mathf.Sqrt(_JumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
            if (CanMove) {
                _Rb.AddForce(_Body.forward * (_Move.y * _JumpForwardMultiplier), ForceMode.VelocityChange);
                _Rb.AddForce(_Body.right   * (_Move.x * _JumpForwardMultiplier), ForceMode.VelocityChange);
            }
            _TimeOfLastJump = Time.time;
        }

        [ShowInInspector, ReadOnly, HideInEditorMode, ToggleLeft, FoldoutGroup("Debug")] bool _Running = false;

        void OnRunPressed() => _Running = CanRun;

        void OnRunReleased( float HoldTime ) => _Running = false;

        [ShowInInspector, ReadOnly, HideInEditorMode, FoldoutGroup("Debug")] Vector2 _Move;

        void OnMoveChanged( Vector2 OldValue, Vector2 NewValue ) => _Move = NewValue.normalized;

        [ShowInInspector, ReadOnly, HideInEditorMode, ToggleLeft, FoldoutGroup("Debug")] bool _Crouching = false;

        void OnCrouchPressed() => _Crouching = CanCrouch;

        void OnCrouchReleased( float HoldTime ) => _Crouching = false;

        float GetSpeed() {
            float Speed = _Running ? _RunSpeed : _WalkSpeed;
            if (CanCrouch && _Crouching) {
                Speed *= _CrouchSpeedMultiplier;
            }
            return Speed;
        }

        static bool CheckCollision( CapsuleCollider CC, Vector3 Delta, float RadiusMult, float HeightMult, float Lookahead, out RaycastHit Hit ) {
            // Perform a collision test on a theoretical capsule collider, centred at CC's centre, but with the height and radius scaled by the given multipliers
            // Then perform a sweep test at CC.position + Delta * Lookahead, assuming delta is a velocity vector (i.e. pre-multiplied by Time.fixedDeltaTime)

            Vector3 Centre = CC.center;
            float   Radius = CC.radius * RadiusMult;
            float   Height = CC.height * HeightMult;

            Vector3 Origin = CC.transform.position + Centre;

            Vector3 Bottom = Origin + Vector3.down * (Height * 0.5f - Radius);
            Vector3 Top    = Origin + Vector3.up   * (Height * 0.5f - Radius);

            return Physics.CapsuleCast(Bottom, Top, Radius, Delta.normalized, out Hit, Delta.magnitude * Lookahead, ~0, QueryTriggerInteraction.Ignore);
        }

        Vector3 GetDesiredMovement( float Speed, float DeltaTime ) {
            Vector3 Desired = _Body.forward   * (_Move.y * (_Move.y < 0f ? _BackpedalMultiplier : 1f) * Speed * DeltaTime);
            Desired += _Body.right * (_Move.x * _StrafeMultiplier * Speed * DeltaTime);

            return Desired;
        }

        void FixedUpdate_Move() {
            if (!CanMove || _Move.sqrMagnitude < 0.01f) {
                return;
            }

            float Speed = GetSpeed();

            // OLD:
            // _Rb.MovePosition(_Rb.position + _Body.forward * (_Move.y * Speed * Time.fixedDeltaTime));
            // _Rb.MovePosition(_Rb.position + _Body.right   * (_Move.x * _StrafeMultiplier * Speed * Time.fixedDeltaTime));

            // NEW: (Performs sweep-test as necessary)
            Vector3 Desired = GetDesiredMovement(Speed, Time.fixedDeltaTime);
            if (!CheckCollision(_Collider, Desired, _ObsRadiusMult, _ObsHeightMult, _ObsLookahead, out RaycastHit Hit)) {
                _Rb.MovePosition(_Rb.position + Desired);
                #if UNITY_EDITOR
                _Editor_MovementHit = null;
            } else {
                _Editor_MovementHit = Hit;
                if (Editor_LogHits) {
                    Editor_LogHit();
                }
            }
            #else
            }
            #endif
        }

        #if UNITY_EDITOR
        static bool Editor_LogHits {
            get => UnityEditor.EditorPrefs.GetBool("HeroMotor/LogMovementHits", false);
            set => UnityEditor.EditorPrefs.SetBool("HeroMotor/LogMovementHits", value);
        }
        void Editor_LogHit() {
            if (_Editor_MovementHit is { } Hit) {
                Debug.LogWarning($"Movement blocked by {Hit.collider.name} at {Hit.point}", Hit.collider);
            } else {
                Debug.Log("Movement not blocked");
            }
        }
        RaycastHit? _Editor_MovementHit = null;
        [UnityEditor.MenuItem("CONTEXT/HeroMotor/What am I hitting?", false, 100)]
        static void ContextMenu_WhatAmIHitting(UnityEditor.MenuCommand Command) => ((PlayerMotor)Command.context).Editor_LogHit();
        [UnityEditor.MenuItem("CONTEXT/HeroMotor/What am I hitting?", true, 100)]
        static bool ContextMenu_WhatAmIHitting_Validate(UnityEditor.MenuCommand Command) => Application.isPlaying && ((PlayerMotor)Command.context)._Editor_MovementHit is not null;
        [UnityEditor.MenuItem("CONTEXT/HeroMotor/Log Movement Hits", false, 101)]
        static void ContextMenu_LogMovementHits(UnityEditor.MenuCommand Command) {
            Editor_LogHits = !Editor_LogHits;
            UnityEditor.Menu.SetChecked("CONTEXT/HeroMotor/Log Movement Hits", Editor_LogHits);
        }
        [UnityEditor.MenuItem("CONTEXT/HeroMotor/Log Movement Hits", true, 101)]
        static bool ContextMenu_LogMovementHits_Validate(UnityEditor.MenuCommand Command) {
            UnityEditor.Menu.SetChecked("CONTEXT/HeroMotor/Log Movement Hits", Editor_LogHits);
            return true;
        }

        void OnDrawGizmosSelected_Movement() {
            if (!Application.isPlaying) { return; }

            if (_Origin == null) { return; }
            if (_Collider == null) { return; }
            if (_Rb == null) { return; }

            static void DrawCapsule( Vector3 Root, float Height, float Radius ) {
                Vector3 Bottom = Root + Vector3.up * Radius;
                Vector3 Top    = Root + Vector3.up * (Height - Radius);
                Gizmos.DrawWireSphere(Bottom, Radius);
                Gizmos.DrawWireSphere(Top,    Radius);
                Gizmos.DrawLine(Bottom + Vector3.forward * Radius, Top + Vector3.forward * Radius);
                Gizmos.DrawLine(Bottom + Vector3.back    * Radius, Top + Vector3.back    * Radius);
                Gizmos.DrawLine(Bottom + Vector3.left    * Radius, Top + Vector3.left    * Radius);
                Gizmos.DrawLine(Bottom + Vector3.right   * Radius, Top + Vector3.right   * Radius);
            }

            // Perform the sweep test. Draw a green circle at the desired position if it's clear, or a red circle if it's blocked.
            const float PointRadius = 0.1f;
            float       Speed       = GetSpeed();

            Vector3 Origin  = _Origin.position;
            Vector3 Desired = GetDesiredMovement(Speed, Time.deltaTime);
            if (CheckCollision(_Collider, Desired, _ObsRadiusMult, _ObsHeightMult, _ObsLookahead, out RaycastHit Hit)) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(Hit.point, PointRadius);
                Gizmos.color = Color.red;
                DrawCapsule(Origin + Desired * _ObsLookahead, _Collider.height * _ObsHeightMult, _Collider.radius * _ObsRadiusMult);
            } else {
                Gizmos.color = Color.green;
            }

            DrawCapsule(Origin, _Collider.height * _ObsHeightMult, _Collider.radius * _ObsRadiusMult);
            Gizmos.DrawSphere(Origin       + Desired, PointRadius);
            Gizmos.DrawLine(Origin, Origin + Desired);
        }
        #endif

        [ShowInInspector, ReadOnly, HideInEditorMode, FoldoutGroup("Debug")] Vector2 _Look;

        void OnLookChanged( Vector2 OldValue, Vector2 NewValue ) => _Look = NewValue;
        void Update_Look() {
            if (!CanLook || Pointer.Visible) { return; }

            _Look.x *= _LookInvert.x ? -1f : 1f;
            _Look.y *= _LookInvert.y ? -1f : 1f;

            _Body.Rotate(Vector3.up, _Look.x    * _MouseSensitivity.x);
            _Head.Rotate(Vector3.right, _Look.y * _MouseSensitivity.y);

            Vector3 HeadRotation = _Head.localRotation.eulerAngles;
            if (HeadRotation.x > 180) { HeadRotation.x -= 360; }
            HeadRotation.x      = Mathf.Clamp(HeadRotation.x, _HeadAngle.x, _HeadAngle.y);
            HeadRotation.y      = 0f; // No yaw.
            HeadRotation.z      = 0f; // No roll. Both are possible to occur due to floating-point imprecision and rapid mouse movement.
            _Head.localRotation = Quaternion.Euler(HeadRotation);
        }

        void FixedUpdate() => FixedUpdate_Move();

        void Update() {
            Update_Look();
            Update_FOV();
            Update_Model();
            Update_Anim();
        }

        /// <summary> Teleports the player to the specified position. </summary>
        /// <param name="Position"> The position to teleport to. </param>
        /// <param name="Rotation"> The rotation to set the player to. </param>
        public void Teleport( Vector3 Position, Quaternion Rotation ) {
            _Rb.velocity        = Vector3.zero;
            _Rb.angularVelocity = Vector3.zero;
            _Rb.position        = Position;
            _Rb.rotation        = Rotation;
        }

        /// <inheritdoc cref="Teleport(UnityEngine.Vector3,UnityEngine.Quaternion)"/>
        public static void TeleportTo( Vector3 Position, Quaternion Rotation ) => Instance.Teleport(Position, Rotation);

        /// <inheritdoc cref="Teleport(UnityEngine.Vector3,UnityEngine.Quaternion)"/>
        public void Teleport( Vector3 Position ) => Teleport(Position, _Rb.rotation);

        /// <inheritdoc cref="Teleport(UnityEngine.Vector3,UnityEngine.Quaternion)"/>
        public static void TeleportTo( Vector3 Position ) => Instance.Teleport(Position);

        /// <inheritdoc cref="Teleport(UnityEngine.Vector3,UnityEngine.Quaternion)"/>
        public static void TeleportTo( Transform Transform ) => Instance.Teleport(Transform.position, Transform.rotation);

        [ShowInInspector, ReadOnly, HideInEditorMode, FoldoutGroup("Debug")] MovementState _State = MovementState.None;

        int AnimState {
            get => _Anim.GetInteger(_AnimParamHash);
            set => _Anim.SetInteger(_AnimParamHash, value);
        }

        void Update_Anim() {
            // Update states.
            if (CanCrouch && _Crouching) {
                _State |= MovementState.Crouching;
            } else {
                _State &= ~MovementState.Crouching;
            }
            if (CanRun && _Running) {
                _State |= MovementState.Running;
            } else {
                _State &= ~MovementState.Running;
            }
            if (CanMove && _Move.sqrMagnitude > 0.01f) {
                _State |= MovementState.Walking;
            } else {
                _State &= ~MovementState.Walking;
            }
            if (CanJump && !_IsGrounded && Time.time - _TimeOfLastJump < _JumpGracePeriod) {
                _State |= MovementState.Jumping;
            } else {
                _State &= ~MovementState.Jumping;
            }
            if (CanMove && _Move.x < -_SidewalkThreshold) {
                _State |= MovementState.Left;
            } else {
                _State &= ~MovementState.Left;
            }
            if (CanMove && _Move.x >  _SidewalkThreshold) {
                _State |= MovementState.Right;
            } else {
                _State &= ~MovementState.Right;
            }

            // Update animations.
            if ((_State & MovementState.Jumping) != 0) {
                AnimState = _JumpAnimID;
            } else if ((_State & MovementState.Crouching) != 0) {
                if ((_State & MovementState.Running) != 0) {
                    AnimState = _RunCrouchedAnimID;
                } else if ((_State & MovementState.Walking) != 0) {
                    AnimState = _WalkCrouchedAnimID;
                } else {
                    AnimState = _IdleCrouchedAnimID;
                }
            } else if ((_State & MovementState.Running) != 0) {
                if ((_State & MovementState.Left) != 0) {
                    AnimState = _RunLeftAnimID;
                } else if ((_State & MovementState.Right) != 0) {
                    AnimState = _RunRightAnimID;
                } else {
                    AnimState = _RunAnimID;
                }
            } else if ((_State & MovementState.Walking) != 0) {
                if ((_State & MovementState.Left) != 0) {
                    AnimState = _WalkLeftAnimID;
                } else if ((_State & MovementState.Right) != 0) {
                    AnimState = _WalkRightAnimID;
                } else {
                    AnimState = _WalkAnimID;
                }
            } else {
                AnimState = _IdleAnimID;
            }
        }
    }

    [Flags]
    public enum MovementState {
        None      = 0,
        Walking   = 1 << 0,
        Running   = 1 << 1,
        Crouching = 1 << 2,
        Jumping   = 1 << 3,
        Left      = 1 << 4,
        Right     = 1 << 5
    }

    public enum MotorPriority {
        /// <summary> General gameplay. </summary>
        Gameplay,
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
