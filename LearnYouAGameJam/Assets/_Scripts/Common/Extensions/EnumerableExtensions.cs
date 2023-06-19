using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Random = UnityEngine.Random;

namespace LYGJ.Common {
    public static class EnumerableExtensions {

        /// <inheritdoc cref="Enumerable.Cast{TResult}"/>
        public static TResult[] Cast<TResult>( this Array Array ) {
            int       Ln     = Array.Length;
            TResult[] Result = new TResult[Ln];
            for (int I = 0; I < Ln; I++) {
                Result[I] = (TResult)Array.GetValue(I);
            }

            return Result;
        }

        /// <summary> Performs a binary search on a given list. </summary>
        /// <param name="List"> The list to search. </param>
        /// <param name="Item"> The item to search for. </param>
        /// <param name="Comparer"> The comparer to use. </param>
        /// <typeparam name="T"> The type of the items in the list. </typeparam>
        /// <returns> The index of the item if found, otherwise the bitwise complement of the index of the next item. </returns>
        public static int BinarySearch<T>( this IReadOnlyList<T> List, T Item, IComparer<T> Comparer ) {
            int Lower = 0;
            int Upper = List.Count - 1;

            while (Lower <= Upper) {
                int Middle           = Lower + (Upper - Lower) / 2;
                int ComparisonResult = Comparer.Compare(Item, List[Middle]);
                switch (ComparisonResult) {
                    case 0: return Middle;
                    case < 0:
                        Upper = Middle - 1;
                        break;
                    default:
                        Lower = Middle + 1;
                        break;
                }
            }

            return ~Lower;
        }

        /// <inheritdoc cref="BinarySearch{T}(System.Collections.Generic.IReadOnlyList{T},T,System.Collections.Generic.IComparer{T})"/>
        public static int BinarySearch<T>( this IReadOnlyList<T> List, T Item ) => BinarySearch(List, Item, Comparer<T>.Default);

        /// <summary> Performs a binary search on a given list. </summary>
        /// <param name="List"> The list to search. </param>
        /// <param name="Item"> The item to search for. </param>
        /// <param name="Comparer"> The comparer to use. </param>
        /// <param name="Converter"> The converter to use to convert the items in the list to the type of the item to search for. </param>
        /// <typeparam name="T"> The type of the items in the list. </typeparam>
        /// <typeparam name="TUnderlying"> The type of the item to search for. </typeparam>
        /// <returns> The index of the item if found, otherwise the bitwise complement of the index of the next item. </returns>
        public static int BinarySearch<T, TUnderlying>( this IReadOnlyList<T> List, TUnderlying Item, IComparer<TUnderlying> Comparer, Func<T, TUnderlying> Converter ) {
            int Lower = 0;
            int Upper = List.Count - 1;

            while (Lower <= Upper) {
                int Middle           = Lower + (Upper - Lower) / 2;
                int ComparisonResult = Comparer.Compare(Item, Converter(List[Middle]));
                switch (ComparisonResult) {
                    case 0: return Middle;
                    case < 0:
                        Upper = Middle - 1;
                        break;
                    default:
                        Lower = Middle + 1;
                        break;
                }
            }

            return ~Lower;
        }

        /// <inheritdoc cref="BinarySearch{T,TUnderlying}(System.Collections.Generic.IReadOnlyList{T},TUnderlying,System.Collections.Generic.IComparer{TUnderlying},System.Func{T,TUnderlying})"/>
        public static int BinarySearch<T, TUnderlying>( this IReadOnlyList<T> List, TUnderlying Item, Func<T, TUnderlying> Converter ) => BinarySearch(List, Item, Comparer<TUnderlying>.Default, Converter);

        /// <inheritdoc cref="Enumerable.ToArray{TSource}"/>
        /// <param name="Source"> The source enumerable. </param>
        /// <param name="Count"> The number of items to take. </param>
        public static T[] ToArray<T>( this IEnumerable<T> Source, int Count ) {
            T[] Result = new T[Count];
            int I = 0;
            foreach (T Item in Source) {
                Result[I++] = Item;
                if (I == Count) {
                    break;
                }
            }

            return Result;
        }

        /// <summary> Gets the strongly-typed enumerator. </summary>
        /// <typeparam name="T"> The type of the items in the enumerable. </typeparam>
        /// <param name="Enumerable"> The enumerable to get the enumerator for. </param>
        /// <returns> The strongly-typed enumerator. </returns>
        public static IEnumerator<T> GetStrongEnumerator<T>( this IEnumerable<T> Enumerable ) => Enumerable.GetEnumerator();

        /// <summary> Gets the weakly-typed enumerator. </summary>
        /// <param name="Enumerable"> The enumerable to get the enumerator for. </param>
        /// <returns> The weakly-typed enumerator. </returns>
        public static IEnumerator GetWeakEnumerator( this IEnumerable Enumerable ) => Enumerable.GetEnumerator();

        /// <summary> Gets a random item from the enumerable. </summary>
        /// <typeparam name="T"> The type of the items in the enumerable. </typeparam>
        /// <param name="Enumerable"> The enumerable to get the item from. </param>
        /// <returns> A random item from the enumerable. </returns>
        public static T GetRandom<T>( this IEnumerable<T> Enumerable ) =>
            Enumerable switch {
                IList<T> List               => List[Random.Range(0, List.Count)],
                IReadOnlyList<T> List       => List[Random.Range(0, List.Count)],
                ICollection<T> List         => List.ElementAt(Random.Range(0, List.Count)),
                IReadOnlyCollection<T> List => List.ElementAt(Random.Range(0, List.Count)),
                _                           => GetRandom(Array: Enumerable.ToArray())
            };

        /// <inheritdoc cref="Enumerable.ToArray{TSource}"/>
        public static T GetRandom<T>( this IList<T> List ) {
            int Ln = List.Count;
            return Ln > 0 ? List[Random.Range(0, Ln)] : throw new IndexOutOfRangeException("The list is empty.");
        }

        /// <inheritdoc cref="Enumerable.ToArray{TSource}"/>
        public static T GetRandom<T>( this IReadOnlyList<T> List ) {
            int Ln = List.Count;
            return Ln > 0 ? List[Random.Range(0, Ln)] : throw new IndexOutOfRangeException("The list is empty.");
        }

        /// <inheritdoc cref="Enumerable.ToArray{TSource}"/>
        public static T GetRandom<T>( this T[] Array ) => GetRandom((IReadOnlyList<T>)Array);

        /// <inheritdoc cref="Enumerable.ToArray{TSource}"/>
        public static T GetRandom<T>( this List<T> List ) => GetRandom((IList<T>)List);

        /// <inheritdoc cref="Enumerable.ToArray{TSource}"/>
        public static T GetRandom<T>( this ICollection<T> Collection ) {
            int Ln = Collection.Count;
            return Ln > 0 ? Collection.ElementAt(Random.Range(0, Ln)) : throw new IndexOutOfRangeException("The collection is empty.");
        }

        /// <inheritdoc cref="Enumerable.ToArray{TSource}"/>
        public static T GetRandom<T>( this IReadOnlyCollection<T> Collection ) {
            int Ln = Collection.Count;
            return Ln > 0 ? Collection.ElementAt(Random.Range(0, Ln)) : throw new IndexOutOfRangeException("The collection is empty.");
        }

        /// <inheritdoc cref="Enumerable.Contains{TSource}(System.Collections.Generic.IEnumerable{TSource},TSource,System.Collections.Generic.IEqualityComparer{TSource})"/>
        public static bool Contains( this IEnumerable<string> Enumerable, string Item, StringComparison Comparison ) {
            foreach (string Str in Enumerable) {
                if (Str.Equals(Item, Comparison)) {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc cref="HashSet{T}.Add(T)"/>
        public static bool TryAdd<T>( this ICollection<T> Collection, T Item, IEqualityComparer<T> Comparer ) {
            if (Collection.Contains(Item, Comparer)) {
                return false;
            }

            Collection.Add(Item);
            return true;
        }

        /// <inheritdoc cref="HashSet{T}.Add(T)"/>
        public static bool TryAdd( this ICollection<string> Collection, string Item, StringComparison Comparison ) {
            if (Collection.Contains(Item, Comparison)) {
                return false;
            }

            Collection.Add(Item);
            return true;
        }

        static class EnumerableExtensionsTyped<T> {
            [ThreadStatic] static List<T>? _Iterated;

            /// <summary> Iterates over the enumerable and returns the items as a list. </summary>
            /// <param name="Enumerable"> The enumerable to iterate over. </param>
            /// <returns> The items in the enumerable as a list. </returns>
            [LinqTunnel]
            public static IReadOnlyList<T> Iterate( IEnumerable<T> Enumerable ) {
                if (_Iterated is null) {
                    _Iterated = new();
                } else {
                    _Iterated.Clear();
                }

                foreach (T Item in Enumerable) {
                    _Iterated.Add(Item);
                }
                return _Iterated;
            }
        }

        /// <inheritdoc cref="EnumerableExtensionsTyped{T}.Iterate"/>
        /// <typeparam name="T"> The type of the items in the enumerable. </typeparam>
        [LinqTunnel]
        public static IReadOnlyList<T> Iterate<T>( this IEnumerable<T> Enumerable ) => EnumerableExtensionsTyped<T>.Iterate(Enumerable);

        /// <inheritdoc cref="Enumerable.ToList{TSource}"/>
        /// <param name="Source"> The source enumerator. </param>
        /// <returns> The items in the enumerator as a list. </returns>
        public static List<T> ToList<T>( this IEnumerator<T> Source ) {
            List<T> Result = new();
            while (Source.MoveNext()) {
                Result.Add(Source.Current);
            }

            return Result;
        }
    }
}
