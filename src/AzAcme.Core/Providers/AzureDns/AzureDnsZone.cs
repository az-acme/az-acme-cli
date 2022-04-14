using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers.Models;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Providers.AzureDns
{
    public class AzureDnsZone : IDnsZone
    {
        private readonly DnsManagementClient client;
        private ResourceId privateDnsResource;
        public AzureDnsZone(DnsManagementClient client, string azureDnsZoneResourceId)
        {
            this.privateDnsResource = ResourceId.FromString(azureDnsZoneResourceId);
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.client.SubscriptionId = privateDnsResource.SubscriptionId;
        }

        public async Task<Order> SetTxtRecords(Order order)
        {
            // update the record first, so we validate all.
            foreach(var challenge in order.Challenges)
            {
                var record = DetermineTxtRecordName(challenge.Identitifer);
                challenge.SetRecordName(record);
            }

            // add them all
            foreach (var challenge in order.Challenges)
            {
                await UpdateTxtRecord(challenge);
            }

            return order;
        }

        private string DetermineTxtRecordName(string identifier)
        {
            if (!identifier.EndsWith(privateDnsResource.Name))
            {
                throw new ConfigurationException("Invalid DNS Zone. All Subjects and SANs must be part of the same DNS Zone.");
            }

            var remaining = identifier.Substring(0, identifier.Length - privateDnsResource.Name.Length).TrimEnd('.');
            var recordName = "_acme-challenge";
            if (remaining.Length > 0)
            {
                recordName = recordName + "." + remaining;
            }

            return recordName;
        }

        private async Task UpdateTxtRecord(DnsChallenge challenge)
        {
            //this.logger.LogInformation("Configuring TXT challenge for '{0}' using TXT record '{1}'.", item.Identifier, item.TxtRecord);

            var recordSets = await client.RecordSets.ListByTypeAsync(privateDnsResource.ResourceGroupName, privateDnsResource.Name, Microsoft.Azure.Management.Dns.Models.RecordType.TXT);

            var records = recordSets.Where(x => x.Name == challenge.TxtRecord).FirstOrDefault();

            if (records == null)
            {
                records = new RecordSet();
                records.TTL = 60;
                records.TxtRecords = new List<TxtRecord> { new TxtRecord(new List<string>() { challenge.TxtValue }) };

                var result = client.RecordSets.CreateOrUpdate(privateDnsResource.ResourceGroupName, privateDnsResource.Name, challenge.TxtRecord, RecordType.TXT, records);
            }
            else
            {
                if (!(records.TxtRecords.Any(txt => txt.Value.Any(val => val == challenge.TxtValue))))
                {
                    records.TxtRecords.Add(new TxtRecord(new List<string>() { challenge.TxtValue }));
                    var result = client.RecordSets.Update(privateDnsResource.ResourceGroupName, privateDnsResource.Name, challenge.TxtRecord, RecordType.TXT, records);
                }
            }
            
        }

    }
}
