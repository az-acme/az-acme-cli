using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers
{
    public interface IScopedSecret
    {
        Task<bool> Exists();

        Task<string> GetSecret();

        Task CreateOrUpdate(string value);
    }
}
