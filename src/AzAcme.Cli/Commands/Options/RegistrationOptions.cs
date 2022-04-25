using AzAcme.Core.Providers.Models;
using CommandLine;

namespace AzAcme.Cli.Commands.Options
{
    [Verb("register", HelpText = "Register account with ACME provider.")]
    public class RegistrationOptions : Options
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        
        [Option("key-vault-uri", Required = true, HelpText = "Key Vault URI for the Key Vault to be used.")]
        public Uri KeyVaultUri { get; set; }

        [Option("account-secret", Required = true, HelpText = "Secret name for storing the ACME account key.")]
        public string AccountSecretName { get; set; }

        [Option("email", Required = true, HelpText = "Registration email address.")]
        public string AccountEmailAddress { get; set; }

        [Option("server", Required = true, HelpText = "ACME Server URI.")]
        public Uri Server { get; set; }

        [Option("force-registration", HelpText = "Force new registration even if secret exists.")]
        public bool ForceRegistration { get; set; } = false;

        [Option("agree-tos", HelpText = "Agree to the Terms of Service.")]
        public bool AgreeTermsOfService { get; set; } = false;

        [Option("eab-kid", HelpText = "External Account Binding Key ID. Environment variable 'AZ_ACME_EAB_KID' can also be used.")]
        public string? EabKid { get; set; }

        [Option("eab-hmac-key", HelpText = "External Account Binding HMAC Key. Environment variable 'AZ_ACME_EAB_KEY' can also be used.")]
        public string? EabHmacKey { get; set; }


#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }

}
