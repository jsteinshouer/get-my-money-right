using static Api.Features.Import.Import;
using static Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Import;

/// <summary>
/// Pure input-shape checks, below the HTTP seam: what an assignment rule has to say before the
/// question of whether it already exists is even worth asking.
/// </summary>
public class CreateAssignmentRuleValidatorTests
{
    private readonly CreateAssignmentRule.Validator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void MatchText_ThatIsBlank_IsRejected(string matchText)
    {
        var result = _validator.Validate(
            new CreateAssignmentRule.Command(null, matchText, IgnoreMatchType.Contains, null, NeedWant.Need, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAssignmentRule.Command.MatchText));
    }

    [Fact]
    public void MatchText_LongerThanADescriptionCouldBe_IsRejected()
    {
        var result = _validator.Validate(new CreateAssignmentRule.Command(
            null, new string('X', 201), IgnoreMatchType.Contains, null, NeedWant.Need, []));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[0])]
    public void ARuleThatSetsNothing_IsRejected(int[]? tagIds)
    {
        var result = _validator.Validate(
            new CreateAssignmentRule.Command(null, "KROGER", IgnoreMatchType.Contains, null, null, tagIds));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Assignments");
    }

    [Fact]
    public void ARuleThatSetsOnlyACategory_IsValid()
    {
        var result = _validator.Validate(
            new CreateAssignmentRule.Command(null, "KROGER", IgnoreMatchType.Contains, 1, null, []));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ARuleThatSetsOnlyNeedWant_IsValid()
    {
        var result = _validator.Validate(
            new CreateAssignmentRule.Command(null, "KROGER", IgnoreMatchType.Contains, null, NeedWant.Want, []));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ARuleThatSetsOnlyATag_IsValid()
    {
        var result = _validator.Validate(
            new CreateAssignmentRule.Command(null, "DELTA", IgnoreMatchType.Contains, null, null, [1]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AMatchTypeThatIsNotOneOfThem_IsRejected()
    {
        var result = _validator.Validate(
            new CreateAssignmentRule.Command(null, "KROGER", (IgnoreMatchType)99, null, NeedWant.Need, []));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ANeedWantThatIsNotOneOfThem_IsRejected()
    {
        var result = _validator.Validate(
            new CreateAssignmentRule.Command(null, "KROGER", IgnoreMatchType.Contains, null, (NeedWant)99, []));

        Assert.False(result.IsValid);
    }
}
