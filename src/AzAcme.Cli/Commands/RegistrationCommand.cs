using AzAcme.Cli.Commands.Options;
using AzAcme.Cli.Util;
using AzAcme.Core;
using AzAcme.Core.Providers.Models;
using Microsoft.Extensions.Logging;

namespace AzAcme.Cli.Commands
{
    public class RegistrationCommand : Command<RegistrationOptions>
    {
        private readonly IAcmeDirectory acmeDirectory;

        public RegistrationCommand(ILogger logger, EnvironmentVariableResolver environmentVariableResolver, IAcmeDirectory acmeDirectory) : base(logger, environmentVariableResolver)
        {
            this.acmeDirectory = acmeDirectory;
        }
        protected override async Task<int> OnExecute(RegistrationOptions opts)
        {
            AcmeRegistration registration;

            var eabKid = await this.Resolve(()=>opts.EabKid, EnvironmentVariables.AZ_ACME_EAB_KID);
            var eabkey = await this.Resolve(() => opts.EabHmacKey, EnvironmentVariables.AZ_ACME_EAB_KEY);

            if (!string.IsNullOrEmpty(eabKid)
                && !string.IsNullOrEmpty(eabkey))
            {
                registration = new AcmeRegistration(opts.AccountEmailAddress,
                                                            opts.AgreeTermsOfService,
                                                            ExternalAccountBindingAlgorithms.HS256, // only support this for now.
                                                            eabKid,
                                                            eabkey,
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
