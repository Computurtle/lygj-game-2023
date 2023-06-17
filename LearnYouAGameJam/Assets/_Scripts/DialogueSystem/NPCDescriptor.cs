using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using LYGJ.Common;
using UnityEngine;

namespace LYGJ.DialogueSystem {
    [CreateAssetMenu(fileName = "New Name Descriptor", menuName = "LYGJ/NPC/Name Descriptor")]
    public sealed class NPCDescriptor : ScriptableObject {

        [SerializeField, Tooltip("The name of the NPC.")] string _Name;

        #if UNITY_EDITOR
        void Reset() {
            string Path = UnityEditor.AssetDatabase.GetAssetPath(this);
            _Name = System.IO.Path.GetFileNameWithoutExtension(Path).ConvertNamingConvention(NamingConvention.TitleCase);
        }
        #endif

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

        /// <summary> Gets whether the NPC descriptor exists for the given key. </summary>
        /// <param name="Key"> The key of the NPC descriptor. </param>
        /// <returns> Whether or not the NPC descriptor exists. </returns>
        public static bool Exists( string Key ) => TryGet(Key, out _);

        /// <summary> Gets the NPC descriptor for an unknown NPC. </summary>
        public static NPCDescriptor Unknown => Resources.Load<NPCDescriptor>("NPCs/Unknown");

    }
}
