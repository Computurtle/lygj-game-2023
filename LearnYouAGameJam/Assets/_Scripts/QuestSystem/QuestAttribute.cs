using System;
using System.Diagnostics;

namespace LYGJ.QuestSystem {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class QuestAttribute : Attribute {
        /// <summary> Whether or not to allow &quot;None&quot; as an option. </summary>
        public readonly bool AllowNone;

        /// <summary> Indicates that the string should be a quest ID. </summary>
        /// <param name="AllowNone"> Whether or not to allow &quot;None&quot; as an option. </param>
        public QuestAttribute( bool AllowNone = false ) => this.AllowNone = AllowNone;

        /// <inheritdoc cref="QuestAttribute(bool)"/>
        public QuestAttribute() : this(false) { }

    }
}
