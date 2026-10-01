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
const context = {
    document: { getElementById: () => ({ textContent: JSON.stringify({ categories: [{ key:"a", name:"A", groups:[{ name:"G", reports:[{ code:"Q1", name:"Q" }] }] }] }) }) },
    window: { localStorage: { getItem: key => storage.get(key) ?? null, setItem: (key,value) => storage.set(key,value) }, ReportComponents: { OpdPriceQuery:{}, M1DoctorDailyReport:{}, M2DoctorMonthlyReport:{}, M3OpdEmergencyDailyReport:{}, ReportTemplate:{}, C5Report:{}, C7Report:{}, C8Report:{}, C9Report:{}, C11Report:{}, C12Report:{} }, setTimeout, clearTimeout },
    Vue: { createApp: value => { options=value; return { use(){ return this; }, mount(){} }; } },
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
assert.ok(options.beforeUnmount);
console.log("report layout contract tests passed");
