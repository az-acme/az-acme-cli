using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.Models
{
    public class CerticateChain
    {
        public List<byte[]> Chain { get; } = new List<byte[]>();
    }
}
