﻿using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using AzAcme.Core;
using AzAcme.Core.Exceptions;
using AzAcme.Core.Extensions;
using AzAcme.Core.Providers.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AzAcme.Cli.Commands
{
    public class OrderCommand : Command<OrderOptions>
    {
        private readonly ICertificateStore certificateStore;
        private readonly IAcmeDirectory acmeDirectory;
        private readonly IDnsZone dnsZone;

        public OrderCommand(ILogger logger,
            EnvironmentVariableResolver environmentVariableResolver,
            ICertificateStore certificateStore,
            IAcmeDirectory acmeDirectory,
            IDnsZone dnsZone) : base(logger, environmentVariableResolver)
        {
            this.certificateStore = certificateStore ?? throw new ArgumentNullException(nameof(certificateStore));
            this.acmeDirectory = acmeDirectory ?? throw new ArgumentNullException(nameof(acmeDirectory));
            this.dnsZone = dnsZone ?? throw new ArgumentNullException(nameof(dnsZone));
        }

        protected override async Task<int> OnExecute(OrderOptions opts)
        {
            // check the name is valid first.
            await certificateStore.ValidateCertificateName(opts.Certificate);
            
            Console.WriteLine(opts.Certificate);
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
            
            var delaySeconds = 5; // time in the loop
            var attempts = opts.VerificationTimeoutSeconds / delaySeconds;
            
            // work out total attempts. If needed add extra attempt so we wait
            // at least the verification timeout duration.
            if ((attempts * delaySeconds) < opts.VerificationTimeoutSeconds)
            {
                attempts++;
            }
            
            // total time that will be waited.
            var totalTime = attempts * delaySeconds;
            
            this.logger.LogInformation($"Waiting for DNS records to be verified (up to {totalTime} seconds).");

            try
            {
                // Wait while showing live update table.
                await this.WaitForVerificationWithTable(order, acmeDirectory, attempts, delaySeconds, opts);

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

                    this.logger.LogInformation("Certificate successfully ordered and merged into Azure Key Vault.");

                    AnsiConsole.MarkupLine("[green]Successfully completed order process.[/]");
                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]DNS challenge verification failed.[/]");
                    return 1;
                }
            }
            finally
            {
                try
                {
                    this.logger.LogInformation("Cleaning up DNZ Zone Records.");
                    await dnsZone.RemoveTxtRecords(order);
                }
                catch(Exception ex)
                {
                    this.logger.LogError(ex,"Error cleaning up DNZ Zone Records. Ignoring.");
                }
                
            }
        }

        private async Task WaitForVerificationWithTable(Order order, IAcmeDirectory directory, int attempts, int delaySeconds, OrderOptions opts)
        {
            if (opts.DisableLiveTable)
            {
                // Workaround for Azure DevOps pipeline. AnsiConsole.Live throws an exception. 
                this.logger.LogDebug("LiveTable is disabled.");

                var table = order.ToTable();
                AnsiConsole.Write(table);
                int attempt = 1;
                while (attempt <= attempts)
                {
                    await directory.ValidateChallenges(order);
                    table.Rows.Clear();
                    foreach (var item in order.Challenges)
                    {
                        table.AddRow(item.Identitifer, item.TxtRecord ?? "-", item.TxtValue, item.Status.ToString());
                    }
                    AnsiConsole.Write(table);

                    if (order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated
                        || order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Failed)))
                    {
                        break;
                    }
                    await Task.Delay(delaySeconds * 1000);
                    attempt++;
                }
            }
            else
            {
                var table = order.ToTable();
                await AnsiConsole.Live(table)
                .StartAsync(async ctx =>
                {
                    ctx.Refresh();

                    int attempt = 1;
                    while (attempt <= attempts)
                    {
                        await directory.ValidateChallenges(order);

                        table.Rows.Clear();
                        foreach (var item in order.Challenges)
                        {
                            table.AddRow(item.Identitifer, item.TxtRecord ?? "-", item.TxtValue, item.Status.ToString());
                        }
                        ctx.Refresh();

                        if (order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated
                            || order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Failed)))
                        {
                            break;
                        }

                        await Task.Delay(delaySeconds * 1000);
                        attempt++;
                    }
                });
            }
        }
    }
}
