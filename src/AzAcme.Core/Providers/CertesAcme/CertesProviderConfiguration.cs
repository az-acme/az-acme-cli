using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.CertesAcme
{
    public class CertesProviderConfiguration
    {
        public Uri Directory { get; set; }
        public string RegistrationEmailAddress { get; set; }

        public bool AgreedTermsOfService { get; set; }

        public bool ForceRegistration { get; set; }
        public int VerificationAttempts { get; set; } = 10;

        public int VerificationAttemptWaitSeconds { get; set; } = 5;
    }
}
