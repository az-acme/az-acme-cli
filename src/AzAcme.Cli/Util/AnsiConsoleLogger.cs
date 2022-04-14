using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.Cli.Util
{
    public class AnsiConsoleLogger : ILogger
    {
        private readonly bool verbose = false;
        public AnsiConsoleLogger(bool verbose)
        {
            this.verbose = verbose;
        }

        public class Scope : IDisposable
        {
            public void Dispose()
            {

            }
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return new Scope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (verbose)
            {
                return true;
            }

            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (AnsiConsole.Profile.Capabilities.Unicode)
            {
            

                string log = string.Empty;
                string pre = "";
                string post = "";

                switch (logLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        {
                            log = "DBG";
                            pre = "[silver]";
                            post = "[/]";
                            break;
                        }
                    case LogLevel.Information:
                        {
                            log = "LOG";
                            pre = "[grey]";
                            post = "[/]";
                            break;
                        }
                    case LogLevel.Warning:
                        {
                            log = "WRN";
                            pre = "[yellow]";
                            post = "[/]";
                            break;
                        }
                    case LogLevel.Error:
                    case LogLevel.Critical:
                        {
                            pre = "[red]";
                            post = "[/]";
                            log = "ERR";
                            break;
                        }
                }
                
                AnsiConsole.MarkupLine(pre + log + ": " + formatter(state, exception) + post);
            }
            else
            {
                AnsiConsole.WriteLine(logLevel.ToString().ToUpper() + ":" + formatter(state, exception));
            }
        }
    }
}
