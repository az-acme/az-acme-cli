using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzAcme.Core.Providers.Helpers;
using AzAcme.Core.Providers.Models;
using Microsoft.Extensions.Logging;

namespace AzAcme.Core.Providers.CloudflareDns
{
    public class CloudflareDnsZone : IDnsZone
    {
        private readonly ILogger logger;
        private readonly string apiToken;
        private readonly string zoneIdentifier;

        public CloudflareDnsZone(ILogger logger, string apiToken, string zoneIdentifier)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.apiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));
            this.zoneIdentifier = zoneIdentifier ?? throw new ArgumentNullException(nameof(zoneIdentifier));
        }

        public async Task<Order> SetTxtRecords(Order order)
        {
            var retVal = await ProcessOrder(order, async (client, challenge, zoneName) => 
            {
                logger.LogDebug("Challenge: TXT:{0}:{1}", challenge.Identitifer, challenge.TxtValue);
                var record = DnsHelpers.DetermineTxtRecordName(challenge.Identitifer, zoneName);
                challenge.SetRecordName(record); 
                await UpdateTxtRecord(client, challenge, zoneName);
                
                // Cloudflare needs some time to create/propagate the records.
                // So we manually check via DNS whether the records are really created.
                // We do this via DoH against Cloudflare. 
                // 
                // We do this because letsencrypt simply stops testing
                // the DNS challenge, if the TXT entry does not exists
                // on the first try and throws "urn:ietf:params:acme:error:malformed".
                // There's some reference at https://github.com/fszlin/certes/issues/285
                // 
                // This should probably be moved to CertesAcmeProvider.ValidateChallenges()
                // (and tries/timeout should be configurable, then).
                // I added it here for now, because this error happens 9 out of 10 times
                // when using Cloudflare.
                var tries = 0;
                DohAnswer? answer;
                logger.LogDebug("Checking via DoH for TXT record {0}.", record);
                do
                {
                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri($"https://1.1.1.1/dns-query?type=16&name={record}"),
                        Method = HttpMethod.Get,
                    };
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));
                    
                    var doh = await client.SendAsync(request);
                    doh.EnsureSuccessStatusCode();
                    
                    var dohResult = await doh.Content.ReadFromJsonAsync<DohResult>();
                    answer = dohResult?.Answers?.FirstOrDefault();
                    if (answer != null)
                    {
                        continue;
                    }
                    
                    tries++;
                    await Task.Delay(500);
                } while (tries < 11 && answer == null);
            });

            
            

            return retVal;
        }

        public async Task<Order> RemoveTxtRecords(Order order)
            => await ProcessOrder(order, RemoveTxtRecord);

        private async Task<Order> ProcessOrder(Order order, Func<HttpClient, DnsChallenge, string, Task> processor)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.apiToken);
                client.BaseAddress = new Uri($"https://api.cloudflare.com/client/v4/zones/{zoneIdentifier}/");
                
                // get zone name
                var zoneInfoResponse = await client.GetAsync(string.Empty);
                zoneInfoResponse.EnsureSuccessStatusCode();
                var zoneInfo = await zoneInfoResponse.Content.ReadFromJsonAsync<Wrapper<ZoneDetails>>();
                var zoneName = zoneInfo!.Result!.Name;
                logger.LogDebug("Zone name is: {0}", zoneName);
                
                foreach (var challenge in order.Challenges)
                {
                    await processor(client, challenge, zoneName);
                }
            }

            return order;
        }
        
        private async Task UpdateTxtRecord(HttpClient client, DnsChallenge challenge, string zoneName)
        {
            var name = $"{challenge.TxtRecord}.{zoneName}";
            var existsResult = await client.GetAsync(
                $"dns_records?type=TXT&name={name}&match=all");
            existsResult.EnsureSuccessStatusCode();
            var existingByName = (await existsResult.Content.ReadFromJsonAsync<Wrapper<DnsRecord[]>>())!
                .Result!.Where(x => x.Name == name).ToArray();

            if (existingByName.Length == 0)
            {
                logger.LogDebug("DNS Records do not exist for '{0}'. Creating.", name);
                var content = JsonContent.Create(new NewDnsRecord
                    {
                        Name = challenge.TxtRecord ?? throw new InvalidOperationException("TxtRecord can not be null."),
                        Content = challenge.TxtValue,
                    },
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json),
                    new JsonSerializerOptions());
                
                var response = await client.PostAsync(
                    "dns_records",
                    content);
                response.EnsureSuccessStatusCode();
                return;
            }

            if (existingByName.Any(x => x.Content == challenge.TxtValue))
            {
                logger.LogDebug("DNS Record with matching challenge value already exists for '{0}'. Skipping.", name);
                return;
            }
                
            logger.LogDebug("Updating DNS Record for '{0}'.", name);
            var updateContent = JsonContent.Create(new NewDnsRecord
                {
                    Name = challenge.TxtRecord ?? throw new InvalidOperationException("TxtRecord can not be null."),
                    Content = challenge.TxtValue,
                },
                new MediaTypeHeaderValue(MediaTypeNames.Application.Json),
                new JsonSerializerOptions());
                
            var updateResponse = await client.PutAsync(
                $"dns_records/{existingByName.Last().Id}",
                updateContent);
            updateResponse.EnsureSuccessStatusCode();
        }

        private async Task RemoveTxtRecord(HttpClient client, DnsChallenge challenge, string zoneName)
        {
            var name = $"{challenge.TxtRecord}.{zoneName}";
            var existsResult = await client.GetAsync(
                $"dns_records?type=TXT&name={name}&match=all");
            existsResult.EnsureSuccessStatusCode();
            var existing = (await existsResult.Content.ReadFromJsonAsync<Wrapper<DnsRecord[]>>())?
                .Result!.FirstOrDefault(x => x.Name == name);

            if (existing == null)
            {
                logger.LogDebug("DNS Records do not exist for '{0}'. Skipping delete.", name);
                return;
            }

            logger.LogDebug("Removing TXT record set '{0}'.", name);
            var updateResponse = await client.DeleteAsync(
                $"dns_records/{existing.Id}");
            updateResponse.EnsureSuccessStatusCode();
        }
        
        public class DnsRecord
        {
            [JsonPropertyName("id")] 
            public string Id { get; set; } = string.Empty;
            
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;
            
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
            
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        public class NewDnsRecord
        {
            [JsonPropertyName("type")] 
            public string Type { get; set; } = "TXT";
            
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
            
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;

            [JsonPropertyName("ttl")] 
            public int Ttl { get; set; } = 60;
        }
        
        public class Wrapper<T>
        {
            [JsonPropertyName("result")] 
            public T? Result { get; set; } = default;
        }
        
        public class ZoneDetails
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;
            
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }
        
        public class DohResult
        {
            [JsonPropertyName("Answer")]
            public DohAnswer[]? Answers { get; set; }
        }
        
        public class DohAnswer
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            
            [JsonPropertyName("data")]
            public string? Data { get; set; }
        }
    }
}