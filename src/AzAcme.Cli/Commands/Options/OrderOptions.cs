using Certes.Acme;
using CommandLine;

namespace AzAcme.Cli.Commands.Options
{
    [Verb("order", HelpText = "Order and renew certificate.")]
    public class OrderOptions : Options
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Option("key-vault-uri", Required = true, HelpText = "Key Vault URI for the Key Vault to be used.")]
        public Uri KeyVaultUri { get; set; }

        [Option("account-secret", Required = true, HelpText = "Secret name used for storing the ACME account key.")]
        public string AccountSecretName { get; set; }

        [Option("certificate", Required = true, HelpText = "Name of certificate resource in Key Vault")]
        public string Certificate { get; internal set; }

        [Option("server", HelpText = "ACME Server URI. Defaults to ACME for Letsencrypt.")]
        public Uri Server { get; set; } = WellKnownServers.LetsEncryptV2;

        [Option("renew-within", Required = false, Default = 30, HelpText = "Renew within this amount of days before expiry.")]
        public int RenewWithinDays { get; set; }

        [Option("subject", Required = true, HelpText = "The subject name of the certificate, such as CN=contoso.com")]
        public string Subject { get; set; }

        [Option("sans", Required = false, Separator = ' ', HelpText = "The SANs to add to the certificate, as space separated strings.")]
        public IList<string> SubjectAlternativeNames { get; set; }
        
        [Option("azure-dns-zone", Required = true, HelpText = "Full resource ID to the DNS Zone in which to create the challenge.")]
        public string DnsZoneResourceId { get; set; }

        [Option("aad-tenant", Required = false, HelpText = "Optionally explicitly set the AAD Tenant ID to use when obtaining JWT token.")]
        public string? AadTenantId { get; set; } = null;

        [Option("zone", Required = false, HelpText = "Optionally override the zone name inferred by the Azure DNS Zone Resource Name. Needed if delegating domain for ACME verifications.")]
        public string? Zone { get; set; } = null;

        [Option("force-order", HelpText = "Force order / renewal even if not expiring soon.")]
        public bool ForceOrder { get; set; } = false;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


    }
}
