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
        OpdPriceQuery: reportTemplate,
        MedicalRecordQuery: reportTemplate,
        RegistrationQuery: reportTemplate
    },
    clearTimeout() {},
    setTimeout() {}
};
const document = { getElementById() { return { textContent: JSON.stringify(initialState) }; } };
const Vue = { createApp() { return { directive(name, implementation) {
    assert.equal(name, "report-query-collapse");
    assert.equal(typeof implementation.mounted, "function");
    return this;
}, use() { return this; }, mount() { mounted = true; } }; } };
const VueRouter = {
    createWebHistory() { return {}; },
    createRouter(options) {
        routesSeen.push(...options.routes);
        assert.ok(options.routes.every(route => route.component || route.redirect), "routes must render or redirect");
        return {};
    }
};

vm.runInNewContext(source, { window, document, Vue, VueRouter });

assert.equal(mounted, true);
assert.ok(routesSeen.some(route => route.path === "/Report/C1"));
assert.ok(routesSeen.some(route => route.path === "/Report" && route.redirect === "/Report/C1"));
assert.ok(routesSeen.some(route => route.path === "/medical-statistics/doctor-daily"));
assert.ok(routesSeen.some(route => route.path === "/data-query/opd-price"));
assert.doesNotMatch(indexMarkup, /<template>\s*<div class="breadcrumb">/, "router view must not be hidden inside an inert template element");
const partialPaths = [...indexMarkup.matchAll(/Html\.PartialAsync\("([^"]+)"\)/g)].map(match => match[1]);
assert.ok(partialPaths.length > 0, "report shell must render report partials");
assert.ok(
    partialPaths.every(path => path.startsWith("~/Views/Report/")),
    "report shell partial paths must be rooted so dedicated report controllers can render it on a direct refresh"
);
const sharedSkeleton = fs.readFileSync("Views/Shared/_TableSkeleton.cshtml", "utf8");
assert.match(
    sharedSkeleton,
    /PartialAsync\("~\/Views\/Report\/_TableSkeleton\.cshtml"\)/,
    "shared skeleton fallback must delegate to the canonical report skeleton for dedicated-controller refreshes"
);
console.log("report app mounts all report routes");
