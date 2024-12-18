using AzAcme.Cli;
using AzAcme.Cli.Commands;
using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using AzAcme.Core;
using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers.CertesAcme;
using AzAcme.Core.Providers.KeyVault;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Text;
using AzAcme.Core.Providers;
using DnsClient;

namespace AzAcmi
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var console = AnsiConsole.Console;
            var verbose = args.Contains("--verbose");
            var logger = new AnsiConsoleLogger(verbose);

            var parser = new CommandLine.Parser(with => with.HelpWriter = null)
                                .ParseArguments<OrderOptions, RegistrationOptions>(args);

            try
            {
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
            catch (ProviderException ex)
            {
                console.MarkupLine("[red]{0}[/]", ex.Message);
                if (verbose)
                {
                    throw;
                }

                return 1;
            }
            catch (ConfigurationException ex)
            {
                console.MarkupLine("[red]{0}[/]", ex.Message);
                if (verbose)
                {
                    throw;
                }
                return 1;
            }
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

                // Environment Variables
                var envResolver = new EnvironmentVariableResolver(logger, secretStore, Environment.GetEnvironmentVariables());
                var ok = envResolver.Parse(options.EnvFromSecrets);

                if (!ok)
                {
                    throw new ConfigurationException("Error parsing Environment to Secret values. Exiting for safety.");
                }

                if (options.VerificationTimeoutSeconds < 30)
                {
                    logger.LogWarning("Overriding verification timeout to minimum value (30 seconds).");
                    options.VerificationTimeoutSeconds = 30;
                }

                // Key Vault Certificates
                logger.LogDebug("Creating Azure Key Vault certificate client...");
                var kvCertificateClient = new CertificateClient(options.KeyVaultUri, azureCreds);
                ICertificateStore certificateStore = new AzureKeyVaultCertificateStore(logger, kvCertificateClient);

                // ACME Directory
                logger.LogDebug("Creating ACME provider instance...");
                var certesConfiguration = new CertesAcmeConfiguration(options.Server);
                IAcmeDirectory acmeProvider = new CertesAcmeProvider(logger, registrationSecret, certesConfiguration);

                ctx.Status($"Creating {Enum.GetName(typeof(DnsProviders), options.DnsProvider)} DNS Client...");

                // create using factory for future and to hide lazy logic.
                var dns = DnsFactory.Create(logger,
                    new DnsFactory.DnsOptions
                    {
                        Provider = options.DnsProvider,
                        AadTenantId = options.AadTenantId,
                        AzureCredential = azureCreds,
                        AzureDnsResourceId = options.DnsZoneResourceId,
                        ZoneOverride = options.Zone,
                        CloudlfareZoneIdentifier = options.CloudlfareZoneIdentifier,
                        CloudlfareApiToken = options.CloudlfareApiToken,
                        AzureChinaCloud = options.AzureChinaCloud
                    }
                );

                var dnsLookup = new DnsLookup(options.DnsLookup.ToLowerInvariant() switch
                {
                    "" => null,
                    "google" => NameServer.GooglePublicDns,
                    "google2" => NameServer.GooglePublicDns2,
                    "cloudflare" => NameServer.Cloudflare,
                    "cloudflare2" => NameServer.Cloudflare2,
                    _ => IPEndPoint.TryParse(options.DnsLookup, out var nameServerIp)
                        ? nameServerIp
                        : throw new ConfigurationException("Error parsing DNS lookup server IP address.")
                });

                // command
                var rc = new OrderCommand(logger, envResolver, certificateStore, acmeProvider, dns, dnsLookup);

                return rc;
            });

        }

        static async Task<RegistrationCommand> BuildRegistrationCommand(ILogger logger, RegistrationOptions options)
        {
            return await AnsiConsole.Status().StartAsync<RegistrationCommand>("Validating credentials and dependencies", async ctx =>
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

                // Environment Variables
                var envResolver = new EnvironmentVariableResolver(logger, secretStore, Environment.GetEnvironmentVariables());
                var ok = envResolver.Parse(options.EnvFromSecrets);

                if (!ok)
                {
                    throw new ConfigurationException("Error parsing Environment to Secret values. Exiting for safety.");
                }

                // ACME Directory
                logger.LogDebug("Creating ACME provider instance...");
                var certesConfiguration = new CertesAcmeConfiguration(options.Server);
                IAcmeDirectory acmeProvider = new CertesAcmeProvider(logger, registrationSecret, certesConfiguration);

                // command
                var rc = new RegistrationCommand(logger, envResolver, acmeProvider);

                return rc;
            });
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

            // if version is requested, then dont return 1 error code.
            if (parserResult is CommandLine.NotParsed<object>)
            {
                var pr = parserResult as CommandLine.NotParsed<object>;
                if (pr != null && pr.Errors.Any())
                {
                    var vre = pr.Errors.First() as CommandLine.VersionRequestedError;

                    if (vre != null)
                    {
                        return 0;
                    }
                }
            }

            return 1;
        }
    }
}