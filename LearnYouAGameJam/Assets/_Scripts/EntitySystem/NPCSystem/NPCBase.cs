using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem.NPCSystem {
    public abstract class NPCBase : Entity {
        Emotion _Emotion = (Emotion)((int)default(Emotion)+1);

        /// <summary> Event signature for the <see cref="EmotionChanged"/> event. </summary>
        /// <param name="OldEmotion"> The old emotion. </param>
        /// <param name="NewEmotion"> The new emotion. </param>
        public delegate void EmotionChangedEventHandler( Emotion OldEmotion, Emotion NewEmotion );

        /// <summary> Raised when the emotion of the NPC changes. </summary>
        public event EmotionChangedEventHandler? EmotionChanged;

        /// <summary> Raises the <see cref="EmotionChanged"/> event. </summary>
        /// <param name="OldEmotion"> The old emotion. </param>
        /// <param name="NewEmotion"> The new emotion. </param>
        protected virtual void PropagateEmotionChanged( Emotion OldEmotion, Emotion NewEmotion ) => EmotionChanged?.Invoke(OldEmotion, NewEmotion);

        /// <summary> Gets or sets the NPC's emotion. </summary>
        [ShowInInspector, HideInEditorMode, Tooltip("The NPC's emotion.")]
        public Emotion Emotion {
            get => _Emotion;
            set {
                if (_Emotion == value) {
                    return;
                }

                Emotion OldEmotion = _Emotion;
                _Emotion = value;
                PropagateEmotionChanged(OldEmotion, value);
            }
        }

        [SerializeField, Tooltip("The NPC's default emotion.")]
        Emotion _DefaultEmotion = Emotion.Neutral;

        /// <summary> Gets the NPC's default emotion. </summary>
        public Emotion DefaultEmotion => _DefaultEmotion;

        /// <summary> Sets the NPC's emotion to the default emotion. </summary>
        public void ResetEmotion() => Emotion = DefaultEmotion;

        /// <inheritdoc cref="ResetEmotion"/>
        /// <remarks> Alias for <see cref="ResetEmotion"/>. </remarks>
        public void ClearEmotion() => ResetEmotion();

        /// <summary> Event signature for the <see cref="MotionChanged"/> event. </summary>
        /// <param name="OldMotion"> The old motion. </param>
        /// <param name="NewMotion"> The new motion. </param>
        /// <param name="Token"> The cancellation token. </param>
        public delegate IEnumerator MotionChangedEventHandler( Motion OldMotion, Motion NewMotion, CoroutineCancellation Token );

        /// <summary> Raised when the motion of the NPC changes. </summary>
        public event MotionChangedEventHandler? MotionChanged;


        /// <summary> Raises the <see cref="MotionChanged"/> event. </summary>
        /// <param name="OldMotion"> The old motion. </param>
        /// <param name="NewMotion"> The new motion. </param>
        /// <param name="Token"> The cancellation token. </param>
        protected virtual IEnumerator PropagateMotionChanged( Motion OldMotion, Motion NewMotion, CoroutineCancellation Token ) {
            if (MotionChanged != null) {
                yield return MotionChanged(OldMotion, NewMotion, Token);
            }
        }

        /// <summary> Gets the NPC's motion. </summary>
        [ShowInInspector, HideInEditorMode, Tooltip("The NPC's motion.")]
        public Motion Motion { get; private set; } = (Motion)((int)default(Motion)+1);

        /// <summary> The queue for motions. </summary>
        protected readonly Queue<Motion> MotionQueue = new();

        CancellableCoroutine? _MotionLoop;

        /// <summary> Pushes a motion to the queue. </summary>
        /// <param name="Motion"> The motion to push. </param>
        /// <param name="Force"> Whether to force the motion to play immediately. </param>
        public virtual void PushMotion( Motion Motion, bool Force = false ) {
            if (Force) {
                MotionQueue.Clear();
                if (_MotionLoop is { } MotionLoop) {
                    MotionLoop.Cancel();
                    _MotionLoop = null;
                }
            }

            MotionQueue.Enqueue(Motion);
            if (_MotionLoop == null) {
                CoroutineCancellationSource Cancellation = new();
                _MotionLoop = new(PlayMotion, Cancellation);
                StartCoroutine(_MotionLoop);
            }
        }

        /// <summary> Plays the motions in the queue. </summary>
        /// <param name="Token"> The cancellation token. </param>
        protected virtual IEnumerator PlayMotion( CoroutineCancellation Token ) {
            if (!MotionQueue.TryDequeue(out Motion NewMotion)) {
                Debug.LogError("Motion queue is empty.", this);
                yield break;
            }

            while (true) {
                Motion OldMotion = Motion;
                Motion = NewMotion;
                yield return PropagateMotionChanged(OldMotion, NewMotion, Token);
                if (Token.IsCancellationRequested) {
                    // Log($"Motion {NewMotion} cancelled.");
                    yield break;
                }

                // If queue is empty, add the last motion back to the queue.
                if (!MotionQueue.TryDequeue(out NewMotion)) {
                    MotionQueue.Enqueue(NewMotion);
                }
            }
        }

        protected virtual void Start() {
            if (EmotionChanged is null) {
                Debug.LogWarning($"No event handler for EmotionChanged. {Key} will not be able to visually display emotions.", this);
            }
            Emotion = default;

            if (MotionChanged is null) {
                Debug.LogWarning($"No event handler for MotionChanged. {Key} will not be able to visually display movements.", this);
            }
            PushMotion(Motion.Idle, true);
        }
    }
}
