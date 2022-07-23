using AzAcme.Core.Exceptions;

namespace AzAcme.Core.Providers.Helpers;

public static class DnsHelpers
{
    public static string DetermineTxtRecordName(string identifier, string zoneName)
    {
        if (!identifier.EndsWith(zoneName))
        {
            throw new ConfigurationException("Invalid DNS Zone. All Subjects and SANs must be part of the same DNS Zone.");
        }

        if (identifier.StartsWith("*."))
        {
            identifier = identifier.Substring(2);
        }

        var remaining = identifier.Substring(0, identifier.Length - zoneName.Length).TrimEnd('.');
        var recordName = "_acme-challenge";
        if (remaining.Length > 0)
        {
            recordName = recordName + "." + remaining;
        }

        return recordName;
    }
}