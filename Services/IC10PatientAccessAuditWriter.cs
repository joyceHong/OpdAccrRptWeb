using OpdAccrRptWeb.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace OpdAccrRptWeb.Services;

public interface IC10PatientAccessAuditWriter
{
    void Write(C10DebtVisit visit, CancellationToken cancellationToken = default);
}

public sealed class C10AuditNotConfiguredException()
    : InvalidOperationException("C10 病歷存取稽核尚未設定；依預設政策拒絕產生報表。");

public sealed class FailClosedC10PatientAccessAuditWriter : IC10PatientAccessAuditWriter
{
    public void Write(C10DebtVisit visit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new C10AuditNotConfiguredException();
    }
}

public sealed class C10SerilogPatientAccessAuditWriter : IC10PatientAccessAuditWriter
{
    private readonly ILogger<C10SerilogPatientAccessAuditWriter> _logger;
    private readonly byte[] _fingerprintKey;

    public C10SerilogPatientAccessAuditWriter(
        ILogger<C10SerilogPatientAccessAuditWriter> logger)
        : this(logger, RandomNumberGenerator.GetBytes(32))
    {
    }

    internal C10SerilogPatientAccessAuditWriter(
        ILogger<C10SerilogPatientAccessAuditWriter> logger,
        byte[] fingerprintKey)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(fingerprintKey);
        if (fingerprintKey.Length < 16)
            throw new ArgumentException("稽核 fingerprint key 至少需要 16 bytes。", nameof(fingerprintKey));
        _logger = logger;
        _fingerprintKey = fingerprintKey.ToArray();
    }

    public void Write(C10DebtVisit visit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string patientFingerprint = Fingerprint(visit.MedicalRecordNumber);
        string visitFingerprint = Fingerprint(string.Join('|',
            visit.VisitKey.VisitDate,
            visit.VisitKey.VisitTime,
            visit.VisitKey.VisitRoom,
            visit.VisitKey.VisitNumber));
        _logger.LogInformation(
            "C10 patient access audit. PatientFingerprint={PatientFingerprint}, VisitFingerprint={VisitFingerprint}",
            patientFingerprint,
            visitFingerprint);
    }

    private string Fingerprint(string value)
    {
        byte[] hash = HMACSHA256.HashData(_fingerprintKey, Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..16];
    }
}
