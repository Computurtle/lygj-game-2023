using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using LYGJ.Common.Enums;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.Common {
    public static class Matrix {

        /// <summary> Gets the width of the matrix. </summary>
        /// <remarks> This assumes the matrix is setup as [Width, Height] (x, y). </remarks>
        /// <typeparam name="T"> The type of values in the matrix. </typeparam>
        /// <param name="Matrix"> The matrix. </param>
        /// <returns> The width of the matrix. </returns>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)] public static int GetWidth<T>( this T[,] Matrix ) => Matrix.GetLength(0);

        /// <summary> Gets the height of the matrix. </summary>
        /// <remarks> This assumes the matrix is setup as [Width, Height] (x, y). </remarks>
        /// <typeparam name="T"> The type of values in the matrix. </typeparam>
        /// <param name="Matrix"> The matrix. </param>
        /// <returns> The height of the matrix. </returns>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)] public static int GetHeight<T>( this T[,] Matrix ) => Matrix.GetLength(1);

        /// <summary> Flatten the matrix into a single array. </summary>
        /// <typeparam name="T"> The type of values in the matrix. </typeparam>
        /// <param name="Matrix"> The matrix. </param>
        /// <returns> The flattened matrix. </returns>
        public static T[] Flatten<T>( this T[,] Matrix ) {
            int Width  = Matrix.GetWidth();
            int Height = Matrix.GetHeight();
            T[] Flat   = new T[Width * Height];
            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    Flat[X + Y * Width] = Matrix[X, Y];
                }
            }

            return Flat;
        }

        /// <summary> Rotates a position within a given width and height. </summary>
        /// <param name="Position"> The position to rotate. </param>
        /// <param name="Width"> The width of the area to rotate within. </param>
        /// <param name="Height"> The height of the area to rotate within. </param>
        /// <param name="Rotation"> The rotation to apply. </param>
        /// <returns> The rotated position. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="Rotation"/> is not a valid value. </exception>
        public static Vector2Int GetRotated( Vector2Int Position, int Width, int Height, ItemRotation Rotation ) =>
            Rotation switch {
                ItemRotation.None => // (x,y) -> (x,y)
                    Position,
                ItemRotation.Clockwise => // (x,y) -> (y, w-1-x) // 90 degrees clockwise
                    new(Position.y, Width - 1 - Position.x),
                ItemRotation.OneEightie => // (x,y) -> (w-1-x, h-1-y) // 180 degrees clockwise
                    new(Width - 1 - Position.x, Height - 1 - Position.y),
                ItemRotation.Counter => // (x,y) -> (h-1-y, x) // 270 degrees clockwise
                    new(Height - 1 - Position.y, Position.x),
                _ => throw new ArgumentOutOfRangeException(nameof(Rotation))
            };

        /// <summary> Creates a new array, copying from the source, and rotating it. </summary>
        /// <param name="Source"> The array to copy. </param>
        /// <param name="Rotation"> The rotation to apply. </param>
        /// <typeparam name="T"> The type of values in the array. </typeparam>
        /// <returns> The rotated array. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="Rotation"/> is not a valid value. </exception>
        public static T[,] GetRotated<T>( this T[,] Source, ItemRotation Rotation ) {
            switch (Rotation) {
                case ItemRotation.None:
                    return Source;
                case ItemRotation.Clockwise: { // 90 degrees clockwise
                    int  SourceWidth  = Source.GetLength(0);
                    int  SourceHeight = Source.GetLength(1);
                    T[,] Result       = new T[SourceHeight, SourceWidth];
                    for (int X = 0; X < SourceWidth; X++) {
                        for (int Y = 0; Y < SourceHeight; Y++) {
                            Result[Y, SourceWidth - 1 - X] = Source[X, Y];
                        }
                    }

                    return Result;
                }
                case ItemRotation.OneEightie: { // 180 degrees clockwise
                    int  SourceWidth  = Source.GetLength(0);
                    int  SourceHeight = Source.GetLength(1);
                    T[,] Result       = new T[SourceWidth, SourceHeight];
                    for (int X = 0; X < SourceWidth; X++) {
                        for (int Y = 0; Y < SourceHeight; Y++) {
                            Result[SourceWidth - 1 - X, SourceHeight - 1 - Y] = Source[X, Y];
                        }
                    }

                    return Result;
                }
                case ItemRotation.Counter: { // 270 degrees clockwise
                    int  SourceWidth  = Source.GetLength(0);
                    int  SourceHeight = Source.GetLength(1);
                    T[,] Result       = new T[SourceHeight, SourceWidth];
                    for (int X = 0; X < SourceWidth; X++) {
                        for (int Y = 0; Y < SourceHeight; Y++) {
                            Result[SourceHeight - 1 - Y, X] = Source[X, Y];
                        }
                    }

                    return Result;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Rotation));
            }
        }

        /// <summary> Creates a new array with the given dimensions, copying from the source, and filling in any new space with the given <paramref name="Fill"/> value. </summary>
        /// <remarks> The resize will be based around (0, 0). </remarks>
        /// <param name="Source"> The array to copy. </param>
        /// <param name="NewWidth"> The new width of the array. </param>
        /// <param name="NewHeight"> The new height of the array. </param>
        /// <param name="Fill"> The value to fill in any new space with. </param>
        /// <typeparam name="T"> The type of values in the array. </typeparam>
        /// <returns> The resized array. </returns>
        public static T[,] GetResized<T>( this T[,] Source, int NewWidth, int NewHeight, T Fill = default! ) {
            int OldWidth  = Source.GetLength(0);
            int OldHeight = Source.GetLength(1);

            T[,] Result = new T[NewWidth, NewHeight];
            for (int X = 0; X < NewWidth; X++) {
                for (int Y = 0; Y < NewHeight; Y++) {
                    if (X >= OldWidth || Y >= OldHeight) {
                        Result[X, Y] = Fill;
                    } else {
                        Result[X, Y] = Source[X, Y];
                    }
                }
            }

            return Result;
        }

        /// <summary> Creates a new spliced array with the given dimensions and offset, copying from the source. </summary>
        /// <param name="Source"> The array to copy. </param>
        /// <param name="X"> The X offset to start the splice at. </param>
        /// <param name="Y"> The Y offset to start the splice at. </param>
        /// <param name="Width"> The width of the splice. </param>
        /// <param name="Height"> The height of the splice. </param>
        /// <typeparam name="T"> The type of values in the array. </typeparam>
        /// <returns> The spliced array. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="X"/>, <paramref name="Y"/>, <paramref name="Width"/>, or <paramref name="Height"/> are less than zero. </exception>
        public static T[,] GetTruncated<T>( this T[,] Source, int X, int Y, int Width, int Height ) {
            if (X < 0) { throw new ArgumentOutOfRangeException(nameof(X), "X is less than zero. This creates undefined behaviour when truncating."); }

            if (Y < 0) { throw new ArgumentOutOfRangeException(nameof(Y), "Y is less than zero. This creates undefined behaviour when truncating."); }

            switch (Width) {
                case < 0:
                    throw new ArgumentOutOfRangeException(nameof(Width), "Width is less than zero. This creates undefined behaviour when truncating.");
                case 0:
                    Debug.LogWarning("Creating a truncated fill with zero width. This is redundant as it contains no actual data.");
                    return new T[0, Height];
            }

            switch (Height) {
                case < 0:
                    throw new ArgumentOutOfRangeException(nameof(Height), "Height is less than zero. This creates undefined behaviour when truncating.");
                case 0:
                    Debug.LogWarning("Creating a truncated fill with zero height. This is redundant as it contains no actual data.");
                    return new T[Width, 0];
            }

            T[,] Result = new T[Width, Height];

            for (int I = 0; I < Width; I++) {
                for (int J = 0; J < Height; J++) {
                    Result[I, J] = Source[I + X, J + Y];
                }
            }

            return Result;
        }

        /// <inheritdoc cref="GetTruncated{T}(T[,],int,int,int,int)"/>
        /// <param name="Source"> The array to copy. </param>
        /// <param name="Area"> The area to splice. </param>
        public static T[,] GetTruncated<T>( this T[,] Source, RectInt Area ) => GetTruncated(Source, Area.xMin, Area.yMin, Area.width, Area.height);

        static class TypeHelper<T> {
            /// <summary> An empty matrix. </summary>
            public static readonly T[,] Empty = new T[0, 0];
        }

        /// <summary> Gets an empty matrix. </summary>
        /// <typeparam name="T"> The type of the matrix. </typeparam>
        /// <returns> An empty matrix. </returns>
        public static T[,] Empty<T>() => TypeHelper<T>.Empty;

        /// <inheritdoc cref="Enumerable.Count{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static int Count<T>( this T[,] Source, Func<T, bool> Predicate ) {
            int Width  = Source.GetWidth();
            int Height = Source.GetHeight();

            int Count = 0;
            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    if (Predicate(Source[X, Y])) {
                        Count++;
                    }
                }
            }

            return Count;
        }

        /// <inheritdoc cref="Enumerable.First{TSource}(IEnumerable{TSource})"/>
        public static T First<T>( this T[,] Source ) => Source.Length > 0 ? Source[0, 0] : throw new InvalidOperationException("The source array is empty.");

        /// <inheritdoc cref="Enumerable.First{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static T First<T>( this T[,] Source, Func<T, bool> Predicate ) {
            T? Result = Source.FirstOrDefault(Predicate);
            if (Result == null) {
                throw new InvalidOperationException("No element matches the predicate.");
            }

            return Result;
        }

        /// <inheritdoc cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>
        public static T? FirstOrDefault<T>( this T[,] Source ) => Source.Length > 0 ? Source[0, 0] : default;

        /// <inheritdoc cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static T? FirstOrDefault<T>( this T[,] Source, Func<T, bool> Predicate ) {
            int Width  = Source.GetWidth();
            int Height = Source.GetHeight();

            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    T Value = Source[X, Y];
                    if (Predicate(Value)) {
                        return Value;
                    }
                }
            }

            return default;
        }

        /// <inheritdoc cref="Enumerable.Last{TSource}(IEnumerable{TSource})"/>
        public static T Last<T>( this T[,] Source ) => Source.Length > 0 ? Source[Source.GetWidth() - 1, Source.GetHeight() - 1] : throw new InvalidOperationException("The source array is empty.");

        /// <inheritdoc cref="Enumerable.Last{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static T Last<T>( this T[,] Source, Func<T, bool> Predicate ) {
            T? Result = Source.LastOrDefault(Predicate);
            if (Result == null) {
                throw new InvalidOperationException("No element matches the predicate.");
            }

            return Result;
        }

        /// <inheritdoc cref="Enumerable.LastOrDefault{TSource}(IEnumerable{TSource})"/>
        public static T? LastOrDefault<T>( this T[,] Source ) => Source.Length > 0 ? Source[Source.GetWidth() - 1, Source.GetHeight() - 1] : default;

        /// <inheritdoc cref="Enumerable.LastOrDefault{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static T? LastOrDefault<T>( this T[,] Source, Func<T, bool> Predicate ) {
            int Width  = Source.GetWidth();
            int Height = Source.GetHeight();

            for (int X = Width - 1; X >= 0; X--) {
                for (int Y = Height - 1; Y >= 0; Y--) {
                    T Value = Source[X, Y];
                    if (Predicate(Value)) {
                        return Value;
                    }
                }
            }

            return default;
        }

        /// <inheritdoc cref="Enumerable.Where{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static IEnumerable<T> Where<T>( this T[,] Source, Func<T, bool> Predicate ) {
            int Width  = Source.GetWidth();
            int Height = Source.GetHeight();

            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    T Value = Source[X, Y];
                    if (Predicate(Value)) {
                        yield return Value;
                    }
                }
            }
        }

        /// <inheritdoc cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>
        public static bool Any<T>( this T[,] Source ) => Source.Length > 0;

        /// <inheritdoc cref="Enumerable.Any{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static bool Any<T>( this T[,] Source, Func<T, bool> Predicate ) {
            int Width  = Source.GetWidth();
            int Height = Source.GetHeight();

            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    if (Predicate(Source[X, Y])) {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc cref="Enumerable.All{TSource}(IEnumerable{TSource},Func{TSource,bool})"/>
        public static bool All<T>( this T[,] Source, Func<T, bool> Predicate ) {
            int Width  = Source.GetWidth();
            int Height = Source.GetHeight();

            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    if (!Predicate(Source[X, Y])) {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public static class BoolMatrix {

        /// <summary> Creates a new array, copying from the source, and trimming any empty space from the edges. </summary>
        /// <param name="Source"> The array to copy. </param>
        /// <returns> The trimmed array. </returns>
        public static bool[,] GetTrimmed( this bool[,] Source ) {
            int Width = Source.GetLength(0), Height = Source.GetLength(1);

            int FirstCol = Width,  LastCol = 0;
            int FirstRow = Height, LastRow = 0;

            bool FoundAny = false;
            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    if (Source[X, Y]) { // Defined at x,y position
                        FoundAny = true;
                        FirstCol = Mathf.Min(FirstCol, X);
                        LastCol  = Mathf.Max(LastCol, X);
                        FirstRow = Mathf.Min(FirstRow, Y);
                        LastRow  = Mathf.Max(LastRow, Y);
                        // Debug.Log($"\tDefined at {X},{Y} ; update: {FirstCol}, {FirstRow} ... {LastCol}(+1), {LastRow}(+1)");
                    }
                }
            }

            if (!FoundAny) { // Item is undefined
                Debug.LogWarning("Item is undefined on X/Y axis");
                return Matrix.Empty<bool>();
            }

            // Debug.Log($"Found: {FirstCol}, {FirstRow} ... {LastCol}(+1), {LastRow}(+1)");
            RectInt Area = new(xMin: FirstCol, yMin: FirstRow, width: LastCol - FirstCol + 1, height: LastRow - FirstRow + 1);
            return Source.GetTruncated(Area);
        }

        /// <summary> Gets the union of the two given areas. </summary>
        /// <param name="A"> The first area. </param>
        /// <param name="B"> The second area. </param>
        /// <param name="Method"> The boolean operator to use to combine the areas. </param>
        /// <returns> The union of the two areas. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="Method"/> is not a valid value. </exception>
        public static bool[,] GetUnion( this bool[,] A, bool[,] B, BooleanOperator Method = BooleanOperator.And ) {
            int     Width  = Math.Max(A.GetLength(0), B.GetLength(0));
            int     Height = Math.Max(A.GetLength(1), B.GetLength(1));
            bool[,] Result = new bool[Width, Height];
            switch (Method) {
                case BooleanOperator.And: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = A[X, Y] && B[X, Y];
                        }
                    }

                    break;
                }
                case BooleanOperator.Or: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = A[X, Y] || B[X, Y];
                        }
                    }

                    break;
                }
                case BooleanOperator.Xor: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = A[X, Y] ^ B[X, Y];
                        }
                    }

                    break;
                }
                case BooleanOperator.Nand: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = !(A[X, Y] && B[X, Y]);
                        }
                    }

                    break;
                }
                case BooleanOperator.Nor: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = !(A[X, Y] || B[X, Y]);
                        }
                    }

                    break;
                }
                case BooleanOperator.Xnor: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = !(A[X, Y] ^ B[X, Y]);
                        }
                    }

                    break;
                }
                case BooleanOperator.LeftOnly: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = A[X, Y] && !B[X, Y];
                        }
                    }

                    break;
                }
                case BooleanOperator.RightOnly: {
                    for (int X = 0; X < Width; X++) {
                        for (int Y = 0; Y < Height; Y++) {
                            Result[X, Y] = !A[X, Y] && B[X, Y];
                        }
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Method));
            }

            return Result;
        }
    }
}
