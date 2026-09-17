(() => {
    const reportConfigurations = Object.freeze({
        C1: Object.freeze({ serverPaged: true }),
        C3: Object.freeze({
            serverPaged: true,
            c3: true,
            encounterSource: Object.freeze({
                defaultValue: "O",
                options: Object.freeze([
                    Object.freeze({ value: "O", label: "門急診" }),
                    Object.freeze({ value: "I", label: "住院" })
                ])
            })
        }),
        C10: Object.freeze({
            serverPaged: true,
            advancedConditions: false,
            c10: true,
            encounterSource: Object.freeze({
                defaultValue: "OpdEr",
                options: Object.freeze([
                    Object.freeze({ value: "OpdEr", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        }),
        C13: Object.freeze({ serverPaged: true, c13: true }),
        C15: Object.freeze({ serverPaged: true, c15: true }),
        C16: Object.freeze({
            serverPaged: true,
            c16: true,
            encounterSource: Object.freeze({
                defaultValue: "OutpatientEmergency",
                options: Object.freeze([
                    Object.freeze({ value: "OutpatientEmergency", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            }),
            reportType: Object.freeze({
                defaultValue: "All",
                options: Object.freeze([
                    Object.freeze({ value: "All", label: "全部" }),
                    Object.freeze({ value: "Child", label: "兒童" }),
                    Object.freeze({ value: "NewHope", label: "新希望關懷" })
                ])
            })
        }),
        C143: Object.freeze({
            serverPaged: true,
            advancedConditions: false,
            c143: true,
            encounterSource: Object.freeze({
                defaultValue: "OpdEr",
                options: Object.freeze([
                    Object.freeze({ value: "OpdEr", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            }),
            reportType: Object.freeze({
                defaultValue: "Difference",
                options: Object.freeze([
                    Object.freeze({ value: "Difference", label: "差異帳表" }),
                    Object.freeze({ value: "All", label: "全部帳表" })
                ])
            })
        }),
        C144: Object.freeze({
            serverPaged: true,
            advancedConditions: false,
            c144: true,
            encounterSource: Object.freeze({
                defaultValue: "OpdEr",
                options: Object.freeze([
                    Object.freeze({ value: "OpdEr", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        }),
        C21: Object.freeze({
            serverPaged: true,
            advancedConditions: false,
            c21: true,
            encounterSource: Object.freeze({
                defaultValue: "Outpatient",
                options: Object.freeze([
                    Object.freeze({ value: "Outpatient", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        }),
        C23: Object.freeze({
            serverPaged: true,
            advancedConditions: false,
            c23: true,
            encounterSource: Object.freeze({
                defaultValue: "Outpatient",
                options: Object.freeze([
                    Object.freeze({ value: "Outpatient", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        }),
        C24: Object.freeze({
            serverPaged: true,
            advancedConditions: false,
            c24: true,
            encounterSource: Object.freeze({
                defaultValue: "OpdEr",
                options: Object.freeze([
                    Object.freeze({ value: "OpdEr", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        }),
        C22: Object.freeze({
            serverPaged: true,
            cashierUserId: true,
            cashierCashSort: Object.freeze({
                defaultValue: "Cashier",
                options: Object.freeze([
                    Object.freeze({ value: "Cashier", label: "依櫃員" }),
                    Object.freeze({ value: "Encounter", label: "依門急診" })
                ])
            })
        }),
        C213: Object.freeze({ serverPaged: true, advancedConditions: false }),
        C214: Object.freeze({
            serverPaged: true,
            endDateOnly: true,
            advancedConditions: false,
            receivableBalanceType: Object.freeze({
                defaultValue: "SelfPay",
                options: Object.freeze([
                    Object.freeze({ value: "SelfPay", label: "自費" }),
                    Object.freeze({ value: "Insurance", label: "健保" })
                ])
            })
        }),
        C25: Object.freeze({ serverPaged: true }),
        C27: Object.freeze({ serverPaged: true, endDateOnly: true }),
        C28: Object.freeze({ serverPaged: true, endDateOnly: true }),
        C211: Object.freeze({
            serverPaged: false,
            endDateOnly: true,
            advancedConditions: false,
            c211: true,
            encounterSource: Object.freeze({
                defaultValue: "O",
                options: Object.freeze([
                    Object.freeze({ value: "O", label: "門急" }),
                    Object.freeze({ value: "I", label: "住院" })
                ])
            })
        }),
        C212: Object.freeze({
            serverPaged: false,
            endDateOnly: true,
            advancedConditions: false,
            c212: true
        }),
        C29: Object.freeze({
            serverPaged: true,
            billingCode: true,
            encounterSource: Object.freeze({
                defaultValue: "Emergency",
                options: Object.freeze([
                    Object.freeze({ value: "Emergency", label: "門急診" }),
                    Object.freeze({ value: "Inpatient", label: "住院" })
                ])
            })
        }),
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
    const advancedConditionKeys = Object.freeze([
        "stationOrBedPrefix",
        "cashierUserId",
        "cashierCashSort",
        "billingCode"
    ]);
    const previousDate = value => {
        const date = new Date(`${value}T12:00:00`);
        date.setDate(date.getDate() - 1);
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
    };
    const createInitialForm = (startDate, endDate, reportConfiguration) => ({
        startDate: reportConfiguration.c143 === true ? previousDate(startDate) : startDate,
        endDate: reportConfiguration.c211 === true || reportConfiguration.c212 === true
            || reportConfiguration.c143 === true ? previousDate(endDate) : endDate,
        encounterSource: reportConfiguration.encounterSource?.defaultValue ?? "",
        stationOrBedPrefix: "",
        cashierUserId: "",
        cashierCashSortType: reportConfiguration.cashierCashSort?.defaultValue ?? "",
        department: "",
        clinic: "",
        hospitalCode: "",
        billingCode: "",
        accountingScope: reportConfiguration.c21 === true
            ? (reportConfiguration.encounterSource?.defaultValue === "Inpatient" ? 4 : 0)
            : null,
        forceRebuild: false,
        dateMode: reportConfiguration.c23 === true ? "General" : (reportConfiguration.c16 === true ? "AccountingDate" : ""),
        inpatientType: "",
        contractCode: "",
        receivableBalanceType: reportConfiguration.receivableBalanceType?.defaultValue ?? "",
        source: reportConfiguration.c24 === true || reportConfiguration.c10 === true
            ? reportConfiguration.encounterSource.defaultValue
            : "",
        mode: reportConfiguration.c24 === true ? "Accounting" : "",
        roomScope: reportConfiguration.c24 === true || reportConfiguration.c10 === true ? "All" : "",
        medicalRecordNo: "",
        reportType: reportConfiguration.reportType?.defaultValue ?? ""
        ,detailType: 0
        ,logisticsType: 0
        ,departmentCode: ""
        ,roomCodes: ""
        ,chargeCodes: ""
    });

    window.ReportComponents = window.ReportComponents || {};
    window.ReportConfigurations = reportConfigurations;
    window.ReportComponents.ReportTemplate = {
        template: "#report-template",
        props: {
            selectedReport: { type: Object, required: true },
            defaultStartDate: { type: String, required: true },
            defaultEndDate: { type: String, required: true },
            c21RebuildEnabled: { type: Boolean, default: false },
            c23RebuildEnabled: { type: Boolean, default: false }
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
                ,c21BillingItems: [],
                c23Contracts: [],
                c211Contracts: [],
                c211ContractOpen: false,
                c211ContractActiveIndex: -1,
                summaries: [],
                c211Summary: null,
                c212Summary: null,
                c15Summary: null,
                c15PreviewOpen: false,
                c15PreviewLoading: false,
                c15PreviewRows: [],
                c15PreviewSummary: null,
                c15PrintGeneratedAt: "",
                c3PreviewOpen: false,
                c3PreviewLoading: false,
                c3Preview: null
            };
        },
        computed: {
            reportConfiguration() { return getReportConfiguration(this.selectedReport.code); },
            encounterSourceConfiguration() { return this.reportConfiguration.encounterSource ?? null; },
            hasEncounterSource() { return this.encounterSourceConfiguration !== null; },
            hasStationOrBedPrefix() { return this.reportConfiguration.stationOrBedPrefix === true; },
            hasCashierUserId() { return this.reportConfiguration.cashierUserId === true; },
            hasCashierCashSort() { return this.reportConfiguration.cashierCashSort !== undefined; },
            hasBillingCode() { return this.reportConfiguration.billingCode === true; },
            isC21() { return this.reportConfiguration.c21 === true; },
            isC23() { return this.reportConfiguration.c23 === true; },
            isC24() { return this.reportConfiguration.c24 === true; },
            isC10() { return this.reportConfiguration.c10 === true; },
            isC211() { return this.reportConfiguration.c211 === true; },
            isC212() { return this.reportConfiguration.c212 === true; },
            isPrintableLegacyReport() { return this.isC211 || this.isC212; },
            c21ScopeOptions() {
                return this.form.encounterSource === "Inpatient"
                    ? [{ value: 4, label: "全部" }, { value: 5, label: "住院總帳" }, { value: 8, label: "出院總帳" }]
                    : [{ value: 0, label: "全部" }, { value: 1, label: "門急診" }, { value: 2, label: "門診" }, { value: 3, label: "急診" }];
            },
            canForceC21Rebuild() {
                return this.c21RebuildEnabled && this.isC21
                    && this.form.encounterSource === "Inpatient"
                    && this.form.startDate === this.form.endDate;
            },
            canForceC23Rebuild() {
                return this.c23RebuildEnabled && this.isC23
                    && this.form.dateMode === "General"
                    && this.form.startDate === this.form.endDate;
            },
            canForceC24Rebuild() {
                return this.isC24 && this.form.mode === "Accounting"
                    && this.form.startDate === this.form.endDate;
            },
            receivableBalanceTypeConfiguration() { return this.reportConfiguration.receivableBalanceType ?? null; },
            hasReceivableBalanceType() { return this.receivableBalanceTypeConfiguration !== null; },
            hasAdvancedConditions() {
                return advancedConditionKeys.some(key => this.reportConfiguration[key] !== undefined);
            },
            cashierCashSortConfiguration() { return this.reportConfiguration.cashierCashSort ?? null; },
            isEndDateOnly() { return this.reportConfiguration.endDateOnly === true; },
            isServerPaged() { return getReportConfiguration(this.selectedReport.code).serverPaged === true; },
            filteredRows() { return this.rows; },
            hasResults() { return this.rows.length > 0; },
            canExport() { return ["C10", "C144", "C174"].includes(this.selectedReport.code) && this.hasResults && !this.isExporting; },
            isC13() { return this.reportConfiguration.c13 === true; },
            isC15() { return this.reportConfiguration.c15 === true; },
            isC16() { return this.reportConfiguration.c16 === true; },
            isC3() { return this.reportConfiguration.c3 === true; },
            isC143() { return this.reportConfiguration.c143 === true; },
            isC144() { return this.reportConfiguration.c144 === true; },
            reportTypeConfiguration() { return this.reportConfiguration.reportType ?? null; },
            hasReportType() { return this.reportTypeConfiguration !== null; },
            totalCount() { return this.isServerPaged ? this.serverTotalCount : this.filteredRows.length; },
            totalPages() {
                return this.isServerPaged
                    ? this.serverTotalPages
                    : Math.max(1, Math.ceil(this.filteredRows.length / this.pageSize));
            },
            pagedRows() {
                if (this.isPrintableLegacyReport) return this.rows;
                if (this.isServerPaged) {
                    return this.rows;
                }
                const start = (this.currentPage - 1) * this.pageSize;
                return this.filteredRows.slice(start, start + this.pageSize);
            },
            c15Groups() {
                if (!this.isC15) return [];
                const summaries = Array.isArray(this.c15Summary?.groups) ? this.c15Summary.groups : [];
                return summaries
                    .map(summary => ({
                        ...summary,
                        rows: this.pagedRows.filter(row => row.type === summary.type)
                    }))
                    .filter(group => group.rows.length > 0);
            },
            c15PreviewPages() {
                if (!this.isC15) return [];
                const rowsPerPage = 18;
                const summaries = Array.isArray(this.c15PreviewSummary?.groups)
                    ? this.c15PreviewSummary.groups : [];
                const pages = [];
                summaries.forEach(summary => {
                    const groupRows = this.c15PreviewRows.filter(row => row.type === summary.type);
                    for (let start = 0; start < groupRows.length; start += rowsPerPage) {
                        pages.push({
                            ...summary,
                            rows: groupRows.slice(start, start + rowsPerPage),
                            showTotals: start + rowsPerPage >= groupRows.length
                        });
                    }
                });
                return pages.map((page, index) => ({
                    ...page,
                    pageNumber: index + 1,
                    totalPages: pages.length
                }));
            },
            c3PreviewPages() {
                if (!this.isC3 || !Array.isArray(this.c3Preview?.rows)) return [];
                const rowsPerPage = Number(this.c3Preview.detailType) === 1 ? 12 : 20;
                const pages = [];
                for (let start = 0; start < this.c3Preview.rows.length; start += rowsPerPage) {
                    pages.push({ rows: this.c3Preview.rows.slice(start, start + rowsPerPage) });
                }
                return pages.map((page, index) => ({
                    ...page,
                    pageNumber: index + 1,
                    totalPages: pages.length
                }));
            },
            c211Groups() {
                if (!this.isC211 || !this.c211Summary) return [];
                return this.c211Summary.groups.map(group => ({
                    ...group,
                    rows: this.rows.filter(row => row.contractCode === group.contractCode)
                }));
            },
            filteredC211Contracts() {
                const query = (this.form.contractCode ?? "").trim().toUpperCase();
                return this.c211Contracts.filter(item => !query
                    || item.code.toUpperCase().includes(query)
                    || item.name.toUpperCase().includes(query));
            }
        },
        watch: {
            "selectedReport.code"() {
                this.resetForm();
                this.loadC21BillingItems();
                this.loadC23Contracts();
                this.loadC211Contracts();
            }
        },
        mounted() {
            this.loadC21BillingItems();
            this.loadC23Contracts();
            this.loadC211Contracts();
            if (typeof document !== "undefined") document.addEventListener("pointerdown", this.closeC211ContractsFromOutside);
        },
        beforeUnmount() {
            this.stopExportPolling();
            if (typeof document !== "undefined") document.removeEventListener("pointerdown", this.closeC211ContractsFromOutside);
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
                this.summaries = [];
                this.c211Summary = null;
                this.c212Summary = null;
                this.c15Summary = null;
                this.c15PreviewOpen = false;
                this.c15PreviewLoading = false;
                this.c15PreviewRows = [];
                this.c15PreviewSummary = null;
                this.c15PrintGeneratedAt = "";
                this.c3PreviewOpen = false;
                this.c3PreviewLoading = false;
                this.c3Preview = null;
                this.c211ContractOpen = false;
                this.c211ContractActiveIndex = -1;
                this.currentPage = 1;
                this.pageSize = 10;
                this.serverTotalCount = 0;
                this.serverTotalPages = 0;
                this.isExporting = false;
                this.exportJob = null;
            },
            async loadC21BillingItems() {
                this.c21BillingItems = [];
                if (!this.isC21) return;
                try {
                    const response = await fetch("/Report/C21/BillingItems");
                    if (response.ok) this.c21BillingItems = await response.json();
                } catch {
                    this.c21BillingItems = [];
                }
            },
            async loadC23Contracts() {
                this.c23Contracts = [];
                if (!this.isC23) return;
                try {
                    const response = await fetch("/Report/C23/Contracts");
                    if (response.ok) this.c23Contracts = await response.json();
                } catch {
                    this.c23Contracts = [];
                }
            },
            async loadC211Contracts() {
                this.c211Contracts = [];
                if (!this.isC211) return;
                try {
                    const response = await fetch("/Report/C211/Contracts", { cache: "no-store" });
                    if (response.ok) this.c211Contracts = await response.json();
                } catch {
                    this.c211Contracts = [];
                }
            },
            openC211Contracts() {
                this.c211ContractOpen = true;
                this.c211ContractActiveIndex = -1;
            },
            selectC211Contract(item) {
                this.form.contractCode = item.code;
                this.c211ContractOpen = false;
                this.c211ContractActiveIndex = -1;
            },
            onC211ContractKeydown(event) {
                const count = this.filteredC211Contracts.length;
                if (event.key === "Escape") {
                    this.c211ContractOpen = false;
                    this.c211ContractActiveIndex = -1;
                    return;
                }
                if (!count) return;
                if (event.key === "ArrowDown") {
                    event.preventDefault();
                    this.c211ContractOpen = true;
                    this.c211ContractActiveIndex = (this.c211ContractActiveIndex + 1) % count;
                } else if (event.key === "ArrowUp") {
                    event.preventDefault();
                    this.c211ContractOpen = true;
                    this.c211ContractActiveIndex = (this.c211ContractActiveIndex - 1 + count) % count;
                } else if (event.key === "Enter" && this.c211ContractOpen && this.c211ContractActiveIndex >= 0) {
                    event.preventDefault();
                    this.selectC211Contract(this.filteredC211Contracts[this.c211ContractActiveIndex]);
                }
            },
            closeC211ContractsFromOutside(event) {
                if (!this.$refs.c211Autocomplete?.contains(event.target)) {
                    this.c211ContractOpen = false;
                    this.c211ContractActiveIndex = -1;
                }
            },
            async search() {
                if (!this.form.endDate || (!this.isEndDateOnly && !this.form.startDate)) {
                    this.validationMessage = this.isEndDateOnly
                        ? "請輸入截止日期。"
                        : "請輸入起始日期與截止日期。";
                    return;
                }
                if (!this.isEndDateOnly && this.form.startDate > this.form.endDate) {
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
                if (this.isC23 && this.form.dateMode === "EncounterDate"
                    && this.form.startDate.substring(0, 7) !== this.form.endDate.substring(0, 7)) {
                    this.validationMessage = "C23 就診日模式起訖日期必須在同一月份。";
                    return;
                }
                if (this.isC23 && this.form.dateMode === "General"
                    && this.form.encounterSource === "Inpatient" && !this.form.inpatientType) {
                    this.validationMessage = "請選擇住院或出院。";
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
                            startDate: this.isEndDateOnly ? undefined : this.form.startDate,
                            endDate: this.form.endDate,
                            encounterSource: this.hasEncounterSource && !this.isC24 && !this.isC10 && !this.isC143 && !this.isC144
                                ? this.form.encounterSource
                                : undefined,
                            stationOrBedPrefix: this.hasStationOrBedPrefix
                                ? this.form.stationOrBedPrefix
                                : undefined,
                            cashierUserId: this.hasCashierUserId ? this.form.cashierUserId : undefined,
                            cashierCashSortType: this.hasCashierCashSort ? this.form.cashierCashSortType : undefined,
                            billingCode: this.hasBillingCode
                                ? this.form.billingCode.trim()
                                : (this.isC21 && this.form.billingCode.trim() ? this.form.billingCode.trim() : undefined),
                            accountingScope: this.isC21 ? this.form.accountingScope : undefined,
                            dateMode: this.isC23 || this.isC16 ? this.form.dateMode : undefined,
                            inpatientType: this.isC23 && this.form.dateMode === "General" ? this.form.inpatientType || undefined : undefined,
                            contractCode: (this.isC23 || this.isC211) && this.form.contractCode.trim()
                                ? this.form.contractCode.trim() : undefined,
                            forceRebuild: (this.isC21 || this.isC23 || this.canForceC24Rebuild)
                                ? this.form.forceRebuild : undefined,
                            source: this.isC3 || this.isC24 || this.isC10 || this.isC143 || this.isC144 || this.isC16 ? this.form.encounterSource : undefined,
                            reportType: this.isC143 || this.isC16 ? this.form.reportType : undefined,
                            detailType: this.isC3 ? this.form.detailType : undefined,
                            logisticsType: this.isC3 ? this.form.logisticsType : undefined,
                            departmentCode: this.isC3 && this.form.departmentCode.trim() ? this.form.departmentCode.trim() : undefined,
                            roomCodes: this.isC3 && this.form.roomCodes.trim() ? this.form.roomCodes.trim() : undefined,
                            chargeCodes: this.isC3 && this.form.chargeCodes.trim() ? this.form.chargeCodes.trim() : undefined,
                            mode: this.isC24 ? this.form.mode : undefined,
                            roomScope: this.isC24 || this.isC10 ? this.form.roomScope : undefined,
                            medicalRecordNo: (this.isC24 || this.isC10) && this.form.medicalRecordNo.trim()
                                ? this.form.medicalRecordNo.trim() : undefined,
                            receivableBalanceType: this.hasReceivableBalanceType
                                ? this.form.receivableBalanceType
                                : undefined,
                            chop1sec: this.hasAdvancedConditions
                                ? this.form.department
                                : undefined,
                            pageNumber: this.isServerPaged ? this.currentPage : undefined,
                            pageSize: this.isServerPaged ? this.pageSize : undefined
                        })
                    });

                    if (!response.ok) {
                        const problem = await response.json().catch(() => null);
                        const traceId = problem && typeof problem.traceId === "string"
                            ? `（追蹤碼：${problem.traceId}）`
                            : "";
                        const title = typeof problem === "string"
                            ? problem
                            : problem && typeof problem.title === "string"
                            ? problem.title
                            : `查詢失敗（HTTP ${response.status}）`;
                        throw new Error(`${title}${traceId}`);
                    }

                    const result = await response.json();
                    this.columns = Array.isArray(result.columns) ? result.columns : [];
                    this.rows = Array.isArray(result.data) ? result.data : [];
                    this.summaries = Array.isArray(result.summary) ? result.summary : [];
                    this.c211Summary = this.isC211 && result.summary && !Array.isArray(result.summary)
                        ? result.summary : null;
                    this.c212Summary = this.isC212 && result.summary && !Array.isArray(result.summary)
                        ? result.summary : null;
                    this.c15Summary = this.isC15 && result.summary && !Array.isArray(result.summary)
                        ? result.summary : null;
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
                    this.summaries = [];
                    this.c211Summary = null;
                    this.c212Summary = null;
                    this.c15Summary = null;
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
            async previewC13() {
                if (!this.isC13 || !this.hasResults || this.isLoading) return;
                this.isLoading = true;
                this.validationMessage = "";
                try {
                    const response = await fetch("/Report/C13/Preview", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            reportCode: "C13",
                            startDate: this.form.startDate,
                            endDate: this.form.endDate
                        })
                    });
                    if (!response.ok) {
                        const problem = await response.json().catch(() => null);
                        throw new Error(problem?.title ?? "無法建立 C13 預覽。");
                    }
                    const previewWindow = window.open("", "_blank");
                    if (!previewWindow) throw new Error("瀏覽器已封鎖預覽視窗，請允許彈出視窗後重試。");
                    previewWindow.opener = null;
                    previewWindow.document.write(await response.text());
                    previewWindow.document.close();
                } catch (error) {
                    this.validationMessage = error instanceof Error ? error.message : "無法建立 C13 預覽。";
                } finally {
                    this.isLoading = false;
                }
            },
            async previewC16() {
                if (!this.isC16 || !this.hasResults || this.isLoading) return;
                this.isLoading = true;
                this.validationMessage = "";
                try {
                    const response = await fetch("/Report/C16/Preview", {
                        method: "POST", headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({ reportCode: "C16", startDate: this.form.startDate,
                            endDate: this.form.endDate, source: this.form.encounterSource,
                            reportType: this.form.reportType, dateMode: this.form.dateMode,
                            pageNumber: 1, pageSize: 10 })
                    });
                    if (!response.ok) {
                        const problem = await response.json().catch(() => null);
                        throw new Error(problem?.title ?? "無法建立 C16 預覽。");
                    }
                    const previewWindow = window.open("", "_blank");
                    if (!previewWindow) throw new Error("瀏覽器已封鎖預覽視窗，請允許彈出視窗後重試。");
                    previewWindow.opener = null;
                    previewWindow.document.write(await response.text());
                    previewWindow.document.close();
                } catch (error) {
                    this.validationMessage = error instanceof Error ? error.message : "無法建立 C16 預覽。";
                } finally { this.isLoading = false; }
            },
            async openC3Preview() {
                if (!this.isC3 || !this.hasResults || this.c3PreviewLoading) return;
                this.c3PreviewLoading = true;
                this.validationMessage = "";
                this.c3PreviewOpen = false;
                this.c3Preview = null;
                try {
                    const response = await fetch("/Report/C3/Preview", {
                        method: "POST", headers: { "Content-Type": "application/json", Accept: "application/json" },
                        body: JSON.stringify({ reportCode: "C3", startDate: this.form.startDate,
                            endDate: this.form.endDate, source: this.form.encounterSource,
                            detailType: this.form.detailType, logisticsType: this.form.logisticsType,
                            departmentCode: this.form.departmentCode.trim() || undefined,
                            roomCodes: this.form.roomCodes.trim() || undefined,
                            chargeCodes: this.form.chargeCodes.trim() || undefined,
                            pageNumber: 1, pageSize: 10 })
                    });
                    if (!response.ok) {
                        const problem = await response.json().catch(() => null);
                        throw new Error(typeof problem === "string"
                            ? problem : problem?.title ?? "無法建立 C3 預覽。");
                    }
                    const preview = await response.json();
                    if (!Array.isArray(preview?.rows) || preview.rows.length === 0) {
                        throw new Error("查無符合條件的資料，無法建立預覽。");
                    }
                    this.c3Preview = preview;
                    this.c3PreviewOpen = true;
                } catch (error) {
                    this.validationMessage = error instanceof Error ? error.message : "無法建立 C3 預覽。";
                } finally { this.c3PreviewLoading = false; }
            },
            closeC3Preview() {
                this.c3PreviewOpen = false;
            },
            printC3Preview() {
                if (this.c3PreviewOpen) window.print();
            },
            changeEncounterSource() {
                this.currentPage = 1;
                if (this.isC21) {
                    this.form.accountingScope = this.form.encounterSource === "Inpatient" ? 4 : 0;
                    this.form.forceRebuild = false;
                }
                if (this.isC23) {
                    this.form.inpatientType = "";
                    this.form.forceRebuild = false;
                }
                if (this.isC24 || this.isC10) {
                    this.form.source = this.form.encounterSource;
                    if (this.form.source === "Inpatient") this.form.roomScope = "All";
                }
                if (this.isC16 && this.form.encounterSource === "Inpatient") {
                    this.form.dateMode = "AccountingDate";
                }
            },
            changeC143ReportType() {
                this.currentPage = 1;
                this.hasSearched = false;
                this.rows = [];
                this.columns = [];
                this.serverTotalCount = 0;
                this.serverTotalPages = 0;
                this.validationMessage = "";
            },
            useYesterday() {
                const today = new Date();
                const localToday = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
                this.form.endDate = previousDate(localToday);
                this.currentPage = 1;
            },
            changeC24Source() {
                this.currentPage = 1;
                if (this.form.source === "Inpatient") this.form.roomScope = "All";
            },
            changeC24Mode() {
                this.currentPage = 1;
                this.form.forceRebuild = false;
            },
            changeC23DateMode() {
                this.currentPage = 1;
                this.form.forceRebuild = false;
                if (this.form.dateMode === "EncounterDate") this.form.inpatientType = "";
            },
            changeReceivableBalanceType() {
                this.currentPage = 1;
                this.hasSearched = false;
                this.rows = [];
                this.columns = [];
                this.serverTotalCount = 0;
                this.serverTotalPages = 0;
                this.validationMessage = "";
            },
            printC211() {
                if (this.isPrintableLegacyReport && this.hasResults) window.print();
            },
            async openC15Preview() {
                if (!this.isC15 || !this.hasResults || this.c15PreviewLoading) return;
                this.c15PreviewLoading = true;
                this.validationMessage = "";
                this.c15PreviewOpen = false;
                this.c15PreviewRows = [];
                this.c15PreviewSummary = null;
                try {
                    const response = await fetch("/Report/GetReportData", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            reportCode: "C15",
                            startDate: this.form.startDate,
                            endDate: this.form.endDate,
                            pageNumber: 1,
                            pageSize: this.serverTotalCount
                        })
                    });
                    if (!response.ok) {
                        const problem = await response.json().catch(() => null);
                        const traceId = problem && typeof problem.traceId === "string"
                            ? `（追蹤碼：${problem.traceId}）` : "";
                        const title = problem && typeof problem.title === "string"
                            ? problem.title : `預覽載入失敗（HTTP ${response.status}）`;
                        throw new Error(`${title}${traceId}`);
                    }
                    const result = await response.json();
                    const previewRows = Array.isArray(result.data) ? result.data : [];
                    if (previewRows.length !== this.serverTotalCount || !result.summary) {
                        throw new Error("預覽資料不完整，請重新查詢後再試。");
                    }
                    this.c15PreviewRows = previewRows;
                    this.c15PreviewSummary = result.summary;
                    this.c15PrintGeneratedAt = new Date().toLocaleString("zh-TW", {
                        timeZone: "Asia/Taipei",
                        hour12: false
                    });
                    this.c15PreviewOpen = true;
                } catch (error) {
                    this.validationMessage = error instanceof Error
                        ? error.message : "載入 C15 列印預覽時發生錯誤。";
                } finally {
                    this.c15PreviewLoading = false;
                }
            },
            closeC15Preview() {
                this.c15PreviewOpen = false;
            },
            printC15Preview() {
                if (this.c15PreviewOpen) window.print();
            },
            displayRocDate(value) {
                const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? "");
                if (!match) return value ?? "";
                return `${String(Number(match[1]) - 1911).padStart(3, "0")}/${match[2]}/${match[3]}`;
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
                            endDate: this.form.endDate,
                            source: this.isC10 || this.isC144 ? this.form.encounterSource : undefined,
                            roomScope: this.isC10 ? this.form.roomScope : undefined,
                            medicalRecordNo: this.isC10 && this.form.medicalRecordNo.trim()
                                ? this.form.medicalRecordNo.trim() : undefined
                        })
                    });
                    if (response.status === 200) {
                        const blob = await response.blob();
                        const disposition = response.headers?.get("Content-Disposition") ?? "";
                        const match = /filename\*?=(?:UTF-8''|\")?([^\";]+)/i.exec(disposition);
                        this.downloadBlob(blob, match ? decodeURIComponent(match[1]) : `${this.selectedReport.code}.xlsx`);
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
