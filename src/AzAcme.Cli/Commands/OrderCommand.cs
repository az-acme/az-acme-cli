using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using AzAcme.Core;
using AzAcme.Core.Extensions;
using AzAcme.Core.Providers.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Certes;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.X509;
using Spectre.Console;
using System.Diagnostics;
using System.Text;

namespace AzAcme.Cli.Commands
{
    public class OrderCommand : Command<OrderOptions>
    {
        private readonly ICertificateStore certificateStore;
        private readonly IAcmeDirectory acmeDirectory;
        private readonly IDnsZone dnsZone;

        public OrderCommand(ILogger logger,
            ICertificateStore certificateStore,
            IAcmeDirectory acmeDirectory,
            IDnsZone dnsZone) : base(logger)
        {
            this.certificateStore = certificateStore ?? throw new ArgumentNullException(nameof(certificateStore));
            this.acmeDirectory = acmeDirectory ?? throw new ArgumentNullException(nameof(acmeDirectory));
            this.dnsZone = dnsZone ?? throw new ArgumentNullException(nameof(dnsZone));
        }

        protected override async Task<int> OnExecute(OrderOptions opts)
        {
            var certificateRequest = new CertificateRequest(opts.Certificate, opts.Subject, opts.SubjectAlternativeNames);

            this.logger.LogInformation("Loading metadata from certificate store for certificate '{0}'.", opts.Certificate);

            var metadata = await certificateStore.GetMetadata(certificateRequest);

            if (!metadata.Exists)
            {
                logger.LogInformation("Certificate '{0}' does not exist in the certificate store.", opts.Certificate);
            }
            
            if (opts.ForceOrder)
            {
                this.logger.LogInformation("Force order / renewal has been requested.");
            }

            // check if an order is required.
            bool requiresOrder = false == metadata.Exists
                                || metadata.Expires.HasValue && DateTime.UtcNow.AddDays(opts.RenewWithinDays) > metadata.Expires
                                || true == opts.ForceOrder;

            AnsiConsole.Write(certificateRequest.ToTable(metadata, requiresOrder));

            if (!requiresOrder)
            {
                AnsiConsole.MarkupLine("[green]:check_mark: Certificate does not require ordering.[/]");
                return 0;
            }

            this.logger.LogInformation("Obtaining credentials to ACME Provider from Azure Key Vault.");
            var credential = await acmeDirectory.Login();

            this.logger.LogInformation("Initiating order to ACME provider.");
            var order = await acmeDirectory.Order(credential, certificateRequest);
            
            this.logger.LogInformation("Updating DNS challenge records.");
            await dnsZone.SetTxtRecords(order);

            var attempts = 5;
            var delaySeconds = 5;

            this.logger.LogInformation("Waiting for DNS records to be verified.");

            // Wait while showing live update table.
            await order.WaitForVerificationWithTable(acmeDirectory, attempts, delaySeconds);

            var validated = order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated);

            if (validated)
            {
                this.logger.LogInformation("DNS challenges successfully validated.");

                this.logger.LogInformation("Preparing Certificate CSR in Azure Key Vault.");
                var csr = await certificateStore.Prepare(certificateRequest);

                this.logger.LogInformation("Finalising the order with ACME provider using generated CSR.");
                var chain = await acmeDirectory.Finalise(order, csr);

                this.logger.LogInformation("Completing the merge of CSR with ACME generated certificate chain.");
                await certificateStore.Complete(certificateRequest, chain);

                AnsiConsole.MarkupLine("[green]:check_mark: Certificate successfully ordered/renewed.[/]");
                return 0;
                
            }
            else
            {
                AnsiConsole.MarkupLine("[red]:cross_mark: DNS challenge verification failed.[/]");
                return 1;
            }

            
        }

    }
}
