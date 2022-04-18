using AzAcme.Core.Providers.Models;

namespace AzAcme.Core
{
    public interface ICertificateStore
    {
        Task<CertificateMetadata> GetMetadata(CertificateRequest request);

        Task<CertificateCsr> Prepare(CertificateRequest request);

        Task Complete(CertificateRequest request, CerticateChain chain);
    }
}
