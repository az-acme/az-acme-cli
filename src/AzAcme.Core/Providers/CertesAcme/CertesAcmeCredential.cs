namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesAcmeCredential : IAcmeCredential
    {
        public CertesAcmeCredential(string pem)
        {
            this.Pem = pem;
        }

        public string Pem { get; }
    }
}
