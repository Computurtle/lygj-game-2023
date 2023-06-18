using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.InventoryManagement {
    [Serializable]
    public readonly struct Recipe {
        /// <summary> The ingredients for this recipe. </summary>
        [ShowInInspector, Tooltip("The ingredients for this recipe.")]
        public readonly ItemInstance[] Ingredients;

        /// <summary> The result of this recipe. </summary>
        [ShowInInspector, Tooltip("The result of this recipe.")]
        public readonly ItemInstance Result;

        /// <summary> Creates a new recipe. </summary>
        /// <param name="Ingredients"> The ingredients for this recipe. </param>
        /// <param name="Result"> The result of this recipe. </param>
        public Recipe( ItemInstance Result, params ItemInstance[] Ingredients ) {
            this.Ingredients = Ingredients;
            this.Result      = Result;
        }
    }
}
