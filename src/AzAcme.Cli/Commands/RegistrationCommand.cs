using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using Certes;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AzAcme.Cli.Commands
{
    public class RegistrationCommand : Command<RegistrationOptions>
    {
        public RegistrationCommand(ILogger logger) : base(logger)
        {

        }
        protected override async Task<int> OnExecute(StatusContext ctx, RegistrationOptions opts)
        {
            ctx.Status("Checking ACME Registration...");

            var client = this.CreateSecretClient(opts.KeyVaultUri);

            var context = new AcmeContext(opts.Server);

            this.logger.LogWithColor(LogLevel.Information, "Checking secret '{0}' exists...", opts.AccountSecretName);
            var exists = KeyVaultHelpers.SecretExists(client, opts.AccountSecretName);

            if (!exists || opts.ForceRegistration) // needs TOS agreement.
            {
                if (opts.ForceRegistration && exists)
                {
                    this.logger.LogWithColor(LogLevel.Warning, "Force registration flag provided. New account credentials will be created...");
                }

                if (!opts.AgreeTermsOfService)
                {
                    logger.LogError("Terms of service must be agreed to when registering to ACME provider. Include the '--agree-terms' flag when agreed.");
                    AnsiConsole.MarkupLine("[red]Unable to complete registration[/]");
                    return 1;
                }

                this.logger.LogWithColor(LogLevel.Information, "Registering account with admin email '{0}'.", opts.AccountEmailAddress);

                ctx.Status("Registering with ACME provider...");
                var account = await context.NewAccount(opts.AccountEmailAddress, termsOfServiceAgreed: true);

                this.logger.LogWithColor(LogLevel.Information,"Saving account key to secret '{0}'", opts.AccountSecretName);
                Thread.Sleep(1000);
                await client.SetSecretAsync(opts.AccountSecretName, context.AccountKey.ToPem());

                AnsiConsole.MarkupLine("[green]Successfully registered with ACME provider[/]");
            }
            else
            {
                // exists, so try and load it.
                context = await this.CreateContext(opts.Server, client, opts.AccountSecretName);
                if (context == null)
                {
                    AnsiConsole.MarkupLine("[red]Unable to parse and create context. Key may be corrupt. Use --force to create new registration.[/]");
                    return 1;
                }

                AnsiConsole.MarkupLine("[green]Already registered with ACME provider[/]");
            }

            return 0;
        }

        
    }
}
