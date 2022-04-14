using AzAcme.Cli.Util;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Certes;
using Microsoft.Azure.Management.Dns;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Spectre.Console;

namespace AzAcme.Cli.Commands
{
    public abstract class Command<T> where T : Options.Options
    {
        protected readonly ILogger logger;

        protected Command(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected async Task<AcmeContext> CreateContext(Uri acmeServer, SecretClient client, string secretName)
        {
            var accSecret = await client.GetSecretAsync(secretName);

            this.logger.LogInformation("Parsing ACME account key from secret...");

            var pem = KeyFactory.FromPem(accSecret.Value.Value);

            var context = new AcmeContext(acmeServer,pem);

            return context;

        }

        protected abstract Task<int> OnExecute(StatusContext ctx, T opts);

        public async Task<int> Execute(T opts)
        {
            return await AnsiConsole.Status()
                .StartAsync<int>($"Processing...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Default);

                    return await OnExecute(ctx, opts);
                });
        }

        protected DnsManagementClient CreateDnsManagementClient(string? tenantId = null)
        {
            this.logger.LogDebug("Creating DNS Management Client...");

            var deafultClient = new DefaultAzureCredential();
            
            var token = deafultClient.GetToken(new Azure.Core.TokenRequestContext(new[] { $"https://management.azure.com/.default" }, tenantId: tenantId));
            ServiceClientCredentials serviceClientCreds = new TokenCredentials(token.Token);
            var dnsClient = new DnsManagementClient(serviceClientCreds);

            return dnsClient;
        }

        protected CertificateClient CreateCertificateClient(Uri keyVaultUri)
        {
            var client = new CertificateClient(keyVaultUri, new DefaultAzureCredential());

            return client;
        }

        protected SecretClient CreateSecretClient(Uri keyVaultUri)
        {
            var client = new SecretClient(keyVaultUri, new DefaultAzureCredential());

            return client;
        }

    }
}
