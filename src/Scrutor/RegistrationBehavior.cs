namespace Scrutor
{
    public enum RegistrationBehavior
    {
        /// <summary>
        /// If ServiceType is already registered then this registration is skipped (TryAdd)
        /// </summary>
        SkipIfExists,
        /// <summary>
        /// Always add registration
        /// </summary>
        Append,
        /// <summary>
        /// Replace all other registrations by ServiceType with this one.  Replacement type can be changed by ReplacementStrategy
        /// </summary>
        Replace
    }
}