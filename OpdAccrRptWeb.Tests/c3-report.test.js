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
const preview = fs.readFileSync("Views/Report/_C3NursingStationChargePreview.cshtml", "utf8");
assert.match(app, /C3:\s*window\.ReportComponents\.ReportTemplate/);
assert.match(reportTemplateScript, /source:\s*this\.isC3\s*\|\|/, "C3 queries must send the selected care source");
assert.match(reportTemplateScript, /typeof problem === "string"/,
    "plain-text validation responses must be shown to the user");
assert.match(markup, /v-else-if="isC3"[\s\S]*openC3Preview/);
assert.match(markup, /<partial name="_TableSkeleton" \/>/);
assert.match(markup, /診間（逗號分隔）/);
assert.match(preview, /@@page \{ size: A4 landscape/);
assert.match(preview, /Model\.DetailType/);
console.log("c3 report tests passed");
