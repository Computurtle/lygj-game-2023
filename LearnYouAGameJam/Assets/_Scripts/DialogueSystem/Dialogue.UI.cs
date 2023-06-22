using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Febucci.UI;
using LYGJ.Common;
using LYGJ.Common.Attributes;
using LYGJ.EntitySystem.PlayerManagement;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem {
    public sealed class Dialogue_UI : SingletonMB<Dialogue_UI> {

        #region Overrides of SingletonMB<Dialogue_UI>

        /// <inheritdoc />
        protected override void Awake() {
            base.Awake();

            Dialogue.Started          += OnDialogueStarted;
            Dialogue.Ended            += OnDialogueEnded;
            Dialogue.TextDisplayed    += OnTextDisplayed;
            // Dialogue.TextCleared      += OnTextCleared;
            Dialogue.ChoicesDisplayed += OnChoicesDisplayed;
            Dialogue.ChoicesCleared   += OnChoicesCleared;

            _VisibleHash = Animator.StringToHash(_VisibleParameter);
            _ChoicesHash = Animator.StringToHash(_ChoicesParameter);
            _ShakeHash   = Animator.StringToHash(_ShakeParameter);

            Pool<Button>.ReturnAll(_ChoiceContainer);

            _ContinueButton.onClick.AddListener(SendInput);
        }

        void Start() => PlayerInput.DialogueContinue.Pressed += SendInput;

        /// <inheritdoc />
        protected override void OnDestroy() {
            base.OnDestroy();

            Dialogue.Started          -= OnDialogueStarted;
            Dialogue.Ended            -= OnDialogueEnded;
            Dialogue.TextDisplayed    -= OnTextDisplayed;
            // Dialogue.TextCleared      -= OnTextCleared;
            Dialogue.ChoicesDisplayed -= OnChoicesDisplayed;
            Dialogue.ChoicesCleared   -= OnChoicesCleared;
        }

        #endregion

        [SerializeField, Tooltip("The button to press to continue the dialogue."), Required, ChildGameObjectsOnly] Button _ContinueButton = null!;

        [Space]
        [SerializeField, Tooltip("The name to display for unknown speakers.")] string _UnknownSpeakerName = "???";

        [Title("Animations")]
        [SerializeField, Tooltip("The animator component."), Required, ChildGameObjectsOnly] Animator _Animator = null!;

        [Space]
        [SerializeField, AnimParam, Tooltip("The 'visible' boolean parameter.")] string _VisibleParameter = "Visible";
        [SerializeField, AnimParam, Tooltip("The 'choices' boolean parameter.")] string _ChoicesParameter = "Choices";
        [SerializeField, AnimParam, Tooltip("The 'shake' trigger parameter.")]   string _ShakeParameter   = "Shake";

        [Space]
        [SerializeField, Tooltip("The time, in seconds, for the dialogue text to be shown."), MinValue(0f), SuffixLabel("s")]
        float _ShowTime = 1f;
        [SerializeField, Tooltip("The time, in seconds, for the dialogue text to be hidden."), MinValue(0f), SuffixLabel("s")]
        float _HideTime = 1f;

        int _VisibleHash, _ChoicesHash, _ShakeHash;

        bool Anim_Visible => _Animator.GetBool(_VisibleHash);
        UniTask SetVisible( bool Visible, CancellationToken Token = default ) {
            if (Anim_Visible == Visible) { return UniTask.CompletedTask; }
            _Animator.SetBool(_VisibleHash, Visible);
            return UniTask.Delay(TimeSpan.FromSeconds(Visible ? _ShowTime : _HideTime), cancellationToken: Token);
        }
        bool Anim_Choice {
            get => _Animator.GetBool(_ChoicesHash);
            set => _Animator.SetBool(_ChoicesHash, value);
        }
        void Anim_Shake() => _Animator.SetTrigger(_ShakeHash);

        [Title("Text")]
        [SerializeField, Tooltip("The text component for the speaker's name."), Required, ChildGameObjectsOnly]
        TMP_Text _SpeakerText = null!;
        [SerializeField, Tooltip("The format for the speaker's name.")] string _SpeakerFormat = "{0}:";
        [SerializeField, Tooltip("The text animator for the dialogue."), Required, ChildGameObjectsOnly]
        Dialogue_UI_TAnimPlayer _DialogueText = null!;
        [SerializeField, Tooltip("The lookup for character voices."), AssetsOnly, AssetSelector]
        VoiceCollectionLookup? _VoiceLookup = null;

        string SpeakerText {
            set => _SpeakerText.text = string.Format(_SpeakerFormat, value);
        }

        static UniTask HideTextAsync( TextAnimatorPlayer Animator ) => Animator.DisappearTextRoutine().ToUniTask();

        static UniTask ShowTextAsync( TextAnimatorPlayer Animator, string Text, CancellationToken Token ) {
            IEnumerator Routine = Animator.ShowTextRoutine(Text, out Action Cancel);
            Token.Register(Cancel);
            // ReSharper disable once MethodSupportsCancellation
            return Routine.ToUniTask();
        }

        async UniTask DisplayText( string Speaker, bool SpeakerKnown, string Text, NPCDescriptor? Descriptor, CancellationToken Token ) {
            Anim_Choice = false;
            if (!SpeakerKnown) {
                Speaker = _UnknownSpeakerName;
            } else if (Descriptor != null) {
                Speaker = Descriptor.Name;
            }

            if (_VoiceLookup != null) {
                if (Descriptor != null) {
                    _DialogueText.CharacterSFX = _VoiceLookup[Descriptor.Voice];
                }
            } else {
                _DialogueText.CharacterSFX = null;
            }
            SpeakerText = Speaker;
            Token = CancellationTokenSource.CreateLinkedTokenSource(Token, this.GetCancellationTokenOnDestroy()).Token;
            await SetVisible(true, Token);
            await ShowTextAsync(_DialogueText, Text, Token);
        }

        // UniTask ClearText() => HideTextAsync(_DialogueText);

        [Title("Choices")]
        [SerializeField, Tooltip("The container for the choices."), Required, ChildGameObjectsOnly] RectTransform _ChoiceContainer = null!;
        [SerializeField, Tooltip("The prefab for choices."), Required, AssetsOnly] Dialogue_UI_Choice _ChoicePrefab = null!;

        async UniTask<int> SpawnChoices( IEnumerable<string> Choices ) {
            Anim_Choice = true;
            UniTaskCompletionSource<int> Task    = new();
            List<Dialogue_UI_Choice>     Buttons = new();

            int I = 0;
            foreach (string? Choice in Choices) {
                Dialogue_UI_Choice Button = Pool<Dialogue_UI_Choice>.Get(_ChoicePrefab, _ChoiceContainer);
                Button.Setup(Choice, I, Idx => Task.TrySetResult(Idx));
                Buttons.Add(Button);
                if (I == 0) {
                    Button.Focus();
                }
                I++;
            }
            CancellationToken Token = this.GetCancellationTokenOnDestroy();
            await SetVisible(true, Token);
            (_, int Result) = await Task.Task.AttachExternalCancellation(Token).SuppressCancellationThrow();
            foreach (Dialogue_UI_Choice Button in Buttons) {
                Button.Clear();
                Pool<Dialogue_UI_Choice>.Return(Button);
            }
            // Debug.Log($"Choice {Result} selected", this);
            return Result;
        }

        async UniTask ClearChoices() {
            await SetVisible(false, this.GetCancellationTokenOnDestroy());
            Pool<Button>.ReturnAll(_ChoiceContainer);
        }

        static void OnDialogueStarted( DialogueChain Chain ) {
            Pointer.SetVisible(PointerPriority.Dialogue);
            PlayerMotor.SetCanMove(MotorPriority.Dialogue, false);
            PlayerInteractor.SetCanInteract(InteractionPriority.Dialogue, false);
            PlayerHealth.SetInvulnerable(HealthPriority.Dialogue);
        }

        void OnDialogueEnded( DialogueChain Chain, int Exit ) {
            async UniTask Hide( CancellationToken Token ) {
                // IEnumerator Routine = _DialogueText.DisappearTextRoutine(out Action Cancel);
                // Token.Register(Cancel);
                // // ReSharper disable once MethodSupportsCancellation
                // await Routine.ToUniTask();

                await SetVisible(false, Token);
                Pool<Button>.ReturnAll(_ChoiceContainer);

                Pointer.ClearVisible(PointerPriority.Dialogue);
                PlayerMotor.ClearCanMove(MotorPriority.Dialogue);
                PlayerInteractor.ClearCanInteract(InteractionPriority.Dialogue);
                PlayerHealth.ClearInvulnerable(HealthPriority.Dialogue);
            }
            Hide(this.GetCancellationTokenOnDestroy()).Forget();
        }

        UniTask OnTextDisplayed( DialogueObject DialogueObject, string Speaker, bool SpeakerKnown, string Text, CancellationToken Token ) {
            if (!NPCDescriptor.TryGet(Speaker, out NPCDescriptor? Descriptor)) {
                Debug.LogWarning($"No descriptor for speaker '{Speaker}'. Without a descriptor, the speaker's name will be displayed as the key of the NPC as opposed to a proper display name. Create an NPC Descriptor for '{Speaker}' in the Resources/NPCs folder to fix this.", this);
            }
            return DisplayText(Speaker, SpeakerKnown, Text, Descriptor, Token);
        }

        // UniTask OnTextCleared( DialogueObject DialogueObject, string Speaker ) => ClearText();

        async UniTask OnChoicesDisplayed( IReadOnlyList<DialogueChoiceOption> Choices, DynamicTaskReturn<int> Chosen ) {
            int C = await SpawnChoices(Choices.Select(Choice => Choice.Text));
            Chosen.Lock(C);
        }

        UniTask OnChoicesCleared() => ClearChoices();

        [Button, HideInEditorMode]
        void SendInput() {
            if (!Dialogue.SkipAnimation()) {
                if (Dialogue.Continue()) {
                    // Anim_Shake();
                }
            }
        }

    }
}
