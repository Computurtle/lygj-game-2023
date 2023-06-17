using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using LYGJ.Common;
using LYGJ.SceneManagement;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem {
    public sealed class Entities : SingletonMB<Entities> {

        /// <inheritdoc cref="SingletonMB{T}.Instance"/>
        public new static Entities Instance {
            get {
                try {
                    return SingletonMB<Entities>.Instance;
                } catch (SingletonNotFoundException) {
                    _Instance = new GameObject(nameof(Entities), typeof(Entities)).GetComponent<Entities>();
                } catch (Exception Exception) {
                    Debug.LogException(Exception);
                    throw;
                }
                return _Instance;
            }
        }

        const    int                        _Capacity = 32;
        readonly Dictionary<string, Entity> _Entities = new(_Capacity);

        public delegate void EntityEventHandler( Entity Entity );

        /// <summary> Raised when an entity is added. </summary>
        public event EntityEventHandler? EntityAdded;

        /// <inheritdoc cref="EntityAdded"/>
        public static event EntityEventHandler? OnAdd {
            add => Instance.EntityAdded += value;
            remove => Instance.EntityAdded -= value;
        }

        /// <summary> Raised when an entity is removed. </summary>
        public event EntityEventHandler? EntityRemoved;

        /// <inheritdoc cref="EntityRemoved"/>
        public static event EntityEventHandler? OnRemove {
            add => Instance.EntityRemoved += value;
            remove => Instance.EntityRemoved -= value;
        }

        /// <summary> Waits for a given entity to be added. </summary>
        /// <param name="Key"> The key of the entity to wait for. </param>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> The asynchronous operation which will complete when the entity is added. </returns>
        [MustUseReturnValue] public static async UniTask<Entity> WaitForAddAsync( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) => await WaitForAddAsync<Entity>(Key, Token);

        /// <inheritdoc cref="WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForAddAsync( Entity Entity, CancellationToken Token = default ) => WaitForAddAsync<Entity>(Entity.Key, Token);

        /// <inheritdoc cref="WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        /// <typeparam name="T"> The type of the entity to wait for. </typeparam>
        public static async UniTask<T> WaitForAddAsync<T>( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) where T : Entity {
            if (TryGet(Key, out T? Entity)) {
                Debug.Log($"Found {Key}/{Entity} in cache. No need to wait.");
                return Entity;
            }
            UniTaskCompletionSource<T>? TCS = new();
            void Callback( Entity Entity ) {
                if (string.Equals(Entity.Key, Key, StringComparison.OrdinalIgnoreCase)) {
                    TCS.TrySetResult((T) Entity);
                    OnAdd -= Callback;
                }
            }
            OnAdd += Callback;
            await using (Token.Register(() => TCS.TrySetCanceled())) {
                (bool IsCanceled, T Result) Result = await TCS.Task.SuppressCancellationThrow();
                if (Result.IsCanceled) { throw new OperationCanceledException(); }
                return Result.Result;
            }
        }

        /// <inheritdoc cref="WaitForAddAsync(string,System.Threading.CancellationToken)"/>
        /// <typeparam name="T"> The type of the entity to wait for. </typeparam>
        [MustUseReturnValue] public static UniTask<T> WaitForAddAsync<T>( T Entity, CancellationToken Token = default ) where T : Entity => WaitForAddAsync<T>(Entity.Key, Token);

        /// <summary> Waits for a given entity to be removed. </summary>
        /// <param name="Key"> The key of the entity to wait for. </param>
        /// <param name="Token"> The cancellation token. </param>
        /// <returns> The asynchronous operation which will complete when the entity is removed. </returns>
        /// <remarks> <see cref="Remove(Entity)"/> is also called when an entity is destroyed. As such, the return result of the operation may be <see langword="null"/>. Entertain caution when using this method result. </remarks>
        [MustUseReturnValue] public static async UniTask<Entity?> WaitForRemoveAsync( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) => await WaitForRemoveAsync<Entity>(Key, Token);

        /// <inheritdoc cref="WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        [MustUseReturnValue] public static UniTask WaitForRemoveAsync( Entity Entity, CancellationToken Token = default ) => WaitForRemoveAsync<Entity>(Entity.Key, Token);

        /// <inheritdoc cref="WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        /// <typeparam name="T"> The type of the entity to wait for. </typeparam>
        [MustUseReturnValue] public static async UniTask<T?> WaitForRemoveAsync<T>( [LocalizationRequired(false)] string Key, CancellationToken Token = default ) where T : Entity {
            if (!Exists(Key)) {
                Debug.Log($"Found no entity with key {Key} in cache. Assuming already removed. No need to wait.");
                return null;
            }
            UniTaskCompletionSource<T>? TCS = new();
            void Callback( Entity Entity ) {
                if (string.Equals(Entity.Key, Key, StringComparison.OrdinalIgnoreCase)) {
                    TCS.TrySetResult((T) Entity);
                    OnRemove -= Callback;
                }
            }
            OnRemove += Callback;
            await using (Token.Register(() => TCS.TrySetCanceled())) {
                return await TCS.Task;
            }
        }

        /// <inheritdoc cref="WaitForRemoveAsync(string,System.Threading.CancellationToken)"/>
        /// <typeparam name="T"> The type of the entity to wait for. </typeparam>
        [MustUseReturnValue] public static UniTask WaitForRemoveAsync<T>( T Entity, CancellationToken Token = default ) where T : Entity => WaitForRemoveAsync<T>(Entity.Key, Token);

        /// <summary> Adds an entity to the dictionary. </summary>
        /// <param name="Entity"> The entity to add. </param>
        /// <exception cref="EntityAlreadyExistsException"> Thrown when an entity with the same key already exists. </exception>
        public static void Add( Entity Entity ) {
            string Key = Entity.Key.ToLowerInvariant();
            Debug.Assert(Key == Entity.Key, "Custom entity keys must be lowercase.", Entity);
            Instance.AddInternal(Key, Entity);
        }

        void AddInternal( string Key, Entity Entity ) {
            // Debug.Log($"Adding entity {entity} with key {Key} to dictionary.", entity);
            if (_Entities.TryGetValue(Key, out Entity? ExistingEntity)) {
                if (ExistingEntity == null) {
                    _Entities[Key] = Entity;
                    EntityAdded?.Invoke(Entity);
                    return;
                }

                throw new EntityAlreadyExistsException(ExistingEntity);
            }

            _Entities.Add(Key, Entity);
            EntityAdded?.Invoke(Entity);
        }

        /// <summary> Removes an entity from the dictionary. </summary>
        /// <param name="Entity"> The entity to remove. </param>
        /// <exception cref="EntityNotFoundException"> Thrown when the entity could not be found. </exception>
        public static void Remove( Entity Entity ) {
            string Key = Entity.Key.ToLowerInvariant();
            Debug.Assert(Key == Entity.Key, "Custom entity keys must be lowercase.", Entity);
            if (_Instance == null) { return; }
            Instance.RemoveInternal(Key);
        }

        /// <summary> Removes an entity from the dictionary. </summary>
        /// <param name="Key"> The entity to remove. </param>
        /// <exception cref="EntityNotFoundException"> Thrown when the entity could not be found. </exception>
        public static void Remove( [LocalizationRequired(false)] string Key ) {
            string LKey = Key.ToLowerInvariant();
            Debug.Assert(Key == LKey, "Custom entity keys must be lowercase.");
            if (_Instance == null) { return; }
            Instance.RemoveInternal(LKey);
        }

        void RemoveInternal( string Key ) {
            // Debug.Log($"Removing entity with key {Key} from dictionary.");
            if (!_Entities.Remove(Key)) {
                // Only throw EntityNotFoundException if the game is running (and not changing state/exiting)
                #if UNITY_EDITOR
                if (Scenes.IsApplicationExiting) { return; }
                throw new EntityNotFoundException(Key);
                #else
                Debug.LogError($"Entity with key {Key} could not be removed because it could not be found!");
                #endif
            }
            EntityRemoved?.Invoke(_Entities[Key]);
        }

        bool TryGetInternal( string Key, [NotNullWhen(true)] out Entity? Entity ) {
            if (_Entities.TryGetValue(Key, out Entity)) {
                if (Entity == null) {
                    _Entities.Remove(Key);
                    Debug.LogWarning($"Entity with key {Key} was destroyed without calling Entities.Remove!", Entity);
                    return false;
                }
                return true;
            }
            // Debug.LogWarning($"Entity with key {Key} could not be found!", entity);
            return false;
        }

        bool TryGetInternal<T>( string Key, [NotNullWhen(true)] out T? Entity ) where T : Entity {
            if (TryGetInternal(Key, out Entity? Found)) {
                if (Found is T Typed) {
                    Entity = Typed;
                    return true;
                }
                Debug.LogWarning($"Entity with key {Key} is not of type {typeof(T).GetNiceName()}! (actual type: {Found.GetType().GetNiceName()})", Found);
            }
            Entity = null;
            return false;
        }

        /// <summary> Attempts to get an entity by its key. </summary>
        /// <param name="Key"> The key of the entity to get. </param>
        /// <param name="Entity"> The entity with the given key, or <see langword="null"/> if no entity with the given key exists. </param>
        /// <returns> <see langword="true"/> if an entity with the given key exists, <see langword="false"/> otherwise. </returns>
        public static bool TryGet( [LocalizationRequired(false)] string Key, [NotNullWhen(true)] out Entity? Entity ) {
            if (Instance.TryGetInternal(Key.ToLowerInvariant(), out Entity)) { return true; }
            return false;
        }

        /// <inheritdoc cref="TryGet(string,out Entity)"/>
        /// <typeparam name="T"> The type of entity to get. </typeparam>
        public static bool TryGet<T>( [LocalizationRequired(false)] string Key, [NotNullWhen(true)] out T? Entity ) where T : Entity {
            if (Instance.TryGetInternal(Key.ToLowerInvariant(), out Entity)) { return true; }
            return false;
        }

        /// <summary> Determines if an entity with the given key exists. </summary>
        /// <param name="Key"> The key of the entity to check for. </param>
        /// <returns> <see langword="true"/> if an entity with the given key exists, <see langword="false"/> otherwise. </returns>
        public static bool Exists( [LocalizationRequired(false)] string Key ) => Instance.TryGetInternal(Key.ToLowerInvariant(), out _);

        /// <summary> Gets an entity by its key. </summary>
        /// <param name="Key"> The key of the entity to get. </param>
        /// <returns> The entity with the given key. </returns>
        /// <exception cref="EntityNotFoundException"> Thrown when no entity with the given key exists. </exception>
        public static Entity Get( [LocalizationRequired(false)] string Key ) {
            Key = Key.ToLowerInvariant();
            if (TryGet(Key, out Entity? Entity)) { return Entity; }
            throw new EntityNotFoundException(Key);
        }

        /// <inheritdoc cref="Get(string)"/>
        /// <typeparam name="T"> The type of entity to get. </typeparam>
        /// <exception cref="EntityInvalidTypeException"> Thrown when the entity with the given key is not of the given type. </exception>
        public static T Get<T>( [LocalizationRequired(false)] string Key ) where T : Entity {
            Entity Found = Get(Key);
            return Found as T ?? throw new EntityInvalidTypeException(Found, typeof(T));
        }

        /// <summary> Gets all entities. </summary>
        /// <returns> All entities. </returns>
        public static IEnumerable<Entity> All {
            get {
                #if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying) {
                    foreach (Entity Entity in FindObjectsOfType<Entity>()) {
                        yield return Entity;
                    }
                }
                #endif

                foreach (Entity? Entity in Instance._Entities.Values) {
                    if (Entity != null) { yield return Entity; }
                }
            }
        }

        /// <inheritdoc cref="All"/>
        /// <typeparam name="T"> The type of entity to get. </typeparam>
        public static IEnumerable<T> OfType<T>() where T : Entity {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                foreach (T Entity in FindObjectsOfType<T>()) {
                    yield return Entity;
                }
            }
            #endif

            foreach (Entity? Entity in Instance._Entities.Values) {
                if (Entity is T TypedEntity) { yield return TypedEntity; }
            }
        }

    }

    public abstract class EntityException : Exception {
        protected EntityException( string Message ) : base(Message) { }
    }

    public sealed class EntityAlreadyExistsException : EntityException {

        /// <inheritdoc />
        public EntityAlreadyExistsException( string Key ) : base($"Entity with key {Key} already exists!") { }

        /// <inheritdoc />
        public EntityAlreadyExistsException( Entity Entity ) : this(Entity.Key) { }
    }

    public sealed class EntityNotFoundException : EntityException {

        /// <inheritdoc />
        public EntityNotFoundException( string Key ) : base($"Entity with key {Key} could not be found!") { }

        /// <inheritdoc />
        public EntityNotFoundException( Entity Entity ) : this(Entity.Key) { }
    }

    public sealed class EntityInvalidTypeException : EntityException {

        /// <inheritdoc />
        public EntityInvalidTypeException( string Key, Type ExpectedType, Type ActualType ) : base($"Entity with key {Key} is not of type {ExpectedType.GetNiceName()} (actual type: {ActualType.GetNiceName()})!") { }

        /// <inheritdoc />
        public EntityInvalidTypeException( Entity Entity, Type ExpectedType ) : this(Entity.Key, ExpectedType, Entity.GetType()) { }
    }
}
