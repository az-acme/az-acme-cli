using AzAcme.Core.Providers.Models;

namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesAcmeOrder : Order
    {
        public CertesAcmeOrder(global::Certes.Acme.IOrderContext context)
        {
            Context = context;
        }

        public global::Certes.Acme.IOrderContext Context { get; }

    }
}
