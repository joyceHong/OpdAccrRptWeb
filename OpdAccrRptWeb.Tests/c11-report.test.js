const assert = require("node:assert/strict");
const fs = require("node:fs");

const view = fs.readFileSync("Views/Report/_C11ReceivablesCollectionReport.cshtml", "utf8");
const router = fs.readFileSync("wwwroot/js/report-app.js", "utf8");

assert.match(router, /C11:\s*window\.ReportComponents\.C11Report/);
assert.match(view, /source:\s*"OpdEr"/);
assert.match(view, /form\.endDate = form\.startDate/);
assert.match(view, /1～30天/);
assert.match(view, /30天以上/);
assert.match(view, /report\.groups\.length === 0/);
for (const sharedClass of ["page-title", "panel query-panel", "panel-title", "date-row", "source-fieldset", "form-actions", "panel result-panel", "result-heading", "table-wrap", "pagination", "empty-result"]) {
    assert.ok(view.includes(sharedClass), `missing shared report structure: ${sharedClass}`);
}
for (const heading of ["診別", "年度", "催收款", "1～30天", "30天以上", "本期欠款人數", "本期欠款金額"]) {
    assert.ok(view.includes(`<th>${heading}</th>`), `missing detail heading: ${heading}`);
}
assert.match(view, /pageSize:\s*10/);
assert.match(view, /<option :value="10">10 筆<\/option>/);
assert.match(view, /<option :value="30">30 筆<\/option>/);
assert.match(view, /<option :value="50">50 筆<\/option>/);
assert.match(view, /v-if="previewOpen"/);
assert.match(view, /openPreview\(\)/);
assert.match(view, /window\.print\(\)/);
assert.doesNotMatch(view, /<article v-if="report"/);
assert.equal((view.match(/type="date"/g) || []).length, 2);
assert.match(view, /props:\s*\["selectedReport",\s*"defaultStartDate",\s*"defaultEndDate"\]/);
assert.match(view, /toRocDate\(value\)/);
assert.match(view, /Number\(year\) - 1911/);
assert.match(view, /startDate:\s*this\.toRocDate\(this\.form\.startDate\)/);
assert.match(view, /endDate:\s*this\.toRocDate\(this\.form\.endDate\)/);
assert.match(view, /<partial name="_TableSkeleton"\s*\/>/);
assert.match(view, /class="visually-hidden" role="status">資料查詢中/);
assert.doesNotMatch(view, /⌛/);
assert.match(view, /^<template id="c11-report-template">/);
console.log("C11 report component contract tests passed");
