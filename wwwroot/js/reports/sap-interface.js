(() => {
    const eventCodes = ["SAPCASH", "SAPCONS", "SAPACC", "SAPREV2"];
    const csrf = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    const failureMessage = async response => {
        const body = await response.json().catch(() => null);
        return typeof body === "string" ? body : body?.title || "SAP 作業暫時無法使用。";
    };

    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.SapInterface = {
        template: "#sap-interface-template",
        data() {
            return {
                businessDate: "",
                events: [],
                selected: Object.fromEntries(eventCodes.map(code => [code, true])),
                confirmRerun: {},
                confirmationRequired: [],
                results: {},
                loading: false,
                error: "",
                pendingRequest: null
            };
        },
        methods: {
            async loadDefaultDate() {
                this.loading = true;
                this.error = "";
                try {
                    const response = await fetch("/sap-interface/default-date", { cache: "no-store" });
                    if (!response.ok) throw new Error(await failureMessage(response));
                    this.businessDate = (await response.json()).businessDate;
                    await this.loadStatus();
                } catch (error) {
                    this.error = error.message;
                } finally {
                    this.loading = false;
                }
            },
            async loadStatus() {
                this.confirmationRequired = [];
                this.pendingRequest = null;
                this.results = {};
                if (!this.businessDate) { this.events = []; return; }
                this.loading = true;
                this.error = "";
                try {
                    const response = await fetch(`/sap-interface/status?businessDate=${encodeURIComponent(this.businessDate)}`, { cache: "no-store" });
                    if (!response.ok) throw new Error(await failureMessage(response));
                    this.events = await response.json();
                } catch (error) {
                    this.events = [];
                    this.error = error.message;
                } finally {
                    this.loading = false;
                }
            },
            request() {
                return {
                    businessDate: this.businessDate,
                    runCash: this.selected.SAPCASH,
                    runContract: this.selected.SAPCONS,
                    runAcc: this.selected.SAPACC,
                    runRev: this.selected.SAPREV2,
                    confirmRerun: []
                };
            },
            async run() {
                this.results = {};
                this.pendingRequest = this.request();
                await this.submit(this.pendingRequest);
            },
            async runConfirmed() {
                const request = { ...this.pendingRequest, confirmRerun: [...this.pendingRequest.confirmRerun] };
                const skipped = {};
                for (const item of this.confirmationRequired) {
                    if (this.confirmRerun[item.event]) {
                        request.confirmRerun.push(item.event);
                    } else {
                        const property = { SAPCASH: "runCash", SAPCONS: "runContract", SAPACC: "runAcc", SAPREV2: "runRev" }[item.event];
                        request[property] = false;
                        skipped[item.event] = { status: "skipped" };
                    }
                }
                this.confirmationRequired = [];
                await this.submit(request, skipped);
            },
            cancelConfirmation() {
                this.confirmationRequired = [];
                this.pendingRequest = null;
            },
            async submit(request, skipped = {}) {
                this.loading = true;
                this.error = "";
                try {
                    const response = await fetch("/sap-interface/run", {
                        method: "POST",
                        headers: { "Content-Type": "application/json", "RequestVerificationToken": csrf() },
                        body: JSON.stringify(request)
                    });
                    if (!response.ok) throw new Error(await failureMessage(response));
                    const result = await response.json();
                    if (result.confirmationRequired?.length) {
                        for (const item of result.events || []) {
                            if (item.status === "completed") {
                                this.results[item.event] = item;
                                const property = { SAPCASH: "runCash", SAPCONS: "runContract", SAPACC: "runAcc", SAPREV2: "runRev" }[item.event];
                                request[property] = false;
                            }
                        }
                        this.confirmationRequired = result.confirmationRequired;
                        this.confirmRerun = {};
                        this.pendingRequest = request;
                    } else {
                        this.results = { ...this.results, ...skipped, ...Object.fromEntries(result.events.map(item => [item.event, item])) };
                        this.pendingRequest = null;
                        await this.loadStatusAfterRun();
                    }
                } catch (error) {
                    this.error = error.message;
                } finally {
                    this.loading = false;
                }
            },
            async loadStatusAfterRun() {
                const response = await fetch(`/sap-interface/status?businessDate=${encodeURIComponent(this.businessDate)}`, { cache: "no-store" });
                if (response.ok) this.events = await response.json();
            },
            resultLabel(event) {
                const result = this.results[event];
                return result?.status === "completed" ? "完成"
                    : result?.status === "noData" ? "查無資料"
                        : result?.status === "failed" ? result.message || "失敗"
                            : result?.status === "skipped" ? "已略過"
                                : result?.status === "confirmationRequired" ? "待確認重跑" : "—";
            }
        },
        mounted() { this.loadDefaultDate(); }
    };
})();
