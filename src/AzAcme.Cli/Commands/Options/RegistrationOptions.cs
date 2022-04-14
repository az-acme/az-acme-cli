using Certes.Acme;
using CommandLine;

namespace AzAcme.Cli.Commands.Options
{
    [Verb("register", HelpText = "Register account if not already registered.")]
    public class RegistrationOptions : Options
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        
        [Option("key-vault-uri", Required = true, HelpText = "Key Vault URI for the Key Vault to be used.")]
        public Uri KeyVaultUri { get; set; }

        [Option("account-secret", Required = true, HelpText = "Secret name used for storing the ACME account key.")]
        public string AccountSecretName { get; set; }

        [Option("email", Required = true, HelpText = "Registration email address.")]
        public string AccountEmailAddress { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


        [Option("force", HelpText = "Force new registration even if secret exists.")]
        public bool ForceRegistration { get; set; } = false;

        [Option("server", HelpText = "ACME Server URI. Defaults to ACME for Letsencrypt.")]
        public Uri Server { get; set; } = WellKnownServers.LetsEncryptV2;

        [Option("agree-terms", HelpText = "Agree to the Terms of Service.")]
        public bool AgreeTermsOfService { get; set; } = false;


    }

}
