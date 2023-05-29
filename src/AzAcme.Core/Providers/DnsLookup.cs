using System.Net;
using DnsClient;

using AzAcme.Core.Providers.Helpers;
using AzAcme.Core.Providers.Models;

namespace AzAcme.Core.Providers
{
    public class DnsLookup : IDnsLookup
    {
        private readonly IPEndPoint? nameServer;

        public DnsLookup() : this(null) { }

        public DnsLookup(IPEndPoint? nameServer)
        {
            this.nameServer = nameServer;
        }

        public async Task<bool> ValidateTxtRecords(Order order)
        {
            bool dnsResolved = true;

            var lookup = nameServer != null ? new LookupClient(nameServer) : new LookupClient();

            foreach (var challenge in order.Challenges)
            {
                var dnsName = DnsHelpers.DetermineTxtRecordName(challenge.Identitifer, string.Empty);

                var result = await lookup.QueryAsync(dnsName, QueryType.TXT);

                if (!result.Answers.TxtRecords().SelectMany(x => x.Text).Contains(challenge.TxtValue))
                {
                    challenge.SetStatus(DnsChallenge.DnsChallengeStatus.Pending);
                    dnsResolved = false;
                }
            }

            return dnsResolved;
        }
    }
}
