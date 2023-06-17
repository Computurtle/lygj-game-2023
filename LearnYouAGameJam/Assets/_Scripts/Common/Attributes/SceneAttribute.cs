using System;
using System.Diagnostics;

namespace LYGJ.Common.Attributes {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class SceneAttribute : Attribute {
        /// <summary> Whether to include a &quot;None&quot; option. Even if not allowed, an invalid scene may still be returned (i.e. if it was deleted). </summary>
        public readonly bool IncludeNone;

        /// <summary> Marks a string field, property or parameter as a scene. </summary>
        /// <param name="IncludeNone"> Whether to include a &quot;None&quot; option. Even if not allowed, an invalid scene may still be returned (i.e. if it was deleted). </param>
        public SceneAttribute( bool IncludeNone = false ) => this.IncludeNone = IncludeNone;
    }
}
