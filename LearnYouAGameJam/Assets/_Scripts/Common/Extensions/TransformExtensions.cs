using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace LYGJ.Common {
    public static class TransformExtensions {
        /// <summary> Gets all children of the given transform. </summary>
        /// <param name="Parent"> The transform whose children to get. </param>
        /// <param name="Recurse"> Whether to get all descendants of the given transform. </param>
        /// <returns> All children of the given transform. </returns>
        public static IEnumerable<Transform> GetChildren( this Transform Parent, bool Recurse ) {
            foreach (Transform Child in Parent) {
                yield return Child;
                if (Recurse) {
                    foreach (Transform Grandchild in Child.GetChildren(true)) {
                        yield return Grandchild;
                    }
                }
            }
        }

        /// <inheritdoc cref="GetChildren(Transform,bool)"/>
        public static IReadOnlyList<Transform> GetChildren( this Transform Parent ) => Parent.Cast<Transform>().Iterate();
    }
}
