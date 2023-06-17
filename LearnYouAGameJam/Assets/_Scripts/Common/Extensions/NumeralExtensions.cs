using System;
using System.Diagnostics;
using LYGJ.Common.Enums;
using UnityEngine;

namespace LYGJ.Common {
    public static class NumeralExtensions {
        /// <summary> Returns the value of the specified number rounded to the nearest integer. </summary>
        /// <param name="Value"> A double-precision floating-point number to be rounded. </param>
        /// <param name="Method"> The rounding method to use. </param>
        /// <returns> The integer nearest <paramref name="Value" />. If <paramref name="Value" /> is halfway between two integers, one of which is even and the other odd, then <paramref name="Method" /> determines which of the two is returned. </returns>
        public static int Round( this float Value, RoundMethod Method ) =>
            Method switch {
                RoundMethod.Round => Mathf.RoundToInt(Value),
                RoundMethod.Floor => Mathf.FloorToInt(Value),
                RoundMethod.Ceil  => Mathf.CeilToInt(Value),
                _                 => throw new ArgumentOutOfRangeException(nameof(Method), Method, null)
            };
    }

}
