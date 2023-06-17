using System;
using System.Diagnostics;

namespace LYGJ.Common.Attributes {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class AnimParamAttribute : Attribute {
        /// <summary> The source of the component. </summary>
        public RelativeComponentSource Source { get; }

        /// <summary> Indicates that the given string is the name of the parameter. </summary>
        /// <param name="Source"> The source of the component. </param>
        public AnimParamAttribute( RelativeComponentSource Source = RelativeComponentSource.This ) => this.Source = Source;
    }

    public enum RelativeComponentSource {
        This,
        Children,
        Parent
    }
}
