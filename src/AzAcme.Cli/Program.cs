using AzAcme.Cli;
using AzAcme.Cli.Commands;
using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using AzAcme.Core;
using AzAcme.Core.Providers.CertesAcme;
using AzAcme.Core.Providers.KeyVault;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;

namespace AzAcmi
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var console = AnsiConsole.Console;
            var logger = new AnsiConsoleLogger(args.Contains("--verbose"));

            var parser = new CommandLine.Parser(with => with.HelpWriter = null)
                                .ParseArguments<OrderOptions, RegistrationOptions>(args);

            return await parser.MapResult(
                (RegistrationOptions opts) => 
                {
                    var cmd = BuildRegistrationCommand(logger, opts).Result;
                    return cmd.Execute(opts);
                },
                (OrderOptions opts) =>
                {
                    var cmd = BuildOrderCommand(logger, opts).Result;
                    return cmd.Execute(opts);
                },
                errs => Task.FromResult(DisplayHelp(parser)));
        }

        static async Task<OrderCommand> BuildOrderCommand(ILogger logger, OrderOptions options)
        {
            return await AnsiConsole.Status().StartAsync<OrderCommand>("Validating credentials and dependencies", async ctx =>
            {
                logger.LogDebug("Loading Azure Credentials from environment...");
                ctx.Status("Loading Azure Credentials...");

                // Azure Credentials.
                var azureCreds = CreateDefaultAzureCredentials();

                ctx.Status("Creating Key Vault Clients...");

                // Key Vault Related
                logger.LogDebug("Creating Azure Key Vault secret client...");
                var kvSecretClient = new SecretClient(options.KeyVaultUri, azureCreds);
                ISecretStore secretStore = new AzureKeyVaultSecretStore(logger, kvSecretClient);
                IScopedSecret registrationSecret = await secretStore.CreateScopedSecret(options.AccountSecretName);

                // Key Vault Certificates
                logger.LogDebug("Creating Azure Key Vault certificate client...");
                var kvCertificateClient = new CertificateClient(options.KeyVaultUri, azureCreds);
                ICertificateStore certificateStore = new AzureKeyVaultCertificateStore(logger, kvCertificateClient);

                // ACME Directory
                logger.LogDebug("Creating ACME provider instance...");
                var certesConfiguration = new CertesAcmeConfiguration(options.Server);
                IAcmeDirectory acmeProvider = new CertesAcmeProvider(logger, registrationSecret, certesConfiguration);

                ctx.Status("Creating Azure DNS Client...");

                // create using factory for future and to hide lazy logic.
                var dns = DnsFactory.Create(logger,
                    new DnsFactory.DnsOptions
                    {
                        Provider = DnsProviders.AzureDns,
                        AadTenantId = options.AadTenantId,
                        AzureCredential = azureCreds,
                        AzureDnsResourceId = options.DnsZoneResourceId,
                        ZoneOverride = options.Zone
                    }
                );

                // command
                var rc = new OrderCommand(logger, certificateStore, acmeProvider, dns);

                return rc;
            });
            
        }

        static async Task<RegistrationCommand> BuildRegistrationCommand(ILogger logger, RegistrationOptions options)
        {
            // Azure Credentials.
            var azureCreds = CreateDefaultAzureCredentials();

            // Key Vault Related
            var kvSecretClient = new SecretClient(options.KeyVaultUri, azureCreds);
            ISecretStore secretStore = new AzureKeyVaultSecretStore(logger, kvSecretClient);
            IScopedSecret registrationSecret = await secretStore.CreateScopedSecret(options.AccountSecretName);
            
            // directory
            var certesConfiguration = new CertesAcmeConfiguration(options.Server);
            IAcmeDirectory acmeProvider = new CertesAcmeProvider(logger, registrationSecret, certesConfiguration);

            // command
            var rc = new RegistrationCommand(logger, acmeProvider);

            return rc;
        }

        static DefaultAzureCredential CreateDefaultAzureCredentials()
        {
            var authOptions = new DefaultAzureCredentialOptions()
            {
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true
            };
            var azureCreds = new DefaultAzureCredential(authOptions);

            return azureCreds;
        }

        static string Banner()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"     ___      ________          ___       ______ .___  ___.  _______     ");
            sb.AppendLine(@"    /   \    |       /         /   \     /      ||   \/   | |   ____|    ");
            sb.AppendLine(@"   /  ^  \   `---/  /         /  ^  \   |  ,----'|  \  /  | |  |__       ");
            sb.AppendLine(@"  /  /_\  \     /  /         /  /_\  \  |  |     |  |\/|  | |   __|      ");
            sb.AppendLine(@" /  _____  \   /  /----.    /  _____  \ |  `----.|  |  |  | |  |____     ");
            sb.AppendLine(@"/__/     \__\ /________|   /__/     \__\ \______||__|  |__| |_______|    ");
            sb.AppendLine();
            return sb.ToString();
        }

        static int DisplayHelp(ParserResult<object> parserResult)
        {
            AnsiConsole.MarkupLine(HelpText.AutoBuild(parserResult, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "[blue]" + Banner() + "[/]";
                h.Copyright = "Copyright (c) " + DateTime.UtcNow.Year + " AZ ACME Authors";
                return h;
            }));
            return 1;
        }
    }
}