using CommandLine;

namespace AzAcme.Cli.Commands.Options
{
    public class Options
    {
        [Option("verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }
}
