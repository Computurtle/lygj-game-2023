#define FORCE_RUNTIME
// #define LOG_ASSEMBLIES
// #define LOG_PRODUCT_NAME
#define ONLY_SELF_ASSEMBLIES

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if LOG_ASSEMBLIES
using System.Linq;
#endif

namespace LYGJ.Common {
    public static class RuntimeTypeCache {
        /// <inheritdoc cref="UnityEditor.TypeCache.GetTypesWithAttribute"/>
        public static RuntimeTypeCollection GetTypesWithAttribute<T>() where T : Attribute => GetTypesWithAttribute(typeof(T));

        /// <inheritdoc cref="UnityEditor.TypeCache.GetTypesWithAttribute"/>
        public static RuntimeTypeCollection GetTypesWithAttribute( Type Type ) {
            #if !FORCE_RUNTIME && UNITY_EDITOR
            return UnityEditor.TypeCache.GetTypesWithAttribute(Type);
            #else
            RuntimeTypeCollection Types = new();
            foreach (Type T in _AllTypes) {
                if (T.GetCustomAttribute(Type) != null) {
                    Types.Add(T);
                }
            }
            return Types;
            #endif
        }

        /// <inheritdoc cref="UnityEditor.TypeCache.GetMethodsWithAttribute"/>
        public static RuntimeMethodCollection GetMethodsWithAttribute<T>() where T : Attribute => GetMethodsWithAttribute(typeof(T));

        /// <inheritdoc cref="UnityEditor.TypeCache.GetMethodsWithAttribute"/>
        public static RuntimeMethodCollection GetMethodsWithAttribute( Type Type ) {
            #if !FORCE_RUNTIME && UNITY_EDITOR
            return UnityEditor.TypeCache.GetMethodsWithAttribute(Type);
            #else
            RuntimeMethodCollection Methods = new();
            foreach (MethodInfo M in _AllMethods) {
                if (M.GetCustomAttribute(Type, true) != null) {
                    // Debug.Log($"Found method with attribute {Type.GetNiceName()}: {M.GetNiceName()}");
                    Methods.Add(M);
                }/* else {
                    Debug.Log($"Method {M.GetNiceName()} does not have attribute {Type.GetNiceName()}");
                }*/
            }
            return Methods;
            #endif
        }

        /// <inheritdoc cref="UnityEditor.TypeCache.GetFieldsWithAttribute"/>
        public static RuntimeFieldCollection GetFieldsWithAttribute<T>() where T : Attribute => GetFieldsWithAttribute(typeof(T));

        /// <inheritdoc cref="UnityEditor.TypeCache.GetFieldsWithAttribute"/>
        public static RuntimeFieldCollection GetFieldsWithAttribute( Type Type ) {
            #if !FORCE_RUNTIME && UNITY_EDITOR
            return UnityEditor.TypeCache.GetFieldsWithAttribute(Type);
            #else
            RuntimeFieldCollection Fields = new();
            foreach (FieldInfo F in _AllFields) {
                if (F.GetCustomAttribute(Type) != null) {
                    Fields.Add(F);
                }
            }
            return Fields;
            #endif
        }

        /// <inheritdoc cref="UnityEditor.TypeCache.GetTypesDerivedFrom"/>
        public static RuntimeTypeCollection GetTypesDerivedFrom<T>() => GetTypesDerivedFrom(typeof(T));

        /// <inheritdoc cref="UnityEditor.TypeCache.GetTypesDerivedFrom"/>
        public static RuntimeTypeCollection GetTypesDerivedFrom( Type Type ) {
            #if !FORCE_RUNTIME && UNITY_EDITOR
            return UnityEditor.TypeCache.GetTypesDerivedFrom(Type);
            #else
            return Type.IsInterface ? GetTypesDerivedFromInterface(Type) : GetTypesDerivedFromType(Type);
            #endif
        }

        static RuntimeTypeCollection GetTypesDerivedFromType( Type Type ) {
            RuntimeTypeCollection Types = new();
            foreach (Type T in _AllTypes) {
                if (T.IsSubclassOf(Type)) {
                    Types.Add(T);
                }
            }

            return Types;
        }

        static RuntimeTypeCollection GetTypesDerivedFromInterface( Type Type ) {
            RuntimeTypeCollection Types = new();
            foreach (Type T in _AllTypes) {
                if (T.GetInterface(Type.FullName!) != null) {
                    Types.Add(T);
                }
            }

            return Types;
        }

        static readonly RuntimeTypeCollection   _AllTypes;
        static readonly RuntimeMethodCollection _AllMethods;
        static readonly RuntimeFieldCollection  _AllFields;

        static bool ContainsAnyWord( IEnumerable<string> A, string B, StringComparison Comparison = StringComparison.OrdinalIgnoreCase ) {
            foreach (string AWord in A) {
                if (B.Contains(AWord, Comparison)) {
                    return true;
                }
            }

            return false;
        }

        static bool ContainsAnyWord( string A, string B, StringComparison Comparison ) => ContainsAnyWord(A.Split(' '), B, Comparison);

        static IEnumerable<Assembly> GetUserAssemblies() {
            #if LOG_ASSEMBLIES
            List<Assembly> Included = new(), Excluded = new();
            #endif
            #if ONLY_SELF_ASSEMBLIES
            string   Product   = Application.productName;
            string[] SelfWords = Product.Split(' ');
            #if LOG_PRODUCT_NAME
            Debug.Log($"[{nameof(RuntimeTypeCache)}] Product name: {Product} ; Words: '{string.Join("', '", SelfWords)}'");
            #endif
            #endif
            foreach (Assembly Assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (!Assembly.IsFullyTrusted
                    || Assembly.IsDynamic
                    || Assembly.ReflectionOnly
                    #if ONLY_SELF_ASSEMBLIES
                    || !ContainsAnyWord(SelfWords, Assembly.FullName)
                    #else
                    || Assembly.FullName.StartsWith("Bee")
                    || Assembly.FullName.StartsWith("Cecil")
                    || Assembly.FullName.StartsWith("Unity")
                    || Assembly.FullName.StartsWith("System")
                    || Assembly.FullName.StartsWith("Mono")
                    || Assembly.FullName.StartsWith("mscorlib")
                    || Assembly.FullName.StartsWith("net")
                    #endif
                ) {
                    #if LOG_ASSEMBLIES
                    Excluded.Add(Assembly);
                    #endif
                    continue;
                }

                #if LOG_ASSEMBLIES
                Included.Add(Assembly);
                #endif
                yield return Assembly;
            }

            #if LOG_ASSEMBLIES
            Debug.Log($"[{nameof(RuntimeTypeCache)}] Included {Included.Count} assemblies: {string.Join(", ", Included.Select(A => A.GetName().Name))}");
            Debug.Log($"[{nameof(RuntimeTypeCache)}] Excluded {Excluded.Count} assemblies: {string.Join(", ", Excluded.Select(A => A.GetName().Name))}");
            #endif
        }

        static RuntimeTypeCache() {
            #if !FORCE_RUNTIME && UNITY_EDITOR
            _AllTypes   = RuntimeTypeCollection.Empty;
            _AllMethods = RuntimeMethodCollection.Empty;
            _AllFields  = RuntimeFieldCollection.Empty;
            #else
            // Scan all assemblies for types, methods, and fields.
            Debug.Log($"[{nameof(RuntimeTypeCache)}] Scanning assemblies for types, methods, and fields...");
            float Start = Time.realtimeSinceStartup;
            _AllTypes   = new();
            _AllMethods = new();
            _AllFields  = new();

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            // foreach (Assembly? Assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach (Assembly? Assembly in GetUserAssemblies()) {
                foreach (Type Type in Assembly.GetTypes()) {
                    _AllTypes.Add(Type);
                    foreach (MethodInfo Method in Type.GetMethods(Flags)) {
                        _AllMethods.Add(Method);
                    }

                    foreach (FieldInfo Field in Type.GetFields(Flags)) {
                        _AllFields.Add(Field);
                    }
                }
            }
            Debug.Log($"[{nameof(RuntimeTypeCache)}] Scanned {_AllTypes.Count} types, {_AllMethods.Count} methods, and {_AllFields.Count} fields in {Time.realtimeSinceStartup - Start:0.000} seconds.");
            #endif
        }

        public abstract class RuntimeCollection<T> : IList<T>, IReadOnlyList<T> {
            readonly IList<T> _Values;

            internal RuntimeCollection( IList<T> Values ) => _Values = Values;

            #region Implementation of IEnumerable

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator() => _Values.GetEnumerator();

            #endregion

            #region Implementation of IEnumerable<T>

            /// <inheritdoc />
            public IEnumerator<T> GetEnumerator() => _Values.GetEnumerator();

            #endregion

            #region Implementation of IReadOnlyCollection<T>

            /// <inheritdoc cref="ICollection{T}.Count"/>
            public int Count => _Values.Count;

            /// <inheritdoc />
            public bool Contains( T Item ) => _Values.Contains(Item);

            #endregion

            #region Implementation of ICollection<T>

            /// <inheritdoc />
            void ICollection<T>.Add( T Item ) => _Values.Add(Item);

            /// <inheritdoc cref="ICollection{T}.Add"/>
            internal void Add( T Item ) => _Values.Add(Item);

            /// <inheritdoc cref="ICollection{T}.Add"/>
            internal void Add( IEnumerable<T> Items ) {
                foreach (T Item in Items) {
                    _Values.Add(Item);
                }
            }

            /// <inheritdoc />
            void ICollection<T>.Clear() => _Values.Clear();

            /// <inheritdoc />
            void ICollection<T>.CopyTo( T[] Array, int ArrayIndex ) => _Values.CopyTo(Array, ArrayIndex);

            /// <inheritdoc />
            bool ICollection<T>.Remove( T Item ) => _Values.Remove(Item);

            /// <inheritdoc />
            bool ICollection<T>.IsReadOnly => _Values.IsReadOnly;

            #endregion

            #region Implementation of IReadOnlyList<Type>

            /// <inheritdoc />
            public int IndexOf( T Item ) => _Values.IndexOf(Item);

            /// <inheritdoc />
            public T this[ int Index ] => _Values[Index];

            #endregion

            #region Implementation of IList<Type>

            /// <inheritdoc />
            void IList<T>.Insert( int Index, T Item ) => _Values.Insert(Index, Item);

            /// <inheritdoc />
            void IList<T>.RemoveAt( int Index ) => _Values.RemoveAt(Index);

            /// <inheritdoc />
            T IList<T>.this[ int Index ] {
                get => _Values[Index];
                set => _Values[Index] = value;
            }

            #endregion

        }

        public sealed class RuntimeTypeCollection : RuntimeCollection<Type> {

            /// <inheritdoc />
            internal RuntimeTypeCollection( IList<Type> Types ) : base(Types) { }

            /// <inheritdoc />
            internal RuntimeTypeCollection() : base(new List<Type>()) { }

            /// <summary> An empty collection of <see cref="Type"/>s. </summary>
            public static readonly RuntimeTypeCollection Empty = new(Type.EmptyTypes);

            #if UNITY_EDITOR
            public static implicit operator RuntimeTypeCollection( UnityEditor.TypeCache.TypeCollection Collection ) => new(Collection);
            #endif
        }

        public sealed class RuntimeFieldCollection : RuntimeCollection<FieldInfo> {

            /// <inheritdoc />
            internal RuntimeFieldCollection( IList<FieldInfo> Fields ) : base(Fields) { }

            /// <inheritdoc />
            internal RuntimeFieldCollection() : base(new List<FieldInfo>()) { }

            /// <summary> An empty collection of <see cref="FieldInfo"/>s. </summary>
            public static readonly RuntimeFieldCollection Empty = new(Array.Empty<FieldInfo>());

            #if UNITY_EDITOR
            public static implicit operator RuntimeFieldCollection( UnityEditor.TypeCache.FieldInfoCollection Collection ) => new(Collection);
            #endif
        }

        public sealed class RuntimeMethodCollection : RuntimeCollection<MethodInfo> {

            /// <inheritdoc />
            internal RuntimeMethodCollection( IList<MethodInfo> Methods ) : base(Methods) { }

            /// <inheritdoc />
            internal RuntimeMethodCollection() : base(new List<MethodInfo>()) { }

            /// <summary> An empty collection of <see cref="MethodInfo"/>s. </summary>
            public static readonly RuntimeMethodCollection Empty = new(Array.Empty<MethodInfo>());

            #if UNITY_EDITOR
            public static implicit operator RuntimeMethodCollection( UnityEditor.TypeCache.MethodCollection Collection ) => new(Collection);
            #endif
        }
    }
}
