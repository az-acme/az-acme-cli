using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.KeyVault
{
    public class AzureKeyVaultSecretStore : ISecretStore
    {
        private readonly ILogger logger;
        private readonly SecretClient client;

        public AzureKeyVaultSecretStore(ILogger logger, SecretClient client)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<IScopedSecret> CreateScopedSecret(string name)
        {
            IScopedSecret ss = new AzureKeyVaultScopedSecret(this.client, name);

            return Task.FromResult(ss);
        }

        internal class AzureKeyVaultScopedSecret : IScopedSecret
        {
            private readonly SecretClient client;
            private readonly string secretName;

            public AzureKeyVaultScopedSecret(SecretClient client, string secretName)
            {
                this.client = client;
                this.secretName = secretName;
            }

            public async Task CreateOrUpdate(string value)
            {
                await this.client.SetSecretAsync(this.secretName, value);
            }

            public Task<bool> Exists()
            {
                var secs = client.GetPropertiesOfSecrets();

                var exist = secs.Any(x => x.Name == secretName);

                return Task.FromResult(exist);
            }

            public async Task<string> GetSecret()
            {
                var item = await this.client.GetSecretAsync(this.secretName);

                return item.Value.Value;
            }
        }

    }
}
