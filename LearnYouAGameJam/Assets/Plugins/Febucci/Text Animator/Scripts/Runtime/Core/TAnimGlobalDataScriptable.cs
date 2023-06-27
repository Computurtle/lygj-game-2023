using System;
using System.Collections.Generic;
using UnityEngine;
using TagFormatting = Febucci.UI.Core.TAnimBuilder.TagFormatting;

namespace Febucci.UI.Core
{
    /// <summary>
    /// Stores TextAnimator's global data, shared in all your project (eg. Global Behaviors and Appearances).<br/>
    /// Must be placed inside the Resources Path <see cref="resourcesPath"/><br/>
    /// - Manual: <see href="https://www.febucci.com/text-animator-unity/docs/creating-effects-in-the-inspector/#global-effects">Creating Global Effects</see>
    /// </summary>
    [System.Serializable]
    // [CreateAssetMenu(fileName = "TextAnimator GlobalData", menuName = "TextAnimator/Create Global Text Animator Data")]
    public class TAnimGlobalDataScriptable : ScriptableObject
    {
        /// <summary>
        /// Resources Path where the scriptable object must be stored
        /// </summary>
        public const string resourcesPath = "UI/Dialogue/TextAnimator GlobalData";

        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Febucci/TextAnimator/Create Global Text Animator Data", false)]
        static void CreateGlobalData() {
            TAnimGlobalDataScriptable asset = CreateInstance<TAnimGlobalDataScriptable>();
            UnityEditor.ProjectWindowUtil.CreateAsset(asset, $"Assets/Resources/{resourcesPath}.asset");
            UnityEditor.Selection.activeObject = asset;
        }
        [UnityEditor.MenuItem("Tools/Febucci/TextAnimator/Create Global Text Animator Data", true)]
        static bool ValidateCreateGlobalData() {
            bool Exists = Resources.Load<TAnimGlobalDataScriptable>(resourcesPath) != null;
            UnityEditor.Menu.SetChecked("Tools/Febucci/Text Animator/Create Global Text Animator Data", Exists);
            return !Exists;
        }
        #endif

        /// <summary>
        /// Gets the scriptable object from the Resources folder
        /// </summary>
        public static TAnimGlobalDataScriptable Instance => Resources.Load<TAnimGlobalDataScriptable>(resourcesPath);

        [SerializeField]
        internal PresetBehaviorValues[] globalBehaviorPresets = Array.Empty<PresetBehaviorValues>();

        #if UNITY_EDITOR
        public PresetBehaviorValues[] GlobalBehaviorPresets {
            get => globalBehaviorPresets;
            set => globalBehaviorPresets = value;
        }
        #else
        public IReadOnlyList<PresetBehaviorValues> GlobalBehaviorPresets => globalBehaviorPresets;
        #endif

        [SerializeField]
        internal PresetAppearanceValues[] globalAppearancePresets = Array.Empty<PresetAppearanceValues>();

        #if UNITY_EDITOR
        public PresetAppearanceValues[] GlobalAppearancePresets {
            get => globalAppearancePresets;
            set => globalAppearancePresets = value;
        }
        #else
        public IReadOnlyList<PresetAppearanceValues> GlobalAppearancePresets => globalAppearancePresets;
        #endif

        [SerializeField]
        internal string[] customActions = Array.Empty<string>();

        public string[] CustomActions {
            get => customActions;
            set => customActions = value;
        }

        [SerializeField] internal bool          customTagsFormatting = false;
        [SerializeField] internal TagFormatting tagInfo_behaviors    = new('<', '>');
        [SerializeField] internal TagFormatting tagInfo_appearances  = new('{', '}');

        #if UNITY_EDITOR
        public bool CustomTagsFormatting {
            get => customTagsFormatting;
            set => customTagsFormatting = value;
        }
        public TagFormatting TagInfo_Behaviors {
            get => tagInfo_behaviors;
            set => tagInfo_behaviors = value;
        }
        public TagFormatting TagInfo_Appearances {
            get => tagInfo_appearances;
            set => tagInfo_appearances = value;
        }
        #else
        public bool CustomTagsFormatting => customTagsFormatting;
        public TagFormatting TagInfo_Behaviors => tagInfo_behaviors;
        public TagFormatting TagInfo_Appearances => tagInfo_appearances;
        #endif
    }

}
