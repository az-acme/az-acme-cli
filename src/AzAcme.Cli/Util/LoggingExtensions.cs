using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Cli.Util
{
    internal static class LoggingExtensions
    {
        internal static void LogWithColor(this ILogger logger, LogLevel level, string message, params object[] args)
        {
            logger.Log(level, message, args.Select(x => "[yellow]" + x.ToString() + "[/]").ToArray());
        }
    }
}
