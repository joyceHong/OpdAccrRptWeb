(() => {
    const SIDEBAR_PREFERENCE_KEY = "opd-report-sidebar:v1";
    const readSidebarPreference = () => {
        try {
            const value = window.localStorage.getItem(SIDEBAR_PREFERENCE_KEY);
            return value === "collapsed" || value === "expanded" ? value : "expanded";
        } catch {
            return "expanded";
        }
    };
    const writeSidebarPreference = value => {
        try { window.localStorage.setItem(SIDEBAR_PREFERENCE_KEY, value); } catch { /* storage is optional */ }
    };
    window.ReportLayout = Object.freeze({ SIDEBAR_PREFERENCE_KEY, readSidebarPreference, writeSidebarPreference });
    const initialState = JSON.parse(document.getElementById("report-initial-state").textContent);
    const { createApp } = Vue;
    const reportComponentMap = Object.freeze({
        C1: window.ReportComponents.ReportTemplate,
        C3: window.ReportComponents.ReportTemplate,
        C4: window.ReportComponents.ReportTemplate,
        C5: window.ReportComponents.C5Report,
        C6: window.ReportComponents.C5Report,
        C7: window.ReportComponents.C7Report,
        C8: window.ReportComponents.C8Report,
        C9: window.ReportComponents.C9Report,
        C10: window.ReportComponents.ReportTemplate,
        C11: window.ReportComponents.C11Report,
        C12: window.ReportComponents.C12Report,
        C13: window.ReportComponents.ReportTemplate,
        C15: window.ReportComponents.ReportTemplate,
        C16: window.ReportComponents.ReportTemplate,
        C143: window.ReportComponents.ReportTemplate,
        C144: window.ReportComponents.ReportTemplate,
        C21: window.ReportComponents.ReportTemplate,
        C23: window.ReportComponents.ReportTemplate,
        C24: window.ReportComponents.ReportTemplate,
        C22: window.ReportComponents.ReportTemplate,
        C213: window.ReportComponents.ReportTemplate,
        C214: window.ReportComponents.ReportTemplate,
        C25: window.ReportComponents.ReportTemplate,
        C27: window.ReportComponents.ReportTemplate,
        C28: window.ReportComponents.ReportTemplate,
        C211: window.ReportComponents.ReportTemplate,
        C212: window.ReportComponents.ReportTemplate,
        C29: window.ReportComponents.ReportTemplate,
        C171: window.ReportComponents.ReportTemplate,
        C172: window.ReportComponents.ReportTemplate,
        C173: window.ReportComponents.ReportTemplate,
        C174: window.ReportComponents.ReportTemplate,
        C18: window.ReportComponents.ReportTemplate,
        C19: window.ReportComponents.ReportTemplate,
        M1: window.ReportComponents.M1DoctorDailyReport,
        M2: window.ReportComponents.M2DoctorMonthlyReport,
        M3: window.ReportComponents.M3OpdEmergencyDailyReport,
        Q1: window.ReportComponents.OpdPriceQuery
    });
    const unavailableReportComponent = {
        props: ["selectedReport"],
        template: '<section class="panel empty-result"><strong>{{ selectedReport ? selectedReport.name : "此功能" }}尚未建置</strong><p>請選擇已開放的查詢或報表。</p></section>'
    };
    const reportRoutes = Object.entries(reportComponentMap).map(([reportCode, component]) => ({
        path: reportCode === "Q1" ? "/data-query/opd-price" : `/Report/${reportCode}`,
        component,
        meta: { reportCode }
    }));
    reportRoutes.unshift({ path: "/medical-statistics/doctor-daily", component: window.ReportComponents.M1DoctorDailyReport, meta: { reportCode: "M1" } });
    reportRoutes.unshift({ path: "/medical-statistics/doctor-monthly", component: window.ReportComponents.M2DoctorMonthlyReport, meta: { reportCode: "M2" } });
    reportRoutes.unshift({ path: "/medical-statistics/opd-emergency-daily", component: window.ReportComponents.M3OpdEmergencyDailyReport, meta: { reportCode: "M3" } });
    reportRoutes.push(
        { path: "/Report", component: unavailableReportComponent },
        { path: "/Report/:reportCode", component: unavailableReportComponent }
    );
    const router = VueRouter.createRouter({
        history: VueRouter.createWebHistory(),
        routes: reportRoutes
    });

    const app = createApp({
        data() {
            return {
                state: initialState,
                activeCategory: initialState.categories[0],
                selectedReport: initialState.categories[0].groups[0].reports[0],
                sidebarPinned: true,
                mobileDrawerOpen: false,
                reportKeyword: "",
                openGroups: [0],
                toast: "",
                toastTimer: null
            };
        },
        computed: {
            sidebarClasses() {
                return {
                    "is-pinned": this.sidebarPinned,
                    "is-collapsed": !this.sidebarPinned,
                    "is-drawer-open": this.mobileDrawerOpen
                };
            },
            filteredGroups() {
                const keyword = this.reportKeyword.toLowerCase();
                if (!keyword) return this.activeCategory.groups;
                return this.activeCategory.groups.map(group => ({ ...group, reports: group.reports.filter(report => `${report.code} ${report.name}`.toLowerCase().includes(keyword)) })).filter(group => group.reports.length);
            }
        },
        watch: {
            "$route.path": {
                immediate: true,
                handler() {
                    const reportCode = this.$route.meta.reportCode || this.$route.params.reportCode;
                    if (!reportCode) return;
                    const reportLocation = this.findReport(reportCode);
                    if (!reportLocation) return;
                    this.activeCategory = reportLocation.category;
                    this.selectedReport = reportLocation.report;
                }
            }
        },
        methods: {
            findReport(reportCode) {
                for (const category of this.state.categories) {
                    for (const group of category.groups) {
                        const report = group.reports.find(item => item.code === reportCode);
                        if (report) return { category, report };
                    }
                }
                return null;
            },
            selectCategory(category) {
                this.activeCategory = category;
                this.openGroups = [0];
                this.reportKeyword = "";
                this.selectedReport = category.groups[0]?.reports[0] ?? null;
                this.closeMobileDrawer();
                const path = this.selectedReport ? this.reportPath(this.selectedReport) : "/Report";
                if (this.$route.path !== path) this.$router.push(path);
            },
            selectReport(report) {
                this.selectedReport = report;
                this.closeMobileDrawer();
                const path = this.reportPath(report);
                if (this.$route.path !== path) this.$router.push(path);
            },
            reportPath(report) {
                if (report.code === "Q1") return "/data-query/opd-price";
                if (report.code === "M1") return "/medical-statistics/doctor-daily";
                if (report.code === "M2") return "/medical-statistics/doctor-monthly";
                if (report.code === "M3") return "/medical-statistics/opd-emergency-daily";
                return `/Report/${report.code}`;
            },
            toggleGroup(index) { this.openGroups = this.openGroups.includes(index) ? this.openGroups.filter(value => value !== index) : [...this.openGroups, index]; },
            collapseAll() { this.openGroups = []; },
            togglePinnedSidebar() {
                this.sidebarPinned = !this.sidebarPinned;
                writeSidebarPreference(this.sidebarPinned ? "expanded" : "collapsed");
                if (this.sidebarPinned) this.mobileDrawerOpen = false;
            },
            closeSidebarOverlay() {
                if (this.sidebarPinned) {
                    this.sidebarPinned = false;
                    writeSidebarPreference("collapsed");
                    this.$nextTick(() => this.$refs.sidebarTrigger?.focus());
                }
                this.closeMobileDrawer();
            },
            toggleMobileDrawer() { this.mobileDrawerOpen = !this.mobileDrawerOpen; },
            closeMobileDrawer() { this.mobileDrawerOpen = false; },
            showToast(message) { this.toast = message; window.clearTimeout(this.toastTimer); this.toastTimer = window.setTimeout(() => { this.toast = ""; }, 3000); }
        },
        beforeUnmount() {
            window.clearTimeout(this.toastTimer);
        }
    });

    app.use(router);
    app.mount("#report-app");
})();
