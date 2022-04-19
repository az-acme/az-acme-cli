namespace AzAcme.Core.Exceptions
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException() : base() { }

        public ConfigurationException(string message) : base(message) { }

        public ConfigurationException(string message, Exception exception) : base(message, exception) { }
    }
}
