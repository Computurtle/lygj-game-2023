using System.Diagnostics;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.QuestSystem {
    [CreateAssetMenu(menuName = "LYGJ/Quests/Stage Descriptor", fileName = "New QuestStage Descriptor")]
    public sealed class QuestStageDescriptor : ScriptableObject {
        [SerializeField, Tooltip("The name."), BoxGroup("Details"), HorizontalGroup("Details/H"), VerticalGroup("Details/H/V"), PropertyOrder(1), LocalizationRequired(true)]
        string _Name = string.Empty;
        [SerializeField, Tooltip("The description."), TextArea, BoxGroup("Details"), HorizontalGroup("Details/H"), VerticalGroup("Details/H/V"), PropertyOrder(2), LocalizationRequired(true)]
        string _Description = string.Empty;

        /// <summary> Gets the name. </summary>
        [LocalizationRequired(true)]
        public string Name => _Name;

        /// <summary> Gets the description. </summary>
        [LocalizationRequired(true)]
        public string Description => _Description;

        [SerializeField, Tooltip("The icon."), AssetsOnly, BoxGroup("Details"), HorizontalGroup("Details/H", 80f), HideLabel, PreviewField(80f, ObjectFieldAlignment.Center), PropertyOrder(0)]
        Sprite? _Icon = null;

        /// <summary> Gets the icon. </summary>
        public Sprite? Icon => _Icon;
    }
}
