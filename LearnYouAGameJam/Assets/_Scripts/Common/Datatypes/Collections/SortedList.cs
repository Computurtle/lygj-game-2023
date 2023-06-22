using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.Common.Datatypes.Collections {
    [Serializable]
    public sealed class SortedList<T> : IList<T>, IReadOnlyList<T> { // Similar to C#'s SortedList<TKey, TValue> but without the value part. Supports both values that implement IComparable<T> and custom comparers.
        readonly IComparer<T> _Comparer;

        public SortedList( IComparer<T> Comparer ) => _Comparer = Comparer;
        public SortedList() : this(Comparer<T>.Default) { }

        [SerializeField, InlineProperty, HideReferenceObjectPicker, HideLabel]
        List<T> _List = new();

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => _List.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Implementation of IReadOnlyCollection<out T>

        /// <inheritdoc />
        int IReadOnlyCollection<T>.Count => _List.Count;

        #endregion

        #region Implementation of ICollection<T>

        /// <inheritdoc />
        public void Add( T Item ) {
            int Index = _List.BinarySearch(Item, _Comparer);
            if (Index < 0) {
                Index = ~Index;
            }

            _List.Insert(Index, Item);
        }

        /// <inheritdoc />
        public void Clear() => _List.Clear();

        /// <inheritdoc />
        public bool Contains( T Item ) => _List.BinarySearch(Item, _Comparer) >= 0;

        /// <inheritdoc />
        public void CopyTo( T[] Array, int ArrayIndex ) => _List.CopyTo(Array, ArrayIndex);

        /// <inheritdoc />
        public bool Remove( T Item ) {
            int Index = _List.BinarySearch(Item, _Comparer);
            if (Index < 0) {
                return false;
            }

            _List.RemoveAt(Index);
            return true;
        }

        /// <inheritdoc />
        public int Count => _List.Count;

        /// <inheritdoc />
        bool ICollection<T>.IsReadOnly => ((ICollection<T>)_List).IsReadOnly;

        #endregion

        #region Implementation of IReadOnlyList<T>

        /// <inheritdoc />
        public int IndexOf( T Item ) => _List.BinarySearch(Item, _Comparer);

        /// <inheritdoc cref="IReadOnlyList{T}.this"/>
        public T this[ int Index ] => _List[Index];

        #endregion

        #region Implementation of IList<T>

        /// <inheritdoc />
        void IList<T>.Insert( int Index, T Item ) {
            Debug.LogWarning("Inserting into a sorted list is not supported. Use Add() instead.");
            Add(Item);
        }

        /// <inheritdoc />
        void IList<T>.RemoveAt( int Index ) {
            Debug.LogWarning("Removing from a sorted list is not supported. Use Remove() instead.");
            _List.RemoveAt(Index);
        }

        /// <inheritdoc cref="IList{T}.this"/>
        T IList<T>.this[ int Index ] {
            get => _List[Index];
            set {
                Debug.LogWarning("Setting an item in a sorted list is not supported. Use Add() instead.");
                _List[Index] = value;
            }
        }

        #endregion

        /// <inheritdoc cref="List{T}.Capacity"/>
        public int Capacity {
            get => _List.Capacity;
            set => _List.Capacity = value;
        }

        /// <inheritdoc cref="List{T}.TrimExcess"/>
        public void TrimExcess() => _List.TrimExcess();

    }
}
