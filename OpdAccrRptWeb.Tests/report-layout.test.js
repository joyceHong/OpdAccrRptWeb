const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const root = require("node:path").join(__dirname, "..");
const source = fs.readFileSync(`${root}/wwwroot/js/report-app.js`, "utf8");
const view = fs.readFileSync(`${root}/Views/Report/Index.cshtml`, "utf8");
const css = fs.readFileSync(`${root}/wwwroot/css/site.css`, "utf8");
const q1 = fs.readFileSync(`${root}/Views/Report/_OpdPriceQuery.cshtml`, "utf8");
const specializedViews = ["_C5ChargeQuantityReport.cshtml","_C7DailyChargeDetailReport.cshtml","_C8PatchBillDetailReport.cshtml","_C9MaterialAccountingMonthlyReport.cshtml","_C11ReceivablesCollectionReport.cshtml","_C12MedicalReceiptSummary.cshtml","_M1DoctorDailyReport.cshtml","_M2DoctorMonthlyReport.cshtml","_M3OpdEmergencyDailyReport.cshtml","_OpdPriceQuery.cshtml"]
    .map(name => fs.readFileSync(`${root}/Views/Report/${name}`, "utf8"));

const storage = new Map();
let options;
let queryCollapseDirective;
let createdButton;
const mutationObservers = [];
const context = {
    MutationObserver: class {
        constructor(callback) { this.callback = callback; this.disconnected = false; mutationObservers.push(this); }
        observe(element, options) { this.element = element; this.options = options; }
        disconnect() { this.disconnected = true; }
        notify() { this.callback(); }
    },
    document: {
        getElementById: () => ({ textContent: JSON.stringify({ categories: [{ key:"a", name:"A", groups:[{ name:"G", reports:[{ code:"Q1", name:"Q" }] }] }] }) }),
        createElement: tagName => {
            const attributes = new Map();
            const node = {
                setAttribute: (name, value) => attributes.set(name, value),
                getAttribute: name => attributes.get(name),
                addEventListener: (event, listener) => { if (event === "click") node.click = listener; },
                removeEventListener: (event, listener) => { if (event === "click" && node.click === listener) node.click = null; },
                replaceChildren: (...children) => { node.children = children; },
                focus: () => { node.focused = true; }
            };
            if (tagName === "button") createdButton = node;
            return node;
        }
    },
    window: { localStorage: { getItem: key => storage.get(key) ?? null, setItem: (key,value) => storage.set(key,value) }, ReportComponents: { OpdPriceQuery:{}, M1DoctorDailyReport:{}, M2DoctorMonthlyReport:{}, M3OpdEmergencyDailyReport:{}, ReportTemplate:{}, C5Report:{}, C7Report:{}, C8Report:{}, C9Report:{}, C11Report:{}, C12Report:{} }, setTimeout, clearTimeout },
    Vue: { createApp: value => { options=value; return { directive(name, value){ if (name === "report-query-collapse") queryCollapseDirective=value; return this; }, use(){ return this; }, mount(){} }; } },
    VueRouter: { createWebHistory(){}, createRouter(){ return {}; } },
    setTimeout, clearTimeout
};
vm.runInNewContext(source, context);
assert.equal(context.window.ReportLayout.readSidebarPreference(), "expanded");
storage.set("opd-report-sidebar:v1", "collapsed");
assert.equal(context.window.ReportLayout.readSidebarPreference(), "collapsed");
context.window.localStorage.getItem = () => { throw new Error("blocked"); };
assert.equal(context.window.ReportLayout.readSidebarPreference(), "expanded");
assert.equal(options.data().sidebarPinned, true);
assert.doesNotMatch(view, /sidebar-rail/);
assert.match(view, /sidebar-slide-trigger/);
assert.match(view, /&gt;&gt;/);
assert.match(view, /&lt;&lt;/);
assert.match(view, /sidebar-backdrop/);
assert.match(view, /v-on:keydown\.esc/);
assert.match(css, /sidebar-collapsed \{ grid-template-columns:0/);
assert.match(css, /translateX\(calc\(-1 \* var\(--sidebar-full-width\)\)\)/);
assert.doesNotMatch(source, /HOVER_OPEN_DELAY|openSidebarSoon/);
assert.match(css, /\.table-wrap thead th\{position:sticky/);
assert.match(q1, /query-basic-row/);
assert.match(q1, /active-filter-count/);
for (const reportView of specializedViews) assert.match(reportView, /panel query-panel/);
assert.match(css, /\.query-panel form\{display:flex;align-items:flex-end;flex-wrap:wrap;gap:10px;padding:12px 16px\}/);
assert.match(css, /\.query-panel \.primary-button\{background:#173f63/);
assert.match(css, /\.advanced-filter-toggle[^}]*background:transparent!important/);
assert.match(css, /\.query-panel \.advanced-filter-region\{flex:1 0 100%;width:100%/);
assert.match(css, /\.query-panel \.advanced-filter-toggle\{min-width:220px;[^}]*white-space:nowrap/);
assert.match(css, /\.query-panel \.advanced-filter-region\.open:not\(\.q1-advanced-collapse\)/);
assert.match(css, /\.advanced-filter-enter-active/);
assert.match(view, /content-inner/);
assert.match(css, /\.content-inner\{width:100%;max-width:none/);
assert.match(css, /\.content\{min-width:0;padding:20px 24px 48px\}/);
assert.match(css, /\.tree-children button \.report-name\{[^}]*text-overflow:ellipsis;white-space:nowrap/);
assert.match(css, /\.tree-group-name\{[^}]*text-overflow:ellipsis;white-space:nowrap/);
assert.match(view, /:title="group\.name"/);
assert.match(view, /:title="`\$\{report\.code\} \$\{report\.name\}`"/);
assert.match(css, /\.q1-report \.q1-basic-actions\{margin-left:0\}/);
assert.match(css, /\.page-title>div:first-child\{display:grid;grid-template-columns:auto minmax\(0,1fr\)/);
assert.match(css, /\.page-title h1\{grid-column:2;min-width:0;margin:0/);
assert.match(css, /\.page-title p\{grid-column:1\/-1;margin:6px 0 0\}/);
assert.match(css, /\.query-panel form\{gap:10px 14px;padding:16px 18px 28px\}/);
assert.match(view, /v-report-query-collapse="selectedReport\.code"/);
const catalog = fs.readFileSync(`${root}/Services/ReportCatalogService.cs`, "utf8");
const implementedCodes = Array.from(catalog.matchAll(/Report\("([CMQ]\d+)",/g), match => match[1])
    .filter(code => !["C141", "C142", "Q2", "Q3"].includes(code));
for (const code of implementedCodes) assert.match(source, new RegExp(`\\b${code}: window\\.ReportComponents\\.`), `${code} must use the shared report route shell`);
assert.ok(implementedCodes.includes("C10") && implementedCodes.includes("C11") && implementedCodes.includes("C12") && implementedCodes.includes("C13"));
assert.ok(implementedCodes.includes("C171") && implementedCodes.includes("C172") && implementedCodes.includes("C173") && implementedCodes.includes("C174"));
assert.match(css, /\.query-panel\.query-panel-collapsed \.panel-title\{border-bottom:0;border-radius:9px\}/);
assert.match(css, /\.query-panel form\[hidden\]\{display:none!important\}/);
assert.match(css, /\.query-condition-tag\{[^}]*border-radius:999px/);
assert.match(source, /const queryConditionTags = form =>/);
assert.match(source, /duration: 240, easing: "ease-in-out"/);
assert.match(source, /prefers-reduced-motion: reduce/);
assert.match(css, /\.report-filter:focus-within\{border-color:var\(--border\);box-shadow:inset 0 -2px #7f94a7\}/);
assert.match(css, /\.report-filter input:focus-visible\{outline:none\}/);
assert.ok(queryCollapseDirective);
const field = { value: "1908897" };
const medicalLabel = { querySelector: () => ({ textContent: "病歷號 *" }), textContent: "病歷號 *" };
Object.assign(field, { type: "text", tagName: "INPUT", disabled: false, closest: selector => selector === "label" ? medicalLabel : null });
const visitDate = { value: "2026-10-01", type: "date", tagName: "INPUT", disabled: false, closest: selector => selector === "label" ? { querySelector: () => ({ textContent: "就診日期" }) } : null };
const sourceOption = { value: "OpdEr", type: "radio", tagName: "INPUT", checked: true, disabled: false, closest: selector => selector === "label" ? { textContent: "門急診" } : selector === "fieldset" ? { querySelector: () => ({ textContent: "就醫來源 *" }) } : null };
const roomScope = { value: "Emergency", type: "select-one", tagName: "SELECT", disabled: false, selectedOptions: [{ textContent: "急診" }], closest: selector => selector === "label" ? { querySelector: () => ({ textContent: "門急診別" }) } : null };
const showDc = { value: "on", type: "checkbox", tagName: "INPUT", checked: true, disabled: false, closest: selector => selector === "label" ? { textContent: "顯示 DC", querySelector: () => null } : null };
const disabledRoom = { value: "2100", type: "text", tagName: "INPUT", disabled: true };
const formListeners = new Map();
const form = { id: "", hidden: false, inert: false, scrollHeight: 120, style: {}, contains: element => element === field,
    querySelectorAll: () => [field, visitDate, sourceOption, roomScope, showDc, disabledRoom], getBoundingClientRect: () => ({ height: 120 }),
    addEventListener: (name, listener) => formListeners.set(name, listener),
    removeEventListener: name => formListeners.delete(name) };
const classes = new Set();
const panel = { querySelector: selector => selector === "form" ? form : selector === ".panel-title" ? title : null, classList: { toggle: (name, enabled) => enabled ? classes.add(name) : classes.delete(name) } };
const title = { appendChild: button => { title.button = button; }, insertBefore: summary => { title.summary = summary; }, querySelector: () => null,
    contains: node => node === title.button || node === title.summary };
const queryRoot = { querySelector: selector => selector === ".query-panel" ? panel : null };
queryCollapseDirective.mounted(queryRoot, { value: "Q1" });
assert.equal(title.button, createdButton);
assert.equal(createdButton.getAttribute("aria-expanded"), "true");
assert.equal(createdButton.getAttribute("aria-controls"), "report-query-q1");
context.document.activeElement = field;
createdButton.click();
assert.equal(form.hidden, true);
assert.equal(form.inert, true);
assert.equal(createdButton.focused, true);
assert.equal(createdButton.getAttribute("aria-expanded"), "false");
assert.equal(classes.has("query-panel-collapsed"), true);
assert.deepEqual(Array.from(title.summary.children, node => node.textContent), ["病歷號：1908897", "就診日期：2026/10/01", "就醫來源：門急診", "門急診別：急診", "顯示 DC"]);
assert.equal(title.summary.hidden, false);
createdButton.click();
assert.equal(form.hidden, false);
assert.equal(form.inert, false);
assert.equal(field.value, "1908897");
assert.equal(classes.has("query-panel-collapsed"), false);
assert.equal(title.summary.hidden, true);
let animationDuration;
form.animate = (_frames, options) => { animationDuration = options.duration; return { cancel() {}, onfinish: null }; };
createdButton.click();
assert.equal(animationDuration, 240);
assert.equal(form.hidden, false);
assert.equal(form.inert, true);
context.window.matchMedia = () => ({ matches: true });
createdButton.click();
assert.equal(form.inert, false);
assert.equal(form.hidden, false);
queryCollapseDirective.beforeUnmount(queryRoot);
assert.equal(createdButton.click, null);
let latePanelReady = false;
const lateTitle = { appendChild: button => { lateTitle.button = button; }, insertBefore: summary => { lateTitle.summary = summary; }, querySelector: () => null,
    contains: node => node === lateTitle.button || node === lateTitle.summary };
const lateForm = { ...form, id: "", hidden: false, inert: false, style: {}, addEventListener() {}, removeEventListener() {} };
const latePanel = { querySelector: selector => selector === "form" ? lateForm : selector === ".panel-title" ? lateTitle : null, classList: { toggle() {} } };
let activePanel = latePanel;
const lateRoot = { querySelector: selector => latePanelReady && selector === ".query-panel" ? activePanel : null };
queryCollapseDirective.mounted(lateRoot, { value: "C11" });
const lateObserver = mutationObservers.at(-1);
assert.equal(lateObserver.element, lateRoot);
assert.equal(lateObserver.options.childList, true);
assert.equal(lateObserver.options.subtree, true);
latePanelReady = true;
lateObserver.notify();
assert.equal(lateObserver.disconnected, false);
assert.equal(lateTitle.button.getAttribute("aria-controls"), "report-query-c11");
const initializedButton = lateTitle.button;
lateTitle.button = null;
lateTitle.summary = null;
lateObserver.notify();
assert.equal(lateTitle.button, initializedButton, "Vue updates must not leave the collapse control missing");
assert.ok(lateTitle.summary, "Vue updates must not leave the condition tags missing");
initializedButton.click();
assert.equal(lateForm.hidden, true);
assert.deepEqual(Array.from(lateTitle.summary.children, node => node.textContent), ["病歷號：1908897", "就診日期：2026/10/01", "就醫來源：門急診", "門急診別：急診", "顯示 DC"]);
const replacementTitle = { appendChild: button => { replacementTitle.button = button; }, insertBefore: summary => { replacementTitle.summary = summary; }, querySelector: () => null,
    contains: node => node === replacementTitle.button || node === replacementTitle.summary };
const replacementForm = { ...lateForm, id: "", hidden: false, inert: false, style: {} };
activePanel = { querySelector: selector => selector === "form" ? replacementForm : selector === ".panel-title" ? replacementTitle : null, classList: { toggle() {} } };
lateObserver.notify();
assert.notEqual(replacementTitle.button, initializedButton, "replaced Vue title must receive a new control");
assert.equal(replacementTitle.button.getAttribute("aria-expanded"), "false");
assert.equal(replacementForm.hidden, true);
assert.equal(replacementForm.inert, true);
assert.deepEqual(Array.from(replacementTitle.summary.children, node => node.textContent), ["病歷號：1908897", "就診日期：2026/10/01", "就醫來源：門急診", "門急診別：急診", "顯示 DC"]);
queryCollapseDirective.beforeUnmount(lateRoot);
assert.equal(lateObserver.disconnected, true);
assert.equal(initializedButton.click, null);
assert.equal(replacementTitle.button.click, null);
const missingRoot = { querySelector: () => null };
queryCollapseDirective.mounted(missingRoot, { value: "C142" });
const missingObserver = mutationObservers.at(-1);
queryCollapseDirective.beforeUnmount(missingRoot);
assert.equal(missingObserver.disconnected, true);
for (const name of ["_C5ChargeQuantityReport.cshtml", "_M2DoctorMonthlyReport.cshtml", "_TemplateReport.cshtml", "_OpdPriceQuery.cshtml"]) {
    const markup = fs.readFileSync(`${root}/Views/Report/${name}`, "utf8");
    assert.match(markup, /advanced-filter-toggle/);
    assert.match(markup, /advanced-filter-region/);
}
for (const name of ["_C5ChargeQuantityReport.cshtml", "_M2DoctorMonthlyReport.cshtml", "_TemplateReport.cshtml"]) {
    const markup = fs.readFileSync(`${root}/Views/Report/${name}`, "utf8");
    assert.match(markup, /<transition name="advanced-filter">/);
    assert.match(markup, /v-show="advancedOpen"/);
}
assert.ok(options.beforeUnmount);
console.log("report layout contract tests passed");
