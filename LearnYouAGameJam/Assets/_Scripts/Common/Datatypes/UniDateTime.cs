using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace LYGJ.Common.Datatypes {
    [Serializable]
    public sealed class UniDateTime : ISerializationCallbackReceiver, IEquatable<UniDateTime>, IEquatable<DateTime>, IComparable<UniDateTime>, IComparable<DateTime>, IComparable, IFormattable { // Note: Timezones are not a consideration here.

        /// <summary> The underlying <see cref="DateTime" />. </summary>
        public DateTime DateTime { get; private set; }

        public UniDateTime( DateTime DateTime ) => this.DateTime = DateTime;
        public UniDateTime( long Ticks ) => DateTime = new(Ticks);

        public UniDateTime( int Year, int Month, int Day ) => DateTime = new(Year, Month, Day);

        public UniDateTime( int Year, int Month, int Day, int Hour, int Minute, int Second ) => DateTime = new(Year, Month, Day, Hour, Minute, Second);

        public UniDateTime( int Year, int Month, int Day, int Hour, int Minute, int Second, int Millisecond ) => DateTime = new(Year, Month, Day, Hour, Minute, Second, Millisecond);

        #region Constants

        /// <inheritdoc cref="DateTime.MinValue" />
        public static UniDateTime MinValue => new(DateTime.MinValue);

        /// <inheritdoc cref="DateTime.MaxValue" />
        public static UniDateTime MaxValue => new(DateTime.MaxValue);

        /// <inheritdoc cref="DateTime.Now" />
        public static UniDateTime Now => new(DateTime.Now);

        /// <inheritdoc cref="DateTime.Today" />
        public static UniDateTime Today => new(DateTime.Today);

        /// <inheritdoc cref="DateTime.UnixEpoch" />
        public static UniDateTime UnixEpoch => new(DateTime.UnixEpoch);

        #endregion

        #region Properties

        /// <inheritdoc cref="DateTime.Date" />
        public DateTime Date => DateTime.Date;

        /// <inheritdoc cref="DateTime.Day" />
        public int Day => DateTime.Day;

        /// <inheritdoc cref="DateTime.DayOfWeek" />
        public DayOfWeek DayOfWeek => DateTime.DayOfWeek;

        /// <inheritdoc cref="DateTime.DayOfYear" />
        public int DayOfYear => DateTime.DayOfYear;

        /// <inheritdoc cref="DateTime.Hour" />
        public int Hour => DateTime.Hour;

        /// <inheritdoc cref="DateTime.Millisecond" />
        public int Millisecond => DateTime.Millisecond;

        /// <inheritdoc cref="DateTime.Minute" />
        public int Minute => DateTime.Minute;

        /// <inheritdoc cref="DateTime.Month" />
        public int Month => DateTime.Month;

        /// <inheritdoc cref="DateTime.Second" />
        public int Second => DateTime.Second;

        /// <inheritdoc cref="DateTime.Ticks" />
        public long Ticks => DateTime.Ticks;

        /// <inheritdoc cref="DateTime.TimeOfDay" />
        public TimeSpan TimeOfDay => DateTime.TimeOfDay;

        /// <inheritdoc cref="DateTime.Year" />
        public int Year => DateTime.Year;

        #endregion

        #region Methods

        /// <inheritdoc cref="DateTime.Add(TimeSpan)" />
        public UniDateTime Add( TimeSpan Time ) => new(DateTime.Add(Time));

        /// <inheritdoc cref="DateTime.Add(TimeSpan)" />
        public UniDateTime Add( UniTimeSpan Time ) => new(DateTime.Add(Time));

        /// <inheritdoc cref="DateTime.AddTicks(long)" />
        public UniDateTime AddTicks( long Ticks ) => new(DateTime.AddTicks(Ticks));

        /// <inheritdoc cref="DateTime.AddMilliseconds(double)" />
        public UniDateTime AddMilliseconds( double Milliseconds ) => new(DateTime.AddMilliseconds(Milliseconds));

        /// <inheritdoc cref="DateTime.AddSeconds(double)" />
        public UniDateTime AddSeconds( double Seconds ) => new(DateTime.AddSeconds(Seconds));

        /// <inheritdoc cref="DateTime.AddMinutes(double)" />
        public UniDateTime AddMinutes( double Minutes ) => new(DateTime.AddMinutes(Minutes));

        /// <inheritdoc cref="DateTime.AddHours(double)" />
        public UniDateTime AddHours( double Hours ) => new(DateTime.AddHours(Hours));

        /// <inheritdoc cref="DateTime.AddDays(double)" />
        public UniDateTime AddDays( double Days ) => new(DateTime.AddDays(Days));

        /// <inheritdoc cref="DateTime.AddMonths(int)" />
        public UniDateTime AddMonths( int Months ) => new(DateTime.AddMonths(Months));

        /// <inheritdoc cref="DateTime.AddYears(int)" />
        public UniDateTime AddYears( int Years ) => new(DateTime.AddYears(Years));

        /// <inheritdoc cref="DateTime.Subtract(System.DateTime)" />
        public UniTimeSpan Subtract( DateTime DateTime ) => new(this.DateTime.Subtract(DateTime));

        /// <inheritdoc cref="DateTime.Subtract(System.DateTime)" />
        public UniTimeSpan Subtract( UniDateTime DateTime ) => new(this.DateTime.Subtract(DateTime.DateTime));

        /// <inheritdoc cref="DateTime.Subtract(TimeSpan)" />
        public UniDateTime Subtract( TimeSpan Time ) => new(DateTime.Subtract(Time));

        /// <inheritdoc cref="DateTime.Subtract(TimeSpan)" />
        public UniDateTime Subtract( UniTimeSpan Time ) => new(DateTime.Subtract(Time));

        #endregion
        
        #region Arithmetic Operators

        public static UniDateTime operator +( UniDateTime Left, UniDateTime Right ) => new(Left.Ticks + Right.Ticks);
        public static UniDateTime operator +( UniDateTime Left, DateTime    Right ) => new(Left.Ticks + Right.Ticks);
        public static UniDateTime operator +( DateTime    Left, UniDateTime Right ) => new(Left.Ticks + Right.Ticks);
        public static UniDateTime operator +( UniDateTime Left, long        Right ) => new(Left.Ticks + Right);

        public static UniDateTime operator -( UniDateTime Left, UniDateTime Right ) => new(Left.Ticks - Right.Ticks);
        public static UniDateTime operator -( UniDateTime Left, DateTime    Right ) => new(Left.Ticks - Right.Ticks);
        public static UniDateTime operator -( DateTime    Left, UniDateTime Right ) => new(Left.Ticks - Right.Ticks);
        public static UniDateTime operator -( UniDateTime Left, long        Right ) => new(Left.Ticks - Right);

        public static UniDateTime operator *( UniDateTime Left, long        Right ) => new(Left.Ticks  * Right);
        public static UniDateTime operator *( long        Left, UniDateTime Right ) => new(Right.Ticks * Left);
        public static UniDateTime operator *( UniDateTime Left, double      Right ) => new((long)(Left.Ticks  * Right));
        public static UniDateTime operator *( double      Left, UniDateTime Right ) => new((long)(Right.Ticks * Left));

        public static UniDateTime operator /( UniDateTime Left, long   Right ) => new(Left.Ticks / Right);
        public static UniDateTime operator /( UniDateTime Left, double Right ) => new((long)(Left.Ticks / Right));
        
        #endregion

        #region Implementation of ISerializationCallbackReceiver

        [SerializeField] long _Ticks;

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize() => _Ticks = DateTime.Ticks;

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize() => DateTime = new(_Ticks);

        #endregion

        public static implicit operator DateTime( UniDateTime UniDateTime ) => UniDateTime.DateTime;
        public static implicit operator UniDateTime( DateTime DateTime )    => new(DateTime);
        public static implicit operator UniDateTime( long     Ticks )       => new(Ticks);
        public static implicit operator long( UniDateTime     UniDateTime ) => UniDateTime.Ticks;

        #region Overrides of Object

        /// <inheritdoc />
        public override string ToString() => DateTime.ToString();

        /// <inheritdoc />
        public string ToString( string? Format, IFormatProvider? FormatProvider ) => DateTime.ToString(Format, FormatProvider);

        #endregion

        #region Equality Members

        /// <inheritdoc />
        public bool Equals( UniDateTime? Other ) => !ReferenceEquals(null, Other) && (ReferenceEquals(this, Other) || DateTime.Equals(Other.DateTime));

        /// <inheritdoc />
        public bool Equals( DateTime Other ) => DateTime.Equals(Other);

        /// <inheritdoc />
        public override bool Equals( object? Obj ) => ReferenceEquals(this, Obj)
            || Obj switch {
                null              => false,
                UniDateTime Other => Equals(Other),
                DateTime Other    => DateTime.Equals(Other),
                _                 => false
            };

        /// <inheritdoc />
        public override int GetHashCode() => DateTime.GetHashCode();

        public static bool operator ==( UniDateTime? Left, UniDateTime? Right ) => Equals(Left, Right);
        public static bool operator !=( UniDateTime? Left, UniDateTime? Right ) => !Equals(Left, Right);

        public static bool operator ==( UniDateTime? Left, DateTime     Right ) => !(Left  == null) && Left.Equals(Right);
        public static bool operator !=( UniDateTime? Left, DateTime     Right ) => !(Left  == null) && !Left.Equals(Right);
        public static bool operator ==( DateTime     Left, UniDateTime? Right ) => !(Right == null) && Right.Equals(Left);
        public static bool operator !=( DateTime     Left, UniDateTime? Right ) => !(Right == null) && !Right.Equals(Left);

        #endregion

        #region Relational Members

        /// <inheritdoc />
        public int CompareTo( UniDateTime? Other ) => ReferenceEquals(this, Other) ? 0 : ReferenceEquals(null, Other) ? 1 : DateTime.CompareTo(Other.DateTime);
        
        /// <inheritdoc />
        public int CompareTo( DateTime Other ) => DateTime.CompareTo(Other);

        /// <inheritdoc />
        public int CompareTo( object? Obj ) =>
            ReferenceEquals(this, Obj)
                ? 0
                : Obj switch {
                    UniDateTime UniDateTime => CompareTo(UniDateTime),
                    DateTime DateTime       => CompareTo(DateTime),
                    _                       => throw new ArgumentException($"Object must be of type {nameof(UniDateTime)} or {nameof(DateTime)}")
                };

        public static bool operator <( UniDateTime?  Left, UniDateTime? Right ) => !(Left == null) && Left.CompareTo(Right) < 0;
        public static bool operator >( UniDateTime?  Left, UniDateTime? Right ) => !(Left == null) && Left.CompareTo(Right) > 0;
        public static bool operator <=( UniDateTime? Left, UniDateTime? Right ) => !(Left == null) && Left.CompareTo(Right) <= 0;
        public static bool operator >=( UniDateTime? Left, UniDateTime? Right ) => !(Left == null) && Left.CompareTo(Right) >= 0;

        public static bool operator <( UniDateTime?  Left, DateTime Right ) => !(Left == null) && Left.CompareTo(Right) < 0;
        public static bool operator >( UniDateTime?  Left, DateTime Right ) => !(Left == null) && Left.CompareTo(Right) > 0;
        public static bool operator <=( UniDateTime? Left, DateTime Right ) => !(Left == null) && Left.CompareTo(Right) <= 0;
        public static bool operator >=( UniDateTime? Left, DateTime Right ) => !(Left == null) && Left.CompareTo(Right) >= 0;

        public static bool operator <( DateTime  Left, UniDateTime? Right ) => !(Right == null) && Right.CompareTo(Left) > 0;
        public static bool operator >( DateTime  Left, UniDateTime? Right ) => !(Right == null) && Right.CompareTo(Left) < 0;
        public static bool operator <=( DateTime Left, UniDateTime? Right ) => !(Right == null) && Right.CompareTo(Left) >= 0;
        public static bool operator >=( DateTime Left, UniDateTime? Right ) => !(Right == null) && Right.CompareTo(Left) <= 0;

        #endregion

    }
}
