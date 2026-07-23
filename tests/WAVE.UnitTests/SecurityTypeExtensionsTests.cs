using WAVE.Domain.Networking;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Shared rule (domain + UI) that decides when the password and username/domain
/// fields should appear in the manual registration form.
/// </summary>
public class SecurityTypeExtensionsTests
{
    [Theory]
    [InlineData(SecurityType.Open, false)]
    [InlineData(SecurityType.Wpa2Personal, true)]
    [InlineData(SecurityType.Wpa3Personal, true)]
    [InlineData(SecurityType.Wpa2Enterprise, true)]
    [InlineData(SecurityType.Wpa3Enterprise, true)]
    public void RequiresCredential_MatchesExpectation(SecurityType security, bool expected) =>
        Assert.Equal(expected, security.RequiresCredential());

    [Theory]
    [InlineData(SecurityType.Open, false)]
    [InlineData(SecurityType.Wpa2Personal, false)]
    [InlineData(SecurityType.Wpa3Personal, false)]
    [InlineData(SecurityType.Wpa2Enterprise, true)]
    [InlineData(SecurityType.Wpa3Enterprise, true)]
    public void IsEnterprise_OnlyForEnterpriseTypes(SecurityType security, bool expected) =>
        Assert.Equal(expected, security.IsEnterprise());

    [Fact]
    public void Profile_DerivesFromSameRule()
    {
        var personal = new WifiNetworkProfile("S", "S", SecurityType.Wpa2Personal);
        var enterprise = new WifiNetworkProfile("S", "S", SecurityType.Wpa3Enterprise);
        var open = new WifiNetworkProfile("S", "S", SecurityType.Open);

        Assert.True(personal.RequiresCredential);
        Assert.False(personal.IsEnterprise);
        Assert.True(enterprise.IsEnterprise);
        Assert.False(open.RequiresCredential);
    }
}
