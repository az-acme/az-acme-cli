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
