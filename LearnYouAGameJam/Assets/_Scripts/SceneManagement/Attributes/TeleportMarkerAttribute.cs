using System;
using System.Diagnostics;

namespace LYGJ.SceneManagement.Attributes {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class TeleportMarkerAttribute : Attribute {
        /// <summary> Whether to include the &quot;None&quot; option. </summary>
        public readonly bool IncludeNone;

        /// <summary> Marks the member as a teleport marker. </summary>
        /// <param name="IncludeNone"> Whether to include the &quot;None&quot; option. </param>
        public TeleportMarkerAttribute( bool IncludeNone ) => this.IncludeNone = IncludeNone;

        /// <inheritdoc cref="TeleportMarkerAttribute(bool)"/>
        public TeleportMarkerAttribute() : this(true) { }

    }
}
