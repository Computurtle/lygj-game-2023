using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using LYGJ.Common;
using Random = UnityEngine.Random;

namespace LYGJ.EntitySystem.NPCSystem {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    public enum Motion {
        [Motion(R: true)]             Idle,   // Idle | Idle02 | Idle03 | Idle05 | IdleB | IdleC
        [Motion(1, Idle, 10)]         _Idle1, // Idle
        [Motion(32, Idle, 10)]        _Idle2, // Idle02
        [Motion(33, Idle, 10)]        _Idle3, // Idle03
        [Motion(34, Idle, 350)]       _Idle4, // Idle05
        [Motion(35, Idle, 10)]        _Idle5, // IdleB
        [Motion(36, Idle, 10)]        _Idle6, // IdleC
        [Motion(2)]                   Alert,
        [Motion(3)]                   Victory,
        [Motion(4)]                   Block,
        [Motion(5)]                   Damage,
        [Motion(R: true)]             Die,   // Die1 | Die2
        [Motion(6)]                   _Die1, // Die1
        [Motion(7)]                   _Die2, // Die2
        [Motion(8)]                   Dash,
        [Motion(9)]                   Angry,
        [Motion(10)]                  Cute, // Cute1
        [Motion(11)]                  Yes,
        [Motion(12)]                  No,
        [Motion(13)]                  Hi,
        [Motion(14)]                  Tired,
        [Motion(15)]                  Move,
        [Motion(17)]                  MoveLeft,  // MoveL
        [Motion(16)]                  MoveRight, // MoveR
        [Motion(18)]                  Run,
        [Motion(19)]                  RunLeft, // RunL
        [Motion(20)]                  RunRight,
        [Motion(21)]                  Walk,
        [Motion(22)]                  WalkLeft,  // WalkL
        [Motion(23)]                  WalkRight, // WalkR
        [Motion(24)]                  Sit,
        [Motion(25)]                  Sleep,
        [Motion(26)]                  Swoon,
        [Motion(27)]                  Worship,
        [Motion(R: true)]             Dig,
        [Motion(28, Dig, 10)]         _Dig1, // DigA
        [Motion(29, Dig, 10)]         _Dig2, // DigB
        [Motion(30, Dig, 10)]         _Dig3, // DigC
        [Motion(31)]                  Bow,
        [Motion(R: true)]             PowerAttack,   // ATK1 | ATK2 | ATK3
        [Motion(37, PowerAttack, 10)] _PowerAttack1, // ATK1
        [Motion(38, PowerAttack, 10)] _PowerAttack2, // ATK2
        [Motion(39, PowerAttack, 10)] _PowerAttack3, // ATK3
        [Motion(40)]                  NormalAttack,  // NormalATK
        [Motion(41)]                  Shoot,
        [Motion(R: true)]             Make,   // MakeA | MakeB
        [Motion(42)]                  _Make1, // MakeA
        [Motion(43)]                  _Make2, // MakeB
        [Motion(49)]                  Swimming,
        [Motion(50)]                  Jump,
        [Motion(44, Q: true)]         Q_Idle,
        [Motion(45, Q: true)]         Q_Eating,
        [Motion(46, Q: true)]         Q_Chilling, // Q_Chiling
        [Motion(48, Q: true)]         Q_Walk,
        [Motion(47, Q: true)]         Q_Run
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MotionAttribute : Attribute {
        /// <summary> Describes extended behaviour of a motion. </summary>
        /// <param name="Index"> The index of the animation this motion plays. </param>
        /// <param name="Root"> The root motion of the motion variant. </param>
        /// <param name="Weight"> The weight of the motion variant. </param>
        /// <param name="Cat"> Whether this motion is intended for cat pose, or standard. </param>
        MotionAttribute( int Index, Motion? Root = default, int Weight = 0, bool Cat = false ) {
            this.Index  = Index;
            this.Root   = Root;
            this.Weight = Weight;
            this.Cat    = Cat;
        }

        /// <inheritdoc cref="MotionAttribute(int,Motion?,int,bool)"/>
        public MotionAttribute( int Index, Motion R, int Weight, bool Q = false ) : this(Index, R, Weight, Cat: Q) {
            if (Index < 0) {
                throw new ArgumentException("Non-root motions must specify an index.");
            }
        }

        /// <inheritdoc cref="MotionAttribute(int,Motion?,int,bool)"/>
        public MotionAttribute( int Index, bool Q = false ) : this(Index, Root: default, 0, Q) {
            if (Index < 0) {
                throw new ArgumentException("Non-root motions must specify an index.");
            }
        }

        /// <inheritdoc cref="MotionAttribute(int,Motion?,int,bool)"/>
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local // Parameter is not used, correct, beyond sanity checks. This is still included to force the user to specify R=true for root motions, or give an index if not a root motion.
        public MotionAttribute( bool R ) : this(-1, null, 0, false) {
            if (!R) {
                throw new ArgumentException("Only root motions may omit an index.");
            }
        }

        /// <summary> The index of the animation this motion plays. </summary>
        public int Index { get; }

        /// <summary> The root motion of the motion variant. </summary>
        public Motion? Root { get; }

        /// <summary> The weight of the motion variant. </summary>
        public int Weight { get; }

        /// <summary> Whether this motion is intended for cat pose, or standard. </summary>
        public bool Cat { get; }
    }

    public static class Motions {
        static readonly Dictionary<Motion, ReadOnlyMemory<Motion>> _VariantLookup;

        static Motions() {
            Dictionary<Motion, List<Motion>> Variants = new();
            foreach (Motion M in Enum<Motion>.Values) {
                MotionAttribute Attr = Enum<Motion>.Attribute<MotionAttribute>(M);
                if (Attr.Index == -1 || Attr.Root is not { } R) { continue; }

                if (!Variants.TryGetValue(R, out List<Motion> List)) {
                    Variants.Add(R, List = new());
                }
                List.Add(M);
            }

            _VariantLookup = new();
            foreach (KeyValuePair<Motion, List<Motion>> Pair in Variants) {
                _VariantLookup.Add(Pair.Key, Pair.Value.ToArray());
            }
        }

        /// <summary> Creates a random grab-bag. </summary>
        /// <typeparam name="T"> The type of values in the span. </typeparam>
        /// <param name="Span"> The span to grab from. </param>
        /// <param name="Bag"> The bag to grab into. </param>
        /// <param name="Deplete"> The depletion threshold. This states how low the bag can get before being reset (for example, 0.3 means ~30% of the bag must be depleted before it is reset). This still has the benefit of feeling more random as the same values aren't returned in a row, but also prevents the weird pseudo-random nature you get where as the bag gets more and more empty, you can predict what will be returned next. </param>
        /// <returns> A random grab-bag. </returns>
        /// <exception cref="ArgumentException"> Thrown if <paramref name="Span"/> is empty. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="Deplete"/> is not in the range [0,1]. </exception>
        public static IReadOnlyList<T> GetRandomGrabBag<T>( this ReadOnlySpan<T> Span, ref List<T> Bag, float Deplete = 0f ) {
            if (Span.Length == 0) { throw new ArgumentException("Span must not be empty.", nameof(Span)); }
            if (Deplete is < 0f or > 1f) { throw new ArgumentOutOfRangeException(nameof(Deplete), Deplete, "Deplete must be in the range [0,1]."); }

            if (Bag.Count == 0 || Bag.Count <= Span.Length * Deplete) {
                Bag.Clear();
                foreach (T Value in Span) {
                    Bag.Add(Value);
                }
                Bag.Shuffle();
            }

            return Bag;
        }

        /// <summary> Shuffles the given span in-place. </summary>
        /// <param name="Span"> The span to shuffle. </param>
        /// <typeparam name="T"> The type of the span. </typeparam>
        public static void Shuffle<T>( this IList<T> Span ) {
            // Fisher-Yates shuffle. Re-implemented with Unity's Random class to avoid the need for a seeded RNG.
            for (int I = Span.Count - 1; I > 0; I--) {
                int J = Random.Range(0, I + 1);
                (Span[I], Span[J]) = (Span[J], Span[I]);
            }
        }

        /// <summary> Gets the variant of the given motion, if the motion is a root, or the motion itself if it is not. </summary>
        /// <param name="Motion"> The motion to get the variant of. </param>
        /// <param name="GrabBag"> [ref] The grab bag to use for random selection. </param>
        /// <param name="Index"> [out] The animation index of the variant. </param>
        /// <param name="Deplete"> [optional] How low the grab bag must get before being reset. </param>
        /// <returns> The variant of the given motion, if the motion is a root, or the motion itself if it is not. </returns>
        public static Motion GetVariant( this Motion Motion, ref List<Motion> GrabBag, out int Index, float Deplete = 0f ) {
            static Motion GetRandom( IReadOnlyList<Motion> All ) {
                // Uses weighting to select a random motion.
                int TotalWeight = 0;
                foreach (Motion M in All) {
                    TotalWeight += Enum<Motion>.Attribute<MotionAttribute>(M).Weight;
                }
                int Weight = Random.Range(0, TotalWeight);
                // Debug.Log($"Chosen weight: {Weight} [0..{TotalWeight})");
                foreach (Motion M in All) {
                    int MW = Enum<Motion>.Attribute<MotionAttribute>(M).Weight;
                    Weight -= MW;
                    if (Weight <= 0) {
                        // Debug.Log($"Chosen motion: {M} (weight: {MW})");
                        return M;
                    }

                    // Debug.Log($"Skipping motion: {M} (weight: {MW})");
                }
                // Debug.Log("Last motion");
                return All[^1];
            }
            Motion M =
                _VariantLookup.TryGetValue(Motion, out ReadOnlyMemory<Motion> Variants)
                    ? GetRandom(GetRandomGrabBag(Variants.Span, ref GrabBag, Deplete))
                    : Motion;
            MotionAttribute Attr = Enum<Motion>.Attribute<MotionAttribute>(M);
            Index = Attr.Index;
            return M;
        }

        /// <summary> Creates a grab bag for use with <see cref="GetVariant(Motion,ref List{Motion},out int,float)"/>. </summary>
        /// <returns> A grab bag for use with <see cref="GetVariant(Motion,ref List{Motion},out int,float)"/>. </returns>
        public static List<Motion> CreateGrabBag() => new(Enum<Motion>.Count);

        /// <inheritdoc cref="GetVariant(Motion,ref List{Motion},out int,float)"/>
        /// <returns> The index of the animation to play. </returns>
        public static int GetAnimation( this Motion Motion, ref List<Motion> GrabBag, float Deplete = 0f ) {
            _ = GetVariant(Motion, ref GrabBag, out int Index, Deplete);
            return Index;
        }
    }
}
