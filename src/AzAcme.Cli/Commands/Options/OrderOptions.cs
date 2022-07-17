using CommandLine;

namespace AzAcme.Cli.Commands.Options
{
    [Verb("order", HelpText = "Order and renew certificate.")]
    public class OrderOptions : Options
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Option("key-vault-uri", Required = true, HelpText = "Key Vault URI for the Key Vault to be used.")]
        public Uri KeyVaultUri { get; set; }

        [Option("account-secret", Required = true, HelpText = "Secret name for storing the ACME account key.")]
        public string AccountSecretName { get; set; }

        [Option("certificate", Required = true, HelpText = "Name of certificate resource in Key Vault.")]
        public string Certificate { get; internal set; }

        [Option("server", Required = true, HelpText = "ACME Server URI.")]
        public Uri Server { get; set; }

        [Option("renew-within-days", Required = false, Default = 30, HelpText = "Renew within days before expiry.")]
        public int RenewWithinDays { get; set; }

        [Option("subject", Required = true, HelpText = "Subject name of the certificate, such as 'foo.example.com'.")]
        public string Subject { get; set; }

        [Option("dns-provider", Required = true, Default = DnsProviders.Azure, HelpText = "DNS provider for challenges.")]
        public DnsProviders DnsProvider { get; set; }

        [Option("azure-dns-zone", Required = false, HelpText = "Resource ID for Azure DNS Zone to use for challenge.")]
        public string DnsZoneResourceId { get; set; }

        [Option("sans", Required = false, Separator = ' ', HelpText = "Subjet Alternative Names (SANs) space separated.")]
        public IList<string> SubjectAlternativeNames { get; set; }
        
        [Option("aad-tenant", Required = false, HelpText = "Explicitly set AAD Tenant ID for obtaining JWT token for Azure DNS API.")]
        public string? AadTenantId { get; set; } = null;

        [Option("zone-name", Required = false, HelpText = "Set zone name when being delegated from to calculare correct TXT records.")]
        public string Zone { get; set; }

        [Option("force-order", HelpText = "Force order / renewal even if not expiring soon.")]
        public bool ForceOrder { get; set; } = false;

        [Option("prop-delay", Required = false, Default = 0, HelpText = "Additional delay seconds after creating DNS challenge TXT record.")]
        public int PropDelay { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


    }
}
