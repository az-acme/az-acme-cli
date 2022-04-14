using AzAcme.Core.Providers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesOrder : Order
    {
        public CertesOrder(global::Certes.Acme.IOrderContext context)
        {
            Context = context;
        }

        public global::Certes.Acme.IOrderContext Context { get; }

    }
}
