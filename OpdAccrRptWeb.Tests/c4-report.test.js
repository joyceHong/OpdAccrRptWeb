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
    const window = {};
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
    assert.match(view, /class="report-autocomplete-panel"/);
    assert.match(view, /selectC4Organization\(item\)/);
    assert.doesNotMatch(view.match(/<div v-if="isC4"[\s\S]*?<\/div>\s*<\/label>/)?.[0] ?? "", /<select/);
    assert.match(styles, /\.report-autocomplete-panel\{[^}]*position:absolute[^}]*width:100%/);
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
});

test("C4 candidate UI presents the new code while retaining legacy code in state", () => {
    assert.match(view, /<strong>\{\{ item\.newCode \}\}<\/strong>/);
    assert.doesNotMatch(view, /<strong>\{\{ item\.legacyCode \}\}<\/strong>/);
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
