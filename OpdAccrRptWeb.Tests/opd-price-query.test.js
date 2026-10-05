const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

let opened = [];
global.window = {
    open() {
        const preview = { html: "", closed: false, document: {
            open() {}, write(value) { preview.html += value; }, close() {}
        }, close() { this.closed = true; } };
        opened.push(preview);
        return preview;
    }
};
global.document = { querySelector() { return { value: "csrf" }; } };
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/opd-price-query.js", "utf8"));
const component = window.ReportComponents.OpdPriceQuery;
const markup = fs.readFileSync("Views/Report/_OpdPriceQuery.cshtml", "utf8");
const styles = fs.readFileSync("wwwroot/css/site.css", "utf8");
const catalog = fs.readFileSync("Services/ReportCatalogService.cs", "utf8");
const app = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
const shell = fs.readFileSync("Views/Report/Index.cshtml", "utf8");
const receipt = fs.readFileSync("Views/OpdPriceQuery/BatchReceipt.cshtml", "utf8");

assert.match(catalog, /Report\("Q1", "批價查詢"\).*Report\("Q2", "病歷查詢"\).*Report\("Q3", "掛號查詢"\)/s);
assert.match(app, /Q1:\s*window\.ReportComponents\.OpdPriceQuery/);
assert.match(markup, /type="date"/);
assert.match(markup, /partial name="_TableSkeleton"/);
assert.match(markup, /<report-autocomplete/);
assert.match(markup, /normalizedSectionOptions/);
assert.match(markup, /medicalRecordNo"[^>]*required/);
assert.match(markup, /class="q1-advanced-collapse"/);
assert.match(markup, /aria-expanded="advancedOpen/);
assert.match(markup, /class="panel q1-patient-panel"/);
assert.match(markup, /class="q1-patient-content"/);
assert.match(markup, /class="q1-row-action q1-row-action-detail"/);
assert.match(markup, /class="q1-row-action q1-row-action-receipt"/);
assert.match(markup, /v-on:click\.stop="openDetail\(row,\$event\)"/);
assert.match(markup, /v-on:click\.stop="openReceipts\(row,\$event\)"/);
assert.match(markup, /醫令明細/);
assert.match(markup, /藥令明細/);
assert.match(markup, /無收據資料/);
assert.match(markup, /列印已勾選收據/);
assert.match(markup, /:disabled="!row.receiptToken"/);
assert.doesNotMatch(markup, /v-on:change="invalidate"/);
assert.match(markup, /searched \? '查無資料' : '尚無查詢結果'/);
assert.match(markup, /v-model\.number="pageSize" v-on:change="changePageSize"/);
assert.match(markup, /第 \{\{pageNumber\}\} \/ \{\{totalPages\}\} 頁/);
assert.match(markup, /class="active" type="button"/);
assert.match(styles, /\.q1-top-grid\{/);
assert.match(styles, /\.q1-report \.q1-top-grid\{grid-template-columns:repeat\(2,minmax\(0,1fr\)\);align-items:stretch\}/);
assert.match(styles, /\.q1-row-action\{display:inline-flex;min-width:64px;min-height:44px/);
assert.match(styles, /\.q1-row-action:focus-visible\{/);
assert.match(styles, /\.q1-patient\{[^}]*margin:0/);
assert.match(styles, /\.q1-patient-content\{min-height:156px;padding:16px 18px 22px 28px/);
assert.match(styles, /\.q1-patient-content\{padding:14px 14px 14px 20px\}/);
assert.match(styles, /\.q1-modal-backdrop\{/);
assert.match(styles, /\.q1-batch-paper\{break-after:page/);
assert.match(styles, /\.q1-batch-paper:last-of-type\{break-after:auto/);
assert.match(receipt, /@foreach \(var receipt in Model\.Receipts\)/);
assert.match(receipt, /可列印 @Model\.ReadyCount 張收據/);
assert.match(receipt, /onclick="openReceiptPrintPreview\(\)"/);
assert.match(receipt, /@if \(Model\.ReadyCount > 0\)\s*\{\s*<script>[\s\S]*window\.addEventListener\("load", openReceiptPrintPreview, \{ once: true \}\)/);
assert.match(receipt, /window\.addEventListener\("afterprint"/);
assert.ok(shell.indexOf("js/components/report-autocomplete.js") < shell.indexOf("_OpdPriceQuery"));

function instance() {
    const state = component.data();
    for (const [name, method] of Object.entries(component.methods)) state[name] = method.bind(state);
    return state;
}
function deferred() {
    let resolve;
    return { promise: new Promise(done => { resolve = done; }), resolve };
}
const okJson = value => ({ ok: true, json: async () => value });

async function run() {
    const state = instance();
    const calls = [];
    global.fetch = async (url, options) => {
        if (url.includes("/sections?")) return okJson([]);
        const body = JSON.parse(options.body); calls.push([url, body]);
        if (url.endsWith("/visits")) return okJson({ rows: [{ visitToken: "v1", visitDate: "1150930" }, { visitToken: "v2", visitDate: "1150930" }], totalCount: 2, totalPages: 1, pageNumber: 1 });
        return okJson({ patient: { medicalRecordNo: body.visitToken, name: body.visitToken }, encounter: { diagnoses: [] }, charges: [], receipts: [] });
    };
    state.form.medicalRecordNo = "MR1";
    const options = component.computed.normalizedSectionOptions.call({ sectionOptions: [{ code: "11910", name: "心臟內科" }] });
    state.selectSectionOption(options[0]);
    assert.equal(state.form.sectionQuery, "11910｜心臟內科");
    await state.search();
    assert.equal(calls[0][1].sectionCode, "11910");
    assert.equal(calls[0][1].visitDate, null);
    assert.equal(state.selectedDetail.patient.medicalRecordNo, "v1");
    state.pageNumber = 2;
    state.pageSize = 30;
    await state.changePageSize();
    assert.equal(state.pageNumber, 1);
    assert.equal(calls.at(-2)[1].pageSize, 30);
    state.form.showDc = true;
    assert.equal(state.snapshot.showDc, false);
    assert.equal(state.selectedDetail.patient.name, "v1");
    await state.openDetail(state.visits[1]);
    assert.equal(calls.at(-1)[1].visitToken, "v2");
    assert.equal(calls.at(-1)[1].showDc, false);
    assert.equal(state.modalDetail.patient.name, "v2");
    assert.equal(state.selectedDetail.patient.name, "v1");
    state.closeModal();
    assert.equal(state.selectedDetail.patient.name, "v1");

    const slow = deferred();
    global.fetch = (url, options) => url.endsWith("/visits") ? slow.promise : okJson({ patient: {}, encounter: { diagnoses: [] }, charges: [], receipts: [] });
    const pending = state.search();
    state.reset();
    slow.resolve(okJson({ rows: [{ visitToken: "old" }], totalCount: 1, totalPages: 1, pageNumber: 1 }));
    await pending;
    assert.equal(state.visits.length, 0);
    assert.equal(state.selectedDetail, null);
    assert.equal(state.modalKind, "");

    const slowModal = deferred();
    state.snapshot = { showDc: false, showExtendedCode: false };
    global.fetch = () => slowModal.promise;
    const pendingModal = state.openReceipts({ visitToken: "old" });
    state.closeModal();
    slowModal.resolve(okJson({ patient: { name: "obsolete" }, receipts: [] }));
    await pendingModal;
    assert.equal(state.modalKind, "");
    assert.equal(state.modalDetail, null);
    const groups = component.computed.chargeGroups.call({ modalDetail: { charges: [{ category: "2醫" }] } });
    assert.equal(groups[0].rows.length, 1);
    assert.equal(groups[1].rows.length, 0);

    const newer = instance(), queryA = deferred(), queryB = deferred();
    let visitCalls = 0;
    global.fetch = (url, options) => {
        if (url.endsWith("/visits")) return ++visitCalls === 1 ? queryA.promise : queryB.promise;
        return okJson({ patient: { name: JSON.parse(options.body).visitToken }, encounter: { diagnoses: [] } });
    };
    newer.form.medicalRecordNo = "MR1";
    const firstQuery = newer.search();
    newer.form.medicalRecordNo = "MR2";
    const secondQuery = newer.search();
    queryB.resolve(okJson({ rows: [{ visitToken: "new" }], totalCount: 1, totalPages: 1, pageNumber: 1 }));
    await secondQuery;
    queryA.resolve(okJson({ rows: [{ visitToken: "old" }], totalCount: 1, totalPages: 1, pageNumber: 1 }));
    await firstQuery;
    assert.equal(newer.visits[0].visitToken, "new");
    assert.equal(newer.selectedDetail.patient.name, "new");

    state.modalKind = "receipts";
    state.modalVisit = { visitToken: "visit" };
    state.toggleReceipt("second", true);
    state.toggleReceipt("first", true);
    let submitted;
    global.fetch = async (url, options) => {
        submitted = [url, [...options.body.entries()]];
        return { ok: true, text: async () => "<html><body>2 pages</body></html>" };
    };
    await state.printSelectedReceipts();
    assert.equal(submitted[0], "/data-query/opd-price/receipts");
    assert.deepEqual(submitted[1].filter(([key]) => key === "receiptTokens").map(([, value]) => value), ["second", "first"]);
    assert.equal(submitted[1].find(([key]) => key === "__RequestVerificationToken")[1], "csrf");
    assert.match(opened.at(-1).html, /2 pages/);
    assert.equal(state.printPending, false);

    const blocked = instance();
    blocked.modalKind = "receipts";
    blocked.modalVisit = { visitToken: "visit" };
    blocked.selectedReceipts = ["receipt"];
    const originalOpen = window.open;
    window.open = () => null;
    let blockedFetchCalled = false;
    global.fetch = async () => { blockedFetchCalled = true; throw new Error("unexpected fetch"); };
    await blocked.printSelectedReceipts();
    assert.match(blocked.modalError, /封鎖/);
    assert.equal(blockedFetchCalled, false);
    window.open = originalOpen;
    console.log("opd price query tests passed");
}
run().catch(error => { console.error(error); process.exitCode = 1; });
