using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.Common.Datatypes {
    [Serializable, InlineProperty, HideReferenceObjectPicker]
    public sealed class Observable<T> {
        /// <summary> The value of the observable. </summary>
        [ShowInInspector, Tooltip("The value."), InlineProperty, HideLabel, HideInEditorMode]
        public T Value {
            get => _Value;
            set {
                if (EqualityComparer<T>.Default.Equals(_Value, value)) { return; }

                _Value = value;
                Changed?.Invoke(value);
            }
        }

        [SerializeField, Tooltip("The value."), InlineProperty, HideLabel, HideInPlayMode]
        T _Value;

        /// <summary> Invoked when the value of the observable changes. </summary>
        public event Action<T>? Changed;

        /// <summary> Creates a new observable with the default value. </summary>
        public Observable() { }

        /// <summary> Creates a new observable with the given value. </summary>
        /// <param name="Value"> The value of the observable. </param>
        public Observable( T Value ) => this.Value = Value;

        public static implicit operator T( Observable<T> Observable ) => Observable.Value;
        public static implicit operator Observable<T>( T Value ) => new(Value);
        
    }
}
