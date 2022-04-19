namespace AzAcme.Core.Providers.Models
{
    public class CertificateCsr
    {
        public CertificateCsr(string name, byte[] csr)
        {
            Name = name;
            Csr = csr;
        }

        public string Name { get; }
        public byte[] Csr { get; }
    }
}
