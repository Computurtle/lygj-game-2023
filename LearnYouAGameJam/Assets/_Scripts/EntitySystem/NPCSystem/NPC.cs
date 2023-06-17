using System.Diagnostics;
using JetBrains.Annotations;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.NPCSystem {
    public sealed class NPC : NPCBase {

        #if UNITY_EDITOR
        [OnValueChanged(nameof(ForceFixInput))]
        #endif
        [SerializeField, Tooltip("The key used to identify the NPC.")] string _Key = string.Empty;

        #if UNITY_EDITOR
        void Reset() {
            ForceFixInput();
        }

        [UsedImplicitly] void ForceFixInput() => _Key = _Key.ConvertNamingConvention(NamingConvention.KebabCase);
        #endif

        #region Overrides of NPCBase

        /// <inheritdoc />
        public override string Key => _Key;

        #endregion

    }
}
