using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
