using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Certes;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.X509;
using Spectre.Console;
using System.Diagnostics;
using System.Text;

namespace AzAcme.Cli.Commands
{
    public class OrderCommand : Command<OrderOptions>
    {
        public OrderCommand(ILogger logger) : base(logger)
        {

        }
        
        protected override async Task<int> OnExecute(StatusContext ctx, OrderOptions opts)
        {
            var context = await PrepareContext(ctx,opts);

            if (context == null)
            {
                AnsiConsole.MarkupLine("[red]Unable to initialize ACME Context[/]");
                return 1;
            }

            var certClient = CreateCertificateClient(opts.KeyVaultUri);

            ctx.Status("Checking certificate info from Key Vault...");

            // ignores pending.
            var existingProps = KeyVaultHelpers.LoadCertificateProperties(certClient, opts.Certificate);

            bool renew = false;

            // check renewal flow first.
            if (existingProps != null)
            {
                this.logger.LogWithColor(LogLevel.Debug, "Found properties, checking expiration information...");
                if (existingProps.ExpiresOn != null)
                {
                    this.logger.LogWithColor(LogLevel.Information, "Certificate set to expire at '{0}'", existingProps.ExpiresOn);
                    if (DateTime.UtcNow.AddDays(opts.RenewWithinDays) > existingProps.ExpiresOn)
                    {
                        this.logger.LogWithColor(LogLevel.Information, "Certificate requires renewal (expires within '{0}' days).", opts.RenewWithinDays);
                        renew = true;
                    }
                }
                else
                {
                    this.logger.LogWithColor(LogLevel.Error, "Certificate does not have expiry defined. Cannot process.");
                    return 1;
                }

                if (!renew)
                {
                    AnsiConsole.MarkupLine("[green]Certificate is valid and does not expire within {0} days.[/]", opts.RenewWithinDays);
                    return 0;
                }
            }

            if(renew || existingProps == null)
            {
                var op = KeyVaultHelpers.GetExistingOperationOrNull(certClient, opts.Certificate);

                if (op != null)
                {
                    this.logger.LogWithColor(LogLevel.Warning, "Key Vault Certificate operation exists. Continuing existing...");
                }
                else
                {
                    ctx.Status("Creating CSR in Key Vault...");
                    this.logger.LogWithColor(LogLevel.Information, "Creating new CSR for subject '{0}'", opts.Subject);
                    var sans = new SubjectAlternativeNames();
                    foreach(var san in opts.SubjectAlternativeNames)
                    {
                        sans.DnsNames.Add(san);
                    }

                    CertificatePolicy policy;

                    if (opts.SubjectAlternativeNames.Count > 0)
                    {
                        policy = new CertificatePolicy("Unknown", "CN=" + opts.Subject,sans);
                    }
                    else
                    {
                        policy = new CertificatePolicy("Unknown", "CN=" + opts.Subject);
                    }

                    op = await certClient.StartCreateCertificateAsync(opts.Certificate, policy);
                }

                ctx.Status("Submitting Order to ACME...");
                
                var all = new List<string>();
                all.Add(opts.Subject);
                all.AddRange(opts.SubjectAlternativeNames);

                var orderContext = await context.NewOrder(all);

                AnsiConsole.MarkupLine("[green]Submitted order successfully to ACME[/]", opts.RenewWithinDays);

                // create client for use
                var dnsClient = this.CreateDnsManagementClient(opts.AadTenantId);

                // object to manage the dns actions (adding, and removing TXT records)
                var dnsActions = new DnsActions(ResourceId.FromString(opts.DnsZoneResourceId), dnsClient, opts.Zone, logger);


                ctx.Status("Determining challenges from ACME provider..");
                
                var authorizations = await orderContext.Authorizations();
                // prepare the info
                foreach (var auth in authorizations)
                {
                    var resource = (await auth.Resource());
                    var dns = await auth.Dns();
                    var dnsTxt = context.AccountKey.DnsTxt(dns.Token);
                    
                    if(!dnsActions.AddDomainIdenitifier(resource.Identifier.Value, dnsTxt))
                    {
                        return 1;
                    }
                }

                ctx.Status("Updating DNS with ACME challenge(s)...");

                // add the records
                await dnsActions.UpdateTxtRecords();

                ctx.Status("Waiting for ACME verification...");

                var verified = await dnsActions.WaitForVerification(ctx, orderContext);

                if (verified)
                {
                    AnsiConsole.MarkupLine("[green]Successfully verified TXT records for order[/]", opts.RenewWithinDays);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Unable to verify TXT records[/]");
                    return 1;
                }

                ctx.Status("Finalising order with ACME provider...");

                var order = await orderContext.Finalize(op.Properties.Csr);
#pragma warning disable CS8604 // Possible null reference argument.
                this.logger.LogWithColor(LogLevel.Information, "Order Finalised. ACME Order Status is '{0}'.", order.Status);
#pragma warning restore CS8604 // Possible null reference argument.

                while (order.Status != Certes.Acme.Resource.OrderStatus.Valid)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    this.logger.LogWithColor(LogLevel.Information, "Order status is '{0}', waiting for '{1}'.", order.Status, Certes.Acme.Resource.OrderStatus.Valid);
#pragma warning restore CS8604 // Possible null reference argument.
                }

                ctx.Status("Perparing Certificate...");

                this.logger.LogWithColor(LogLevel.Information, "Downloading Certificate from ACME provider...");
                var certChain = await orderContext.Download();
                

//                var issuers = new List<IEncodable>(certChain.Issuers);
//                issuers.Add(new CertificateContent(@"-----BEGIN CERTIFICATE-----
//MIIFVDCCBDygAwIBAgIRAO1dW8lt+99NPs1qSY3Rs8cwDQYJKoZIhvcNAQELBQAw
//cTELMAkGA1UEBhMCVVMxMzAxBgNVBAoTKihTVEFHSU5HKSBJbnRlcm5ldCBTZWN1
//cml0eSBSZXNlYXJjaCBHcm91cDEtMCsGA1UEAxMkKFNUQUdJTkcpIERvY3RvcmVk
//IER1cmlhbiBSb290IENBIFgzMB4XDTIxMDEyMDE5MTQwM1oXDTI0MDkzMDE4MTQw
//M1owZjELMAkGA1UEBhMCVVMxMzAxBgNVBAoTKihTVEFHSU5HKSBJbnRlcm5ldCBT
//ZWN1cml0eSBSZXNlYXJjaCBHcm91cDEiMCAGA1UEAxMZKFNUQUdJTkcpIFByZXRl
//bmQgUGVhciBYMTCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBALbagEdD
//Ta1QgGBWSYkyMhscZXENOBaVRTMX1hceJENgsL0Ma49D3MilI4KS38mtkmdF6cPW
//nL++fgehT0FbRHZgjOEr8UAN4jH6omjrbTD++VZneTsMVaGamQmDdFl5g1gYaigk
//kmx8OiCO68a4QXg4wSyn6iDipKP8utsE+x1E28SA75HOYqpdrk4HGxuULvlr03wZ
//GTIf/oRt2/c+dYmDoaJhge+GOrLAEQByO7+8+vzOwpNAPEx6LW+crEEZ7eBXih6V
//P19sTGy3yfqK5tPtTdXXCOQMKAp+gCj/VByhmIr+0iNDC540gtvV303WpcbwnkkL
//YC0Ft2cYUyHtkstOfRcRO+K2cZozoSwVPyB8/J9RpcRK3jgnX9lujfwA/pAbP0J2
//UPQFxmWFRQnFjaq6rkqbNEBgLy+kFL1NEsRbvFbKrRi5bYy2lNms2NJPZvdNQbT/
//2dBZKmJqxHkxCuOQFjhJQNeO+Njm1Z1iATS/3rts2yZlqXKsxQUzN6vNbD8KnXRM
//EeOXUYvbV4lqfCf8mS14WEbSiMy87GB5S9ucSV1XUrlTG5UGcMSZOBcEUpisRPEm
//QWUOTWIoDQ5FOia/GI+Ki523r2ruEmbmG37EBSBXdxIdndqrjy+QVAmCebyDx9eV
//EGOIpn26bW5LKerumJxa/CFBaKi4bRvmdJRLAgMBAAGjgfEwge4wDgYDVR0PAQH/
//BAQDAgEGMA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFLXzZfL+sAqSH/s8ffNE
//oKxjJcMUMB8GA1UdIwQYMBaAFAhX2onHolN5DE/d4JCPdLriJ3NEMDgGCCsGAQUF
//BwEBBCwwKjAoBggrBgEFBQcwAoYcaHR0cDovL3N0Zy1kc3QzLmkubGVuY3Iub3Jn
//LzAtBgNVHR8EJjAkMCKgIKAehhxodHRwOi8vc3RnLWRzdDMuYy5sZW5jci5vcmcv
//MCIGA1UdIAQbMBkwCAYGZ4EMAQIBMA0GCysGAQQBgt8TAQEBMA0GCSqGSIb3DQEB
//CwUAA4IBAQB7tR8B0eIQSS6MhP5kuvGth+dN02DsIhr0yJtk2ehIcPIqSxRRmHGl
//4u2c3QlvEpeRDp2w7eQdRTlI/WnNhY4JOofpMf2zwABgBWtAu0VooQcZZTpQruig
//F/z6xYkBk3UHkjeqxzMN3d1EqGusxJoqgdTouZ5X5QTTIee9nQ3LEhWnRSXDx7Y0
//ttR1BGfcdqHopO4IBqAhbkKRjF5zj7OD8cG35omywUbZtOJnftiI0nFcRaxbXo0v
//oDfLD0S6+AC2R3tKpqjkNX6/91hrRFglUakyMcZU/xleqbv6+Lr3YD8PsBTub6lI
//oZ2lS38fL18Aon458fbc0BPHtenfhKj5
//-----END CERTIFICATE-----"));

//                certChain.Issuers = issuers;

                List<byte[]> chain = new List<byte[]>();
                chain.Add(System.Text.Encoding.UTF8.GetBytes(certChain.ToPem()));
                foreach(var issuer in certChain.Issuers)
                {
                    chain.Add(System.Text.Encoding.UTF8.GetBytes(issuer.ToPem()));
                }
                


                this.logger.LogWithColor(LogLevel.Information, "Merging Certiciate in Key Vault...");
                var merged = await certClient.MergeCertificateAsync(new MergeCertificateOptions(opts.Certificate, chain));

                if(merged != null)
                {
                    AnsiConsole.MarkupLine("[green]Successfully completed certificate order[/]");
                }
                    
            }
            
            return 0;
        }

        

        private async Task<AcmeContext?> PrepareContext(StatusContext ctx, OrderOptions opts)
        {
            ctx.Status("Initializing ACME Context...");

            var client = this.CreateSecretClient(opts.KeyVaultUri);

            this.logger.LogWithColor(LogLevel.Information, "Checking secret '{0}' exists...", opts.AccountSecretName);
            
            var exists = KeyVaultHelpers.SecretExists(client, opts.AccountSecretName);

            if (!exists)
            {
                this.logger.LogError("Unable to find secret. Check it exists and caller has permissions.");
                return null;
            }

            var context = await this.CreateContext(opts.Server, client, opts.AccountSecretName);

            if (context != null)
            {
                AnsiConsole.MarkupLine("[green4]Initialized ACME Context OK[/]");
            }
      
            return context;
        }


        internal class CertificateContent : IEncodable
        {
            private readonly string pem;

            public CertificateContent(string pem)
            {
                this.pem = pem.Trim();
            }

            public byte[] ToDer()
            {
                var certParser = new X509CertificateParser();
                var cert = certParser.ReadCertificate(
                    Encoding.UTF8.GetBytes(pem));
                return cert.GetEncoded();
            }

            public string ToPem() => pem;
        }

    }
}
