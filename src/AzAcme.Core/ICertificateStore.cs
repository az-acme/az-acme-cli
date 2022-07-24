﻿using AzAcme.Core.Providers.Models;

namespace AzAcme.Core
{
    public interface ICertificateStore
    {
        Task ValidateCertificateName(string name);
        
        Task<CertificateMetadata> GetMetadata(CertificateRequest request);

        Task<CertificateCsr> Prepare(CertificateRequest request);

        Task Complete(CertificateRequest request, CerticateChain chain);
    }
}
