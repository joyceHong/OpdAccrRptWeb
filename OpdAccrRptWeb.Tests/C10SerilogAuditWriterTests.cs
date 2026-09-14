using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C10SerilogAuditWriterTests
{
    [Fact]
    public void SuccessfulWrite_EmitsDeidentifiedAuditAndAllowsCallerToContinue()
    {
        var logger = new CapturingLogger<C10SerilogPatientAccessAuditWriter>();
        var writer = new C10SerilogPatientAccessAuditWriter(
            logger,
            "unit-test-audit-key"u8.ToArray());

        writer.Write(Visit("SECRET-MRN"));

        CapturedLog entry = Assert.Single(logger.Entries);
        Assert.Contains("C10 patient access audit", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET-MRN", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(entry.Properties.Values,
            value => value?.ToString()?.Contains("SECRET-MRN", StringComparison.Ordinal) == true);
        Assert.Contains(entry.Properties, value => value.Key == "PatientFingerprint");
    }

    [Fact]
    public void Cancellation_PreventsAuditSuccess()
    {
        var writer = new C10SerilogPatientAccessAuditWriter(
            new CapturingLogger<C10SerilogPatientAccessAuditWriter>(),
            "unit-test-audit-key"u8.ToArray());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            writer.Write(Visit("M1"), cancellation.Token));
    }

    private static C10DebtVisit Visit(string medicalRecordNumber) => new(
        new C10VisitKey("1150901", "080000", "A", 1),
        "1", "O", medicalRecordNumber, "Patient", "Doctor", "Dept", null,
        "phone", "address", null, "contact", "relation", "contact phone", 10m);
}
