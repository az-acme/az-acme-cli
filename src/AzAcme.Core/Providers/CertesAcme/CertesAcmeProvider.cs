using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers.Models;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesAcmeProvider : IAcmeDirectory
    {
     
        private readonly CertesAcmeConfiguration configuration;
        private readonly ILogger logger;
        private readonly IScopedSecret registrationSecret;

        public CertesAcmeProvider(ILogger logger, IScopedSecret registrationSecret, CertesAcmeConfiguration configuration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.registrationSecret = registrationSecret ?? throw new ArgumentNullException(nameof(registrationSecret));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IAcmeCredential> Register(AcmeRegistration registration)
        {
            if(registration.Force || false == await this.registrationSecret.Exists())
            {
                this.logger.LogInformation("Registering with provider...");

                if (string.IsNullOrEmpty(registration.Email))
                {
                    throw new ConfigurationException("Registration Email Address must be set for registration");
                }

                if (!registration.AcceptTermsOfService)
                {
                    throw new ConfigurationException("Terms of service must be accepted before registration");
                }

                var context = new AcmeContext(configuration.Directory);

                // use EAB if we need to.
                if(registration.EabKeyId != null
                    && registration.EabKey != null)
                {
                    _ = await context.NewAccount(registration.Email, termsOfServiceAgreed: true, registration.EabKeyId, registration.EabKey, registration.EabAlgorithm.ToString());
                }
                else
                {
                    _ = await context.NewAccount(registration.Email, termsOfServiceAgreed: true);
                }
                
                var credential = context.AccountKey.ToPem();

                await this.registrationSecret.CreateOrUpdate(credential);
            }
            else
            {
                this.logger.LogInformation("Alredy registered. Use '--force-registration' or remove the account secret from Key Vault to re-register.");
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

            return new CertesAcmeCredential(registration);
        }

        public async Task<Order> Order(IAcmeCredential credential, CertificateRequest certificateRequest)
        {
            var creds = credential as CertesAcmeCredential;

            if(creds == null)
            {
                throw new ArgumentException($"Expecting to be of type {typeof(CertesAcmeCredential).Name} but was {credential.GetType().Name}", nameof(credential));
            }

            var pem = KeyFactory.FromPem(creds.Pem);
            var acmeContext = new AcmeContext(this.configuration.Directory,pem);

            var all = new List<string>();
            all.Add(certificateRequest.Subject);
            all.AddRange(certificateRequest.SubjectAlternativeNames);

            var newOrder = await acmeContext.NewOrder(all);

            var order = new CertesAcmeOrder(newOrder);

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
            var certesOrder = order as CertesAcmeOrder;

            if(certesOrder == null)
            {
                throw new ArgumentException($"Expecing Order to be of type '{typeof(CertesAcmeOrder).Name}' but was '{order.GetType().Name}'");
            }

            foreach (var challenge in certesOrder.Challenges)
            {
                // only need to do anything if challenge is pending.
                if (challenge.Status == DnsChallenge.DnsChallengeStatus.Pending)
                {
                    var auth = await certesOrder.Context.Authorization(challenge.Identitifer);
                    await (await auth.Dns()).Validate();

                    var res = await auth.Resource();

                    if (res.Status == Certes.Acme.Resource.AuthorizationStatus.Valid)
                    {
                        challenge.SetStatus(DnsChallenge.DnsChallengeStatus.Validated);
                    }
                    else if (res.Status == Certes.Acme.Resource.AuthorizationStatus.Invalid)
                    {
                        challenge.SetStatus(DnsChallenge.DnsChallengeStatus.Failed);
                    }
                }                
            }

            return order;
        }

        public async Task<CerticateChain> Finalise(Order order, CertificateCsr csr)
        {
            var certesOrder = order as CertesAcmeOrder;

            if (certesOrder == null)
            {
                throw new ArgumentException($"Expecing Order to be of type '{typeof(CertesAcmeOrder).Name}' but was '{order.GetType().Name}'");
            }

            var finalisedOrder = await certesOrder.Context.Finalize(csr.Csr);


            var timeOut = DateTime.UtcNow.AddMinutes(5);
            while(finalisedOrder.Status != Certes.Acme.Resource.OrderStatus.Valid)
            {
                this.logger.LogDebug("Waiting for order to be status '{0}'. Current status is '{1}'.", Certes.Acme.Resource.OrderStatus.Valid, finalisedOrder.Status);
                if (DateTime.UtcNow > timeOut)
                    break;

                await Task.Delay(5000);
                // update status.
                finalisedOrder = await certesOrder.Context.Resource();
            }

            if(finalisedOrder.Status != Certes.Acme.Resource.OrderStatus.Valid)
            {
                throw new NotSupportedException($"Expecting ACME Order to be Finalised, but is still in status '{finalisedOrder.Status}'");
            }

            var certChain = await certesOrder.Context.Download();

            var chain = new CerticateChain();

            var certToPem = ConvertToPem(certChain);

            chain.Chain.Add(System.Text.Encoding.UTF8.GetBytes(certToPem));
            foreach (var issuer in certChain.Issuers)
            {
                chain.Chain.Add(System.Text.Encoding.UTF8.GetBytes(issuer.ToPem()));
            }

            return chain;
        }

        /// <summary>
        /// Encodes the full certificate chain in PEM.
        /// </summary>
        /// <param name="certificateChain">The certificate chain.</param>
        /// <param name="certKey">The certificate key.</param>
        /// <returns>The encoded certificate chain.</returns>
        private static string ConvertToPem(CertificateChain certificateChain)
        {
            var certStore = new RelaxedCertificateStore();
            
            foreach (var issuer in certificateChain.Issuers)
            {
                certStore.Add(issuer.ToDer());
            }

            var issuers = certStore.GetIssuers(certificateChain.Certificate.ToDer());

            using (var writer = new StringWriter())
            {
                //if (certKey != null)
                //{
                //    writer.WriteLine(certKey.ToPem().TrimEnd());
                //}

                writer.WriteLine(certificateChain.Certificate.ToPem().TrimEnd());

                var certParser = new X509CertificateParser();
                var pemWriter = new PemWriter(writer);
                foreach (var issuer in issuers)
                {
                    var cert = certParser.ReadCertificate(issuer);
                    pemWriter.WriteObject(cert);
                }

                return writer.ToString();
            }
        }
    }
}
