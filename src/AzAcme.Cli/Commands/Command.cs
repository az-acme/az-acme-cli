using AzAcme.Cli.Util;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Certes;
using Microsoft.Azure.Management.Dns;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Spectre.Console;

namespace AzAcme.Cli.Commands
{
    public abstract class Command<T> where T : Options.Options
    {
        protected readonly ILogger logger;

        protected Command(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected abstract Task<int> OnExecute(T opts);

        public async Task<int> Execute(T opts)
        {
            return await OnExecute(opts);
        }
    }
}
