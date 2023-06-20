using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LYGJ.Common {
    public static class Pool<T> where T : Component {

        // A 'pooled' item is any disabled item in the parent.

        /// <summary> Gets a pooled item, creating one if necessary. </summary>
        /// <param name="Parent"> The parent of the item. </param>
        /// <param name="Prefab"> The prefab to use if a new item is created. </param>
        /// <param name="New"> [out] Whether a new item was created. </param>
        /// <returns> The pooled item. </returns>
        public static T Get( Transform Parent, T Prefab, out bool New ) {
            if (Parent == null) { throw new InvalidOperationException("No parent provided."); }
            if (Prefab == null) { throw new InvalidOperationException("No prefab provided."); }

            New = false;
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: true)) {
                if (Child.gameObject.activeSelf) { continue; }
                Child.gameObject.SetActive(true);
                return Child;
            }

            New = true;
            return Object.Instantiate(Prefab, Parent);
        }

        /// <inheritdoc cref="Get(Transform,T,out bool)"/>
        public static T Get( Transform Parent, T Prefab ) => Get(Parent, Prefab, out _);

        /// <inheritdoc cref="Get(Transform,T,out bool)"/>
        public static T Get( T Prefab, Transform Parent, out bool New ) => Get(Parent, Prefab, out New);

        /// <inheritdoc cref="Get(Transform,T,out bool)"/>
        public static T Get( T Prefab, Transform Parent ) => Get(Parent, Prefab, out _);

        /// <summary> Attempts to get a pooled item. </summary>
        /// <param name="Parent"> The parent of the item. </param>
        /// <param name="Item"> [out] The pooled item. </param>
        /// <param name="IncludeActive"> Whether to include active items. </param>
        /// <returns> <see langword="true"/> if a pooled item was found, <see langword="false"/> otherwise. </returns>
        public static bool TryGet( Transform Parent, [MaybeNullWhen(false)] out T Item, bool IncludeActive = false ) {
            if (Parent == null) { Debug.LogError("Parent cannot be null."); Item = null; return false; }
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: true)) {
                if (!IncludeActive && Child.gameObject.activeSelf) { continue; }
                Item = Child;
                return true;
            }

            Item = null;
            return false;
        }

        /// <summary> Retrieves all pooled items. </summary>
        /// <param name="Parent"> The parent of the items. </param>
        /// <param name="IncludeUsed"> Whether to include used items. </param>
        /// <returns> The pooled items. </returns>
        public static IEnumerable<T> GetAll( Transform Parent, bool IncludeUsed = false ) {
            if (Parent == null) { Debug.LogError("Parent cannot be null."); yield break; }
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: !IncludeUsed)) {
                if (!IncludeUsed && Child.gameObject.activeSelf) { continue; }
                yield return Child;
            }
        }

        /// <summary> Returns all pooled items to the pool. </summary>
        /// <param name="Parent"> The parent of the items. </param>
        /// <returns> The number of items returned. </returns>
        public static int ReturnAll( Transform Parent ) {
            if (Parent == null) { Debug.LogError("Parent cannot be null."); return 0; }
            int Returned = 0;
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: true)) {
                Child.gameObject.SetActive(false);
                Returned++;
            }
            return Returned;
        }

        /// <summary> Returns the specified item to the pool. </summary>
        /// <param name="Item"> The item to return. </param>
        public static void Return( T Item ) {
            if (Item == null) { Debug.LogError("Cannot return a null item."); return; }
            Item.gameObject.SetActive(false);
        }

        /// <summary> Purges the pool. </summary>
        /// <remarks> This destroys all pooled items. As such it is not recommended, as it defeats the purpose of pooling. </remarks>
        /// <param name="Parent"> The parent of the items. </param>
        public static void Purge( Transform Parent ) {
            if (Parent == null) { Debug.LogError("Parent cannot be null."); return; }
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: true)) {
                Object.Destroy(Child.gameObject);
            }
        }

        /// <summary> Returns all active items in the pool. </summary>
        /// <param name="Parent"> The parent of the items. </param>
        /// <returns> The active items. </returns>
        public static IEnumerable<T> Active( Transform Parent ) {
            if (Parent == null) { Debug.LogError("Parent cannot be null."); yield break; }
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: true)) {
                if (Child.gameObject.activeSelf) { yield return Child; }
            }
        }

        /// <summary> Returns all inactive items in the pool. </summary>
        /// <param name="Parent"> The parent of the items. </param>
        /// <returns> The inactive items. </returns>
        public static IEnumerable<T> Inactive( Transform Parent ) {
            if (Parent == null) { Debug.LogError("Parent cannot be null."); yield break; }
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: true)) {
                if (!Child.gameObject.activeSelf) { yield return Child; }
            }
        }

        /// <summary> Returns the number of active items in the pool. </summary>
        /// <param name="Parent"> The parent of the items. </param>
        /// <returns> The number of active items. </returns>
        public static int ActiveCount( Transform Parent ) {
            if (Parent == null) { Debug.LogError("Parent cannot be null."); return 0; }
            int Count = 0;
            foreach (T Child in Parent.GetComponentsInChildren<T>(includeInactive: true)) {
                if (Child.gameObject.activeSelf) { Count++; }
            }
            return Count;
        }

    }

    public static class Pool {
        /// <inheritdoc cref="Pool{T}.Get(Transform,T,out bool)"/>
        public static T Get<T>( Transform Parent, T Prefab, out bool New ) where T : Component => Pool<T>.Get(Parent, Prefab, out New);

        /// <inheritdoc cref="Pool{T}.Get(Transform,T,out bool)"/>
        public static T Get<T>( Transform Parent, T Prefab ) where T : Component => Pool<T>.Get(Parent, Prefab, out _);

        /// <inheritdoc cref="Pool{T}.Get(Transform,T,out bool)"/>
        public static T Get<T>( T Prefab, Transform Parent, out bool New ) where T : Component => Pool<T>.Get(Parent, Prefab, out New);

        /// <inheritdoc cref="Pool{T}.Get(Transform,T,out bool)"/>
        public static T Get<T>( T Prefab, Transform Parent ) where T : Component => Pool<T>.Get(Parent, Prefab, out _);

        /// <inheritdoc cref="Pool{T}.TryGet(Transform,out T,bool)"/>
        public static bool TryGet<T>( Transform Parent, [MaybeNullWhen(false)] out T Item, bool IncludeActive = false ) where T : Component => Pool<T>.TryGet(Parent, out Item, IncludeActive);

        /// <inheritdoc cref="Pool{T}.ReturnAll(Transform)"/>
        public static void ReturnAll<T>( Transform Parent ) where T : Component => Pool<T>.ReturnAll(Parent);

        /// <inheritdoc cref="Pool{T}.Return(T)"/>
        public static void Return<T>( T Item ) where T : Component => Pool<T>.Return(Item);

        /// <inheritdoc cref="Pool{T}.Purge(Transform)"/>
        public static void Purge<T>( Transform Parent ) where T : Component => Pool<T>.Purge(Parent);

        /// <inheritdoc cref="Pool{T}.Active(Transform)"/>
        public static IEnumerable<T> Active<T>( Transform Parent ) where T : Component => Pool<T>.Active(Parent);

        /// <inheritdoc cref="Pool{T}.Inactive(Transform)"/>
        public static IEnumerable<T> Inactive<T>( Transform Parent ) where T : Component => Pool<T>.Inactive(Parent);
    }
}
