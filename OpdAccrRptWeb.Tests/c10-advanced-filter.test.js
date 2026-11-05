const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

global.window = { clearTimeout };
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8"));

const component = window.ReportComponents.ReportTemplate;
const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
const state = {
    ...component.data.call({
        selectedReport: { code: "C10" },
        defaultStartDate: "2026-11-02",
        defaultEndDate: "2026-11-04"
    }),
    isC10: true, isC3: false, isC4: false, isC22: false,
    isC29: false, isC211: false, currentPage: 3,
    $nextTick(callback) { callback(); },
    $refs: { reportFilterInput: { focus() {} } },
    async search() { this.searchCount = (this.searchCount ?? 0) + 1; }
};

assert.match(markup, /v-if="isC3 \|\| isC10 \|\| isC22 \|\| isC29 \|\| isC211" class="c22-source-filter-row"/);
assert.match(markup, /v-model="reportFilterDraft\.roomScope"[^>]*:disabled="form\.encounterSource === 'Inpatient'"/);
assert.match(markup, /id="c10-filter-mrn" v-model\.trim="reportFilterDraft\.medicalRecordNo"/);
assert.match(markup, /aria-label="清除病歷號"/);
assert.doesNotMatch(markup, /v-if="isC24 \|\| isC10" class="advanced-grid"/);

(async () => {
    component.methods.openReportFilters.call(state);
    state.reportFilterDraft.roomScope = "Emergency";
    state.reportFilterDraft.medicalRecordNo = "T10ER0001";
    component.methods.closeReportFilters.call(state);
    assert.equal(state.form.roomScope, "All");
    assert.equal(state.form.medicalRecordNo, "");

    component.methods.openReportFilters.call(state);
    state.reportFilterDraft.roomScope = "Emergency";
    state.reportFilterDraft.medicalRecordNo = " T10ER0001 ";
    await component.methods.applyReportFilters.call(state);
    assert.equal(state.form.roomScope, "Emergency");
    assert.equal(state.form.medicalRecordNo, "T10ER0001");
    assert.equal(state.currentPage, 1);
    assert.equal(state.searchCount, 1);
    assert.equal(state.reportFilterOpen, false);

    state.form.encounterSource = "Inpatient";
    component.methods.openReportFilters.call(state);
    await component.methods.applyReportFilters.call(state);
    assert.equal(state.form.roomScope, "All");
    assert.equal(state.searchCount, 2);

    state.hasResults = true;
    state.isLoading = false;
    state.selectedReport = { code: "C10", canPrint: true };
    state.form.encounterSource = "OpdEr";
    state.form.roomScope = "Emergency";
    state.openLegacyPreview = async (url, payload) => { state.previewRequest = { url, payload }; };
    await component.methods.previewC10.call(state);
    assert.equal(state.previewRequest.url, "/Report/C10/Preview");
    assert.equal(state.previewRequest.payload.startDate, "2026-11-02");
    assert.equal(state.previewRequest.payload.endDate, "2026-11-04");
    assert.equal(state.previewRequest.payload.roomScope, "Emergency");
    assert.equal(state.previewRequest.payload.medicalRecordNo, "T10ER0001");

    let printCount = 0;
    window.print = () => { printCount++; };
    state.legacyPreviewOpen = true;
    component.methods.printLegacyPreview.call(state);
    assert.equal(printCount, 1);
    console.log("C10 advanced filter tests passed.");
})().catch(error => { console.error(error); process.exitCode = 1; });
