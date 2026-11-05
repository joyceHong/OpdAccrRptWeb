(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.C5Report = {
        template: "#c5-report-template",
        mixins: [window.ReportDateRangeMixin],
        props: ["selectedReport", "defaultStartDate", "defaultEndDate"],
        data() {
            return { isLoading: false, hasSearched: false, validationMessage: "", rows: [], columns: [], totalCount: 0, totalPages: 0, currentPage: 1, pageSize: 10, lastLoadedPayload: null, organizations: [], organizationMenuOpen: false, organizationSearchTimer: null, organizationSearchGeneration: 0, previewOpen: false, previewLoading: false, preview: null, filterOpen: false, filterDraft: null, form: this.initialForm() };
        },
        computed: {
            hasResults() { return this.rows.length > 0; },
            previewAvailable() { return this.hasResults && this.lastLoadedPayload === JSON.stringify(this.payload()); },
            activeFilterCount() { const f=this.form; return [f.encounterType!==0,f.organizationQuery,f.roomNo,f.chargeCode,f.insuranceIdentityCode].filter(Boolean).length; },
            previewLeadingColumns() { return this.preview?.detailType === 2 ? 8 : this.preview?.detailType === 1 ? 5 : 4; },
            previewPages() {
                const bySource = new Map();
                for (const row of this.preview?.rows ?? []) {
                    const source = this.preview?.dataSource === 1 ? "住院" : row.roomType === "急診" ? "急診" : "門診";
                    if (!bySource.has(source)) bySource.set(source, []);
                    bySource.get(source).push(row);
                }
                const pages = [];
                const sourceOrder = this.preview?.dataSource === 1 ? ["住院"] : ["急診", "門診"];
                for (const source of [...sourceOrder, ...bySource.keys()].filter((name, index, names) =>
                    bySource.has(name) && names.indexOf(name) === index)) {
                    const rows = bySource.get(source).sort((a, b) =>
                        ["sectionCode", "chargeCode", "serviceDate", "medicalRecordNo"]
                            .map(key => String(a[key] ?? "").localeCompare(String(b[key] ?? ""), "zh-Hant"))
                            .find(value => value !== 0) ?? 0);
                    const lines = [];
                    const totals = { quantity: 0, amount: 0 };
                    if (this.preview?.detailType === 2) {
                        let sectionKey = null;
                        let chargeKey = null;
                        for (const row of rows) {
                            const nextSection = `${row.sectionCode}|${row.sectionName ?? ""}`;
                            if (nextSection !== sectionKey) {
                                lines.push({ type: "section", row });
                                sectionKey = nextSection;
                                chargeKey = null;
                            }
                            const nextCharge = `${row.chargeCode}|${row.chargeName ?? ""}`;
                            if (nextCharge !== chargeKey) {
                                lines.push({ type: "charge", row });
                                chargeKey = nextCharge;
                            }
                            lines.push({ type: "row", row });
                        }
                        for (let index = 0; index < lines.length; index += 25) {
                            const pageLines = lines.slice(index, index + 25);
                            if (index > 0 && pageLines[0]?.type !== "section") {
                                const previous = lines.slice(0, index).reverse();
                                const section = previous.find(line => line.type === "section");
                                if (section) pageLines.unshift(section);
                                if (lines[index].type === "row") {
                                    const charge = previous.find(line => line.type === "charge");
                                    if (charge) pageLines.splice(section ? 1 : 0, 0, charge);
                                }
                            }
                            pages.push({ source, lines: pageLines, totals,
                                isLastForSource: index + 25 >= lines.length });
                        }
                        continue;
                    }
                    let sectionKey = null;
                    let sectionQuantity = 0;
                    let sectionAmount = 0;
                    for (const row of rows) {
                        const key = `${row.sectionCode}|${row.sectionName ?? ""}`;
                        if (sectionKey !== null && key !== sectionKey) {
                            lines.push({ type: "subtotal", quantity: sectionQuantity, amount: sectionAmount });
                            sectionQuantity = 0;
                            sectionAmount = 0;
                        }
                        sectionKey = key;
                        lines.push({ type: "row", row });
                        sectionQuantity += Number(row.quantity) || 0;
                        sectionAmount += Number(row.amount) || 0;
                        totals.quantity += Number(row.quantity) || 0;
                        totals.amount += Number(row.amount) || 0;
                    }
                    if (sectionKey !== null) lines.push({ type: "subtotal", quantity: sectionQuantity, amount: sectionAmount });
                    for (let index = 0; index < lines.length; index += 22) {
                        pages.push({ source, lines: lines.slice(index, index + 22),
                            totals, isLastForSource: index + 22 >= lines.length });
                    }
                }
                return pages;
            }
        },
        methods: {
            formatPreviewNumber(value, quantity = false) {
                return new Intl.NumberFormat("zh-TW", { minimumFractionDigits: quantity ? 1 : 0, maximumFractionDigits: 3 }).format(Number(value) || 0);
            },
            formatPreviewDate(value) {
                const digits = String(value ?? "").replace(/\D/g, "");
                return digits.length >= 7 ? `${digits.slice(0, 3)}/${digits.slice(3, 5)}/${digits.slice(5, 7)}` : String(value ?? "");
            },
            initialForm() {
                const isC6 = this.selectedReport?.code === "C6";
                return { startDate: this.defaultStartDate, endDate: this.defaultEndDate, dataSource: 0, detailType: 0, encounterType: isC6 ? 1 : 0, chargeKind: isC6 ? 1 : 0, organizationQuery: "", newOrganizationUnitCode: "", roomNo: "", chargeCode: "", insuranceIdentityCode: "" };
            },
            changeSource() { this.currentPage = 1; if (this.form.dataSource === 1) { this.form.encounterType = 0; this.form.roomNo = ""; } },
            openFilters() {
                window.clearTimeout(this.organizationSearchTimer);
                this.organizationSearchGeneration++;
                this.organizations = [];
                this.organizationMenuOpen = false;
                this.filterDraft = { encounterType: this.form.encounterType, organizationQuery: this.form.organizationQuery, newOrganizationUnitCode: this.form.newOrganizationUnitCode, roomNo: this.form.roomNo, chargeCode: this.form.chargeCode, insuranceIdentityCode: this.form.insuranceIdentityCode };
                this.filterOpen = true;
                this.$nextTick(() => this.$refs.filterTitle?.focus());
            },
            closeFilters() {
                this.filterOpen = false;
                this.filterDraft = null;
                this.dismissOrganizationMenu();
                this.$nextTick(() => this.$refs.filterButton?.focus());
            },
            dismissOrganizationMenu() {
                window.clearTimeout(this.organizationSearchTimer);
                this.organizationSearchGeneration++;
                this.organizations = [];
                this.organizationMenuOpen = false;
            },
            onFilterEscape() {
                if (this.organizationMenuOpen) this.dismissOrganizationMenu();
                else this.closeFilters();
            },
            onOrganizationFocusOut(event) {
                if (!event.currentTarget.contains(event.relatedTarget)) this.dismissOrganizationMenu();
            },
            async applyFilters() {
                if (!this.filterDraft) return;
                Object.assign(this.form, this.filterDraft);
                this.closeFilters();
                this.currentPage = 1;
                await this.load();
            },
            clearFilterText(field) {
                if (!this.filterDraft) return;
                this.filterDraft[field] = "";
                if (field === "organizationQuery") {
                    this.filterDraft.newOrganizationUnitCode = "";
                    this.dismissOrganizationMenu();
                }
                const inputRefs = { organizationQuery: "organizationFilterInput", roomNo: "roomFilterInput", chargeCode: "chargeFilterInput" };
                this.$nextTick(() => this.$refs[inputRefs[field]]?.focus());
            },
            searchOrganizations(showDefaults = false) {
                window.clearTimeout(this.organizationSearchTimer);
                const generation = ++this.organizationSearchGeneration;
                const query = showDefaults ? "" : (this.filterOpen ? this.filterDraft.organizationQuery : this.form.organizationQuery).trim();
                if (!query && !showDefaults) { this.organizations = []; this.organizationMenuOpen = false; return; }
                this.organizationSearchTimer = window.setTimeout(async () => {
                    try {
                        const response = await fetch(`/reports/c5/organization-units?query=${encodeURIComponent(query)}`);
                        const organizations = response.ok ? await response.json() : [];
                        if (generation === this.organizationSearchGeneration) this.organizations = organizations;
                    } catch { if (generation === this.organizationSearchGeneration) this.organizations = []; }
                }, showDefaults ? 0 : 250);
            },
            onOrganizationFocus() { this.organizationMenuOpen = true; this.searchOrganizations(true); },
            onOrganizationClick() { if (!this.organizationMenuOpen) this.onOrganizationFocus(); },
            onOrganizationInput() { (this.filterOpen ? this.filterDraft : this.form).newOrganizationUnitCode = ""; if (!this.filterOpen) this.currentPage = 1; this.organizationMenuOpen = true; this.searchOrganizations(); },
            selectOrganization(item) {
                const newCode = String(item?.newCode ?? "").trim().toUpperCase();
                if (!newCode) return;
                const displayName = String(item?.displayName ?? "").trim();
                const target = this.filterOpen ? this.filterDraft : this.form;
                target.newOrganizationUnitCode = newCode;
                target.organizationQuery = displayName ? `${newCode}|${displayName}` : newCode;
                this.organizations = [];
                this.organizationSearchGeneration++;
                this.organizationMenuOpen = false;
                if (!this.filterOpen) this.currentPage = 1;
            },
            payload() {
                const selectedCode = String(this.form.newOrganizationUnitCode ?? "").trim().toUpperCase();
                const directCode = String(this.form.organizationQuery ?? "").trim().toUpperCase();
                const { organizationQuery, ...form } = this.form;
                return { ...form, roomNo: this.selectedReport?.code === "C6" ? "" : form.roomNo, newOrganizationUnitCode: selectedCode || directCode, pageNumber: this.currentPage, pageSize: this.pageSize };
            },
            async search() { this.currentPage = 1; await this.load(); },
            async load() { this.isLoading = true; this.validationMessage = ""; const requestedPayload = JSON.stringify(this.payload()); try { const r = await fetch("/reports/c5/query", { method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": token() }, body: requestedPayload }); if (!r.ok) throw new Error(await r.text()); const x = await r.json(); this.rows = x.data; this.columns = x.columns; this.totalCount = x.totalCount; this.totalPages = x.totalPages; this.currentPage = x.pageNumber; this.lastLoadedPayload = requestedPayload; this.hasSearched = true; } catch (e) { this.rows = []; this.totalCount = 0; this.totalPages = 0; this.lastLoadedPayload = null; this.validationMessage = e.message; } finally { this.isLoading = false; } },
            async goToPage(p) { if (p < 1 || p > this.totalPages || p === this.currentPage) return; this.currentPage = p; await this.load(); },
            async changePageSize() { this.currentPage = 1; await this.load(); },
            resetForm() { this.form = this.initialForm(); this.filterOpen = false; this.filterDraft = null; window.clearTimeout(this.organizationSearchTimer); this.organizationSearchGeneration++; this.organizations = []; this.organizationMenuOpen = false; this.rows = []; this.columns = []; this.totalCount = 0; this.totalPages = 0; this.currentPage = 1; this.pageSize = 10; this.lastLoadedPayload = null; this.hasSearched = false; this.validationMessage = ""; this.closePreview(); },
            async openPreview() { if (!this.previewAvailable || this.previewLoading) return; this.previewLoading = true; this.previewOpen = false; this.preview = null; this.validationMessage = ""; try { const r = await fetch("/reports/c5/preview", { method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": token() }, body: JSON.stringify(this.payload()) }); if (!r.ok) { const message = await r.text(); throw new Error(r.status === 403 ? (message || "沒有 C5 預覽權限，請聯絡系統管理員。") : r.status === 401 ? "登入已逾時，請重新登入。" : message || `無法建立 C5 預覽（HTTP ${r.status}）。`); } const x = await r.json(); if (!Array.isArray(x?.rows) || x.rows.length === 0) throw new Error("查無此筆資料"); this.preview = x; this.previewOpen = true; } catch (e) { this.preview = null; this.previewOpen = false; this.validationMessage = e instanceof Error ? e.message : "無法建立 C5 預覽。"; } finally { this.previewLoading = false; } },
            closePreview() { this.previewOpen = false; this.preview = null; },
            printPreview() { if (this.previewOpen) window.print(); }
        }
    };
})();
