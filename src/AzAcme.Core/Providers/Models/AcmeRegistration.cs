using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.Models
{
    public enum ExternalAccountBindingAlgorithms { NONE, HS256, HS512, HS384 }

    public class AcmeRegistration
    {
        public AcmeRegistration(string email, bool acceptTermsOfService, bool force = false)
        {
            this.Email = email;
            this.AcceptTermsOfService = acceptTermsOfService;
            this.Force = force;
        }

        public AcmeRegistration(string email, bool acceptTermsOfService, ExternalAccountBindingAlgorithms eabAlgo, string eabKeyId, string eabKey, bool force = false)
        {
            this.Email = email;
            this.AcceptTermsOfService = acceptTermsOfService;
            this.Force = force;
            this.EabAlgorithm = eabAlgo;
            this.EabKeyId = eabKeyId;
            this.EabKey = eabKey;
        }

        public string Email { get; }

        public ExternalAccountBindingAlgorithms EabAlgorithm { get; set; } = ExternalAccountBindingAlgorithms.NONE;

        public string? EabKeyId { get; }

        public string? EabKey { get; }

        public bool Force { get; }

        public bool AcceptTermsOfService { get; }
    }
}
