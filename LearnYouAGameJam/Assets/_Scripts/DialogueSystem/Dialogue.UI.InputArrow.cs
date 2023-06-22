using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.DialogueSystem {
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class Dialogue_UI_InputArrow : MonoBehaviour {
        [SerializeField, Tooltip("The canvas group."), Required, ChildGameObjectsOnly] CanvasGroup _Group = null!;

        [Space]
        [SerializeField, Tooltip("The time, in seconds, to fade in."), Min(0f), SuffixLabel("s")] float _FadeInTime = 0.25f;
        [SerializeField, Tooltip("The time, in seconds, to fade out."), Min(0f), SuffixLabel("s")] float _FadeOutTime = 0.25f;

        float _WantedAlpha = 0f;

        void Update() {
            float Alpha = _Group.alpha;
            _Group.alpha = Mathf.MoveTowards(Alpha, _WantedAlpha, Time.deltaTime / (_WantedAlpha > Alpha ? _FadeInTime : _FadeOutTime));
        }

        /// <summary> Gets or sets the visibility of the arrow. </summary>
        public bool Visible {
            get => _WantedAlpha > 0f;
            set => _WantedAlpha = value ? 1f : 0f;
        }

        void Awake() => _Group.alpha = _WantedAlpha = 0f;

        void Start() {
            Dialogue.ContinueInputRequested += OnContinueInputRequested;
            Dialogue.ContinueInputReceived  += OnContinueInputReceived;

            void OnContinueInputRequested() => Visible = true;
            void OnContinueInputReceived()  => Visible = false;
        }

        #if UNITY_EDITOR
        [Button("Show"), ButtonGroup("ShowHide"), HideInEditorMode, DisableIf(nameof(Visible))]
        void Editor_Show() => Visible = true;
        [Button("Hide"), ButtonGroup("ShowHide"), HideInEditorMode, EnableIf(nameof(Visible))]
        void Editor_Hide() => Visible = false;

        void Reset() {
            _Group = GetComponent<CanvasGroup>();
        }
        #endif
    }
}
