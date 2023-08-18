namespace Scrutor;

public class DecorationException : MissingTypeRegistrationException
{
    public DecorationException(DecorationStrategy strategy) : base(strategy.ServiceType)
    {
        Strategy = strategy;
    }

    public DecorationStrategy Strategy { get; }
}
