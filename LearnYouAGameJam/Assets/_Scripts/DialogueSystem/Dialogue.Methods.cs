using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Febucci.UI.Core;
using LYGJ.Common;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LYGJ.DialogueSystem {
    public static partial class Dialogue {

        public static class Methods {
            public delegate string HandledMethod( string[] Args );

            static readonly Dictionary<string, HandledMethod> _MethodsContainer = new(); // Name is lower invariant.
            static bool _MethodsScanned;

            [ExecuteOnReload]
            static void Cleanup() {
                _MethodsContainer.Clear();
                _MethodsContainer.TrimExcess();
                _MethodsScanned = false;
            }

            // ReSharper disable once InconsistentNaming
            static IReadOnlyDictionary<string, HandledMethod> _Methods {
                get {
                    if (!_MethodsScanned) {
                        ScanAndPopulate();
                    }
                    return _MethodsContainer;
                }
            }

            /// <summary> Invokes the given method. </summary>
            /// <param name="MethodName"> The name of the method to invoke. </param>
            /// <param name="Args"> The arguments to pass to the method. </param>
            /// <returns> The result of the method. </returns>
            public static string Invoke( string MethodName, string[] Args ) {
                if (!_Methods.TryGetValue(MethodName.ToLowerInvariant(), out HandledMethod Method)) {
                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{MethodName}' does not exist.");
                    return string.Empty;
                }

                return Method(Args);
            }

            /// <summary> Registers a method. </summary>
            /// <param name="MethodName"> The name of the method to register. </param>
            /// <param name="Method"> The method to register. </param>
            public static void Register( string MethodName, HandledMethod Method ) {
                string Name = MethodName.ToLowerInvariant();
                if (!_MethodsContainer.TryAdd(Name, Method)) {
                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{MethodName}' is already registered.");
                    return;
                }

                // Add it to the GlobalData for the text animator.
                TAnimGlobalDataScriptable GlobalData = TAnimGlobalDataScriptable.Instance;
                if (GlobalData.CustomActions.Contains(Name, StringComparison.OrdinalIgnoreCase)) {
                    // Debug.Log($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{MethodName}' is already registered in the text animator. Skipping.");
                    return;
                }
                Debug.Log($"[{nameof(Dialogue)}/{nameof(Methods)}] Registered method '{MethodName}' to the text animator global data.");

                List<string> CustomActions = GlobalData.CustomActions.ToList();
                CustomActions.Add(Name);
                #if UNITY_EDITOR
                Undo.RecordObject(GlobalData, $"Register {nameof(Method)}");
                #endif
                GlobalData.CustomActions = CustomActions.ToArray();
            }

            #if UNITY_EDITOR
            [MenuItem("LYGJ/Dialogue/Refresh Methods")]
            static void Refresh() {
                Cleanup();
                ScanAndPopulate();
            }
            #endif

            delegate bool TryParse<T>( string Input, out T Result ) where T : struct;

            static bool TryCastAsPrimitive( Type Type, string Input, [NotNullWhen(true)] out object? Result ) {
                Result = null;
                if (Type == typeof(string)) {
                    Result = Input;
                    return true;
                }

                bool Parse<T>( TryParse<T> TryParse, out object? Result, T Fallback = default ) where T : struct {
                    if (TryParse(Input, out T Parsed)) {
                        Result = Parsed;
                        return true;
                    }

                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Could not parse '{Input}' to type '{typeof(T).GetNiceName()}'.");
                    Result = Fallback;
                    return true;
                }

                if (Type == typeof(int)) {
                    return Parse<int>(int.TryParse, out Result);
                }
                if (Type == typeof(float)) {
                    return Parse<float>(float.TryParse, out Result);
                }
                if (Type == typeof(double)) {
                    return Parse<double>(double.TryParse, out Result);
                }
                if (Type == typeof(bool)) {
                    return Parse<bool>(bool.TryParse, out Result);
                }
                if (Type == typeof(char)) {
                    return Parse<char>(char.TryParse, out Result);
                }
                if (Type == typeof(byte)) {
                    return Parse<byte>(byte.TryParse, out Result);
                }
                if (Type == typeof(sbyte)) {
                    return Parse<sbyte>(sbyte.TryParse, out Result);
                }
                if (Type == typeof(short)) {
                    return Parse<short>(short.TryParse, out Result);
                }
                if (Type == typeof(ushort)) {
                    return Parse<ushort>(ushort.TryParse, out Result);
                }
                if (Type == typeof(uint)) {
                    return Parse<uint>(uint.TryParse, out Result);
                }
                if (Type == typeof(long)) {
                    return Parse<long>(long.TryParse, out Result);
                }
                if (Type == typeof(ulong)) {
                    return Parse<ulong>(ulong.TryParse, out Result);
                }
                if (Type == typeof(decimal)) {
                    return Parse<decimal>(decimal.TryParse, out Result);
                }
                if (Type == typeof(DateTime)) {
                    return Parse<DateTime>(DateTime.TryParse, out Result);
                }
                if (Type == typeof(TimeSpan)) {
                    return Parse<TimeSpan>(TimeSpan.TryParse, out Result);
                }
                if (Type == typeof(Guid)) {
                    return Parse<Guid>(Guid.TryParse, out Result);
                }

                throw new NotImplementedException($"[{nameof(Dialogue)}/{nameof(Methods)}] Type '{Type.GetNiceName()}' is not supported.");
            }

            static bool TryParseArgs( string[] Args, Type[] Types, [NotNullWhen(true)] out object[]? Parsed ) {
                int Ln = Args.Length;
                if (Ln != Types.Length) {
                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Argument length mismatch. Expected {Types.Length}, got {Ln}.");
                    Parsed = null;
                    return false;
                }

                Parsed = new object[Ln];
                for (int I = 0; I < Ln; I++) {
                    if (!TryCastAsPrimitive(Types[I], Args[I], out object? Result)) {
                        return false;
                    }

                    Parsed[I] = Result;
                }

                return true;
            }

            static bool TryParseArgs( string[] Args, ParameterInfo[] Params, [NotNullWhen(true)] out object[]? Parsed, int Skip = 0 ) {
                int ParamsLn = Params.Length;
                Type[] Types = new Type[ParamsLn - Skip];
                for (int I = Skip; I < ParamsLn; I++) {
                    Types[I - Skip] = Params[I].ParameterType;
                }

                return TryParseArgs(Args, Types, out Parsed);
            }

            /// <inheritdoc cref="Register(string, HandledMethod)"/>
            public static void Register( MethodInfo Method, Object? Target, string? Name = null ) {
                // Method must be:
                //  1. Static, or Instance with a non-null target.
                //  2. Have a return type of void, or string
                //  3. Have the remaining parameters: Be empty ; or only a string[] ; or multiple primitive-only parameters.

                // Method Styles:
                //  0 = void(string[])
                //  1 = string(string[])
                //  2 = void(primitives)
                //  3 = string(primitives)
                //  4 = void()
                //  5 = string()

                Name ??= Method.Name.ToLowerInvariant();
                if (_MethodsContainer.ContainsKey(Name)) {
                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{Name}' is already registered.");
                    return;
                }

                if (!Method.IsStatic && Target == null) {
                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{Name}' is not static, but no target was provided.");
                    return;
                }

                ParameterInfo[] Params   = Method.GetParameters();
                int             ParamsLn = Params.Length;
                if (Method.ReturnType == typeof(void)) {
                    // Style 0, 2, 4 (void)
                    switch (ParamsLn) {
                        case 0: // Style 4 (no parameters, no return)
                            string Style4Method( string[] Args ) {
                                Method.Invoke(Target, null);
                                return string.Empty;
                            }
                            Register(Name, Style4Method);
                            break;
                        case 1 when Params[0].ParameterType == typeof(string[]):
                            // Style 2 (string[], no return)
                            string Style2Method( string[] Args ) {
                                Method.Invoke(Target, new object[] { Args });
                                return string.Empty;
                            }
                            Register(Name, Style2Method);
                            break;
                        default:
                            // Style 0 (primitives, no return)
                            string Style0Method( string[] Args ) {
                                if (TryParseArgs(Args, Params, out object[]? Parsed, Skip: 0)) {
                                    Method.Invoke(Target, Parsed);
                                } else {
                                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Failed to parse arguments for method '{Name}'. Either the argument count is incorrect, or the arguments are of an unsupported (non-primitive) type.");
                                }
                                return string.Empty;
                            }
                            Register(Name, Style0Method);
                            break;
                    }
                } else if (Method.ReturnType == typeof(string)) {
                    // Style 1, 3, 5 (string)
                    switch (ParamsLn) {
                        case 0: // Style 5 (no parameters, string return)
                            string Style5Method( string[] Args ) => (string)Method.Invoke(Target, null);
                            Register(Name, Style5Method);
                            break;
                        case 1 when Params[0].ParameterType == typeof(string[]):
                            // Style 3 (string[], string return)
                            string Style3Method( string[] Args ) => (string)Method.Invoke(Target, new object[] { Args });
                            Register(Name, Style3Method);
                            break;
                        default:
                            // Style 1 (primitives, string return)
                            string Style1Method( string[] Args ) {
                                if (TryParseArgs(Args, Params, out object[]? Parsed, Skip: 0)) {
                                    return (string)Method.Invoke(Target, Parsed);
                                }
                                Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Failed to parse arguments for method '{Name}'. Either the argument count is incorrect, or the arguments are of an unsupported (non-primitive) type.");
                                return string.Empty;
                            }
                            Register(Name, Style1Method);
                            break;
                    }
                } else {
                    Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{Name}' has an unsupported return type '{Method.ReturnType}'.");
                }
            }

            static bool InheritsFromGenericType( Type? Type, Type GenericType, int Depth = 0 ) {
                const int MaxRecursionDepth = 100;
                if (Depth > MaxRecursionDepth) { // In standard .NET class hierarchies, it's not possible for a loop to occur in the inheritance tree. Each class can only inherit from one parent class, and eventually every class traces back to System.Object, after which there are no further parents and the loop ends. Regardless, this check is here to prevent infinite recursion in the event that a loop does occur such as with dynamically-generated third-party types.
                    Debug.LogWarning($"Reached maximum recursion depth when checking if {Type.GetNiceName()} inherits from {GenericType.GetNiceName()}");
                    return false;
                }

                while (Type != null && Type != typeof(object)) {
                    if (Type.IsGenericType && Type.GetGenericTypeDefinition() == GenericType) {
                        return true;
                    }

                    Type = Type.BaseType;
                }
                return false;
            }

            static bool InheritsFromSingletonMB( Type Type ) => InheritsFromGenericType(Type, typeof(SingletonMB<>));

            static void ScanAndPopulate() {
                if (_MethodsScanned) { return; }
                _MethodsScanned = true;

                foreach (MethodInfo Method in RuntimeTypeCache.GetMethodsWithAttribute<DialogueFunctionAttribute>()) {
                    DialogueFunctionAttribute? Attribute = Method.GetCustomAttribute<DialogueFunctionAttribute>();
                    if (Attribute == null) {
                        Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{Method.Name}' has the '{nameof(DialogueFunctionAttribute)}' attribute, but the attribute is null.");
                        continue;
                    }

                    // If method is static, continue immediately.
                    // If method is instanced, we only allow target resolution if the method's declaring type inherits from SingletonMB<T> (so we can call .Instance to retrieve the target)

                    // Register(Method, null, string.IsNullOrEmpty(Attribute.Name) ? null : Attribute.Name);

                    switch (Method.IsStatic) {
                        case true:
                            // Debug.Log($"[{nameof(Dialogue)}/{nameof(Methods)}] Registering static method '{Method.Name}' with name '{Attribute.Name}'.");
                            Register(Method, null, string.IsNullOrEmpty(Attribute.Name) ? null : Attribute.Name);
                            break;
                        case false when Method.DeclaringType == null:
                            Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{Method.Name}' has a null declaring type.");
                            break;
                        default:
                            Type Tp = Method.DeclaringType;
                            if (!InheritsFromSingletonMB(Tp)) {
                                Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Method '{Method.Name}' has a declaring type '{Tp.GetNiceName()}' that does not inherit from '{typeof(SingletonMB<>).GetNiceName()}'. (actual type: {Tp.BaseType?.GetNiceName()})");
                                break;
                            }

                            Type GenTp;
                            try {
                                GenTp = typeof(SingletonMB<>).MakeGenericType(Tp); // SingletonMB<Tp>
                            } catch (ArgumentException) {
                                Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Failed to create generic type '{typeof(SingletonMB<>).GetNiceName()}' with type '{Tp.GetNiceName()}'.");
                                break;
                            }

                            if (!GenTp.IsAssignableFrom(Tp)) {
                                Debug.LogError($"Type {Tp.GetNiceName()} does not inherit from {GenTp.GetNiceName()}. For non-static methods, the declaring type must inherit from {GenTp.GetNiceName()}.");
                                break;
                            }

                            #pragma warning disable CS0618
                            if (!SingletonMB.TryGetSingleton(Tp, out MonoBehaviour? Instance)) {
                                #pragma warning restore CS0618
                                Debug.LogError($"[{nameof(Dialogue)}/{nameof(Methods)}] Failed to retrieve singleton instance of type '{Tp.GetNiceName()}'.");
                                break;
                            }

                            // Debug.Log($"[{nameof(Dialogue)}/{nameof(Methods)}] Registering instanced method '{Method.Name}' with name '{Attribute.Name}'.");
                            Register(Method, Instance, string.IsNullOrEmpty(Attribute.Name) ? null : Attribute.Name);
                            break;
                    }
                }
            }

            [DialogueFunction]
            static string Jump( string Label ) => Label;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DialogueFunctionAttribute : Attribute {
        /// <summary> The name of the function. </summary>
        public readonly string Name;

        /// <summary> Allows a function to be invoked by the Dialogue system. </summary>
        /// <param name="Name"> The name of the function. </param>
        public DialogueFunctionAttribute( string Name ) => this.Name = Name;

        /// <summary> Allows a function to be invoked by the Dialogue system. </summary>
        public DialogueFunctionAttribute() : this(string.Empty) { }
    }
}
