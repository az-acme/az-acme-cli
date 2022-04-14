using Certes;
using Certes.Acme;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Cli.Util
{
    internal class DnsActions
    {
        private string zoneName;
        private readonly ResourceId zone;
        private readonly DnsManagementClient client;
        private readonly ILogger logger;
        public List<DnsActionItem> Items { get; } = new List<DnsActionItem>();

        public DnsActions(ResourceId zone, DnsManagementClient client, string? zoneOverride, ILogger logger)
        {
            this.zone = zone ?? throw new ArgumentNullException(nameof(zone));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // set it once.
            client.SubscriptionId = zone.SubscriptionId;
            if(zoneOverride != null)
            {
                zoneName = zoneOverride;
            } 
            else
            {
                zoneName = zone.Name;
            }
        }

        public class DnsActionItem
        {
            public DnsActionItem(string identifier, string txtRecord, string challengeText)
            {
                this.Identifier = identifier;
                this.TxtRecord = txtRecord;
                this.ChallengeText = challengeText;
            }

            public string Identifier { get; private set; }
            public string TxtRecord { get; private set; }
            public string ChallengeText { get; private set; }
        }

        public bool AddDomainIdenitifier(string domainIdenitifer, string challengeText)
        {
            if (!domainIdenitifer.EndsWith(zoneName))
            {
                this.logger.LogWithColor(LogLevel.Error, "Expecting identifier '{0}' to end with '{1}'.", domainIdenitifer, zoneName);
                return false;
            }

            var remaining = domainIdenitifer.Substring(0, domainIdenitifer.Length - zoneName.Length).TrimEnd('.');
            var recordName = "_acme-challenge";
            if (remaining.Length > 0)
            {
                recordName = recordName + "." + remaining;
            }

            Items.Add(new DnsActionItem(domainIdenitifer, recordName, challengeText));
            
            return true;
        }

        public async Task UpdateTxtRecords()
        {
            foreach (var item in Items)
            {
                this.logger.LogInformation("Configuring TXT challenge for '{0}' using TXT record '{1}'.", item.Identifier, item.TxtRecord);

                var recordSets = await client.RecordSets.ListByTypeAsync(zone.ResourceGroupName, zone.Name, Microsoft.Azure.Management.Dns.Models.RecordType.TXT);

                var records = recordSets.Where(x => x.Name == item.TxtRecord).FirstOrDefault();

                if (records == null)
                {
                    records = new RecordSet();
                    records.TTL = 60;
                    records.TxtRecords = new List<TxtRecord> { new TxtRecord(new List<string>() { item.ChallengeText }) };

                    var result = client.RecordSets.CreateOrUpdate(zone.ResourceGroupName, zone.Name, item.TxtRecord, RecordType.TXT, records);
                }
                else
                {
                    if (!(records.TxtRecords.Any(txt => txt.Value.Any(val => val == item.ChallengeText))))
                    {
                        records.TxtRecords.Add(new TxtRecord(new List<string>() { item.ChallengeText }));
                        var result = client.RecordSets.Update(zone.ResourceGroupName, zone.Name, item.TxtRecord, RecordType.TXT, records);
                    }
                }
            }
        }

        public async Task<bool> WaitForVerification(StatusContext ctx, IOrderContext orderContext, int attempts = 10, int delaySeconds = 5)
        {
            bool verified = true;

            foreach (var action in this.Items)
            {
                this.logger.LogWithColor(LogLevel.Information, "Waiting for ownership verification of '{0}'...", action.Identifier);
               
                var auth = await orderContext.Authorization(action.Identifier);
                await (await auth.Dns()).Validate();
                int attempt = 1;
                while (attempt <= attempts)
                {
                    ctx.Status($"Waiting for DNS TXT verification for '{action.Identifier}'. Attmept {attempt}/{attempts}...");

                    var res = await auth.Resource();

                    if (res.Status == Certes.Acme.Resource.AuthorizationStatus.Valid)
                    {
                        this.logger.LogWithColor(LogLevel.Information, "TXT Verified for '{0}' :check_mark:", action.Identifier);
                        break;
                    }
                    else if (res.Status == Certes.Acme.Resource.AuthorizationStatus.Invalid)
                    {
                        this.logger.LogWithColor(LogLevel.Error, "Unable to verify for '{0}', status is '{1}' :cross_mark:", action.Identifier, res.Status);
                        verified = false; // cant complete.
                        break;
                    }
                    else if(attempt == attempts)
                    {
                        // run out of chances.
                        verified = false;
                        break;
                    }
                    
#pragma warning disable CS8604 // Possible null reference argument.
                    this.logger.LogWithColor(LogLevel.Information, "Status is '{0}'. Waiting {1} seconds before checking again...", res.Status, delaySeconds);
#pragma warning restore CS8604 // Possible null reference argument.

                    attempt++;
                    Thread.Sleep(delaySeconds * 1000);
                }

            }

            return verified;
        }



    }
}
