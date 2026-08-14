using System.Globalization;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class ReportCatalogService : IReportCatalogService
{
    public ReportIndexViewModel GetReportIndex()
    {
        string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new ReportIndexViewModel
        {
            DefaultStartDate = today,
            DefaultEndDate = today,
            Categories =
            [
                Category("outpatient", "門診批價統計報表",
                    Group("門診批價統計報表", Report("C1", "門急診手術核帳表")),
                    Group("會計報表",
                        Report("C21", "批價會計總表"), Report("C22", "櫃員現金表"),
                        Report("C23", "合約單位記帳表"), Report("C24", "欠繳補繳核帳表"),
                        Report("C25", "住院預收醫療費餘額明細表（月報）"), Report("C27", "輔具保證金餘額明細表（月報）"),
                        Report("C28", "住院應收帳款餘額明細表（月報）"), Report("C29", "合約單位收款明細表"),
                        Report("C211", "合約單位餘額明細表"), Report("C212", "骨庫餘額明細表"),
                        Report("C213", "收款員現金彙總表"), Report("C214", "門急診應收帳款餘額明細表")),
                    Group("計價、材料與明細報表",
                        Report("C3", "各護理站計價品彙總／明細表"), Report("C4", "門急診材料寄售表及庫存處理"),
                        Report("C5", "批價數量查詢表"), Report("C6", "急診特殊檢查治療查詢表"),
                        Report("C7", "門急診每日批價明細表"), Report("C8", "批價補帳明細表"), Report("C9", "維康耗材記帳月報表")),
                    Group("應收、收據、社服與催款報表",
                        Report("C10", "應收帳款記錄明細表"), Report("C11", "應收帳款催收款月報表"),
                        Report("C12", "病患醫療費用收據彙總證明"), Report("C13", "社服需求急診高危險群個案明細表"),
                        Report("C141", "住院應收帳款排行"), Report("C142", "病患欠醫院費用明細表"),
                        Report("C143", "會計餘額 VS 批價欠款表"), Report("C144", "欠款明細報表"),
                        Report("C15", "社工輔助器具保證金明細表"), Report("C16", "新北市醫療補助費用申請總表")),
                    Group("健康管理中心及其他報表",
                        Report("C171", "健康管理中心明細資料"), Report("C172", "健康管理中心金額統計"),
                        Report("C173", "健檢人次"), Report("C174", "健康管理中心合約單位記帳表"),
                        Report("C18", "醫療群會員急診住院查詢"), Report("C19", "安全針具使用情形查檢表"))),
                Category("medical", "醫務統計報表",
                    Group("醫務統計報表", Report("M1", "醫師看診人數日表"), Report("M2", "醫師看診人數月表"), Report("M3", "門急診日報表"))),
                Category("query", "資料查詢", Group("資料查詢"))
            ]
        };
    }

    private static ReportCategoryViewModel Category(string key, string name, params ReportGroupViewModel[] groups) =>
        new() { Key = key, Name = name, Groups = groups };

    private static ReportGroupViewModel Group(string name, params ReportDefinitionViewModel[] reports) =>
        new() { Name = name, Reports = reports };

    private static ReportDefinitionViewModel Report(string code, string name) => new() { Code = code, Name = name };
}
