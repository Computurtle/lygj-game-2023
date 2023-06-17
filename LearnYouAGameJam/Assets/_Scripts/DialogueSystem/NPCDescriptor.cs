using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace LYGJ.DialogueSystem {
    [CreateAssetMenu(fileName = "New Name Descriptor", menuName = "LYGJ/NPC/Name Descriptor")]
    public sealed class NPCDescriptor : ScriptableObject {

        [Space]
        [SerializeField, Tooltip("The name of the NPC.")] string _Name;

        /// <summary> The key used to identify the NPC. </summary>
        public string Key => name;

        /// <summary> The name of the NPC. </summary>
        public string Name => _Name;

        /// <summary> Attempts to get the NPC descriptor with the given key. </summary>
        /// <param name="Key"> The key of the NPC descriptor. </param>
        /// <param name="Descriptor"> The NPC descriptor. </param>
        /// <returns> Whether or not the NPC descriptor was found. </returns>
        public static bool TryGet( string Key, [NotNullWhen(true)] out NPCDescriptor? Descriptor ) {
            string Path = $"NPCs/{Key}";
            Descriptor = Resources.Load<NPCDescriptor>(Path);
            return Descriptor != null;
        }

        /// <summary> Gets the NPC descriptor for an unknown NPC. </summary>
        public static NPCDescriptor Unknown => Resources.Load<NPCDescriptor>("NPCs/Unknown");

    }
}
