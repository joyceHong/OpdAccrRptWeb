using System.Globalization;
using System.Text;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C4MaterialReportRenderer : IC4MaterialReportRenderer
{
    public byte[] RenderPdf(C4MaterialPreviewViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        // A deliberately small, dependency-free PDF. Preview carries the full Unicode layout;
        // the PDF preserves row order and printable ASCII identifiers/totals without aggregation.
        var text = new StringBuilder("C4 Material Consignment Report ")
            .Append("DateB ").Append(model.Metadata.DateB).Append(" DateE ").Append(model.Metadata.DateE)
            .Append(" UserID ").Append(Safe(model.Metadata.UserId)).Append(" Today ").Append(model.Metadata.Today);
        text.Append(" | Columns: ID Type MaterialCode ClaimCode OrderName Unit ChargeCode Total SectionCode");
        foreach (var row in model.Rows)
            text.Append(" | ").Append(row.Id).Append(' ').Append(Safe(row.Type)).Append(' ')
                .Append(Safe(row.MaterialCode)).Append(' ').Append(Safe(row.ClaimCode)).Append(' ')
                .Append(Safe(row.OrderName)).Append(' ').Append(Safe(row.Unit)).Append(' ')
                .Append(Safe(row.ChargeCode)).Append(' ')
                .Append(row.Total.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(Safe(row.SectionCode));
        string content = $"BT /F1 9 Tf 36 780 Td ({Escape(text.ToString())}) Tj ET";
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        ];
        var output = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (int i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
            output.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }
        int xref = Encoding.ASCII.GetByteCount(output.ToString());
        output.Append("xref\n0 ").Append(objects.Length + 1).Append("\n0000000000 65535 f \n");
        foreach (int offset in offsets.Skip(1)) output.Append(offset.ToString("0000000000")).Append(" 00000 n \n");
        output.Append("trailer << /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string Safe(string? value) => string.Concat((value ?? string.Empty)
        .Where(character => character is >= ' ' and <= '~'));
    private static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
}
