using AzAcme.Cli.Util;
using AzAcme.Core;
using AzAcme.UnitTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AzAcme.Cli.UnitTests.Util
{
    public class EnvironmentVariableResolverTests
    {
        public class Parse
        {
            private EnvironmentVariableResolver resolver;
            public Parse()
            {
                var secretStore = new Mock<ISecretStore>();
                resolver = new EnvironmentVariableResolver(NullLogger.Instance, secretStore.Object, new Dictionary<string,string>());
            }

            [Fact]
            public void Parse_ValidItems_Ok()
            {
                // arrange
                var items = new List<string>();
                items.Add("VAR=secret");
                items.Add("VAR2=secret2");

                // act
                var ok = resolver.Parse(items);

                // assert
                Assert.True(ok);
            }

            [Fact]
            public void Parse_InvalidValidItems_Fails()
            {
                // arrange
                var items = new List<string>();
                items.Add("VAR=secret");
                items.Add("VAR2secret2");

                // act
                var ok = resolver.Parse(items);

                // assert
                Assert.False(ok);
            }
        }

        public class Resolve
        {
            [Fact]
            public async Task Use_Env_When_Present()
            {
                // arrange
                var secretStore = new Mock<ISecretStore>();
                
                var envVarName = "ENV_VAR";
                var envVarParameterValue = "paramValue";

                var items = new List<string>();

                var envVariables = new Dictionary<string, string>();
                envVariables[envVarName] = envVarParameterValue;

                var resolver = new EnvironmentVariableResolver(NullLogger.Instance, secretStore.Object, envVariables);
                
                var ok = resolver.Parse(items);
                Assert.True(ok);

                // act
                var resolve = await resolver.Resolve(envVarName);

                // assert
                Assert.Equal(resolve, envVarParameterValue);
            }

            [Fact]
            public async Task Use_Env_When_Both_Present()
            {
                // arrange
                var secretStore = new Mock<ISecretStore>();

                var envVarName = "ENV_VAR";
                var envVarParameterValue = "paramValue";
                var secretName = "seccret1";
                var secretValue = "secretValue1";

                var items = new List<string>();
                items.Add(string.Format("{0}={1}", envVarName, secretName));

                var envVariables = new Dictionary<string, string>();
                envVariables[envVarName] = envVarParameterValue;

                secretStore.Setup(x => x.CreateScopedSecret(secretName)).Returns(InMemoryScopedSecret.Create(secretValue));

                var resolver = new EnvironmentVariableResolver(NullLogger.Instance, secretStore.Object, envVariables);

                var ok = resolver.Parse(items);
                Assert.True(ok);

                // act
                var resolve = await resolver.Resolve(envVarName);

                // assert
                Assert.Equal(resolve, envVarParameterValue);
            }

            [Fact]
            public async Task Use_Secret_When_No_Env_Var()
            {
                // arrange
                var secretStore = new Mock<ISecretStore>();

                var envVarName = "ENV_VAR";
                var secretName = "seccret1";
                var secretValue = "secretValue1";

                var items = new List<string>();
                items.Add(string.Format("{0}={1}", envVarName, secretName));

                var envVariables = new Dictionary<string, string>();

                secretStore.Setup(x => x.CreateScopedSecret(secretName)).Returns(InMemoryScopedSecret.Create(secretValue));

                var resolver = new EnvironmentVariableResolver(NullLogger.Instance, secretStore.Object, envVariables);

                var ok = resolver.Parse(items);
                Assert.True(ok);

                // act
                var resolve = await resolver.Resolve(envVarName);

                // assert
                Assert.Equal(resolve, secretValue);
            }

        }
    }
}
