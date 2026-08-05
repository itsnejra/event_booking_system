using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.ValueObjects;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("lejla@example.ba")]
    [InlineData("a.b+tag@sub.domain.co.uk")]
    public void Create_AcceptsWellFormedAddresses(string value)
    {
        Assert.Equal(value, EmailAddress.Create(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("no@domain")]
    [InlineData("two@@example.ba")]
    [InlineData("spaces in@example.ba")]
    public void Create_RejectsMalformedAddresses(string value)
    {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void Create_NormalisesCaseAndWhitespace()
    {
        Assert.Equal("lejla@example.ba", EmailAddress.Create("  Lejla@Example.BA  ").Value);
    }

    [Fact]
    public void Equality_IgnoresTheCaseItWasTypedIn()
    {
        Assert.Equal(EmailAddress.Create("A@B.ba"), EmailAddress.Create("a@b.ba"));
    }

    [Fact]
    public void Domain_IsThePartAfterTheAtSign()
    {
        Assert.Equal("example.ba", EmailAddress.Create("lejla@example.ba").Domain);
    }

    [Fact]
    public void TryCreate_ReportsFailureWithoutThrowing()
    {
        Assert.False(EmailAddress.TryCreate("nonsense", out var result));
        Assert.Null(result);
    }
}
