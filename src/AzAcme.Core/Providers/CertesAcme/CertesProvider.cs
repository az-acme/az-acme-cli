using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers.Models;
using Certes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesProvider : IAcmeDirectory
    {
     
        private readonly CertesProviderConfiguration configuration;

        private readonly IScopedSecret registrationSecret;

        public CertesProvider(IScopedSecret registrationSecret, CertesProviderConfiguration configuration)
        {
            this.registrationSecret = registrationSecret ?? throw new ArgumentNullException(nameof(registrationSecret));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IAcmeCredential> Register()
        {
            if(configuration.ForceRegistration || false == await this.registrationSecret.Exists())
            {
                if (string.IsNullOrEmpty(configuration.RegistrationEmailAddress))
                {
                    throw new ConfigurationException("Registration Email Address must be set for registration");
                }

                if (!configuration.AgreedTermsOfService)
                {
                    throw new ConfigurationException("Terms of service must be accepted before registration");
                }

                var context = new AcmeContext(configuration.Directory);
                _ = await context.NewAccount(configuration.RegistrationEmailAddress, termsOfServiceAgreed: true);
                var credential = context.AccountKey.ToPem();

                await this.registrationSecret.CreateOrUpdate(credential);
            }

            return await Login();
        }

        public async Task<IAcmeCredential> Login()
        {
            if (!await this.registrationSecret.Exists())
            {
                throw new ConfigurationException("Unable to login. Registion not found.");
            }

            var registration = await this.registrationSecret.GetSecret();

            return new CertesAcmeCredential(this.configuration.Directory, registration);
        }

        public async Task<Order> Order(IAcmeCredential credential, CertificateRequest certificateRequest)
        {
            var creds = credential as CertesAcmeCredential;
            var pem = KeyFactory.FromPem(creds.Pem);
            var acmeContext = new AcmeContext(creds.Directory,pem);

            var all = new List<string>();
            all.Add(certificateRequest.Subject);
            all.AddRange(certificateRequest.SubjectAlternativeNames);

            var newOrder = await acmeContext.NewOrder(all);

            var order = new CertesOrder(newOrder);

            foreach (var identifier in all)
            {
                var auth = await newOrder.Authorization(identifier);
                var dns = await auth.Dns();
                var dnsTxt = acmeContext.AccountKey.DnsTxt(dns.Token);

                order.Challenges.Add(new DnsChallenge(identifier, dnsTxt));
            }

            return order;
        }

        public async Task<Order> ValidateChallenges(Order order)
        {
            var certesOrder = order as CertesOrder;

            if(certesOrder == null)
            {
                throw new ArgumentException($"Expecing Order to be of type '{typeof(CertesOrder).Name}' but was '{order.GetType().Name}'");
            }

            foreach (var challenge in certesOrder.Challenges)
            {
                //this.logger.LogWithColor(LogLevel.Information, "Waiting for ownership verification of '{0}'...", action.Identifier);

                var auth = await certesOrder.Context.Authorization(challenge.Identitifer);
                await (await auth.Dns()).Validate();
                int attempt = 1;
                while (attempt <= configuration.VerificationAttempts && challenge.Status == DnsChallenge.DnsChallengeStatus.Pending)
                {
                    //ctx.Status($"Waiting for DNS TXT verification for '{action.Identifier}'. Attmept {attempt}/{attempts}...");

                    var res = await auth.Resource();

                    if (res.Status == Certes.Acme.Resource.AuthorizationStatus.Valid)
                    {
                        //this.logger.LogWithColor(LogLevel.Information, "TXT Verified for '{0}' :check_mark:", action.Identifier);
                        challenge.SetStatus(DnsChallenge.DnsChallengeStatus.Validated);
                    }
                    else if (res.Status == Certes.Acme.Resource.AuthorizationStatus.Invalid)
                    {
                        challenge.SetStatus(DnsChallenge.DnsChallengeStatus.Failed);
                    }
                    

//#pragma warning disable CS8604 // Possible null reference argument.
//                    this.logger.LogWithColor(LogLevel.Information, "Status is '{0}'. Waiting {1} seconds before checking again...", res.Status, delaySeconds);
//#pragma warning restore CS8604 // Possible null reference argument.

                    
                    if (challenge.Status == DnsChallenge.DnsChallengeStatus.Pending)
                    {
                        attempt++;
                        await Task.Delay(configuration.VerificationAttemptWaitSeconds * 1000);
                    }
                }
            }

            return order;
        }

        public async Task<CerticateChain> Finalise(Order order, CertificateCsr csr)
        {
            var certesOrder = order as CertesOrder;

            if (certesOrder == null)
            {
                throw new ArgumentException($"Expecing Order to be of type '{typeof(CertesOrder).Name}' but was '{order.GetType().Name}'");
            }

            var finalisedOrder = await certesOrder.Context.Finalize(csr.Csr);

            if(finalisedOrder.Status != Certes.Acme.Resource.OrderStatus.Valid)
            {
                throw new NotSupportedException($"Expecting ACME Order to be Finalised, but is still in status '{finalisedOrder.Status}'");
            }

            var certChain = await certesOrder.Context.Download();

            var chain = new CerticateChain();

            chain.Chain.Add(System.Text.Encoding.UTF8.GetBytes(certChain.ToPem()));
            foreach (var issuer in certChain.Issuers)
            {
                chain.Chain.Add(System.Text.Encoding.UTF8.GetBytes(issuer.ToPem()));
            }

            return chain;

        }
    }
}
