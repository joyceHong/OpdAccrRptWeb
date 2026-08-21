(() => {
    const createInitialForm = (startDate, endDate) => ({
        startDate,
        endDate,
        department: "",
        clinic: "",
        hospitalCode: "",
        billingCode: ""
    });

    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.ReportTemplate = {
        template: "#report-template",
        props: {
            selectedReport: { type: Object, required: true },
            defaultStartDate: { type: String, required: true },
            defaultEndDate: { type: String, required: true }
        },
        emits: ["show-toast"],
        data() {
            return {
                advancedOpen: false,
                validationMessage: "",
                isLoading: false,
                hasSearched: false,
                rows: [],
                currentPage: 1,
                pageSize: 10,
                serverTotalCount: 0,
                serverTotalPages: 0,
                form: createInitialForm(this.defaultStartDate, this.defaultEndDate),
                columns: []
            };
        },
        computed: {
            isServerPaged() { return ["C171", "C174"].includes(this.selectedReport.code); },
            filteredRows() { return this.rows; },
            hasResults() { return this.rows.length > 0; },
            totalCount() { return this.isServerPaged ? this.serverTotalCount : this.filteredRows.length; },
            totalPages() {
                return this.isServerPaged
                    ? this.serverTotalPages
                    : Math.max(1, Math.ceil(this.filteredRows.length / this.pageSize));
            },
            pagedRows() {
                if (this.isServerPaged) {
                    return this.rows;
                }
                const start = (this.currentPage - 1) * this.pageSize;
                return this.filteredRows.slice(start, start + this.pageSize);
            }
        },
        methods: {
            resetForm() {
                this.form = createInitialForm(this.defaultStartDate, this.defaultEndDate);
                this.advancedOpen = false;
                this.validationMessage = "";
                this.hasSearched = false;
                this.rows = [];
                this.columns = [];
                this.currentPage = 1;
                this.serverTotalCount = 0;
                this.serverTotalPages = 0;
            },
            async search() {
                if (!this.form.startDate || !this.form.endDate) {
                    this.validationMessage = "請輸入起始日期與截止日期。";
                    return;
                }
                if (this.form.startDate > this.form.endDate) {
                    this.validationMessage = "起始日期不可晚於截止日期。";
                    return;
                }

                this.validationMessage = "";
                this.currentPage = 1;
                await this.fetchResults();
            },
            async fetchResults() {
                this.isLoading = true;

                try {
                    const response = await fetch("/Report/GetReportData", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            reportCode: this.selectedReport.code,
                            startDate: this.form.startDate,
                            endDate: this.form.endDate,
                            chop1sec: this.form.department,
                            pageNumber: this.isServerPaged ? this.currentPage : null,
                            pageSize: this.isServerPaged ? this.pageSize : null
                        })
                    });

                    if (!response.ok) {
                        const problem = await response.json().catch(() => null);
                        const traceId = problem && typeof problem.traceId === "string"
                            ? `（追蹤碼：${problem.traceId}）`
                            : "";
                        const title = problem && typeof problem.title === "string"
                            ? problem.title
                            : `查詢失敗（HTTP ${response.status}）`;
                        throw new Error(`${title}${traceId}`);
                    }

                    const result = await response.json();
                    this.columns = Array.isArray(result.columns) ? result.columns : [];
                    this.rows = Array.isArray(result.data) ? result.data : [];
                    if (this.isServerPaged) {
                        this.serverTotalCount = Number.isInteger(result.totalCount) ? result.totalCount : 0;
                        this.serverTotalPages = Number.isInteger(result.totalPages) ? result.totalPages : 0;
                        this.currentPage = Number.isInteger(result.pageNumber) ? result.pageNumber : this.currentPage;
                    }
                    this.hasSearched = true;
                    //this.$emit("show-toast", this.totalCount > 0
                    //    ? `查詢完成，共 ${this.totalCount} 筆資料。`
                    //    : "查無符合條件的資料。");
                } catch (error) {
                    this.columns = [];
                    this.rows = [];
                    this.serverTotalCount = 0;
                    this.serverTotalPages = 0;
                    this.hasSearched = true;
                    this.validationMessage = error instanceof Error
                        ? error.message
                        : "查詢時發生未預期的錯誤。";
                } finally {
                    this.isLoading = false;
                }
            },
            async goToPage(pageNumber) {
                if (this.isLoading || pageNumber < 1 || pageNumber > this.totalPages) {
                    return;
                }

                this.currentPage = pageNumber;
                if (this.isServerPaged) {
                    await this.fetchResults();
                }
            },
            async changePageSize() {
                this.currentPage = 1;
                if (this.isServerPaged && this.hasSearched) {
                    await this.fetchResults();
                }
            },
            exportResults() {
                this.$emit("show-toast", "Excel 匯出將於後端 API 階段實作。");
            }
        }
    };
})();
