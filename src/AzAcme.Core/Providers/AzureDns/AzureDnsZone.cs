﻿using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers.Helpers;
using AzAcme.Core.Providers.Models;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;

namespace AzAcme.Core.Providers.AzureDns
{
    public class AzureDnsZone : IDnsZone
    {
        private readonly ILogger logger;
        private readonly DnsManagementClient client;
        private ResourceId azureDnsResource;
        private string zoneName;

        public AzureDnsZone(ILogger logger, DnsManagementClient client, string azureDnsZoneResourceId, string? zoneOverride)
        {
            this.azureDnsResource = ResourceId.FromString(azureDnsZoneResourceId);
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.client.SubscriptionId = azureDnsResource.SubscriptionId;
            this.zoneName = !string.IsNullOrEmpty(zoneOverride) ? zoneOverride : this.azureDnsResource.Name;
            
        }

        public async Task<Order> SetTxtRecords(Order order)
        {
            // determine the TXT records needed first, so we validate all before applying.
            foreach(var challenge in order.Challenges)
            {
                var record = DnsHelpers.DetermineTxtRecordName(challenge.Identitifer, this.zoneName);
                challenge.SetRecordName(record);
            }

            // add them all
            foreach (var challenge in order.Challenges)
            {
                await UpdateTxtRecord(challenge);
            }

            return order;
        }

        public async Task<Order> RemoveTxtRecords(Order order)
        {
            // update the record first, so we validate all.
            foreach (var challenge in order.Challenges)
            {
                await RemoveTxtRecord(challenge);
            }

            return order;
        }

        private async Task RemoveTxtRecord(DnsChallenge challenge)
        {
            // Load all then filter
            var recordSets = await client.RecordSets.ListByTypeAsync(azureDnsResource.ResourceGroupName, azureDnsResource.Name, Microsoft.Azure.Management.Dns.Models.RecordType.TXT);

            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
            var records = recordSets.Where(x => x.Name == challenge.TxtRecord).FirstOrDefault();
            
            if (records == null)
            {
                this.logger.LogDebug("No TXT record set for '{0}' found. Skipping delete.", challenge.TxtRecord);
            }
            else
            {
                this.logger.LogDebug("Removing TXT record set '{0}'.", challenge.TxtRecord);
                await client.RecordSets.DeleteAsync(azureDnsResource.ResourceGroupName, azureDnsResource.Name, challenge.TxtRecord, RecordType.TXT);
            }
        }

        private async Task UpdateTxtRecord(DnsChallenge challenge)
        {
            var recordSets = await client.RecordSets.ListByTypeAsync(azureDnsResource.ResourceGroupName, azureDnsResource.Name, Microsoft.Azure.Management.Dns.Models.RecordType.TXT);

            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
            var records = recordSets.Where(x => x.Name == challenge.TxtRecord).FirstOrDefault();

            if (records == null)
            {
                this.logger.LogDebug("DNS Records do not exist for '{0}'. Creating.",challenge.TxtRecord);
                records = new RecordSet();
                records.TTL = 60;
                records.TxtRecords = new List<TxtRecord> { new TxtRecord(new List<string>() { challenge.TxtValue }) };

                var result = client.RecordSets.CreateOrUpdate(azureDnsResource.ResourceGroupName, azureDnsResource.Name, challenge.TxtRecord, RecordType.TXT, records);
            }
            else
            {
                if (!(records.TxtRecords.Any(txt => txt.Value.Any(val => val == challenge.TxtValue))))
                {
                    this.logger.LogDebug("Updating DNS Record for '{0}'.", challenge.TxtRecord);
                    records.TxtRecords.Add(new TxtRecord(new List<string>() { challenge.TxtValue }));
                    var result = client.RecordSets.Update(azureDnsResource.ResourceGroupName, azureDnsResource.Name, challenge.TxtRecord, RecordType.TXT, records);
                }
                else
                {
                    this.logger.LogDebug("DNS Record with matching challenge value already exisits for '{0}'. Skipping.",challenge.TxtRecord);
                }
            }
            
        }

    }
}
