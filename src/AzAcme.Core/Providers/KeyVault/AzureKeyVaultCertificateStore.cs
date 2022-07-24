using System.Text.RegularExpressions;
using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers.Models;
using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;

namespace AzAcme.Core.Providers.KeyVault
{
    public class AzureKeyVaultCertificateStore : ICertificateStore
    {
        private readonly ILogger logger;
        private readonly CertificateClient client;

        public AzureKeyVaultCertificateStore(ILogger logger, CertificateClient client)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        public Task ValidateCertificateName(string name)
        {
            const string regex = "^[A-Za-z0-9-]+$";

            if (!Regex.IsMatch(name, regex))
            {
                throw new ConfigurationException($"Key Vault Certificate '{name}' invalid. Should be alphanumeric and dashes only.");
            }
            
            return Task.CompletedTask;
        }
        public async Task<CertificateCsr> Prepare(CertificateRequest request)
        {
            const string IssuerName = "Unknown"; // always same for externally managed lifecycles in azure.

            var op = this.GetExistingOperationOrNull(request.Name);

            bool create = false;
            if (op == null)
            {
                create = true;
            }
            else if(op != null && op.Properties != null && false == "inprogress".Equals(op.Properties.Status, StringComparison.InvariantCultureIgnoreCase))
            {
                create = true;
            }

            if (create)
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

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return new CertificateCsr(request.Name, op.Properties.Csr);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
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
