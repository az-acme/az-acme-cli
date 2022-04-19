namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesAcmeConfiguration
    {
        public CertesAcmeConfiguration(Uri acmeDirectory)
        {
            this.Directory = acmeDirectory;
        }

        /// <summary>
        /// ACME Server Directory URI
        /// </summary>
        public Uri Directory { get; set; }
    }
}
