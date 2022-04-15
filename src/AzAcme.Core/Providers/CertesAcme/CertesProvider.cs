using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers.Models;
using Certes;
using Certes.Acme;
using Certes.Pkcs;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
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

        public async Task<IAcmeCredential> Register(AcmeRegistration registration)
        {
            if(registration.Force || false == await this.registrationSecret.Exists())
            {
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
                if(registration.EabAlgorithm != ExternalAccountBindingAlgorithms.NONE
                    && registration.EabKeyId != null
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


            var timeOut = DateTime.UtcNow.AddMinutes(5);
            while(finalisedOrder.Status != Certes.Acme.Resource.OrderStatus.Valid)
            {
                Console.WriteLine("Waiting...status is " + finalisedOrder.Status);
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

            //var certParser1 = new X509CertificateParser();
            //foreach (var additional in certificates.Certificates)
            //{
            //    var cert1 = certParser1.ReadCertificate(Encoding.UTF8.GetBytes(additional));
            //    certStore.Add(cert1.GetEncoded());
            //}

            var issuers = certStore.GetIssuers(certificateChain.Certificate.ToDer(), requireAllIssuers: false);

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
