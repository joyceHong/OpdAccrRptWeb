const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const context = { window: {} };
vm.runInNewContext(fs.readFileSync("wwwroot/js/components/report-date-range.js", "utf8"), context);
const mixin = context.window.ReportDateRangeMixin;

const report = {
    form: { startDate: "2026-10-27", endDate: "2026-10-27" },
    ...mixin.data()
};
Object.assign(report, mixin.methods);

report.form.startDate = "2026-10-01";
assert.equal(report.syncDefaultEndDate(), true);
assert.equal(report.form.endDate, "2026-10-01");

report.form.endDate = "2026-10-31";
report.markEndDateChanged();
report.form.startDate = "2026-10-08";
assert.equal(report.syncDefaultEndDate(), false);
assert.equal(report.form.endDate, "2026-10-31");

report.form = { startDate: "2026-10-27", endDate: "2026-10-27" };
mixin.watch.form.call(report);
report.form.startDate = "2026-10-15";
assert.equal(report.syncDefaultEndDate(), true);
assert.equal(report.form.endDate, "2026-10-15");

console.log("Shared report date range behavior passed");
