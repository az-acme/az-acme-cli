using AzAcme.Core.Providers.Models;
using Spectre.Console;

namespace AzAcme.Core.Extensions
{
    internal static class SpectreExtensions
    {
        internal static Table ToTable(this CertificateRequest request, CertificateMetadata metadata, bool orderNeeded)
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


        internal static Table ToTable(this Order order)
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
