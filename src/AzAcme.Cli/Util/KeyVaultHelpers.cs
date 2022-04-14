using Azure;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Cli.Util
{
    internal static class KeyVaultHelpers
    {
        public static CertificateProperties? LoadCertificateProperties(CertificateClient client, string certificateName)
        {
            var certs = client.GetPropertiesOfCertificates(includePending: false);

            var cert = certs.FirstOrDefault(x => x.Name == certificateName);

            return cert;
        }

        public static CertificateOperation? GetExistingOperationOrNull(CertificateClient client, string name)
        {
            try
            {
                var op = client.GetCertificateOperation(name);
                return op;
            }
            catch(RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    return null;
                }

                throw;
            }

        }

        public static bool SecretExists(SecretClient client, string secretName)
        {
            var secs = client.GetPropertiesOfSecrets();

            var exist = secs.Any(x => x.Name == secretName);

            return exist;
        }
    }
}
