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
    public class RelaxedCertificateStore
    {
        private readonly Dictionary<X509Name, X509Certificate> certificates = new Dictionary<X509Name, X509Certificate>();

        private readonly Lazy<Dictionary<X509Name, X509Certificate>> embeddedCertificates = new Lazy<Dictionary<X509Name, X509Certificate>>(() =>
        {
            var certParser = new X509CertificateParser();
            var assembly = typeof(PfxBuilder).GetTypeInfo().Assembly;
            return assembly
                .GetManifestResourceNames()
                .Where(n => n.EndsWith(".pem"))
                .Select(n =>
                {
                    using (var stream = assembly.GetManifestResourceStream(n))
                    {
                        return certParser.ReadCertificate(stream);
                    }
                })
                .ToDictionary(c => c.SubjectDN, c => c);
        }, true);

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
        /// <param name="requireAllIssuers">If true, don't attempt to resolve all issuers from cert store.</param>
        /// <returns>
        /// The issuers of the certificate.
        /// </returns>
        public IList<byte[]> GetIssuers(byte[] der, bool requireAllIssuers = false)
        {
            var certParser = new X509CertificateParser();
            var certificate = certParser.ReadCertificate(der);

            var chain = new List<X509Certificate>();
            while (!certificate.SubjectDN.Equivalent(certificate.IssuerDN))
            {
                if (certificates.TryGetValue(certificate.IssuerDN, out var issuer) ||
                    embeddedCertificates.Value.TryGetValue(certificate.IssuerDN, out issuer))
                {
                    chain.Add(issuer);
                    certificate = issuer;
                }
                else
                {
                    if (requireAllIssuers)
                    {
                        throw new AcmeException(
                            string.Format("Issuer not found '{0}' for '{1}'", certificate.IssuerDN, certificate.SubjectDN));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return chain.Select(cert => cert.GetEncoded()).ToArray();
        }
    }
    
}
