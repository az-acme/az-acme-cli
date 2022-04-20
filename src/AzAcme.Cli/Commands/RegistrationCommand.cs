using AzAcme.Cli.Commands.Options;
using AzAcme.Core;
using AzAcme.Core.Providers.Models;
using Microsoft.Extensions.Logging;

namespace AzAcme.Cli.Commands
{
    public class RegistrationCommand : Command<RegistrationOptions>
    {
        private readonly IAcmeDirectory acmeDirectory;

        public RegistrationCommand(ILogger logger, IAcmeDirectory acmeDirectory) : base(logger)
        {
            this.acmeDirectory = acmeDirectory;
        }
        protected override async Task<int> OnExecute(RegistrationOptions opts)
        {
            AcmeRegistration registration;

            if (!string.IsNullOrEmpty(opts.EabKid)
                && !string.IsNullOrEmpty(opts.EabHmacKey))
            {
                registration = new AcmeRegistration(opts.AccountEmailAddress,
                                                            opts.AgreeTermsOfService,
                                                            ExternalAccountBindingAlgorithms.HS256, // only support this for now.
                                                            opts.EabKid,
                                                            opts.EabHmacKey,
                                                            opts.ForceRegistration);
            }
            else
            {
                registration = new AcmeRegistration(opts.AccountEmailAddress,
                                                            opts.AgreeTermsOfService,
                                                            opts.ForceRegistration);
            }

            // register
            _ = await this.acmeDirectory.Register(registration);

            return 0;
        }


    }
}
