using AzAcme.Core;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace AzAcme.Cli.Util
{
    public class EnvironmentVariableResolver
    {
        private readonly ILogger logger;
        private readonly ISecretStore secretStore;
        private readonly IDictionary environmentVariables;
        private readonly Dictionary<string,string> variablesLookup = new Dictionary<string, string>();

        public EnvironmentVariableResolver(ILogger logger, ISecretStore secretStore, IDictionary environmentVariables)
        {
            this.logger = logger;
            this.secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
            this.environmentVariables = environmentVariables ?? throw new ArgumentNullException(nameof(environmentVariables));
        }

        /// <summary>
        /// Parse list of items. Expecting to be in the formal ENV_VAR_1=SECRET_NAME_1
        /// </summary>
        /// <param name="variables">Variables to parse and add to lookup</param>
        /// <returns>True if parsed ok, or false if any items failed to parse.</returns>
        public bool Parse(IList<string> variables)
        {
            bool valid = true;
            foreach (var item in variables)
            {
                var split = item.Split('=', 2);
                if (split.Length == 2)
                {
                    logger.LogDebug("Registered to fetch '{0}' from secret '{1}'", split[0].ToUpper(), split[1]);
                    variablesLookup.Add(split[0].ToUpper(), split[1]);
                }
                else
                {
                    logger.LogError("Unable to parse env from variable using value '{0}'.", item);
                    valid = false;
                }
            }

            return valid;
        }

        public async Task<string?> Resolve(string variable)
        {
            if(this.environmentVariables.Contains(variable))
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                return environmentVariables[variable].ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            if(variablesLookup.ContainsKey(variable))
            {
                var secret = variablesLookup[variable];
                this.logger.LogInformation("Resolving environment variable '{0}' from secret '{1}'.", variable, secret);
                var ss = await secretStore.CreateScopedSecret(secret);
                if (!await ss.Exists())
                {
                    this.logger.LogError("Secret '{0}' not found. Ignoring.", secret);
                }

                return await ss.GetSecret();
            }

            return null;
        }
    }
}
