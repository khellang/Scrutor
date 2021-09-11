using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

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

        private static class ConstructorAnalyzer
        {
            #region Inner

            public struct ConstructorMatch
            {
                public ConstructorInfo ConstructorInfo;
                public ParameterInfo[] Parameters;
                public object?[] ParameterValues;

                public ConstructorMatch(ConstructorInfo ci)
                {
                    ConstructorInfo = ci;

                    Parameters = ci.GetParameters();
                    ParameterValues = new object?[Parameters.Length];
                }

                public bool IsPrefered => ConstructorInfo.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false);

                public int MatchWeightAccordingToGiven(object[] givenParameters)
                {
                    int applyIndexStart = 0;
                    int applyExactLength = 0;
                    for (int givenIndex = 0; givenIndex != givenParameters.Length; givenIndex++)
                    {
                        Type? givenType = givenParameters[givenIndex]?.GetType();
                        bool givenMatched = false;

                        for (int applyIndex = applyIndexStart; givenMatched == false && applyIndex != Parameters.Length; ++applyIndex)
                        {
                            if (ParameterValues[applyIndex] == null &&
                                Parameters[applyIndex].ParameterType.IsAssignableFrom(givenType))
                            {
                                givenMatched = true;
                                ParameterValues[applyIndex] = givenParameters[givenIndex];
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
                    for (int index = 0; index != Parameters.Length; index++)
                    {
                        if (ParameterValues[index] == null)
                        {
                            object? value = provider.GetService(Parameters[index].ParameterType);
                            if (value == null)
                            {
                                if (!ParameterDefaultValue.TryGetDefaultValue(Parameters[index], out object? defaultValue))
                                {
                                    throw new InvalidOperationException($"Unable to resolve service for type '{Parameters[index].ParameterType}' while attempting to activate '{ConstructorInfo.DeclaringType}'.");
                                }
                                else
                                {
                                    ParameterValues[index] = defaultValue;
                                }
                            }
                            else
                            {
                                ParameterValues[index] = value;
                            }
                        }
                    }

#if NETFRAMEWORK || NETSTANDARD2_0
                    try
                    {
                        return ConstructorInfo.Invoke(ParameterValues);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        // The above line will always throw, but the compiler requires we throw explicitly.
                        throw;
                    }
#else
                return ConstructorInfo.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: ParameterValues, culture: null);
#endif
                }
            }


            #endregion

            public static (ConstructorMatch? constructorMatch, Stack<ConstructorMatch>? fallbackQueue) MatchBest(Type typeToAnalyze, object[] passedParameters, bool useFallbacks)
            {
                ConstructorMatch? result = null;
                Stack<ConstructorMatch>? fallbackQueue = useFallbacks ? new Stack<ConstructorMatch>() : null;

                if (!typeToAnalyze.IsAbstract)
                {
                    ConstructorInfo[] constructors = typeToAnalyze.GetConstructors();

                    bool seenPreferred = false;
                    bool seenExactlyMatched = false;

                    int maxGivenWeight = -1;
                    int maxArgumentsWeight = -1;
                    for (int i = 0; i < constructors.Length; ++i)
                    {
                        var constructorMatch = new ConstructorMatch(constructors[i]);

                        if (constructorMatch.IsPrefered)
                        {
                            if (seenPreferred)
                            {
                                throw new InvalidOperationException($"Multiple constructors were marked with {nameof(ActivatorUtilitiesConstructorAttribute)}.");
                            }

                            seenPreferred = true;

                            int givenWeight = constructorMatch.MatchWeightAccordingToGiven(passedParameters);
                            if (givenWeight == -1)
                            {
                                throw new InvalidOperationException($"Constructor marked with {nameof(ActivatorUtilitiesConstructorAttribute)} does not accept all given argument types.");
                            }

                            result = constructorMatch;
                        }

                        // We considering other constructors only if prefered or exactly matched accroding to a given arguments was not found.
                        if (!seenPreferred && !seenExactlyMatched)
                        {
                            int givenWeight = constructorMatch.MatchWeightAccordingToGiven(passedParameters);
                            if (givenWeight == -1)
                                continue;

                            int argumentsWeight = constructorMatch.Parameters.Length;

                            // This situation means exactly match, so we will use this constructor if preferred was not defined.
                            if (passedParameters.Length == argumentsWeight && ((passedParameters.Length - 1) == givenWeight))
                            {
                                seenExactlyMatched = true;

                                result = constructorMatch;

                                continue;
                            }

                            // No sense to consider because we already have a better match according to the passed argumetns.
                            if (givenWeight < maxGivenWeight)
                                continue;

                            if (givenWeight == maxGivenWeight)
                            {
                                if (argumentsWeight > maxArgumentsWeight)
                                {
                                    maxArgumentsWeight = argumentsWeight;

                                    if (result.HasValue)
                                        fallbackQueue?.Push(result.Value);
                                    result = constructorMatch;
                                }
                            }
                            // Means that givenWeight more than maxGivenWeigth.
                            else
                            {
                                maxGivenWeight = givenWeight;
                                maxArgumentsWeight = argumentsWeight;

                                if (result.HasValue)
                                    fallbackQueue?.Push(result.Value);
                                result = constructorMatch;
                            }
                        }
                    }
                }

                return (result, fallbackQueue);
            }
        }

        #endregion

        /// <summary>
        /// JUST FOR <see cref="Activator.CreateInstance()"/>
        /// </summary>
        internal ScrutorServiceActivator()
            : this(useFallbacks: false)
        {
        }


        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="useFallbacks"><see cref="UseFallbacks"/></param>
        public ScrutorServiceActivator(bool useFallbacks = false)
        {
            UseFallbacks = useFallbacks;
        }

        /// <summary>
        /// Whether fallback constructors needs to be used if it's not possible to resolve the DI prefered one? 
        /// </summary>
        public bool UseFallbacks { get; internal set; }

        public object CreateInstance(IServiceProvider provider, Type type, params object[] arguments)
        {
            var bestMatched = ConstructorAnalyzer.MatchBest(type, arguments, UseFallbacks);
            ConstructorAnalyzer.ConstructorMatch? constructorMatch = bestMatched.constructorMatch;
            Stack<ConstructorAnalyzer.ConstructorMatch>? fallbacks = bestMatched.fallbackQueue;

            if (constructorMatch == null)
            {
                string message = $"A suitable constructor for type '{type}' could not be located. " +
                    $"Ensure the type is concrete and all parameters of a public constructor are either registered as services or passed as arguments. " +
                    $"Also ensure no extraneous arguments are provided.";

                throw new InvalidOperationException(message);
            }

            while (constructorMatch != null)
            {
                try
                {
                    object result = constructorMatch.Value.CreateInstance(provider);

                    return result;
                }
                catch
                {
                    if (UseFallbacks && fallbacks?.Count != 0)
                        constructorMatch = fallbacks?.Pop();
                    else constructorMatch = null;

                    if (constructorMatch == null)
                        throw;
                }
            }

            throw new InvalidOperationException("SHOULD NEVER HAPPEN");
        }
    }
}
