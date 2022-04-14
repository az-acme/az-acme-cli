using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.Models
{
    

    public abstract class Order
    {
        public IList<DnsChallenge> Challenges { get; } = new List<DnsChallenge>();
    }
}
