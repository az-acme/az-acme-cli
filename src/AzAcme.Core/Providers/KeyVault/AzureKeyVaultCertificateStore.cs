using AzAcme.Core.Providers.Models;
using Azure;
using Azure.Security.KeyVault.Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.KeyVault
{
    public class AzureKeyVaultCertificateStore : ICertificateStore
    {
        private readonly CertificateClient client;

        public AzureKeyVaultCertificateStore(CertificateClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<CertificateMetadata> GetMetadata(CertificateRequest request)
        {
            var info = new CertificateMetadata();
            var existing = this.LoadCertificateProperties(request.Name);

            if (existing != null)
            {
                info.Exists = true;
                info.Expires = existing.ExpiresOn;
            }

            return Task.FromResult(info);
        }

        public async Task<CertificateCsr> Prepare(CertificateRequest request)
        {
            const string IssuerName = "Unknown"; // always same for externally managed lifecycles in azure.

            var op = this.GetExistingOperationOrNull(request.Name);

            if (op == null || op.HasValue == false)
            {
                // create pending operation.
                var sans = new SubjectAlternativeNames();
                foreach (var san in request.SubjectAlternativeNames)
                {
                    sans.DnsNames.Add(san);
                }

                CertificatePolicy policy;

                if (request.SubjectAlternativeNames.Count > 0)
                {
                    policy = new CertificatePolicy(IssuerName, "CN=" + request.Subject, sans);
                }
                else
                {
                    policy = new CertificatePolicy(IssuerName, "CN=" + request.Subject);
                }

                op = await client.StartCreateCertificateAsync(request.Name, policy);
            }

            return new CertificateCsr(request.Name, op.Properties.Csr);
        }

        public async Task Complete(CertificateRequest request, CerticateChain chain)
        {
            await client.MergeCertificateAsync(new MergeCertificateOptions(request.Name, chain.Chain));
        }

        private CertificateOperation? GetExistingOperationOrNull(string name)
        {
            try
            {
                var op = client.GetCertificateOperation(name);
                return op;
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    return null;
                }

                throw;
            }

        }

        private CertificateProperties? LoadCertificateProperties(string certificateName)
        {
            var certs = client.GetPropertiesOfCertificates(includePending: false);

            var cert = certs.FirstOrDefault(x => x.Name == certificateName);

            return cert;
        }
    }
}
