namespace AzAcme.Core
{
    public interface IScopedSecret
    {
        Task<bool> Exists();

        Task<string> GetSecret();

        Task CreateOrUpdate(string value);
    }
}
