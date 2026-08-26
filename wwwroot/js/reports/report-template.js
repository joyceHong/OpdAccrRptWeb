(() => {
    const reportConfigurations = Object.freeze({
        C1: Object.freeze({ serverPaged: true }),
        C171: Object.freeze({ serverPaged: true }),
        C172: Object.freeze({ serverPaged: false }),
        C173: Object.freeze({ serverPaged: false }),
        C174: Object.freeze({ serverPaged: true }),
        C18: Object.freeze({
            serverPaged: true,
            requireSameYear: true,
            encounterSource: Object.freeze({
                defaultValue: "Emergency",
                options: Object.freeze([
                    Object.freeze({ value: "Emergency", label: "急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        }),
        C19: Object.freeze({
            serverPaged: true,
            singleDay: true,
            stationOrBedPrefix: true,
            encounterSource: Object.freeze({
                defaultValue: "Emergency",
                options: Object.freeze([
                    Object.freeze({ value: "Emergency", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        })
    });
    const getReportConfiguration = reportCode => reportConfigurations[reportCode]
        ?? Object.freeze({ serverPaged: false });
    const createInitialForm = (startDate, endDate, reportConfiguration) => ({
        startDate,
        endDate,
        encounterSource: reportConfiguration.encounterSource?.defaultValue ?? "",
        stationOrBedPrefix: "",
        department: "",
        clinic: "",
        hospitalCode: "",
        billingCode: ""
    });

    window.ReportComponents = window.ReportComponents || {};
    window.ReportConfigurations = reportConfigurations;
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
                isExporting: false,
                exportJob: null,
                exportPollTimer: null,
                form: createInitialForm(
                    this.defaultStartDate,
                    this.defaultEndDate,
                    getReportConfiguration(this.selectedReport.code)),
                columns: []
            };
        },
        computed: {
            reportConfiguration() { return getReportConfiguration(this.selectedReport.code); },
            encounterSourceConfiguration() { return this.reportConfiguration.encounterSource ?? null; },
            hasEncounterSource() { return this.encounterSourceConfiguration !== null; },
            hasStationOrBedPrefix() { return this.reportConfiguration.stationOrBedPrefix === true; },
            isServerPaged() { return getReportConfiguration(this.selectedReport.code).serverPaged === true; },
            filteredRows() { return this.rows; },
            hasResults() { return this.rows.length > 0; },
            canExport() { return this.selectedReport.code === "C174" && this.hasResults && !this.isExporting; },
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
        watch: {
            "selectedReport.code"() {
                this.resetForm();
            }
        },
        beforeUnmount() {
            this.stopExportPolling();
        },
        methods: {
            resetForm() {
                this.stopExportPolling?.();
                this.form = createInitialForm(
                    this.defaultStartDate,
                    this.defaultEndDate,
                    getReportConfiguration(this.selectedReport.code));
                this.advancedOpen = false;
                this.validationMessage = "";
                this.hasSearched = false;
                this.rows = [];
                this.columns = [];
                this.currentPage = 1;
                this.pageSize = 10;
                this.serverTotalCount = 0;
                this.serverTotalPages = 0;
                this.isExporting = false;
                this.exportJob = null;
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
                if (this.hasEncounterSource && !this.form.encounterSource) {
                    this.validationMessage = this.selectedReport.code === "C19"
                        ? "請選擇門急診或住院來源。"
                        : "請選擇急診或住院來源。";
                    return;
                }
                if (this.reportConfiguration.singleDay === true
                    && this.form.startDate !== this.form.endDate) {
                    this.validationMessage = "C19 僅限查詢單日資料，起始日期與截止日期必須相同。";
                    return;
                }
                if (this.reportConfiguration.requireSameYear === true
                    && this.form.startDate.substring(0, 4) !== this.form.endDate.substring(0, 4)) {
                    this.validationMessage = "C18 起訖日期必須屬於同一民國年度。";
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
                            encounterSource: this.hasEncounterSource
                                ? this.form.encounterSource
                                : undefined,
                            stationOrBedPrefix: this.hasStationOrBedPrefix
                                ? this.form.stationOrBedPrefix
                                : undefined,
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
            changeEncounterSource() {
                this.currentPage = 1;
            },
            async exportResults() {
                if (!this.canExport) return;
                this.isExporting = true;
                this.validationMessage = "";
                this.exportJob = null;
                try {
                    const response = await fetch("/Report/Export", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            reportCode: this.selectedReport.code,
                            startDate: this.form.startDate,
                            endDate: this.form.endDate
                        })
                    });
                    if (response.status === 200) {
                        const blob = await response.blob();
                        const disposition = response.headers?.get("Content-Disposition") ?? "";
                        const match = /filename\*?=(?:UTF-8''|\")?([^\";]+)/i.exec(disposition);
                        this.downloadBlob(blob, match ? decodeURIComponent(match[1]) : "C174.xlsx");
                        this.isExporting = false;
                        this.$emit("show-toast", "Excel 匯出完成。");
                        return;
                    }
                    if (response.status === 202) {
                        this.exportJob = await response.json();
                        this.$emit("show-toast", "資料量較大，已建立背景匯出工作。");
                        this.scheduleExportPoll();
                        return;
                    }
                    throw new Error(await this.readExportError(response));
                } catch (error) {
                    this.isExporting = false;
                    this.validationMessage = error instanceof Error ? error.message : "建立 Excel 匯出時發生錯誤。";
                }
            },
            async pollExportJob() {
                if (!this.exportJob?.statusUrl) return;
                try {
                    const response = await fetch(this.exportJob.statusUrl);
                    if (!response.ok) throw new Error(await this.readExportError(response));
                    this.exportJob = await response.json();
                    if (["Ready", "Failed", "Expired"].includes(this.exportJob.status)) {
                        this.isExporting = false;
                        this.stopExportPolling();
                        if (this.exportJob.status !== "Ready") {
                            this.validationMessage = this.exportJob.message
                                ?? (this.exportJob.status === "Expired" ? "匯出檔案已過期，請重新申請。" : "報表匯出失敗，請重新申請。");
                        }
                        return;
                    }
                    this.scheduleExportPoll();
                } catch (error) {
                    this.isExporting = false;
                    this.stopExportPolling();
                    this.validationMessage = error instanceof Error ? error.message : "查詢匯出狀態時發生錯誤。";
                }
            },
            scheduleExportPoll() {
                this.stopExportPolling();
                this.exportPollTimer = window.setTimeout(() => this.pollExportJob(), 2000);
            },
            stopExportPolling() {
                if (this.exportPollTimer !== null) {
                    window.clearTimeout(this.exportPollTimer);
                    this.exportPollTimer = null;
                }
            },
            downloadBlob(blob, fileName) {
                const url = window.URL.createObjectURL(blob);
                const anchor = document.createElement("a");
                anchor.href = url;
                anchor.download = fileName;
                anchor.click();
                window.URL.revokeObjectURL(url);
            },
            async readExportError(response) {
                const problem = await response.json().catch(() => null);
                return problem?.title ?? problem?.message ?? `匯出失敗（HTTP ${response.status}）`;
            }
        }
    };
})();
