(() => {
    window.ReportDateRangeMixin = {
        data() {
            return { endDateManuallyChanged: false };
        },
        watch: {
            form() {
                this.endDateManuallyChanged = false;
            }
        },
        methods: {
            syncDefaultEndDate() {
                if (this.endDateManuallyChanged || !this.form.startDate) return false;
                this.form.endDate = this.form.startDate;
                return true;
            },
            markEndDateChanged() {
                this.endDateManuallyChanged = true;
            }
        }
    };
})();
