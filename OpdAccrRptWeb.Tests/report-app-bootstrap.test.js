const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const source = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
const indexMarkup = fs.readFileSync("Views/Report/Index.cshtml", "utf8");
const routesSeen = [];
let mounted = false;
const initialState = {
    categories: [{ key: "outpatient", name: "Reports", groups: [{ name: "Group", reports: [{ code: "C1", name: "One" }] }] }],
    defaultStartDate: "2026-09-30",
    defaultEndDate: "2026-09-30"
};
const reportTemplate = { template: "<div>ok</div>" };
const window = {
    ReportComponents: {
        ReportTemplate: reportTemplate,
        C5Report: reportTemplate,
        C7Report: reportTemplate,
        C8Report: reportTemplate,
        C9Report: reportTemplate,
        C11Report: reportTemplate,
        C12Report: reportTemplate,
        M1DoctorDailyReport: reportTemplate,
        M2DoctorMonthlyReport: reportTemplate,
        M3OpdEmergencyDailyReport: reportTemplate,
        OpdPriceQuery: reportTemplate
    },
    clearTimeout() {},
    setTimeout() {}
};
const document = { getElementById() { return { textContent: JSON.stringify(initialState) }; } };
const Vue = { createApp() { return { use() { return this; }, mount() { mounted = true; } }; } };
const VueRouter = {
    createWebHistory() { return {}; },
    createRouter(options) {
        routesSeen.push(...options.routes);
        assert.ok(options.routes.every(route => route.component), "routes must never contain an unavailable component");
        return {};
    }
};

vm.runInNewContext(source, { window, document, Vue, VueRouter });

assert.equal(mounted, true);
assert.ok(routesSeen.some(route => route.path === "/Report/C1"));
assert.ok(routesSeen.some(route => route.path === "/medical-statistics/doctor-daily"));
assert.ok(routesSeen.some(route => route.path === "/data-query/opd-price"));
assert.doesNotMatch(indexMarkup, /<template>\s*<div class="breadcrumb">/, "router view must not be hidden inside an inert template element");
console.log("report app mounts all report routes");
