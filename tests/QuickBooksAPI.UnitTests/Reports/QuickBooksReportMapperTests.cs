using QuickBooksAPI.Application.Reports;
using QuickBooksAPI.Services.Reports;

namespace QuickBooksAPI.UnitTests.Reports;

/// <summary>
/// Exercises the report flattener against a real captured QBO sandbox response
/// (Craig's Design and Landscaping — see docs/REPORTS_SYNC_QBO_API_RESEARCH.md §4.1), not a
/// fabricated payload. The traps asserted here are the ones the real response actually contains.
/// </summary>
public sealed class QuickBooksReportMapperTests
{
    /// <summary>
    /// Real sandbox ProfitAndLoss response. Note the shapes it exercises: nesting three levels deep,
    /// "group" only on outermost sections, summary-only virtual rows with no Header/Rows, a section
    /// header that carries its own amount, and a negative leaf value.
    /// </summary>
    private const string RealSandboxProfitAndLossJson = """
    {
      "Header": {
        "Time": "2022-09-14T12:20:17-07:00",
        "ReportName": "ProfitAndLoss",
        "ReportBasis": "Accrual",
        "StartPeriod": "2022-06-01",
        "EndPeriod": "2022-09-30",
        "SummarizeColumnsBy": "Total",
        "Currency": "USD",
        "Customer": "1",
        "Option": [
          { "Name": "AccountingStandard", "Value": "GAAP" },
          { "Name": "NoReportData", "Value": "false" }
        ]
      },
      "Columns": {
        "Column": [
          { "ColTitle": "", "ColType": "Account", "MetaData": [{ "Name": "ColKey", "Value": "account" }] },
          { "ColTitle": "Total", "ColType": "Money", "MetaData": [{ "Name": "ColKey", "Value": "total" }] }
        ]
      },
      "Rows": {
        "Row": [
          {
            "Header": { "ColData": [{ "value": "Income" }, { "value": "" }] },
            "Rows": {
              "Row": [
                {
                  "Header": { "ColData": [{ "value": "Landscaping Services", "id": "45" }, { "value": "220.00" }] },
                  "Rows": {
                    "Row": [
                      {
                        "Header": { "ColData": [{ "value": "Job Materials", "id": "46" }, { "value": "" }] },
                        "Rows": {
                          "Row": [
                            { "ColData": [{ "value": "Fountains and Garden Lighting", "id": "48" }, { "value": "275.00" }], "type": "Data" },
                            { "ColData": [{ "value": "Plants and Soil", "id": "49" }, { "value": "150.00" }], "type": "Data" }
                          ]
                        },
                        "Summary": { "ColData": [{ "value": "Total Job Materials" }, { "value": "425.00" }] },
                        "type": "Section"
                      },
                      {
                        "Header": { "ColData": [{ "value": "Labor", "id": "51" }, { "value": "" }] },
                        "Rows": { "Row": [{ "ColData": [{ "value": "Maintenance and Repair", "id": "53" }, { "value": "50.00" }], "type": "Data" }] },
                        "Summary": { "ColData": [{ "value": "Total Labor" }, { "value": "50.00" }] },
                        "type": "Section"
                      }
                    ]
                  },
                  "Summary": { "ColData": [{ "value": "Total Landscaping Services" }, { "value": "695.00" }] },
                  "type": "Section"
                },
                { "ColData": [{ "value": "Pest Control Services", "id": "54" }, { "value": "-65.00" }], "type": "Data" }
              ]
            },
            "Summary": { "ColData": [{ "value": "Total Income" }, { "value": "630.00" }] },
            "type": "Section",
            "group": "Income"
          },
          { "Summary": { "ColData": [{ "value": "Gross Profit" }, { "value": "630.00" }] }, "type": "Section", "group": "GrossProfit" },
          {
            "Header": { "ColData": [{ "value": "Expenses" }, { "value": "" }] },
            "Summary": { "ColData": [{ "value": "Total Expenses" }, { "value": "" }] },
            "type": "Section",
            "group": "Expenses"
          },
          { "Summary": { "ColData": [{ "value": "Net Operating Income" }, { "value": "630.00" }] }, "type": "Section", "group": "NetOperatingIncome" },
          { "Summary": { "ColData": [{ "value": "Net Income" }, { "value": "630.00" }] }, "type": "Section", "group": "NetIncome" }
        ]
      }
    }
    """;

    private static QuickBooksAPI.DataAccessLayer.DTOs.FlattenedReport FlattenSample() =>
        QuickBooksReportMapper.Flatten(
            RealSandboxProfitAndLossJson,
            userId: 7,
            realmId: "realm-1",
            reportType: ReportTypes.ProfitAndLoss,
            granularity: ReportGranularity.Month,
            accountingMethod: "Accrual",
            periodStart: new DateTime(2022, 6, 1),
            periodEnd: new DateTime(2022, 9, 30));

    [Fact]
    public void Flatten_RealPayload_PopulatesRunHeaderFields()
    {
        var result = FlattenSample();

        Assert.Equal(7, result.Run.UserId);
        Assert.Equal("realm-1", result.Run.RealmId);
        Assert.Equal(ReportTypes.ProfitAndLoss, result.Run.ReportType);
        Assert.Equal("USD", result.Run.Currency);
        Assert.False(result.Run.NoReportData);
        Assert.NotNull(result.Run.GeneratedAtUtc);
        Assert.False(string.IsNullOrWhiteSpace(result.Run.RawJson));
    }

    [Fact]
    public void Flatten_ProfitAndLoss_IsMarkedAsFlowSoItCanBeSummed()
    {
        Assert.Equal(ReportValueSemantics.Flow, FlattenSample().Run.ValueSemantics);
    }

    [Fact]
    public void Flatten_BalanceSheet_IsMarkedAsStockSoItIsNeverSummed()
    {
        var json = RealSandboxProfitAndLossJson.Replace("\"ProfitAndLoss\"", "\"BalanceSheet\"");

        var result = QuickBooksReportMapper.Flatten(
            json, 7, "realm-1", ReportTypes.BalanceSheet, ReportGranularity.Month, "Accrual",
            new DateTime(2022, 6, 1), new DateTime(2022, 9, 30));

        Assert.Equal(ReportValueSemantics.Stock, result.Run.ValueSemantics);
    }

    [Fact]
    public void Flatten_NestingGoesDeeperThanTwoLevels()
    {
        var result = FlattenSample();

        // Income > Landscaping Services > Job Materials > leaf = depth 3.
        Assert.Contains(result.Rows, r => r.Depth >= 3);
    }

    [Fact]
    public void Flatten_GroupIsPresentOnTopLevelSectionsOnly()
    {
        var result = FlattenSample();

        var income = result.Rows.Single(r => r.GroupName == "Income");
        Assert.Equal(0, income.Depth);

        // Nested sub-sections are still Sections but carry no group.
        var jobMaterials = result.Rows.Single(r => r.Label == "Job Materials");
        Assert.Equal("Section", jobMaterials.RowType);
        Assert.Null(jobMaterials.GroupName);
    }

    [Fact]
    public void Flatten_SummaryOnlyVirtualRowsAreEmittedWithoutChildren()
    {
        var result = FlattenSample();

        // GrossProfit/NetOperatingIncome/NetIncome have no Header and no Rows at all.
        foreach (var group in new[] { "GrossProfit", "NetOperatingIncome", "NetIncome" })
        {
            var row = result.Rows.Single(r => r.GroupName == group);
            Assert.Equal("Section", row.RowType);
            Assert.DoesNotContain(result.Rows, child => child.ParentRowPath == row.RowPath);
        }
    }

    [Fact]
    public void Flatten_SectionAmountComesFromItsSummarySubtotal()
    {
        var result = FlattenSample();

        var income = result.Rows.Single(r => r.GroupName == "Income");
        var incomeTotal = result.Values.Single(v => v.RowNumber == income.RowNumber && v.ColumnNumber == 1);

        Assert.Equal(630.00m, incomeTotal.Amount);
    }

    [Fact]
    public void Flatten_ParentAccountOwnPostingsAreKeptAlongsideItsSubtotal()
    {
        var result = FlattenSample();

        // "Landscaping Services" has a subtotal of 695.00 AND its own direct postings of 220.00.
        // Losing the latter would silently understate the report.
        var section = result.Rows.Single(r => r.Label == "Landscaping Services" && r.RowType == "Section");
        var ownPostings = result.Rows.Single(r => r.Label == "Landscaping Services" && r.RowType == "Data");

        var subtotal = result.Values.Single(v => v.RowNumber == section.RowNumber && v.ColumnNumber == 1);
        var own = result.Values.Single(v => v.RowNumber == ownPostings.RowNumber && v.ColumnNumber == 1);

        Assert.Equal(695.00m, subtotal.Amount);
        Assert.Equal(220.00m, own.Amount);
        Assert.Equal(section.RowPath, ownPostings.ParentRowPath);
    }

    [Fact]
    public void Flatten_NegativeLeafAmountsAreParsed()
    {
        var result = FlattenSample();

        var pestControl = result.Rows.Single(r => r.Label == "Pest Control Services");
        var value = result.Values.Single(v => v.RowNumber == pestControl.RowNumber && v.ColumnNumber == 1);

        Assert.Equal(-65.00m, value.Amount);
        Assert.Equal("-65.00", value.RawValue);
    }

    [Fact]
    public void Flatten_LeafRowsCarryTheQboAccountIdAsAJoinKey()
    {
        var result = FlattenSample();

        var plants = result.Rows.Single(r => r.Label == "Plants and Soil");
        Assert.Equal("49", plants.AccountQboId);
        Assert.Equal("Data", plants.RowType);
    }

    [Fact]
    public void Flatten_RowPathsAreUniqueSoRowsSurviveGroupingAcrossRuns()
    {
        var result = FlattenSample();

        var duplicates = result.Rows
            .GroupBy(r => r.RowPath)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Flatten_EveryChildParentPathResolvesToARealRow()
    {
        var result = FlattenSample();
        var paths = result.Rows.Select(r => r.RowPath).ToHashSet();

        var orphans = result.Rows
            .Where(r => r.ParentRowPath is not null && !paths.Contains(r.ParentRowPath))
            .ToList();

        Assert.Empty(orphans);
    }

    [Fact]
    public void Flatten_EmptyCellsAreSkippedRatherThanStoredAsZero()
    {
        var result = FlattenSample();

        // "Total Expenses" has an empty value in the real payload. Storing 0 would assert a fact
        // QBO never reported.
        var expenses = result.Rows.Single(r => r.GroupName == "Expenses");
        Assert.DoesNotContain(result.Values, v => v.RowNumber == expenses.RowNumber);
    }

    [Fact]
    public void Flatten_TotalColumnGetsNoPeriodSoItCannotBeDoubleCounted()
    {
        var result = FlattenSample();

        var totalColumn = result.Columns.Single(c => c.ColKey == "total");
        Assert.Null(totalColumn.ColPeriodStart);
        Assert.Null(totalColumn.ColPeriodEnd);

        var accountColumn = result.Columns.Single(c => c.ColKey == "account");
        Assert.Null(accountColumn.ColPeriodStart);
    }

    [Fact]
    public void Flatten_MonthlyColumnsTakeTheirPeriodFromMetadataNotTheDisplayTitle()
    {
        // ColTitle here is deliberately misleading; the MetaData dates are authoritative.
        const string monthlyJson = """
        {
          "Header": { "ReportName": "ProfitAndLoss", "Currency": "USD" },
          "Columns": {
            "Column": [
              { "ColTitle": "", "ColType": "Account", "MetaData": [{ "Name": "ColKey", "Value": "account" }] },
              { "ColTitle": "WRONG LABEL", "ColType": "Money",
                "MetaData": [
                  { "Name": "StartDate", "Value": "2024-03-01" },
                  { "Name": "EndDate", "Value": "2024-03-31" },
                  { "Name": "ColKey", "Value": "Mar 2024" }
                ] }
            ]
          },
          "Rows": { "Row": [] }
        }
        """;

        var result = QuickBooksReportMapper.Flatten(
            monthlyJson, 1, "realm", ReportTypes.ProfitAndLoss, ReportGranularity.Month, "Accrual",
            new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var monthColumn = result.Columns.Single(c => c.ColumnNumber == 1);
        Assert.Equal(new DateTime(2024, 3, 1), monthColumn.ColPeriodStart);
        Assert.Equal(new DateTime(2024, 3, 31), monthColumn.ColPeriodEnd);
    }

    [Fact]
    public void Flatten_NoReportDataOptionIsDetected()
    {
        const string emptyJson = """
        {
          "Header": {
            "ReportName": "ProfitAndLoss",
            "Option": [{ "Name": "NoReportData", "Value": "true" }]
          },
          "Columns": { "Column": [] },
          "Rows": { "Row": [] }
        }
        """;

        var result = QuickBooksReportMapper.Flatten(
            emptyJson, 1, "realm", ReportTypes.ProfitAndLoss, ReportGranularity.Month, "Accrual",
            new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        Assert.True(result.Run.NoReportData);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Flatten_EmptyJson_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            QuickBooksReportMapper.Flatten(
                "", 1, "realm", ReportTypes.ProfitAndLoss, ReportGranularity.Month, "Accrual",
                DateTime.Today, DateTime.Today));
    }
}
