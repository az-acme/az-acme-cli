﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.Models
{
    public class CertificateMetadata
    {
        public bool Exists { get; set; }
        public DateTimeOffset? Expires { get; set; }

        public bool NewOrExpiresInDays(int days)
        {
            return !Exists || Expires.HasValue && DateTime.UtcNow.AddDays(days) > Expires;
        }
    }
}
