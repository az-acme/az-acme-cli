﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers
{
    public interface ISecretStore
    {
        Task<IScopedSecret> CreateScopedSecret(string name);
    }
}
