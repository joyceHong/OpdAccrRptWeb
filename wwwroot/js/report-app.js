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
    const queryCollapseCleanups = new WeakMap();
    const queryConditionTags = form => Array.from(form.querySelectorAll("input, select, textarea"))
        .filter(control => !control.disabled && control.type !== "hidden" && control.type !== "submit" && control.type !== "button")
        .filter(control => !["checkbox", "radio"].includes(control.type) || control.checked)
        .map(control => {
            const label = control.closest("label");
            const group = control.closest("fieldset");
            const caption = (control.type === "radio"
                ? group?.querySelector("legend")?.textContent.trim()
                : label?.querySelector("span")?.textContent.trim() || label?.textContent.trim())?.replace(/\s*\*$/, "");
            const value = control.tagName === "SELECT"
                ? control.selectedOptions[0]?.textContent.trim()
                : control.type === "radio"
                    ? label?.textContent.trim()
                    : control.type === "checkbox"
                        ? ""
                        : String(control.value || "").trim().replaceAll("-", "/");
            if (!caption || (control.type !== "checkbox" && !value)) return null;
            return value ? `${caption}：${value}` : caption;
        })
        .filter(Boolean);
    const queryCollapseDirective = {
        mounted(element, binding) {
            let active = null;
            let disposed = false;
            let observer = null;
            let collapsed = false;
            const release = () => {
                active?.cleanup();
                active = null;
            };
            const synchronize = () => {
                if (disposed) return;
                const panel = element.querySelector(".query-panel");
                const form = panel?.querySelector("form");
                const title = panel?.querySelector(".panel-title");
                if (!panel || !form || !title) {
                    release();
                    return;
                }
                if (active?.panel === panel && active.form === form && active.title === title) {
                    active.restore();
                    return;
                }
                release();
                if (!form.id) form.id = `report-query-${String(binding.value || "default").toLowerCase()}`;
                const button = document.createElement("button");
                button.type = "button";
                button.className = "query-collapse-button";
                button.setAttribute("aria-controls", form.id);
                button.setAttribute("aria-expanded", String(!collapsed));
                button.textContent = collapsed ? "⌄ 展開" : "⌃ 收合";
                const summary = document.createElement("div");
                summary.className = "query-condition-tags";
                summary.hidden = !collapsed;
                summary.setAttribute("aria-label", "目前查詢條件");
                const refreshSummary = () => {
                    const conditions = queryConditionTags(form);
                    const labels = conditions.length ? conditions : ["尚未設定條件"];
                    summary.replaceChildren(...labels.map(value => {
                        const tag = document.createElement("span");
                        tag.className = "query-condition-tag";
                        tag.textContent = value;
                        tag.title = value;
                        return tag;
                    }));
                    summary.title = labels.join(" · ");
                };
                let animation = null;
                const toggle = () => {
                    const nextCollapsed = !collapsed;
                    if (nextCollapsed && form.contains(document.activeElement)) button.focus();
                    const startHeight = form.getBoundingClientRect?.().height ?? form.scrollHeight;
                    animation?.cancel();
                    collapsed = nextCollapsed;
                    form.hidden = false;
                    form.inert = collapsed;
                    panel.classList.toggle("query-panel-collapsed", collapsed);
                    if (collapsed) refreshSummary();
                    summary.hidden = !collapsed;
                    button.setAttribute("aria-expanded", String(!collapsed));
                    button.textContent = collapsed ? "⌄ 展開" : "⌃ 收合";
                    const reducedMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
                    if (!reducedMotion && typeof form.animate === "function") {
                        const endHeight = collapsed ? 0 : form.scrollHeight;
                        form.style.overflow = "hidden";
                        animation = form.animate([
                            { height: `${startHeight}px`, opacity: collapsed ? 1 : 0 },
                            { height: `${endHeight}px`, opacity: collapsed ? 0 : 1 }
                        ], { duration: 240, easing: "ease-in-out" });
                        const currentAnimation = animation;
                        animation.onfinish = () => {
                            if (animation !== currentAnimation) return;
                            form.hidden = collapsed;
                            form.style.overflow = "";
                            animation = null;
                        };
                    } else {
                        form.hidden = collapsed;
                        form.style.overflow = "";
                        animation = null;
                    }
                };
                const restore = () => {
                    if (!title.contains(summary)) title.insertBefore(summary, title.querySelector("small,.report-code"));
                    if (!title.contains(button)) title.appendChild(button);
                };
                button.addEventListener("click", toggle);
                form.addEventListener("input", refreshSummary);
                form.addEventListener("change", refreshSummary);
                form.hidden = collapsed;
                form.inert = collapsed;
                panel.classList.toggle("query-panel-collapsed", collapsed);
                if (collapsed) refreshSummary();
                active = { panel, form, title, restore, cleanup: () => {
                    animation?.cancel();
                    button.removeEventListener("click", toggle);
                    form.removeEventListener("input", refreshSummary);
                    form.removeEventListener("change", refreshSummary);
                    button.remove?.();
                    summary.remove?.();
                } };
                restore();
            };
            synchronize();
            if (typeof MutationObserver === "function") {
                observer = new MutationObserver(synchronize);
                observer.observe(element, { childList: true, subtree: true });
            }
            queryCollapseCleanups.set(element, () => {
                disposed = true;
                observer?.disconnect();
                release();
            });
        },
        beforeUnmount(element) {
            queryCollapseCleanups.get(element)?.();
            queryCollapseCleanups.delete(element);
        }
    };
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
        Q1: window.ReportComponents.OpdPriceQuery,
        Q2: window.ReportComponents.MedicalRecordQuery,
        Q3: window.ReportComponents.RegistrationQuery
    });
    const unavailableReportComponent = {
        props: ["selectedReport"],
        template: '<section class="panel empty-result"><strong>{{ selectedReport ? selectedReport.name : "此功能" }}尚未建置</strong><p>請選擇已開放的查詢或報表。</p></section>'
    };
    const reportRoutes = Object.entries(reportComponentMap).map(([reportCode, component]) => ({
        path: reportCode === "Q1"
            ? "/data-query/opd-price"
            : reportCode === "Q2"
                ? "/data-query/medical-record"
                : reportCode === "Q3"
                    ? "/data-query/registration"
                    : `/Report/${reportCode}`,
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
                if (report.code === "Q2") return "/data-query/medical-record";
                if (report.code === "Q3") return "/data-query/registration";
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

    app.directive("report-query-collapse", queryCollapseDirective);
    app.use(router);
    app.mount("#report-app");
})();
