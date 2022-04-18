using AzAcme.Core.Providers.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Core.Extensions
{
    public static class SpectreExtensions
    {
        public static async Task WaitForVerificationWithTable(this Order order, IAcmeDirectory directory, int attempts, int delaySeconds)
        {
            var table = order.ToChallengeTable();
            await AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                ctx.Refresh();

                int attempt = 1;
                while (attempt <= attempts)
                {
                    await directory.ValidateChallenges(order);

                    table.Rows.Clear();
                    foreach (var item in order.Challenges)
                    {
                        table.AddRow(item.Identitifer, item.TxtRecord ?? "-", item.TxtValue, item.Status.ToString());
                    }
                    ctx.Refresh();

                    if (order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Validated
                        || order.Challenges.All(x => x.Status == DnsChallenge.DnsChallengeStatus.Failed)))
                    {
                        break;
                    }

                    await Task.Delay(delaySeconds * 1000);
                    attempt++;
                }
            });

        }

        public static Table ToTable(this CertificateRequest request, CertificateMetadata metadata, bool orderNeeded)
        {
            // Create a table
            var table = new Table();

            // Add some columns
            table.AddColumn("Subject");
            table.AddColumn("Subject Alternative Names");
            table.AddColumn("Expiry (UTC)");
            table.AddColumn("Action");

            string expiry = "-";
            if(metadata.Expires != null)
            {
                expiry = metadata.Expires.Value.UtcDateTime.ToString("yyyy-MM-dd");
            }

            table.AddRow(request.Subject, string.Join(Environment.NewLine, request.SubjectAlternativeNames), expiry, orderNeeded ? "Order" : "None");

            return table;
        }


        public static Table ToChallengeTable(this Order order)
        {
            // Create a table
            var table = new Table();

            // Add some columns
            table.AddColumn("Identifier");
            table.AddColumn("TXT Record");
            table.AddColumn("Challenge");
            table.AddColumn("Status");

            foreach (var item in order.Challenges)
            {
                table.AddRow(item.Identitifer, item.TxtRecord ?? "-", item.TxtValue, item.Status.ToString());
            }

            return table;
        }
    }
}
