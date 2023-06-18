using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace LYGJ.EntitySystem {
    
    public static class Objects {
        /// <inheritdoc cref="Entities.Add(Entity)"/>
        public static void Add( ObjectBase Object ) => Entities.Add(Object);

        /// <inheritdoc cref="Entities.Remove(Entity)"/>
        public static void Remove( ObjectBase Object ) => Entities.Remove(Object);

        /// <inheritdoc cref="Entities.Remove(string)"/>
        public static void Remove( [LocalizationRequired(false)] string Key ) => Entities.Remove(Key);

        /// <inheritdoc cref="Entities.TryGet{T}(string,out T)"/>
        public static bool TryGet( [LocalizationRequired(false)] string Key, [NotNullWhen(true)] out ObjectBase? Object ) => Entities.TryGet(Key, out Object);

        /// <inheritdoc cref="Entities.TryGet{T}(string,out T)"/>
        public static bool TryGet<T>( [LocalizationRequired(false)] string Key, [NotNullWhen(true)] out T? Object ) where T : ObjectBase => Entities.TryGet(Key, out Object);

        /// <inheritdoc cref="Entities.Exists(string)"/>
        public static bool Exists( [LocalizationRequired(false)] string Key ) => Entities.Exists(Key);

        /// <inheritdoc cref="Entities.Get{T}(string)"/>
        public static ObjectBase Get( [LocalizationRequired(false)] string Key ) => Entities.Get<ObjectBase>(Key);

        /// <inheritdoc cref="Entities.Get{T}(string)"/>
        public static T Get<T>( [LocalizationRequired(false)] string Key ) where T : ObjectBase => Entities.Get<T>(Key);

        /// <summary> Gets all objects. </summary>
        public static IEnumerable<ObjectBase> All => Entities.OfType<ObjectBase>();

        /// <inheritdoc cref="Entities.OfType{T}"/>
        public static IEnumerable<T> OfType<T>() where T : ObjectBase => Entities.OfType<T>();
        
        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<ObjectBase> WaitForAddAsync( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) => Entities.WaitForAddAsync<ObjectBase>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForAddAsync( ObjectBase Object, CancellationToken Token = default ) => Entities.WaitForAddAsync(Object, Token);

        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<T> WaitForAddAsync<T>( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) where T : ObjectBase => Entities.WaitForAddAsync<T>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForAddAsync<T>( T Object, CancellationToken Token = default ) where T : ObjectBase => Entities.WaitForAddAsync(Object, Token);
        
        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<ObjectBase?> WaitForRemoveAsync( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) => Entities.WaitForRemoveAsync<ObjectBase>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForRemoveAsync( ObjectBase Object, CancellationToken Token = default ) => Entities.WaitForRemoveAsync(Object, Token);

        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<T?> WaitForRemoveAsync<T>( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) where T : ObjectBase => Entities.WaitForRemoveAsync<T>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForRemoveAsync<T>( T Object, CancellationToken Token = default ) where T : ObjectBase => Entities.WaitForRemoveAsync(Object, Token);
    }
}
