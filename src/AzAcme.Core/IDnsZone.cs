using AzAcme.Core.Providers.Models;

namespace AzAcme.Core
{
    public interface IDnsZone
    {
        Task<Order> SetTxtRecords(Order order);
    }
}
