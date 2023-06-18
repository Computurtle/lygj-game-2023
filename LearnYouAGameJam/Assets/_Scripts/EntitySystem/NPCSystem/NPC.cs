using System.Diagnostics;
using LYGJ.Common;
using LYGJ.DialogueSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.NPCSystem {
    public sealed class NPC : NPCBase {

        #if UNITY_EDITOR
        [OnValueChanged(nameof(ForceFixInput)),
         InfoBox("No descriptor exists for this NPC.", InfoMessageType.Warning, nameof(MissingDescriptor)),
         InfoBox("NPC requires a key.", InfoMessageType.Error, nameof(MissingKey))]
        #endif
        [SerializeField, Tooltip("The key used to identify the NPC.")] string _Key = string.Empty;

        #if UNITY_EDITOR
        void Reset() {
            ForceFixInput();
        }

        bool MissingDescriptor => !string.IsNullOrEmpty(_Key) && !NPCDescriptor.Exists(_Key);
        bool MissingKey        => string.IsNullOrEmpty(_Key);

        void ForceFixInput() => _Key = _Key.ConvertNamingConvention(NamingConvention.KebabCase);
        #endif

        #region Overrides of NPCBase

        /// <inheritdoc />
        public override string Key => _Key;

        #endregion

    }
}
