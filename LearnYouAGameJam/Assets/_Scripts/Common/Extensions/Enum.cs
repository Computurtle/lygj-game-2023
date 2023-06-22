using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
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
                _Values.TryAdd(Value, Def);
            }
            Count = _Values.Count;
            _Values.TrimExcess(Count);

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

        /// <summary> Gets whether the two given values are equal. </summary>
        /// <param name="Value1"> The first value to compare. </param>
        /// <param name="Value2"> The second value to compare. </param>
        /// <returns> <see langword="true"/> if the two given values are equal; otherwise, <see langword="false"/>. </returns>
        public static bool Equals( TEnum Value1, TEnum Value2 ) => EqualityComparer<TEnum>.Default.Equals(Value1, Value2);

        /// <summary> Gets the index of the given value. </summary>
        /// <param name="Value"> The value to get the index of. </param>
        /// <returns> The index of the given value, or <c>-1</c> if the given value is not defined. </returns>
        public static int IndexOf( TEnum Value ) {
            if (!IsDefined(Value)) {
                return -1;
            }

            int I = 0;
            foreach (TEnum EnumValue in Values) {
                if (Equals(EnumValue, Value)) {
                    return I;
                }
                I++;
            }
            Debug.LogError($"Enum value '{Value}' is defined, but could not be found in enum '{typeof(TEnum).FullName}'.");
            return -1;
        }

        /// <summary> Gets the value at the given index. </summary>
        /// <param name="Index"> The index to get the value of. </param>
        /// <returns> The value at the given index. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if the given index is out of range. </exception>
        public static TEnum ValueAt( int Index ) {
            if (Index < 0 || Index >= Count) {
                throw new ArgumentOutOfRangeException(nameof(Index), Index, $"Index must be between 0 and {Count - 1}.");
            }
            int I = 0;
            foreach (TEnum EnumValue in Values) {
                if (I == Index) {
                    return EnumValue;
                }
                I++;
            }
            throw new ArgumentOutOfRangeException(nameof(Index), Index, $"Index must be between 0 and {Count - 1}.");
        }

        /// <summary> Gets the next value after the given value. </summary>
        /// <param name="Value"> The value to get the next value of. </param>
        /// <param name="Loop"> Whether to loop back to the first value if the given value is the last value. </param>
        /// <returns> The next value after the given value. </returns>
        public static TEnum Next( TEnum Value, bool Loop = true ) {
            int Index = IndexOf(Value);
            if (Index == -1) {
                throw new ArgumentException($"Value '{Value}' is not defined in enum '{typeof(TEnum).FullName}'.");
            }

            Index++;
            if (Index >= Count) {
                if (Loop) {
                    Index = 0;
                } else {
                    Index = Count - 1;
                }
            }

            return ValueAt(Index);
        }

        /// <summary> Gets the previous value before the given value. </summary>
        /// <param name="Value"> The value to get the previous value of. </param>
        /// <param name="Loop"> Whether to loop back to the last value if the given value is the first value. </param>
        /// <returns> The previous value before the given value. </returns>
        public static TEnum Previous( TEnum Value, bool Loop = true ) {
            int Index = IndexOf(Value);
            if (Index == -1) {
                throw new ArgumentException($"Value '{Value}' is not defined in enum '{typeof(TEnum).FullName}'.");
            }

            Index--;
            if (Index < 0) {
                if (Loop) {
                    Index = Count - 1;
                } else {
                    Index = 0;
                }
            }

            return ValueAt(Index);
        }

        /// <summary> Gets the first value in the enum. </summary>
        /// <returns> The first value in the enum. </returns>
        /// <remarks> This is not necessarily the minimum value, but the first value defined in the enum. </remarks>
        public static TEnum First => ValueAt(0);

        /// <summary> Gets the last value in the enum. </summary>
        /// <returns> The last value in the enum. </returns>
        /// <remarks> This is not necessarily the maximum value, but the last value defined in the enum. </remarks>
        public static TEnum Last => ValueAt(Count - 1);

        /// <summary> Gets the minimum value in the enum. </summary>
        /// <returns> The minimum value in the enum. </returns>
        /// <remarks> This is not necessarily the first defined value, but the minimum value defined in the enum. </remarks>
        public static TEnum Min => Values.Min();

        /// <summary> Gets the maximum value in the enum. </summary>
        /// <returns> The maximum value in the enum. </returns>
        /// <remarks> This is not necessarily the last defined value, but the maximum value defined in the enum. </remarks>
        public static TEnum Max => Values.Max();
    }

    public static class EnumExtensions {
        /// <inheritdoc cref="Enum{TEnum}.Name(TEnum)"/>
        public static string GetName<TEnum>( this TEnum Value ) where TEnum : struct, Enum => Enum<TEnum>.Name(Value);

        /// <inheritdoc cref="Enum{TEnum}.Underlying(TEnum)"/>
        public static IComparable GetUnderlyingValue<TEnum>( this TEnum Value ) where TEnum : struct, Enum => Enum<TEnum>.Underlying(Value);

        /// <inheritdoc cref="Enum{TEnum}.Field(TEnum)"/>
        public static FieldInfo GetField<TEnum>( this TEnum Value ) where TEnum : struct, Enum => Enum<TEnum>.Field(Value);

        /// <inheritdoc cref="Enum{TEnum}.Attribute{TAttribute}(TEnum)"/>
        public static TAttribute GetAttribute<TEnum, TAttribute>( this TEnum Value ) where TEnum : struct, Enum where TAttribute : Attribute => Enum<TEnum>.Attribute<TAttribute>(Value);

        /// <inheritdoc cref="Enum{TEnum}.TryAttribute{TAttribute}(TEnum, out TAttribute)"/>
        public static bool TryGetAttribute<TEnum, TAttribute>( this TEnum Value, [NotNullWhen(true)] out TAttribute? Attribute ) where TEnum : struct, Enum where TAttribute : Attribute => Enum<TEnum>.TryAttribute(Value, out Attribute);

        /// <inheritdoc cref="Enum{TEnum}.AppendName(StringBuilder, TEnum)"/>
        public static StringBuilder Append<TEnum>( this StringBuilder Builder, TEnum Value ) where TEnum : struct, Enum => Enum<TEnum>.AppendName(Builder, Value);

        /// <inheritdoc cref="Enum{TEnum}.IsDefined(TEnum)"/>
        public static bool IsDefined<TEnum>( this TEnum Value ) where TEnum : struct, Enum => Enum<TEnum>.IsDefined(Value);

        /// <inheritdoc cref="Enum{TEnum}.IndexOf(TEnum)"/>
        public static int IndexOf<TEnum>( this TEnum Value ) where TEnum : struct, Enum => Enum<TEnum>.IndexOf(Value);

        /// <inheritdoc cref="Enum{TEnum}.ValueAt(int)"/>
        public static TEnum GetEnumValueAtIndex<TEnum>( this int Index ) where TEnum : struct, Enum => Enum<TEnum>.ValueAt(Index);

        /// <inheritdoc cref="Enum{TEnum}.Next(TEnum, bool)"/>
        public static TEnum Next<TEnum>( this TEnum Value, bool Loop = true ) where TEnum : struct, Enum => Enum<TEnum>.Next(Value, Loop);

        /// <inheritdoc cref="Enum{TEnum}.Previous(TEnum, bool)"/>
        public static TEnum Previous<TEnum>( this TEnum Value, bool Loop = true ) where TEnum : struct, Enum => Enum<TEnum>.Previous(Value, Loop);
    }
}
