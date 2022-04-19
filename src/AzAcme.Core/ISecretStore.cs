namespace AzAcme.Core
{
    public interface ISecretStore
    {
        Task<IScopedSecret> CreateScopedSecret(string name);
    }
}
