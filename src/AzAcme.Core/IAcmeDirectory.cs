using AzAcme.Core.Providers.Models;

namespace AzAcme.Core
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
