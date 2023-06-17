using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace LYGJ.EntitySystem.NPCSystem {
    public sealed class CoroutineCancellationSource {
        /// <summary> Returns <see langword="true"/> if the coroutine has been cancelled. </summary>
        public bool IsCancellationRequested { get; private set; } = false;

        /// <summary> Cancels the coroutine. </summary>
        public void Cancel() => IsCancellationRequested = true;

        /// <summary> Attempts to cancel the coroutine. </summary>
        /// <returns> <see langword="true"/> if the coroutine was cancelled; otherwise, <see langword="false"/>. </returns>
        public bool TryCancel() {
            if (IsCancellationRequested) { return false; }
            IsCancellationRequested = true;
            return true;
        }

        /// <summary> Gets the token. </summary>
        /// <returns> The token. </returns>
        public CoroutineCancellation Token { get; }

        /// <summary> Attaches a cancellation token to the given coroutine. </summary>
        /// <param name="Coroutine"> The coroutine to attach the cancellation token to. </param>
        /// <param name="Token"> The cancellation token to attach. </param>
        /// <returns> The coroutine with the cancellation token attached. </returns>
        public static IEnumerator WithCancellation( IEnumerator Coroutine, CoroutineCancellationSource Token ) {
            while (true) {
                if (Token.IsCancellationRequested) {
                    yield break;
                }

                if (Coroutine.MoveNext()) {
                    yield return Coroutine.Current;
                } else {
                    yield break;
                }
            }
        }

        /// <summary> Returns a cancelled cancellation token. </summary>
        /// <returns> A cancelled cancellation token. </returns>
        public static CoroutineCancellation Cancelled => (_Cancelled ??= new(true)).Token;

        static CoroutineCancellationSource? _Cancelled;

        /// <summary> Returns a non-cancelled cancellation token. </summary>
        /// <returns> A non-cancelled cancellation token. </returns>
        public static CoroutineCancellation NotCancelled => (_NotCancelled ??= new(false)).Token;

        static CoroutineCancellationSource? _NotCancelled;

        /// <summary> Returns a finished coroutine. </summary>
        /// <returns> A finished coroutine. </returns>
        public static IEnumerator Finished { get; } = new FinishedCoroutine();

        public CoroutineCancellationSource() : this(false) { }
        CoroutineCancellationSource( bool Cancelled ) {
            IsCancellationRequested = Cancelled;
            Token                   = new(this);
        }
    }

    public readonly struct CoroutineCancellation {
        /// <inheritdoc cref="CoroutineCancellationSource.IsCancellationRequested"/>
        public bool IsCancellationRequested => _Source.IsCancellationRequested;

        readonly CoroutineCancellationSource _Source;

        internal CoroutineCancellation( CoroutineCancellationSource Source ) => _Source = Source;

        public CoroutineCancellation( bool Cancelled = false ) : this((Cancelled ? CoroutineCancellationSource.Cancelled : CoroutineCancellationSource.NotCancelled)._Source) { }

        public static implicit operator CoroutineCancellation( CoroutineCancellationSource Source ) => new(Source);
    }

    internal sealed class FinishedCoroutine : IEnumerator {
        /// <inheritdoc />
        public object? Current => null;

        /// <inheritdoc />
        public bool MoveNext() => false;

        /// <inheritdoc />
        public void Reset() { }
    }

    public static class CoroutineExtensions {
        /// <inheritdoc cref="CoroutineCancellationSource.WithCancellation"/>
        public static IEnumerator WithCancellation( this IEnumerator Coroutine, CoroutineCancellationSource Token ) => CoroutineCancellationSource.WithCancellation(Coroutine, Token);
    }

    public abstract class WaitForSecondsWithCancellationBase : CustomYieldInstruction {
        float                          _DelayTime;
        readonly CoroutineCancellation _Token;

        /// <summary> Gets the delta time. </summary>
        protected abstract float DeltaTime { get; }

        /// <inheritdoc />
        public override bool keepWaiting {
            get {
                _DelayTime -= DeltaTime;
                return _DelayTime > 0 && !_Token.IsCancellationRequested;
            }
        }

        /// <inheritdoc />
        protected WaitForSecondsWithCancellationBase( float DelayTime, CoroutineCancellation Token ) {
            _DelayTime = DelayTime;
            _Token     = Token;
        }
    }

    /// <inheritdoc cref="UnityEngine.WaitForSeconds"/>
    public sealed class WaitForSecondsWithCancellation : WaitForSecondsWithCancellationBase {
        /// <inheritdoc />
        protected override float DeltaTime => Time.deltaTime;

        /// <inheritdoc />
        public WaitForSecondsWithCancellation( float DelayTime, CoroutineCancellation Token ) : base(DelayTime, Token) { }
    }

    /// <inheritdoc cref="UnityEngine.WaitForSecondsRealtime"/>
    public sealed class WaitForSecondsRealtimeWithCancellation : WaitForSecondsWithCancellationBase {
        /// <inheritdoc />
        protected override float DeltaTime => Time.unscaledDeltaTime;

        /// <inheritdoc />
        public WaitForSecondsRealtimeWithCancellation( float DelayTime, CoroutineCancellation Token ) : base(DelayTime, Token) { }
    }

    public sealed class CancellableCoroutine : IEnumerator {
        readonly CoroutineCancellationSource _Token;
        readonly IEnumerator                 _Coroutine;

        /// <inheritdoc />
        public object? Current => _Coroutine.Current;

        /// <inheritdoc />
        public bool MoveNext() => _Coroutine.MoveNext() && !_Token.IsCancellationRequested;

        /// <inheritdoc />
        public void Reset() => _Coroutine.Reset();

        /// <summary> Creates a new <see cref="CancellableCoroutine"/>. </summary>
        /// <param name="Coroutine"> The coroutine to attach the cancellation token to. </param>
        /// <param name="Token"> The cancellation token to attach. </param>
        public CancellableCoroutine( IEnumerator Coroutine, CoroutineCancellationSource Token ) {
            _Coroutine = Coroutine;
            _Token     = Token;
        }

        /// <inheritdoc cref="CancellableCoroutine(IEnumerator,CoroutineCancellationSource)"/>
        public CancellableCoroutine( Func<CoroutineCancellation, IEnumerator> Coroutine, CoroutineCancellationSource Token ) : this(Coroutine(Token), Token) { }

        /// <inheritdoc cref="CancellableCoroutine(IEnumerator,CoroutineCancellationSource)"/>
        public CancellableCoroutine( IEnumerator Coroutine ) : this(Coroutine, new()) { }

        /// <inheritdoc cref="CancellableCoroutine(IEnumerator,CoroutineCancellationSource)"/>
        public CancellableCoroutine( Func<CoroutineCancellation, IEnumerator> Coroutine ) : this(Coroutine, new()) { }

        /// <inheritdoc cref="CoroutineCancellationSource.Cancel"/>
        public void Cancel() => _Token.Cancel();

        public static implicit operator CancellableCoroutine( Func<CoroutineCancellation, IEnumerator> Coroutine ) => new(Coroutine);
    }

}
