using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace LYGJ.AudioManagement {
    [AssetsOnly, AssetSelector]
    [CreateAssetMenu(menuName = "The Deliverer/Audio/SFX", fileName = "New SFX", order = 1000)]
    public sealed class SFX : ScriptableObject {
        /// <summary> Gets the audio clips to pick from. </summary>
        [field: SerializeField, Tooltip("The audio clips to pick from."), ListDrawerSettings(DefaultExpandedState = true)]
        public AudioClip[] Clips { get; private set; } = Array.Empty<AudioClip>();

        int _LastIndex = -1;

        /// <summary> Gets the volume to play the clip at. </summary>
        [field: SerializeField, Tooltip("The volume to play the clip at."), Range(0f, 1f)]
        public float Volume { get; private set; } = 1f;

        /// <summary> Gets the random pitch variation to play the clip at. </summary>
        [field: SerializeField, Tooltip("The random pitch variation to play the clip at."), LabelText("Variance"), MinMaxSlider(0f, 2f, true), ToggleGroup(nameof(_UsePitchVariance), "Pitch Variance")]
        public Vector2 PitchVariance { get; private set; } = new(0.9f, 1.1f);

        /// <summary> Gets the pitch to play the clip at. </summary>
        public float Pitch => UsePitchVariance ? Random.Range(PitchVariance.x, PitchVariance.y) : 1f;

        /// <summary> Gets whether or not to use pitch variance. </summary>
        public bool UsePitchVariance => _UsePitchVariance;

        [SerializeField, ToggleGroup(nameof(_UsePitchVariance), "Pitch Variance"), Tooltip("Whether or not to use pitch variance.")]
        bool _UsePitchVariance = true;

        /// <summary> The mixer to play the clip on. </summary>
        [field: SerializeField, Tooltip("The mixer to play the clip on."), OnValueChanged("UpdateFromMixer")]
        public Mixer Mixer { get; private set; } = Mixer.SFX;

        /// <summary> Whether or not to play the clip globally, or in 3D space. </summary>
        [field: SerializeField, Tooltip("Whether or not to play the clip globally, or in 3D space.")]
        public bool Is3D { get; private set; } = false;

        static T? GetRandom<T>( IReadOnlyList<T> List, ref int LastIndex ) {
            int Ln = List.Count;
            switch (Ln) {
                case 0:
                    return default;
                case 1:
                    return List[0];
                default:
                    int Index = Random.Range(0, Ln);
                    if (Index == LastIndex) {
                        Index = (Index + 1) % Ln;
                    }

                    LastIndex = Index;
                    return List[Index];
            }
        }

        void UpdateFromMixer() {
            Is3D              = Mixer is Mixer.SFX;
            _UsePitchVariance = Mixer is Mixer.SFX;
        }

        /// <summary> Plays the clip. </summary>
        /// <param name="Position"> The position to play the clip at. </param>
        public void PlaySelf( Vector3 Position ) {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                Audio.Play(Clip, Mixer, Volume, Pitch, Position);
            }
        }

        /// <summary> Plays the clip. </summary>
        /// <param name="Position"> The position to play the clip at. </param>
        /// <param name="Volume"> The volume to play the clip at. </param>
        /// <param name="IsMultiplied"> Whether or not the volume is multiplied by the <see cref="Volume"/> property, or if it is absolute. </param>
        public void PlaySelf( Vector3 Position, float Volume, bool IsMultiplied = true ) {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                Audio.Play(Clip, Mixer, IsMultiplied ? Volume * this.Volume : Volume, Pitch, Position);
            }
        }

        /// <inheritdoc cref="PlaySelf(Vector3)"/>
        /// <param name="Transform"> The transform to play the clip at. </param>
        /// <remarks> Shorthand for <see cref="PlaySelf(Vector3)"/> with <see cref="Transform.position"/>. This does not follow the transform, and only inherits the initial position. Used for Unity Events. </remarks>
        public void PlaySelf( Transform Transform ) => PlaySelf(Transform.position);

        /// <summary> Plays the clip globally, disregarding the <see cref="Is3D"/> property. </summary>
        public void PlaySelfGlobal() {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                Audio.Play(Clip, Mixer, Volume, Pitch);
            }
        }

        /// <inheritdoc cref="PlaySelfGlobal()"/>
        /// <param name="Volume"> The volume to play the clip at. </param>
        /// <param name="IsMultiplied"> Whether or not the volume is multiplied by the <see cref="Volume"/> property, or if it is absolute. </param>
        public void PlaySelfGlobal( float Volume, bool IsMultiplied = true ) {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                Audio.Play(Clip, Mixer, IsMultiplied ? Volume * this.Volume : Volume, Pitch);
            }
        }

        /// <inheritdoc cref="PlaySelf(Vector3)"/>
        /// <returns> The coroutine. </returns>
        public IEnumerator PlaySelfCoroutine( Vector3 Position ) {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                yield return Audio.PlayCoroutine(Clip, Mixer, Volume, Pitch, Position);
            }
        }

        /// <inheritdoc cref="PlaySelfGlobal()"/>
        /// <returns> The coroutine. </returns>
        public IEnumerator PlaySelfGlobalCoroutine() {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                yield return Audio.PlayCoroutine(Clip, Mixer, Volume, Pitch);
            }
        }

        /// <inheritdoc cref="PlaySelf(Vector3)"/>
        /// <param name="Position"> The position to play the clip at. </param>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> The asynchronous operation. </returns>
        public UniTask PlaySelfAsync( Vector3 Position, CancellationToken Token = default ) {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                return Audio.PlayAsync(Clip, Mixer, Volume, Pitch, Position, Token);
            }

            return Token.IsCancellationRequested ? UniTask.FromCanceled(Token) : UniTask.CompletedTask;
        }

        /// <inheritdoc cref="PlaySelfGlobal()"/>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> The asynchronous operation. </returns>
        public UniTask PlaySelfGlobalAsync( CancellationToken Token = default ) {
            AudioClip? Clip = GetRandom(Clips, ref _LastIndex);
            if (Clip != null) {
                return Audio.PlayAsync(Clip, Mixer, Volume, Pitch, Token: Token);
            }

            return Token.IsCancellationRequested ? UniTask.FromCanceled(Token) : UniTask.CompletedTask;
        }

        /// <summary> Gets a random clip from the <see cref="Clips"/> list. </summary>
        /// <returns> The random clip. </returns>
        public AudioClip? GetRandomClip() => GetRandom(Clips, ref _LastIndex);

        #if UNITY_EDITOR
        static string GetFolder( string AssetPath ) {
            int Index = AssetPath.LastIndexOf('/');
            return Index == -1 ? AssetPath : AssetPath[..Index];
        }
        static string GetAssetName( string AssetPath ) {
            // If the path contains an underscore in the last part, remove it and everything after it
            // Replace the extension with ".asset"
            // Finally, call AssetDatabase.GenerateUniqueAssetPath to ensure the name is unique
            int LastSlash      = AssetPath.LastIndexOf('/');
            int LastUnderscore = AssetPath.LastIndexOf('_');
            if (LastUnderscore > LastSlash) {
                AssetPath = AssetPath[..LastUnderscore];
            }

            int LastPeriod = AssetPath.LastIndexOf('.');
            if (LastPeriod > LastSlash) {
                AssetPath = AssetPath[..LastPeriod];
            }

            return AssetDatabase.GenerateUniqueAssetPath($"{AssetPath}.asset");
        }
        static Mixer AttemptDetectMixer( string AssetPath ) {
            // Check each folder in the path (in reverse order) for a mixer name (i.e. "Assets/Sounds/Music" -> "Music")
            Dictionary<string, Mixer> Mixers  = Enum.GetValues(typeof(Mixer)).Cast<Mixer>().ToDictionary(X => X.ToString().ToLowerInvariant());
            string[]                  Folders = AssetPath.Split('/');
            for (int I = Folders.Length - 1; I >= 0; I--) {
                string Folder = Folders[I].ToLowerInvariant();
                if (Mixers.TryGetValue(Folder, out Mixer Mixer)) {
                    return Mixer;
                }
            }

            // Attempt 2: Instead of direct matches, check if it contains the name (i.e. "Assets/Sounds/GUI" -> "UI")
            foreach (KeyValuePair<string, Mixer> Pair in Mixers) {
                if (AssetPath.Contains(Pair.Key, StringComparison.OrdinalIgnoreCase)) {
                    return Pair.Value;
                }
            }

            // If no folder matches, fallback to SFX
            return Mixer.SFX;
        }

        static int _LastCalledFrame = -1; // If multiple clips are selected, this will be called multiple times in the same frame. Use this to reject the second call

        // "AudioClip"([]) -> SFX context menu option
        [MenuItem("CONTEXT/AudioClip/Create SFX")]
        static void CreateSFX( MenuCommand Command ) {
            int CurrentFrame = Time.frameCount;
            if (CurrentFrame == _LastCalledFrame) {
                return;
            }

            _LastCalledFrame = CurrentFrame;
            // If multiple are selected, combine into a single SFX
            // If one is selected, check for similar named files in the same directory (i.e. Clip_01, Clip_02, etc.)
            // If none are selected, this is impossible (since its a context menu option)

            AudioClip[] Clips = Selection.GetFiltered<AudioClip>(SelectionMode.Assets);
            int         Ln    = Clips.Length;
            switch (Ln) {
                case 0: {
                    if (Command.context is AudioClip Clip) {
                        string Path = AssetDatabase.GetAssetPath(Clip);
                        SFX    SFX  = CreateInstance<SFX>();
                        SFX.Clips = new[] { Clip };
                        SFX.Mixer = AttemptDetectMixer(Path);
                        SFX.UpdateFromMixer();
                        ProjectWindowUtil.CreateAsset(SFX, GetAssetName(Path));
                    } else {
                        Debug.LogError("No audio clip selected.");
                    }

                    break;
                }
                case 1 when Clips[0].name.Contains('_'): {
                    AudioClip Clip = Clips[0];
                    string    Path = AssetDatabase.GetAssetPath(Clip);

                    // Determine the name (all text until the first underscore)
                    string Name = Clip.name.Split('_')[0];

                    // Grab all the files with the same name (all text until the first underscore)
                    // Then sort alphabetically
                    // Then finally, create a new SFX with the same name

                    AudioClip[] InFolder = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AudioClip>().Where(A => A.name.StartsWith(Name)).ToArray();
                    Array.Sort(InFolder, ( A, B ) => string.Compare(A.name, B.name, StringComparison.Ordinal));

                    SFX SFX = CreateInstance<SFX>();
                    SFX.Clips = InFolder;
                    SFX.Mixer = AttemptDetectMixer(Path);
                    SFX.UpdateFromMixer();
                    ProjectWindowUtil.CreateAsset(SFX, GetAssetName(Path));

                    break;
                }
                default: { // Both one clip (without an underscore) and multiple clips are the same case. In both cases a new SFX is created with the name of the first clip, and all selected clips are added to it.
                    // Create a new SFX with the same name
                    string Path = AssetDatabase.GetAssetPath(Clips[0]);
                    SFX    SFX  = CreateInstance<SFX>();
                    Array.Sort(Clips, ( A, B ) => string.Compare(A.name, B.name, StringComparison.Ordinal));
                    SFX.Clips = Clips;
                    SFX.Mixer = AttemptDetectMixer(Path);
                    SFX.UpdateFromMixer();
                    ProjectWindowUtil.CreateAsset(SFX, GetAssetName(Path));
                    break;
                }
            }
        }
        #endif
    }

    public static class SFXExtensions {
        /// <inheritdoc cref="SFX.PlaySelf(Vector3)"/>
        public static void Play( this SFX? SFX, Vector3 Position ) {
            if (SFX != null) {
                SFX.PlaySelf(Position);
            }
        }

        /// <inheritdoc cref="SFX.PlaySelfGlobal()"/>
        public static void PlayGlobal( this SFX? SFX ) {
            if (SFX != null) {
                SFX.PlaySelfGlobal();
            }
        }

        /// <inheritdoc cref="SFX.PlaySelfCoroutine"/>
        public static IEnumerator PlayCoroutine( this SFX? SFX, Vector3 Position ) {
            if (SFX != null) {
                yield return SFX.PlaySelfCoroutine(Position);
            }
        }

        /// <inheritdoc cref="SFX.PlaySelfGlobalCoroutine"/>
        public static IEnumerator PlayGlobalCoroutine( this SFX? SFX ) {
            if (SFX != null) {
                yield return SFX.PlaySelfGlobalCoroutine();
            }
        }

        /// <inheritdoc cref="SFX.PlaySelfAsync"/>
        public static UniTask PlayAsync( this SFX? SFX, Vector3 Position, CancellationToken Token = default ) =>
            SFX != null
                ? SFX.PlaySelfAsync(Position, Token)
                : Token.IsCancellationRequested
                    ? UniTask.FromCanceled(Token)
                    : UniTask.CompletedTask;

        /// <inheritdoc cref="SFX.PlaySelfGlobalAsync"/>
        public static UniTask PlayGlobalAsync( this SFX? SFX, CancellationToken Token = default ) =>
            SFX != null
                ? SFX.PlaySelfGlobalAsync(Token)
                : Token.IsCancellationRequested
                    ? UniTask.FromCanceled(Token)
                    : UniTask.CompletedTask;
    }
}
