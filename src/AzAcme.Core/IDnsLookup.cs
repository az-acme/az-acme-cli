using AzAcme.Core.Providers.Models;

namespace AzAcme.Core
{
    public interface IDnsLookup
    {
        Task<bool> ValidateTxtRecords(Order order);
    }
}
