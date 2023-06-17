using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LYGJ.Common.Enums {

    /// <summary> Represents a rotation of an item. </summary>
    public enum ItemRotation {
        /// <summary> No rotation (0°). </summary>
        None = 0,
        /// <summary> 90° rotation (clockwise) / 270° rotation (counter-clockwise). </summary>
        Clockwise = 90,
        /// <summary> 180° rotation (&quot;upside-down&quot;). </summary>
        OneEightie = 180,
        /// <summary> 270° rotation (clockwise) / 90° rotation (counter-clockwise). </summary>
        Counter = 270
    }

    public static class ItemRotationExtensions {
        /// <summary> Rotates the given rotation clockwise by 90°. </summary>
        /// <param name="Rotation"> The rotation to rotate. </param>
        /// <returns> The rotated rotation. </returns>
        public static ItemRotation RotateRight( this ItemRotation Rotation ) => Rotation switch {
            ItemRotation.None       => ItemRotation.Clockwise,
            ItemRotation.Clockwise  => ItemRotation.OneEightie,
            ItemRotation.OneEightie => ItemRotation.Counter,
            ItemRotation.Counter    => ItemRotation.None,
            _                       => throw new ArgumentOutOfRangeException(nameof(Rotation), Rotation, null)
        };

        /// <inheritdoc cref="RotateRight(ItemRotation)"/>
        /// <param name="Rotation"> The rotation to rotate. </param>
        /// <param name="Times"> The amount of times to rotate. </param>
        public static ItemRotation RotateRight( this ItemRotation Rotation, int Times ) {
            switch (Times) {
                case 0:   return Rotation;
                case < 0: return Rotation.RotateLeft(-Times);
            }

            for (int I = 0; I < Times; I++) {
                Rotation = Rotation.RotateRight();
            }
            return Rotation;
        }

        /// <summary> Rotates the given rotation counter-clockwise by 90° (clockwise by -90°). </summary>
        /// <param name="Rotation"> The rotation to rotate. </param>
        /// <returns> The rotated rotation. </returns>
        public static ItemRotation RotateLeft( this ItemRotation Rotation ) => Rotation switch {
            ItemRotation.None       => ItemRotation.Counter,
            ItemRotation.Clockwise  => ItemRotation.None,
            ItemRotation.OneEightie => ItemRotation.Clockwise,
            ItemRotation.Counter    => ItemRotation.OneEightie,
            _                       => throw new ArgumentOutOfRangeException(nameof(Rotation), Rotation, null)
        };

        /// <inheritdoc cref="RotateLeft(ItemRotation)"/>
        /// <param name="Rotation"> The rotation to rotate. </param>
        /// <param name="Times"> The amount of times to rotate. </param>
        public static ItemRotation RotateLeft( this ItemRotation Rotation, int Times ) {
            switch (Times) {
                case 0:   return Rotation;
                case < 0: return Rotation.RotateRight(-Times);
            }

            for (int I = 0; I < Times; I++) {
                Rotation = Rotation.RotateLeft();
            }
            return Rotation;
        }

        /// <summary> Rotates the given rotation by 180°. </summary>
        /// <param name="Rotation"> The rotation to rotate. </param>
        /// <returns> The rotated rotation. </returns>
        public static ItemRotation Flip( this ItemRotation Rotation ) => Rotation switch {
            ItemRotation.None       => ItemRotation.OneEightie,
            ItemRotation.Clockwise  => ItemRotation.Counter,
            ItemRotation.OneEightie => ItemRotation.None,
            ItemRotation.Counter    => ItemRotation.Clockwise,
            _                       => throw new ArgumentOutOfRangeException(nameof(Rotation), Rotation, null)
        };

        /// <summary> Rotates the given rotation by the given amount. </summary>
        /// <param name="Rotation"> The rotation to rotate. </param>
        /// <param name="Amount"> The amount to rotate by. 1 = 90° clockwise, -1 = 90° counter-clockwise, 2 = 180°, etc. </param>
        /// <returns> The rotated rotation. </returns>
        public static ItemRotation Rotate( this ItemRotation Rotation, int Amount ) =>
            Amount switch {
                0   => Rotation,
                < 0 => Rotation.RotateLeft(-Amount),
                _   => Rotation.RotateRight(Amount)
            };

        /// <summary> Gets all potential rotations. </summary>
        /// <returns> All potential rotations. </returns>
        public static IReadOnlyCollection<ItemRotation> All { get; } = new[] {
            ItemRotation.None,
            ItemRotation.Clockwise,
            ItemRotation.OneEightie,
            ItemRotation.Counter
        };
    }
}
