using Certes;
using Certes.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.CertesAcme
{
  
    /// <summary>
    /// Represents a collection of X509 certificates.
    /// </summary>
    /// <remarks>Adaption from the Certes implementation to not require full chains.</remarks>
    public class RelaxedCertificateStore
    {
        private readonly Dictionary<X509Name, X509Certificate> certificates = new Dictionary<X509Name, X509Certificate>();

        /// <summary>
        /// Adds issuer certificates.
        /// </summary>
        /// <param name="certificates">The issuer certificates.</param>
        public void Add(byte[] certificates)
        {
            var certParser = new X509CertificateParser();
            var issuers = certParser.ReadCertificates(certificates).OfType<X509Certificate>();
            foreach (var cert in issuers)
            {
                this.certificates[cert.SubjectDN] = cert;
            }
        }

        /// <summary>
        /// Gets the issuers of given certificate.
        /// </summary>
        /// <param name="der">The certificate.</param>
        /// <returns>
        /// The issuers of the certificate.
        /// </returns>
        public IList<byte[]> GetIssuers(byte[] der)
        {
            var certParser = new X509CertificateParser();
            var certificate = certParser.ReadCertificate(der);

            var chain = new List<X509Certificate>();
            while (!certificate.SubjectDN.Equivalent(certificate.IssuerDN))
            {
                if (certificates.TryGetValue(certificate.IssuerDN, out var issuer))
                {
                    chain.Add(issuer);
                    certificate = issuer;
                }
                else
                {
                    // just break out, we dont need all issuers.
                    break;
                }
            }

            return chain.Select(cert => cert.GetEncoded()).ToArray();
        }
    }
    
}
