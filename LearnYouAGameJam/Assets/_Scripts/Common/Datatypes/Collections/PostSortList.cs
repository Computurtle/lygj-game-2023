using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.Common.Datatypes.Collections {
    [Serializable]
    public sealed class PostSortList<TKey, TValue> : IReadOnlyCollection<SortingPair<TKey, TValue>>, ICollection, ICollection<SortingPair<TKey, TValue>>, IEnumerable<TValue> where TKey : notnull {
        [SerializeField, HideInInspector]
        List<SortingPair<TKey, TValue>> _List;

        readonly IComparer<TKey> _Comparer;

        bool _IsSorted = false;

        void EnsureSorted() {
            if (_IsSorted) {
                return;
            }

            _List.Sort(); // Uses IComparable<SortingPair> automatically, so we are still using the _Comparer.
            _IsSorted = true;
        }

        SortingPair<TKey, TValue> CreatePair( TKey Key, TValue Value ) => new(Key, Value, _Comparer);

        #region Constructors

        /// <summary> Creates a new instance of the <see cref="PostSortList{TKey,TValue}"/> class. </summary>
        /// <param name="Comparer"> The comparer to use. <see langword="null"/> to use the default comparer for <typeparamref name="TKey"/> (which assumes the type implements <see cref="IComparable{T}"/> and/or <see cref="IComparable{T}"/> and raises an exception if not). </param>
        /// <param name="Capacity"> The initial capacity. </param>
        /// <exception cref="InvalidOperationException"> Thrown if <typeparamref name="TKey"/> does not implement <see cref="IComparable{T}"/> and/or <see cref="IComparable{T}"/>. </exception>
        public PostSortList( IComparer<TKey>? Comparer = null, int Capacity = 10 ) {
            if (Comparer is null) {
                if (!typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)) && !typeof(IComparable).IsAssignableFrom(typeof(TKey))) {
                    throw new InvalidOperationException($"The type {typeof(TKey)} does not implement IComparable<{typeof(TKey)}>, nor IComparable.");
                }

                _Comparer = Comparer<TKey>.Default;
            } else {
                _Comparer = Comparer;
            }

            _List = new(Capacity);
        }

        /// <inheritdoc cref="PostSortList{TKey,TValue}(System.Collections.Generic.IComparer{TKey}?,int)"/>
        public PostSortList() : this(null, 10) { }

        /// <inheritdoc cref="PostSortList{TKey,TValue}(System.Collections.Generic.IComparer{TKey}?,int)"/>
        public PostSortList( int Capacity ) : this(null, Capacity) { }

        /// <inheritdoc cref="PostSortList{TKey,TValue}(System.Collections.Generic.IComparer{TKey}?,int)"/>
        public PostSortList( IComparer<TKey> Comparer ) : this(Comparer, 10) { }

        #endregion

        #region Implementation of IEnumerable

        /// <inheritdoc />

        IEnumerator IEnumerable.GetEnumerator() {
            EnsureSorted();
            return _List.GetWeakEnumerator();
        }

        #endregion

        #region Implementation of IEnumerable<out SortingPair<TKey,TValue>>

        /// <inheritdoc />
        public IEnumerator<SortingPair<TKey, TValue>> GetEnumerator() {
            EnsureSorted();
            return _List.GetStrongEnumerator();
        }

        #endregion

        #region Implementation of IEnumerable<out TValue>

        /// <inheritdoc />
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
            EnsureSorted();
            foreach (SortingPair<TKey, TValue> Pair in _List) {
                yield return Pair.Value;
            }
        }

        #endregion

        #region Implementation of IReadOnlyCollection<out SortingPair<TKey,TValue>>

        /// <inheritdoc />
        int IReadOnlyCollection<SortingPair<TKey, TValue>>.Count => _List.Count;

        #endregion

        #region Implementation of ICollection

        /// <inheritdoc />
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc />
        object ICollection.SyncRoot => this;

        /// <inheritdoc />
        void ICollection.CopyTo( Array Array, int Index ) => ((ICollection)_List).CopyTo(Array, Index);

        /// <inheritdoc />
        int ICollection.Count => _List.Count;

        #endregion

        #region Implementation of ICollection<out SortingPair<TKey,TValue>>

        /// <inheritdoc />
        void ICollection<SortingPair<TKey, TValue>>.Add( SortingPair<TKey, TValue> Item ) => Add(Item.Key, Item.Value);

        /// <inheritdoc />
        public void Clear() {
            _List.Clear();
            _IsSorted = true; // No need to sort nothing.
        }

        /// <inheritdoc />
        bool ICollection<SortingPair<TKey, TValue>>.Contains( SortingPair<TKey, TValue> Item ) => ContainsKey(Item.Key);

        /// <inheritdoc />
        void ICollection<SortingPair<TKey, TValue>>.CopyTo( SortingPair<TKey, TValue>[] Array, int ArrayIndex ) => _List.CopyTo(Array, ArrayIndex);

        /// <inheritdoc />
        bool ICollection<SortingPair<TKey, TValue>>.Remove( SortingPair<TKey, TValue> Item ) => Remove(Item.Key);

        /// <inheritdoc />
        int ICollection<SortingPair<TKey, TValue>>.Count => _List.Count;

        /// <inheritdoc />
        bool ICollection<SortingPair<TKey, TValue>>.IsReadOnly => false;

        #endregion

        /// <summary> Adds a new item to the list. </summary>
        /// <param name="Key"> The key. </param>
        /// <param name="Value"> The value. </param>
        public void Add( TKey Key, TValue Value ) {
            _List.Add(CreatePair(Key, Value));
            _IsSorted = false;
        }

        /// <summary> Adds a new item to the list. </summary>
        /// <param name="Pair"> The pair. </param>
        public void Add( KeyValuePair<TKey, TValue> Pair ) => Add(Pair.Key, Pair.Value);

        /// <summary> Adds a new item to the list. </summary>
        /// <param name="Pair"> The pair. </param>
        public void Add( (TKey Key, TValue Value) Pair ) => Add(Pair.Key, Pair.Value);

        /// <summary> Removes an item from the list. </summary>
        /// <param name="Key"> The key. </param>
        /// <returns> <see langword="true"/> if the item was removed; otherwise, <see langword="false"/>. </returns>
        public bool Remove( TKey Key ) {
            int Index = IndexOf(Key);
            if (Index == -1) {
                return false;
            }

            _List.RemoveAt(Index);
            _IsSorted = false;
            return true;
        }

        /// <summary> Gets the index of an item in the list. </summary>
        /// <param name="Key"> The key. </param>
        /// <returns> The index of the item, or <c>-1</c> if the item was not found. </returns>
        [MustUseReturnValue]
        public int IndexOf( TKey Key ) {
            EnsureSorted();
            // Binary search as we can use the _Comparer.
            // return _List.BinarySearch(CreatePair(Key, default), _Comparer); // OLD: Unnecessary allocations.
            int Min = 0;
            int Max = _List.Count - 1;
            while (Min <= Max) {
                int Mid = (Min + Max) / 2;
                int Cmp = _Comparer.Compare(_List[Mid].Key, Key);
                switch (Cmp) {
                    case 0:
                        return Mid;
                    case < 0:
                        Min = Mid + 1;
                        break;
                    default:
                        Max = Mid - 1;
                        break;
                }
            }

            return -1;
        }

        /// <summary> Determines whether the list contains an item with the specified key. </summary>
        /// <param name="Key"> The key. </param>
        /// <returns> <see langword="true"/> if the list contains an item with the specified key; otherwise, <see langword="false"/>. </returns>
        [MustUseReturnValue]
        public bool ContainsKey( TKey Key ) => IndexOf(Key) != -1;

        /// <summary> Attempts to get the value associated with the specified key. </summary>
        /// <param name="Key"> The key. </param>
        /// <param name="Value"> The value. </param>
        /// <returns> <see langword="true"/> if the value was found; otherwise, <see langword="false"/>. </returns>
        public bool TryGetValue( TKey Key, [MaybeNullWhen(false)] out TValue Value ) {
            int Index = IndexOf(Key);
            if (Index == -1) {
                Value = default;
                return false;
            }

            Value = _List[Index].Value;
            return true;
        }

        /// <summary> Gets or sets the value associated with the specified key. </summary>
        /// <param name="Key"> The key. </param>
        /// <returns> The value associated with the specified key. </returns>
        /// <value> The value associated with the specified key. </value>
        /// <exception cref="KeyNotFoundException"> The key was not found. </exception>
        public TValue this[ TKey Key ] {
            get {
                int Index = IndexOf(Key);
                if (Index == -1) {
                    throw new KeyNotFoundException($"The key '{Key}' was not found.");
                }

                return _List[Index].Value;
            }
            set {
                int Index = IndexOf(Key);
                if (Index == -1) {
                    Add(Key, value);
                } else {
                    _List[Index] = CreatePair(Key, value);
                    // _IsSorted = false; // No need to sort, as the key is not changed.
                }
            }
        }

        /// <inheritdoc cref="List{T}.Capacity"/>
        public int Capacity {
            get => _List.Capacity;
            set => _List.Capacity = value;
        }

        /// <inheritdoc cref="List{T}.Count"/>
        public int Count => _List.Count;

    }

    [Serializable, InlineProperty]
    [DebuggerDisplay("{" + nameof(Key) + "}: {" + nameof(Value) + "}")]
    public struct SortingPair<TKey, TValue> : IComparable<SortingPair<TKey, TValue>>, IComparable where TKey : notnull {
        [SerializeField, HorizontalGroup, HideLabel]
        TKey _Key;

        /// <summary> The key. </summary>
        public TKey Key {
            get => _Key;
            init => _Key = value;
        }

        [SerializeField, HorizontalGroup, HideLabel]
        TValue _Value;

        /// <summary> The value. </summary>
        public TValue Value {
            get => _Value;
            init => _Value = value;
        }

        /// <summary> Initialises a new instance of the <see cref="SortingPair{TKey,TValue}"/> struct. </summary>
        /// <param name="Key"> The key. </param>
        /// <param name="Value"> The value. </param>
        /// <param name="Comparer"> The comparer. </param>
        public SortingPair( TKey Key, TValue Value, IComparer<TKey> Comparer ) {
            _Key      = Key;
            _Value    = Value;
            _Comparer = Comparer;
        }

        readonly IComparer<TKey> _Comparer;

        public static implicit operator KeyValuePair<TKey, TValue>( SortingPair<TKey, TValue> Pair ) => new(Pair.Key, Pair.Value);
        public static implicit operator (TKey Key, TValue Value)( SortingPair<TKey, TValue>   Pair ) => (Pair.Key, Pair.Value);

        public void Deconstruct( out TKey Key, out TValue Value ) {
            Key   = this.Key;
            Value = this.Value;
        }

        #region Relational Members

        /// <inheritdoc />
        public int CompareTo( SortingPair<TKey, TValue> Other ) => _Comparer.Compare(Key, Other.Key);

        /// <inheritdoc />
        public int CompareTo( object? Obj ) {
            if (ReferenceEquals(null, Obj)) {
                return 1;
            }

            return Obj is SortingPair<TKey, TValue> Other ? CompareTo(Other) : throw new ArgumentException($"Object must be of type {nameof(SortingPair<TKey, TValue>)}");
        }

        public static bool operator <( SortingPair<TKey, TValue>  Left, SortingPair<TKey, TValue> Right ) => Left.CompareTo(Right) < 0;
        public static bool operator >( SortingPair<TKey, TValue>  Left, SortingPair<TKey, TValue> Right ) => Left.CompareTo(Right) > 0;
        public static bool operator <=( SortingPair<TKey, TValue> Left, SortingPair<TKey, TValue> Right ) => Left.CompareTo(Right) <= 0;
        public static bool operator >=( SortingPair<TKey, TValue> Left, SortingPair<TKey, TValue> Right ) => Left.CompareTo(Right) >= 0;

        #endregion

        #region Overrides of ValueType

        /// <inheritdoc />
        public override string ToString() => $"{Key} = {Value}";

        #endregion

    }
}
