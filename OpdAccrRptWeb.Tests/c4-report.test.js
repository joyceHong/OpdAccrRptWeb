const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const root = path.resolve(__dirname, "..");
const script = fs.readFileSync(path.join(root, "wwwroot/js/reports/report-template.js"), "utf8");
const view = fs.readFileSync(path.join(root, "Views/Report/_TemplateReport.cshtml"), "utf8");
const app = fs.readFileSync(path.join(root, "wwwroot/js/report-app.js"), "utf8");
const shell = fs.readFileSync(path.join(root, "Views/Report/Index.cshtml"), "utf8");
const styles = fs.readFileSync(path.join(root, "wwwroot/css/site.css"), "utf8");

function getC4Methods() {
    const window = { clearTimeout() {} };
    vm.runInNewContext(script, { window });
    return window.ReportComponents.ReportTemplate.methods;
}

test("C4 payload uses sectionPrefix and has no rebuild field", () => {
    assert.match(script, /sectionPrefix/);
    assert.doesNotMatch(script.match(/C4:[\s\S]*?C10:/)?.[0] ?? "", /chkReRun|ReRun|rebuild/i);
});

test("C4 uses shared loading, pagination and explicit document actions", () => {
    assert.match(view, /<partial name="_TableSkeleton" \/>/);
    assert.match(view, /aria-label="第一頁"/);
    assert.match(view, /aria-label="最後一頁"/);
    assert.match(view, /預覽／列印/);
    assert.doesNotMatch(view.match(/<template v-else-if="isC4">[\s\S]*?<\/template>/)?.[0] ?? "", /匯出 PDF/);
    assert.match(view, /type="date"/);
});

test("C4 stays inside the shared Report shell", () => {
    assert.match(app, /C4:\s*window\.ReportComponents\.ReportTemplate/);
    assert.doesNotMatch(app, /C4:[\s\S]{0,180}window\.location/);
    assert.match(shell, /class="app-header"/);
    assert.match(shell, /class="sidebar"/);
    assert.match(shell, /class="breadcrumb"/);
    assert.match(view, /v-if="isC4"/);
    assert.match(view, /onC4OrganizationInput/);
});

test("C4 organization autocomplete uses the shared floating panel", () => {
    assert.match(view, /<report-autocomplete/);
    assert.match(view, /:options="c4OrganizationOptions"/);
    assert.match(view, /selectC4OrganizationOption/);
    assert.doesNotMatch(view.match(/<div v-if="isC4" class="advanced-grid">[\s\S]*?<\/div>/)?.[0] ?? "", /<select/);
    assert.match(styles, /\.report-autocomplete-panel\{[^}]*position:absolute[^}]*width:100%/);
});

test("C4 advanced filter keeps edits in the dialog until apply and queries immediately", async () => {
    const methods = getC4Methods();
    const state = {
        isC4: true, isC3: false, isC22: false, isC29: false, isC211: false,
        form: { organizationQuery: "11910｜急診", sectionPrefix: "0201", cashierUserId: "",
            cashierCashSortType: "Cashier", billingCode: "", contractCode: "",
            logisticsType: 0, departmentCode: "", roomCodes: "", chargeCodes: "" },
        reportFilterOpen: false, c4Organizations: [], currentPage: 3,
        $nextTick(callback) { callback(); }, $refs: { reportFilterTitle: { focus() {} } },
        async search() { this.searchCount = (this.searchCount ?? 0) + 1; }
    };

    methods.openReportFilters.call(state);
    assert.equal(state.reportFilterDraft.organizationQuery, "11910｜急診");
    state.reportFilterDraft.organizationQuery = "12010｜外科";
    state.reportFilterDraft.sectionPrefix = "0301";
    assert.equal(state.form.sectionPrefix, "0201");
    methods.closeReportFilters.call(state);
    assert.equal(state.form.sectionPrefix, "0201");

    methods.openReportFilters.call(state);
    state.reportFilterDraft.organizationQuery = "12010｜外科";
    state.reportFilterDraft.sectionPrefix = "0301";
    await methods.applyReportFilters.call(state);
    assert.equal(state.form.sectionPrefix, "0301");
    assert.equal(state.currentPage, 1);
    assert.equal(state.searchCount, 1);
    assert.equal(state.reportFilterOpen, false);
});

test("C4 organization field loads choices when focused with no text", async () => {
    const requests = [];
    let pendingRequest;
    const window = {
        clearTimeout() {},
        setTimeout(callback) { pendingRequest = callback(); return 1; }
    };
    vm.runInNewContext(script, { window, fetch: async url => {
        requests.push(url);
        return { ok: true, json: async () => [{ legacyCode: "0201", newCode: "11910", displayName: "急診" }] };
    } });
    const methods = window.ReportComponents.ReportTemplate.methods;
    const state = {
        isC4: true, reportFilterOpen: true, c4OrganizationSearchGeneration: 0,
        c4Organizations: [], reportFilterDraft: { organizationQuery: "", sectionPrefix: "" }
    };
    state.searchC4Organizations = () => methods.searchC4Organizations.call(state);

    methods.setC4OrganizationOpen.call(state, true);
    await pendingRequest;

    assert.equal(state.c4OrganizationDropdownOpen, true);
    assert.equal(requests[0], "/reports/c4/organization-units?query=");
    assert.equal(state.c4Organizations[0]?.legacyCode, "0201");
});

test("C4 organization filter uses a selected legacy code or normalized direct input", () => {
    const methods = getC4Methods();
    const selected = { form: { sectionPrefix: " 0201 ", organizationQuery: "急診" } };

    assert.equal(methods.getC4SectionPrefix.call(selected), "0201");
    assert.equal(methods.getC4SectionPrefix.call({ form: { sectionPrefix: "", organizationQuery: "11910" } }), "");
    assert.match(script, /organization-units\/resolve\?newCode=/);
});

test("C4 organization selection synchronizes visible text and clears stale selection on edit", () => {
    const methods = getC4Methods();
    const state = {
        form: { sectionPrefix: "OLD", organizationQuery: "old" },
        c4Organizations: [{ legacyCode: "0201", newCode: "11910", displayName: "急診" }],
        currentPage: 3,
        searchC4Organizations() {}
    };

    methods.selectC4Organization.call(state, state.c4Organizations[0]);
    assert.deepEqual(state.form, { sectionPrefix: "0201", organizationQuery: "11910｜急診" });
    assert.equal(state.currentPage, 1);
    assert.equal(state.c4Organizations.length, 0);

    state.form.organizationQuery = "0281";
    methods.onC4OrganizationInput.call(state);
    assert.equal(state.form.sectionPrefix, "");

    state.c4Organizations = [{ legacyCode: "0301", newCode: "12010", displayName: "外科", source: "科別" }];
    methods.selectC4OrganizationOption.call({ ...state, selectC4Organization: item => methods.selectC4Organization.call(state, item) }, { raw: state.c4Organizations[0] });
    assert.deepEqual(state.form, { sectionPrefix: "0301", organizationQuery: "12010｜外科" });
});

test("C4 candidate UI presents the new code while retaining legacy code in state", () => {
    assert.match(script, /code:\s*item\.newCode/);
    assert.match(script, /label:\s*item\.displayName/);
    assert.match(script, /suffix:\s*item\.source/);
    assert.match(script, /this\.form\.sectionPrefix\s*=\s*legacyCode/);
});

test("C4 exposes one modal preview action and no PDF UI action", () => {
    const titleActions = view.match(/<template v-else-if="isC4">[\s\S]*?<\/template>/)?.[0] ?? "";
    assert.equal((titleActions.match(/<button/g) ?? []).length, 1);
    assert.match(titleActions, /openC4Preview/);
    assert.doesNotMatch(titleActions, /export\/pdf|匯出 PDF/);
    assert.match(view, /c4-preview-overlay/);
    assert.match(view, /role="dialog"/);
    assert.match(view, /closeC4Preview/);
    assert.match(view, /printC4Preview/);
    assert.doesNotMatch(titleActions, /_blank/);
});

test("C4 modal preview requests structured data using the resolved legacy filter", () => {
    assert.match(script, /async openC4Preview\(\)/);
    assert.match(script, /await this\.resolveC4SectionPrefix\(\)/);
    assert.match(script, /fetch\("\/reports\/c4\/preview"/);
    assert.match(script, /this\.c4PreviewOpen\s*=\s*true/);
    assert.match(script, /if \(this\.c4PreviewOpen\) window\.print\(\)/);
});
