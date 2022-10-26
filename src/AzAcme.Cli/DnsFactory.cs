using AzAcme.Cli.Commands.Options;
using AzAcme.Core;
using AzAcme.Core.Exceptions;
using AzAcme.Core.Providers;
using AzAcme.Core.Providers.AzureDns;
using AzAcme.Core.Providers.AzurePrivateDns;
using Azure.Identity;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.PrivateDns;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Cli
{
    internal class DnsFactory
    {
        public class DnsOptions
        {
            public DnsProviders Provider { get; set; }

            public DefaultAzureCredential? AzureCredential { get; set; } 
            
            public string? AzureDnsResourceId { get; set; }
            public string? AadTenantId { get; set; }
            public string? ZoneOverride { get; set; }
        }

        public static IDnsZone Create(ILogger logger, DnsOptions options)
        {
            switch (options.Provider)
            {
                case DnsProviders.Azure:
                    {
                        if(options.AzureCredential == null)
                        {
                            throw new ArgumentException("Azure Credentials must be set.");
                        }

                        if(string.IsNullOrEmpty(options.AzureDnsResourceId))
                        {
                            throw new ConfigurationException("Azure DNS Resource ID must be set.");
                        }

                        Lazy<IDnsZone> zone = new Lazy<IDnsZone>(() =>
                        {
                            logger.LogDebug("Getting DNS Client Token from AAD...");
                            var token = options.AzureCredential.GetToken(new Azure.Core.TokenRequestContext(new[] { $"https://management.azure.com/.default" }, tenantId: options.AadTenantId));
                            ServiceClientCredentials serviceClientCreds = new TokenCredentials(token.Token);
                            var dnsClient = new DnsManagementClient(serviceClientCreds);
                            IDnsZone azureDns = new AzureDnsZone(logger, dnsClient, options.AzureDnsResourceId, options.ZoneOverride);

                            return azureDns;
                        });

                        return new LazyDnsZone(zone);
                    }
                case DnsProviders.AzurePrivate:
                    {
                        if(options.AzureCredential == null)
                        {
                            throw new ArgumentException("Azure Credentials must be set.");
                        }

                        if(string.IsNullOrEmpty(options.AzureDnsResourceId))
                        {
                            throw new ConfigurationException("Azure DNS Resource ID must be set.");
                        }

                        Lazy<IDnsZone> zone = new Lazy<IDnsZone>(() =>
                        {
                            logger.LogDebug("Getting DNS Client Token from AAD...");
                            var token = options.AzureCredential.GetToken(new Azure.Core.TokenRequestContext(new[] { $"https://management.azure.com/.default" }, tenantId: options.AadTenantId));
                            ServiceClientCredentials serviceClientCreds = new TokenCredentials(token.Token);
                            var dnsClient = new PrivateDnsManagementClient(serviceClientCreds);
                            IDnsZone azureDns = new AzurePrivateDnsZone(logger, dnsClient, options.AzureDnsResourceId, options.ZoneOverride);

                            return azureDns;
                        });

                        return new LazyDnsZone(zone);
                    }
            }

            throw new NotSupportedException("Unable to build DNS Client");
        }
    }
}
