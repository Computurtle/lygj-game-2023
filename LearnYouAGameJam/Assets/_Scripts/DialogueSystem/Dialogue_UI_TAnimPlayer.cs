using System.Collections;
using System.Diagnostics;
using Febucci.UI;
using LYGJ.Common;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem {
    public sealed class Dialogue_UI_TAnimPlayer : TextAnimatorPlayer {

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
