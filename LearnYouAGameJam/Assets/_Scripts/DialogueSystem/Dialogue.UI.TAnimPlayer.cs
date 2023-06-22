using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Febucci.UI;
using LYGJ.AudioManagement;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem {
    public sealed class Dialogue_UI_TAnimPlayer : TextAnimatorPlayer {

        [Title("Audio")]
        [SerializeField, Tooltip("The sound effect to use when a character is displayed.")]
        SFX? _CharacterSFX = null;
        [SerializeField, Tooltip("The chance that the character sound effect will play."), EnableIf(nameof(HasCharacterSFX)), Range(0, 1)]
        float _CharacterSFXChance = 1;
        [SerializeField, Tooltip("Whether to interrupt the current audio when a new one is played."), EnableIf(nameof(HasCharacterSFX)), ToggleLeft]
        bool _InterruptAudio = true;
        [SerializeField, Tooltip("The minimum time, in seconds, between character sound effects."), EnableIf(nameof(HasCharacterSFX)), MinValue(0), SuffixLabel("s")]
        float _MinCharacterSFXInterval = 0.1f;

        [MemberNotNullWhen(true, nameof(_CharacterSFX))]
        bool HasCharacterSFX => _CharacterSFX != null;

        float _LastCharacterSFXTime = 0f;

        SFXPlayer?   _Player;
        IEnumerator? _PlayerPlayback;

        /// <summary> Gets or sets the sound effect to use when a character is displayed. </summary>
        public SFX? CharacterSFX {
            get => _CharacterSFX;
            set => _CharacterSFX = value;
        }

        void Awake() {
            _LastCharacterSFXTime = -_MinCharacterSFXInterval;
            onCharacterVisible.AddListener(PlayCharacterSFX);
        }

        void PlayCharacterSFX( char C ) {
            if (!HasCharacterSFX) { return; }
            if (Random.value > _CharacterSFXChance) { return; }
            float Now = Time.time;
            if (Now - _LastCharacterSFXTime < _MinCharacterSFXInterval) { return; }
            _LastCharacterSFXTime = Now;

            if (_InterruptAudio) {
                if (_PlayerPlayback != null) {
                    if (_Player != null) {
                        _Player.StopCoroutine(_PlayerPlayback);
                        _Player.gameObject.SetActive(false);
                    }
                    _PlayerPlayback = null;
                    _Player         = null;
                }
                if (_Player == null) {
                    _Player = Audio.GetOrCreateSFXPlayer();
                }

                AudioClip? Clip = _CharacterSFX.GetRandomClip();
                if (Clip == null) { return; }

                _PlayerPlayback = _Player.PlayCoroutine(Clip, _CharacterSFX.Volume, _CharacterSFX.Pitch, Audio.GetMixerGroup(Mixer.Vox), null);
                _Player.gameObject.SetActive(true);
                _Player.StartCoroutine(_PlayerPlayback);
            } else {
                _CharacterSFX.PlayGlobal();
            }
        }

        #region Overrides of TAnimPlayerBase

        /// <inheritdoc />
        protected override IEnumerator DoCustomAction( TypewriterAction Action ) {
            string Result = Dialogue.Methods.Invoke(Action.actionID, Action.parameters.ToArray());
            if (!string.IsNullOrEmpty(Result)) {
                Debug.LogWarning($"Typewriter action {Action.actionID} returned a non-empty string: '{Result}'. Result was discarded.");
            }

            return new CompletedCoroutine();
        }

        #endregion

    }
}
