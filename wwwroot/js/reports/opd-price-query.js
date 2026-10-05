(() => {
    const csrf = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.OpdPriceQuery = {
        template: "#opd-price-query-template",
        components: { ReportAutocomplete: window.ReportComponents.ReportAutocomplete },
        data() {
            return {
                form: { medicalRecordNo: "", visitDate: "", sectionCode: "", sectionQuery: "", showDc: false, showExtendedCode: false },
                advancedOpen: false, visits: [], selectedDetail: null, patientLoading: false, patientError: "",
                modalKind: "", modalVisit: null, modalDetail: null, modalLoading: false, modalError: "",
                selectedReceipts: [], printPending: false, loading: false, searched: false, error: "",
                pageNumber: 1, pageSize: 10, totalCount: 0, totalPages: 0, selectedToken: "",
                sectionOptions: [], sectionOpen: false, snapshot: null, generation: 0, modalGeneration: 0,
                lastTrigger: null
            };
        },
        computed: {
            activeFilterCount() {
                return [this.form.visitDate, this.form.sectionCode || this.form.sectionQuery, this.form.showDc, this.form.showExtendedCode]
                    .filter(value => Boolean(typeof value === "string" ? value.trim() : value)).length;
            },
            normalizedSectionOptions() {
                return this.sectionOptions.map(item => ({ value: item.code, code: item.code, label: item.name, suffix: "", raw: item }));
            },
            chargeGroups() {
                const rows = this.modalDetail?.charges || [];
                return [
                    { name: "醫令明細", rows: rows.filter(row => !row.category?.startsWith("1")) },
                    { name: "藥令明細", rows: rows.filter(row => row.category?.startsWith("1")) }
                ];
            },
            showFemhColumns() { return this.modalDetail?.facilityCode === "FEMH"; },
            showReceiptSequence() { return this.modalDetail?.facilityCode === "亞東紀念醫院"; }
        },
        methods: {
            invalidate() {
                this.generation++; this.closeModal(false);
                this.visits = []; this.selectedDetail = null; this.patientError = "";
                this.selectedToken = ""; this.searched = false; this.totalCount = 0; this.totalPages = 0;
                this.snapshot = null; this.loading = false; this.patientLoading = false;
            },
            reset() {
                this.form = { medicalRecordNo: "", visitDate: "", sectionCode: "", sectionQuery: "", showDc: false, showExtendedCode: false };
                this.advancedOpen = false; this.sectionOptions = []; this.sectionOpen = false;
                this.invalidate(); this.pageNumber = 1; this.error = "";
            },
            conditions() {
                return {
                    medicalRecordNo: this.form.medicalRecordNo.trim(), visitDate: this.form.visitDate || null,
                    sectionCode: this.form.sectionCode || this.form.sectionQuery.trim(),
                    showDc: this.form.showDc, showExtendedCode: this.form.showExtendedCode
                };
            },
            async search() { this.pageNumber = 1; await this.fetchVisits(true); },
            async fetchVisits(newQuery = false) {
                if (newQuery || !this.snapshot) {
                    this.snapshot = this.conditions(); this.closeModal(false); this.selectedDetail = null;
                    this.selectedToken = ""; this.visits = []; this.patientError = "";
                    this.patientLoading = false;
                }
                const current = ++this.generation;
                this.loading = true; this.error = "";
                try {
                    const response = await fetch("/data-query/opd-price/visits", {
                        method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                        body: JSON.stringify({ ...this.snapshot, pageNumber: this.pageNumber, pageSize: this.pageSize })
                    });
                    if (!response.ok) throw new Error(await this.message(response));
                    const result = await response.json();
                    if (current !== this.generation) return;
                    this.visits = result.rows || []; this.totalCount = result.totalCount || 0;
                    this.totalPages = result.totalPages || 0; this.pageNumber = result.pageNumber || 1;
                    this.searched = true; this.loading = false;
                    if (this.visits.length) await this.selectVisit(this.visits[0]);
                    else { this.selectedDetail = null; this.selectedToken = ""; this.patientLoading = false; }
                } catch (error) {
                    if (current !== this.generation) return;
                    this.error = error.message; this.visits = []; this.selectedDetail = null;
                    this.selectedToken = ""; this.searched = true; this.totalCount = 0; this.totalPages = 0;
                    this.patientLoading = false;
                } finally { if (current === this.generation) this.loading = false; }
            },
            async requestDetail(row) {
                const response = await fetch("/data-query/opd-price/detail", {
                    method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                    body: JSON.stringify({ visitToken: row.visitToken, showDc: this.snapshot.showDc,
                        showExtendedCode: this.snapshot.showExtendedCode })
                });
                if (!response.ok) throw new Error(await this.message(response));
                return await response.json();
            },
            async requestBasic(row) {
                const response = await fetch("/data-query/opd-price/basic", {
                    method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                    body: JSON.stringify({ visitToken: row.visitToken })
                });
                if (!response.ok) throw new Error(await this.message(response));
                return await response.json();
            },
            async selectVisit(row) {
                const current = this.generation;
                this.selectedToken = row.visitToken; this.selectedDetail = null; this.patientError = "";
                this.patientLoading = true;
                try {
                    const detail = await this.requestBasic(row);
                    if (current === this.generation && this.selectedToken === row.visitToken) this.selectedDetail = detail;
                } catch (error) {
                    if (current === this.generation && this.selectedToken === row.visitToken) this.patientError = error.message;
                } finally { if (current === this.generation && this.selectedToken === row.visitToken) this.patientLoading = false; }
            },
            async openDetail(row, event) { await this.openModal("detail", row, event); },
            async openReceipts(row, event) { await this.openModal("receipts", row, event); },
            async openModal(kind, row, event) {
                const current = ++this.modalGeneration;
                const query = this.generation;
                this.lastTrigger = event?.currentTarget || null;
                this.modalKind = kind; this.modalVisit = row; this.modalDetail = null;
                this.modalError = ""; this.modalLoading = true; this.selectedReceipts = [];
                if (typeof this.$nextTick === "function") this.$nextTick(() => this.$refs.modalClose?.focus());
                try {
                    const detail = await this.requestDetail(row);
                    if (current === this.modalGeneration && query === this.generation) this.modalDetail = detail;
                } catch (error) {
                    if (current === this.modalGeneration && query === this.generation) this.modalError = error.message;
                } finally { if (current === this.modalGeneration && query === this.generation) this.modalLoading = false; }
            },
            closeModal(restoreFocus = true) {
                this.modalGeneration++; this.modalKind = ""; this.modalVisit = null;
                this.modalDetail = null; this.modalLoading = false; this.modalError = "";
                this.selectedReceipts = [];
                if (restoreFocus) this.lastTrigger?.focus();
                this.lastTrigger = null;
            },
            toggleReceipt(token, checked) {
                if (!token) return;
                if (checked && !this.selectedReceipts.includes(token)) this.selectedReceipts.push(token);
                if (!checked) this.selectedReceipts = this.selectedReceipts.filter(value => value !== token);
            },
            async printSelectedReceipts() {
                if (this.printPending || !this.selectedReceipts.length || !this.modalVisit) return;
                const preview = window.open("", "_blank");
                if (!preview) { this.modalError = "瀏覽器封鎖了預覽視窗，請允許開啟新視窗。"; return; }
                preview.document.write("<p>正在產生收據預覽…</p>");
                const query = this.generation, modal = this.modalGeneration;
                const body = new URLSearchParams();
                body.append("visitToken", this.modalVisit.visitToken);
                this.selectedReceipts.forEach(value => body.append("receiptTokens", value));
                body.append("__RequestVerificationToken", csrf());
                this.printPending = true;
                try {
                    const response = await fetch("/data-query/opd-price/receipts", { method: "POST", body });
                    if (!response.ok) throw new Error(await this.message(response));
                    const html = await response.text();
                    if (query !== this.generation || modal !== this.modalGeneration) { preview.close(); return; }
                    preview.document.open(); preview.document.write(html); preview.document.close();
                } catch (error) {
                    preview.close();
                    if (query === this.generation && modal === this.modalGeneration) this.modalError = error.message;
                } finally { this.printPending = false; }
            },
            async goToPage(page) {
                if (page < 1 || page > this.totalPages) return;
                this.pageNumber = page; await this.fetchVisits(false);
            },
            async loadSections(value) {
                this.form.sectionCode = "";
                const query = String(value ?? this.form.sectionQuery).trim();
                if (!query) { this.sectionOptions = []; this.sectionOpen = false; return; }
                try {
                    const response = await fetch(`/data-query/opd-price/sections?q=${encodeURIComponent(query)}`);
                    if (!response.ok) throw new Error();
                    this.sectionOptions = await response.json(); this.sectionOpen = this.sectionOptions.length > 0;
                } catch { this.error = "無法載入科別清單，仍可直接輸入科別代碼。"; this.sectionOpen = false; }
            },
            selectSectionOption(option) {
                this.form.sectionCode = option.code;
                this.form.sectionQuery = option.label ? `${option.code}｜${option.label}` : option.code;
                this.sectionOpen = false;
            },
            async message(response) {
                try { const value = await response.json(); return value.title || value.message || "查詢失敗"; }
                catch { try { return await response.text() || "查詢失敗"; } catch { return "查詢失敗"; } }
            }
        }
    };
})();
