using CommandLine;

namespace AzAcme.Cli.Commands.Options
{
    public class Options
    {
        [Option("verbose", Required = false, HelpText = "Enable detailed logging.")]
        public bool Verbose { get; set; }
    }
}
