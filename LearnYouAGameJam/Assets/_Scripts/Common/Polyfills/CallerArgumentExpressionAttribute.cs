// Recreation of CallerArgumentExpressionAttribute from System.Runtime.CompilerServices

using System;
using System.Diagnostics;

namespace LYGJ.Common.Polyfills
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}
