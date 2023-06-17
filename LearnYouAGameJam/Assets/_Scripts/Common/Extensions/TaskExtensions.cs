using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.Common {
    public static class TaskExtensions {
        /// <inheritdoc cref="UniTaskExtensions.ContinueWith{T}(Cysharp.Threading.Tasks.UniTask{T},System.Action{T})"/>
        public static UniTask<T1> ContinueWith<T1>( this UniTask Task, UniTask<T1> Continuation ) {
            async UniTask<T1> Continue() {
                await Task;
                return await Continuation;
            }

            return Continue();
        }

        /// <inheritdoc cref="UniTaskExtensions.ContinueWith{T}(Cysharp.Threading.Tasks.UniTask{T},System.Action{T})"/>
        public static UniTask<T0> ContinueWith<T0>( this UniTask<T0> Task, Func<T0, CancellationToken, UniTask> Continuation, CancellationToken Token ) {
            async UniTask<T0> Continue() {
                T0 Result = await Task;
                await Continuation(Result, Token);
                return Result;
            }

            return Continue();
        }

        /// <summary> Returns a coroutine that waits for the given task to complete. </summary>
        /// <param name="Task"> The task to wait for. </param>
        /// <returns> A coroutine that waits for the given task to complete. </returns>
        public static IEnumerator AsCoroutine( this UniTask Task ) =>
            // bool Finished = false;
            //
            // void ContinuationFunction() => Finished = true;
            // Task.ContinueWith(ContinuationFunction);
            //
            // bool Predicate() => Finished;
            // return new WaitUntil(Predicate);
            new WaitForCompletion(Task);

        static UniTask<T>[] GetTasks<T>( MulticastDelegate MulticastDelegate, object[] Args ) {
            Delegate[]   Delegates = MulticastDelegate.GetInvocationList();
            int          Ln        = Delegates.Length;
            UniTask<T>[] Tasks     = new UniTask<T>[Ln];
            int          ActLn     = Ln;
            for (int I = 0; I < Ln; I++) {
                Delegate Delegate = Delegates[I];
                object?  Result   = Delegate.DynamicInvoke(Args);
                switch (Result) {
                    case UniTask<T> Task:
                        Tasks[I] = Task;
                        // Debug.Log($"Task {I+1} of {Ln} ({Delegate.Method.DeclaringType.GetNiceName()} -> \"{Delegate.Method.GetNiceName()}\") in {MulticastDelegate.Method.GetNiceName()} is a UniTask<{typeof(T).GetNiceName()}>.");
                        break;
                    case null:
                        ActLn--;
                        // Debug.Log($"Task {I+1} of {Ln} ({Delegate.Method.DeclaringType.GetNiceName()} -> \"{Delegate.Method.GetNiceName()}\") in {MulticastDelegate.Method.GetNiceName()} is null (skipping).");
                        break;
                    default:
                        throw new InvalidCastException($"Delegate {I+1} of {Ln} ({Delegate.Method.DeclaringType.GetNiceName()} -> \"{Delegate.Method.GetNiceName()}\") in {MulticastDelegate.Method.GetNiceName()} returned an object of type {Result.GetType().GetNiceName()}, which is not a UniTask!");
                }
            }
            Array.Resize(ref Tasks, ActLn);
            return Tasks;
        }

        static UniTask[] GetTasks( MulticastDelegate MulticastDelegate, object[] Args ) {
            Delegate[] Delegates = MulticastDelegate.GetInvocationList();
            int        Ln        = Delegates.Length;
            UniTask[]  Tasks     = new UniTask[Ln];
            int        ActLn     = Ln;
            for (int I = 0; I < Ln; I++) {
                Delegate Delegate = Delegates[I];
                object?  Result   = Delegate.DynamicInvoke(Args);
                switch (Result) {
                    case UniTask Task:
                        Tasks[I] = Task;
                        // Debug.Log($"Task {I+1} of {Ln} ({Delegate.Method.DeclaringType.GetNiceName()} -> \"{Delegate.Method.GetNiceName()}\") in {MulticastDelegate.Method.GetNiceName()} is a UniTask.");
                        break;
                    case null:
                        ActLn--;
                        // Debug.Log($"Task {I+1} of {Ln} ({Delegate.Method.DeclaringType.GetNiceName()} -> \"{Delegate.Method.GetNiceName()}\") in {MulticastDelegate.Method.GetNiceName()} is null (skipping).");
                        break;
                    default:
                        throw new InvalidCastException($"Delegate {I+1} of {Ln} ({Delegate.Method.DeclaringType.GetNiceName()} -> \"{Delegate.Method.GetNiceName()}\") in {MulticastDelegate.Method.GetNiceName()} returned an object of type {Result.GetType().GetNiceName()}, which is not a UniTask!");
                }
            }
            Array.Resize(ref Tasks, ActLn);
            return Tasks;
        }

        /// <summary> Returns a task that begins all of the given tasks at the same time, and waits for all of them to complete. </summary>
        /// <typeparam name="T"> The type of the return value of the multicast delegate. </typeparam>
        /// <param name="MulticastDelegate"> The multicast delegate to run. </param>
        /// <param name="Aggregator"> The function to aggregate the results of the tasks. </param>
        /// <param name="Args"> The arguments to pass to the multicast delegate. </param>
        /// <returns> A task that begins all of the given tasks at the same time, and waits for all of them to complete. </returns>
        public static UniTask<T> RunAll<T>( this MulticastDelegate MulticastDelegate, Func<T[], T> Aggregator, params object[] Args ) {
            UniTask<T>[] Tasks = GetTasks<T>(MulticastDelegate, Args);
            return UniTask.WhenAll(Tasks).ContinueWith(Aggregator);
        }

        /// <inheritdoc cref="RunAll{T}(System.MulticastDelegate,System.Func{T[],T},System.Object[])"/>
        public static UniTask<T[]> RunAll<T>( this MulticastDelegate MulticastDelegate, params object[] Args ) {
            UniTask<T>[] Tasks = GetTasks<T>(MulticastDelegate, Args);
            return UniTask.WhenAll(Tasks);
        }

        /// <inheritdoc cref="RunAll{T}(System.MulticastDelegate,System.Func{T[],T},System.Object[])"/>
        public static UniTask RunAll( this MulticastDelegate MulticastDelegate, params object[] Args ) {
            UniTask[] Tasks = GetTasks(MulticastDelegate, Args);
            return UniTask.WhenAll(Tasks);
        }

        /// <summary> Returns a cancellation token that is cancelled when either of the given cancellation tokens is cancelled. </summary>
        /// <param name="A"> The first cancellation token. </param>
        /// <param name="B"> The second cancellation token. </param>
        /// <returns> A cancellation token that is cancelled when either of the given cancellation tokens is cancelled. </returns>
        public static CancellationToken Or( this CancellationToken A, CancellationToken B ) {
            CancellationTokenSource Source = new();
            void Callback() => Source.Cancel();
            A.RegisterWithoutCaptureExecutionContext(Callback);
            B.RegisterWithoutCaptureExecutionContext(Callback);
            return Source.Token;
        }
    }

    public sealed class WaitForCompletion : CustomYieldInstruction {
        bool _Finished = false;

        public WaitForCompletion( UniTask Task ) => Task.ContinueWith(ContinuationFunction).Forget(Debug.LogException);

        void ContinuationFunction() => _Finished = true;

        /// <inheritdoc />
        public override bool keepWaiting => !_Finished;
    }

    public sealed class CompletedCoroutine : IEnumerator {
        /// <inheritdoc />
        public object? Current => null;

        /// <inheritdoc />
        public bool MoveNext() => false;

        /// <inheritdoc />
        public void Reset() { }
    }

    public sealed class DynamicTaskReturn<T> where T : notnull {
        bool _Locked;

        /// <summary> Locks this return value. </summary>
        /// <exception cref="InvalidOperationException"> This return value has already been locked. </exception>
        public void Lock() {
            if (_Locked) {
                throw new InvalidOperationException("This return value has already been locked!");
            }
            _Locked = true;
        }

        /// <inheritdoc cref="Lock()"/>
        /// <param name="Value"> The value to set. </param>
        public void Lock( T Value ) {
            this.Value = Value;
            Lock();
        }

        /// <summary> Gets or sets the value. </summary>
        /// <remarks> Make sure to call <see cref="Lock()"/> after setting the value if no other code should be able to change it. </remarks>
        public T Value { get; set; }

        /// <summary> Creates a new dynamic task return value. </summary>
        /// <param name="Value"> The initial value. </param>
        public DynamicTaskReturn( T Value = default! ) => this.Value = Value;

        /// <summary> Throws an exception if this return value has not been locked. </summary>
        /// <exception cref="InvalidOperationException"> This return value has not been locked. </exception>
        public void EnsureLocked() {
            if (!_Locked) {
                throw new InvalidOperationException("This return value has not been locked!");
            }
        }
    }
}
