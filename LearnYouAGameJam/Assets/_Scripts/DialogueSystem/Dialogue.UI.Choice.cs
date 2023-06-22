using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ.DialogueSystem {
    public sealed class Dialogue_UI_Choice : MonoBehaviour {
        [SerializeField, Tooltip("The text component."), Required, ChildGameObjectsOnly] TMP_Text _Text = null!;

        [SerializeField, Tooltip("The format string.\n\n0 = Index\n1 = Text")] string _Format = "{0}. {1}";

        [Space]
        [SerializeField, Tooltip("The button component."), Required, ChildGameObjectsOnly] Button _Button = null!;

        #if UNITY_EDITOR
        void Reset() {
            _Text   = GetComponentInChildren<TMP_Text>(true);
            _Button = GetComponentInChildren<Button>(true);
        }
        #endif

        /// <summary> Sets the text of the choice. </summary>
        /// <param name="Text"> The text to set. </param>
        /// <param name="Index"> The index of the choice. </param>
        /// <param name="Callback"> The callback to invoke when the choice is selected. </param>
        public void Setup( string Text, int Index, Action<int> Callback ) {
            void Clicked() {
                Callback(Index);
                _Button.onClick.RemoveListener(Clicked);
            }
            _Button.onClick.AddListener(Clicked);
            _Text.text = string.Format(_Format, Index + 1, Text);
        }

        /// <summary> Resets the choice. </summary>
        public void Clear() {
            _Button.onClick.RemoveAllListeners();
            _Text.text = string.Empty;
        }

        /// <summary> Focuses the choice. </summary>
        public void Focus() => _Button.Select();
    }
}
