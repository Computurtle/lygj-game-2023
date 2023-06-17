using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace LYGJ.Common.Datatypes {
    [Serializable]
    public sealed class UniTimeSpan : ISerializationCallbackReceiver, IEquatable<UniTimeSpan>, IEquatable<TimeSpan>, IComparable<UniTimeSpan>, IComparable<TimeSpan>, IComparable, IFormattable {
        /// <summary> The underlying <see cref="TimeSpan" />. </summary>
        public TimeSpan TimeSpan { get; private set; }

        public UniTimeSpan( TimeSpan TimeSpan ) => this.TimeSpan = TimeSpan;

        public UniTimeSpan( long Ticks ) => TimeSpan = new(Ticks);

        public UniTimeSpan( int Days, int Hours, int Minutes, int Seconds ) => TimeSpan = new(Days, Hours, Minutes, Seconds);

        public UniTimeSpan( int Days, int Hours, int Minutes, int Seconds, int Milliseconds ) => TimeSpan = new(Days, Hours, Minutes, Seconds, Milliseconds);

        public UniTimeSpan( int Hours, int Minutes, int Seconds ) => TimeSpan = new(Hours, Minutes, Seconds);


        #region Constants

        /// <inheritdoc cref="TimeSpan.MinValue" />
        public static UniTimeSpan MinValue => new(TimeSpan.MinValue);

        /// <inheritdoc cref="TimeSpan.MaxValue" />
        public static UniTimeSpan MaxValue => new(TimeSpan.MaxValue);

        /// <inheritdoc cref="TimeSpan.Zero" />
        public static UniTimeSpan Zero => new(TimeSpan.Zero);

        /// <summary> The current time. </summary>
        public static UniTimeSpan Now => new(DateTime.Now.TimeOfDay);

        #endregion

        #region Properties

        /// <inheritdoc cref="TimeSpan.Days" />
        public int Days => TimeSpan.Days;

        /// <inheritdoc cref="TimeSpan.Hours" />
        public int Hours => TimeSpan.Hours;

        /// <inheritdoc cref="TimeSpan.Minutes" />
        public int Minutes => TimeSpan.Minutes;

        /// <inheritdoc cref="TimeSpan.Seconds" />
        public int Seconds => TimeSpan.Seconds;

        /// <inheritdoc cref="TimeSpan.Milliseconds" />
        public int Milliseconds => TimeSpan.Milliseconds;

        /// <inheritdoc cref="TimeSpan.Ticks" />
        public long Ticks => TimeSpan.Ticks;

        /// <inheritdoc cref="TimeSpan.TotalDays" />
        public double TotalDays => TimeSpan.TotalDays;

        /// <inheritdoc cref="TimeSpan.TotalHours" />
        public double TotalHours => TimeSpan.TotalHours;

        /// <inheritdoc cref="TimeSpan.TotalMinutes" />
        public double TotalMinutes => TimeSpan.TotalMinutes;

        /// <inheritdoc cref="TimeSpan.TotalSeconds" />
        public double TotalSeconds => TimeSpan.TotalSeconds;

        /// <inheritdoc cref="TimeSpan.TotalMilliseconds" />
        public double TotalMilliseconds => TimeSpan.TotalMilliseconds;

        #endregion

        #region Methods

        /// <inheritdoc cref="TimeSpan.Add(System.TimeSpan)" />
        public UniTimeSpan Add( TimeSpan TimeSpan ) => new(this.TimeSpan.Add(TimeSpan));

        /// <inheritdoc cref="TimeSpan.Add(System.TimeSpan)" />
        public UniTimeSpan Add( UniTimeSpan UniTimeSpan ) => new(TimeSpan.Add(UniTimeSpan.TimeSpan));

        /// <inheritdoc cref="TimeSpan.Subtract(System.TimeSpan)" />
        public UniTimeSpan Subtract( TimeSpan TimeSpan ) => new(this.TimeSpan.Subtract(TimeSpan));

        /// <inheritdoc cref="TimeSpan.Subtract(System.TimeSpan)" />
        public UniTimeSpan Subtract( UniTimeSpan UniTimeSpan ) => new(TimeSpan.Subtract(UniTimeSpan.TimeSpan));

        #endregion

        #region Arithmetic Operators

        public static UniTimeSpan operator +( UniTimeSpan Left, UniTimeSpan Right ) => new(Left.TimeSpan + Right.TimeSpan);
        public static UniTimeSpan operator +( UniTimeSpan Left, long        Right ) => new(Left.Ticks    + Right);
        public static UniTimeSpan operator +( UniTimeSpan Left, TimeSpan    Right ) => new(Left.TimeSpan + Right);
        public static UniTimeSpan operator +( TimeSpan    Left, UniTimeSpan Right ) => new(Left          + Right.TimeSpan);

        public static UniTimeSpan operator -( UniTimeSpan Left, UniTimeSpan Right ) => new(Left.TimeSpan - Right.TimeSpan);
        public static UniTimeSpan operator -( UniTimeSpan Left, long        Right ) => new(Left.Ticks    - Right);
        public static UniTimeSpan operator -( UniTimeSpan Left, TimeSpan    Right ) => new(Left.TimeSpan - Right);
        public static UniTimeSpan operator -( TimeSpan    Left, UniTimeSpan Right ) => new(Left          - Right.TimeSpan);

        public static UniTimeSpan operator *( UniTimeSpan Left, long        Right ) => new(Left.Ticks  * Right);
        public static UniTimeSpan operator *( long        Left, UniTimeSpan Right ) => new(Right.Ticks * Left);
        public static UniTimeSpan operator *( UniTimeSpan Left, double      Right ) => new((long)(Left.Ticks  * Right));
        public static UniTimeSpan operator *( double      Left, UniTimeSpan Right ) => new((long)(Right.Ticks * Left));

        public static UniTimeSpan operator /( UniTimeSpan Left, long   Right ) => new(Left.Ticks / Right);
        public static UniTimeSpan operator /( UniTimeSpan Left, double Right ) => new((long)(Left.Ticks / Right));

        #endregion

        #region Implementation of ISerializationCallbackReceiver

        [SerializeField] long _Ticks;

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize() => _Ticks = TimeSpan.Ticks;

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize() => TimeSpan = new(_Ticks);

        #endregion

        public static implicit operator TimeSpan( UniTimeSpan UniTimeSpan ) => UniTimeSpan.TimeSpan;
        public static implicit operator UniTimeSpan( TimeSpan TimeSpan ) => new(TimeSpan);
        public static implicit operator UniTimeSpan( long Ticks ) => new(Ticks);
        public static implicit operator long( UniTimeSpan UniTimeSpan ) => UniTimeSpan.Ticks;

        #region Overrides of Object

        /// <inheritdoc />
        public override string ToString() => TimeSpan.ToString();

        /// <inheritdoc />
        public string ToString( string Format, IFormatProvider FormatProvider ) => TimeSpan.ToString(Format, FormatProvider);

        #endregion

        #region Equality Members

        /// <inheritdoc />
        public bool Equals( UniTimeSpan? Other ) => !ReferenceEquals(null, Other) && (ReferenceEquals(this, Other) || TimeSpan.Equals(Other.TimeSpan));

        /// <inheritdoc />
        public bool Equals( TimeSpan Other ) => TimeSpan.Equals(Other);

        /// <inheritdoc />
        public override bool Equals( object? Obj ) =>
            ReferenceEquals(this, Obj)
            || Obj switch {
                UniTimeSpan UniTimeSpan => Equals(UniTimeSpan),
                TimeSpan TimeSpan       => Equals(TimeSpan),
                _                       => false
            };

        /// <inheritdoc />
        public override int GetHashCode() => TimeSpan.GetHashCode();

        public static bool operator ==( UniTimeSpan? Left, UniTimeSpan? Right ) => Equals(Left, Right);
        public static bool operator !=( UniTimeSpan? Left, UniTimeSpan? Right ) => !Equals(Left, Right);

        public static bool operator ==( UniTimeSpan? Left, TimeSpan Right ) => !(Left == null) && Left.Equals(Right);
        public static bool operator !=( UniTimeSpan? Left, TimeSpan Right ) => !(Left == null) && !Left.Equals(Right);
        public static bool operator ==( TimeSpan Left, UniTimeSpan? Right ) => !(Right == null) && Right.Equals(Left);
        public static bool operator !=( TimeSpan Left, UniTimeSpan? Right ) => !(Right == null) && !Right.Equals(Left);

        #endregion

        #region Relational Members

        /// <inheritdoc />
        public int CompareTo( UniTimeSpan? Other ) => ReferenceEquals(this, Other) ? 0 : ReferenceEquals(null, Other) ? 1 : TimeSpan.CompareTo(Other.TimeSpan);

        /// <inheritdoc />
        public int CompareTo( TimeSpan Other ) => TimeSpan.CompareTo(Other);

        /// <inheritdoc />
        public int CompareTo( object? Obj ) =>
            ReferenceEquals(this, Obj)
                ? 0
                : Obj switch {
                    UniTimeSpan UniTimeSpan => CompareTo(UniTimeSpan),
                    TimeSpan TimeSpan       => CompareTo(TimeSpan),
                    _                       => throw new ArgumentException($"Object must be of type {nameof(UniTimeSpan)} or {nameof(TimeSpan)}")
                };

        public static bool operator <( UniTimeSpan?  Left, UniTimeSpan? Right ) => !(Left == null) && Left.CompareTo(Right) < 0;
        public static bool operator >( UniTimeSpan?  Left, UniTimeSpan? Right ) => !(Left == null) && Left.CompareTo(Right) > 0;
        public static bool operator <=( UniTimeSpan? Left, UniTimeSpan? Right ) => !(Left == null) && Left.CompareTo(Right) <= 0;
        public static bool operator >=( UniTimeSpan? Left, UniTimeSpan? Right ) => !(Left == null) && Left.CompareTo(Right) >= 0;

        public static bool operator <( UniTimeSpan?  Left, TimeSpan Right ) => !(Left == null) && Left.CompareTo(Right) < 0;
        public static bool operator >( UniTimeSpan?  Left, TimeSpan Right ) => !(Left == null) && Left.CompareTo(Right) > 0;
        public static bool operator <=( UniTimeSpan? Left, TimeSpan Right ) => !(Left == null) && Left.CompareTo(Right) <= 0;
        public static bool operator >=( UniTimeSpan? Left, TimeSpan Right ) => !(Left == null) && Left.CompareTo(Right) >= 0;

        public static bool operator <( TimeSpan Left, UniTimeSpan? Right ) => !(Right == null) && Right.CompareTo(Left) > 0;
        public static bool operator >( TimeSpan Left, UniTimeSpan? Right ) => !(Right == null) && Right.CompareTo(Left) < 0;
        public static bool operator <=( TimeSpan Left, UniTimeSpan? Right ) => !(Right == null) && Right.CompareTo(Left) >= 0;
        public static bool operator >=( TimeSpan Left, UniTimeSpan? Right ) => !(Right == null) && Right.CompareTo(Left) <= 0;

        #endregion

    }
}
