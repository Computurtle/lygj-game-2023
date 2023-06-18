using System;
using System.Diagnostics;
using JetBrains.Annotations;
using LYGJ.Common.Enums;
using UnityEngine;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace LYGJ.Common {
    public static class NumeralExtensions {
        /// <summary> Returns the value of the specified number rounded to the nearest integer. </summary>
        /// <param name="Value"> A double-precision floating-point number to be rounded. </param>
        /// <param name="Method"> The rounding method to use. </param>
        /// <returns> The integer nearest <paramref name="Value" />. If <paramref name="Value" /> is halfway between two integers, one of which is even and the other odd, then <paramref name="Method" /> determines which of the two is returned. </returns>
        [DebuggerStepThrough, Pure, MustUseReturnValue]
        public static int Round( this float Value, RoundMethod Method ) =>
            Method switch {
                RoundMethod.Round => Mathf.RoundToInt(Value),
                RoundMethod.Floor => Mathf.FloorToInt(Value),
                RoundMethod.Ceil  => Mathf.CeilToInt(Value),
                _                 => throw new ArgumentOutOfRangeException(nameof(Method), Method, null)
            };

        /// <summary> Returns whether or not the specified number achieves the given comparison. </summary>
        /// <typeparam name="T"> The type of the operands. </typeparam>
        /// <param name="Left"> The left operand. </param>
        /// <param name="Right"> The right operand. </param>
        /// <param name="Comparison"> The comparison to perform. </param>
        /// <returns> <see langword="true" /> if the comparison is achieved; otherwise, <see langword="false" />. </returns>
        [DebuggerStepThrough, Pure, MustUseReturnValue]
        public static bool Compare<T>( this T Left, in T Right, NumericComparison Comparison ) where T : IComparable<T> =>
            Comparison switch {
                NumericComparison.Equal              => Left.CompareTo(Right) == 0,
                NumericComparison.NotEqual           => Left.CompareTo(Right) != 0,
                NumericComparison.LessThan           => Left.CompareTo(Right) < 0,
                NumericComparison.LessThanOrEqual    => Left.CompareTo(Right) <= 0,
                NumericComparison.GreaterThan        => Left.CompareTo(Right) > 0,
                NumericComparison.GreaterThanOrEqual => Left.CompareTo(Right) >= 0,
                _                                    => throw new ArgumentOutOfRangeException(nameof(Comparison), Comparison, null)
            };
    }

}
