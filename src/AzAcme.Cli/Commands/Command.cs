using AzAcme.Cli.Util;
using Microsoft.Extensions.Logging;

namespace AzAcme.Cli.Commands
{
    public abstract class Command<T> where T : Options.Options
    {
        protected readonly ILogger logger;
        private readonly EnvironmentVariableResolver environmentVariableResolver;

        protected Command(ILogger logger, EnvironmentVariableResolver environmentVariableResolver)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.environmentVariableResolver = environmentVariableResolver ?? throw new ArgumentNullException(nameof(environmentVariableResolver));
        }

        protected abstract Task<int> OnExecute(T opts);

        public async Task<int> Execute(T opts)
        {
            return await OnExecute(opts);
        }

        protected async Task<string?> Resolve(Func<string?> option, string envVarName)
        {
            var ops = option();

            if (ops == null)
            {
                ops = await environmentVariableResolver.Resolve(envVarName);
            }

            return ops;
        }
    }
}
