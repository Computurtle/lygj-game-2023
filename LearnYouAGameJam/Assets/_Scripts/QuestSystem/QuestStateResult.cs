using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using LYGJ.EntitySystem;
using LYGJ.EntitySystem.NPCSystem;
using OneOf;

namespace LYGJ.QuestSystem {
    public sealed class QuestStateResult : OneOfBase<QuestStateResultConstant, QuestStateResultRequiresRestart>, IEquatable<QuestStateResult>, IEquatable<QuestStateResultConstant>, IEquatable<QuestStateResultRequiresRestart> {

        QuestStateResult(OneOf<QuestStateResultConstant, QuestStateResultRequiresRestart> Value) : base(Value) { }

        public static implicit operator QuestStateResult( QuestStateResultConstant Value ) => new(Value);
        public static implicit operator QuestStateResult( QuestStateResultRequiresRestart Value ) => new(Value);

        /// <inheritdoc cref="QuestStateResultConstant.OK"/>
        public static QuestStateResult OK { get; } = new(QuestStateResultConstant.OK);

        /// <inheritdoc cref="QuestStateResultConstant.Skip"/>
        public static QuestStateResult Skip { get; } = new(QuestStateResultConstant.Skip);

        /// <inheritdoc cref="QuestStateResultConstant.RequiresSceneChange"/>
        public static QuestStateResult RequiresSceneChange { get; } = new(QuestStateResultConstant.RequiresSceneChange);

        /// <inheritdoc cref="QuestStateResultRequiresRestart(System.Func{System.Threading.CancellationToken,Cysharp.Threading.Tasks.UniTask})"/>
        public static QuestStateResult RequiresRestart( Func<CancellationToken, UniTask> Delay ) => new QuestStateResultRequiresRestart(Delay);
        /// <inheritdoc cref="QuestStateResultRequiresRestart(int)"/>
        public static QuestStateResult RequiresRestart( int Delay ) => new QuestStateResultRequiresRestart(Delay);
        /// <inheritdoc cref="QuestStateResultRequiresRestart.Immediate()"/>
        public static QuestStateResult RequiresImmediateRestart() => QuestStateResultRequiresRestart.Immediate();

        /// <summary> Delays the quest state until an entity of the given key is added to the registrar. </summary>
        /// <param name="Key"> The key. </param>
        public static QuestStateResult RequiresEntity( [LocalizationRequired(false)] string Key ) => new QuestStateResultRequiresRestart(Token => Entities.WaitForAddAsync(Key, Token));
        /// <summary> Delays the quest state until an object of the given key and type is added to the registrar. </summary>
        /// <param name="Key"> The key. </param>
        public static QuestStateResult RequiresObject( [LocalizationRequired(false)] string Key ) => new QuestStateResultRequiresRestart(Token => Objects.WaitForAddAsync(Key, Token));
        /// <summary> Delays the quest state until an object of the given key and type is added to the registrar. </summary>
        /// <typeparam name="T"> The type of object. </typeparam>
        /// <param name="Key"> The key. </param>
        public static QuestStateResult RequiresObject<T>( [LocalizationRequired(false)] string Key ) where T : ObjectBase => new QuestStateResultRequiresRestart(Token => Objects.WaitForAddAsync<T>(Key, Token));
        /// <summary> Delays the quest state until an object of the given key and type is added to the registrar. </summary>
        /// <param name="Key"> The key. </param>
        public static QuestStateResult RequiresNPC( [LocalizationRequired(false)] string Key ) => new QuestStateResultRequiresRestart(Token => NPCs.WaitForAddAsync(Key, Token));
        /// <summary> Delays the quest state until an object of the given key and type is added to the registrar. </summary>
        /// <typeparam name="T"> The type of object. </typeparam>
        /// <param name="Key"> The key. </param>
        public static QuestStateResult RequiresNPC<T>( [LocalizationRequired(false)] string Key ) where T : NPCBase => new QuestStateResultRequiresRestart(Token => NPCs.WaitForAddAsync<T>(Key, Token));

        /// <summary> Delays the quest state until the given task is raised. </summary>
        public static QuestStateResult RequiresTaskCompletion( Func<CancellationToken, UniTask> Task ) => new QuestStateResultRequiresRestart(Task);

        #region Equality Members

        /// <inheritdoc />
        public bool Equals( QuestStateResult? Other ) => base.Equals(Other);

        /// <inheritdoc />
        public bool Equals( QuestStateResultConstant Other ) => Index == 0 && (QuestStateResultConstant)Value == Other;

        /// <inheritdoc />
        public bool Equals( QuestStateResultRequiresRestart Other ) => Index == 1 && (QuestStateResultRequiresRestart)Value == Other;

        /// <inheritdoc />
        public override bool Equals( object? Obj ) => ReferenceEquals(this, Obj)
            || Obj switch {
                QuestStateResult Result                 => Equals(Result),
                QuestStateResultConstant Constant       => Equals(Constant),
                QuestStateResultRequiresRestart Restart => Equals(Restart),
                _                                       => false
            };

        /// <inheritdoc />
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==( QuestStateResult? Left, QuestStateResult? Right ) => Equals(Left, Right);
        public static bool operator !=( QuestStateResult? Left, QuestStateResult? Right ) => !Equals(Left, Right);

        #endregion

    }

    public readonly struct QuestStateResultRequiresRestart : IEquatable<QuestStateResultRequiresRestart> {
        readonly Func<CancellationToken, UniTask> _Delay;

        /// <summary> Delays the quest state until the predefined delay is complete. </summary>
        /// <param name="Delay"> The delay. </param>
        public QuestStateResultRequiresRestart( Func<CancellationToken, UniTask> Delay ) => _Delay = Delay;

        /// <summary> Delays the quest state until the predefined delay is complete. </summary>
        /// <param name="Delay"> The delay, in milliseconds. </param>
        public QuestStateResultRequiresRestart( int Delay ) => _Delay = Token => UniTask.Delay(Delay, cancellationToken: Token);

        /// <summary> Retries the quest state immediately. </summary>
        public static QuestStateResultRequiresRestart Immediate() => new(Token => Token.IsCancellationRequested ? UniTask.FromCanceled(Token) : UniTask.CompletedTask);

        /// <summary> Await the delay. </summary>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> A <see cref="UniTask"/> representing the asynchronous operation. </returns>
        public UniTask Delay( CancellationToken Token ) => _Delay(Token).SuppressCancellationThrow();

        #region Equality Members

        /// <inheritdoc />
        public bool Equals( QuestStateResultRequiresRestart Other ) => _Delay.Equals(Other._Delay);

        /// <inheritdoc />
        public override bool Equals( object? Obj ) => Obj is QuestStateResultRequiresRestart Other && Equals(Other);

        /// <inheritdoc />
        public override int GetHashCode() => _Delay.GetHashCode();

        public static bool operator ==( QuestStateResultRequiresRestart Left, QuestStateResultRequiresRestart Right ) => Left.Equals(Right);
        public static bool operator !=( QuestStateResultRequiresRestart Left, QuestStateResultRequiresRestart Right ) => !Left.Equals(Right);

        #endregion

    }

    public enum QuestStateResultConstant {
        /// <summary> The quest state completed successfully. </summary>
        OK = 0,
        /// <summary> The quest state should be skipped. </summary>
        Skip = -1,

        /// <summary> The quest state requires a scene change before being reconsidered. </summary>
        RequiresSceneChange = 1
    }
}
