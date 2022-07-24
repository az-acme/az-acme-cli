using System.Text.RegularExpressions;
using AzAcme.Core.Exceptions;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

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

        public Task ValidateSecretName(string name)
        {
            const string regex = "^[A-Za-z0-9-]+$";

            if (!Regex.IsMatch(name, regex))
            {
                throw new ConfigurationException($"Key Vault Certificate '{name}' invalid. Should be alphanumeric and dashes only.");
            }
            
            return Task.CompletedTask;
        }
        
        public async Task<IScopedSecret> CreateScopedSecret(string name)
        {
            // ensure validation has occured.
            await ValidateSecretName(name);
            
            IScopedSecret ss = new AzureKeyVaultScopedSecret(this.client, name);

            return ss;
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
