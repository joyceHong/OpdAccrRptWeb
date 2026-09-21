(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.C5Report = {
        template: "#c5-report-template",
        props: ["selectedReport", "defaultStartDate", "defaultEndDate"],
        data() {
            return { isLoading: false, hasSearched: false, validationMessage: "", rows: [], columns: [], totalCount: 0, totalPages: 0, currentPage: 1, pageSize: 10, organizations: [], organizationSearchTimer: null, form: this.initialForm() };
        },
        computed: { hasResults() { return this.rows.length > 0; } },
        methods: {
            initialForm() {
                const isC6 = this.selectedReport?.code === "C6";
                return { startDate: this.defaultStartDate, endDate: this.defaultEndDate, dataSource: 0, detailType: 0, encounterType: isC6 ? 1 : 0, chargeKind: isC6 ? 1 : 0, organizationQuery: "", newOrganizationUnitCode: "", roomNo: "", chargeCode: "", insuranceIdentityCode: "" };
            },
            changeSource() { this.currentPage = 1; if (this.form.dataSource === 1) { this.form.encounterType = 0; this.form.roomNo = ""; } },
            searchOrganizations() {
                window.clearTimeout(this.organizationSearchTimer);
                const query = this.form.organizationQuery.trim();
                if (!query) { this.organizations = []; return; }
                this.organizationSearchTimer = window.setTimeout(async () => {
                    try {
                        const response = await fetch(`/reports/c5/organization-units?query=${encodeURIComponent(query)}`);
                        this.organizations = response.ok ? await response.json() : [];
                    } catch { this.organizations = []; }
                }, 250);
            },
            onOrganizationInput() { this.form.newOrganizationUnitCode = ""; this.currentPage = 1; this.searchOrganizations(); },
            selectOrganization(item) {
                const newCode = String(item?.newCode ?? "").trim().toUpperCase();
                if (!newCode) return;
                const displayName = String(item?.displayName ?? "").trim();
                this.form.newOrganizationUnitCode = newCode;
                this.form.organizationQuery = displayName ? `${newCode}|${displayName}` : newCode;
                this.organizations = [];
                this.currentPage = 1;
            },
            payload() {
                const selectedCode = String(this.form.newOrganizationUnitCode ?? "").trim().toUpperCase();
                const directCode = String(this.form.organizationQuery ?? "").trim().toUpperCase();
                const { organizationQuery, ...form } = this.form;
                return { ...form, roomNo: this.selectedReport?.code === "C6" ? "" : form.roomNo, newOrganizationUnitCode: selectedCode || directCode, pageNumber: this.currentPage, pageSize: this.pageSize };
            },
            async search() { this.currentPage = 1; await this.load(); },
            async load() { this.isLoading = true; this.validationMessage = ""; try { const r = await fetch("/reports/c5/query", { method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": token() }, body: JSON.stringify(this.payload()) }); if (!r.ok) throw new Error(await r.text()); const x = await r.json(); this.rows = x.data; this.columns = x.columns; this.totalCount = x.totalCount; this.totalPages = x.totalPages; this.currentPage = x.pageNumber; this.hasSearched = true; } catch (e) { this.rows = []; this.totalCount = 0; this.totalPages = 0; this.validationMessage = e.message; } finally { this.isLoading = false; } },
            async goToPage(p) { if (p < 1 || p > this.totalPages || p === this.currentPage) return; this.currentPage = p; await this.load(); },
            async changePageSize() { this.currentPage = 1; await this.load(); },
            resetForm() { this.form = this.initialForm(); this.organizations = []; this.rows = []; this.columns = []; this.totalCount = 0; this.totalPages = 0; this.currentPage = 1; this.pageSize = 10; this.hasSearched = false; this.validationMessage = ""; },
            async openPreview() { const r = await fetch("/reports/c5/preview", { method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": token() }, body: JSON.stringify(this.payload()) }); if (!r.ok) { this.validationMessage = await r.text(); return; } const x = await r.json(), w = window.open("", "_blank", "noopener"); if (w) { w.document.write(`<title>${x.title}</title><button onclick="print()">列印</button><h1>${x.title}</h1><p>此為新站列印版面</p><pre>${JSON.stringify(x.rows, null, 2)}</pre>`); w.document.close(); } }
        }
    };
})();
