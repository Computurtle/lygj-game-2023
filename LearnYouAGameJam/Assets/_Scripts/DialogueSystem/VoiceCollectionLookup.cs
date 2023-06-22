using System.Collections.Generic;
using System.Linq;
using LYGJ.AudioManagement;
using LYGJ.Common;
using LYGJ.Common.Datatypes.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.DialogueSystem {
    [CreateAssetMenu(fileName = "New Voice Collection Lookup", menuName = "LYGJ/Dialogue/Voice Collection Lookup")]
    public sealed class VoiceCollectionLookup : ScriptableObject {
        [SerializeField, InlineProperty]
        UniDict<Voice, SFX> _Lookup = new();

        /// <summary> Returns the <see cref="SFX"/> for the given <see cref="Voice"/>. </summary>
        /// <param name="Voice"> The <see cref="Voice"/> to get the <see cref="SFX"/> for. </param>
        /// <returns> The <see cref="SFX"/> for the given <see cref="Voice"/>. </returns>
        public SFX? this[ Voice Voice ] => Voice is not Voice.None && _Lookup.TryGetValue(Voice, out SFX? SFX) ? SFX : null;

        #if UNITY_EDITOR
        void Reset() {
            int Ln = Enum<Voice>.Count;
            _Lookup = new(Ln);

            string Path = UnityEditor.AssetDatabase.GetAssetPath(this);
            Path = Path[..Path.LastIndexOf('/')];
            SFX? TryFind( Voice V ) {
                string Nm = V.ToString();
                // Attempt to find the SFX 'Nm' in this folder.
                string[] GUIDs = UnityEditor.AssetDatabase.FindAssets(Nm, new[] {Path});
                switch (GUIDs.Length) {
                    case 0:
                        Debug.LogWarning($"Could not find SFX for {Nm} in {Path}.");
                        return null;
                    case > 1:
                        Debug.LogWarning($"Found multiple SFX for {Nm} in {Path}.\n\t'{string.Join("', '", GUIDs)}'");
                        break;
                }
                string GUID = GUIDs[0];
                string Pth = UnityEditor.AssetDatabase.GUIDToAssetPath(GUID);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<SFX>(Pth);
            }
            foreach (Voice V in Enum<Voice>.Values.Skip(1)) {
                _Lookup[V] = TryFind(V)!;
            }
        }
        #endif

    }
}
