using AzAcme.Core.Providers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers
{
    public interface IAcmeDirectory
    {
        Task<IAcmeCredential> Register(AcmeRegistration registration);
        Task<IAcmeCredential> Login();

        Task<Order> ValidateChallenges(Order order);

        Task<Order> Order(IAcmeCredential credential, CertificateRequest certificateRequest);

        Task<CerticateChain> Finalise(Order order, CertificateCsr csr);
    }
}
