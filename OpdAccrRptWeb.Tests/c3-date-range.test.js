const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const window = {};
vm.runInNewContext(fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8"), { window });
const component = window.ReportComponents.ReportTemplate;
const configuration = window.ReportConfigurations.C3;

async function searchWithEndDate(endDate) {
    const context = {
        form: { startDate: "2026-05-14", endDate, encounterSource: "O" },
        isC3: true,
        isC211: false,
        isC212: false,
        isC23: false,
        isC141: false,
        isC142: false,
        isEndDateOnly: false,
        hasEncounterSource: true,
        reportConfiguration: configuration,
        selectedReport: { code: "C3" },
        validationMessage: "",
        async fetchResults() { this.fetchCount = (this.fetchCount || 0) + 1; }
    };
    await component.methods.search.call(context);
    return context;
}

(async () => {
    const eightDays = await searchWithEndDate("2026-05-21");
    assert.equal(eightDays.validationMessage, "C3 查詢日期區間最多 7 天（含起訖日）。");
    assert.equal(eightDays.fetchCount, undefined);

    const sevenDays = await searchWithEndDate("2026-05-20");
    assert.equal(sevenDays.validationMessage, "");
    assert.equal(sevenDays.fetchCount, 1);
    console.log("C3 date range validation passed");
})().catch(error => { console.error(error); process.exitCode = 1; });
