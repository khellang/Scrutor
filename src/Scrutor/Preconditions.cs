using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Scrutor;

[DebuggerStepThrough]
internal static class Preconditions
{
    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>([NoEnumeration] T value, [InvokerParameterName] string parameterName)
        where T : class
    {
        if (ReferenceEquals(value, null))
        {
            NotEmpty(parameterName, nameof(parameterName));

            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotEmpty(string value, [InvokerParameterName] string parameterName)
    {
        if (ReferenceEquals(value, null))
        {
            NotEmpty(parameterName, nameof(parameterName));

            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            NotEmpty(parameterName, nameof(parameterName));

            throw new ArgumentException("String value cannot be null.", parameterName);
        }

        return value;
    }

    public static TEnum IsDefined<TEnum>(TEnum value, [InvokerParameterName] string parameterName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            NotEmpty(parameterName, nameof(parameterName));

            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}
