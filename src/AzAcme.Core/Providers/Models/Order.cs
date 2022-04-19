namespace AzAcme.Core.Providers.Models
{
    public abstract class Order
    {
        public IList<DnsChallenge> Challenges { get; } = new List<DnsChallenge>();
    }
}
