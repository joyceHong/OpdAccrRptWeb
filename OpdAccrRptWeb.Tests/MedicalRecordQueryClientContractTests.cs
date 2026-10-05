namespace OpdAccrRptWeb.Tests;

public sealed class MedicalRecordQueryClientContractTests
{
    [Fact]
    public void ComponentDeclaresSelectedReportPropUsedByTemplate()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string componentPath = Path.Combine(
            projectRoot!.FullName,
            "wwwroot",
            "js",
            "reports",
            "medical-record-query.js");

        string component = File.ReadAllText(componentPath);

        Assert.Contains("props: [\"selectedReport\"]", component, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2TemplateUsesExplicitDetailActionWithoutInlineSelection()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string templatePath = Path.Combine(
            projectRoot!.FullName,
            "Views",
            "Report",
            "_MedicalRecordQuery.cshtml");

        string template = File.ReadAllText(templatePath);

        Assert.Contains("<th>明細</th>", template, StringComparison.Ordinal);
        Assert.Contains("medical-record-row-action", template, StringComparison.Ordinal);
        Assert.Contains("v-on:click.stop=\"openDetail(row, $event)\"", template, StringComparison.Ordinal);
        Assert.DoesNotContain("v-on:click=\"selectRow(row)\"", template, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"panel medical-record-detail\"", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2TemplatePlacesEmptyConditionValidationBelowQueryControls()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string templatePath = Path.Combine(
            projectRoot!.FullName,
            "Views",
            "Report",
            "_MedicalRecordQuery.cshtml");

        string template = File.ReadAllText(templatePath);

        Assert.Contains("<p v-if=\"conditionError\" class=\"validation-message\" role=\"alert\">{{ conditionError }}</p>", template, StringComparison.Ordinal);
        Assert.DoesNotContain("v-if=\"error && !searched\"", template, StringComparison.Ordinal);
        Assert.Contains("v-else-if=\"error && searched\"", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2ComponentSeparatesEmptyConditionValidationFromResultErrors()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string componentPath = Path.Combine(
            projectRoot!.FullName,
            "wwwroot",
            "js",
            "reports",
            "medical-record-query.js");

        string component = File.ReadAllText(componentPath);
        int searchStart = component.IndexOf("async search()", StringComparison.Ordinal);
        int fetchStart = component.IndexOf("async fetchRecords", searchStart, StringComparison.Ordinal);

        Assert.True(searchStart >= 0);
        Assert.True(fetchStart > searchStart);

        string searchMethod = component[searchStart..fetchStart];
        Assert.Contains("conditionError: \"\"", component, StringComparison.Ordinal);
        Assert.Contains("this.conditionError = \"\";", searchMethod, StringComparison.Ordinal);
        Assert.Contains("this.searched = false;", searchMethod, StringComparison.Ordinal);
        Assert.Contains("this.conditionError = \"請輸入任一條件\";", searchMethod, StringComparison.Ordinal);
        Assert.Contains("請輸入任一條件", component, StringComparison.Ordinal);
        Assert.Contains("this.error = \"\";", searchMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("fetch(", searchMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2TemplateDefinesGroupedDetailModal()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string templatePath = Path.Combine(
            projectRoot!.FullName,
            "Views",
            "Report",
            "_MedicalRecordQuery.cshtml");

        string template = File.ReadAllText(templatePath);

        Assert.Contains("class=\"medical-record-modal panel\"", template, StringComparison.Ordinal);
        Assert.Contains("role=\"dialog\"", template, StringComparison.Ordinal);
        Assert.Contains("aria-modal=\"true\"", template, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"medical-record-modal-title\"", template, StringComparison.Ordinal);
        Assert.Contains("<h3>基本資料</h3>", template, StringComparison.Ordinal);
        Assert.Contains("<h3>聯絡與地址</h3>", template, StringComparison.Ordinal);
        Assert.Contains("<h3>其他資料</h3>", template, StringComparison.Ordinal);
        Assert.Contains("<h3>關聯與欠款資訊</h3>", template, StringComparison.Ordinal);
        Assert.Contains("modalDetail.debtTotal", template, StringComparison.Ordinal);
        Assert.Contains("modalDetail.spouseId", template, StringComparison.Ordinal);
        Assert.Contains("modalDetail.fatherId", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2ComponentPreservesResultStateWhenClosingModal()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string componentPath = Path.Combine(
            projectRoot!.FullName,
            "wwwroot",
            "js",
            "reports",
            "medical-record-query.js");

        string component = File.ReadAllText(componentPath);
        int closeStart = component.IndexOf("closeModal(restoreFocus = true)", StringComparison.Ordinal);
        int clearStart = component.IndexOf("clearModal()", closeStart, StringComparison.Ordinal);

        Assert.True(closeStart >= 0);
        Assert.True(clearStart > closeStart);

        string closeMethod = component[closeStart..clearStart];
        Assert.DoesNotContain("this.rows =", closeMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("this.snapshot =", closeMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("fetch(", closeMethod, StringComparison.Ordinal);
        Assert.Contains("this.clearModal();", component, StringComparison.Ordinal);
        Assert.Contains("async goToPage(page)", component, StringComparison.Ordinal);
        Assert.Contains("async changePageSize()", component, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2ComponentSurfacesSafeDetailFailuresWithoutRawResponseBody()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string componentPath = Path.Combine(
            projectRoot!.FullName,
            "wwwroot",
            "js",
            "reports",
            "medical-record-query.js");

        string component = File.ReadAllText(componentPath);

        Assert.Contains("if (!response.ok) throw new Error(await this.message(response));", component, StringComparison.Ordinal);
        Assert.Contains("this.modalDetail = null;", component, StringComparison.Ordinal);
        Assert.Contains("this.modalError = error.message || \"病歷明細暫時無法使用。\";", component, StringComparison.Ordinal);
        Assert.Contains("const fallback = \"病歷查詢暫時無法使用。\";", component, StringComparison.Ordinal);
        Assert.DoesNotContain("return await response.text()", component, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2ComponentUsesDetailRequestContractAndGenerationGuard()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string componentPath = Path.Combine(
            projectRoot!.FullName,
            "wwwroot",
            "js",
            "reports",
            "medical-record-query.js");

        string component = File.ReadAllText(componentPath);

        Assert.Contains("modalOpen: false", component, StringComparison.Ordinal);
        Assert.Contains("modalRow: null", component, StringComparison.Ordinal);
        Assert.Contains("modalDetail: null", component, StringComparison.Ordinal);
        Assert.Contains("modalLoading: false", component, StringComparison.Ordinal);
        Assert.Contains("modalError: \"\"", component, StringComparison.Ordinal);
        Assert.Contains("modalGeneration: 0", component, StringComparison.Ordinal);
        Assert.Contains("lastTrigger: null", component, StringComparison.Ordinal);
        Assert.Contains("fetch(\"/data-query/medical-record/detail\"", component, StringComparison.Ordinal);
        Assert.Contains("body: JSON.stringify({ medicalRecordNo: row.medicalRecordNo })", component, StringComparison.Ordinal);
        Assert.Contains("const current = ++this.modalGeneration;", component, StringComparison.Ordinal);
        Assert.Contains("query === this.generation", component, StringComparison.Ordinal);
        Assert.Contains("this.modalRow?.medicalRecordNo === row.medicalRecordNo", component, StringComparison.Ordinal);
        Assert.Contains("this.$refs.modalClose?.focus()", component, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2TemplateDefinesAccessibleModalCloseControls()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string templatePath = Path.Combine(
            projectRoot!.FullName,
            "Views",
            "Report",
            "_MedicalRecordQuery.cshtml");

        string template = File.ReadAllText(templatePath);

        Assert.Contains("v-on:click.self=\"closeModal\"", template, StringComparison.Ordinal);
        Assert.Contains("v-on:keydown.esc.stop=\"closeModal\"", template, StringComparison.Ordinal);
        Assert.Contains("ref=\"modalClose\"", template, StringComparison.Ordinal);
        Assert.Contains("v-on:click=\"closeModal\"", template, StringComparison.Ordinal);
        Assert.Contains("class=\"medical-record-modal-body\"", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Q2StylesKeepDetailActionVisibleAndModalBodyScrollable()
    {
        DirectoryInfo? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string stylesPath = Path.Combine(projectRoot!.FullName, "wwwroot", "css", "site.css");
        string styles = File.ReadAllText(stylesPath);

        Assert.Contains("medical-record-table-wrap table th:last-child", styles, StringComparison.Ordinal);
        Assert.Contains("position:sticky;right:0", styles, StringComparison.Ordinal);
        Assert.Contains("medical-record-modal-body{min-height:0;flex:1 1 auto;overflow-x:hidden;overflow-y:auto", styles, StringComparison.Ordinal);
        Assert.Contains(".medical-record-modal{width:100%;max-height:calc(100vh - 16px)}", styles, StringComparison.Ordinal);
    }

    private static DirectoryInfo? FindProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OpdAccrRptWeb.csproj")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
