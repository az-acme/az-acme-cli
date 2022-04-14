using AzAcme.Core.Providers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers
{
    public interface IDnsZone
    {
        Task<Order> SetTxtRecords(Order order);
    }
}
