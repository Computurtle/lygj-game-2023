using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LYGJ.Common {
    public abstract class SingletonMB<T> : MonoBehaviour where T : SingletonMB<T> {
        // ReSharper disable once InconsistentNaming
        protected static T? _Instance;

        /// <summary> Gets the singleton instance in the scene. </summary>
        public static T Instance {
            get {
                if (_Instance == null) {
                    #if UNITY_EDITOR
                    if (Application.isPlaying) {
                        #endif
                        Debug.LogWarning($"No instance of {typeof(T).GetNiceName()} was set. Make sure that cross-script dependencies are performed in Start() or later.");
                        #if UNITY_EDITOR
                    }
                    #endif
                    _Instance = FindObjectOfType<T>();
                    if (_Instance == null) {
                        throw new SingletonNotFoundException($"No instance of {typeof(T).GetNiceName()} found in the scene.");
                    }
                }
                return _Instance;
            }
        }

        /// <summary> Attempts to get the singleton instance in the scene. </summary>
        /// <param name="Instance"> The singleton instance. </param>
        /// <returns> <see langword="true"/> if the singleton was found; otherwise, <see langword="false"/>. </returns>
        public static bool TryGetInstance( [MaybeNullWhen(false)] out T Instance ) {
            if (_Instance == null) {
                _Instance = FindObjectOfType<T>();
            }
            Instance = _Instance;
            return Instance != null;
        }

        protected virtual void Awake() {
            #if UNITY_EDITOR
            if (_Instance != null && _Instance != this) {
                Debug.LogWarning($"Multiple instances of {typeof(T).Name} found in the scene.");
                foreach (T Instance in FindObjectsOfType<T>()) {
                    Debug.LogWarning($"\tInstance: {Instance.name}.", Instance);
                }
                return;
            }
            #endif
            _Instance = (T)this;
        }

        protected virtual void OnDestroy() {
            if (_Instance == this) {
                _Instance = null;
            }
        }
    }

    public static class SingletonMB {
        /// <summary> Attempts to get the singleton instance in the scene. </summary>
        /// <param name="Tp"> The type of the singleton. </param>
        /// <param name="Instance"> The singleton instance. </param>
        /// <returns> <see langword="true"/> if the singleton was found; otherwise, <see langword="false"/>. </returns>
        [Obsolete("Use SingletonMB<T> instead.")]
        public static bool TryGetSingleton( Type Tp, [MaybeNullWhen(false)] out MonoBehaviour Instance ) {
            Type GenTp = typeof(SingletonMB<>).MakeGenericType(Tp);
            Debug.Assert(GenTp.IsAssignableFrom(Tp), $"Type {Tp.Name} does not inherit from {GenTp.Name}.");

            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                Instance = Object.FindObjectOfType(Tp) as MonoBehaviour;
                return Instance != null;
            }
            #endif

            PropertyInfo? Prop = GenTp.GetProperty("Instance");
            if (Prop == null) {
                Debug.LogError($"No instance property found for {Tp.Name} in {GenTp.Name}.");
                Instance = null;
                return false;
            }

            try {
                if ((Instance = Prop.GetValue(null) as MonoBehaviour) != null) {
                    return true;
                }
            } catch (SingletonNotFoundException) {
            } catch (Exception Ex) {
                Debug.LogError($"Failed to get instance property for {Tp.Name} in {GenTp.Name}. {Ex}");
            }

            Instance = Object.FindObjectOfType(Tp) as MonoBehaviour;
            return Instance != null;
        }

        /// <summary> Gets the singleton instance in the scene. </summary>
        /// <param name="Tp"> The type of the singleton. </param>
        /// <returns> The singleton instance. </returns>
        [Obsolete("Use SingletonMB<T> instead.")]
        public static MonoBehaviour? GetSingleton( Type Tp ) {
            Type GenTp = typeof(SingletonMB<>).MakeGenericType(Tp);
            Debug.Assert(GenTp.IsAssignableFrom(Tp), $"Type {Tp.Name} does not inherit from {GenTp.Name}.");

            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                return Object.FindObjectOfType(Tp) as MonoBehaviour;
            }
            #endif
            return GenTp.GetProperty("Instance")?.GetValue(null) as MonoBehaviour;
        }
    }

    public sealed class SingletonNotFoundException : Exception {
        public SingletonNotFoundException() : base("Singleton not found.") { }
        public SingletonNotFoundException( string Message ) : base(Message) { }
    }
}
