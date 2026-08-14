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
                form: createInitialForm(this.defaultStartDate, this.defaultEndDate),
                columns: []
            };
        },
        computed: {
            filteredRows() { return this.rows; },
            hasResults() { return this.rows.length > 0; },
            totalPages() { return Math.max(1, Math.ceil(this.filteredRows.length / this.pageSize)); },
            pagedRows() {
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
                this.isLoading = true;

                try {
                    const response = await fetch("/Report/GetReportData", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            reportCode: this.selectedReport.code,
                            startDate: this.form.startDate,
                            endDate: this.form.endDate,
                            chop1sec: this.form.department
                        })
                    });

                    if (!response.ok) {
                        throw new Error(`查詢失敗（HTTP ${response.status}）`);
                    }

                    const result = await response.json();
                    this.columns = Array.isArray(result.columns) ? result.columns : [];
                    this.rows = Array.isArray(result.data) ? result.data : [];
                    this.hasSearched = true;
                    this.$emit("show-toast", this.rows.length > 0
                        ? `查詢完成，共 ${this.rows.length} 筆資料。`
                        : "查無符合條件的資料。");
                } catch (error) {
                    this.columns = [];
                    this.rows = [];
                    this.hasSearched = true;
                    this.validationMessage = error instanceof Error
                        ? error.message
                        : "查詢時發生未預期的錯誤。";
                } finally {
                    this.isLoading = false;
                }
            },
            exportResults() {
                this.$emit("show-toast", "Excel 匯出將於後端 API 階段實作。");
            }
        }
    };
})();
