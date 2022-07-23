namespace AzAcme.Core.Exceptions;

public class ProviderException : Exception
{
    public ProviderException() : base() { }

    public ProviderException(string message) : base(message) { }

    public ProviderException(string message, Exception exception) : base(message, exception) { }
}