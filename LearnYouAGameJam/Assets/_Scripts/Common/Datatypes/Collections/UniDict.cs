using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.Common.Datatypes.Collections {
    [Serializable, InlineProperty]
    public sealed class UniDict<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver where TKey : notnull {
        [ShowInInspector, HideLabel, InlineProperty]
        readonly Dictionary<TKey, TValue> _Dict = new();

        #region Constructors

        public UniDict() { }
        public UniDict( int Capacity ) : this() => _Dict = new(Capacity);

        public UniDict( IEnumerable<KeyValuePair<TKey, TValue>> Items ) : this() {
            foreach ( (TKey Key, TValue Value) in Items ) {
                _Dict.Add(Key, Value);
            }
        }
        public UniDict( params KeyValuePair<TKey, TValue>[] Items ) : this((IEnumerable<KeyValuePair<TKey, TValue>>)Items) { }

        #endregion

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _Dict.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Dict).GetEnumerator();

        #endregion

        #region Implementation of ICollection<KeyValuePair<TKey,TValue>>

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey,TValue>>.Add( KeyValuePair<TKey, TValue> Item ) => _Dict.Add(Item.Key, Item.Value);

        /// <inheritdoc />
        public void Clear() => _Dict.Clear();

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey,TValue>>.Contains( KeyValuePair<TKey, TValue> Item ) => _Dict.ContainsKey(Item.Key);

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey,TValue>>.CopyTo( KeyValuePair<TKey, TValue>[] Array, int ArrayIndex ) => ((ICollection<KeyValuePair<TKey, TValue>>)_Dict).CopyTo(Array, ArrayIndex);

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey,TValue>>.Remove( KeyValuePair<TKey, TValue> Item ) => _Dict.Remove(Item.Key);

        /// <inheritdoc cref="ICollection{T}.Count" />
        public int Count => _Dict.Count;

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey,TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_Dict).IsReadOnly;

        #endregion

        #region Implementation of IDictionary<TKey,TValue>

        /// <inheritdoc />
        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue( TKey Key, out TValue Value ) => _Dict.TryGetValue(Key, out Value);

        /// <inheritdoc />
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey( TKey Key ) => _Dict.ContainsKey(Key);

        /// <inheritdoc />
        public bool ContainsKey( TKey Key ) => _Dict.ContainsKey(Key);

        /// <inheritdoc />
        public bool TryGetValue( TKey Key, out TValue Value ) => _Dict.TryGetValue(Key, out Value);

        /// <inheritdoc />
        public void Add( TKey Key, TValue Value ) => _Dict.Add(Key, Value);

        /// <inheritdoc />
        public bool Remove( TKey Key ) => _Dict.Remove(Key);

        /// <inheritdoc cref="IDictionary{TKey,TValue}.this" />
        public TValue this[ TKey Key ] {
            get => _Dict[Key];
            set => _Dict[Key] = value;
        }

        /// <inheritdoc />
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _Dict.Keys;

        /// <inheritdoc />
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _Dict.Values;

        /// <inheritdoc />
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => _Dict.Keys;

        /// <inheritdoc />
        ICollection<TValue> IDictionary<TKey, TValue>.Values => _Dict.Values;

        /// <inheritdoc cref="IDictionary{TKey,TValue}.Keys" />
        public IReadOnlyCollection<TKey> Keys => _Dict.Keys;

        /// <inheritdoc cref="IDictionary{TKey,TValue}.Values" />
        public IReadOnlyCollection<TValue> Values => _Dict.Values;

        #endregion

        #region Implementation of ISerializationCallbackReceiver

        [Serializable] sealed class Pair {
            [SerializeField] TKey   _Key;
            [SerializeField] TValue _Value;

            public Pair( TKey Key, TValue Value ) {
                _Key   = Key;
                _Value = Value;
            }
            public Pair( KeyValuePair<TKey, TValue> Pair ) : this(Pair.Key, Pair.Value) { }
            public Pair() { }

            /// <summary> The key of the pair. </summary>
            public TKey Key => _Key;

            /// <summary> The value of the pair. </summary>
            public TValue Value => _Value;

            public static implicit operator KeyValuePair<TKey, TValue>( Pair Pair ) => new(Pair.Key, Pair.Value);
            public static implicit operator Pair( KeyValuePair<TKey, TValue> Pair ) => new(Pair);
        }

        [SerializeField, HideInInspector] Pair[] _Pairs = Array.Empty<Pair>();

        /// <inheritdoc />
        public void OnBeforeSerialize() {
            _Pairs = new Pair[_Dict.Count];
            int I = 0;
            foreach ( KeyValuePair<TKey, TValue> Pair in _Dict ) {
                _Pairs[I++] = Pair;
            }
        }

        /// <inheritdoc />
        public void OnAfterDeserialize() {
            _Dict.Clear();
            foreach ( Pair Pair in _Pairs ) {
                _Dict.Add(Pair.Key, Pair.Value);
            }
        }

        #endregion

    }
}
