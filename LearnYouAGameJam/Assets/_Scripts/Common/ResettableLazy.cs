using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace LYGJ.Common {
    public sealed class ResettableLazy<T> {

        readonly Func<T> _Factory;
        T?               _Value;

        /// <summary> Gets the value. </summary>
        /// <remarks> If the value has not been initialised yet, it will be initialised. </remarks>
        /// <returns> The value. </returns>
        public T Value {
            get {
                if (!IsValueCreated) {
                    _Value         = _Factory();
                    IsValueCreated = true;
                }
                return _Value;
            }
        }

        /// <summary> Gets whether the value has been initialised. </summary>
        /// <returns> <see langword="true"/> if the value has been initialised, <see langword="false"/> otherwise. </returns>
        [MemberNotNullWhen(true, nameof(_Value))]
        public bool IsValueCreated { get; private set; } = false;

        /// <summary> Attempts to get the value, if already initialised. </summary>
        /// <param name="Value"> [out] The value. </param>
        /// <returns> <see langword="true"/> if the value was already initialised, <see langword="false"/> otherwise. </returns>
        public bool TryGetValue( [NotNullWhen(true)] out T? Value ) {
            Value = _Value;
            return IsValueCreated;
        }

        /// <summary> Resets the value. </summary>
        public void Reset() {
            _Value         = default;
            IsValueCreated = false;
        }

        /// <summary> Initialises a new instance of the <see cref="ResettableLazy{T}"/> class. </summary>
        /// <param name="Factory"> The factory. </param>
        public ResettableLazy( Func<T> Factory ) => _Factory = Factory;

        /// <summary> Initialises a new instance of the <see cref="ResettableLazy{T}"/> class. </summary>
        /// <param name="Value"> The value. </param>
        [Obsolete("Literal values are not lazy, and may not be reset sufficiently.", true)]
        public ResettableLazy( T Value ) {
            _Value         = Value;
            IsValueCreated = true;

            T Constructor() => Value;
            _Factory = Constructor;
        }
    }
}
