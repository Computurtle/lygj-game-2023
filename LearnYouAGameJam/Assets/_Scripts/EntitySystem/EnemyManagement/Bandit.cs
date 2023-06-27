using LYGJ.Common;
using LYGJ.EntitySystem.PlayerManagement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.EnemyManagement {
    public sealed class Bandit : EnemyBase {

        #region Overrides of EnemyBase

        /// <inheritdoc />
        public override EnemyType Type => EnemyType.Bandit;

        #endregion

        [SerializeField, Tooltip("The rigidbody component."), Required, ChildGameObjectsOnly]
        Rigidbody _Rb = null!;

        #if UNITY_EDITOR
        protected override void Reset() {
            base.Reset();
            _Rb = GetComponent<Rigidbody>();
        }
        #endif

        [Space]
        [SerializeField, Tooltip("The origin for the bandits."), Required, SceneObjectsOnly]
        Transform _Home = null!;

        // Bandits have three modes: aimless, backing up, and panic.
        // Aimless is just that, listlessly wandering around. Stopping to admire scenery, etc.
        // Backing up is when they've been alerted to the player's presence (by being in proximity), and they're backing away from the player slowly, carefully.
        // Panic is when they've been attacked by the player (taken damage), and they're running around like a headless chicken.

        [Title("Aimless")]
        [SerializeField, Tooltip("The time, in seconds, that the bandit will spend staying in one place before moving on."), MinMaxSlider(0, 30, true), SuffixLabel("s")]
        Vector2 _IdleDuration = new(5, 15);
        [SerializeField, Tooltip("The time, in seconds, that the bandit will spend moving before stopping to idle."), MinMaxSlider(0, 30, true), SuffixLabel("s")]
        Vector2 _WanderDuration = new(2, 5);
        [SerializeField, Tooltip("The speed, in metres per second, that the bandit will move at."), MinMaxSlider(0, 10, true), SuffixLabel("m/s")]
        Vector2 _WanderSpeed = new(1, 2);
        [SerializeField, Tooltip("The maximum distance bandits will happily travel."), Range(0, 100), SuffixLabel("m")]
        float _WanderDistance = 10; // This does not include backing up / panic, which are obvious 'emergency' states, you sick freaks.
        [SerializeField, Tooltip("The curve determining how far bandits will wander from their home.")]
        AnimationCurve _WanderDistanceCurve = new(new Keyframe(0, 1), new Keyframe(1, 0));
        [SerializeField, Tooltip("The curve determining how far bandits will want to turn when wandering.")]
        AnimationCurve _WanderTurnCurve = new(new Keyframe(0, 1), new Keyframe(1, 0));
        [SerializeField, Tooltip("The chance for a bandit to stop when unable to move."), Range(0, 1)]
        float _WanderStopChance = 0.5f;
        [SerializeField, Tooltip("The threshold, in metres per second, below which the bandit will stop moving."), Range(0, 10), SuffixLabel("m/s")]
        float _WanderStuckThreshold = 0.1f;

        [Title("Backing Up")]
        [SerializeField, Tooltip("The speed, in metres per second, that the bandit will move at."), MinMaxSlider(0, 10, true), SuffixLabel("m/s")]
        Vector2 _BackingUpSpeed = new(1, 2);
        [SerializeField, Tooltip("The minimum distance, in metres, that the bandit will keep between themselves and the player."), Range(0, 10), SuffixLabel("m")]
        float _BackingUpDistance = 2f;
        [SerializeField, Tooltip("The minimum time, in seconds, that the bandit will spend backing up before returning to aimless wandering."), MinMaxSlider(0, 30, true), SuffixLabel("s")]
        Vector2 _BackingUpDuration = new(5, 15);

        [Title("Panic")]
        [SerializeField, Tooltip("The speed, in metres per second, that the bandit will move at."), MinMaxSlider(0, 10, true), SuffixLabel("m/s")]
        Vector2 _PanicSpeed = new(3, 5);
        [SerializeField, Tooltip("The time, in seconds, that the bandit will spend running in a single direction in panic."), MinMaxSlider(0, 30, true), SuffixLabel("s")]
        Vector2 _PanicDuration = new(5, 15);
        [SerializeField, Tooltip("The chance, in percent, that the bandit will stop panicking and return to aimless wandering."), Range(0, 1), SuffixLabel("%")]
        float _PanicStopChance = 0.5f;

        public enum BanditState {
            Aimless_Idle,
            Aimless_Wander,
            BackingUp,
            Panic
        }

        [Title("Runtime")]
        [ShowInInspector, ReadOnly, Tooltip("The current state of the bandit."), HideInEditorMode]
        BanditState _State = BanditState.Aimless_Idle;
        [ShowInInspector, ReadOnly, Tooltip("The current direction the bandit is moving in."), HideInEditorMode]
        Vector3 _Direction = Vector3.zero;
        [ShowInInspector, ReadOnly, Tooltip("The current speed the bandit is moving at."), HideInEditorMode, SuffixLabel("m/s")]
        float _Speed = 0f;
        [ShowInInspector, ReadOnly, Tooltip("The current remaining duration the bandit has spent in the current state."), HideInEditorMode, SuffixLabel("s")]
        float _StateTime = 0f;
        [ShowInInspector, ReadOnly, Tooltip("The last wander position. Used to determine if the bandit has moved enough to not cancel."), HideInEditorMode]
        Vector3 _LastWanderPosition = Vector3.zero;

        void Start() {
            _Health.DamageTaken += OnDamageTaken;
        }

        void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            if (_Home) {
                Gizmos.DrawWireSphere(_Home.position, _WanderDistance);
            }
            if (!Application.isPlaying) { return; }

            Vector3 Pos = transform.position;
            switch (_State) {
                case BanditState.Aimless_Idle:
                case BanditState.Aimless_Wander:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(_LastWanderPosition, 0.5f);
                    break;
                case BanditState.BackingUp:
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(Pos, _BackingUpDistance);
                    break;
                case BanditState.Panic:
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(Pos, 0.5f);
                    break;
            }

            Gizmos.DrawLine(Pos, Pos + _Direction * _Speed);
        }

        Vector3 GetWanderDirection() {
            Vector3 HomePos = _Home.position;
            Vector3 SelfPos = _Rb.position;

            // If too far from home, move towards home.
            Vector3 Dir;
            if (Vector3.Distance(HomePos, SelfPos) > _WanderDistance) {
                Dir = (HomePos - SelfPos).normalized * _WanderDistance;
                return Dir;
            }
            Dir = Random.insideUnitSphere;
            Dir.y = 0f;
            Dir.Normalize();
            Dir *= _WanderDistance;

            // Shrink in relation to distance from home.
            float Distance = Vector3.Distance(HomePos, SelfPos);
            float Ratio    = _WanderDistanceCurve.Evaluate(Distance / _WanderDistance);
            Dir *= Ratio;

            // Turn in relation to distance from current direction.
            if (_Direction != Vector3.zero) {
                float AngleDelta = Vector3.Angle(_Direction, Dir);
                float TurnRatio  = _WanderTurnCurve.Evaluate(AngleDelta / 180f);
                Dir = Vector3.RotateTowards(_Direction, Dir, (1f - TurnRatio) * Mathf.PI, 0f);
            }

            return Dir;
        }

        void Enter_Aimless() {
            _State     = BanditState.Aimless_Wander;
            _StateTime = _WanderDuration.Random();
            _Direction = GetWanderDirection();
            _Speed     = _WanderSpeed.Random();
        }

        void FixedUpdate_Aimless() {
            CheckStimuli(false);
            _StateTime -= Time.fixedDeltaTime;
            if (_StateTime <= 0f) {
                CheckStimuli();
                return;
            }

            // Try and move to position. If we're stuck keep rolling a dice to see if we should stop.
            Vector3 Self = _Rb.position;
            Vector3 Dir  = _Direction.normalized;
            Vector3 Pos  = Self + Dir * (_Speed * Time.fixedDeltaTime);

            // "Stuck" is determined by delta movement.
            float Delta = Vector3.Distance(_LastWanderPosition, Pos);
            // Debug.Log($"{name}: {Delta}", this);
            if (Delta < _WanderStuckThreshold) {
                // Debug.Log("Stuck!", this);
                if (Random.value < _WanderStopChance) {
                    // Debug.Log("Gave up.", this);
                    Enter_Idle();
                    return;
                }
            }

            // Move to position.
            _Rb.MovePosition(Pos);
            _LastWanderPosition = _Rb.position;
        }

        void Enter_Idle() {
            _State     = BanditState.Aimless_Idle;
            _StateTime = _IdleDuration.Random();
            _Direction = Vector3.zero;
            _Speed     = 0f;
        }

        void FixedUpdate_Idle() {
            CheckStimuli(false);
            _StateTime -= Time.fixedDeltaTime;
            if (_StateTime <= 0f) {
                Enter_Aimless();
            }
        }

        void Enter_BackingUp() {
            _State                    = BanditState.BackingUp;
            _StateTime                = _BackingUpDuration.Random();
            _Speed                    = _BackingUpSpeed.Random();
        }

        void FixedUpdate_BackingUp() {
            Vector3 Plr  = Player.Position;
            Vector3 Self = _Rb.position;
            Vector3 Dir  = (Self - Plr).normalized;
            _Rb.MovePosition(Self + Dir * (_Speed * Time.fixedDeltaTime));
            if (Vector3.Distance(Plr, Self) >= _BackingUpDistance) {
                _StateTime -= Time.fixedDeltaTime;
                if (_StateTime <= 0f) {
                    CheckStimuli();
                }
            } else {
                _StateTime = _BackingUpDuration.Random();
            }
        }

        void Enter_Panic() {
            _State     = BanditState.Panic;
            _StateTime = _PanicDuration.Random();
            _Direction = Random.insideUnitSphere;
            _Direction.y = 0f;
            _Speed     = _PanicSpeed.Random();
        }

        void FixedUpdate_Panic() {
            _Rb.MovePosition(_Rb.position + _Direction * (_Speed * Time.fixedDeltaTime));
            _StateTime -= Time.fixedDeltaTime;
            if (_StateTime <= 0f) {
                if (Random.value <= _PanicStopChance) {
                    CheckStimuli();
                } else {
                    Enter_Panic();
                }
            }
        }

        void FixedUpdate() {
            switch (_State) {
                case BanditState.Aimless_Idle:
                    FixedUpdate_Idle();
                    break;
                case BanditState.Aimless_Wander:
                    FixedUpdate_Aimless();
                    break;
                case BanditState.BackingUp:
                    FixedUpdate_BackingUp();
                    break;
                case BanditState.Panic:
                    FixedUpdate_Panic();
                    break;
            }
        }

        void CheckStimuli( bool IdleFallback = true ) {
            // Check for stimuli (proximity, etc.) and change state accordingly.
            Vector3 Plr  = Player.Position;
            Vector3 Self = _Rb.position;
            if (Vector3.Distance(Plr, Self) <= _BackingUpDistance) {
                Enter_BackingUp();
            } else if (IdleFallback) {
                // 50/50 chance of entering aimless wander or idle.
                if (Random.value <= 0.5f) {
                    Enter_Aimless();
                } else {
                    Enter_Idle();
                }
            }
        }

        void OnDamageTaken( float Damage ) {
            Enter_Panic();
        }

    }
}
