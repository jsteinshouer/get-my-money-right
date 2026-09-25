using static Api.Features.Import.Import;

namespace Api.Tests.Features.Import;

/// <summary>
/// Pure input-shape checks, below the HTTP seam: what a rule's words have to be before the question
/// of whether that rule already exists is even worth asking.
/// </summary>
public class CreateIgnoreRuleValidatorTests
{
    private readonly CreateIgnoreRule.Validator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void MatchText_ThatIsBlank_IsRejected(string matchText)
    {
        var result = _validator.Validate(new CreateIgnoreRule.Command(null, matchText, IgnoreMatchType.Contains));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIgnoreRule.Command.MatchText));
    }

    [Fact]
    public void MatchText_LongerThanADescriptionCouldBe_IsRejected()
    {
        var result = _validator.Validate(
            new CreateIgnoreRule.Command(null, new string('X', 201), IgnoreMatchType.Contains));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(IgnoreMatchType.Contains)]
    [InlineData(IgnoreMatchType.StartsWith)]
    [InlineData(IgnoreMatchType.Equals)]
    public void EachMatchType_IsAccepted(IgnoreMatchType matchType)
    {
        var result = _validator.Validate(new CreateIgnoreRule.Command(null, "AUTOPAY", matchType));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AMatchTypeThatIsNotOneOfThem_IsRejected()
    {
        var result = _validator.Validate(new CreateIgnoreRule.Command(null, "AUTOPAY", (IgnoreMatchType)99));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ARuleForEveryAccount_IsValid()
    {
        // A null AccountId is the global rule, not a missing field.
        var result = _validator.Validate(new CreateIgnoreRule.Command(null, "TRANSFER TO SAVINGS", IgnoreMatchType.StartsWith));

        Assert.True(result.IsValid);
    }
}
