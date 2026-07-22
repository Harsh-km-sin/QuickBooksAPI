using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Services.Auth;
using QuickBooksAPI.Services.Reports;

namespace QuickBooksAPI.UnitTests.Reports;

public sealed class ReportTreeBuilderTests
{
    private static ReportLineRow Line(string path, string? parentPath, int rowNumber, decimal? amount, string rowType = "Data") =>
        new()
        {
            RowNumber = rowNumber,
            RowPath = path,
            ParentRowPath = parentPath,
            Depth = path.Count(c => c == '|'),
            RowType = rowType,
            Label = path.Split('|').Last(),
            Amount = amount
        };

    [Fact]
    public void BuildTree_NestsChildrenUnderTheirParent()
    {
        var lines = new[]
        {
            Line("Income", null, 0, 630m, "Section"),
            Line("Income|Landscaping", "Income", 1, 695m, "Section"),
            Line("Income|Landscaping|Plants", "Income|Landscaping", 2, 150m)
        };

        var roots = ReportReadService.BuildTree(lines);

        var income = Assert.Single(roots);
        var landscaping = Assert.Single(income.Children);
        var plants = Assert.Single(landscaping.Children);

        Assert.Equal("Income", income.Label);
        Assert.Equal(150m, plants.Amount);
    }

    [Fact]
    public void BuildTree_PreservesRowOrderWithinAParent()
    {
        var lines = new[]
        {
            Line("Income", null, 0, null, "Section"),
            Line("Income|B", "Income", 2, 2m),
            Line("Income|A", "Income", 1, 1m)
        };

        var income = Assert.Single(ReportReadService.BuildTree(lines));

        Assert.Equal(new[] { "A", "B" }, income.Children.Select(c => c.Label));
    }

    [Fact]
    public void BuildTree_MultipleTopLevelSectionsAllBecomeRoots()
    {
        var lines = new[]
        {
            Line("Income", null, 0, 630m, "Section"),
            Line("Expenses", null, 1, 100m, "Section"),
            Line("NetIncome", null, 2, 530m, "Section")
        };

        Assert.Equal(3, ReportReadService.BuildTree(lines).Count);
    }

    [Fact]
    public void BuildTree_RowWithMissingParentIsPromotedRatherThanDropped()
    {
        // Losing a row silently would understate the report, so an unresolvable parent is
        // surfaced as a root instead of discarded.
        var lines = new[]
        {
            Line("Income|Orphan", "Income", 0, 42m)
        };

        var roots = ReportReadService.BuildTree(lines);

        var orphan = Assert.Single(roots);
        Assert.Equal(42m, orphan.Amount);
    }

    [Fact]
    public void BuildTree_EmptyInput_ReturnsEmptyTree()
    {
        Assert.Empty(ReportReadService.BuildTree(Array.Empty<ReportLineRow>()));
    }

    [Fact]
    public void BuildTree_SummaryFlagIsSurfacedForRendering()
    {
        var line = Line("Income", null, 0, 630m, "Section");
        line.IsSummaryRow = 1;

        var node = Assert.Single(ReportReadService.BuildTree(new[] { line }));

        Assert.True(node.IsSummary);
    }
}

public sealed class QuickBooksCompanyMetadataParserTests
{
    [Theory]
    [InlineData("Cash", "Cash")]
    [InlineData("cash", "Cash")]
    [InlineData("Accrual", "Accrual")]
    // "Both" is a real QBO value but not a valid accounting_method, so it falls back to QBO's
    // own report default rather than being passed through and rejected by the API.
    [InlineData("Both", "Accrual")]
    [InlineData(null, "Accrual")]
    [InlineData("", "Accrual")]
    [InlineData("nonsense", "Accrual")]
    public void ParseAccountingBasis_NormalizesToAValidAccountingMethod(string? input, string expected)
    {
        Assert.Equal(expected, QuickBooksCompanyMetadataParser.ParseAccountingBasis(input));
    }

    [Theory]
    [InlineData("January", 1)]
    [InlineData("april", 4)]
    [InlineData("Dec", 12)]
    [InlineData("7", 7)]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("Smarch", 1)]
    [InlineData("13", 1)]
    [InlineData("0", 1)]
    public void ParseFiscalYearStartMonth_AcceptsNamesAndNumbersAndDefaultsSafely(string? input, int expected)
    {
        Assert.Equal(expected, QuickBooksCompanyMetadataParser.ParseFiscalYearStartMonth(input));
    }

    [Fact]
    public void ParseCompanyStartDate_ParsesTheQboFormat()
    {
        Assert.Equal(new DateTime(2019, 6, 3), QuickBooksCompanyMetadataParser.ParseCompanyStartDate("2019-06-03"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    public void ParseCompanyStartDate_ReturnsNullWhenUnusable(string? input)
    {
        // Null means "discover the range by walking back" rather than trusting a bad date.
        Assert.Null(QuickBooksCompanyMetadataParser.ParseCompanyStartDate(input));
    }
}
