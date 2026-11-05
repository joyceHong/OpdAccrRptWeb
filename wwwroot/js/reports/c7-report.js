(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    const toLegacyTime = value => value.replace(":", "");

    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.C7Report = {
        template: "#c7-report-template",
        mixins: [window.ReportDateRangeMixin],
        props: ["selectedReport", "defaultStartDate", "defaultEndDate"],
        data() {
            return {
                isLoading: false, hasSearched: false, validationMessage: "", queryVersion: 0,
                rows: [], columns: [], totalCount: 0, totalPages: 0, currentPage: 1, pageSize: 10,
                users: [], previewOpen: false, previewLoading: false, previewHtml: "",
                form: this.initialForm()
            };
        },
        computed: {
            hasResults() { return this.rows.length > 0; },
            dateRangeInvalid() {
                return Boolean(this.form.startDate && this.form.endDate && this.form.startDate > this.form.endDate);
            },
            timeRangeInvalid() {
                return Boolean(this.form.startTime && this.form.endTime && this.form.startTime > this.form.endTime);
            }
        },
        mounted() { this.loadUsers(); },
        methods: {
            initialForm() {
                return {
                    startDate: this.defaultStartDate, endDate: this.defaultEndDate,
                    startTime: "00:00", endTime: "23:59", inputUserId: "", chargeKind: 0
                };
            },
            conditionChanged() {
                this.queryVersion += 1;
                this.isLoading = false;
                this.currentPage = 1;
                this.rows = [];
                this.columns = [];
                this.totalCount = 0;
                this.totalPages = 0;
                this.hasSearched = false;
                this.validationMessage = "";
                this.closePreview();
            },
            startDateChanged() {
                if (this.syncDefaultEndDate()) this.endDateChanged();
                else this.conditionChanged();
            },
            async endDateChanged() {
                this.conditionChanged();
                this.form.inputUserId = "";
                await this.loadUsers();
            },
            async loadUsers() {
                if (!this.form.endDate) return;
                try {
                    const r = await fetch(`/reports/c7/input-users?endDate=${encodeURIComponent(this.form.endDate)}`);
                    this.users = r.ok ? await r.json() : [];
                } catch { this.users = []; }
            },
            payload() {
                return {
                    startDate: this.form.startDate, endDate: this.form.endDate,
                    startTime: toLegacyTime(this.form.startTime), endTime: toLegacyTime(this.form.endTime),
                    inputUserId: this.form.inputUserId, chargeKind: this.form.chargeKind,
                    pageNumber: this.currentPage, pageSize: this.pageSize
                };
            },
            async search() {
                this.currentPage = 1;
                if (this.dateRangeInvalid || this.timeRangeInvalid) {
                    this.conditionChanged();
                    return;
                }
                await this.load();
            },
            async load() {
                const queryVersion = ++this.queryVersion;
                this.isLoading = true;
                this.validationMessage = "";
                try {
                    const r = await fetch("/reports/c7/query", {
                        method: "POST",
                        headers: { "Content-Type": "application/json", "RequestVerificationToken": token() },
                        body: JSON.stringify(this.payload())
                    });
                    if (!r.ok) throw new Error(await r.text());
                    const x = await r.json();
                    if (queryVersion !== this.queryVersion) return;
                    this.rows = x.data;
                    this.columns = x.columns;
                    this.totalCount = x.totalCount;
                    this.totalPages = x.totalPages;
                    this.currentPage = x.pageNumber;
                    this.hasSearched = true;
                } catch (e) {
                    if (queryVersion !== this.queryVersion) return;
                    this.rows = [];
                    this.totalCount = 0;
                    this.totalPages = 0;
                    this.validationMessage = e.message;
                } finally {
                    if (queryVersion === this.queryVersion) this.isLoading = false;
                }
            },
            async goToPage(p) {
                if (p < 1 || p > this.totalPages || p === this.currentPage) return;
                this.currentPage = p;
                await this.load();
            },
            async changePageSize() { this.currentPage = 1; await this.load(); },
            resetForm() {
                this.form = this.initialForm();
                this.users = [];
                this.conditionChanged();
                this.pageSize = 10;
                this.loadUsers();
            },
            async openPreview() {
                if (!this.hasResults || this.previewLoading) return;
                this.previewLoading = true;
                this.previewOpen = false;
                this.previewHtml = "";
                this.validationMessage = "";
                try {
                    const body = new URLSearchParams({ ...this.payload(), __RequestVerificationToken: token() });
                    const r = await fetch("/reports/c7/preview", {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
                            "RequestVerificationToken": token()
                        },
                        body
                    });
                    if (!r.ok) throw new Error(await r.text() || "無法建立 C7 預覽。");
                    const html = await r.text();
                    if (!html.trim()) throw new Error("查無此筆資料");
                    this.previewHtml = html;
                    this.previewOpen = true;
                } catch (e) {
                    this.previewHtml = "";
                    this.previewOpen = false;
                    this.validationMessage = e instanceof Error ? e.message : "無法建立 C7 預覽。";
                } finally { this.previewLoading = false; }
            },
            closePreview() { this.previewOpen = false; this.previewHtml = ""; },
            printPreview() { if (this.previewOpen) window.print(); }
        }
    };
})();
