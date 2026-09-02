(() => {
    const initialState = JSON.parse(document.getElementById("report-initial-state").textContent);
    const { createApp } = Vue;
    const reportComponentMap = Object.freeze({
        C1: window.ReportComponents.ReportTemplate,
        C22: window.ReportComponents.ReportTemplate,
        C213: window.ReportComponents.ReportTemplate,
        C214: window.ReportComponents.ReportTemplate,
        C25: window.ReportComponents.ReportTemplate,
        C27: window.ReportComponents.ReportTemplate,
        C28: window.ReportComponents.ReportTemplate,
        C29: window.ReportComponents.ReportTemplate,
        C171: window.ReportComponents.ReportTemplate,
        C172: window.ReportComponents.ReportTemplate,
        C173: window.ReportComponents.ReportTemplate,
        C174: window.ReportComponents.ReportTemplate,
        C18: window.ReportComponents.ReportTemplate,
        C19: window.ReportComponents.ReportTemplate
    });
    const unavailableReportComponent = {
        template: '<section class="panel empty-result"><strong>此報表畫面尚未建置</strong><p>請選擇已開放的報表。</p></section>'
    };
    const reportRoutes = Object.entries(reportComponentMap).map(([reportCode, component]) => ({
        path: `/Report/${reportCode}`,
        component
    }));
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
                sidebarOpen: false,
                reportKeyword: "",
                openGroups: [0],
                toast: "",
                toastTimer: null
            };
        },
        computed: {
            filteredGroups() {
                const keyword = this.reportKeyword.toLowerCase();
                if (!keyword) return this.activeCategory.groups;
                return this.activeCategory.groups.map(group => ({ ...group, reports: group.reports.filter(report => `${report.code} ${report.name}`.toLowerCase().includes(keyword)) })).filter(group => group.reports.length);
            }
        },
        watch: {
            "$route.params.reportCode": {
                immediate: true,
                handler(reportCode) {
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
                this.sidebarOpen = false;
                const path = this.selectedReport ? `/Report/${this.selectedReport.code}` : "/Report";
                if (this.$route.path !== path) this.$router.push(path);
            },
            selectReport(report) {
                this.selectedReport = report;
                this.sidebarOpen = false;
                const path = `/Report/${report.code}`;
                if (this.$route.path !== path) this.$router.push(path);
            },
            toggleGroup(index) { this.openGroups = this.openGroups.includes(index) ? this.openGroups.filter(value => value !== index) : [...this.openGroups, index]; },
            collapseAll() { this.openGroups = []; },
            showToast(message) { this.toast = message; window.clearTimeout(this.toastTimer); this.toastTimer = window.setTimeout(() => { this.toast = ""; }, 3000); }
        }
    });

    app.use(router);
    app.mount("#report-app");
})();
