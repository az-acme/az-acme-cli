namespace AzAcme.Core.Providers.Models
{
    public class CertificateMetadata
    {
        public bool Exists { get; set; }
        public DateTimeOffset? Expires { get; set; }

    }
}
