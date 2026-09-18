using System.Text;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C4MaterialReportRendererTests
{
    [Fact]
    public void RenderPdf_PreservesInputOrderAndHasPdfSignature()
    {
        var model = new C4MaterialPreviewViewModel(new("115/09/01", "115/09/02", "tester",
            "115/09/17  10:30:00"), [
                new(1, "新品", "M001", "N1", "A", "EA", "C1", 2m, "S1"),
                new(2, "舊品", "M002", "N2", "B", "EA", "C2", 3m, "S2")]);
        byte[] actual = new C4MaterialReportRenderer().RenderPdf(model);
        string text = Encoding.ASCII.GetString(actual);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.True(text.IndexOf("M001", StringComparison.Ordinal) < text.IndexOf("M002", StringComparison.Ordinal));
        Assert.Contains("2 S1", text); Assert.Contains("3 S2", text);
    }
}

public sealed class C4ReportCatalogTests
{
    [Fact]
    public void Catalog_ExposesRenamedC4Entry()
    {
        ReportDefinitionViewModel report = new ReportCatalogService().GetReportIndex().Categories
            .SelectMany(category => category.Groups).SelectMany(group => group.Reports)
            .Single(item => item.Code == "C4");
        Assert.Equal("門急診材料寄售表", report.Name);
    }
}
