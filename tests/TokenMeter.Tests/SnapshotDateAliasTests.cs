namespace TokenMeter.Tests;

/// <summary>
/// A month-year snapshot suffix (<c>-04-2024</c>) is a date, not a further version, and a family whose longer alias refused an
/// id does not hand it to a shorter one.
/// </summary>
public class SnapshotDateAliasTests
{
    [Theory]
    [InlineData("command-r-plus-04-2024", "command-r-plus")]
    [InlineData("command-r-03-2024", "command-r")]
    [InlineData("command-a-03-2025", "command-a")]
    [InlineData("command-r7b-12-2024", "command-r7b")]
    [InlineData("command-r-plus-08-2024", "command-r-plus-08-2024")]
    public void FindModel_AMonthYearSnapshot_IsTheModelItIsASnapshotOf(string id, string expected) =>
        Assert.Equal(expected, ModelCatalog.FindModel(id)?.ModelId);

    [Theory]
    [InlineData("command-r-plus-2")]
    [InlineData("command-r-plus-13-2024")] // no 13th month: a version-like continuation, not a date
    public void FindModel_ANewerVersionOfALongerFamily_IsUnknown_NotTheShorterFamily(string id) =>
        Assert.Null(ModelCatalog.FindModel(id));
}
