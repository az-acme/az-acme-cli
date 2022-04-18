//using AzAcme.Core;
//using AzAcme.Core.Extensions;
//using AzAcme.Core.Providers.Models;
//using Spectre.Console;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace AzAcme.Cli
//{
//    internal class Runner
//    {
//        private readonly ISecretStore secretStore;
//        private readonly ICertificateStore certificateStore;
//        private readonly IAcmeDirectory acmeDirectory;
//        private readonly IDnsZone dnsZone;

//        public Runner(ISecretStore secretStore,
//            ICertificateStore certificateStore,
//            IAcmeDirectory acmeDirectory,
//            IDnsZone dnsZone)
//        {
//            this.secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
//            this.certificateStore = certificateStore ?? throw new ArgumentNullException(nameof(certificateStore));
//            this.acmeDirectory = acmeDirectory ?? throw new ArgumentNullException(nameof(acmeDirectory));
//            this.dnsZone = dnsZone ?? throw new ArgumentNullException(nameof(dnsZone));
//        }

//        internal async Task Order(IAcmeCredential credential, CertificateRequest certificateRequest)
//        {
//            var metadata = await certificateStore.GetMetadata(certificateRequest);

//            // check if an order is required.
//            bool requiresOrder = false == metadata.Exists
//                                || metadata.Expires.HasValue && DateTime.UtcNow.AddDays(30) > metadata.Expires;
            
//            AnsiConsole.Write(certificateRequest.ToTable(metadata, requiresOrder)); 

//            if (requiresOrder)
//            {
//                var csr = await certificateStore.Prepare(certificateRequest);
//                var order = await acmeDirectory.Order(credential, certificateRequest);

//                await dnsZone.SetTxtRecords(order);

//                var attempts = 5;
//                var delaySeconds = 5;

//                // Wait while showing live update table.
//                await order.WaitForVerificationWithTable(acmeDirectory, attempts, delaySeconds);

//                var validated = order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated);

//                if (validated)
//                {
//                    AnsiConsole.MarkupLine("[green]:check_mark:[/] DNS challenges successfully validated.");
//                    if (order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated))
//                    {
//                        var chain = await acmeDirectory.Finalise(order, csr);
//                        await certificateStore.Complete(certificateRequest, chain);
//                        AnsiConsole.MarkupLine("[green]:check_mark:[/] Certificate successfully ordered/renewed.");
//                    }
//                }
//                else
//                {
//                    AnsiConsole.MarkupLine("[red]:cross_mark:[/] DNS challenge verification failed.");
//                }

//            }
//        }
//    }
//}
