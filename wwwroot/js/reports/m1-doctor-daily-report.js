(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    const yesterday = value => {
        const date = new Date(`${value}T00:00:00`);
        date.setDate(date.getDate() - 1);
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
    };
    const message = async response => {
        const contentType = response.headers.get("content-type") || "";
        if (contentType.includes("application/problem+json") || contentType.includes("application/json")) {
            const body = await response.json();
            return { text: body.title || "M1 查詢失敗。", code: body.code || null };
        }
        return { text: (await response.text()) || "M1 查詢失敗。", code: null };
    };
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.M1DoctorDailyReport = {
        template: "#m1-doctor-daily-template",
        props: ["selectedReport", "defaultStartDate"],
        data() {
            return {
                form: { reportDate: yesterday(this.defaultStartDate) }, futureDateConfirmed: false,
                runId: null, rows: [], columns: [], totalCount: 0, totalPages: 0,
                currentPage: 1, pageSize: 10, isLoading: false, hasSearched: false,
                validationMessage: "", previewOpen: false, previewLoading: false, previewHtml: ""
            };
        },
        computed: { hasResults() { return this.rows.length > 0 && !!this.runId; } },
        methods: {
            conditionChanged() { this.clearResults(); },
            clearResults() { this.runId = null; this.rows = []; this.columns = []; this.totalCount = 0; this.totalPages = 0; this.currentPage = 1; this.hasSearched = false; this.futureDateConfirmed = false; this.closePreview(); },
            resetForm() { this.form.reportDate = yesterday(this.defaultStartDate); this.validationMessage = ""; this.pageSize = 10; this.clearResults(); },
            payload() { return { reportDate: this.runId ? null : this.form.reportDate, futureDateConfirmed: this.futureDateConfirmed, pageNumber: this.currentPage, pageSize: this.pageSize, runId: this.runId }; },
            async search() { this.currentPage = 1; this.runId = null; this.futureDateConfirmed = false; await this.load(); },
            async load() {
                this.isLoading = true; this.validationMessage = "";
                try {
                    const response = await fetch("/medical-statistics/doctor-daily/query", { method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": token() }, body: JSON.stringify(this.payload()) });
                    if (response.status === 409) {
                        const problem = await message(response);
                        if (problem.code === "M1_FUTURE_DATE_CONFIRMATION_REQUIRED" && window.confirm(`${problem.text}\n是否仍要繼續查詢？`)) { this.futureDateConfirmed = true; return await this.load(); }
                        throw new Error(problem.text);
                    }
                    if (!response.ok) { const problem = await message(response); throw new Error(problem.text); }
                    const result = await response.json();
                    this.runId = result.runId; this.rows = result.data; this.columns = result.columns;
                    this.totalCount = result.totalCount; this.totalPages = result.totalPages;
                    this.currentPage = result.pageNumber; this.hasSearched = true;
                } catch (error) {
                    this.rows = []; this.totalCount = 0; this.totalPages = 0; this.runId = null;
                    this.hasSearched = true; this.validationMessage = error instanceof Error ? error.message : "M1 查詢失敗。";
                } finally { this.isLoading = false; }
            },
            async goToPage(page) { if (page < 1 || page > this.totalPages || page === this.currentPage) return; this.currentPage = page; await this.load(); },
            async changePageSize() { this.currentPage = 1; await this.load(); },
            async openPreview() {
                if (!this.hasResults || this.previewLoading) return;
                this.previewLoading = true; this.validationMessage = "";
                try {
                    const body = new URLSearchParams({ runId: this.runId, pageNumber: "1", pageSize: String(this.pageSize), __RequestVerificationToken: token() });
                    const response = await fetch("/medical-statistics/doctor-daily/preview", { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8", "RequestVerificationToken": token() }, body });
                    if (!response.ok) { const problem = await message(response); throw new Error(problem.text); }
                    this.previewHtml = await response.text(); this.previewOpen = true;
                } catch (error) { this.closePreview(); this.validationMessage = error instanceof Error ? error.message : "M1 預覽失敗。"; }
                finally { this.previewLoading = false; }
            },
            exportReport(format) { if (this.hasResults) window.location.assign(`/medical-statistics/doctor-daily/export?runId=${encodeURIComponent(this.runId)}&format=${format}`); },
            closePreview() { this.previewOpen = false; this.previewHtml = ""; },
            printPreview() { if (this.previewOpen) window.print(); }
        }
    };
})();
