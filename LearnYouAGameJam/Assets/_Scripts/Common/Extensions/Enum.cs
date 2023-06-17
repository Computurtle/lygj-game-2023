using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using SysEnum = System.Enum;

namespace LYGJ.Common {
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public static class Enum<TEnum> where TEnum : struct, SysEnum {
        /// <summary> Gets the values of the enum. </summary>
        public static IEnumerable<TEnum> Values => _Values.Keys;

        /// <summary> Gets the names of the enum. </summary>
        public static IReadOnlyCollection<string> Names => _Values.Values.Select(E => E.Name).ToArray();

        /// <summary> Gets the number of values in the enum. </summary>
        public static readonly int Count;

        /// <summary> Gets whether the given value is defined in the enum. </summary>
        /// <param name="Value"> The value to check. </param>
        /// <returns> Whether the given value is defined in the enum. </returns>
        public static bool IsDefined( TEnum Value ) => SysEnum.IsDefined(typeof(TEnum), Value);

        static readonly Dictionary<TEnum, EnumDef> _Values;

        /// <summary> Gets the underlying type of the enum. </summary>
        public static readonly Type UnderlyingType;

        static Enum() {
            if (!typeof(TEnum).IsEnum) {
                throw new ArgumentException($"Type '{typeof(TEnum).FullName}' is not an enum.");
            }

            Array Values = SysEnum.GetValues(typeof(TEnum));
            Count = Values.Length;

            _Values        = new(Count);
            UnderlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            foreach (TEnum Value in Values) {
                EnumDef Def = new(Value, UnderlyingType);
                _Values.Add(Value, Def);
            }

            IsFlags = typeof(TEnum).GetCustomAttribute<FlagsAttribute>() != null;
        }

        static readonly StringBuilder _SB = new();

        /// <summary> Gets the name of the given value. </summary>
        /// <param name="Value"> The value to get the name of. </param>
        /// <returns> The name of the given value. </returns>
        public static string Name( TEnum Value ) {
            if (_Values.TryGetValue(Value, out EnumDef Def)) {
                return Def.Name;
            }

            _SB.Clear();
            AppendName(_SB, Value);
            return _SB.ToString();
        }

        /// <summary> Gets the underlying value of the given value. </summary>
        /// <param name="Value"> The value to get the underlying value of. </param>
        /// <returns> The underlying value of the given value. </returns>
        public static IComparable Underlying( TEnum Value ) => _Values[Value].Underlying;

        /// <summary> Gets the field info of the given value. </summary>
        /// <param name="Value"> The value to get the field info of. </param>
        /// <returns> The field info of the given value. </returns>
        public static FieldInfo Field( TEnum Value ) => _Values[Value].Field;

        /// <summary> Gets the attribute of the given value. </summary>
        /// <typeparam name="TAttribute"> The type of the attribute to get. </typeparam>
        /// <param name="Value"> The value to get the attribute of. </param>
        /// <exception cref="InvalidOperationException"> Thrown if the given value does not have the given attribute. </exception>
        public static TAttribute Attribute<TAttribute>( TEnum Value ) where TAttribute : Attribute => _Values[Value].Field.GetCustomAttribute<TAttribute>() ?? throw new InvalidOperationException($"Enum value '{Value}' does not have attribute '{typeof(TAttribute).FullName}'.");

        /// <summary> Attempts to get the attribute of the given value. </summary>
        /// <typeparam name="TAttribute"> The type of the attribute to get. </typeparam>
        /// <param name="Value"> The value to get the attribute of. </param>
        /// <param name="Attribute"> The attribute of the given value. </param>
        /// <returns> <see langword="true"/> if the given value has the given attribute; otherwise, <see langword="false"/>. </returns>
        public static bool TryAttribute<TAttribute>( TEnum Value, [NotNullWhen(true)] out TAttribute? Attribute ) where TAttribute : Attribute {
            Attribute = _Values[Value].Field.GetCustomAttribute<TAttribute>();
            return Attribute != null;
        }

        /// <summary> Gets whether the enum is a flag enum. </summary>
        public static readonly bool IsFlags;

        sealed class EnumDef {
            public readonly string      Name;
            public readonly IComparable Underlying;

            public readonly FieldInfo Field;

            public EnumDef( TEnum Value, Type UnderlyingType ) {
                Name       = SysEnum.GetName(typeof(TEnum), Value)!;
                Underlying = (IComparable)Convert.ChangeType(Value, UnderlyingType);
                Field      = typeof(TEnum).GetField(Name);
            }
        }

        /// <summary> Appends the name of the given value to the given string builder. </summary>
        /// <param name="Builder"> The string builder to append to. </param>
        /// <param name="Value"> The value to append the name of. </param>
        /// <returns> The given string builder. </returns>
        public static StringBuilder AppendName( StringBuilder Builder, TEnum Value ) {
            if (!IsFlags) {
                if (!IsDefined(Value)) {
                    throw new ArgumentException($"Value '{Value}' is not defined in enum '{typeof(TEnum).FullName}'.");
                }
                Builder.Append(_Values[Value].Name);
            } else {
                bool First = true;
                foreach (TEnum Flag in Values) {
                    if (Flag.Equals(default)) {
                        continue;
                    }

                    if (Value.HasFlag(Flag)) {
                        if (!First) {
                            Builder.Append(", ");
                        }

                        Builder.Append(_Values[Flag].Name);
                        First = false;
                    }
                }
            }

            return Builder;
        }

        /// <summary> Attempts to parse the given value. </summary>
        /// <param name="Value"> The value to parse. </param>
        /// <param name="Result"> The parsed value. </param>
        /// <returns> <see langword="true"/> if the given value was parsed; otherwise, <see langword="false"/>. </returns>
        public static bool TryParse( string Value, out TEnum Result ) {
            if (Enum.TryParse(Value, out Result)) {
                return true;
            }

            foreach (TEnum EnumValue in Values) {
                if (Name(EnumValue).Equals(Value, StringComparison.OrdinalIgnoreCase)) {
                    Result = EnumValue;
                    return true;
                }
            }

            return false;
        }

        /// <summary> Parses the given value. </summary>
        /// <param name="Value"> The value to parse. </param>
        /// <returns> The parsed value. </returns>
        /// <exception cref="ArgumentException"> Thrown if the given value could not be parsed. </exception>
        public static TEnum Parse( string Value ) {
            if (!TryParse(Value, out TEnum Result)) {
                throw new ArgumentException($"Could not parse '{Value}' as enum '{typeof(TEnum).FullName}'.");
            }
            return Result;
        }
    }
}
