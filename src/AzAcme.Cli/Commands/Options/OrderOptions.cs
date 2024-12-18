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

        private string _subject = "";
        [Option("subject", Required = true, HelpText = "Subject name of the certificate, such as 'foo.example.com'.")]
        public string Subject { get => _subject; set { _subject = value.TrimEnd(new char[] { '\n', '\r' }); } }

        [Option("dns-provider", Required = true, Default = DnsProviders.Azure, HelpText = "DNS provider for challenges.")]
        public DnsProviders DnsProvider { get; set; }

        private string _dnsZone = "";
        [Option("azure-dns-zone", Required = false, HelpText = "Resource ID for Azure DNS Zone to use for challenge.")]
        public string DnsZoneResourceId { get => _dnsZone; set { _dnsZone = value.TrimEnd(new char[] { '\n', '\r' }); } }

        private string[] _sans;
        [Option("sans", Required = false, Separator = ' ', HelpText = "Subjet Alternative Names (SANs) space separated.")]
        public IList<string> SubjectAlternativeNames { get => _sans; set { _sans = value.Select(v => v.TrimEnd(new char[] { '\n', '\r' })).ToArray(); } }

        [Option("aad-tenant", Required = false, HelpText = "Explicitly set AAD Tenant ID for obtaining JWT token for Azure DNS API.")]
        public string? AadTenantId { get; set; } = null;

        [Option("zone-name", Required = false, HelpText = "Set zone name when being delegated from to calculare correct TXT records.")]
        public string Zone { get; set; }

        [Option("force-order", HelpText = "Force order / renewal even if not expiring soon.")]
        public bool ForceOrder { get; set; } = false;

        [Option("verification-timeout-seconds", Required = false, Default = 60, HelpText = "Challenge verification timout in seconds.")]
        public int VerificationTimeoutSeconds { get; set; }

        [Option("disable-livetable", HelpText = "Disable Live tables. Required for some Azuer DevOps pipelines.")]
        public bool DisableLiveTable { get; set; } = false;

        [Option("cf-zone-id", Required = false, HelpText = "Zone ID of the Cloudflare-Site. Required when using Cloudflare DNS provider.")]
        public string CloudlfareZoneIdentifier { get; set; }

        [Option("cf-api-token", Required = false, HelpText = "Cloudflare API Token with permissions to modify DNS. Required when using Cloudflare DNS provider.")]
        public string CloudlfareApiToken { get; set; }

        [Option("dns-lookup", Required = false, Default = "", HelpText = "DNS lookup server pre-defined name or custom IP address for challenge pre-validation.")]
        public string DnsLookup { get; set; }

        [Option("AzureChinaCloud", HelpText = "If this flag is present we will use Azure China Cloud endpoints.")]
        public bool AzureChinaCloud { get; set; } = false;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


    }
}
