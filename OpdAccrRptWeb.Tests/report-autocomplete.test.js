const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const listeners = new Map();
global.window = {};
global.document = {
    addEventListener(name, callback) { listeners.set(name, callback); },
    removeEventListener(name) { listeners.delete(name); }
};
vm.runInThisContext(fs.readFileSync("wwwroot/js/components/report-autocomplete.js", "utf8"));
vm.runInThisContext(fs.readFileSync("wwwroot/js/components/report-contract-autocomplete.js", "utf8"));

const component = window.ReportComponents.ReportAutocomplete;
assert.match(component.template, /report-autocomplete-panel/);
assert.match(component.template, /role="combobox"/);
assert.match(component.template, /role="listbox"/);
assert.match(component.template, /option\.code/);
assert.match(component.template, /option\.label/);
assert.match(component.template, /option\.suffix/);

const options = [
    { value: "0201", code: "11510A", label: "心臟內科", suffix: "科別" },
    { value: "0202", code: "11520A", label: "胸腔內科", suffix: "科別" }
];
const emitted = [];
const context = { options, open: true, activeIndex: -1, $emit(...args) { emitted.push(args); } };
context.selectOption = component.methods.selectOption;
component.methods.onKeydown.call(context, { key: "ArrowDown", preventDefault() {} });
assert.equal(context.activeIndex, 0);
component.methods.onKeydown.call(context, { key: "Enter", preventDefault() {} });
assert.deepEqual(emitted.find(item => item[0] === "select")?.[1], options[0]);
component.methods.onKeydown.call(context, { key: "Escape", preventDefault() {} });
assert.equal(context.activeIndex, -1);

const outsideEvents = [];
component.methods.closeFromOutside.call({ $el: { contains: () => false }, $emit(...args) { outsideEvents.push(args); } }, { target: {} });
assert.deepEqual(outsideEvents[0], ["update:open", false]);

const contractComponent = window.ReportComponents.ReportContractAutocomplete;
const contractChoices = [
    { code: "02", name: "本院員工" },
    { code: "03", name: "關企員工" },
    { code: "TT", name: "研究經費" }
];
const contractEvents = [];
const contract = {
    options: contractChoices, query: "", currentCode: "", selectedCode: "", unresolved: false,
    open: false, $emit(...args) { contractEvents.push(args); }
};
for (const [name, method] of Object.entries(contractComponent.methods)) contract[name] = method;
contract.setCode("02");
assert.equal(contract.query, "02 本院員工");
assert.deepEqual(contractComponent.computed.filteredOptions.call(contract).map(item => item.code), ["02", "03", "TT"]);

contract.onInput("關企");
assert.equal(contract.isValid(), false);
assert.equal(contract.currentCode, "");
assert.deepEqual(contractComponent.computed.filteredOptions.call(contract).map(item => item.code), ["03"]);
assert.deepEqual(contractEvents.at(-2), ["update:modelValue", ""]);

contract.selectChoice({ code: "03", value: "03", label: "關企員工" });
assert.equal(contract.query, "03 關企員工");
assert.equal(contract.isValid(), true);
assert.deepEqual(contractEvents.at(-2), ["update:modelValue", "03"]);

contract.onInput("03X");
assert.equal(contract.selectedCode, "");
assert.equal(contract.currentCode, "03X");
assert.equal(contract.isValid(), true);
contract.onInput("");
assert.equal(contract.currentCode, "");
assert.equal(contract.isValid(), true);
contract.setCode("TT");
assert.equal(contract.query, "TT 研究經費");
assert.match(contractComponent.template, /report-autocomplete/);

window.ReportDateRangeMixin = {};
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8"));
const templateMethods = window.ReportComponents.ReportTemplate.methods;
const c29 = {
    isC22: false, isC29: true, form: { billingCode: "02", cashierUserId: "", cashierCashSortType: "Cashier" },
    reportFilterOpen: false, reportFilterContractValid: true,
    $refs: {
        reportFilterTitle: { focus() { c29.titleFocused = true; } },
        c29ContractSelector: { focus() { c29.contractFocused = true; } }
    }, $nextTick(callback) { callback(); },
    loadC29Contracts() { this.choiceLoadCalls = (this.choiceLoadCalls ?? 0) + 1; },
    async search() { this.searchCalls = (this.searchCalls ?? 0) + 1; }
};
templateMethods.openReportFilters.call(c29);
assert.equal(c29.reportFilterDraft.billingCode, "02");
assert.equal(c29.choiceLoadCalls, 1);
assert.equal(c29.titleFocused, true);
assert.equal(c29.contractFocused, undefined);
c29.reportFilterDraft.billingCode = "03";
templateMethods.closeReportFilters.call(c29);
assert.equal(c29.form.billingCode, "02");
c29.reportFilterOpen = true;
c29.reportFilterContractValid = false;
templateMethods.applyReportFilters.call(c29);
assert.equal(c29.form.billingCode, "02");
assert.equal(c29.reportFilterOpen, true);
c29.reportFilterContractValid = true;
templateMethods.applyReportFilters.call(c29).then(async () => {
    assert.equal(c29.form.billingCode, "03");
    assert.equal(c29.searchCalls, 1);
    assert.equal(c29.reportFilterOpen, false);

    const c29Preview = {
        isC29: true, selectedReport: { canPrint: true }, hasResults: true,
        isLoading: false, legacyPreviewLoading: false,
        form: { startDate: "2026-11-01", endDate: "2026-11-01", encounterSource: "Emergency", billingCode: "03" },
        async openLegacyPreview(url, payload) { this.preview = { url, payload }; }
    };
    await templateMethods.previewC29.call(c29Preview);
    assert.equal(c29Preview.preview.payload.billingCode, "03");
    assert.equal(c29Preview.preview.payload.contractCode, undefined);

    const previousFetch = global.fetch;
    global.fetch = async () => { throw new Error("unavailable"); };
    const c29Choices = {
        isC29: true, c29ContractLoading: false, c29ContractsLoaded: false, c29Contracts: []
    };
    await templateMethods.loadC29Contracts.call(c29Choices);
    assert.match(c29Choices.c29ContractError, /仍可直接輸入代碼/);
    assert.equal(c29Choices.c29ContractsLoaded, false);
    global.fetch = previousFetch;

    const c211 = {
        isC211: true, isC212: false, c211ContractValid: false,
        hasResults: true, legacyPreviewLoading: false,
        form: { contractCode: "TT", endDate: "2026-11-01", encounterSource: "O" },
        async openLegacyPreview(url, payload) { this.preview = { url, payload }; }
    };
    await templateMethods.search.call(c211);
    assert.match(c211.validationMessage, /請從建議清單選擇/);
    await templateMethods.previewC211.call(c211);
    assert.equal(c211.preview, undefined);
    c211.c211ContractValid = true;
    await templateMethods.previewC211.call(c211);
    assert.equal(c211.preview.payload.contractCode, "TT");
    console.log("shared report autocomplete tests passed");
});
