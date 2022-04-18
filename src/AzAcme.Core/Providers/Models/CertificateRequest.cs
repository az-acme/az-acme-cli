using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.Models
{
    public class CertificateRequest
    {
        public CertificateRequest(string name, string subject, IList<string> subjectAlternativeNames)
        {
            Name = name;
            Subject = subject;
            SubjectAlternativeNames = subjectAlternativeNames;
        }

        public string Name { get; }

        public string Subject { get; }

        public IList<string> SubjectAlternativeNames { get; }
    }
}
