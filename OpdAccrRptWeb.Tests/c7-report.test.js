const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const root = path.resolve(__dirname, "..");
const view = fs.readFileSync(path.join(root, "Views/Report/_C7DailyChargeDetailReport.cshtml"), "utf8");
const script = fs.readFileSync(path.join(root, "wwwroot/js/reports/c7-report.js"), "utf8");
const app = fs.readFileSync(path.join(root, "wwwroot/js/report-app.js"), "utf8");

for (const name of ["page-title", "panel query-panel", "date-row", "panel result-panel", "empty-result", "pagination", "_TableSkeleton", 'type="date"']) {
    assert.ok(view.includes(name));
}
assert.match(view, /v-model="form\.startTime" type="time"/);
assert.match(view, /v-model="form\.endTime" type="time"/);
assert.match(view, /c7-date-range[\s\S]*c7-charge-kind[\s\S]*c7-time-fields[\s\S]*c7-input-user/);
assert.match(view, /v-if="dateRangeInvalid" class="c7-range-error"/);
assert.match(view, /v-if="timeRangeInvalid" class="c7-range-error"/);
assert.match(view, /<th v-for="c in columns" :key="c\.key">\{\{ c\.label \}\}<\/th>/);
assert.doesNotMatch(view, /時間以 24 小時制選擇/);
assert.match(view, /<select v-model="form\.inputUserId" required/);
assert.match(view, /<option value="">請選擇輸入人員<\/option>/);
assert.doesNotMatch(view, /report-autocomplete/);
assert.match(script, /RequestVerificationToken/);
assert.match(script, /input-users\?endDate=/);
assert.match(script, /pageNumber: this\.currentPage/);
assert.match(script, /\/reports\/c7\/preview/);
assert.match(app, /C7:\s*window\.ReportComponents\.C7Report/);

const window = {};
vm.runInNewContext(script, { window, document: {} });
const component = window.ReportComponents.C7Report;
const form = component.methods.initialForm.call({ defaultStartDate: "2026-01-01", defaultEndDate: "2026-01-02" });
assert.equal(form.startTime, "00:00");
assert.equal(form.endTime, "23:59");
assert.equal(form.chargeKind, 0);
assert.equal(form.inputUserId, "");
const payload = component.methods.payload.call({ form, currentPage: 1, pageSize: 10 });
assert.equal(payload.startTime, "0000");
assert.equal(payload.endTime, "2359");
assert.equal(payload.inputUserId, "");

(async () => {
    const state = {
        form: { ...form, startTime: "18:00", endTime: "08:00" }, currentPage: 1,
        rows: [{ id: 1 }], columns: [{ key: "id" }], totalCount: 1, totalPages: 1,
        hasSearched: true, validationMessage: "", queryVersion: 0, previewOpen: true, previewHtml: "old"
    };
    Object.assign(state, component.methods);
    Object.defineProperty(state, "dateRangeInvalid", {
        get() { return component.computed.dateRangeInvalid.call(this); }
    });
    Object.defineProperty(state, "timeRangeInvalid", {
        get() { return component.computed.timeRangeInvalid.call(this); }
    });
    assert.equal(state.timeRangeInvalid, true);
    await state.search();
    assert.equal(state.validationMessage, "");
    assert.equal(state.rows.length, 0);
    assert.equal(state.previewOpen, false);
    state.form = { ...form, startDate: "2026-01-03", endDate: "2026-01-02" };
    assert.equal(state.dateRangeInvalid, true);
    assert.equal(state.timeRangeInvalid, false);
    await state.search();
    assert.equal(state.validationMessage, "");
    console.log("c7 report UI tests passed");
})().catch(error => { console.error(error); process.exitCode = 1; });
