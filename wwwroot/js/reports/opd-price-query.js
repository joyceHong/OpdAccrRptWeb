(() => {
    const csrf = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.OpdPriceQuery = {
        template: "#opd-price-query-template",
        components: { ReportAutocomplete: window.ReportComponents.ReportAutocomplete },
        data() {
            return {
                form: { medicalRecordNo: "", visitDate: "", sectionCode: "", sectionQuery: "", showDc: false, showExtendedCode: false },
                advancedOpen: false,
                visits: [], detail: null, loading: false, searched: false, error: "",
                pageNumber: 1, pageSize: 10, totalCount: 0, totalPages: 0, selectedToken: "",
                sectionOptions: [], sectionOpen: false
            };
        },
        computed: {
            normalizedSectionOptions() {
                return this.sectionOptions.map(item => ({ value: item.code, code: item.code, label: item.name, suffix: "", raw: item }));
            }
        },
        methods: {
            invalidate() { this.visits = []; this.detail = null; this.searched = false; this.selectedToken = ""; },
            reset() {
                this.form = { medicalRecordNo: "", visitDate: "", sectionCode: "", sectionQuery: "", showDc: false, showExtendedCode: false };
                this.advancedOpen = false; this.sectionOptions = []; this.sectionOpen = false;
                this.invalidate(); this.error = "";
            },
            async search() { this.pageNumber = 1; await this.fetchVisits(); },
            async fetchVisits() {
                this.loading = true; this.error = "";
                try {
                    const { sectionQuery, ...query } = this.form;
                    const response = await fetch("/data-query/opd-price/visits", {
                        method: "POST",
                        headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                        body: JSON.stringify({
                            ...query,
                            visitDate: this.form.visitDate || null,
                            sectionCode: this.form.sectionCode || sectionQuery.trim(),
                            pageNumber: this.pageNumber,
                            pageSize: this.pageSize
                        })
                    });
                    if (!response.ok) throw new Error(await this.message(response));
                    const result = await response.json();
                    this.visits = result.rows; this.totalCount = result.totalCount; this.totalPages = result.totalPages;
                    this.pageNumber = result.pageNumber; this.searched = true; this.detail = null; this.selectedToken = "";
                    if (this.visits.length) await this.selectVisit(this.visits[0]);
                } catch (error) {
                    this.error = error.message; this.visits = []; this.detail = null; this.searched = true;
                } finally { this.loading = false; }
            },
            async selectVisit(row) {
                this.selectedToken = row.visitToken; this.loading = true;
                try {
                    const response = await fetch("/data-query/opd-price/detail", {
                        method: "POST",
                        headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                        body: JSON.stringify({ visitToken: row.visitToken, showDc: this.form.showDc, showExtendedCode: this.form.showExtendedCode })
                    });
                    if (!response.ok) throw new Error(await this.message(response));
                    this.detail = await response.json();
                } catch (error) { this.error = error.message; this.detail = null; }
                finally { this.loading = false; }
            },
            async goToPage(page) { if (page < 1 || page > this.totalPages) return; this.pageNumber = page; await this.fetchVisits(); },
            async loadSections(value) {
                this.form.sectionCode = "";
                const query = String(value ?? this.form.sectionQuery).trim();
                if (!query) { this.sectionOptions = []; this.sectionOpen = false; return; }
                try {
                    const response = await fetch(`/data-query/opd-price/sections?q=${encodeURIComponent(query)}`);
                    if (!response.ok) throw new Error();
                    this.sectionOptions = await response.json(); this.sectionOpen = this.sectionOptions.length > 0;
                } catch {
                    this.error = "無法載入科別清單，仍可直接輸入科別代碼。"; this.sectionOpen = false;
                }
            },
            selectSectionOption(option) {
                this.form.sectionCode = option.code;
                this.form.sectionQuery = option.label ? `${option.code}｜${option.label}` : option.code;
                this.sectionOpen = false;
            },
            previewReceipt(token) { window.open(`/data-query/opd-price/receipt?token=${encodeURIComponent(token)}`, "_blank", "noopener"); },
            async message(response) {
                try { const value = await response.json(); return value.title || value.message || "查詢失敗"; }
                catch { return "查詢失敗"; }
            }
        }
    };
})();
