using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.AudioManagement {
    public sealed class SceneMusicPlayer : MonoBehaviour {

        public enum PlayMode {
            None  = -1,
            Awake = 0,
            Start = 1
        }

        [SerializeField, Tooltip("The timing to play the music.")] PlayMode _PlayMode = PlayMode.Start;

        [Space]
        [SerializeField, Tooltip("The music to play."), Required, AssetsOnly, AssetSelector, EnableIf(nameof(Editor_WillPlay)), DisableInPlayMode]
        Music _Music = null!;

        [SerializeField, Tooltip("The stems to play."), EnableIf(nameof(Editor_WillPlay)), DisableInPlayMode]
        Stems _Stems = Stems.FullMix;

        [SerializeField, Tooltip("Whether to fade out the previous music."), EnableIf(nameof(Editor_WillPlay)), DisableInPlayMode]
        bool _Immediate = false;

        /// <summary> Plays the music. </summary>
        public void Play() => _Music.Play(_Stems, _Immediate);

        void Awake() { if (_PlayMode == PlayMode.Awake) { Play(); } }
        void Start() { if (_PlayMode == PlayMode.Start) { Play(); } }

        #if UNITY_EDITOR
        bool Editor_WillPlay => _PlayMode is not PlayMode.None;

        [Button("Play"), HideInEditorMode, Title("Editor Helpers")]
        static void Editor_PlayMusic( [Required, AssetsOnly, AssetSelector] Music Music, Stems Stems = Stems.FullMix, bool Immediate = false ) => Music.Play(Stems, Immediate);

        [Button("Stop"), HideInEditorMode]
        static void Editor_StopMusic( bool Immediate = false ) => Audio.Instance.HaltMusic(Immediate);

        [Button("Set Stems"), HideInEditorMode]
        static void Editor_SetStems( Stems Stems = Stems.FullMix, [PropertyRange(0f, 1f)] float Volume = 1f ) => Audio.Instance[Stems] = Volume;
        #endif

    }
}
