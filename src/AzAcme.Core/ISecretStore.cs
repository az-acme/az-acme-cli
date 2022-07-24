namespace AzAcme.Core
{
    public interface ISecretStore
    {
        Task ValidateSecretName(string name);
        
        Task<IScopedSecret> CreateScopedSecret(string name);
    }
}
