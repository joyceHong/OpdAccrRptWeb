const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

global.window = { clearTimeout };
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/report-template.js", "utf8"));
const component = window.ReportComponents.ReportTemplate;

async function run() {
    let request;
    const context = {
        isC19: true,
        selectedReport: { canPrint: true },
        hasResults: true,
        isLoading: false,
        legacyPreviewLoading: false,
        form: {
            startDate: "2026-11-13",
            endDate: "2026-11-13",
            encounterSource: "Inpatient",
            stationOrBedPrefix: " 8B "
        },
        openLegacyPreview: async (...args) => { request = args; }
    };

    await component.methods.previewC19.call(context);
    assert.equal(request[0], "/Report/C19/Preview");
    assert.deepEqual(JSON.parse(JSON.stringify(request[1])), {
        reportCode: "C19",
        startDate: "2026-11-13",
        endDate: "2026-11-13",
        encounterSource: "Inpatient",
        stationOrBedPrefix: "8B"
    });

    request = undefined;
    context.selectedReport.canPrint = false;
    await component.methods.previewC19.call(context);
    assert.equal(request, undefined);
    assert.match(fs.readFileSync("Views/Report/_TemplateReport.cshtml", "utf8"),
        /v-else-if="isC19"[^>]*v-show="selectedReport\.canPrint"[^>]*v-on:click="previewC19"/);
}

run().then(() => console.log("C19 preview tests passed."));
