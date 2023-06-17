using System.Diagnostics;
using HighlightPlus;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.Common {
    public static class OutlineExtensions {

        /// <summary> Adds an outline to the given object, overriding one if already present. </summary>
        /// <param name="Obj"> The object to outline. </param>
        /// <param name="Style"> The style of the outline. </param>
        public static void AddOutline( this GameObject Obj, OutlineStyle Style ) {
            HighlightProfile? Profile = Resources.Load<HighlightProfile>($"Outlines/{Style}");
            if (Profile == null) {
                Debug.LogError($"No outline profile found for {Style}. Does a profile exist at 'Assets/Resources/Outlines/{Style}.asset'?");
                return;
            }

            HighlightEffect? Effect = Obj.GetComponent<HighlightEffect>();
            if (Effect == null) {
                Effect = Obj.AddComponent<HighlightEffect>();
            }
            Effect.ProfileLoad(Profile);
            Effect.highlighted = true;
        }

        /// <summary> Removes the outline from the given object. </summary>
        /// <param name="Obj"> The object to remove the outline from. </param>
        public static void RemoveOutline( this GameObject Obj ) {
            if (Obj.TryGetComponent(out HighlightEffect Effect)) {
                Effect.highlighted = false;
            }
        }

    }

    public enum OutlineStyle {
        Interactable
    }
}
