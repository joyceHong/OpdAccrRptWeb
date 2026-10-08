(() => {
    const csrf = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    const registrationNoError = "掛號序號只能輸入 1 至 3 位數字。";
    const emptyForm = defaultDate => ({
        mode: "registered",
        regDate: defaultDate || "",
        medicalRecordNo: "",
        patientId: "",
        birthDate: "",
        newSectionDisplay: "",
        newSectionCode: "",
        newSectionLegacyCode: "",
        room: "",
        time: "",
        registrationNo: "",
        doctorDisplay: "",
        doctorCode: ""
    });
    const inputCode = value => String(value || "").trim().split(/[｜|\s\t]+/)[0] || "";
    const modeOptions = [
        { value: "registered", label: "已掛號" },
        { value: "cancelled", label: "已退號" },
        { value: "unseen", label: "未看診" },
        { value: "seen", label: "已看診" },
        { value: "unpriced", label: "未批價" },
        { value: "summary", label: "報診" }
    ];

    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.RegistrationQuery = {
        template: "#registration-query-template",
        components: { ReportAutocomplete: window.ReportComponents.ReportAutocomplete },
        props: {
            selectedReport: { type: Object, default: () => ({}) },
            defaultStartDate: { type: String, default: "" }
        },
        data() {
            return {
                form: emptyForm(this.defaultStartDate),
                modeOptions,
                advancedOpen: false,
                rows: [],
                summary: null,
                loading: false,
                searched: false,
                error: "",
                conditionError: "",
                pageNumber: 1,
                pageSize: 10,
                totalCount: 0,
                totalPages: 0,
                snapshot: null,
                generation: 0,
                newSectionOptions: [],
                doctorOptions: [],
                newSectionOpen: false,
                doctorOpen: false,
                lookupLoading: { section: false, doctor: false },
                lookupGeneration: { section: 0, doctor: 0 }
            };
        },
        computed: {
            isSummary() {
                return this.form.mode === "summary";
            },
            hasResults() {
                return this.rows.length > 0;
            },
            activeFilterCount() {
                return [
                    this.form.birthDate,
                    this.form.newSectionDisplay,
                    this.form.room,
                    this.form.time,
                    this.form.registrationNo,
                    this.form.doctorDisplay
                ].filter(value => String(value || "").trim().length > 0).length;
            },
            cancelledNumberRows() {
                const numbers = Array.isArray(this.summary?.cancelledNumbers)
                    ? this.summary.cancelledNumbers
                    : [];
                const rows = [];
                for (let index = 0; index < numbers.length; index += 6) {
                    rows.push(numbers.slice(index, index + 6));
                }
                return rows;
            }
        },
        methods: {
            validateRegistrationNo(event) {
                const registrationNo = String(event.target.value || "").trim();
                if (registrationNo && !/^\d{1,3}$/.test(registrationNo)) {
                    this.conditionError = registrationNoError;
                } else if (this.conditionError === registrationNoError) {
                    this.conditionError = "";
                }
            },
            conditions() {
                return {
                    mode: this.form.mode,
                    regDate: this.form.regDate || "",
                    medicalRecordNo: this.form.medicalRecordNo.trim(),
                    patientId: this.form.patientId.trim(),
                    birthDate: this.form.birthDate || "",
                    sectionNo: this.form.newSectionLegacyCode || "",
                    newSectionNo: (this.form.newSectionCode || inputCode(this.form.newSectionDisplay)).trim(),
                    room: this.form.room.trim(),
                    time: this.form.time,
                    registrationNo: this.form.registrationNo.trim(),
                    doctorNo: (this.form.doctorCode || inputCode(this.form.doctorDisplay)).trim()
                };
            },
            resetResults() {
                this.generation++;
                this.rows = [];
                this.summary = null;
                this.loading = false;
                this.searched = false;
                this.error = "";
                this.conditionError = "";
                this.pageNumber = 1;
                this.totalCount = 0;
                this.totalPages = 0;
                this.snapshot = null;
            },
            resetForm() {
                this.form = emptyForm(this.defaultStartDate);
                this.advancedOpen = false;
                this.newSectionOptions = [];
                this.doctorOptions = [];
                this.newSectionOpen = false;
                this.doctorOpen = false;
                this.resetResults();
            },
            changeMode() {
                const mode = this.form.mode;
                const date = this.form.regDate;
                this.form = emptyForm(date || this.defaultStartDate);
                this.form.mode = mode;
                this.advancedOpen = false;
                this.newSectionOptions = [];
                this.doctorOptions = [];
                this.newSectionOpen = false;
                this.doctorOpen = false;
                this.resetResults();
            },
            async search() {
                const registrationNo = String(this.form.registrationNo || "").trim();
                if (this.conditionError === registrationNoError ||
                    (registrationNo && !/^\d{1,3}$/.test(registrationNo))) {
                    this.resetResults();
                    this.conditionError = registrationNoError;
                    return;
                }

                this.pageNumber = 1;
                this.snapshot = this.conditions();
                this.error = "";
                this.conditionError = "";
                await this.fetchResults(true);
            },
            async fetchResults(newQuery = false) {
                if (newQuery) {
                    this.snapshot = this.conditions();
                }
                if (!this.snapshot) return;

                const current = ++this.generation;
                this.loading = true;
                this.error = "";
                this.conditionError = "";
                try {
                    const response = await fetch("/data-query/registration/query", {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            "RequestVerificationToken": csrf()
                        },
                        body: JSON.stringify({
                            ...this.snapshot,
                            pageNumber: this.pageNumber,
                            pageSize: this.pageSize
                        })
                    });
                    if (!response.ok) throw new Error(await this.message(response));
                    const result = await response.json();
                    if (current !== this.generation) return;
                    this.rows = Array.isArray(result.rows) ? result.rows : [];
                    this.summary = result.summary || null;
                    this.totalCount = Number.isInteger(result.totalCount) ? result.totalCount : 0;
                    this.totalPages = Number.isInteger(result.totalPages) ? result.totalPages : 0;
                    this.pageNumber = Number.isInteger(result.pageNumber) ? result.pageNumber : 1;
                    this.pageSize = Number.isInteger(result.pageSize) ? result.pageSize : this.pageSize;
                    this.searched = true;
                } catch (error) {
                    if (current !== this.generation) return;
                    this.rows = [];
                    this.summary = null;
                    this.totalCount = 0;
                    this.totalPages = 0;
                    this.searched = true;
                    this.error = error.message || "掛號資料查詢暫時無法使用。";
                } finally {
                    if (current === this.generation) this.loading = false;
                }
            },
            async goToPage(page) {
                if (this.loading || this.isSummary || page < 1 || page > this.totalPages) return;
                this.pageNumber = page;
                await this.fetchResults(false);
            },
            async changePageSize() {
                this.pageNumber = 1;
                if (!this.isSummary && this.searched && this.snapshot && !this.loading) {
                    await this.fetchResults(false);
                }
            },
            sectionOption(raw) {
                const legacyCode = String(raw?.legacyCode || "").trim();
                const newCode = String(raw?.newCode || "").trim();
                const code = newCode || legacyCode;
                const suffix = legacyCode && legacyCode !== code ? "舊碼 " + legacyCode : "";
                return {
                    value: legacyCode + ":" + newCode,
                    code,
                    legacyCode,
                    label: String(raw?.name || "").trim(),
                    suffix,
                    raw
                };
            },
            async loadSections(value) {
                this.form.newSectionCode = "";
                this.form.newSectionLegacyCode = "";
                const query = String(value || this.form.newSectionDisplay || "").trim();
                const current = ++this.lookupGeneration.section;
                if (!query) {
                    this.newSectionOptions = [];
                    this.newSectionOpen = false;
                    return;
                }
                this.lookupLoading.section = true;
                try {
                    const response = await fetch("/data-query/registration/sections?q=" + encodeURIComponent(query));
                    if (!response.ok) throw new Error();
                    const result = await response.json();
                    if (current !== this.lookupGeneration.section) return;
                    this.newSectionOptions = (Array.isArray(result) ? result : [])
                        .map(item => this.sectionOption(item))
                        .filter(item => item.code);
                    this.newSectionOpen = this.newSectionOptions.length > 0;
                } catch {
                    if (current === this.lookupGeneration.section) {
                        this.newSectionOptions = [];
                        this.newSectionOpen = false;
                        this.conditionError = "無法載入科別清單，仍可直接輸入科別代碼。";
                    }
                } finally {
                    if (current === this.lookupGeneration.section) this.lookupLoading.section = false;
                }
            },
            selectSectionOption(option) {
                this.form.newSectionCode = option.code;
                this.form.newSectionLegacyCode = option.legacyCode;
                this.form.newSectionDisplay = option.code + (option.label ? "｜" + option.label : "");
                this.newSectionOpen = false;
            },
            async loadDoctors(value) {
                this.form.doctorCode = "";
                const query = String(value || this.form.doctorDisplay || "").trim();
                const current = ++this.lookupGeneration.doctor;
                if (!query) {
                    this.doctorOptions = [];
                    this.doctorOpen = false;
                    return;
                }
                this.lookupLoading.doctor = true;
                try {
                    const response = await fetch("/data-query/registration/doctors?q=" + encodeURIComponent(query));
                    if (!response.ok) throw new Error();
                    const result = await response.json();
                    if (current !== this.lookupGeneration.doctor) return;
                    this.doctorOptions = (Array.isArray(result) ? result : [])
                        .map(item => ({
                            value: String(item?.code || "").trim(),
                            code: String(item?.code || "").trim(),
                            label: String(item?.name || "").trim(),
                            suffix: ""
                        }))
                        .filter(item => item.code);
                    this.doctorOpen = this.doctorOptions.length > 0;
                } catch {
                    if (current === this.lookupGeneration.doctor) {
                        this.doctorOptions = [];
                        this.doctorOpen = false;
                        this.conditionError = "無法載入醫師清單，仍可直接輸入醫師代號。";
                    }
                } finally {
                    if (current === this.lookupGeneration.doctor) this.lookupLoading.doctor = false;
                }
            },
            selectDoctor(option) {
                this.form.doctorCode = option.code;
                this.form.doctorDisplay = option.code + (option.label ? "｜" + option.label : "");
                this.doctorOpen = false;
            },
            async message(response) {
                const fallback = "掛號資料查詢暫時無法使用。";
                try {
                    const value = await response.json();
                    if (typeof value === "string" && value.trim()) return value;
                    if (typeof value?.title === "string" && value.title.trim()) return value.title;
                    if (typeof value?.message === "string" && value.message.trim()) return value.message;
                } catch {
                    return fallback;
                }
                return fallback;
            }
        }
    };
})();
