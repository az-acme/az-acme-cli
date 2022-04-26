using CommandLine;

namespace AzAcme.Cli.Commands.Options
{
    public class Options
    {
        [Option("env-from-secrets", Required = false, Separator = ' ', HelpText = "Side load envrionment variables from Key Vault Secrets. (Example: FOO=secret-foo BAR=secret-bar)")]
        public IList<string> EnvFromSecrets { get; set; } = new List<string>();

        [Option("verbose", Required = false, HelpText = "Enable detailed logging.")]
        public bool Verbose { get; set; }
    }
}
