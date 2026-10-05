(() => {
    const csrf = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    const emptyForm = () => ({
        medicalRecordNo: "",
        identityNumber: "",
        name: "",
        address1: "",
        address2: "",
        newIdentityNumber: "",
        newName: "",
        newBirthday: ""
    });

    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.MedicalRecordQuery = {
        template: "#medical-record-query-template",
        props: ["selectedReport"],
        data() {
            return {
                form: emptyForm(),
                advancedOpen: false,
                rows: [],
                loading: false,
                searched: false,
                error: "",
                conditionError: "",
                pageNumber: 1,
                pageSize: 10,
                totalCount: 0,
                totalPages: 0,
                snapshot: null,
                modalOpen: false,
                modalRow: null,
                modalDetail: null,
                modalLoading: false,
                modalError: "",
                generation: 0,
                modalGeneration: 0,
                lastTrigger: null
            };
        },
        computed: {
            hasResults() {
                return this.rows.length > 0;
            },
            activeFilterCount() {
                return [this.form.address1, this.form.address2, this.form.newIdentityNumber,
                    this.form.newName, this.form.newBirthday]
                    .filter(value => String(value || "").trim().length > 0).length;
            },
            hasAnyCondition() {
                return Object.values(this.form).some(value => String(value || "").trim().length > 0);
            }
        },
        methods: {
            conditions() {
                return {
                    medicalRecordNo: this.form.medicalRecordNo.trim(),
                    identityNumber: this.form.identityNumber.trim(),
                    name: this.form.name.trim(),
                    address1: this.form.address1.trim(),
                    address2: this.form.address2.trim(),
                    newIdentityNumber: this.form.newIdentityNumber.trim(),
                    newName: this.form.newName.trim(),
                    newBirthday: this.form.newBirthday || ""
                };
            },
            resetState() {
                this.generation++;
                this.clearModal();
                this.rows = [];
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
                this.form = emptyForm();
                this.advancedOpen = false;
                this.resetState();
            },
            async search() {
                this.pageNumber = 1;
                this.snapshot = this.conditions();
                this.error = "";
                this.conditionError = "";
                if (!this.hasAnyCondition) {
                    this.generation++;
                    this.rows = [];
                    this.loading = false;
                    this.totalCount = 0;
                    this.totalPages = 0;
                    this.searched = false;
                    this.conditionError = "請輸入任一條件";
                    this.clearModal();
                    return;
                }
                await this.fetchRecords(true);
            },
            async fetchRecords(newQuery = false) {
                if (newQuery) {
                    this.snapshot = this.conditions();
                    this.clearModal();
                }
                if (!this.snapshot) return;

                const current = ++this.generation;
                this.loading = true;
                this.error = "";
                this.conditionError = "";
                try {
                    const response = await fetch("/data-query/medical-record/records", {
                        method: "POST",
                        headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                        body: JSON.stringify({ ...this.snapshot, pageNumber: this.pageNumber, pageSize: this.pageSize })
                    });
                    if (!response.ok) throw new Error(await this.message(response));
                    const result = await response.json();
                    if (current !== this.generation) return;
                    this.rows = Array.isArray(result.rows) ? result.rows : [];
                    this.totalCount = Number.isInteger(result.totalCount) ? result.totalCount : 0;
                    this.totalPages = Number.isInteger(result.totalPages) ? result.totalPages : 0;
                    this.pageNumber = Number.isInteger(result.pageNumber) ? result.pageNumber : 1;
                    this.pageSize = Number.isInteger(result.pageSize) ? result.pageSize : this.pageSize;
                    this.searched = true;
                    this.clearModal();
                } catch (error) {
                    if (current !== this.generation) return;
                    this.rows = [];
                    this.totalCount = 0;
                    this.totalPages = 0;
                    this.searched = true;
                    this.error = error.message || "病歷查詢暫時無法使用。";
                    this.clearModal();
                } finally {
                    if (current === this.generation) this.loading = false;
                }
            },
            async openDetail(row, event) {
                if (!row?.medicalRecordNo) return;
                const current = ++this.modalGeneration;
                const query = this.generation;
                this.lastTrigger = event?.currentTarget || null;
                this.modalOpen = true;
                this.modalRow = row;
                this.modalDetail = null;
                this.modalError = "";
                this.modalLoading = true;
                if (typeof this.$nextTick === "function") this.$nextTick(() => this.$refs.modalClose?.focus());
                try {
                    const response = await fetch("/data-query/medical-record/detail", {
                        method: "POST",
                        headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                        body: JSON.stringify({ medicalRecordNo: row.medicalRecordNo })
                    });
                    if (!response.ok) throw new Error(await this.message(response));
                    const detail = await response.json();
                    if (current === this.modalGeneration && query === this.generation && this.modalRow?.medicalRecordNo === row.medicalRecordNo) {
                        this.modalDetail = detail;
                    }
                } catch (error) {
                    if (current === this.modalGeneration && query === this.generation && this.modalRow?.medicalRecordNo === row.medicalRecordNo) {
                        this.modalError = error.message || "病歷明細暫時無法使用。";
                    }
                } finally {
                    if (current === this.modalGeneration && query === this.generation && this.modalRow?.medicalRecordNo === row.medicalRecordNo) {
                        this.modalLoading = false;
                    }
                }
            },
            closeModal(restoreFocus = true) {
                const trigger = this.lastTrigger;
                this.modalGeneration++;
                this.modalOpen = false;
                this.modalRow = null;
                this.modalDetail = null;
                this.modalLoading = false;
                this.modalError = "";
                this.lastTrigger = null;
                if (restoreFocus) trigger?.focus();
            },
            clearModal() {
                this.closeModal(false);
            },
            async goToPage(page) {
                if (this.loading || page < 1 || page > this.totalPages) return;
                this.clearModal();
                this.pageNumber = page;
                await this.fetchRecords(false);
            },
            async changePageSize() {
                this.clearModal();
                this.pageNumber = 1;
                if (this.searched && this.snapshot && !this.loading) await this.fetchRecords(false);
            },
            async message(response) {
                const fallback = "病歷查詢暫時無法使用。";
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
