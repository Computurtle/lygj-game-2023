using System.Diagnostics;
using UnityEngine;

namespace LYGJ.Common {
    public static class ColourExtensions {
        /// <summary> Returns the same colour with the given red channel value. </summary>
        /// <param name="Colour"> The colour to modify. </param>
        /// <param name="Red"> The new red channel value. </param>
        /// <returns> The modified colour. </returns>
        public static Color WithRed( this Color Colour, float Red ) => new(Red, Colour.g, Colour.b, Colour.a);

        /// <summary> Returns the same colour with the given green channel value. </summary>
        /// <param name="Colour"> The colour to modify. </param>
        /// <param name="Green"> The new green channel value. </param>
        /// <returns> The modified colour. </returns>
        public static Color WithGreen( this Color Colour, float Green ) => new(Colour.r, Green, Colour.b, Colour.a);

        /// <summary> Returns the same colour with the given blue channel value. </summary>
        /// <param name="Colour"> The colour to modify. </param>
        /// <param name="Blue"> The new blue channel value. </param>
        /// <returns> The modified colour. </returns>
        public static Color WithBlue( this Color Colour, float Blue ) => new(Colour.r, Colour.g, Blue, Colour.a);

        /// <summary> Returns the same colour with the given alpha channel value. </summary>
        /// <param name="Colour"> The colour to modify. </param>
        /// <param name="Alpha"> The new alpha channel value. </param>
        /// <returns> The modified colour. </returns>
        public static Color WithAlpha( this Color Colour, float Alpha ) => new(Colour.r, Colour.g, Colour.b, Alpha);

    }
}
