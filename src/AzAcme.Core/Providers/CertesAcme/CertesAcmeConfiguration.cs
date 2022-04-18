using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
