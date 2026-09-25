using static Api.Features.Import.Import;

namespace Api.Tests.Features.Import;

/// <summary>
/// Station 3 reads the whole file, so a separator or date format the reader cannot honour is
/// refused before two thousand rows are parsed with it rather than reported row by row.
/// </summary>
public class ReadAllRowsValidatorTests
{
    private readonly ReadAllRows.Validator _validator = new();

    private static ReadAllRows.Command Command(string delimiter = ",", string dateFormat = "MM/dd/yyyy") =>
        new(delimiter, true, dateFormat, "Posted Date", "Payee", "Amount", null, null);

    [Theory]
    [InlineData(",")]
    [InlineData(";")]
    [InlineData("\t")]
    [InlineData("|")]
    public void ASupportedSeparator_IsAccepted(string delimiter)
    {
        Assert.True(_validator.Validate(Command(delimiter: delimiter)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("::")]
    [InlineData("x")]
    public void ASeparatorTheReaderDoesNotKnow_IsRejected(string delimiter)
    {
        Assert.False(_validator.Validate(Command(delimiter: delimiter)).IsValid);
    }

    [Fact]
    public void ADateFormatTheReaderDoesNotKnow_IsRejected()
    {
        Assert.False(_validator.Validate(Command(dateFormat: "the fourteenth of August")).IsValid);
    }
}
