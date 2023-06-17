using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using Debug = UnityEngine.Debug;

namespace LYGJ.AudioManagement {
    public sealed class Audio : SingletonMB<Audio> {

        const string _Resource_SFXPlayer = "Audio/SFX Player";
        const string _Resource_Mixer = "Audio/Mixer";

        static SFXPlayer Prefab
            #if !UNITY_EDITOR
            { get; } = Resources.Load<SFXPlayer>(_Resource_SFXPlayer);
            #else
            => _Prefab.Value;
            #endif

        #if UNITY_EDITOR
        static readonly ResettableLazy<SFXPlayer> _Prefab = new(() => Resources.Load<SFXPlayer>(_Resource_SFXPlayer));

        [ExecuteOnReload]
        static void OnReload() => _Prefab.Reset();
        #endif

        static readonly ResettableLazy<IReadOnlyDictionary<Mixer, AudioMixerGroup?>> _MixerGroups = new(() => {
            AudioMixer Mixer = Resources.Load<AudioMixer>(_Resource_Mixer);
            Array Values = Enum.GetValues(typeof(Mixer));
            Dictionary<Mixer, AudioMixerGroup?> MixerGroups = new(Values.Length);
            if (Mixer == null) {
                Debug.LogError("No audio mixer found.");
                foreach (Mixer Value in Values) {
                    MixerGroups.Add(Value, null);
                }
            } else {
                foreach (Mixer Value in Values) {
                    AudioMixerGroup[] Groups = Mixer.FindMatchingGroups(Value.ToString());
                    if (Groups.Length == 0) {
                        Debug.LogWarning($"No mixer group found for {Value}.");
                        MixerGroups.Add(Value, null);
                    } else {
                        MixerGroups.Add(Value, Groups[0]);
                    }
                }
            }
            return MixerGroups;
        });

        /// <summary> Gets the mixer group for the given mixer. </summary>
        /// <param name="Mixer"> The mixer to get the mixer group for. </param>
        /// <returns> The mixer group. </returns>
        public static AudioMixerGroup? GetMixerGroup( Mixer Mixer ) => _MixerGroups.Value[Mixer];

        /// <summary> Plays the given audio clip. </summary>
        /// <param name="Clip"> The clip to play. </param>
        /// <param name="MixerGroup"> The mixer group to play the clip on. </param>
        /// <param name="Volume"> The volume to play the clip at. </param>
        /// <param name="Pitch"> The pitch to play the clip at. </param>
        /// <param name="Position"> The position to play the clip at. <see langword="null"/> to play globally. </param>
        public static void Play( AudioClip Clip, AudioMixerGroup? MixerGroup, float Volume = 1f, float Pitch = 1f, Vector3? Position = null ) {
            SFXPlayer Player = Pool<SFXPlayer>.Get(Instance.transform, Prefab);
            Player.Play(Clip, Volume, Pitch, MixerGroup, Position);
        }

        /// <inheritdoc cref="Play(AudioClip,AudioMixerGroup,float,float,Vector3?)"/>
        public static void Play( AudioClip Clip, Mixer Mixer = Mixer.Master, float Volume = 1f, float Pitch = 1f, Vector3? Position = null ) => Play(Clip, GetMixerGroup(Mixer), Volume, Pitch, Position);

        /// <inheritdoc cref="Play(AudioClip,AudioMixerGroup,float,float,Vector3?)"/>
        /// <returns> The coroutine. </returns>
        public static IEnumerator PlayCoroutine( AudioClip Clip, AudioMixerGroup? MixerGroup, float Volume = 1f, float Pitch = 1f, Vector3? Position = null ) {
            SFXPlayer Player = Pool<SFXPlayer>.Get(Instance.transform, Prefab);
            yield return Player.PlayCoroutine(Clip, Volume, Pitch, MixerGroup, Position);
        }

        /// <inheritdoc cref="PlayCoroutine(AudioClip,AudioMixerGroup,float,float,Vector3?)"/>
        public static IEnumerator PlayCoroutine( AudioClip Clip, Mixer Mixer = Mixer.Master, float Volume = 1f, float Pitch = 1f, Vector3? Position = null ) => PlayCoroutine(Clip, GetMixerGroup(Mixer), Volume, Pitch, Position);

        /// <inheritdoc cref="Play(AudioClip,AudioMixerGroup,float,float,Vector3?)"/>
        /// <returns> The asynchronous operation. </returns>
        public static UniTask PlayAsync( AudioClip Clip, AudioMixerGroup? MixerGroup, float Volume = 1f, float Pitch = 1f, Vector3? Position = null, CancellationToken Token = default ) {
            SFXPlayer Player = Pool<SFXPlayer>.Get(Instance.transform, Prefab);
            return Player.PlayAsync(Clip, Volume, Pitch, MixerGroup, Position, Token);
        }

        /// <inheritdoc cref="PlayCoroutine(AudioClip,AudioMixerGroup,float,float,Vector3?)"/>
        public static UniTask PlayAsync( AudioClip Clip, Mixer Mixer = Mixer.Master, float Volume = 1f, float Pitch = 1f, Vector3? Position = null, CancellationToken Token = default ) => PlayAsync(Clip, GetMixerGroup(Mixer), Volume, Pitch, Position, Token);

        #if UNITY_EDITOR
        [ExecuteOnReload]
        static void Editor_Cleanup() {
            _MixerGroups.Reset();
            if (_Instance != null) {
                Pool<SFXPlayer>.Purge(_Instance.transform);
            }
        }
        #endif

        /// <summary> Gets the source used to play <see cref="Stems.Melody"/> stems. </summary>
        [field: SerializeField, Title("Music"), Tooltip("The source used to play 'Melody' stems."), Required, ChildGameObjectsOnly] public AudioSource MelodySource { get; private set; } = null!;

        /// <summary> Gets the source used to play <see cref="Stems.Instruments"/> stems. </summary>
        [field: SerializeField, Tooltip("The source used to play 'Instruments' stems."), Required, ChildGameObjectsOnly] public AudioSource InstrumentSource { get; private set; } = null!;

        /// <summary> Gets the source used to play <see cref="Stems.Bass"/> stems. </summary>
        [field: SerializeField, Tooltip("The source used to play 'Bass' stems."), Required, ChildGameObjectsOnly] public AudioSource BassSource { get; private set; } = null!;

        /// <summary> Gets the source used to play <see cref="Stems.Drums"/> stems. </summary>
        [field: SerializeField, Tooltip("The source used to play 'Drums' stems."), Required, ChildGameObjectsOnly] public AudioSource DrumSource { get; private set; } = null!;

        /// <summary> Gets or sets the time, in seconds, to fade in music. </summary>
        [field: SerializeField, Tooltip("The time, in seconds, to fade in music."), MinValue(0f), SuffixLabel("s")] public float FadeInTime { get; set; } = 1f;

        /// <summary> Gets or sets the time, in seconds, to fade out music. </summary>
        [field: SerializeField, Tooltip("The time, in seconds, to fade out music."), MinValue(0f), SuffixLabel("s")] public float FadeOutTime { get; set; } = 1f;

        float _Volume = 1f, _MelodyVolume = 1f, _InstrumentsVolume = 1f, _BassVolume = 1f, _DrumVolume = 1f;

        static void Fade( ref float Value, float Target, float FadeIn, float FadeOut, float DeltaTime ) => Value = Mathf.MoveTowards(Value, Target, DeltaTime / (Target > Value ? FadeIn : FadeOut));

        void Update() {
            float Delta = Time.deltaTime;
            Fade(ref _Volume, Volume, FadeInTime, FadeOutTime, Delta);

            Fade(ref _MelodyVolume, _MelodyWantedVolume, FadeInTime, FadeOutTime, Delta);
            MelodySource.volume = _Volume * _MelodyVolume;
            Fade(ref _InstrumentsVolume, _InstrumentsWantedVolume, FadeInTime, FadeOutTime, Delta);
            InstrumentSource.volume = _Volume * _InstrumentsVolume;
            Fade(ref _BassVolume, _BassWantedVolume, FadeInTime, FadeOutTime, Delta);
            BassSource.volume = _Volume * _BassVolume;
            Fade(ref _DrumVolume, _DrumWantedVolume, FadeInTime, FadeOutTime, Delta);
            DrumSource.volume = _Volume * _DrumVolume;

            if (!_Playing) {
                if (_Volume * _MelodyVolume      == 0f) { MelodySource.Stop(); }
                if (_Volume * _InstrumentsVolume == 0f) { InstrumentSource.Stop(); }
                if (_Volume * _BassVolume        == 0f) { BassSource.Stop(); }
                if (_Volume * _DrumVolume        == 0f) { DrumSource.Stop(); }
            }
        }

        float _MelodyWantedVolume = 1f, _InstrumentsWantedVolume = 1f, _BassWantedVolume = 1f, _DrumWantedVolume = 1f;

        /// <summary> Gets or sets the intended volume of music. </summary>
        [ShowInInspector, Tooltip("The intended volume of music."), HideInEditorMode]
        public float Volume { get; set; }

        /// <summary> Gets or sets the intended volume of the given stem(s). </summary>
        /// <param name="Stems"> The stem(s) to set the volume of. </param>
        /// <value> The intended volume of the given stem(s). </value>
        /// <returns> The intended volume of the given stem(s). </returns>
        public float this[ Stems Stems ] {
            get {
                float Volume = 0f;
                if ((Stems & Stems.Melody)      != 0) { Volume += _MelodyWantedVolume; }
                if ((Stems & Stems.Instruments) != 0) { Volume += _InstrumentsWantedVolume; }
                if ((Stems & Stems.Bass)        != 0) { Volume += _BassWantedVolume; }
                if ((Stems & Stems.Drums)       != 0) { Volume += _DrumWantedVolume; }

                return Volume;
            }
            set {
                if ((Stems & Stems.Melody)      != 0) { _MelodyWantedVolume      = value; }
                if ((Stems & Stems.Instruments) != 0) { _InstrumentsWantedVolume = value; }
                if ((Stems & Stems.Bass)        != 0) { _BassWantedVolume        = value; }
                if ((Stems & Stems.Drums)       != 0) { _DrumWantedVolume        = value; }
            }
        }

        /// <summary> Gets the intended volume of the given stem(s). </summary>
        /// <param name="Stems"> The stem(s) to get the volume of. </param>
        /// <returns> The intended volume of the given stem(s). </returns>
        public float GetStemVolume( Stems Stems ) => this[Stems];

        /// <summary> Sets the intended volume of the given stem(s). </summary>
        /// <param name="Stems"> The stem(s) to set the volume of. </param>
        /// <param name="Volume"> The intended volume of the given stem(s). </param>
        public void SetStemVolume( Stems Stems, float Volume ) => this[Stems] = Volume;

        /// <summary> Gets or sets whether music is currently playing. </summary>
        [ShowInInspector, Tooltip("Whether music is currently playing."), HideInEditorMode]
        public bool Playing {
            get => _Playing;
            set {
                _Playing      = value;
                Volume = value ? 1f : 0f;
            }
        }

        bool _Playing;

        /// <summary> Plays the given music. </summary>
        /// <param name="Music"> The music to play. </param>
        /// <param name="Immediate"> Whether to play the music immediately, or first fade out the current music. <br/>
        /// Note, even if this is <see langword="false"/>, if no other music is playing, the music will be played immediately without fading. </param>
        public void StartMusic( Music Music, bool Immediate = false ) {
            void Switch() {
                MelodySource.Stop();
                InstrumentSource.Stop();
                BassSource.Stop();
                DrumSource.Stop();

                MelodySource.clip     = Music.Melody;
                InstrumentSource.clip = Music.Instruments;
                BassSource.clip       = Music.Bass;
                DrumSource.clip       = Music.Drums;

                MelodySource.Play();
                InstrumentSource.Play();
                BassSource.Play();
                DrumSource.Play();
            }

            if (Music == null) { return; }
            if (Immediate || !_Playing) {
                _Playing = true;
                Volume = 1f;
                Switch();
            } else {
                IEnumerator FadeOut() {
                    Volume = 0f;
                    bool Predicate() => _Volume == 0f;
                    yield return new WaitUntil(Predicate);

                    Switch();
                    Volume = 1f;
                }
                StartCoroutine(FadeOut());
            }
        }

        /// <inheritdoc cref="StartMusic(Music,bool)"/>
        public static void PlayMusic( Music Music, bool Immediate = false ) => Instance.StartMusic(Music, Immediate);

        /// <summary> Stops the current music. </summary>
        /// <param name="Immediate"> Whether to stop the music immediately, or first fade out the current music. </param>
        public void HaltMusic( bool Immediate = false ) {
            if (Immediate) {
                _Playing = false;
                _Volume  = Volume = _MelodyWantedVolume = _InstrumentsWantedVolume = _BassWantedVolume = _DrumWantedVolume = 0f;

                MelodySource.Stop();
                MelodySource.volume = 0f;
                InstrumentSource.Stop();
                InstrumentSource.volume = 0f;
                BassSource.Stop();
                BassSource.volume = 0f;
                DrumSource.Stop();
                DrumSource.volume = 0f;
            } else {
                IEnumerator FadeOut() {
                    Volume = 0f;
                    bool Predicate() => _Volume == 0f;
                    yield return new WaitUntil(Predicate);
                    _Playing = false;
                }
                StartCoroutine(FadeOut());
            }
        }

        /// <inheritdoc cref="HaltMusic(bool)"/>
        public static void StopMusic( bool Immediate = false ) => Instance.HaltMusic(Immediate);

    }

}
