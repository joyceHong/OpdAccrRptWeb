const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const root = path.resolve(__dirname, "..");
const read = relativePath => fs.readFileSync(path.join(root, relativePath), "utf8");
const view = read("Views/Report/_RegistrationQuery.cshtml");
const script = read("wwwroot/js/reports/registration-query.js");
const app = read("wwwroot/js/report-app.js");
const shell = read("Views/Report/Index.cshtml");
const css = read("wwwroot/css/site.css");
const controller = read("Controllers/RegistrationQueryController.cs");

assert.match(view, /id="registration-query-template"/);
assert.match(view, /<h1>掛號資料查詢作業<\/h1>/);
assert.match(view, /class="panel query-panel"/);
assert.match(view, /class="advanced-filter-toggle"/);
assert.match(view, /type="date"/);
assert.match(view, /_TableSkeleton/);
assert.match(view, /class="pagination"/);
assert.match(view, /class="q3-summary-grid"/);
assert.match(view, /id-prefix="q3-new-section"/);
assert.doesNotMatch(view, /舊科別|q3-legacy-section/);
assert.doesNotMatch(view, /q3-select-column|checkbox|已選|isSelected|toggleRow/);
assert.doesNotMatch(view, /F6|F7|F8|F9/);
assert.match(script, /window\.ReportComponents\.RegistrationQuery/);
assert.match(script, /\/data-query\/registration\/query/);
assert.match(script, /\/data-query\/registration\/sections/);
assert.match(script, /\/data-query\/registration\/doctors/);
assert.doesNotMatch(script, /legacySection/);
assert.doesNotMatch(script, /selectedKeys|isSelected|toggleRow/);
assert.match(shell, /_RegistrationQuery\.cshtml/);
assert.match(shell, /js\/reports\/registration-query\.js/);
assert.match(app, /Q3:\s*window\.ReportComponents\.RegistrationQuery/);
assert.match(app, /"\/data-query\/registration"/);
assert.match(css, /\.q3-report \.q3-mode-fieldset/);
assert.match(css, /\.q3-summary-grid/);
assert.match(css, /\.q3-table-wrap table\{min-width:2320px\}/);
assert.doesNotMatch(css, /q3-select-column|q3-selected-row/);
assert.match(controller, /\[HttpPost\("query"\)\]/);
assert.match(controller, /\[HttpGet\("sections"\)\]/);
assert.match(controller, /\[HttpGet\("doctors"\)\]/);
assert.doesNotMatch(controller, /HttpPost\("print|HttpPost\("download|HttpPost\("f6|HttpPost\("f7|HttpPost\("f8|HttpPost\("f9/i);

const context = {
    window: { ReportComponents: { ReportAutocomplete: {} } },
    document: { querySelector: () => null }
};
vm.runInNewContext(script, context);
const component = context.window.ReportComponents.RegistrationQuery;
const data = component.data.call({ defaultStartDate: "2026-10-05" });
assert.equal(data.form.regDate, "2026-10-05");
assert.equal(data.form.mode, "registered");
const conditions = component.methods.conditions.call({ form: data.form });
assert.equal(conditions.mode, "registered");
assert.equal(conditions.regDate, "2026-10-05");

console.log("registration query UI smoke tests passed");
