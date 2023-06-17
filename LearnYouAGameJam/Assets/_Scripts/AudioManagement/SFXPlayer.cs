using System.Collections;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace LYGJ.AudioManagement {
    [RequireComponent(typeof(AudioSource))]
    public sealed class SFXPlayer : MonoBehaviour {
        [SerializeField, Tooltip("The audio source."), Required, ChildGameObjectsOnly] AudioSource _AudioSource;

        #if UNITY_EDITOR
        void Reset() {
            _AudioSource = GetComponentInChildren<AudioSource>();
            if (_AudioSource == null) {
                _AudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        #endif

        /// <summary> Plays the given audio clip. </summary>
        /// <param name="Clip"> The clip to play. </param>
        /// <param name="Volume"> The volume to play the clip at. </param>
        /// <param name="Pitch"> The pitch to play the clip at. </param>
        /// <param name="MixerGroup"> The mixer group to play the clip on. </param>
        /// <param name="Position"> The position to play the clip at. <see langword="null"/> to play globally. </param>
        public void Play( AudioClip Clip, float Volume = 1, float Pitch = 1, AudioMixerGroup? MixerGroup = null, Vector3? Position = null ) => StartCoroutine(PlayCoroutine(Clip, Volume, Pitch, MixerGroup, Position));

        /// <inheritdoc cref="Play(AudioClip,float,float,AudioMixerGroup?,Vector3?)"/>
        /// <returns> The coroutine. </returns>
        public IEnumerator PlayCoroutine( AudioClip Clip, float Volume, float Pitch, AudioMixerGroup? MixerGroup, Vector3? Position ) {
            SetupAndStart(Clip, Volume, Pitch, MixerGroup, Position);
            yield return DeactivateAfter(Clip.length);
        }

        IEnumerator DeactivateAfter( float Seconds ) {
            yield return new WaitForSeconds(Seconds);
            while (_AudioSource.isPlaying) {
                yield return null;
            }
            gameObject.SetActive(false);
        }

        void SetupAndStart( AudioClip Clip, float Volume, float Pitch, AudioMixerGroup? MixerGroup, Vector3? Position ) {
            _AudioSource.clip = Clip;
            _AudioSource.volume                = Volume;
            _AudioSource.pitch                 = Pitch;
            _AudioSource.outputAudioMixerGroup = MixerGroup;
            if (Position != null) {
                _AudioSource.spatialBlend       = 1;
                _AudioSource.transform.position = Position.Value;
            } else {
                _AudioSource.spatialBlend = 0;
            }
            _AudioSource.Play();
        }

        /// <inheritdoc cref="Play(AudioClip,float,float,AudioMixerGroup?,Vector3?)"/>
        /// <returns> The asynchronous operation. </returns>
        public async UniTask PlayAsync( AudioClip Clip, float Volume, float Pitch, AudioMixerGroup? MixerGroup, Vector3? Position, CancellationToken Token ) {
            SetupAndStart(Clip, Volume, Pitch, MixerGroup, Position);

            await UniTask.Delay((int)(Clip.length * 1000), cancellationToken: Token).SuppressCancellationThrow();
            if (!Token.IsCancellationRequested) {
                bool Predicate() => _AudioSource == null || !_AudioSource.isPlaying;
                await UniTask.WaitUntil(Predicate, cancellationToken: Token).SuppressCancellationThrow();
            }

            try {
                Pool.Return(this);
            } catch(MissingReferenceException) { } // Pooling is not possible if the object is destroyed.
        }

    }
}
