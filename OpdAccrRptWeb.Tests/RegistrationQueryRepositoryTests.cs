using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class RegistrationQueryRepositoryTests
{
    [Fact]
    public void BuildWhere_RegisteredWithInputOmitsStatusAndKeepsLegacyOrderShape()
    {
        using var command = new OracleCommand();
        string where = RegistrationQueryPredicates.BuildWhere(
            Filters(RegistrationQueryMode.Registered, registrationDate: "1151005"), command);

        Assert.Contains("R.chOp0Date = :RegDate", where, StringComparison.Ordinal);
        Assert.DoesNotContain("chOp0DigStat", where, StringComparison.Ordinal);
        Assert.DoesNotContain("chOp0DC", where, StringComparison.Ordinal);
        Assert.Contains("R.chOp0Room <> 'ZZZZ'", where, StringComparison.Ordinal);
        Assert.Contains("R.chOp0Room <> 'RRRR'", where, StringComparison.Ordinal);
        Assert.Contains("R.chOp0Room <> 'SSSS'", where, StringComparison.Ordinal);
        Assert.Contains("R.chOp0Room <> 'AAAA'", where, StringComparison.Ordinal);
        Assert.Contains("ROW_NUMBER() OVER", RegistrationQuerySql.PageBase, StringComparison.Ordinal);
        Assert.Contains("R.intOp0No DESC", RegistrationQuerySql.PageBase, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildWhere_SeenWithoutInputUsesUnseenFallback()
    {
        using var command = new OracleCommand();
        string where = RegistrationQueryPredicates.BuildWhere(
            Filters(RegistrationQueryMode.Seen), command);

        Assert.Contains("R.chOp0DigStat = '0' AND R.chOp0DC = '0'", where, StringComparison.Ordinal);
        Assert.DoesNotContain("R.chOp0DigStat = '3'", where, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildWhere_UnpricedWithInputPreservesQuoteFlgGrouping()
    {
        using var command = new OracleCommand();
        string where = RegistrationQueryPredicates.BuildWhere(
            Filters(RegistrationQueryMode.Unpriced, registrationDate: "1151005"), command);

        Assert.Contains("R.chOp0DigStat = '3' AND R.chOp0DC = '0'", where, StringComparison.Ordinal);
        Assert.Contains(
            "(R.chOp0QuoteFlg <> 'S' AND R.chOp0QuoteFlg <> 'U' AND R.chOp0QuoteFlg <> 'Y' OR R.chOp0QuoteFlg IS NULL)",
            where,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BuildWhere_MergedMedicalRecordsUseDistinctBindParameters()
    {
        using var command = new OracleCommand();
        string where = RegistrationQueryPredicates.BuildWhere(
            Filters(RegistrationQueryMode.Registered, medicalRecordNo: "AB123",
                medicalRecordNumbers: ["AB123", "AB124"]), command);

        Assert.Contains("R.chOp0PMrNo IN (:MrNo0, :MrNo1)", where, StringComparison.Ordinal);
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("AB123", command.Parameters["MrNo0"].Value);
        Assert.Equal("AB124", command.Parameters["MrNo1"].Value);
        Assert.DoesNotContain("AB123", where, StringComparison.Ordinal);
    }

    [Fact]
    public void SummarySqlKeepsSeparateLegacyQueriesAndRoomSource()
    {
        Assert.Contains("COUNT(*)", RegistrationQuerySql.SummaryTotal, StringComparison.Ordinal);
        Assert.Contains("chOp0DigStat = '3'", RegistrationQuerySql.SummarySeen, StringComparison.Ordinal);
        Assert.Contains("chOp0DC = '0'", RegistrationQuerySql.SummaryUnseen, StringComparison.Ordinal);
        Assert.Contains("chOp0DC = '1'", RegistrationQuerySql.SummaryCancelled, StringComparison.Ordinal);
        Assert.Contains("OpdRegRoomTbl", RegistrationQuerySql.SummaryRoomNumbers, StringComparison.Ordinal);
        Assert.Contains("intRegCurNextNo", RegistrationQuerySql.SummaryRoomNumbers, StringComparison.Ordinal);
        Assert.Contains("intRegPreNextNo", RegistrationQuerySql.SummaryRoomNumbers, StringComparison.Ordinal);
        Assert.Contains("GenDoctorTbl", RegistrationQuerySql.DoctorOptions, StringComparison.Ordinal);
        Assert.Contains("ESCAPE '\\'", RegistrationQuerySql.DoctorOptions, StringComparison.Ordinal);
        Assert.DoesNotContain("ESCAPE '\\\\'", RegistrationQuerySql.DoctorOptions, StringComparison.Ordinal);
        Assert.Contains(":ResultLimit", RegistrationQuerySql.DoctorOptions, StringComparison.Ordinal);
    }

    private static RegistrationQueryFilters Filters(
        RegistrationQueryMode mode,
        string registrationDate = "",
        string medicalRecordNo = "",
        string patientId = "",
        IReadOnlyList<string>? medicalRecordNumbers = null,
        string birthDate = "",
        string sectionNo = "",
        string room = "",
        string time = "",
        int? registrationNo = null,
        string doctorNo = "") =>
        new(mode, registrationDate, medicalRecordNo, patientId, birthDate, sectionNo, room,
            time, registrationNo, doctorNo, medicalRecordNumbers ?? []);
}
