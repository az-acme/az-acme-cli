using AzAcme.Core.Providers.Helpers;

namespace AzAcme.Core.UnitTests.Providers.Helpers;

public class DnsHelpersTests
{
    [Fact]
    public void DnsHelper_Single()
    {
        var zone = "azacme.dev";
        var record = DnsHelpers.DetermineTxtRecordName("1.azacme.dev",zone);
        
        Assert.Equal("_acme-challenge.1",record);
    }
    
    [Fact]
    public void DnsHelper_Wildcard()
    {
        var zone = "azacme.dev";
        var wildcardChallenge = DnsHelpers.DetermineTxtRecordName("*.azacme.dev",zone);
        
        Assert.Equal("_acme-challenge",wildcardChallenge);
    }
    
    [Fact]
    public void DnsHelper_MixedWildcardSan_DifferentChallenge()
    {
        var zone = "azacme.dev";
        var wildcardChallenge = DnsHelpers.DetermineTxtRecordName("*.azacme.dev",zone);
        var sanRecord = DnsHelpers.DetermineTxtRecordName("1.azacme.dev",zone);
        
        Assert.Equal("_acme-challenge",wildcardChallenge);
        Assert.Equal("_acme-challenge.1",sanRecord);
    }
    
    [Fact]
    public void DnsHelper_MixedWildcardSan_SameChallenge()
    {
        var zone = "demo.azacme.dev";
        var wildcardChallenge = DnsHelpers.DetermineTxtRecordName("*.1.demo.azacme.dev",zone);
        var sanRecord = DnsHelpers.DetermineTxtRecordName("1.demo.azacme.dev",zone);
        
        Assert.Equal("_acme-challenge.1",wildcardChallenge);
        Assert.Equal("_acme-challenge.1",sanRecord);
        Assert.Equal(sanRecord,wildcardChallenge);
    }
}