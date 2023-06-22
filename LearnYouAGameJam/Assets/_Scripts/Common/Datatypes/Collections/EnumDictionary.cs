using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.Common.Datatypes.Collections {
    [Serializable]
    public sealed class EnumDictionary<TEnum, TValue> : IDictionary<TEnum, TValue>, IReadOnlyDictionary<TEnum, TValue> where TEnum : struct, Enum {
        [SerializeField, HideInInspector] SortedList<Pair> _Pairs = new();

        /// <summary> Creates a new empty <see cref="EnumDictionary{TEnum,TValue}"/>. </summary>
        public EnumDictionary() { }

        /// <summary> Creates a new <see cref="EnumDictionary{TEnum,TValue}"/>, auto-populating it with the given <paramref name="DefaultValue"/> for each enum value. </summary>
        /// <param name="DefaultValue"> The default value to use for each enum value. </param>
        public EnumDictionary( TValue DefaultValue ) {
            foreach (TEnum Value in Enum<TEnum>.Values.Distinct()) {
                _Pairs.Add(new(Value, DefaultValue));
            }
        }

        #if UNITY_EDITOR
        bool MissingAny() => Enum<TEnum>.Values.Any(Value => !ContainsKey(Value));
        [Button("Auto-Populate"), ShowIf(nameof(MissingAny)), ContextMenu("Auto-Populate")]
        void AutoPopulate() {
            TValue DefaultValue = default!;
            foreach (TEnum Value in Enum<TEnum>.Values.Distinct()) {
                if (!ContainsKey(Value)) {
                    _Pairs.Add(new(Value, DefaultValue));
                }
            }
        }
        #endif

        [Serializable]
        sealed class Pair : IEquatable<Pair>, IComparable<Pair>, IComparable {
            [SerializeField, HorizontalGroup] TEnum  _Key;
            [SerializeField, HorizontalGroup] TValue _Value;

            public Pair( TEnum Key, TValue Value ) {
                _Key   = Key;
                _Value = Value;
            }
            public Pair( KeyValuePair<TEnum, TValue> Pair ) {
                _Key   = Pair.Key;
                _Value = Pair.Value;
            }
            public Pair( TEnum Key ) {
                _Key   = Key;
                _Value = default!;
            }
            public Pair() { } // For (de/)serialisation

            /// <inheritdoc cref="KeyValuePair{TKey,TValue}.Key"/>
            public TEnum Key {
                get => _Key;
                set => _Key = value;
            }

            /// <inheritdoc cref="KeyValuePair{TKey,TValue}.Value"/>
            public TValue Value {
                get => _Value;
                set => _Value = value;
            }

            public static explicit operator KeyValuePair<TEnum, TValue>( Pair Pair ) => new(Pair.Key, Pair.Value);

            #region Equality Members

            /// <inheritdoc />
            public bool Equals( Pair? Other ) {
                if (ReferenceEquals(null, Other)) {
                    return false;
                }

                if (ReferenceEquals(this, Other)) {
                    return true;
                }

                return _Key.Equals(Other._Key);
            }

            /// <inheritdoc />
            public override bool Equals( object? Obj ) => ReferenceEquals(this, Obj) || Obj is Pair Other && Equals(Other);

            /// <inheritdoc />
            public override int GetHashCode() => _Key.GetHashCode();

            public static bool operator ==( Pair? Left, Pair? Right ) => Equals(Left, Right);
            public static bool operator !=( Pair? Left, Pair? Right ) => !Equals(Left, Right);

            #endregion

            #region Relational Members

            /// <inheritdoc />
            public int CompareTo( Pair? Other ) {
                if (ReferenceEquals(this, Other)) {
                    return 0;
                }

                if (ReferenceEquals(null, Other)) {
                    return 1;
                }

                return _Key.CompareTo(Other._Key);
            }

            /// <inheritdoc />
            public int CompareTo( object? Obj ) {
                if (ReferenceEquals(null, Obj)) {
                    return 1;
                }

                if (ReferenceEquals(this, Obj)) {
                    return 0;
                }

                return Obj is Pair Other ? CompareTo(Other) : throw new ArgumentException($"Object must be of type {nameof(Pair)}");
            }

            public static bool operator <( Pair?  Left, Pair? Right ) => Comparer<Pair?>.Default.Compare(Left, Right) < 0;
            public static bool operator >( Pair?  Left, Pair? Right ) => Comparer<Pair?>.Default.Compare(Left, Right) > 0;
            public static bool operator <=( Pair? Left, Pair? Right ) => Comparer<Pair?>.Default.Compare(Left, Right) <= 0;
            public static bool operator >=( Pair? Left, Pair? Right ) => Comparer<Pair?>.Default.Compare(Left, Right) >= 0;

            #endregion

        }

        [SerializeField, HideInInspector] TValue _Default;

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TEnum, TValue>> GetEnumerator() { yield break; }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Implementation of IReadOnlyCollection<out KeyValuePair<TEnum,TValue>>

        /// <inheritdoc />
        int IReadOnlyCollection<KeyValuePair<TEnum, TValue>>.Count => _Pairs.Count;

        #endregion

        #region Implementation of ICollection<KeyValuePair<TEnum,TValue>>

        /// <inheritdoc />
        void ICollection<KeyValuePair<TEnum,TValue>>.Add( KeyValuePair<TEnum, TValue> Item ) => Add(new(Item));

        /// <inheritdoc />
        public void Clear() => _Pairs.Clear();

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TEnum,TValue>>.Contains( KeyValuePair<TEnum, TValue> Item ) => ContainsKey(Item.Key);

        /// <inheritdoc />
        void ICollection<KeyValuePair<TEnum, TValue>>.CopyTo( KeyValuePair<TEnum, TValue>[] Array, int ArrayIndex ) {
            foreach (Pair Pair in _Pairs) {
                Array[ArrayIndex++] = (KeyValuePair<TEnum, TValue>)Pair;
            }
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TEnum,TValue>>.Remove( KeyValuePair<TEnum, TValue> Item ) => Remove(Item.Key);

        /// <inheritdoc cref="ICollection{T}.Count"/>
        public int Count => _Pairs.Count;

        /// <inheritdoc />
        int ICollection<KeyValuePair<TEnum,TValue>>.Count => _Pairs.Count;

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TEnum,TValue>>.IsReadOnly => false;

        #endregion

        readonly Comparer<Pair> _PairKeyComparer = Comparer<Pair>.Create(( A, B ) => A.Key.CompareTo(B.Key));

        #region Implementation of IDictionary<TEnum,TValue>

        /// <inheritdoc />
        public void Add( TEnum Key, TValue Value ) => Add(new(Key, Value));

        void Add( Pair P ) {
            // Case 1: Key is already present
            //         Replace the value
            // Case 2: Key is not present
            //         Add the key-value pair
            // Case 3: Key is not defined
            if (!Enum<TEnum>.IsDefined(P.Key)) {
                throw new ArgumentException($"Key {P.Key} is not defined in enum {typeof(TEnum).Name}");
            }

            if (_Pairs.Contains(P)) {
                _Pairs.Remove(P);
            }
            _Pairs.Add(P);
        }

        /// <inheritdoc />
        bool IReadOnlyDictionary<TEnum, TValue>.ContainsKey( TEnum Key ) => ContainsKey(Key);

        /// <inheritdoc />
        bool IReadOnlyDictionary<TEnum, TValue>.TryGetValue( TEnum Key, out TValue Value ) => TryGetValue(Key, out Value);

        /// <inheritdoc />
        public bool Remove( TEnum Key ) => Remove(new Pair(Key));

        bool Remove( Pair P ) => _Pairs.Remove(P);

        /// <inheritdoc />
        public bool ContainsKey( TEnum Key ) => Enum<TEnum>.IsDefined(Key);

        /// <inheritdoc />
        public bool TryGetValue( TEnum Key, out TValue Value ) {
            if (!Enum<TEnum>.IsDefined(Key)) {
                Value = default!;
                return false;
            }

            Pair P = new(Key);
            if (_Pairs.Contains(P)) {
                Value = _Pairs[_Pairs.IndexOf(P)].Value;
                return true;
            }

            Value = default!;
            return false;
        }

        /// <inheritdoc cref="IDictionary{TKey,TValue}.this" />
        public TValue this[ TEnum Key ] {
            get {
                if (!Enum<TEnum>.IsDefined(Key)) {
                    throw new KeyNotFoundException($"Key {Key} is not defined in enum {typeof(TEnum).Name}");
                }

                Pair P = new(Key);
                if (_Pairs.Contains(P)) {
                    return _Pairs[_Pairs.IndexOf(P)].Value;
                }

                return _Default;
            }
            set => Add(Key, value);
        }

        /// <inheritdoc />
        public IEnumerable<TEnum> Keys => _Pairs.Select(P => P.Key);

        /// <inheritdoc />
        public IEnumerable<TValue> Values => _Pairs.Select(P => P.Value);

        /// <inheritdoc />
        ICollection<TEnum> IDictionary<TEnum, TValue>.Keys => _Pairs.Select(P => P.Key).ToArray(_Pairs.Count);

        /// <inheritdoc />
        ICollection<TValue> IDictionary<TEnum, TValue>.Values => _Pairs.Select(P => P.Value).ToArray(_Pairs.Count);

        #endregion

    }
}
