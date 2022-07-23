using AzAcme.Core.Providers.Helpers;

namespace AzAcme.Core.UnitTests.Providers.Helpers;

public class DnsHelpersTests
{
    [Fact]
    public void DnsHelper_Single()
    {
        var zone = "azacme.com";
        var record = DnsHelpers.DetermineTxtRecordName("1.azacme.com",zone);
        
        Assert.Equal("_acme-challenge.1",record);
    }
    
    [Fact]
    public void DnsHelper_Wildcard()
    {
        var zone = "azacme.com";
        var wildcardChallenge = DnsHelpers.DetermineTxtRecordName("*.azacme.com",zone);
        
        Assert.Equal("_acme-challenge",wildcardChallenge);
    }
    
    [Fact]
    public void DnsHelper_MixedWildcardSan_DifferentChallenge()
    {
        var zone = "azacme.com";
        var wildcardChallenge = DnsHelpers.DetermineTxtRecordName("*.azacme.com",zone);
        var sanRecord = DnsHelpers.DetermineTxtRecordName("1.azacme.com",zone);
        
        Assert.Equal("_acme-challenge",wildcardChallenge);
        Assert.Equal("_acme-challenge.1",sanRecord);
    }
}