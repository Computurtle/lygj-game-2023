using System.Diagnostics;
using UnityEngine;

namespace LYGJ.EntitySystem {
    public abstract class Entity : MonoBehaviour {
        /// <summary> The key used to identify the entity. </summary>
        public abstract string Key { get; }

        protected virtual void Awake() => Entities.Add(this);

        protected virtual void OnDestroy() => Entities.Remove(this);
    }
}
