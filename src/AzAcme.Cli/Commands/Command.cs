using Microsoft.Extensions.Logging;

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
