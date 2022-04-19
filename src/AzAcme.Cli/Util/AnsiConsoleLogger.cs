﻿using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AzAcme.Cli.Util
{
    internal class AnsiConsoleLogger : ILogger
    {
        private readonly bool verbose = false;
        internal AnsiConsoleLogger(bool verbose)
        {
            this.verbose = verbose;
        }

        internal class Scope : IDisposable
        {
            public void Dispose()
            {

            }
        }
        
        // Scoping not needed. Basic implementation only.
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
            if (!IsEnabled(logLevel))
                return;

            if (AnsiConsole.Profile.Capabilities.Unicode)
            {
                string log = LevelToShortName(logLevel);
                string pre = $"[{LevelToColor(logLevel)}]";
                string post = "[/]";

                AnsiConsole.MarkupLine(pre + log + ": " + formatter(state, exception) + post);
            }
            else
            {
                AnsiConsole.WriteLine(logLevel.ToString().ToUpper() + ":" + formatter(state, exception));
            }
        }

        private static string LevelToColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return "grey";
                case LogLevel.Debug: return "grey";
                case LogLevel.Information: return "silver";
                case LogLevel.Warning: return "yellow";
                case LogLevel.Error: return "red";
                case LogLevel.Critical:return "red";                    
            }

            return "black";
        }

        private static string LevelToShortName(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return "TRCE";
                case LogLevel.Debug: return "DBUG";
                case LogLevel.Information: return "INFO";
                case LogLevel.Warning: return "WARN";
                case LogLevel.Error: return "ERR";
                case LogLevel.Critical: return "CRIT";
            }

            return "LOG";
        }
    }
}
