using AzAcme.Core.Extensions;
using AzAcme.Core.Providers;
using AzAcme.Core.Providers.AzureDns;
using AzAcme.Core.Providers.CertesAcme;
using AzAcme.Core.Providers.KeyVault;
using AzAcme.Core.Providers.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.Dns;
using Microsoft.Rest;
using Spectre.Console;

namespace AzAcme.Cli
{
    public class Demo
    {
        public async Task ZeroSsl()
        {
            var kvUri = new Uri("https://kvazacmedev.vault.azure.net/");
            var azureCreds = new DefaultAzureCredential();
            var kvSecretClient = new SecretClient(kvUri, azureCreds);
            var kvCertificateClient = new CertificateClient(kvUri, azureCreds);
            string tenantId = "6e319734-19af-4abf-84d6-df253e46a6f8";
            string dnsResourceId = "/subscriptions/ab3e2754-6d21-462f-935b-6c8880489ea6/resourceGroups/rg-dns/providers/Microsoft.Network/dnszones/azacme.dev";

            // SDK needs the token
            var token = azureCreds.GetToken(new Azure.Core.TokenRequestContext(new[] { $"https://management.azure.com/.default" }, tenantId: tenantId));
            ServiceClientCredentials serviceClientCreds = new TokenCredentials(token.Token);

            var dnsClient = new DnsManagementClient(serviceClientCreds);


            var certesConfiguration = new CertesProviderConfiguration()
            {
                Directory = new Uri("https://acme.zerossl.com/v2/DV90"),
                VerificationAttempts = 10,
                VerificationAttemptWaitSeconds = 5
            };

            
            ISecretStore secretStore = new AzureKeyVaultSecretStore(kvSecretClient);
            IScopedSecret registrationSecret = await secretStore.CreateScopedSecret("reg-zerossl-azacme-dev");
            ICertificateStore certificateStore = new AzureKeyVaultCertificateStore(kvCertificateClient);
            IAcmeDirectory acmeProvider = new CertesProvider(registrationSecret, certesConfiguration);
            IDnsZone azureDns = new AzureDnsZone(dnsClient, dnsResourceId);

            //var credential = await acmeProvider.Register(registration);
            //// or
            var credential = await acmeProvider.Login(); // registered using EAB already.

            // prepare
            var certificateRequest = new CertificateRequest("cert-zerossl-azacme-dev", "zerossl.azacme.dev", new List<string>());
            var metadata = await certificateStore.GetMetadata(certificateRequest);

            if (metadata.NewOrExpiresInDays(30) || true)
            {
                var csr = await certificateStore.Prepare(certificateRequest);
                var order = await acmeProvider.Order(credential, certificateRequest);

                await azureDns.SetTxtRecords(order);

                await acmeProvider.ValidateChallenges(order);

                AnsiConsole.Write(order.ToChallengeTable());

                if (order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated))
                {
                    var chain = await acmeProvider.Finalise(order, csr);
                    await certificateStore.Complete(certificateRequest, chain);
                }

            }
        }

        public async Task LetsEncryptStaging()
        {
            var kvUri = new Uri("https://kvazacmedev.vault.azure.net/");
            var azureCreds = new DefaultAzureCredential();
            var kvSecretClient = new SecretClient(kvUri, azureCreds);
            var kvCertificateClient = new CertificateClient(kvUri, azureCreds);
            string tenantId = "6e319734-19af-4abf-84d6-df253e46a6f8";
            string dnsResourceId = "/subscriptions/ab3e2754-6d21-462f-935b-6c8880489ea6/resourceGroups/rg-dns/providers/Microsoft.Network/dnszones/azacme.dev";

            // SDK needs the token
            var token = azureCreds.GetToken(new Azure.Core.TokenRequestContext(new[] { $"https://management.azure.com/.default" }, tenantId: tenantId));
            ServiceClientCredentials serviceClientCreds = new TokenCredentials(token.Token);

            var dnsClient = new DnsManagementClient(serviceClientCreds);

            var registration = new AcmeRegistration("demo@azacme.dev", true, false);

            var certesConfiguration = new CertesProviderConfiguration()
            {
                Directory = new Uri("https://acme-staging-v02.api.letsencrypt.org/directory"),
                VerificationAttempts = 10,
                VerificationAttemptWaitSeconds = 5
            };


            ISecretStore secretStore = new AzureKeyVaultSecretStore(kvSecretClient);
            IScopedSecret registrationSecret = await secretStore.CreateScopedSecret("reg-le-stg-azacme-dev");
            ICertificateStore certificateStore = new AzureKeyVaultCertificateStore(kvCertificateClient);
            IAcmeDirectory acmeProvider = new CertesProvider(registrationSecret, certesConfiguration);
            IDnsZone azureDns = new AzureDnsZone(dnsClient, dnsResourceId);

            var credential = await acmeProvider.Register(registration);
            //// or
            //context.Credential = await acmeProvider.Login();

            // prepare
            var certificateRequest = new CertificateRequest("cert-lets-encrypt-stg-azacme-dev", "lestg-demo.azacme.dev", new List<string>());
            var metadata = await certificateStore.GetMetadata(certificateRequest);

            if (metadata.NewOrExpiresInDays(30))
            {
                var csr = await certificateStore.Prepare(certificateRequest);
                var order = await acmeProvider.Order(credential, certificateRequest);

                await azureDns.SetTxtRecords(order);

                await acmeProvider.ValidateChallenges(order);

                AnsiConsole.Write(order.ToChallengeTable());

                if (order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated))
                {
                    var chain = await acmeProvider.Finalise(order, csr);
                    await certificateStore.Complete(certificateRequest, chain);
                }

            }
        }
    }
}