using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace LYGJ.EntitySystem.NPCSystem {
    public static class NPCs {
        /// <inheritdoc cref="Entities.Add(Entity)"/>
        public static void Add( NPCBase NPC ) => Entities.Add(NPC);

        /// <inheritdoc cref="Entities.Remove(Entity)"/>
        public static void Remove( NPCBase NPC ) => Entities.Remove(NPC);

        /// <inheritdoc cref="Entities.Remove(string)"/>
        public static void Remove( [LocalizationRequired(false)] string Key ) => Entities.Remove(Key);

        /// <inheritdoc cref="Entities.TryGet{T}(string,out T)"/>
        public static bool TryGet( [LocalizationRequired(false)] string Key, [NotNullWhen(true)] out NPCBase? NPC ) => Entities.TryGet(Key, out NPC);

        /// <inheritdoc cref="Entities.Exists(string)"/>
        public static bool Exists( [LocalizationRequired(false)] string Key ) => Entities.Exists(Key);

        /// <inheritdoc cref="Entities.Get(string)"/>
        public static NPCBase Get( [LocalizationRequired(false)] string Key ) => Entities.Get<NPCBase>(Key);

        /// <summary> Gets all NPCs. </summary>
        public static IEnumerable<NPCBase> All => Entities.OfType<NPCBase>();
        
        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<NPCBase> WaitForAddAsync( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) => Entities.WaitForAddAsync<NPCBase>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForAddAsync( NPCBase NPC, CancellationToken Token = default ) => Entities.WaitForAddAsync(NPC, Token);

        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<T> WaitForAddAsync<T>( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) where T : NPCBase => Entities.WaitForAddAsync<T>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForAddAsync<T>( T NPC, CancellationToken Token = default ) where T : NPCBase => Entities.WaitForAddAsync(NPC, Token);
        
        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<NPCBase?> WaitForRemoveAsync( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) => Entities.WaitForRemoveAsync<NPCBase>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForRemoveAsync( NPCBase NPC, CancellationToken Token = default ) => Entities.WaitForRemoveAsync(NPC, Token);

        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask<T?> WaitForRemoveAsync<T>( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) where T : NPCBase => Entities.WaitForRemoveAsync<T>(Key, Token);

        /// <inheritdoc cref="Entities.WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForRemoveAsync<T>( T NPC, CancellationToken Token = default ) where T : NPCBase => Entities.WaitForRemoveAsync(NPC, Token);
    }
}
