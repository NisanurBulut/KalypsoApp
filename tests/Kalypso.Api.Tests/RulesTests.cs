using Kalypso.Contracts;

namespace Kalypso.Api.Tests;

public class RulesTests
{
    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://localhost:5299/x?y=1", true)]
    [InlineData("example.com", false)]
    [InlineData("mailto:me@example.com", false)]
    [InlineData("  ", false)]
    [InlineData(null, false)]
    public void ValidateUrl_AcceptsOnlyWebAddresses(string? url, bool valid) =>
        Assert.Equal(valid, LinkRules.ValidateUrl(url) is null);

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("sea-22", true)]
    [InlineData("abc", false)]
    [InlineData("this-name-is-way-too-long", false)]
    [InlineData("bad!char", false)]
    [InlineData("SWAGGER", false)]
    public void ValidateCode_FollowsTheNamingRules(string? code, bool valid) =>
        Assert.Equal(valid, LinkRules.ValidateCode(code) is null);

    [Fact]
    public void NewCode_UsesTheAlphabetAndIsRarelyRepeated()
    {
        var codes = Enumerable.Range(0, 500).Select(_ => CodeGenerator.NewCode()).ToList();

        Assert.All(codes, code =>
        {
            Assert.Equal(CodeGenerator.Length, code.Length);
            Assert.All(code, c => Assert.Contains(c, CodeGenerator.Alphabet));
        });
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }
}
