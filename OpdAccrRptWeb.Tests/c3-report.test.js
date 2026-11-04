const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

global.window = {};
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8"));
const component = window.ReportComponents.ReportTemplate;
const config = window.ReportConfigurations.C3;

assert.equal(config.serverPaged, true);
assert.equal(config.encounterSource.defaultValue, "O");
const initial = component.data.call({
    selectedReport: { code: "C3" }, defaultStartDate: "2026-05-14", defaultEndDate: "2026-05-15"
});
assert.equal(initial.form.detailType, 0);
assert.equal(initial.form.logisticsType, 0);

const app = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
const reportTemplateScript = fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8");
const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
const css = fs.readFileSync("wwwroot/css/site.css", "utf8");
const preview = fs.readFileSync("Views/Report/_C3NursingStationChargePreview.cshtml", "utf8");
assert.match(app, /C3:\s*window\.ReportComponents\.ReportTemplate/);
assert.match(reportTemplateScript, /source:\s*this\.isC3\s*\|\|/, "C3 queries must send the selected care source");
assert.match(reportTemplateScript, /typeof problem === "string"/,
    "plain-text validation responses must be shown to the user");
assert.match(markup, /v-else-if="isC3"[\s\S]*openC3Preview/);
assert.match(markup, /<partial name="_TableSkeleton" \/>/);
assert.match(markup, /診間（逗號分隔）/);
assert.match(markup, /v-if="isC3 \|\| isC22 \|\| isC29 \|\| isC211" class="c22-source-filter-row"/);
assert.match(markup, /v-if="isC3" class="source-fieldset segmented-source-fieldset"[\s\S]*報表內容/);
assert.match(markup, /v-if="isC3"[\s\S]*reportFilterDraft\.logisticsType[\s\S]*reportFilterDraft\.departmentCode[\s\S]*reportFilterDraft\.roomCodes[\s\S]*reportFilterDraft\.chargeCodes/);
assert.match(css, /\.c3-query-panel form\{display:flex;align-items:flex-end;flex-wrap:wrap;gap:14px 24px\}/);
assert.match(css, /\.c3-query-panel form>\.date-row\{flex:1 1 520px;width:520px;max-width:760px;min-width:520px\}/);
assert.match(css, /\.c3-query-panel form>\.c22-source-filter-row\{display:flex;flex:0 0 auto;align-items:flex-end;flex-wrap:nowrap/);
assert.match(css, /@media\(max-width:680px\)\{\.c3-query-panel form>\.date-row[\s\S]*?\.c3-query-panel form>\.c22-source-filter-row\{display:grid;width:100%;grid-template-columns:repeat\(2,minmax\(0,1fr\)\)/);
assert.match(css, /body:has\(\.c3-preview-overlay\) \.c3-preview-overlay,body:has\(\.c3-preview-overlay\) \.c3-preview-overlay \*\{visibility:visible!important\}/,
    "C3 print paper must override the page-wide hidden visibility rule");
const context = {
    form: { ...initial.form },
    isC3: true, isC22: false, isC29: false, isC211: false,
    reportConfiguration: config, defaultStartDate: "2026-05-14", defaultEndDate: "2026-05-15",
    $nextTick: callback => callback(), $refs: { reportFilterInput: { focus() {} } },
    rows: [{ chargeCode: "OLD" }], hasSearched: true, serverTotalCount: 1, serverTotalPages: 1,
    search() { this.searchCount = (this.searchCount || 0) + 1; }
};
component.methods.openReportFilters.call(context);
context.reportFilterDraft.logisticsType = 1;
context.reportFilterDraft.departmentCode = "  0512  ";
context.reportFilterDraft.roomCodes = " C3O01 ";
context.reportFilterDraft.chargeCodes = " C3TLOG1 ";
component.methods.closeReportFilters.call(context);
assert.equal(context.form.roomCodes, "");
component.methods.openReportFilters.call(context);
Object.assign(context.reportFilterDraft, { logisticsType: 1, departmentCode: " 0512 ", roomCodes: " C3O01 ", chargeCodes: " C3TLOG1 " });
component.methods.applyReportFilters.call(context);
assert.equal(context.form.logisticsType, 1);
assert.equal(context.form.departmentCode, "0512");
assert.equal(context.form.roomCodes, "C3O01");
assert.equal(context.form.chargeCodes, "C3TLOG1");
assert.equal(component.computed.activeFilterCount.call(context), 4);
assert.equal(context.rows.length, 0);
assert.equal(context.currentPage, 1);
assert.equal(context.reportFilterOpen, false);
assert.equal(context.searchCount, 1);
assert.match(preview, /@@page \{ size: A4 landscape/);
assert.match(preview, /Model\.DetailType/);
assert.match(preview, /折扣別[\s\S]*批價碼[\s\S]*材料編號[\s\S]*計價日[\s\S]*總使用量/);
assert.match(preview, /row\.DctNo[\s\S]*加總：/);
const detailPages = component.computed.c3PreviewPages.call({
    isC3: true,
    c3Preview: { detailType: 1, rows: [
        { diagnose: "門診", section: "15OPD", dispensary: "門診護理站1", inventoryType: "物流", totalSum: 2 },
        { diagnose: "門診", section: "15OPD", dispensary: "門診護理站1", inventoryType: "AK庫", totalSum: 3 },
        { diagnose: "門診", section: "15OPD", dispensary: "門診護理站1", inventoryType: "物流", totalSum: 1 },
        { diagnose: "門診", section: "15011", dispensary: "急診護理站", inventoryType: "物流", totalSum: 4 }
    ] }
});
assert.equal(detailPages.length, 2);
assert.equal(detailPages[0].station.section, "15011");
assert.equal(detailPages[1].station.section, "15OPD");
assert.deepEqual(detailPages[1].rows.map(row => row.inventoryType), ["AK庫", "物流", "物流"]);
assert.deepEqual(detailPages[1].rows.map(row => row.showInventoryTotal), [true, false, true]);
assert.deepEqual(detailPages[1].rows.map(row => row.inventoryTotal), ["3.00", "2.00", "3.00"]);
const summaryPages = component.computed.c3PreviewPages.call({
    isC3: true,
    c3Preview: { detailType: 0, rows: [
        { diagnose: "門診", section: "15015", dispensary: "血液透析病房", inventoryType: "物流", chargeCode: "T35F001", totalSum: 6 },
        { diagnose: "門診", section: "15012", dispensary: "門診護理站", inventoryType: "物流", chargeCode: "T35F001", totalSum: 2 },
        { diagnose: "門診", section: "15015", dispensary: "血液透析病房", inventoryType: "AK庫", chargeCode: "OTHER", totalSum: 1 }
    ] }
});
assert.deepEqual(summaryPages.map(page => page.station.section), ["15012", "15015"]);
assert.deepEqual(summaryPages.map(page => page.rows.reduce((total, row) => total + row.totalSum, 0)), [2, 7]);
assert.deepEqual(summaryPages[1].rows.map(row => row.inventoryType), ["AK庫", "物流"]);
const modalSummaryMeta = markup.match(/<div v-else class="c3-print-meta c3-summary-meta">[\s\S]*?<\/div>/)[0];
const standaloneSummaryMeta = preview.match(/<div class="meta summary-meta">[\s\S]*?<\/div>/)[0];
for (const summaryMeta of [modalSummaryMeta, standaloneSummaryMeta]) {
    assert.doesNotMatch(summaryMeta, /報表名稱：/);
    assert.match(summaryMeta, /c3-summary-diagnose[\s\S]*c3-summary-page[\s\S]*c3-summary-date[\s\S]*c3-summary-range[\s\S]*c3-summary-department/);
}
assert.match(markup, /總使用量<\/th><th class="number">領用量<\/th><th class="number">借用量<\/th>/);
assert.match(preview, /總使用量<\/th><th class="number">領用量<\/th><th class="number">借用量<\/th>/);
assert.match(markup, /Number\.isInteger\(Number\(row\.totalSum\)\)/);
assert.match(preview, /row\.TotalSum\.ToString\("0\.0#/);
assert.match(css, /\.c3-summary-meta \.c3-summary-date\{grid-column:1;grid-row:2;padding-left:4px\}/);
assert.match(css, /\.c3-print-meta\.c3-summary-meta \.c3-summary-diagnose\{grid-column:3;grid-row:1;text-align:right\}/);
assert.match(preview, /\.summary-meta \.c3-summary-date \{ grid-column: 1; grid-row: 2; padding-left: 4px; \}/);
console.log("c3 report tests passed");
