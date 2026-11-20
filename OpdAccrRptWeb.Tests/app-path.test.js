const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const source = fs.readFileSync("wwwroot/js/app-path.js", "utf8");

for (const [base, expected] of [
    ["", "/Report/C1"],
    ["/OpdAccrRpt", "/OpdAccrRpt/Report/C1"],
    ["/femhF1482/OpdAccrRpt/", "/femhF1482/OpdAccrRpt/Report/C1"]
]) {
    const window = {};
    const document = { querySelector: () => ({ content: base }) };
    vm.runInNewContext(source, { window, document });
    assert.equal(window.siteUrl("/Report/C1"), expected);
    assert.equal(window.siteUrl("/Report/C1?x=1"), `${expected}?x=1`);
    assert.equal(window.siteUrl(expected), expected);
    assert.equal(window.siteUrl("/"), `${window.sitePathBase}/`);
}

console.log("application paths respect the deployment base");
