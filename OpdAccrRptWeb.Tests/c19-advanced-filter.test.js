const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

global.window = { clearTimeout };
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8"));

const component = window.ReportComponents.ReportTemplate;
const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
const state = {
    ...component.data.call({
        selectedReport: { code: "C19" },
        defaultStartDate: "2026-11-13",
        defaultEndDate: "2026-11-13"
    }),
    isC19: true,
    currentPage: 3,
    $nextTick(callback) { callback(); },
    $refs: { reportFilterInput: { focus() {} } },
    async search() { this.searchCount = (this.searchCount ?? 0) + 1; }
};

assert.match(markup, /v-if="isC4 \|\| isC141 \|\| isC142 \|\| isC19" class="c22-source-filter-row"/);
assert.match(markup, /v-if="isC19" class="report-clearable-field"/);
assert.match(markup, /aria-label="清除護理站／床號前綴"/);
assert.doesNotMatch(markup, /v-model\.trim="form\.stationOrBedPrefix"/);
assert.equal(component.computed.hasAdvancedConditions.call({ reportConfiguration: { advancedConditions: false } }), false);

(async () => {
    component.methods.openReportFilters.call(state);
    state.reportFilterDraft.stationOrBedPrefix = "7A";
    component.methods.closeReportFilters.call(state);
    assert.equal(state.form.stationOrBedPrefix, "");

    component.methods.openReportFilters.call(state);
    state.reportFilterDraft.stationOrBedPrefix = " 7A ";
    await component.methods.applyReportFilters.call(state);
    assert.equal(state.form.stationOrBedPrefix, "7A");
    assert.equal(state.currentPage, 1);
    assert.equal(state.searchCount, 1);
    assert.equal(state.reportFilterOpen, false);

    component.methods.openReportFilters.call(state);
    assert.equal(state.reportFilterDraft.stationOrBedPrefix, "7A");
    state.reportFilterDraft.stationOrBedPrefix = "";
    await component.methods.applyReportFilters.call(state);
    assert.equal(state.form.stationOrBedPrefix, "");
    assert.equal(state.searchCount, 2);

    console.log("C19 advanced filter tests passed.");
})().catch(error => { console.error(error); process.exitCode = 1; });
