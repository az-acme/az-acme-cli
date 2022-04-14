using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesAcmeCredential : IAcmeCredential
    {
        public CertesAcmeCredential(Uri directory, string pem)
        {
            this.Directory = directory;
            this.Pem = pem;
        }

        public Uri Directory { get; }
        public string Pem { get; }
    }
}
