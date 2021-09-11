using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace Scrutor.Activation
{
    /// <summary>
    /// Implementation of the <see cref="IServiceActivator"/> based on implementation details of the <see cref="ActivatorUtilities"/> but uses the regular DI constructor matching. 
    /// For more details https://github.com/khellang/Scrutor/issues/116
    /// </summary>
    public class ScrutorServiceActivator : IServiceActivator
    {
        #region Inner

        private static class ParameterDefaultValue
        {
            public static bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
            {
                bool hasDefaultValue;

#if NETFRAMEWORK || NETSTANDARD2_0
                hasDefaultValue = CheckHasDefaultValue_NETSTANDARD(parameter, out bool tryToGetDefaultValue);
#else
                hasDefaultValue = CheckHasDefaultValue_NETCORE(parameter, out bool tryToGetDefaultValue);
#endif

                defaultValue = null;

                if (hasDefaultValue)
                {
                    if (tryToGetDefaultValue)
                    {
                        defaultValue = parameter.DefaultValue;
                    }

                    bool isNullableParameterType = parameter.ParameterType.IsGenericType &&
                        parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>);

                    // Workaround for https://github.com/dotnet/runtime/issues/18599
                    if (defaultValue == null && parameter.ParameterType.IsValueType
                        && !isNullableParameterType) // Nullable types should be left null
                    {
                        defaultValue = CreateValueType(parameter.ParameterType);
                    }

                    static object? CreateValueType(Type t) =>
#if NETFRAMEWORK || NETSTANDARD2_0
                    FormatterServices.GetUninitializedObject(t);
#else
                    RuntimeHelpers.GetUninitializedObject(t);
#endif

                    // Handle nullable enums
                    if (defaultValue != null && isNullableParameterType)
                    {
                        Type? underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType);
                        if (underlyingType != null && underlyingType.IsEnum)
                        {
                            defaultValue = Enum.ToObject(underlyingType, defaultValue);
                        }
                    }
                }

                return hasDefaultValue;
            }

            public static bool CheckHasDefaultValue_NETSTANDARD(ParameterInfo parameter, out bool tryToGetDefaultValue)
            {
                tryToGetDefaultValue = true;
                try
                {
                    return parameter.HasDefaultValue;
                }
                catch (FormatException) when (parameter.ParameterType == typeof(DateTime))
                {
                    // Workaround for https://github.com/dotnet/runtime/issues/18844
                    // If HasDefaultValue throws FormatException for DateTime
                    // we expect it to have default value
                    tryToGetDefaultValue = false;
                    return true;
                }
            }

            public static bool CheckHasDefaultValue_NETCORE(ParameterInfo parameter, out bool tryToGetDefaultValue)
            {
                tryToGetDefaultValue = true;
                return parameter.HasDefaultValue;
            }
        }

        private struct ConstructorMatcher
        {
            private readonly ConstructorInfo _constructor;
            private readonly ParameterInfo[] _parameters;
            private readonly object?[] _parameterValues;

            public ConstructorMatcher(ConstructorInfo constructor)
            {
                _constructor = constructor;
                _parameters = _constructor.GetParameters();
                _parameterValues = new object?[_parameters.Length];
            }

            public int Match(object[] givenParameters)
            {
                int applyIndexStart = 0;
                int applyExactLength = 0;
                for (int givenIndex = 0; givenIndex != givenParameters.Length; givenIndex++)
                {
                    Type? givenType = givenParameters[givenIndex]?.GetType();
                    bool givenMatched = false;

                    for (int applyIndex = applyIndexStart; givenMatched == false && applyIndex != _parameters.Length; ++applyIndex)
                    {
                        if (_parameterValues[applyIndex] == null &&
                            _parameters[applyIndex].ParameterType.IsAssignableFrom(givenType))
                        {
                            givenMatched = true;
                            _parameterValues[applyIndex] = givenParameters[givenIndex];
                            if (applyIndexStart == applyIndex)
                            {
                                applyIndexStart++;
                                if (applyIndex == givenIndex)
                                {
                                    applyExactLength = applyIndex;
                                }
                            }
                        }
                    }

                    if (givenMatched == false)
                    {
                        return -1;
                    }
                }
                return applyExactLength;
            }

            public object CreateInstance(IServiceProvider provider)
            {
                for (int index = 0; index != _parameters.Length; index++)
                {
                    if (_parameterValues[index] == null)
                    {
                        object? value = provider.GetService(_parameters[index].ParameterType);
                        if (value == null)
                        {
                            if (!ParameterDefaultValue.TryGetDefaultValue(_parameters[index], out object? defaultValue))
                            {
                                throw new InvalidOperationException($"Unable to resolve service for type '{_parameters[index].ParameterType}' while attempting to activate '{_constructor.DeclaringType}'.");
                            }
                            else
                            {
                                _parameterValues[index] = defaultValue;
                            }
                        }
                        else
                        {
                            _parameterValues[index] = value;
                        }
                    }
                }

#if NETFRAMEWORK || NETSTANDARD2_0
                try
                {
                    return _constructor.Invoke(_parameterValues);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    // The above line will always throw, but the compiler requires we throw explicitly.
                    throw;
                }
#else
                return _constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: _parameterValues, culture: null);
#endif
            }
        }

        #endregion

        public object CreateInstance(IServiceProvider provider, Type type, params object[] arguments)
        {
            int bestLength = -1;
            bool seenPreferred = false;

            ConstructorMatcher bestMatcher = default;

            if (!type.IsAbstract)
            {
                foreach (ConstructorInfo? constructor in type.GetConstructors())
                {
                    var matcher = new ConstructorMatcher(constructor);

                    bool isPreferred = constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false);

                    int length = matcher.Match(arguments);

                    if (isPreferred)
                    {
                        if (seenPreferred)
                        {
                            throw new InvalidOperationException($"Multiple constructors were marked with {nameof(ActivatorUtilitiesConstructorAttribute)}.");
                        }

                        if (length == -1)
                        {
                            throw new InvalidOperationException($"Constructor marked with {nameof(ActivatorUtilitiesConstructorAttribute)} does not accept all given argument types.");
                        }
                    }

                    if (isPreferred || bestLength < length)
                    {
                        bestLength = length;
                        bestMatcher = matcher;
                    }

                    seenPreferred |= isPreferred;
                }
            }

            if (bestLength == -1)
            {
                string? message = $"A suitable constructor for type '{type}' could not be located. Ensure the type is concrete and all parameters of a public constructor are either registered as services or passed as arguments. Also ensure no extraneous arguments are provided.";
                throw new InvalidOperationException(message);
            }

            return bestMatcher.CreateInstance(provider);

        }
    }
}
