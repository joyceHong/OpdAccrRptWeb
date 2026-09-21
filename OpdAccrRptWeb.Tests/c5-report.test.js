const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const root = path.resolve(__dirname, "..");
const c5 = fs.readFileSync(path.join(root, "Views/Report/_C5ChargeQuantityReport.cshtml"), "utf8");
const template = fs.readFileSync(path.join(root, "Views/Report/_TemplateReport.cshtml"), "utf8");
const app = fs.readFileSync(path.join(root, "wwwroot/js/report-app.js"), "utf8");
for (const className of ["page-title","panel query-panel","panel-title","date-row","source-fieldset","advanced-grid","form-actions","panel result-panel","result-heading","table-wrap","empty-result","pagination"]) {
    assert.match(template, new RegExp(className.replace(" ", "\\s+")));
    assert.match(c5, new RegExp(className.replace(" ", "\\s+")));
}
assert.match(c5, /_TableSkeleton/);
assert.match(c5, /type="date"/);
assert.match(c5, /primary-button/);
assert.match(c5, /ghost-button/);
assert.doesNotMatch(c5, /skeleton-row/);
const script = fs.readFileSync(path.join(root, "wwwroot/js/reports/c5-report.js"), "utf8");
const window = {};
vm.runInNewContext(script, { window, document: {} });
const component = window.ReportComponents.C5Report;
assert.match(script, /form\.dataSource\s*===\s*1/);
assert.match(script, /form\.encounterType\s*=\s*0/);
assert.match(script, /form\.roomNo\s*=\s*""/);
assert.match(script, /RequestVerificationToken/);
assert.match(script, /pageNumber:\s*this\.currentPage/);
assert.match(c5, /class="report-autocomplete-panel"/);
assert.match(c5, /\{\{ item\.newCode \}\}/);
assert.match(c5, /\|\{\{ item\.displayName \}\}/);
assert.doesNotMatch(c5, /科別舊碼/);
assert.doesNotMatch(c5, /legacySectionCode/);
assert.match(c5, /<span>健保身份<\/span><select[^>]*v-model="form\.insuranceIdentityCode"/);
assert.match(c5, /<option value="">全部<\/option>/);
assert.match(c5, /:disabled="form\.dataSource===1 \|\| selectedReport\.code==='C6'"/);
for (const [value, label] of [["01", "一般民眾"], ["030", "健保"], ["035", "健保未帶卡"]]) {
    assert.match(c5, new RegExp(`<option value="${value}">${value} ${label}</option>`));
}
assert.equal((c5.match(/<option value="(?:01|030|035)">/g) ?? []).length, 3);
assert.match(script, /newOrganizationUnitCode:\s*selectedCode\s*\|\|\s*directCode/);
assert.match(script, /organization-units\?query=/);
const initial = component.methods.initialForm.call({ defaultStartDate: "2026-09-01", defaultEndDate: "2026-09-02", selectedReport: { code: "C5" } });
assert.equal(initial.insuranceIdentityCode, "");
assert.equal(initial.legacySectionCode, undefined);
const selectionState = { form: initial, organizations: [{ newCode: " 11910 ", displayName: "急診" }], currentPage: 3 };
component.methods.selectOrganization.call(selectionState, selectionState.organizations[0]);
assert.equal(selectionState.form.organizationQuery, "11910|急診");
assert.equal(selectionState.form.newOrganizationUnitCode, "11910");
const payload = component.methods.payload.call({ form: selectionState.form, currentPage: 2, pageSize: 30 });
assert.equal(payload.newOrganizationUnitCode, "11910");
assert.equal(payload.insuranceIdentityCode, "");
assert.equal(payload.organizationQuery, undefined);
assert.equal(payload.legacySectionCode, undefined);
assert.match(app, /C6:\s*window\.ReportComponents\.C5Report/);
const c6Initial = component.methods.initialForm.call({ defaultStartDate: "2026-09-01", defaultEndDate: "2026-09-02", selectedReport: { code: "C6" } });
assert.equal(c6Initial.dataSource, 0);
assert.equal(c6Initial.detailType, 0);
assert.equal(c6Initial.encounterType, 1);
assert.equal(c6Initial.chargeKind, 1);
assert.equal(c6Initial.organizationQuery, "");
assert.equal(c6Initial.roomNo, "");
assert.equal(c6Initial.chargeCode, "");
assert.equal(c6Initial.insuranceIdentityCode, "");
const c6State = {
    defaultStartDate: "2026-09-01", defaultEndDate: "2026-09-02", selectedReport: { code: "C6" },
    form: { ...c6Initial, encounterType: 2, chargeKind: 0, roomNo: "OP_01" },
    initialForm() { return component.methods.initialForm.call(this); },
    organizations: [{}], rows: [{}], columns: [{}], totalCount: 1, totalPages: 1,
    currentPage: 2, pageSize: 30, hasSearched: true, validationMessage: "error"
};
component.methods.resetForm.call(c6State);
assert.deepEqual(c6State.form, c6Initial);
const c6Payload = component.methods.payload.call({ form: { ...c6Initial, roomNo: "stale" }, selectedReport: { code: "C6" }, currentPage: 1, pageSize: 10 });
assert.equal(c6Payload.roomNo, "");
console.log("c5 shared report UI tests passed");
