using AzAcme.Cli;
using AzAcme.Cli.Commands;
using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using CommandLine;
using CommandLine.Text;
using Spectre.Console;
using System.Text;

namespace AzAcmi
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            var demo = new Demo();
            await demo.ZeroSsl();
            //await demo.LetsEncryptStaging();

            return 0;

            var console = AnsiConsole.Console;
            var logger = new AnsiConsoleLogger(args.Contains("--verbose"));

            var parser = new CommandLine.Parser(with => with.HelpWriter = null)
                                .ParseArguments<OrderOptions, RegistrationOptions>(args);

            var registration = new RegistrationCommand(logger);
            var order = new OrderCommand(logger);

            return await parser.MapResult(
                (RegistrationOptions opts) => registration.Execute(opts),
                (OrderOptions opts) => order.Execute(opts),
                errs => Task.FromResult(DisplayHelp(parser)));
        }

        static string Banner()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"     ___      ________          ___       ______ .___  ___.  _______     ");
            sb.AppendLine(@"    /   \    |       /         /   \     /      ||   \/   | |   ____|    ");
            sb.AppendLine(@"   /  ^  \   `---/  /         /  ^  \   |  ,----'|  \  /  | |  |__       ");
            sb.AppendLine(@"  /  /_\  \     /  /         /  /_\  \  |  |     |  |\/|  | |   __|      ");
            sb.AppendLine(@" /  _____  \   /  /----.    /  _____  \ |  `----.|  |  |  | |  |____     ");
            sb.AppendLine(@"/__/     \__\ /________|   /__/     \__\ \______||__|  |__| |_______|    ");
            sb.AppendLine();
            return sb.ToString();
        }

        static int DisplayHelp(ParserResult<object> parserResult)
        {
            Console.WriteLine(HelpText.AutoBuild(parserResult, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = Banner();
                h.Copyright = "Copyright (c) " + DateTime.UtcNow.Year + " Az Acme Authors";
                return h;
            }));
            return 1;
        }
    }
}