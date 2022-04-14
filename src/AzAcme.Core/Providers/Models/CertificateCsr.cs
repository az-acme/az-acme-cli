using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
