using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LYGJ.Common.Datatypes.Collections {
    public sealed class PriorityList<TEnum, T> where TEnum : struct, Enum where T : notnull {
        T                             _Default;
        readonly SortedList<TEnum, T> _Overrides;

        /// <summary> Event signature for when the value changes. </summary>
        /// <param name="Value"> The new effective value. </param>
        public delegate void ValueChangedEventHander( T Value );

        /// <summary> Raised when the effective value changes. </summary>
        public event ValueChangedEventHander? ValueChanged;

        /// <summary> Adds a new override, or updates an existing one. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <param name="Value"> The value of the override. </param>
        public void AddOverride( TEnum Priority, T Value ) {
            _Overrides[Priority] = Value;
            Update();
        }

        /// <summary> Removes an override. </summary>
        /// <param name="Priority"> The priority of the override. </param>
        /// <returns> <see langword="true"/> if the override was removed, <see langword="false"/> if it didn't exist. </returns>
        public bool RemoveOverride( TEnum Priority ) {
            if (_Overrides.Remove(Priority)) {
                Update();
                return true;
            }

            return false;
        }

        /// <summary> Clears all overrides. </summary>
        public void ClearOverrides() {
            _Overrides.Clear();
            Update();
        }

        /// <summary> Gets or sets the default value. </summary>
        public T DefaultValue {
            get => _Default;
            set {
                if (EqualityComparer<T>.Default.Equals(_Default, value)) { return; }
                _Default = value;
                Update();
            }
        }

        /// <summary> Gets the effective value. </summary>
        public T Value => _Overrides.Count > 0 ? _Overrides.Values[0] : _Default;

        void Update() => ValueChanged?.Invoke(Value);

        sealed class InverseComparer : IComparer<TEnum> {
            readonly IComparer<TEnum> _Comparer;

            public InverseComparer() : this(Comparer<TEnum>.Default) { }
            public InverseComparer( IComparer<TEnum> Comparer ) => _Comparer = Comparer;

            /// <inheritdoc />
            public int Compare( TEnum X, TEnum Y ) => _Comparer.Compare(Y, X);
        }

        /// <summary> Creates a new priority list. </summary>
        /// <param name="Default"> The default value. </param>
        /// <param name="Capacity"> The initial capacity of the overrides. </param>
        /// <param name="Comparer"> The comparer to use for the priority. </param>
        public PriorityList( T Default = default!, int Capacity = 0, IComparer<TEnum>? Comparer = null ) {
            _Default   = Default;
            _Overrides = new(Capacity, Comparer is null ? new InverseComparer() : new(Comparer));
        }

        public static implicit operator T( PriorityList<TEnum, T> List ) => List.Value;

        /// <summary> Gets or sets the value for the given priority. </summary>
        /// <param name="Priority"> The priority to get or set. </param>
        /// <returns> The value for the given priority. </returns>
        /// <value> The value for the given priority. </value>
        public T this[TEnum Priority] {
            get => _Overrides.TryGetValue(Priority, out T? Value) ? Value : _Default;
            set => AddOverride(Priority, value);
        }
    }
}
