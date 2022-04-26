using AzAcme.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzAcme.UnitTests.Helpers
{
    public class InMemoryScopedSecret : IScopedSecret
    {
        public static Task<IScopedSecret> Create(string value)
        {
            var ss = new InMemoryScopedSecret(value);
            return Task.FromResult((IScopedSecret)ss);
        }

        private string initialValue;

        public InMemoryScopedSecret(string initialValue)
        {
            this.initialValue = initialValue;
        }

        public Task CreateOrUpdate(string value)
        {
            this.initialValue = value;

            return Task.CompletedTask;
        }

        public Task<bool> Exists()
        {
            return Task.FromResult(initialValue != null);
        }

        public Task<string> GetSecret()
        {
            return Task.FromResult(initialValue);
        }
    }
}
