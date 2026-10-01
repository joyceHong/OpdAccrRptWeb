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
console.log("shared report autocomplete tests passed");
