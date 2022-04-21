using AzAcme.Core.Providers.Models;

namespace AzAcme.Core.Providers
{
    public class LazyDnsZone : IDnsZone
    {
        private readonly Lazy<IDnsZone> dnsZone;

        public LazyDnsZone(Lazy<IDnsZone> dnsZone)
        {
            this.dnsZone = dnsZone ?? throw new ArgumentNullException(nameof(dnsZone));
        }

        public Task<Order> RemoveTxtRecords(Order order)
        {
            return dnsZone.Value.RemoveTxtRecords(order);
        }

        public Task<Order> SetTxtRecords(Order order)
        {
            return dnsZone.Value.SetTxtRecords(order);
        }
    }
}
