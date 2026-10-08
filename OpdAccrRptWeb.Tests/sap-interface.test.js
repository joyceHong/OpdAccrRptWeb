const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

global.window = {};
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/sap-interface.js", "utf8"));

const component = window.ReportComponents.SapInterface;
const context = {
    results: {
        SAPCASH: { status: "noData" },
        SAPCONS: { status: "failed", message: "執行失敗，該項資料已回復。" }
    }
};

assert.equal(component.methods.resultLabel.call(context, "SAPCASH"), "查無資料");
assert.equal(component.methods.resultLabel.call(context, "SAPCONS"), "執行失敗，該項資料已回復。");

console.log("sap-interface result label tests passed");
