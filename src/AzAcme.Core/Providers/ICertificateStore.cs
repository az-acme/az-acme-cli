using AzAcme.Core.Providers.KeyVault;
using AzAcme.Core.Providers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers
{


    public interface ICertificateStore
    {
        Task<CertificateMetadata> GetMetadata(CertificateRequest request);

        Task<CertificateCsr> Prepare(CertificateRequest request);

        Task Complete(CertificateRequest request, CerticateChain chain);
    }
}
