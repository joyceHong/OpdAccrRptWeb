const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

global.window = { clearTimeout };
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8"));
const component = window.ReportComponents.ReportTemplate;

async function verifiesC171RequestsServerPages() {
    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                columns: [],
                data: [{ billingCode: "B" }],
                totalCount: 28,
                totalPages: 3,
                pageNumber: 2,
                pageSize: 10
            })
        };
    };

    const context = {
        selectedReport: { code: "C171" },
        form: { startDate: "2026-08-01", endDate: "2026-08-18", department: "" },
        currentPage: 2,
        pageSize: 10,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [],
        serverTotalCount: 0,
        serverTotalPages: 0,
        validationMessage: "",
        isServerPaged: true,
        totalCount: 28,
        $emit: () => {}
    };

    await component.methods.fetchResults.call(context);

    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 10);
    assert.equal(Object.hasOwn(requestBody, "encounterSource"), false);
    assert.equal(context.rows.length, 1);
    assert.equal(context.serverTotalCount, 28);
    assert.equal(context.serverTotalPages, 3);
}

async function verifiesC1RequestsDateRangeAndServerPageOnly() {
    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                columns: [],
                data: [{ surgicalOrderCode: "64202B(LEFT)(麻醉)" }],
                totalCount: 28,
                totalPages: 1,
                pageNumber: 2,
                pageSize: 30
            })
        };
    };
    const context = {
        selectedReport: { code: "C1" },
        form: {
            startDate: "2026-08-01",
            endDate: "2026-08-03",
            department: "",
            encounterSource: "",
            stationOrBedPrefix: ""
        },
        currentPage: 2,
        pageSize: 30,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [],
        serverTotalCount: 0,
        serverTotalPages: 0,
        validationMessage: "",
        hasEncounterSource: false,
        hasStationOrBedPrefix: false,
        isServerPaged: true,
        $emit: () => {}
    };

    await component.methods.fetchResults.call(context);

    assert.equal(requestBody.reportCode, "C1");
    assert.equal(requestBody.startDate, "2026-08-01");
    assert.equal(requestBody.endDate, "2026-08-03");
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 30);
    assert.equal(Object.hasOwn(requestBody, "encounterSource"), false);
    assert.equal(Object.hasOwn(requestBody, "stationOrBedPrefix"), false);
    assert.equal(context.serverTotalCount, 28);
}

async function verifiesC174RequestsServerPagesAndReplacesRows() {
    const requests = [];
    global.fetch = async (_url, options) => {
        const request = JSON.parse(options.body);
        requests.push(request);
        return {
            ok: true,
            json: async () => ({
                columns: [],
                data: [{ billingCode: `page-${request.pageNumber}` }],
                totalCount: 28,
                totalPages: 3,
                pageNumber: request.pageNumber,
                pageSize: request.pageSize
            })
        };
    };

    const context = {
        selectedReport: { code: "C174" },
        form: { startDate: "2026-08-01", endDate: "2026-08-31", department: "" },
        currentPage: 1,
        pageSize: 10,
        isLoading: false,
        hasSearched: true,
        columns: [],
        rows: [{ billingCode: "old" }],
        serverTotalCount: 0,
        serverTotalPages: 3,
        validationMessage: "",
        isServerPaged: true,
        totalPages: 3,
        $emit: () => {},
        fetchResults: component.methods.fetchResults
    };

    await component.methods.goToPage.call(context, 2);
    assert.equal(requests[0].reportCode, "C174");
    assert.equal(requests[0].pageNumber, 2);
    assert.equal(requests[0].pageSize, 10);
    assert.deepEqual(context.rows, [{ billingCode: "page-2" }]);
    assert.equal(context.serverTotalCount, 28);

    context.pageSize = 30;
    await component.methods.changePageSize.call(context);
    assert.equal(context.currentPage, 1);
    assert.equal(requests[1].pageNumber, 1);
    assert.equal(requests[1].pageSize, 30);
}

function verifiesServerPagingDesignation() {
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C21" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C22" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C213" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C1" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C171" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C174" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C18" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C19" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C29" } }), true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C172" } }), false);
}

async function verifiesC21SourceScopesPayloadAndWebOnlyMarkup() {
    const configuration = window.ReportConfigurations.C21;
    const initial = component.data.call({
        selectedReport: { code: "C21" }, defaultStartDate: "2026-09-08", defaultEndDate: "2026-09-08"
    });
    assert.equal(initial.form.encounterSource, "Outpatient");
    assert.equal(initial.form.accountingScope, 0);

    let request;
    global.fetch = async (_url, options) => {
        request = JSON.parse(options.body);
        return { ok: true, json: async () => ({ columns: [], data: [], totalCount: 0, totalPages: 0, pageNumber: 1, pageSize: 10 }) };
    };
    const context = {
        ...initial, selectedReport: { code: "C21" }, reportConfiguration: configuration,
        isC21: true, isEndDateOnly: false, hasEncounterSource: true,
        hasStationOrBedPrefix: false, hasCashierUserId: false, hasCashierCashSort: false,
        hasBillingCode: false, hasReceivableBalanceType: false, hasAdvancedConditions: false,
        isServerPaged: true, $emit: () => {}
    };
    await component.methods.fetchResults.call(context);
    assert.equal(request.encounterSource, "Outpatient");
    assert.equal(request.accountingScope, 0);
    assert.equal(request.forceRebuild, undefined);

    context.form.encounterSource = "Inpatient";
    component.methods.changeEncounterSource.call(context);
    assert.equal(context.form.accountingScope, 5);
    assert.equal(context.form.forceRebuild, false);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(markup, /v-if="isC21"/);
    assert.match(markup, /c21ScopeOptions/);
    assert.match(markup, /重新計算/);
    assert.equal(component.computed.canExport.call({ selectedReport: { code: "C21" }, hasResults: true, isExporting: false }), true);
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(appSource, /C21:\s*window\.ReportComponents\.ReportTemplate/);
}

function verifiesC213UsesSharedComponentWithoutAdvancedConditionsAndWithExport() {
    const configuration = window.ReportConfigurations.C213;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.advancedConditions, false);
    assert.equal(component.computed.hasAdvancedConditions.call({ reportConfiguration: configuration }), false);
    assert.equal(component.computed.canExport.call({
        selectedReport: { code: "C213" },
        hasResults: true,
        isExporting: false
    }), true);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    const componentFiles = fs.readdirSync("wwwroot/js/reports");
    assert.match(markup, /v-if="hasAdvancedConditions" class="advanced-filter-toggle"/);
    assert.match(markup, /v-if="hasAdvancedConditions" v-show="advancedOpen" class="advanced-grid"/);
    assert.match(appSource, /C213:\s*window\.ReportComponents\.ReportTemplate/);
    assert.equal(componentFiles.some(file => /c213/i.test(file)), false);
}

function verifiesRebuildVisibilityFollowsPermissionAndQueryBoundary() {
    const canForce = component.computed.canForceC21Rebuild;
    const singleDayInpatient = {
        selectedReport: { canRebuild: false },
        isC21: true,
        form: {
            encounterSource: "Inpatient",
            startDate: "2026-09-08",
            endDate: "2026-09-08"
        }
    };

    assert.equal(canForce.call(singleDayInpatient), false);
    assert.equal(canForce.call({ ...singleDayInpatient, selectedReport: { canRebuild: true } }), true);
    assert.equal(canForce.call({
        ...singleDayInpatient,
        selectedReport: { canRebuild: true },
        form: { ...singleDayInpatient.form, endDate: "2026-09-09" }
    }), false);
    assert.equal(canForce.call({
        ...singleDayInpatient,
        selectedReport: { canRebuild: true },
        form: { ...singleDayInpatient.form, encounterSource: "Outpatient" }
    }), false);
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(markup, /v-if="isC21 && selectedReport\.canRebuild" class="source-option c21-rebuild-option"[\s\S]*?:disabled="!canForceC21Rebuild"/);
    assert.doesNotMatch(markup, /未授予重新計算權限/);
    assert.ok(markup.indexOf('class="advanced-grid c21-filter-grid"') < markup.indexOf('class="source-option c21-rebuild-option"'));
    assert.match(markup, /限住院單日/);
    const watcherState = { form: { forceRebuild: true } };
    component.watch.canForceC21Rebuild.call(watcherState, false);
    assert.equal(watcherState.form.forceRebuild, false);

    const canForceC23 = component.computed.canForceC23Rebuild;
    const c23 = {
        isC23: true, selectedReport: { canRebuild: false },
        form: { dateMode: "General", startDate: "2026-09-08", endDate: "2026-09-08" }
    };
    assert.equal(canForceC23.call(c23), false);
    assert.equal(canForceC23.call({ ...c23, selectedReport: { canRebuild: true } }), true);
    assert.equal(canForceC23.call({ ...c23, selectedReport: { canRebuild: true },
        form: { ...c23.form, dateMode: "EncounterDate" } }), false);

    const indexMarkup = fs.readFileSync("Views/Report/Index.cshtml", "utf8");
    assert.doesNotMatch(indexMarkup, /:c21-rebuild-enabled/);
    assert.doesNotMatch(indexMarkup, /currentUserId/i);
}

async function verifiesC214SharedConfigurationPayloadTypeSwitchAndReset() {
    const configuration = window.ReportConfigurations.C214;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.endDateOnly, true);
    assert.equal(configuration.advancedConditions, false);
    assert.equal(configuration.receivableBalanceType.defaultValue, "SelfPay");
    assert.deepEqual(
        configuration.receivableBalanceType.options.map(option => [option.value, option.label]),
        [["SelfPay", "自費"], ["Insurance", "健保"]]);
    assert.equal(component.computed.canExport.call({
        selectedReport: { code: "C214" }, hasResults: true, isExporting: false
    }), true);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(markup, /v-if="hasReceivableBalanceType" class="source-fieldset"/);
    assert.match(markup, /name="receivableBalanceType"/);
    assert.match(markup, /v-on:change="changeReceivableBalanceType"/);
    assert.match(appSource, /C214:\s*window\.ReportComponents\.ReportTemplate/);
    assert.equal(fs.readdirSync("wwwroot/js/reports").some(file => /c214/i.test(file)), false);

    const requests = [];
    global.fetch = async (_url, options) => {
        requests.push(JSON.parse(options.body));
        return {
            ok: true,
            json: async () => ({
                columns: [], data: [], totalCount: 21, totalPages: 3,
                pageNumber: requests.at(-1).pageNumber,
                pageSize: requests.at(-1).pageSize
            })
        };
    };
    const initial = component.data.call({
        selectedReport: { code: "C214" },
        defaultStartDate: "2026-08-31",
        defaultEndDate: "2026-08-31"
    });
    const context = {
        ...initial,
        selectedReport: { code: "C214" },
        defaultStartDate: "2026-08-31",
        defaultEndDate: "2026-08-31",
        isEndDateOnly: true,
        hasEncounterSource: false,
        hasStationOrBedPrefix: false,
        hasCashierUserId: false,
        hasCashierCashSort: false,
        hasBillingCode: false,
        hasReceivableBalanceType: true,
        hasAdvancedConditions: false,
        isServerPaged: true,
        hasSearched: true,
        totalPages: 3,
        reportConfiguration: configuration,
        fetchResults: component.methods.fetchResults,
        stopExportPolling: () => {}
    };

    await component.methods.fetchResults.call(context);
    assert.deepEqual(requests[0], {
        reportCode: "C214",
        endDate: "2026-08-31",
        receivableBalanceType: "SelfPay",
        pageNumber: 1,
        pageSize: 10
    });
    await component.methods.goToPage.call(context, 2);
    assert.equal(requests[1].pageNumber, 2);
    context.pageSize = 30;
    await component.methods.changePageSize.call(context);
    assert.equal(requests[2].pageNumber, 1);
    assert.equal(requests[2].pageSize, 30);

    context.form.receivableBalanceType = "Insurance";
    context.form.endDate = "2026-08-30";
    context.currentPage = 2;
    context.rows = [{}];
    context.columns = [{}];
    context.serverTotalCount = 21;
    context.serverTotalPages = 3;
    component.methods.changeReceivableBalanceType.call(context);
    assert.equal(context.form.endDate, "2026-08-30");
    assert.equal(context.currentPage, 1);
    assert.deepEqual(context.rows, []);
    assert.equal(context.serverTotalCount, 0);
    assert.equal(context.serverTotalPages, 0);

    component.methods.resetForm.call(context);
    assert.equal(context.form.endDate, "2026-08-31");
    assert.equal(context.form.receivableBalanceType, "SelfPay");
    assert.equal(context.currentPage, 1);
    assert.equal(context.pageSize, 10);
    assert.deepEqual(context.rows, []);
}

async function verifiesC213DatePagingPayloadAndResetLifecycle() {
    const requests = [];
    global.fetch = async (_url, options) => {
        requests.push(JSON.parse(options.body));
        return {
            ok: true,
            json: async () => ({
                columns: [], data: [], totalCount: 24, totalPages: 3,
                pageNumber: requests.at(-1).pageNumber,
                pageSize: requests.at(-1).pageSize
            })
        };
    };

    const initial = component.data.call({
        selectedReport: { code: "C213" },
        defaultStartDate: "2026-08-31",
        defaultEndDate: "2026-08-31"
    });
    const context = {
        ...initial,
        selectedReport: { code: "C213" },
        defaultStartDate: "2026-08-31",
        defaultEndDate: "2026-08-31",
        form: { ...initial.form, startDate: "2026-08-01", endDate: "2026-08-31" },
        isEndDateOnly: false,
        hasEncounterSource: false,
        hasStationOrBedPrefix: false,
        hasCashierUserId: false,
        hasCashierCashSort: false,
        hasBillingCode: false,
        isServerPaged: true,
        hasSearched: true,
        totalPages: 3,
        reportConfiguration: window.ReportConfigurations.C213,
        fetchResults: component.methods.fetchResults,
        stopExportPolling: () => {}
    };

    await component.methods.fetchResults.call(context);
    assert.deepEqual(requests[0], {
        reportCode: "C213",
        startDate: "2026-08-01",
        endDate: "2026-08-31",
        pageNumber: 1,
        pageSize: 10
    });

    await component.methods.goToPage.call(context, 2);
    assert.equal(requests[1].pageNumber, 2);
    assert.equal(requests[1].pageSize, 10);

    context.pageSize = 30;
    await component.methods.changePageSize.call(context);
    assert.equal(requests[2].pageNumber, 1);
    assert.equal(requests[2].pageSize, 30);

    context.form.startDate = "2026-08-02";
    context.rows = [{}];
    context.columns = [{}];
    context.currentPage = 3;
    component.methods.resetForm.call(context);
    assert.equal(context.form.startDate, "2026-08-31");
    assert.equal(context.form.endDate, "2026-08-31");
    assert.equal(context.currentPage, 1);
    assert.equal(context.pageSize, 10);
    assert.deepEqual(context.rows, []);
    assert.deepEqual(context.columns, []);
}

async function verifiesC29SharedConfigurationPayloadPaginationAndReset() {
    const configuration = window.ReportConfigurations.C29;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.billingCode, true);
    assert.equal(configuration.encounterSource.defaultValue, "Emergency");
    assert.deepEqual(
        configuration.encounterSource.options.map(option => [option.value, option.label]),
        [["Emergency", "門急診"], ["Inpatient", "住院"]]);

    const initial = component.data.call({
        selectedReport: { code: "C29" },
        defaultStartDate: "2026-08-01",
        defaultEndDate: "2026-08-31"
    });
    assert.equal(initial.form.encounterSource, "Emergency");
    assert.equal(initial.form.billingCode, "");

    const requests = [];
    global.fetch = async (_url, options) => {
        const request = JSON.parse(options.body);
        requests.push(request);
        return {
            ok: true,
            json: async () => ({
                columns: [], data: [], totalCount: 21, totalPages: 3,
                pageNumber: request.pageNumber, pageSize: request.pageSize
            })
        };
    };
    const context = {
        selectedReport: { code: "C29" },
        form: {
            startDate: "2026-08-01", endDate: "2026-08-31",
            encounterSource: "Inpatient", billingCode: "  A01  ", department: ""
        },
        currentPage: 1, pageSize: 10, isLoading: false, hasSearched: true,
        columns: [], rows: [], serverTotalCount: 0, serverTotalPages: 3,
        validationMessage: "", hasEncounterSource: true, hasStationOrBedPrefix: false,
        hasCashierUserId: false, hasCashierCashSort: false, hasBillingCode: true,
        isEndDateOnly: false, isServerPaged: true, totalPages: 3,
        fetchResults: component.methods.fetchResults, $emit: () => {}
    };

    await context.fetchResults.call(context);
    await component.methods.goToPage.call(context, 2);
    context.pageSize = 30;
    await component.methods.changePageSize.call(context);

    assert.equal(requests.length, 3);
    assert.deepEqual(requests.map(request => request.encounterSource),
        ["Inpatient", "Inpatient", "Inpatient"]);
    assert.deepEqual(requests.map(request => request.billingCode), ["A01", "A01", "A01"]);
    assert.equal(requests[1].pageNumber, 2);
    assert.equal(requests[2].pageNumber, 1);
    assert.equal(requests[2].pageSize, 30);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(markup, /v-if="hasBillingCode"[^>]*><span>合約代碼/);
    assert.match(markup, /!hasCashierUserId && !hasBillingCode/);
    assert.match(appSource, /C29:\s*window\.ReportComponents\.ReportTemplate/);

    Object.assign(context, {
        defaultStartDate: "2026-08-01", defaultEndDate: "2026-08-31",
        advancedOpen: true, validationMessage: "old", rows: [{}], columns: [{}],
        currentPage: 3, pageSize: 30, serverTotalCount: 21, serverTotalPages: 3
    });
    component.methods.resetForm.call(context);
    assert.equal(context.form.startDate, "2026-08-01");
    assert.equal(context.form.endDate, "2026-08-31");
    assert.equal(context.form.encounterSource, "Emergency");
    assert.equal(context.form.billingCode, "");
    assert.equal(context.currentPage, 1);
    assert.equal(context.pageSize, 10);
    assert.deepEqual(context.rows, []);
}

async function verifiesC22DefaultsResetPayloadAndMarkup() {
    const initial = component.data.call({
        selectedReport: { code: "C22" }, defaultStartDate: "2026-08-01", defaultEndDate: "2026-08-03"
    });
    assert.equal(initial.form.cashierUserId, "");
    assert.equal(initial.form.cashierCashSortType, "Cashier");
    assert.equal(initial.form.encounterSource, "Outpatient");

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return { ok: true, json: async () => ({ columns: [], data: [], totalCount: 0, totalPages: 0, pageNumber: 2, pageSize: 30 }) };
    };
    const context = {
        selectedReport: { code: "C22" },
        form: { startDate: "2026-08-01", endDate: "2026-08-03", encounterSource: "Inpatient", cashierUserId: "A123", cashierCashSortType: "Encounter", department: "" },
        currentPage: 2, pageSize: 30, isLoading: false, hasSearched: false, columns: [], rows: [],
        serverTotalCount: 0, serverTotalPages: 0, validationMessage: "", hasEncounterSource: true,
        hasStationOrBedPrefix: false, hasCashierUserId: true, hasCashierCashSort: true, isServerPaged: true
    };
    await component.methods.fetchResults.call(context);
    assert.equal(requestBody.cashierUserId, "A123");
    assert.equal(requestBody.cashierCashSortType, "Encounter");
    assert.equal(requestBody.encounterSource, "Inpatient");
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 30);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(markup, /v-if="hasCashierUserId"/);
    assert.match(markup, /v-if="hasCashierCashSort"/);
    assert.match(markup, /v-else-if="isC22"[\s\S]*selectedReport\.canPrint[\s\S]*previewC22/);
    assert.match(markup, /c22-filter-dialog/);
    assert.match(markup, /確定列印/);
    assert.deepEqual(window.ReportConfigurations.C22.cashierCashSort.options.map(option => option.label), ["依櫃員", "依門急診"]);
}

async function verifiesC25UsesSharedServerPagedLifecycle() {
    assert.equal(window.ReportConfigurations.C25.serverPaged, true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C25" } }), true);
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(appSource, /C25:\s*window\.ReportComponents\.ReportTemplate/);

    const requests = [];
    let resolveResponse;
    global.fetch = (_url, options) => {
        requests.push(JSON.parse(options.body));
        return new Promise(resolve => { resolveResponse = resolve; });
    };
    const context = {
        selectedReport: { code: "C25" },
        form: { startDate: "2026-08-01", endDate: "2026-08-31", department: "" },
        currentPage: 2,
        pageSize: 10,
        isLoading: false,
        hasSearched: true,
        columns: [],
        rows: [],
        serverTotalCount: 0,
        serverTotalPages: 3,
        validationMessage: "",
        hasEncounterSource: false,
        hasStationOrBedPrefix: false,
        hasCashierUserId: false,
        hasCashierCashSort: false,
        hasAdvancedConditions: true,
        isServerPaged: true,
        totalPages: 3,
        fetchResults: component.methods.fetchResults
    };

    const pageRequest = component.methods.goToPage.call(context, 2);
    assert.equal(context.isLoading, true);
    assert.deepEqual(requests[0], {
        reportCode: "C25",
        startDate: "2026-08-01",
        endDate: "2026-08-31",
        chop1sec: "",
        pageNumber: 2,
        pageSize: 10
    });
    resolveResponse({
        ok: true,
        json: async () => ({
            columns: [{ key: "medicalRecordNumber", label: "病歷號" }],
            data: [{ medicalRecordNumber: "A12345" }],
            totalCount: 28,
            totalPages: 3,
            pageNumber: 2,
            pageSize: 10
        })
    });
    await pageRequest;
    assert.equal(context.isLoading, false);
    assert.equal(context.serverTotalCount, 28);
    assert.deepEqual(context.rows, [{ medicalRecordNumber: "A12345" }]);

    global.fetch = async (_url, options) => {
        requests.push(JSON.parse(options.body));
        return {
            ok: true,
            json: async () => ({
                columns: [], data: [], totalCount: 0, totalPages: 0,
                pageNumber: 1, pageSize: 30
            })
        };
    };
    context.pageSize = 30;
    await component.methods.changePageSize.call(context);
    assert.equal(context.currentPage, 1);
    assert.equal(requests[1].startDate, "2026-08-01");
    assert.equal(requests[1].endDate, "2026-08-31");
    assert.equal(requests[1].pageNumber, 1);
    assert.equal(requests[1].pageSize, 30);
    assert.deepEqual(context.rows, []);
    assert.equal(context.serverTotalCount, 0);

    context.form.startDate = "2026-08-02";
    context.currentPage = 3;
    let searchFetchCalls = 0;
    context.fetchResults = async () => { searchFetchCalls++; };
    context.reportConfiguration = window.ReportConfigurations.C25;
    await component.methods.search.call(context);
    assert.equal(context.currentPage, 1);
    assert.equal(searchFetchCalls, 1);

    global.fetch = async () => { throw new Error("C25 unavailable"); };
    context.fetchResults = component.methods.fetchResults;
    await context.fetchResults.call(context);
    assert.equal(context.isLoading, false);
    assert.deepEqual(context.rows, []);
    assert.match(context.validationMessage, /C25 unavailable/);
}

async function verifiesC27UsesCutoffDateOnlySharedLifecycle() {
    const configuration = window.ReportConfigurations.C27;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.endDateOnly, true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C27" } }), true);
    assert.equal(component.computed.isEndDateOnly.call({ reportConfiguration: configuration }), true);

    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(appSource, /C27:\s*window\.ReportComponents\.ReportTemplate/);
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(markup, /v-if="!isEndDateOnly"[^>]*><span>起始日期/);
    assert.match(markup, /<span>{{ isC141 \? '帳務日' : '截止日期' }} <i>\*<\/i><\/span><input v-model="form\.endDate"/);

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                columns: [], data: [], totalCount: 0, totalPages: 0,
                pageNumber: 1, pageSize: 10
            })
        };
    };
    const context = {
        selectedReport: { code: "C27" },
        form: { startDate: "2026-08-01", endDate: "2026-08-31", department: "" },
        currentPage: 1,
        pageSize: 10,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [],
        serverTotalCount: 0,
        serverTotalPages: 0,
        validationMessage: "",
        hasEncounterSource: false,
        hasStationOrBedPrefix: false,
        hasCashierUserId: false,
        hasCashierCashSort: false,
        isEndDateOnly: true,
        isServerPaged: true
    };

    await component.methods.fetchResults.call(context);

    assert.equal(requestBody.reportCode, "C27");
    assert.equal(requestBody.endDate, "2026-08-31");
    assert.equal(Object.hasOwn(requestBody, "startDate"), false);
    assert.equal(requestBody.pageNumber, 1);
    assert.equal(requestBody.pageSize, 10);

    let searchCalls = 0;
    context.form.startDate = "";
    context.fetchResults = async () => { searchCalls++; };
    context.reportConfiguration = configuration;
    await component.methods.search.call(context);
    assert.equal(searchCalls, 1);

    context.form.endDate = "";
    await component.methods.search.call(context);
    assert.equal(searchCalls, 1);
    assert.match(context.validationMessage, /截止日期/);

    assert.equal(component.computed.isEndDateOnly.call({ reportConfiguration: window.ReportConfigurations.C25 }), false);
}

async function verifiesC28UsesCutoffDateOnlySharedLifecycle() {
    const configuration = window.ReportConfigurations.C28;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.endDateOnly, true);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C28" } }), true);
    assert.equal(component.computed.isEndDateOnly.call({ reportConfiguration: configuration }), true);

    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(appSource, /C28:\s*window\.ReportComponents\.ReportTemplate/);

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                columns: [], data: [], totalCount: 0, totalPages: 0,
                pageNumber: 2, pageSize: 30
            })
        };
    };
    const context = {
        selectedReport: { code: "C28" },
        form: { startDate: "2026-08-01", endDate: "2026-08-31", department: "" },
        currentPage: 2,
        pageSize: 30,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [],
        serverTotalCount: 0,
        serverTotalPages: 0,
        validationMessage: "",
        hasEncounterSource: false,
        hasStationOrBedPrefix: false,
        hasCashierUserId: false,
        hasCashierCashSort: false,
        isEndDateOnly: true,
        isServerPaged: true
    };

    await component.methods.fetchResults.call(context);

    assert.equal(requestBody.reportCode, "C28");
    assert.equal(requestBody.endDate, "2026-08-31");
    assert.equal(Object.hasOwn(requestBody, "startDate"), false);
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 30);
}

async function verifiesC19RequestsServerPageAndPrefix() {
    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                columns: [],
                data: [{ category: "X" }],
                totalCount: 28,
                totalPages: 3,
                pageNumber: 2,
                pageSize: 10
            })
        };
    };
    const context = {
        selectedReport: { code: "C19" },
        form: {
            startDate: "2026-08-24",
            endDate: "2026-08-24",
            encounterSource: "Inpatient",
            stationOrBedPrefix: "7A",
            department: ""
        },
        currentPage: 2,
        pageSize: 10,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [],
        serverTotalCount: 0,
        serverTotalPages: 0,
        validationMessage: "",
        hasEncounterSource: true,
        hasStationOrBedPrefix: true,
        isServerPaged: true,
        $emit: () => {}
    };

    await component.methods.fetchResults.call(context);

    assert.equal(requestBody.encounterSource, "Inpatient");
    assert.equal(requestBody.stationOrBedPrefix, "7A");
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 10);
    assert.equal(context.serverTotalCount, 28);
    assert.equal(context.serverTotalPages, 3);
}

function verifiesC18DefaultsAndReset() {
    const initial = component.data.call({
        selectedReport: { code: "C18" },
        defaultStartDate: "2026-08-01",
        defaultEndDate: "2026-08-18"
    });
    assert.equal(initial.form.encounterSource, "Emergency");

    const context = {
        selectedReport: { code: "C18" },
        defaultStartDate: "2026-08-01",
        defaultEndDate: "2026-08-18",
        form: { encounterSource: "Inpatient" },
        advancedOpen: true,
        validationMessage: "old",
        hasSearched: true,
        rows: [{}],
        columns: [{}],
        currentPage: 3,
        pageSize: 30,
        serverTotalCount: 28,
        serverTotalPages: 3
    };
    component.methods.resetForm.call(context);
    assert.equal(context.form.encounterSource, "Emergency");
    assert.equal(context.currentPage, 1);
    assert.equal(context.pageSize, 10);
}

async function verifiesC18RequestsSourceAndUsesServerMetadata() {
    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                columns: [],
                data: Array.from({ length: 10 }, (_, index) => ({ id: index + 1 })),
                totalCount: 28,
                totalPages: 3,
                pageNumber: 2,
                pageSize: 10
            })
        };
    };
    const context = {
        selectedReport: { code: "C18" },
        form: {
            startDate: "2026-08-01",
            endDate: "2026-08-18",
            encounterSource: "Inpatient",
            department: ""
        },
        currentPage: 2,
        pageSize: 10,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [],
        serverTotalCount: 0,
        serverTotalPages: 0,
        validationMessage: "",
        hasEncounterSource: true,
        isServerPaged: true,
        $emit: () => {}
    };

    await component.methods.fetchResults.call(context);

    assert.equal(requestBody.encounterSource, "Inpatient");
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 10);
    assert.equal(context.serverTotalCount, 28);
    assert.equal(context.serverTotalPages, 3);

    context.currentPage = 3;
    component.methods.changeEncounterSource.call(context);
    assert.equal(context.currentPage, 1);
}

async function verifiesC18RejectsCrossYearBeforeFetch() {
    let fetchCalls = 0;
    const context = {
        form: {
            startDate: "2025-12-31",
            endDate: "2026-01-01",
            encounterSource: "Emergency"
        },
        hasEncounterSource: true,
        reportConfiguration: window.ReportConfigurations.C18,
        validationMessage: "",
        currentPage: 2,
        fetchResults: async () => { fetchCalls++; }
    };

    await component.methods.search.call(context);

    assert.equal(fetchCalls, 0);
    assert.match(context.validationMessage, /同一民國年度/);
}

function verifiesReportsWithoutSourceDoNotSubmitSource() {
    const configuration = window.ReportConfigurations.C172;
    assert.equal(configuration.encounterSource, undefined);
    const c1Configuration = window.ReportConfigurations.C1;
    assert.equal(c1Configuration.serverPaged, true);
    assert.equal(c1Configuration.encounterSource, undefined);
    assert.equal(c1Configuration.stationOrBedPrefix, undefined);
}

function verifiesC18SourceMarkup() {
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(markup, /v-else-if="hasEncounterSource"/);
    assert.match(markup, /type="radio"/);
    assert.match(markup, /name="encounterSource"/);
    assert.match(markup, /required/);
    assert.match(markup, /encounterSourceConfiguration\.options/);
}

async function verifiesC174EmptyResultUsesZeroMetadata() {
    global.fetch = async () => ({
        ok: true,
        json: async () => ({
            columns: [],
            data: [],
            totalCount: 0,
            totalPages: 0,
            pageNumber: 1,
            pageSize: 10
        })
    });

    const context = {
        selectedReport: { code: "C174" },
        form: { startDate: "2026-08-01", endDate: "2026-08-31", department: "" },
        currentPage: 1,
        pageSize: 10,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [{ billingCode: "old" }],
        serverTotalCount: 28,
        serverTotalPages: 3,
        validationMessage: "",
        isServerPaged: true,
        $emit: () => {}
    };

    await component.methods.fetchResults.call(context);

    assert.deepEqual(context.rows, []);
    assert.equal(context.serverTotalCount, 0);
    assert.equal(context.serverTotalPages, 0);
    assert.equal(component.computed.hasResults.call(context), false);
}

function verifiesOtherReportsRetainClientSlicing() {
    const rows = Array.from({ length: 28 }, (_, index) => ({ id: index + 1 }));
    const page = component.computed.pagedRows.call({
        isServerPaged: false,
        filteredRows: rows,
        currentPage: 2,
        pageSize: 10,
        rows
    });

    assert.deepEqual(page.map(row => row.id), [11, 12, 13, 14, 15, 16, 17, 18, 19, 20]);
}

async function verifiesC174SynchronousExportDownloadsBlob() {
    let downloaded;
    global.fetch = async (url, options) => {
        assert.equal(url, "/Report/Export");
        const request = JSON.parse(options.body);
        assert.deepEqual(request, {
            reportCode: "C174",
            startDate: "2026-08-01",
            endDate: "2026-08-31"
        });
        return {
            status: 200,
            blob: async () => ({ workbook: true }),
            headers: { get: () => "attachment; filename=C174_test.xlsx" }
        };
    };
    const context = {
        canExport: true,
        selectedReport: { code: "C174" },
        form: { startDate: "2026-08-01", endDate: "2026-08-31" },
        validationMessage: "old",
        exportJob: null,
        isExporting: false,
        downloadBlob: (blob, name) => { downloaded = { blob, name }; },
        $emit: () => {}
    };

    await component.methods.exportResults.call(context);

    assert.equal(downloaded.name, "C174_test.xlsx");
    assert.equal(downloaded.blob.workbook, true);
    assert.equal(context.isExporting, false);
}

async function verifiesC174BackgroundExportPollsAndStopsAtReady() {
    global.fetch = async () => ({
        status: 202,
        json: async () => ({ jobId: "job-1", status: "Queued", statusUrl: "/status/job-1" })
    });
    let scheduled = 0;
    const context = {
        canExport: true,
        selectedReport: { code: "C174" },
        form: { startDate: "2026-08-01", endDate: "2026-08-31" },
        validationMessage: "",
        exportJob: null,
        isExporting: false,
        scheduleExportPoll: () => { scheduled++; },
        readExportError: component.methods.readExportError,
        $emit: () => {}
    };
    await component.methods.exportResults.call(context);
    assert.equal(context.exportJob.status, "Queued");
    assert.equal(scheduled, 1);
    assert.equal(context.isExporting, true);

    global.fetch = async () => ({
        ok: true,
        json: async () => ({ status: "Ready", statusUrl: "/status/job-1", downloadUrl: "/download/job-1" })
    });
    let stopped = 0;
    context.stopExportPolling = () => { stopped++; };
    await component.methods.pollExportJob.call(context);
    assert.equal(context.exportJob.status, "Ready");
    assert.equal(context.isExporting, false);
    assert.equal(stopped, 1);
    assert.equal(context.validationMessage, "");
}

async function verifiesHealthExportPayloadKeepsReportSpecificFilters() {
    const requests = [];
    global.fetch = async (_url, options) => {
        requests.push(JSON.parse(options.body));
        return {
            status: 200,
            blob: async () => ({}),
            headers: { get: () => null }
        };
    };
    const base = {
        canExport: true,
        validationMessage: "",
        exportJob: null,
        isExporting: false,
        downloadBlob: () => {},
        $emit: () => {}
    };

    await component.methods.exportResults.call({
        ...base,
        selectedReport: { code: "C18" },
        isC10: false,
        isC144: false,
        form: { startDate: "2026-01-01", endDate: "2026-12-31", encounterSource: "Emergency" }
    });
    await component.methods.exportResults.call({
        ...base,
        selectedReport: { code: "C19" },
        isC10: false,
        isC144: false,
        form: {
            startDate: "2026-08-05", endDate: "2026-08-05",
            encounterSource: "Inpatient", stationOrBedPrefix: " 7A "
        }
    });
    await component.methods.exportResults.call({
        ...base,
        selectedReport: { code: "C144" },
        isC10: false,
        isC144: true,
        form: { startDate: "2026-08-01", endDate: "2026-08-31", encounterSource: "OpdEr" }
    });

    assert.equal(requests[0].encounterSource, "Emergency");
    assert.equal(requests[1].encounterSource, "Inpatient");
    assert.equal(requests[1].stationOrBedPrefix, "7A");
    assert.equal(requests[2].source, "OpdEr");
}

function verifiesExportStatusMarkupAndUnmountCleanup() {
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(markup, /role="status"/);
    assert.match(markup, /aria-live="polite"/);
    assert.match(markup, /exportJob\.downloadUrl/);
    let stopped = 0;
    component.beforeUnmount.call({ stopExportPolling: () => { stopped++; } });
    assert.equal(stopped, 1);
}

async function verifiesLoadingStateAndExclusiveResultMarkup() {
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(markup, /<template v-if="isLoading">[\s\S]*?<partial name="_TableSkeleton" \/>\s*<\/template>\s*<div v-else-if="!hasResults"/);
    assert.match(markup, /v-if="!isLoading && hasResults" class="pagination"/);

    let resolveResponse;
    global.fetch = () => new Promise(resolve => { resolveResponse = resolve; });
    const successContext = {
        selectedReport: { code: "C171" },
        form: { startDate: "2026-08-01", endDate: "2026-08-18", department: "" },
        currentPage: 1,
        pageSize: 10,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [{ id: "stale" }],
        serverTotalCount: 1,
        serverTotalPages: 1,
        validationMessage: "",
        hasEncounterSource: false,
        hasStationOrBedPrefix: false,
        isServerPaged: true
    };

    const successfulRequest = component.methods.fetchResults.call(successContext);
    assert.equal(successContext.isLoading, true);
    resolveResponse({
        ok: true,
        json: async () => ({
            columns: [{ key: "id", label: "ID" }],
            data: [{ id: "new" }],
            totalCount: 1,
            totalPages: 1,
            pageNumber: 1,
            pageSize: 10
        })
    });
    await successfulRequest;
    assert.equal(successContext.isLoading, false);
    assert.deepEqual(successContext.rows, [{ id: "new" }]);

    let rejectResponse;
    global.fetch = () => new Promise((_resolve, reject) => { rejectResponse = reject; });
    const failedRequest = component.methods.fetchResults.call(successContext);
    assert.equal(successContext.isLoading, true);
    rejectResponse(new Error("network unavailable"));
    await failedRequest;
    assert.equal(successContext.isLoading, false);
    assert.deepEqual(successContext.rows, []);
    assert.match(successContext.validationMessage, /network unavailable/);
}

function verifiesAccessibleLoadingMarkup() {
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const skeletonMarkup = fs.readFileSync("Views/Report/_TableSkeleton.cshtml", "utf8");
    const styles = fs.readFileSync("wwwroot/css/site.css", "utf8");

    assert.match(markup, /class="panel result-panel" :aria-busy="isLoading \? 'true' : 'false'"/);
    assert.equal((markup.match(/role="status">資料查詢中/g) || []).length, 1);
    assert.match(markup, /class="visually-hidden" role="status">資料查詢中/);
    assert.match(skeletonMarkup, /aria-hidden="true"/);
    assert.match(styles, /\.visually-hidden\s*\{/);
}

function verifiesSkeletonMotionAndResponsiveStyles() {
    const styles = fs.readFileSync("wwwroot/css/site.css", "utf8");
    assert.match(styles, /\.table-skeleton\s*\{[^}]*max-width:100%[^}]*overflow-x:hidden/);
    assert.match(styles, /\.skeleton-line\s*\{[^}]*animation:skeleton-shimmer/);
    assert.match(styles, /@keyframes skeleton-shimmer/);
    assert.match(styles, /@media \(prefers-reduced-motion:reduce\)\s*\{\s*\.skeleton-line\s*\{[^}]*animation:none/);
}

async function verifiesC24FormPayloadAndInpatientRoomReset() {
    const configuration = window.ReportConfigurations.C24;
    const canForceRebuild = component.computed.canForceC24Rebuild;
    const rebuildContext = {
        isC24: true,
        selectedReport: { code: "C24", canRebuild: false },
        form: { mode: "Accounting", startDate: "2026-09-01", endDate: "2026-09-01" }
    };
    assert.equal(canForceRebuild.call(rebuildContext), false);
    assert.equal(canForceRebuild.call({ ...rebuildContext,
        selectedReport: { code: "C24", canRebuild: true } }), true);
    assert.equal(canForceRebuild.call({ ...rebuildContext,
        selectedReport: { code: "C24", canRebuild: true },
        form: { ...rebuildContext.form, mode: "Billing" } }), false);
    assert.equal(canForceRebuild.call({ ...rebuildContext,
        selectedReport: { code: "C24", canRebuild: true },
        form: { ...rebuildContext.form, endDate: "2026-09-02" } }), false);
    const c21Configuration = window.ReportConfigurations.C21;
    const initial = component.data.call({
        selectedReport: { code: "C24" }, defaultStartDate: "2026-09-01", defaultEndDate: "2026-09-03"
    });
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.advancedConditions, false);
    assert.deepEqual(Object.keys(configuration.encounterSource), Object.keys(c21Configuration.encounterSource));
    assert.equal(configuration.encounterSource.defaultValue, "OpdEr");
    assert.deepEqual(
        configuration.encounterSource.options.map(option => [option.value, option.label]),
        [["OpdEr", "門急診"], ["Inpatient", "住院"]]);
    assert.deepEqual(
        [initial.form.encounterSource, initial.form.source, initial.form.mode, initial.form.roomScope, initial.form.medicalRecordNo],
        ["OpdEr", "OpdEr", "Accounting", "All", ""]);
    initial.form.encounterSource = "Inpatient";
    initial.form.roomScope = "Emergency";
    initial.form.forceRebuild = true;
    component.methods.changeEncounterSource.call({ ...initial, isC21: false, isC23: false, isC24: true });
    assert.equal(initial.form.source, "Inpatient");
    assert.equal(initial.form.roomScope, "All");

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return { ok: true, json: async () => ({ columns: [], data: [], summary: [], totalCount: 0, totalPages: 0, pageNumber: 1 }) };
    };
    await component.methods.fetchResults.call({
        ...initial,
        selectedReport: { code: "C24" },
        currentPage: 1,
        pageSize: 10,
        isLoading: false,
        hasSearched: false,
        columns: [],
        rows: [],
        summaries: [],
        serverTotalCount: 0,
        serverTotalPages: 0,
        validationMessage: "",
        hasEncounterSource: true,
        hasStationOrBedPrefix: false,
        hasCashierUserId: false,
        hasCashierCashSort: false,
        hasBillingCode: false,
        hasReceivableBalanceType: false,
        hasAdvancedConditions: false,
        isEndDateOnly: false,
        isServerPaged: true,
        isC21: false,
        isC23: false,
        isC24: true
    });
    assert.equal(requestBody.source, "Inpatient");
    assert.equal(Object.hasOwn(requestBody, "encounterSource"), false);
    assert.equal(requestBody.forceRebuild, undefined);

    initial.form.endDate = initial.form.startDate;
    await component.methods.fetchResults.call({
        ...initial,
        selectedReport: { code: "C24", canRebuild: true },
        canForceC24Rebuild: true,
        currentPage: 1, pageSize: 10, isLoading: false, hasSearched: false,
        columns: [], rows: [], summaries: [], serverTotalCount: 0, serverTotalPages: 0,
        validationMessage: "", hasEncounterSource: true, hasStationOrBedPrefix: false,
        hasCashierUserId: false, hasCashierCashSort: false, hasBillingCode: false,
        hasReceivableBalanceType: false, hasAdvancedConditions: false,
        isEndDateOnly: false, isServerPaged: true, isC21: false, isC23: false, isC24: true
    });
    assert.equal(requestBody.forceRebuild, true);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(markup, /v-if="isC24"/);
    assert.match(markup, /v-if="canForceC24Rebuild" class="source-option"/);
    assert.match(markup, /v-else-if="hasEncounterSource"[\s\S]*?v-model="form\.encounterSource"[\s\S]*?type="radio"[\s\S]*?name="encounterSource"/);
    assert.doesNotMatch(markup, /name="c24Source"/);
    assert.doesNotMatch(markup, /<legend>資料來源 <i>\*<\/i><\/legend>/);
    assert.match(markup, /<span>門急診別 <i>\*<\/i><\/span>/);
    assert.match(markup, /<option value="All">全部<\/option>/);
    assert.match(markup, /<option value="Emergency">急診<\/option>/);
    assert.match(markup, /<option value="NonEmergency">門診<\/option>/);
    assert.match(markup, /v-model="form\.roomScope" :disabled="form\.source === 'Inpatient'"/);
    assert.match(appSource, /C24:\s*window\.ReportComponents\.ReportTemplate/);
}

async function verifiesC10UsesSharedQueryAndExportContracts() {
    const configuration = window.ReportConfigurations.C10;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.advancedConditions, false);
    assert.equal(configuration.encounterSource.defaultValue, "OpdEr");
    const initial = component.data.call({
        selectedReport: { code: "C10" }, defaultStartDate: "2026-09-01", defaultEndDate: "2026-09-03"
    });
    assert.deepEqual(
        [initial.form.encounterSource, initial.form.source, initial.form.roomScope, initial.form.medicalRecordNo],
        ["OpdEr", "OpdEr", "All", ""]);

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return { ok: true, json: async () => ({ columns: [], data: [], totalCount: 0, totalPages: 0, pageNumber: 1 }) };
    };
    await component.methods.fetchResults.call({
        ...initial, selectedReport: { code: "C10" }, currentPage: 1, pageSize: 10,
        isLoading: false, hasSearched: false, columns: [], rows: [], summaries: [],
        serverTotalCount: 0, serverTotalPages: 0, validationMessage: "",
        hasEncounterSource: true, hasStationOrBedPrefix: false, hasCashierUserId: false,
        hasCashierCashSort: false, hasBillingCode: false, hasReceivableBalanceType: false,
        hasAdvancedConditions: false, isEndDateOnly: false, isServerPaged: true,
        isC10: true, isC21: false, isC23: false, isC24: false
    });
    assert.equal(requestBody.reportCode, "C10");
    assert.equal(requestBody.source, "OpdEr");
    assert.equal(requestBody.roomScope, "All");
    assert.equal(requestBody.pageSize, 10);
    assert.equal(Object.hasOwn(requestBody, "encounterSource"), false);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(markup, /v-if="isC24 \|\| isC10"/);
    assert.match(markup, /v-if="isC10"[\s\S]*?<option value="Outpatient">門診<\/option>/);
    assert.match(appSource, /C10:\s*window\.ReportComponents\.ReportTemplate/);
}

async function verifiesC23SameMonthValidationOnlyAppliesToEncounterDateMode() {
    let fetchCalls = 0;
    const context = {
        isC23: true,
        isEndDateOnly: false,
        hasEncounterSource: true,
        selectedReport: { code: "C23" },
        reportConfiguration: {},
        validationMessage: "",
        currentPage: 2,
        form: {
            startDate: "2026-09-30",
            endDate: "2026-10-01",
            encounterSource: "Outpatient",
            dateMode: "General",
            inpatientType: ""
        },
        fetchResults: async () => { fetchCalls += 1; }
    };

    await component.methods.search.call(context);
    assert.equal(fetchCalls, 1);
    assert.equal(context.validationMessage, "");

    context.form.dateMode = "EncounterDate";
    await component.methods.search.call(context);
    assert.equal(fetchCalls, 1);
    assert.equal(context.validationMessage, "C23 就診日模式起訖日期必須在同一月份。");
}

async function verifiesC211CutoffSourceContractAndCompletePrintMarkup() {
    const configuration = window.ReportConfigurations.C211;
    assert.equal(configuration.endDateOnly, true);
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.advancedConditions, false);
    assert.equal(configuration.encounterSource.defaultValue, "O");
    const initial = component.data.call({
        selectedReport: { code: "C211" }, defaultStartDate: "2026-09-11", defaultEndDate: "2026-09-11"
    });
    assert.equal(initial.form.startDate, "2026-09-11");
    assert.equal(initial.form.endDate, "2026-09-10");
    assert.equal(initial.form.encounterSource, "O");

    let request;
    global.fetch = async (_url, options) => {
        request = JSON.parse(options.body);
        return { ok: true, json: async () => ({
            columns: [], data: [{ contractCode: "TT", medicalRecordNo: "123" }],
            totalCount: 28, totalPages: 3, pageNumber: 1, pageSize: 10,
            summary: { groups: [{ contractCode: "TT", selfAmount: 100, claimAmount: -100 }] }
        }) };
    };
    const context = {
        ...initial, selectedReport: { code: "C211" }, isC211: true,
        isC23: false, isC24: false, isC21: false, isEndDateOnly: true,
        hasEncounterSource: true, hasStationOrBedPrefix: false, hasCashierUserId: false,
        hasCashierCashSort: false, hasBillingCode: false, hasReceivableBalanceType: false,
        hasAdvancedConditions: false, isServerPaged: true, serverTotalCount: 0,
        serverTotalPages: 0, summaries: [], c211Summary: null, $emit: () => {}
    };
    context.form.contractCode = "  OUTSIDE  ";
    await component.methods.fetchResults.call(context);
    assert.equal(Object.hasOwn(request, "startDate"), false);
    assert.equal(request.endDate, "2026-09-10");
    assert.equal(request.encounterSource, "O");
    assert.equal(request.contractCode, "OUTSIDE");
    assert.equal(request.pageNumber, 1);
    assert.equal(request.pageSize, 10);
    assert.equal(context.rows.length, 1);
    assert.equal(context.c211Summary.groups[0].claimAmount, -100);
    assert.equal(context.serverTotalCount, 28);
    assert.equal(context.serverTotalPages, 3);

    const pageRequests = [];
    global.fetch = async (_url, options) => {
        const pageRequest = JSON.parse(options.body);
        pageRequests.push(pageRequest);
        return { ok: true, json: async () => ({
            columns: [], data: [{ contractCode: "TT", medicalRecordNo: `page-${pageRequest.pageNumber}` }],
            totalCount: 28, totalPages: 3, pageNumber: pageRequest.pageNumber, pageSize: pageRequest.pageSize,
            summary: { groups: [{ contractCode: "TT", selfAmount: 100, claimAmount: -100 }] }
        }) };
    };
    const pagingContext = {
        selectedReport: { code: "C211" }, form: { endDate: "2026-09-10", encounterSource: "O", contractCode: " TT " },
        currentPage: 1, pageSize: 10, isLoading: false, hasSearched: true, isServerPaged: true,
        isEndDateOnly: true, hasEncounterSource: true, isC211: true, isC23: false, isC24: false,
        isC21: false, hasStationOrBedPrefix: false, hasCashierUserId: false, hasCashierCashSort: false,
        hasBillingCode: false, hasReceivableBalanceType: false, hasAdvancedConditions: false,
        columns: [], rows: [], summaries: [], c211Summary: null, c212Summary: null,
        serverTotalCount: 28, serverTotalPages: 3, validationMessage: "", $emit: () => {},
        fetchResults: component.methods.fetchResults
    };
    await component.methods.goToPage.call(pagingContext, 2);
    assert.deepEqual(pagingContext.rows, [{ contractCode: "TT", medicalRecordNo: "page-2" }]);
    pagingContext.pageSize = 30;
    await component.methods.changePageSize.call(pagingContext);
    assert.equal(pagingContext.currentPage, 1);
    assert.deepEqual(pageRequests.map(item => [item.pageNumber, item.pageSize]), [[2, 10], [1, 30]]);
    for (const pageRequest of pageRequests) {
        assert.equal(pageRequest.endDate, "2026-09-10");
        assert.equal(pageRequest.encounterSource, "O");
        assert.equal(pageRequest.contractCode, "TT");
    }

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const styles = fs.readFileSync("wwwroot/css/site.css", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    assert.match(markup, /v-else-if="isC211"[^>]*v-on:click="previewC211">/);
    assert.match(markup, /v-else-if="isC211"[^>]*:disabled="!hasResults/);
    assert.match(markup, /v-else class="outline-button"[^>]*exportResults/);
    assert.match(markup, /isC212 \|\| isC211 \? '查無資料！'/);
    assert.doesNotMatch(markup, /class="c211-report"|c211Summary\.selfGrandTotal|c211Groups/);
    assert.match(markup, /v-for="column in columns"[\s\S]*v-for="\(row, index\) in pagedRows"/);
    assert.match(markup, /v-if="isC211"[^>]*c211-contract-input/);
    assert.doesNotMatch(markup, /<datalist id="c211-contract-list"/);
    assert.match(markup, /ref="c211Autocomplete" class="report-autocomplete"/);
    assert.match(markup, /id="c211-contract-list" class="report-autocomplete-panel" role="listbox"/);
    assert.match(markup, /class="report-autocomplete-option"/);
    assert.match(markup, /role="combobox"/);
    assert.match(styles, /\.report-autocomplete-panel\{[^}]*width:100%/);
    assert.match(styles, /\.report-autocomplete-panel\{[^}]*border-radius:10px/);
    const contracts=[{code:"AA",name:"甲合約"},{code:"BB",name:"乙合約"}];
    assert.deepEqual(component.computed.filteredC211Contracts.call({form:{contractCode:"bb"},c211Contracts:contracts}),[contracts[1]]);
    const autocompleteContext={filteredC211Contracts:contracts,c211ContractOpen:true,c211ContractActiveIndex:-1,form:{contractCode:""},selectC211Contract:component.methods.selectC211Contract};
    component.methods.onC211ContractKeydown.call(autocompleteContext,{key:"ArrowDown",preventDefault(){}});
    assert.equal(autocompleteContext.c211ContractActiveIndex,0);
    component.methods.onC211ContractKeydown.call(autocompleteContext,{key:"Enter",preventDefault(){}});
    assert.equal(autocompleteContext.form.contractCode,"AA");
    autocompleteContext.c211ContractOpen=true;
    component.methods.onC211ContractKeydown.call(autocompleteContext,{key:"Escape",preventDefault(){}});
    assert.equal(autocompleteContext.c211ContractOpen,false);
    assert.match(styles, /@media print/);
    assert.match(styles, /size:portrait/);
    const preview = fs.readFileSync("Views/Report/_C211ContractBalancePreview.cshtml", "utf8");
    assert.match(preview, /class="c211-print-document"/);
    assert.match(preview, /Model\.Summary\.SelfGrandTotal/);
    assert.match(preview, /Model\.Summary\.ClaimGrandTotal/);
    assert.match(preview, /group\.SelfAmount/);
    assert.match(preview, /row\.MedicalRecordNo/);
    assert.match(styles, /\.c211-print-document\{[^}]*page:c211-portrait/);
    assert.match(styles, /@page c211-portrait\{size:A4 portrait/);
    assert.match(styles, /\.c211-print-group\{[^}]*break-inside:avoid/);
    assert.match(styles, /\.c211-print-table thead\{display:table-header-group/);
    assert.match(appSource, /C211:\s*window\.ReportComponents\.ReportTemplate/);

    let previewRequest;
    global.fetch = async (url, options) => {
        assert.equal(url, "/Report/C211/Preview");
        previewRequest = JSON.parse(options.body);
        return { ok: true, text: async () => "<section class='c211-print-document'>complete report</section>" };
    };
    const previewContext = {
        isC211: true, hasResults: true, legacyPreviewLoading: false,
        legacyPreviewOpen: false, legacyPreviewHtml: "stale html", validationMessage: "",
        form: { endDate: "2026-09-10", encounterSource: "O", contractCode: " TT " },
        currentPage: 2, pageSize: 30, rows: [{ contractCode: "TT", medicalRecordNo: "current row" }],
        openLegacyPreview: component.methods.openLegacyPreview
    };
    await component.methods.previewC211.call(previewContext);
    assert.deepEqual(previewRequest, { endDate: "2026-09-10", encounterSource: "O", contractCode: "TT" });
    assert.equal(previewContext.legacyPreviewOpen, true);
    assert.match(previewContext.legacyPreviewHtml, /complete report/);
    assert.equal(previewContext.currentPage, 2);
    assert.equal(previewContext.pageSize, 30);
    assert.deepEqual(previewContext.rows, [{ contractCode: "TT", medicalRecordNo: "current row" }]);
    component.methods.closeLegacyPreview.call(previewContext);
    assert.equal(previewContext.legacyPreviewOpen, false);
    assert.equal(previewContext.legacyPreviewHtml, "");
    assert.equal(previewContext.currentPage, 2);
    assert.equal(previewContext.pageSize, 30);
    assert.deepEqual(previewContext.rows, [{ contractCode: "TT", medicalRecordNo: "current row" }]);
}

async function verifiesC212UsesSharedDatesAndPaginatedResults() {
    const configuration = window.ReportConfigurations.C212;
    assert.equal(configuration.serverPaged, false);
    assert.equal(configuration.advancedConditions, false);
    assert.equal(configuration.endDateOnly, true);
    assert.equal(configuration.encounterSource, undefined);
    const initial = component.data.call({
        selectedReport: { code: "C212" }, defaultStartDate: "2026-09-11", defaultEndDate: "2026-09-11"
    });
    assert.equal(initial.form.startDate, "2026-09-11");
    assert.equal(initial.form.endDate, "2026-09-10");

    const columns = [
        { key: "accountingDate", label: "日期" },
        { key: "medicalRecordNo", label: "病歷號" },
        { key: "patientName", label: "姓名" },
        { key: "amount", label: "金額" }
    ];
    const resultRows = Array.from({ length: 28 }, (_, index) => ({
        accountingDate: "115/09/10", medicalRecordNo: `A${index + 1}`,
        patientName: `病患${index + 1}`, amount: index + 1
    }));
    const resultSummary = {
        title: "骨庫餘額明細表", reportDate: "資料日期：115/09/10",
        programNo: "OpdAccRpt", reportNo: "PFin2Balance42",
        processDateTime: "115/09/10 12:00:00", editUser: "tester",
        totalAmount: 406, dataStatusMessage: "目前無法確認資料完整性"
    };
    let responseData = { columns, data: resultRows, summary: resultSummary };
    let request;
    const c212Fetch = async (_url, options) => {
        request = JSON.parse(options.body);
        return { ok: true, json: async () => responseData };
    };
    global.fetch = c212Fetch;
    const context = {
        ...initial, selectedReport: { code: "C212" }, defaultStartDate: "2026-09-11", defaultEndDate: "2026-09-11",
        isC212: true, isC211: false,
        isC23: false, isC24: false, isC21: false, isEndDateOnly: true,
        hasEncounterSource: false, hasStationOrBedPrefix: false, hasCashierUserId: false,
        hasCashierCashSort: false, hasBillingCode: false, hasReceivableBalanceType: false,
        hasAdvancedConditions: false, isServerPaged: false, serverTotalCount: 0,
        serverTotalPages: 0, summaries: [], c211Summary: null, c212Summary: null,
        isLoading: false, currentPage: 1, pageSize: 10,
        get filteredRows() { return this.rows; },
        get hasResults() { return this.rows.length > 0; },
        get totalPages() { return component.computed.totalPages.call(this); },
        $emit: () => {}
    };
    await component.methods.fetchResults.call(context);
    assert.equal(Object.hasOwn(request, "startDate"), false);
    assert.equal(request.endDate, "2026-09-10");
    assert.equal(Object.hasOwn(request, "encounterSource"), false);
    assert.equal(Object.hasOwn(request, "pageNumber"), false);
    assert.equal(context.c212Summary.dataStatusMessage, "目前無法確認資料完整性");
    assert.equal(component.computed.totalCount.call(context), 28);
    assert.equal(context.totalPages, 3);
    assert.deepEqual(component.computed.pagedRows.call(context).map(row => row.medicalRecordNo),
        Array.from({ length: 10 }, (_, index) => `A${index + 1}`));
    await component.methods.goToPage.call(context, context.totalPages);
    assert.equal(context.currentPage, 3);
    assert.deepEqual(component.computed.pagedRows.call(context).map(row => row.medicalRecordNo),
        Array.from({ length: 8 }, (_, index) => `A${index + 21}`));
    context.pageSize = 30;
    await component.methods.changePageSize.call(context);
    assert.equal(context.currentPage, 1);
    assert.equal(context.totalPages, 1);
    assert.equal(component.computed.pagedRows.call(context).length, 28);

    context.pageSize = 10;
    context.currentPage = 2;
    let printCalls = 0;
    global.window.print = () => { printCalls++; };
    component.methods.openC212Preview.call(context);
    assert.equal(context.c212PreviewOpen, true);
    assert.equal(context.c212PreviewRows.length, 28);
    assert.equal(context.c212PreviewRows[27].medicalRecordNo, "A28");
    assert.equal(context.c212PreviewSummary.totalAmount, 406);
    assert.equal(context.currentPage, 2);
    assert.equal(context.pageSize, 10);
    assert.equal(printCalls, 0);
    component.methods.closeC212Preview.call(context);
    assert.equal(context.c212PreviewOpen, false);
    assert.equal(context.currentPage, 2);
    assert.equal(context.c212PreviewRows.length, 28);
    component.methods.printC212Preview.call(context);
    assert.equal(printCalls, 0);
    component.methods.openC212Preview.call(context);
    assert.equal(printCalls, 0);
    component.methods.printC212Preview.call(context);
    assert.equal(printCalls, 1);
    component.methods.closeC212Preview.call(context);

    context.c212PreviewOpen = true;
    context.c212PreviewRows = resultRows.map(row => ({ ...row }));
    context.c212PreviewSummary = { ...resultSummary };
    responseData = { columns, data: [], summary: { ...resultSummary, totalAmount: 0 } };
    global.fetch = async (_url, options) => {
        request = JSON.parse(options.body);
        assert.equal(context.c212PreviewOpen, false);
        assert.deepEqual(context.c212PreviewRows, []);
        assert.equal(context.c212PreviewSummary, null);
        return { ok: true, json: async () => responseData };
    };
    await component.methods.fetchResults.call(context);
    assert.equal(context.hasResults, false);
    assert.equal(context.c212PreviewOpen, false);
    assert.deepEqual(context.c212PreviewRows, []);
    assert.equal(context.c212PreviewSummary, null);
    component.methods.openC212Preview.call(context);
    assert.equal(context.c212PreviewOpen, false);

    responseData = { columns, data: resultRows, summary: resultSummary };
    global.fetch = c212Fetch;
    await component.methods.fetchResults.call(context);
    component.methods.openC212Preview.call(context);
    assert.equal(context.c212PreviewOpen, true);
    global.fetch = async () => {
        assert.equal(context.c212PreviewOpen, false);
        assert.deepEqual(context.c212PreviewRows, []);
        assert.equal(context.c212PreviewSummary, null);
        throw new Error("C212 unavailable");
    };
    await component.methods.fetchResults.call(context);
    assert.equal(context.c212PreviewOpen, false);
    assert.deepEqual(context.c212PreviewRows, []);
    assert.equal(context.c212PreviewSummary, null);
    assert.deepEqual(context.rows, []);

    context.rows = resultRows;
    context.c212Summary = resultSummary;
    component.methods.openC212Preview.call(context);
    assert.equal(context.c212PreviewOpen, true);
    component.methods.resetForm.call(context);
    assert.equal(context.c212PreviewOpen, false);
    assert.deepEqual(context.c212PreviewRows, []);
    assert.equal(context.c212PreviewSummary, null);

    const emptyPreviewContext = {
        isC212: true, hasResults: false, c212Summary: { totalAmount: 0 }, rows: [],
        c212PreviewOpen: false, c212PreviewRows: [], c212PreviewSummary: null
    };
    component.methods.openC212Preview.call(emptyPreviewContext);
    assert.equal(emptyPreviewContext.c212PreviewOpen, false);

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    const styles = fs.readFileSync("wwwroot/css/site.css", "utf8");
    assert.match(markup, /v-if="!isLoading && hasResults" class="pagination"/);
    assert.match(markup, /每頁顯示[\s\S]*<option :value="10">10 筆<\/option>[\s\S]*<option :value="30">30 筆<\/option>[\s\S]*<option :value="50">50 筆<\/option>/);
    assert.match(markup, /:class="\{ 'c212-results-table': isC212 \}"/);
    assert.match(markup, /amount: \(isC212 && column\.key === 'amount'\) \|\| \(isC141 && column\.isAmount\)/);
    assert.match(markup, /v-else-if="isC212"[^>]*v-on:click="openC212Preview">預覽／列印/);
    assert.match(markup, /c212PreviewOpen && c212PreviewSummary[^>]*role="dialog" aria-modal="true"/);
    assert.match(markup, /c212PreviewSummary\.programNo[\s\S]*c212PreviewSummary\.reportNo[\s\S]*c212PreviewSummary\.processDateTime[\s\S]*c212PreviewSummary\.editUser/);
    assert.match(markup, /v-for="\(row, index\) in c212PreviewRows"/);
    assert.match(markup, /c212PreviewSummary\.totalAmount/);
    assert.match(markup, /c212PreviewSummary\.dataStatusMessage/);
    assert.match(markup, /isC212 \|\| isC211 \? '查無資料！'/);
    assert.match(markup, /v-on:click="printC212Preview">確認列印/);
    assert.doesNotMatch(markup, /printC211|v-on:click="printC212Preview"[^>]*>列印/);
    assert.match(styles, /body:has\(\.c212-preview-overlay \.c212-print-document\) \*\{visibility:hidden!important\}/);
    assert.match(styles, /body:has\(\.c212-preview-overlay \.c212-print-document\) \.c212-print-document,body:has\(\.c212-preview-overlay \.c212-print-document\) \.c212-print-document \*\{visibility:visible!important\}/);
    assert.match(styles, /\.c212-preview-actions\{display:none!important\}/);
    assert.match(appSource, /C212:\s*window\.ReportComponents\.ReportTemplate/);
}

async function verifiesC211PreviewFailureEmptyAndPrintGuards() {
    const context = {
        isC211: true, hasResults: true, legacyPreviewLoading: false,
        legacyPreviewOpen: true, legacyPreviewHtml: "stale report", validationMessage: "",
        form: { endDate: "2026-09-10", encounterSource: "I", contractCode: "" },
        currentPage: 3, pageSize: 50, rows: [{ contractCode: "AA" }],
        openLegacyPreview: component.methods.openLegacyPreview
    };
    let requests = 0;
    global.fetch = async () => {
        requests++;
        assert.equal(context.legacyPreviewOpen, false);
        assert.equal(context.legacyPreviewHtml, "");
        return { ok: false, status: 404, json: async () => ({ title: "查無符合條件的資料。" }) };
    };
    await component.methods.previewC211.call(context);
    assert.equal(requests, 1);
    assert.equal(context.legacyPreviewLoading, false);
    assert.equal(context.legacyPreviewOpen, false);
    assert.equal(context.legacyPreviewHtml, "");
    assert.equal(context.validationMessage, "查無符合條件的資料。");
    assert.equal(context.currentPage, 3);
    assert.equal(context.pageSize, 50);
    assert.deepEqual(context.rows, [{ contractCode: "AA" }]);

    global.fetch = async () => ({ ok: true, text: async () => "  \n" });
    await component.methods.previewC211.call(context);
    assert.equal(context.legacyPreviewOpen, false);
    assert.equal(context.legacyPreviewHtml, "");
    assert.equal(context.validationMessage, "查無此筆資料");

    context.hasResults = false;
    global.fetch = async () => { requests++; throw new Error("Should not request an empty result"); };
    await component.methods.previewC211.call(context);
    assert.equal(requests, 1);
    assert.equal(context.legacyPreviewOpen, false);

    let printCalls = 0;
    global.window.print = () => { printCalls++; };
    component.methods.printLegacyPreview.call({ legacyPreviewOpen: false });
    assert.equal(printCalls, 0);
    component.methods.printLegacyPreview.call({ legacyPreviewOpen: true });
    assert.equal(printCalls, 1);
}

function verifiesC7PreviewRemainsInCurrentPage() {
    const view = fs.readFileSync("Views/Report/_C7DailyChargeDetailReport.cshtml", "utf8");
    const script = fs.readFileSync("wwwroot/js/reports/c7-report.js", "utf8");
    assert.match(view, /v-if="previewOpen && previewHtml" class="report-preview-overlay" role="dialog"/);
    assert.match(view, /closePreview/);
    assert.match(view, /printPreview/);
    assert.doesNotMatch(script, /window\.open|target.?=.?["']_blank/);
}

async function verifiesC13SharedPaginationSkeletonAndPreviewContract() {
    const configuration = window.ReportConfigurations.C13;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.c13, true);
    assert.equal(component.computed.hasAdvancedConditions.call({ reportConfiguration: configuration }), false);
    assert.equal(component.computed.hasAdvancedConditions.call({
        reportConfiguration: window.ReportConfigurations.C22
    }), false);
    assert.equal(component.computed.isServerPaged.call({ selectedReport: { code: "C13" } }), true);
    assert.equal(component.computed.isC13.call({ reportConfiguration: configuration }), true);
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    const templateSource = fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8");
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const preview = fs.readFileSync("Views/Report/_C13HighRiskEmergencyPreview.cshtml", "utf8");
    assert.match(appSource, /C13:\s*window\.ReportComponents\.ReportTemplate/);
    assert.match(markup, /v-else-if="isC13"[\s\S]*?previewC13/);
    assert.match(markup, /v-if="hasAdvancedConditions" class="advanced-filter-toggle"/);
    assert.match(markup, /v-if="hasAdvancedConditions" v-show="advancedOpen" class="advanced-grid"/);
    assert.match(markup, /<partial name="_TableSkeleton" \/>/);
    assert.match(markup, /legacyPreviewOpen[\s\S]*report-preview-overlay[\s\S]*role="dialog"[\s\S]*closeLegacyPreview[\s\S]*printLegacyPreview/);
    const c13Method = templateSource.match(/async previewC13\(\) \{[\s\S]*?\n            \},/)?.[0] || "";
    assert.doesNotMatch(c13Method, /window\.open/);
    assert.match(preview, /社服需求急診高危險群個案明細表/);
    assert.match(preview, /處理日期/);
    assert.match(preview, /急診床號/);
    assert.doesNotMatch(preview, /@Model\.GeneratedBy/);

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return { ok: true, json: async () => ({ columns: [], data: [], totalCount: 28, totalPages: 3, pageNumber: 2 }) };
    };
    const context = {
        selectedReport: { code: "C13" },
        form: { startDate: "2026-09-14", endDate: "2026-09-15", department: "" },
        currentPage: 2, pageSize: 10, isLoading: false, hasSearched: false,
        columns: [], rows: [], serverTotalCount: 0, serverTotalPages: 0, validationMessage: "",
        hasEncounterSource: false, hasStationOrBedPrefix: false, hasCashierUserId: false,
        hasCashierCashSort: false, hasBillingCode: false, hasReceivableBalanceType: false,
        isEndDateOnly: false, isServerPaged: true
    };
    await component.methods.fetchResults.call(context);
    assert.deepEqual(requestBody, {
        reportCode: "C13", startDate: "2026-09-14", endDate: "2026-09-15",
        pageNumber: 2, pageSize: 10
    });
    assert.equal(context.serverTotalCount, 28);
    assert.equal(context.serverTotalPages, 3);
}

async function verifiesC143SharedQueryPagingAndResetContract() {
    const configuration = window.ReportConfigurations.C143;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.encounterSource.defaultValue, "OpdEr");
    assert.equal(configuration.reportType.defaultValue, "Difference");
    const initial = component.data.call({
        selectedReport: { code: "C143" }, defaultStartDate: "2026-09-15", defaultEndDate: "2026-09-15"
    });
    assert.equal(initial.form.startDate, "2026-09-14");
    assert.equal(initial.form.endDate, "2026-09-14");
    assert.equal(initial.form.encounterSource, "OpdEr");
    assert.equal(initial.form.reportType, "Difference");

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return { ok: true, json: async () => ({
            columns: [{ key: "visitDate", label: "就診日" }], data: [{ visitDate: "1150914" }],
            totalCount: 31, totalPages: 4, pageNumber: 2
        }) };
    };
    const context = {
        ...initial, selectedReport: { code: "C143" }, currentPage: 2, pageSize: 10,
        isLoading: false, hasSearched: false, columns: [], rows: [], serverTotalCount: 0,
        serverTotalPages: 0, validationMessage: "", isEndDateOnly: false, isServerPaged: true,
        hasEncounterSource: true, isC24: false, isC10: false, isC143: true,
        hasStationOrBedPrefix: false, hasCashierUserId: false, hasCashierCashSort: false,
        hasBillingCode: false, hasReceivableBalanceType: false, isC21: false, isC23: false,
        isC211: false, canForceC24Rebuild: false, hasAdvancedConditions: false
    };
    await component.methods.fetchResults.call(context);
    assert.equal(requestBody.source, "OpdEr");
    assert.equal(requestBody.reportType, "Difference");
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 10);
    assert.equal(context.serverTotalCount, 31);
    assert.equal(context.serverTotalPages, 4);

    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(appSource, /C143:\s*window\.ReportComponents\.ReportTemplate/);
    assert.match(markup, /v-if="hasReportType"/);
    assert.match(markup, /v-model="form\.reportType"/);
    assert.match(markup, /v-if="isC143"[\s\S]*?改成昨天/);
    assert.match(markup, /<partial name="_TableSkeleton" \/>/);
}

async function verifiesC144SharedQueryPagingAndExportContract() {
    const configuration = window.ReportConfigurations.C144;
    assert.equal(configuration.serverPaged, true);
    assert.equal(configuration.encounterSource.defaultValue, "OpdEr");
    const initial = component.data.call({
        selectedReport: { code: "C144" }, defaultStartDate: "2026-09-15", defaultEndDate: "2026-09-16"
    });
    assert.equal(initial.form.startDate, "2026-09-15");
    assert.equal(initial.form.endDate, "2026-09-16");
    assert.equal(initial.form.encounterSource, "OpdEr");

    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return { ok: true, json: async () => ({
            columns: [{ key: "encounterType", label: "診別" }],
            data: [{ encounterType: "E" }], totalCount: 31, totalPages: 4, pageNumber: 2
        }) };
    };
    const context = {
        ...initial, selectedReport: { code: "C144" }, currentPage: 2, pageSize: 10,
        isLoading: false, hasSearched: false, columns: [], rows: [], serverTotalCount: 0,
        serverTotalPages: 0, validationMessage: "", isEndDateOnly: false, isServerPaged: true,
        hasEncounterSource: true, isC24: false, isC10: false, isC143: false, isC144: true,
        hasStationOrBedPrefix: false, hasCashierUserId: false, hasCashierCashSort: false,
        hasBillingCode: false, hasReceivableBalanceType: false, isC21: false, isC23: false,
        isC211: false, canForceC24Rebuild: false, hasAdvancedConditions: false
    };
    await component.methods.fetchResults.call(context);
    assert.equal(requestBody.source, "OpdEr");
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 10);
    assert.equal(context.serverTotalCount, 31);

    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    const templateSource = fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8");
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    assert.match(appSource, /C144:\s*window\.ReportComponents\.ReportTemplate/);
    assert.match(templateSource, /\["C1",\s*"C21",\s*"C22",\s*"C23",\s*"C24",\s*"C25",\s*"C27",\s*"C28",\s*"C29",[\s\S]*"C214"/);
    assert.match(templateSource, /\["C10",\s*"C143",\s*"C144"\]\.includes\(reportCode\)/);
    assert.match(markup, /<partial name="_TableSkeleton" \/>/);
}

async function verifiesFirstAndLastPageControlsReuseExistingPagination() {
    const sharedMarkup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const searchMarkup = fs.readFileSync("Views/Report/_SearchResults.cshtml", "utf8");
    const c11Markup = fs.readFileSync("Views/Report/_C11ReceivablesCollectionReport.cshtml", "utf8");

    assert.match(sharedMarkup, /aria-label="第一頁"[^>]*goToPage\(1\)[^>]*>‹‹<\/button>[\s\S]*aria-label="最後一頁"[^>]*goToPage\(totalPages\)[^>]*>››<\/button>/);
    assert.match(sharedMarkup, /aria-label="第一頁"[^>]*:disabled="isLoading \|\| currentPage === 1"/);
    assert.match(sharedMarkup, /aria-label="最後一頁"[^>]*:disabled="isLoading \|\| currentPage === totalPages"/);
    assert.match(searchMarkup, /aria-label="第一頁"[^>]*currentPage = 1[^>]*>‹‹<\/button>[\s\S]*aria-label="最後一頁"[^>]*currentPage = totalPages[^>]*>››<\/button>/);
    assert.match(c11Markup, /aria-label="第一頁"[^>]*currentPage = 1[^>]*>‹‹<\/button>[\s\S]*aria-label="最後一頁"[^>]*currentPage = totalPages[^>]*>››<\/button>/);

    let fetchCount = 0;
    const serverContext = {
        isLoading: false, totalPages: 12, currentPage: 5, isServerPaged: true,
        async fetchResults() { fetchCount++; }
    };
    await component.methods.goToPage.call(serverContext, 12);
    assert.equal(serverContext.currentPage, 12);
    assert.equal(fetchCount, 1);
    assert.equal(serverContext.totalPages, 12);
}

async function verifiesC15SharedPagingGroupingAndPrintContract() {
    let requestBody;
    global.fetch = async (_url, options) => {
        requestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                columns: [],
                data: [{ type: "1", encounterOrdinal: 0, rl001: 100 }],
                totalCount: 31,
                totalPages: 4,
                pageNumber: 2,
                pageSize: 10,
                summary: { groups: [{ type: "1", title: "社工室生活輔具租借月報表" }] }
            })
        };
    };
    const context = {
        selectedReport: { code: "C15" },
        form: { startDate: "2026-09-01", endDate: "2026-09-16" },
        currentPage: 2, pageSize: 10, isLoading: false, hasSearched: false,
        columns: [], rows: [], summaries: [], c211Summary: null, c212Summary: null,
        c15Summary: null, serverTotalCount: 0, serverTotalPages: 0,
        validationMessage: "", isServerPaged: true, isC15: true, isC211: false,
        isC212: false, hasEncounterSource: false, hasAdvancedConditions: false,
        hasReceivableBalanceType: false, hasReportType: false, isC21: false,
        isC23: false, isC24: false, isC10: false, isC143: false, isC144: false,
        canForceC24Rebuild: false, $emit: () => {}
    };

    await component.methods.fetchResults.call(context);

    assert.equal(requestBody.reportCode, "C15");
    assert.equal(requestBody.startDate, "2026-09-01");
    assert.equal(requestBody.endDate, "2026-09-16");
    assert.equal(requestBody.pageNumber, 2);
    assert.equal(requestBody.pageSize, 10);
    assert.equal(context.serverTotalCount, 31);
    assert.equal(context.c15Summary.groups[0].type, "1");

    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const appSource = fs.readFileSync("wwwroot/js/report-app.js", "utf8");
    const css = fs.readFileSync("wwwroot/css/site.css", "utf8");
    assert.match(appSource, /C15:\s*window\.ReportComponents\.ReportTemplate/);
    assert.match(markup, /v-else-if="isC15" class="c15-report"/);
    const resultStart = markup.indexOf('<div v-else-if="isC15" class="c15-report">');
    const previewStart = markup.indexOf('<div v-if="c15PreviewOpen" class="c15-preview-overlay"');
    const resultMarkup = markup.slice(resultStart, previewStart);
    assert.doesNotMatch(resultMarkup, /group\.title/);
    assert.match(markup, /租借日期[\s\S]*病歷號[\s\S]*租借人[\s\S]*輔具編號[\s\S]*租借期限[\s\S]*amount001Label[\s\S]*歸還日期[\s\S]*amount002Label[\s\S]*amount004Label[\s\S]*amount003Label/);
    assert.match(markup, /class="c15-preview-overlay"[\s\S]*c15PreviewPages[\s\S]*亞東紀念醫院[\s\S]*page\.title[\s\S]*資料日期[\s\S]*處理時間[\s\S]*page\.pageNumber[\s\S]*class="c15-print-table"/);
    assert.match(markup, /<partial name="_TableSkeleton" \/>/);
    assert.match(markup, /aria-label="第一頁"[\s\S]*aria-label="最後一頁"/);
    assert.match(css, /@page c15-landscape \{ size:A4 landscape;/);
    assert.match(css, /@media print\{\.c15-preview-overlay,\.c15-preview-overlay \*\{visibility:visible!important\}/);
    assert.match(css, /\.c15-preview-overlay\{[^}]*overflow-y:auto/);
    assert.match(css, /\.c15-paper\{[^}]*break-after:page/);

    const previewRows = Array.from({ length: 37 }, (_, encounterOrdinal) => ({
        type: "1", encounterOrdinal, rl001: encounterOrdinal + 1
    }));
    let previewRequestBody;
    global.fetch = async (_url, options) => {
        previewRequestBody = JSON.parse(options.body);
        return {
            ok: true,
            json: async () => ({
                data: previewRows,
                totalCount: 37,
                summary: { groups: [{ type: "1", title: "社工室生活輔具租借月報表", rl001Total: 703 }] }
            })
        };
    };
    const previewContext = {
        isC15: true, hasResults: true, c15PreviewOpen: false, c15PreviewLoading: false,
        c15PreviewRows: [], c15PreviewSummary: null, c15PrintGeneratedAt: "",
        validationMessage: "", serverTotalCount: 37, currentPage: 2, pageSize: 10,
        form: { startDate: "2026-09-01", endDate: "2026-09-16" }
    };
    await component.methods.openC15Preview.call(previewContext);
    assert.deepEqual(previewRequestBody, {
        reportCode: "C15", startDate: "2026-09-01", endDate: "2026-09-16",
        pageNumber: 1, pageSize: 37
    });
    assert.equal(previewContext.c15PreviewOpen, true);
    assert.equal(previewContext.c15PreviewRows.length, 37);
    assert.equal(previewContext.currentPage, 2);
    assert.equal(previewContext.pageSize, 10);
    assert.ok(previewContext.c15PrintGeneratedAt.length > 0);
    const previewPages = component.computed.c15PreviewPages.call(previewContext);
    assert.equal(previewPages.length, 3);
    assert.deepEqual(previewPages.map(page => page.rows.length), [18, 18, 1]);
    assert.deepEqual(previewPages.map(page => page.pageNumber), [1, 2, 3]);
    assert.deepEqual(previewPages.map(page => page.totalPages), [3, 3, 3]);
    assert.deepEqual(previewPages.map(page => page.showTotals), [false, false, true]);
    component.methods.closeC15Preview.call(previewContext);
    assert.equal(previewContext.c15PreviewOpen, false);

    global.fetch = async () => ({
        ok: false, status: 500, json: async () => ({ title: "預覽暫時無法使用" })
    });
    previewContext.c15PreviewRows = previewRows;
    await component.methods.openC15Preview.call(previewContext);
    assert.equal(previewContext.c15PreviewOpen, false);
    assert.deepEqual(previewContext.c15PreviewRows, []);
    assert.equal(previewContext.currentPage, 2);
    assert.match(previewContext.validationMessage, /預覽暫時無法使用/);
    assert.equal(component.methods.displayRocDate.call({}, "2026-09-16"), "115/09/16");
}

async function verifiesC3ModalPreviewAndPrintContract() {
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const css = fs.readFileSync("wwwroot/css/site.css", "utf8");
    assert.match(markup, /class="c3-preview-overlay"[^>]*role="dialog"[^>]*aria-modal="true"/);
    assert.match(markup, /closeC3Preview[\s\S]*printC3Preview[\s\S]*c3PreviewPages/);
    assert.match(markup, /c3Preview\.title[\s\S]*DateB[\s\S]*dateB[\s\S]*DateE[\s\S]*dateE[\s\S]*UserID[\s\S]*userId[\s\S]*Today[\s\S]*today[\s\S]*page\.pageNumber/);
    assert.match(markup, /診別[\s\S]*領用部門[\s\S]*部門代碼[\s\S]*批價碼[\s\S]*材料碼[\s\S]*材料名稱[\s\S]*庫別[\s\S]*數量/);
    assert.match(markup, /detailType\) === 1[\s\S]*醫師代碼[\s\S]*病歷號[\s\S]*病患姓名[\s\S]*報表日[\s\S]*醫師姓名[\s\S]*自費/);
    assert.match(css, /@page c3-landscape\{size:A4 landscape;margin:0\}/);
    assert.match(css, /\.c3-paper\{[^}]*break-after:page/);
    assert.match(css, /@media print\{body:has\(\.c3-preview-overlay\) \*\{visibility:hidden!important\}/);
    assert.match(css, /\.c3-preview-actions\{display:none!important\}/);

    const previewRows = Array.from({ length: 31 }, (_, index) => ({
        diagnose: "門診", dispensary: "門診護理站", section: "15011",
        chargeCode: `C${index}`, materialCode: `M${index}`, materialName: `材料${index}`,
        inventoryType: "物流", totalSum: index + 1
    }));
    let previewUrl;
    let previewOptions;
    global.fetch = async (url, options) => {
        previewUrl = url;
        previewOptions = options;
        return {
            ok: true,
            json: async () => ({
                title: "亞東紀念醫院門急診各護理站計價品彙總表__物流",
                dateB: "115/09/01", dateE: "115/09/16", userId: "tester",
                today: "115/09/17 10:20:30", detailType: 0, rows: previewRows
            })
        };
    };
    const context = {
        isC3: true, hasResults: true, c3PreviewOpen: false, c3PreviewLoading: false,
        c3Preview: null, validationMessage: "", currentPage: 3, pageSize: 10,
        form: {
            startDate: "2026-09-01", endDate: "2026-09-16", encounterSource: "O",
            detailType: 0, logisticsType: 1, departmentCode: " 15011 ",
            roomCodes: " 3J01,3J02 ", chargeCodes: " C1,C2 "
        }
    };

    await component.methods.openC3Preview.call(context);
    assert.equal(previewUrl, "/Report/C3/Preview");
    assert.equal(previewOptions.headers.Accept, "application/json");
    assert.deepEqual(JSON.parse(previewOptions.body), {
        reportCode: "C3", startDate: "2026-09-01", endDate: "2026-09-16",
        source: "O", detailType: 0, logisticsType: 1, departmentCode: "15011",
        roomCodes: "3J01,3J02", chargeCodes: "C1,C2", pageNumber: 1, pageSize: 10
    });
    assert.equal(context.c3PreviewOpen, true);
    assert.equal(context.c3Preview.rows.length, 31);
    assert.equal(context.currentPage, 3);
    assert.equal(context.pageSize, 10);
    const pages = component.computed.c3PreviewPages.call(context);
    assert.deepEqual(pages.map(page => page.rows.length), [20, 11]);
    assert.deepEqual(pages.map(page => page.pageNumber), [1, 2]);

    component.methods.closeC3Preview.call(context);
    assert.equal(context.c3PreviewOpen, false);
    assert.equal(context.currentPage, 3);

    let printCalls = 0;
    global.window.print = () => { printCalls++; };
    context.c3PreviewOpen = true;
    component.methods.printC3Preview.call(context);
    assert.equal(printCalls, 1);

    global.fetch = async () => ({
        ok: true,
        json: async () => ({ title: "empty", rows: [] })
    });
    await component.methods.openC3Preview.call(context);
    assert.equal(context.c3PreviewOpen, false);
    assert.equal(context.c3Preview, null);
    assert.match(context.validationMessage, /查無符合條件/);

    global.fetch = async () => ({
        ok: false, status: 503, json: async () => ({ title: "預覽暫時無法使用" })
    });
    await component.methods.openC3Preview.call(context);
    assert.equal(context.c3PreviewOpen, false);
    assert.equal(context.c3Preview, null);
    assert.match(context.validationMessage, /預覽暫時無法使用/);
}

async function verifiesC141SelectionUsesFourKeysAndPersistsAcrossPages() {
    const rowA = {
        key: { admissionDate: "1150101", admissionTime: "1", admissionRoom: "A01", admissionNumber: 1 },
        isAutomaticallySelected: true
    };
    const rowB = {
        key: { admissionDate: "1150101", admissionTime: "1", admissionRoom: "A01", admissionNumber: 2 },
        isAutomaticallySelected: false
    };
    const rowC = {
        key: { admissionDate: "1150101", admissionTime: "1", admissionRoom: "A02", admissionNumber: 1 },
        isAutomaticallySelected: false
    };
    const context = {
        c141SelectionOverrides: {},
        pagedRows: [rowA, rowB],
        c141RowKey(row) { return component.methods.c141RowKey.call(this, row); }
    };

    assert.equal(component.methods.isC141RowSelected.call(context, rowA), true);
    component.methods.setC141RowSelected.call(context, rowA, { target: { checked: false } });
    component.methods.setC141RowSelected.call(context, rowB, { target: { checked: true } });
    context.pagedRows = [rowC];
    component.methods.setC141CurrentPageSelection.call(context, { target: { checked: true } });

    assert.equal(component.methods.isC141RowSelected.call(context, rowA), false);
    assert.equal(component.methods.isC141RowSelected.call(context, rowB), true);
    assert.equal(component.methods.isC141RowSelected.call(context, rowC), true);
    context.pagedRows = [rowA, rowB];
    assert.equal(component.methods.isC141RowSelected.call(context, rowA), false);
    assert.equal(component.methods.isC141RowSelected.call(context, rowB), true);
    assert.equal(Object.keys(context.c141SelectionOverrides).length, 3);
}

async function verifiesC141NewQueryClearsManualSelections() {
    const context = {
        isC141: true,
        isC212: false,
        isC142: false,
        isEndDateOnly: true,
        hasEncounterSource: false,
        reportConfiguration: {},
        form: { endDate: "2026-10-13" },
        currentPage: 4,
        validationMessage: "",
        c141SelectionOverrides: { manual: true },
        clearC141Selection() { component.methods.clearC141Selection.call(this); },
        async fetchResults() { this.fetched = true; }
    };

    await component.methods.search.call(context);

    assert.equal(context.currentPage, 1);
    assert.deepEqual(context.c141SelectionOverrides, {});
    assert.equal(context.fetched, true);
}

async function verifiesC141SideEffectsSubmitSelectedKeysOnly() {
    const markup = fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8");
    const templateSource = fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8");
    const rowA = {
        key: { admissionDate: "1150101", admissionTime: "1", admissionRoom: "A01", admissionNumber: 1 },
        isAutomaticallySelected: false
    };
    const rowB = {
        key: { admissionDate: "1150101", admissionTime: "1", admissionRoom: "A02", admissionNumber: 2 },
        isAutomaticallySelected: true
    };
    const context = {
        c141SelectionOverrides: { [JSON.stringify(["1150101", "1", "A01", 1])]: true },
        pagedRows: [rowB],
        isC141RowSelected(row) { return component.methods.isC141RowSelected.call(this, row); },
        c141RowKey(row) { return component.methods.c141RowKey.call(this, row); }
    };

    const selected = component.methods.c141SelectedKeys.call(context);

    assert.deepEqual(selected, [rowA.key, rowB.key]);
    assert.match(markup, /通知單預覽也會更新通知日期並寫入交易紀錄/);
    assert.match(markup, /v-on:click="previewC141Notice"/);
    assert.match(markup, /v-on:click="sendC141LineBot"/);
    assert.match(templateSource, /SOAP 結果不明時系統不會自動重送/);
    assert.match(templateSource, /fetch\("\/Report\/C141\/Notice\/Preview"/);
}

verifiesC171RequestsServerPages()
    .then(verifiesC1RequestsDateRangeAndServerPageOnly)
    .then(verifiesC25UsesSharedServerPagedLifecycle)
    .then(verifiesC27UsesCutoffDateOnlySharedLifecycle)
    .then(verifiesC28UsesCutoffDateOnlySharedLifecycle)
    .then(verifiesC29SharedConfigurationPayloadPaginationAndReset)
    .then(verifiesC174RequestsServerPagesAndReplacesRows)
    .then(verifiesC174EmptyResultUsesZeroMetadata)
    .then(verifiesC18RequestsSourceAndUsesServerMetadata)
    .then(verifiesC19RequestsServerPageAndPrefix)
    .then(verifiesC18RejectsCrossYearBeforeFetch)
    .then(verifiesServerPagingDesignation)
    .then(verifiesC21SourceScopesPayloadAndWebOnlyMarkup)
    .then(verifiesRebuildVisibilityFollowsPermissionAndQueryBoundary)
    .then(verifiesC213UsesSharedComponentWithoutAdvancedConditionsAndWithExport)
    .then(verifiesC213DatePagingPayloadAndResetLifecycle)
    .then(verifiesC214SharedConfigurationPayloadTypeSwitchAndReset)
    .then(verifiesC22DefaultsResetPayloadAndMarkup)
    .then(verifiesC18DefaultsAndReset)
    .then(verifiesReportsWithoutSourceDoNotSubmitSource)
    .then(verifiesC18SourceMarkup)
    .then(verifiesOtherReportsRetainClientSlicing)
    .then(verifiesC174SynchronousExportDownloadsBlob)
    .then(verifiesC174BackgroundExportPollsAndStopsAtReady)
    .then(verifiesHealthExportPayloadKeepsReportSpecificFilters)
    .then(verifiesExportStatusMarkupAndUnmountCleanup)
    .then(verifiesLoadingStateAndExclusiveResultMarkup)
    .then(verifiesAccessibleLoadingMarkup)
    .then(verifiesSkeletonMotionAndResponsiveStyles)
    .then(verifiesC23SameMonthValidationOnlyAppliesToEncounterDateMode)
    .then(verifiesC24FormPayloadAndInpatientRoomReset)
    .then(verifiesC10UsesSharedQueryAndExportContracts)
    .then(verifiesC211CutoffSourceContractAndCompletePrintMarkup)
    .then(verifiesC211PreviewFailureEmptyAndPrintGuards)
    .then(verifiesC7PreviewRemainsInCurrentPage)
    .then(verifiesC212UsesSharedDatesAndPaginatedResults)
    .then(verifiesC13SharedPaginationSkeletonAndPreviewContract)
    .then(verifiesC143SharedQueryPagingAndResetContract)
    .then(verifiesC144SharedQueryPagingAndExportContract)
    .then(verifiesC15SharedPagingGroupingAndPrintContract)
    .then(verifiesC3ModalPreviewAndPrintContract)
    .then(verifiesC141SelectionUsesFourKeysAndPersistsAcrossPages)
    .then(verifiesC141NewQueryClearsManualSelections)
    .then(verifiesC141SideEffectsSubmitSelectedKeysOnly)
    .then(verifiesFirstAndLastPageControlsReuseExistingPagination)
    .then(() => console.log("report-template pagination tests passed"));
