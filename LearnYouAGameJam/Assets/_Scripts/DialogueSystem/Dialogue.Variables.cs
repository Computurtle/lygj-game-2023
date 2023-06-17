using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem {
    public static partial class Dialogue {
        public static class Variables {
            static readonly Stack<Dictionary<string, object>> _Variables = new(); // Name is lower invariant.

            [ExecuteOnReload]
            static void Cleanup() {
                _Variables.Clear();
                _Variables.TrimExcess();
            }

            static bool InScope( [NotNullWhen(true)] out Dictionary<string, object>? Vars ) {
                if (!_Variables.TryPeek(out Vars)) {
                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Variables)}] No variables are currently in scope.");
                    return false;
                }

                return true;
            }

            /// <summary> Gets the variable with the given name. </summary>
            /// <typeparam name="T"> The type of the variable to get. </typeparam>
            /// <param name="Name"> The name of the variable to get. </param>
            /// <param name="Fallback"> The fallback value to return if the variable does not exist. </param>
            /// <returns> The variable with the given name. </returns>
            public static T Get<T>( string Name, T Fallback = default ) where T : struct {
                if (!InScope(out Dictionary<string, object>? Vars)) {
                    return Fallback;
                }

                if (!Vars.TryGetValue(Name.ToLowerInvariant(), out object? Value)) {
                    return Fallback;
                }

                if (Value is T Casted) {
                    return Casted;
                }

                // Edge-case: If T is typeof(string), then we can just return the ToString() of the value.
                if (typeof(T) == typeof(string)) {
                    return (T)(object)Value.ToString();
                }

                Debug.LogError($"[{nameof(Dialogue)}/{nameof(Variables)}] Variable '{Name}' is not of type '{typeof(T).GetNiceName()}'.");
                return Fallback;
            }

            /// <summary> Sets the variable with the given name. </summary>
            /// <typeparam name="T"> The type of the variable to set. </typeparam>
            /// <param name="Name"> The name of the variable to set. </param>
            /// <param name="Value"> The value to set the variable to. </param>
            public static void Set<T>( string Name, T Value ) where T : struct {
                if (!InScope(out Dictionary<string, object>? Vars)) {
                    return;
                }

                Vars[Name.ToLowerInvariant()] = Value;
            }

            /// <summary> Deletes the variable with the given name. </summary>
            /// <param name="Name"> The name of the variable to delete. </param>
            /// <returns> <see langword="true"/> if the variable was deleted; otherwise, <see langword="false"/>. </returns>
            public static bool Delete( string Name ) {
                if (!InScope(out Dictionary<string, object>? Vars)) {
                    return false;
                }

                return Vars.Remove(Name.ToLowerInvariant());
            }

            /// <summary> Clears all variables. </summary>
            public static void Clear() {
                _Variables.Clear();
                _Variables.TrimExcess();
                Push();
            }

            /// <summary> Pushes a new variable scope. </summary>
            public static void Push() => _Variables.Push(new());

            /// <summary> Pops the current variable scope. </summary>
            public static void Pop() => _Variables.Pop();

            static Variables() => Push();

        }
    }
}
