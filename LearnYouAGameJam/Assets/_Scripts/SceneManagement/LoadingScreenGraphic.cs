using System;
using System.Collections;
using System.Diagnostics;
using LYGJ.Common;
using LYGJ.Common.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ.SceneManagement {
    public sealed class LoadingScreenGraphic : MonoBehaviour {
        [SerializeField, Tooltip("The sprite renderer for the background."), Required, ChildGameObjectsOnly]
        Image _Background = null!;
        [SerializeField, Tooltip("The text for the tip."), Required, ChildGameObjectsOnly]
        TMP_Text _Text = null!;

        [Title("Animations")]
        [SerializeField, Tooltip("The animator."), Required, ChildGameObjectsOnly]
        Animator _Animator = null!;
        [SerializeField, Tooltip("The 'visible' boolean parameter."), AnimParam]
        string _VisibleParam = "Visible";
        [SerializeField, Tooltip("The duration, in seconds, of the hide animation."), SuffixLabel("s")]
        float _HideDuration = 0.5f;

        int _VisibleParamHash;

        #if UNITY_EDITOR
        void Reset() {
            _Background = GetComponentInChildren<Image>();
            _Text       = GetComponentInChildren<TMP_Text>();
            _Animator   = GetComponentInChildren<Animator>();
        }
        #endif

        void Awake() => _VisibleParamHash = Animator.StringToHash(_VisibleParam);

        /// <summary> Sets the background and tip. </summary>
        /// <param name="Background"> The background to set. </param>
        /// <param name="Tip"> The tip to set. </param>
        /// <param name="Duration"> The duration to set. </param>
        /// <param name="Callback"> The callback to invoke when the graphic is about to be hidden. </param>
        public void Set( Sprite? Background, string? Tip, float Duration, Action Callback ) {
            if (Background == null) {
                _Background.enabled = false;
            } else {
                _Background.enabled = true;
                _Background.sprite  = Background;
            }
            if (string.IsNullOrEmpty(Tip)) {
                _Text.enabled = false;
            } else {
                _Text.enabled = true;
                _Text.text    = Tip;
            }

            IEnumerator Hide() {
                yield return new WaitForSeconds(Duration);
                Callback.Invoke();
                _Animator.SetBool(_VisibleParamHash, false);
                yield return new WaitForSeconds(_HideDuration);
                Pool<LoadingScreenGraphic>.Return(this);
            }
            _Animator.SetBool(_VisibleParamHash, true);
            StartCoroutine(Hide());
        }
    }
}
