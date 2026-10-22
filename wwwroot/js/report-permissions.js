(() => {
    "use strict";

    const h = Vue.h;
    const actionNames = [[1, "查詢"], [2, "Excel"], [4, "列印"], [8, "重建"], [16, "C141 LineBot"]];
    const targetNames = { 0: "管理者", 1: "單位", 2: "個人" };

    function actionSummary(mask) {
        return actionNames.filter(([bit]) => Boolean(mask & bit))
            .map(([, name]) => name).join("、") || "無操作";
    }

    function button(text, onClick, extra = {}) {
        return h("button", { type: "button", onClick, ...extra }, text);
    }

    function checkbox(checked, onChange, extra = {}) {
        return h("input", {
            type: "checkbox", checked,
            onChange: event => onChange(event.target.checked), ...extra
        });
    }

    window.initializeReportPermissions = () => {
        const root = document.getElementById("permission-panel-root");
        const catalogElement = document.getElementById("permission-catalog");
        if (!root || !catalogElement || root.__permissionApp) return;
        const catalog = JSON.parse(catalogElement.textContent);
        const definitions = new Map(catalog.flatMap(category => category.groups)
            .flatMap(group => group.reports).map(report => [report.code, report]));
        const token = document.getElementById("permission-panel-token")
            .querySelector('input[name="__RequestVerificationToken"]').value;

        const app = Vue.createApp({
            data() {
                return {
                    listItems: [], listStatus: "載入中…", listTotal: 0,
                    listPage: 1, listPageSize: 20, listPages: 1,
                    listSearch: "", listCategory: "", listRequest: 0,
                    editorTitle: "新增權限", editorMessage: "", editorLoading: false,
                    saving: false, editingTarget: false, editorRequest: 0,
                    targetType: 0, targetText: "", selectedTargetCode: null,
                    targetHint: "輸入至少兩個字，並從清單中選取。",
                    targetOptions: [], targetSearchRequest: 0,
                    selectedCodes: [], existingGrants: {},
                    actions: { query: true, excel: true, print: true, rebuild: false, lineBot: false },
                    generalActionsChanged: false, initialLineBotChecked: false,
                    copySource: "", copyTarget: "", copySourceGrants: [],
                    copySelectedCodes: [], copyPreviewRows: [], copyPayload: null,
                    copyMessage: "", copyApplying: false,
                    copySourceRequest: 0, copyPreviewRequest: 0,
                    deleteRow: null, deleteMessage: "", deletePayload: null,
                    deleteRequest: 0, deleteApplying: false
                };
            },
            mounted() {
                this.loadList();
            },
            beforeUnmount() {
                clearTimeout(this.listTimer);
                clearTimeout(this.targetTimer);
            },
            methods: {
                async getJson(path, fallback) {
                    const response = await fetch(path, { credentials: "same-origin" });
                    if (!response.ok) throw new Error(await response.text() || fallback);
                    return response.json();
                },
                async postJson(path, payload) {
                    const response = await fetch(path, {
                        method: "POST", credentials: "same-origin",
                        headers: {
                            "Content-Type": "application/json",
                            "RequestVerificationToken": token
                        },
                        body: JSON.stringify(payload)
                    });
                    if (!response.ok) throw new Error(await response.text() || "操作失敗。");
                    return response;
                },
                notifyChange() {
                    window.dispatchEvent(new Event("report-permissions-changed"));
                },
                async loadList() {
                    const requestId = ++this.listRequest;
                    this.listStatus = "載入中…";
                    const query = new URLSearchParams({
                        page: String(this.listPage), pageSize: String(this.listPageSize)
                    });
                    if (this.listSearch.trim()) query.set("search", this.listSearch.trim());
                    if (this.listCategory) query.set("categoryKey", this.listCategory);
                    try {
                        const result = await this.getJson("/permissions/target-list?" + query,
                            "無法讀取權限列表。");
                        if (requestId !== this.listRequest) return false;
                        if (!result.items.length && result.totalCount > 0 && this.listPage > 1) {
                            this.listPage = Math.ceil(result.totalCount / result.pageSize);
                            return this.loadList();
                        }
                        this.listItems = result.items;
                        this.listTotal = result.totalCount;
                        this.listPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
                        this.listStatus = result.items.length ? "" :
                            "找不到符合條件的授權，可清除篩選或新增權限。";
                        return true;
                    } catch (error) {
                        if (requestId === this.listRequest) {
                            this.listItems = [];
                            this.listTotal = 0;
                            this.listPages = 1;
                            this.listStatus = error.message;
                        }
                        return false;
                    }
                },
                searchList() {
                    clearTimeout(this.listTimer);
                    this.listPage = 1;
                    this.listTimer = setTimeout(() => this.loadList(), 250);
                },
                clearList() {
                    this.listSearch = "";
                    this.listCategory = "";
                    this.listPage = 1;
                    this.loadList();
                },
                goToPage(page) {
                    if (page < 1 || page > this.listPages || page === this.listPage) return;
                    this.listPage = page;
                    this.loadList();
                },
                resetEditor() {
                    this.editorRequest++;
                    this.targetSearchRequest++;
                    clearTimeout(this.targetTimer);
                    this.editorLoading = false;
                    this.saving = false;
                    this.editorMessage = "";
                    this.targetOptions = [];
                    this.targetHint = "輸入至少兩個字，並從清單中選取。";
                    this.selectedCodes = [];
                    this.existingGrants = {};
                    this.generalActionsChanged = false;
                    this.actions = {
                        query: true, excel: true, print: true, rebuild: false, lineBot: false
                    };
                    this.initialLineBotChecked = false;
                },
                async openAdd() {
                    this.resetEditor();
                    this.editingTarget = false;
                    this.editorTitle = "新增權限";
                    this.targetType = 0;
                    this.targetText = "";
                    this.selectedTargetCode = null;
                    await this.$nextTick();
                    this.$refs.editorDialog.showModal();
                    this.$refs.targetInput.focus();
                },
                async openEditor(row) {
                    this.resetEditor();
                    const requestId = this.editorRequest;
                    this.editingTarget = true;
                    this.editorLoading = true;
                    this.editorTitle = "修改權限";
                    this.targetType = row.targetType;
                    this.targetText = `${row.targetName || row.target} (${row.target})`;
                    this.selectedTargetCode = row.target;
                    this.targetHint = "編輯既有對象時，授權類型及對象固定。";
                    this.editorMessage = "正在載入此對象的報表權限…";
                    await this.$nextTick();
                    this.$refs.editorDialog.showModal();
                    try {
                        const query = new URLSearchParams({
                            targetType: String(row.targetType), target: row.target
                        });
                        const grants = await this.getJson("/permissions/grants?" + query, "無法讀取授權。");
                        if (requestId !== this.editorRequest || !this.$refs.editorDialog.open) return;
                        this.existingGrants = Object.fromEntries(grants
                            .filter(grant => definitions.has(grant.reportCode))
                            .map(grant => [grant.reportCode, grant.actionMask]));
                        this.selectedCodes = Object.keys(this.existingGrants);
                        const first = grants.find(grant => grant.actionMask > 0);
                        if (first) this.setActionsFromMask(first.actionMask);
                        // LineBot belongs to C141, so another report's mask cannot initialize it.
                        this.actions.lineBot = Boolean((this.existingGrants.C141 ?? 0) & 16);
                        this.initialLineBotChecked = this.actions.lineBot;
                        this.editorLoading = false;
                        this.editorMessage = `${grants.length} 份報表已授權；操作勾選將套用至本次選取的報表。`;
                    } catch (error) {
                        if (requestId === this.editorRequest) {
                            this.editorLoading = false;
                            this.editorMessage = error.message;
                        }
                    }
                },
                closeEditor() {
                    this.$refs.editorDialog.close();
                },
                editorClosed() {
                    this.editorRequest++;
                    this.targetSearchRequest++;
                    clearTimeout(this.targetTimer);
                    this.targetOptions = [];
                },
                targetTypeChanged(value) {
                    this.targetType = Number(value);
                    this.selectedTargetCode = null;
                    this.targetText = "";
                    this.targetOptions = [];
                    this.targetHint = "輸入至少兩個字，並從清單中選取。";
                    this.targetSearchRequest++;
                    clearTimeout(this.targetTimer);
                },
                targetTextChanged(value) {
                    this.targetText = value;
                    this.selectedTargetCode = null;
                    this.targetOptions = [];
                    clearTimeout(this.targetTimer);
                    const requestId = ++this.targetSearchRequest;
                    const search = value.trim();
                    if (search.length < 2) {
                        this.targetHint = "輸入至少兩個字，並從清單中選取。";
                        return;
                    }
                    this.targetHint = "搜尋中…";
                    this.targetTimer = setTimeout(async () => {
                        try {
                            const query = new URLSearchParams({
                                targetType: String(this.targetType), search
                            });
                            const options = await this.getJson("/permissions/targets?" + query,
                                "無法搜尋對象。");
                            if (requestId !== this.targetSearchRequest) return;
                            this.targetOptions = options;
                            this.targetHint = options.length ? "請從結果中選取。" :
                                "找不到符合條件的在職人員或單位。";
                        } catch (error) {
                            if (requestId === this.targetSearchRequest)
                                this.targetHint = error.message;
                        }
                    }, 250);
                },
                chooseTarget(option) {
                    this.selectedTargetCode = option.code;
                    this.targetText = `${option.name} (${option.code})`;
                    this.targetOptions = [];
                    this.targetHint = `已選取 ${option.name}（${option.code}）`;
                    this.targetSearchRequest++;
                    this.$refs.targetInput.focus();
                },
                toggleCodes(codes, checked) {
                    const selected = new Set(this.selectedCodes);
                    for (const code of codes) checked ? selected.add(code) : selected.delete(code);
                    this.selectedCodes = Array.from(selected);
                },
                setActionsFromMask(mask) {
                    this.actions = {
                        query: Boolean(mask & 1), excel: Boolean(mask & 2),
                        print: Boolean(mask & 4), rebuild: Boolean(mask & 8),
                        lineBot: Boolean(mask & 16)
                    };
                },
                setPreset(mask) {
                    this.setActionsFromMask(mask);
                    this.generalActionsChanged = true;
                },
                actionMaskFor(reportCode, baseMask) {
                    const existingMask = this.existingGrants[reportCode];
                    if (this.editingTarget && !this.generalActionsChanged &&
                        Object.hasOwn(this.existingGrants, reportCode)) {
                        return reportCode === "C141" &&
                            this.actions.lineBot !== this.initialLineBotChecked
                            ? (existingMask & ~16) | (this.actions.lineBot ? 16 : 0)
                            : existingMask;
                    }
                    return baseMask | (reportCode === "C141" && this.actions.lineBot ? 16 : 0);
                },
                async saveEditor(event) {
                    event.preventDefault();
                    if (this.saving || this.editorLoading) return;
                    if (!this.selectedTargetCode) {
                        this.editorMessage = "請先從搜尋結果選取授權對象。";
                        this.$refs.targetInput.focus();
                        return;
                    }
                    const codes = this.selectedCodes;
                    if (!codes.length && !Object.keys(this.existingGrants).length) {
                        this.editorMessage = "請選擇至少一個報表。";
                        return;
                    }
                    if (this.actions.lineBot && !codes.includes("C141") &&
                        !Object.hasOwn(this.existingGrants, "C141")) {
                        this.editorMessage = "LineBot 權限只適用 C141；請勾選 C141 報表，或取消 LineBot。";
                        return;
                    }
                    const baseMask = (this.actions.query ? 1 : 0) |
                        (this.actions.excel ? 2 : 0) | (this.actions.print ? 4 : 0) |
                        (this.actions.rebuild ? 8 : 0);
                    const payload = {
                        targetType: this.targetType, target: this.selectedTargetCode,
                        grants: [
                            ...codes.map(reportCode => ({
                                reportCode, actionMask: this.actionMaskFor(reportCode, baseMask),
                                expectedMask: null
                            })),
                            ...Object.keys(this.existingGrants)
                                .filter(reportCode => !codes.includes(reportCode))
                                .map(reportCode => ({
                                    reportCode, actionMask: 0, expectedMask: null, revoke: true
                                }))
                        ]
                    };
                    this.saving = true;
                    this.editorMessage = "儲存中…";
                    try {
                        const preview = await this.postJson("/permissions/changes/preview", payload);
                        const rows = await preview.json();
                        payload.grants.forEach((grant, index) => {
                            grant.expectedMask = rows[index].oldMask;
                        });
                        const changed = rows.some(row => row.operation !== "無變更");
                        if (changed) {
                            await this.postJson("/permissions/changes/apply", payload);
                            this.notifyChange();
                        }
                        this.closeEditor();
                        const loaded = await this.loadList();
                        this.listStatus = loaded
                            ? changed ? "設定已儲存。" : "設定沒有變更。"
                            : "列表重新讀取失敗；您可能已失去權限管理資格。";
                    } catch (error) {
                        this.editorMessage = error.message;
                    } finally {
                        this.saving = false;
                    }
                },
                async openDelete(row) {
                    this.deleteRequest++;
                    const requestId = this.deleteRequest;
                    this.deleteRow = row;
                    this.deletePayload = null;
                    this.deleteApplying = false;
                    this.deleteMessage = "正在讀取權限…";
                    await this.$nextTick();
                    this.$refs.deleteDialog.showModal();
                    try {
                        const query = new URLSearchParams({
                            targetType: String(row.targetType), target: row.target
                        });
                        const grants = await this.getJson("/permissions/grants?" + query, "無法讀取權限。");
                        if (requestId !== this.deleteRequest || !this.$refs.deleteDialog.open) return;
                        if (!grants.length) {
                            this.deleteMessage = "此對象已沒有權限，請重新整理列表。";
                            return;
                        }
                        this.deletePayload = {
                            targetType: row.targetType, target: row.target,
                            expectedMasks: Object.fromEntries(grants.map(grant =>
                                [grant.reportCode, grant.actionMask]))
                        };
                        this.deleteMessage = `將刪除 ${grants.length} 份報表的所有權限。`;
                    } catch (error) {
                        if (requestId === this.deleteRequest) this.deleteMessage = error.message;
                    }
                },
                deleteClosed() {
                    this.deleteRequest++;
                    this.deletePayload = null;
                },
                async applyDelete() {
                    if (!this.deletePayload || this.deleteApplying) return;
                    this.deleteApplying = true;
                    this.deleteMessage = "刪除中…";
                    try {
                        await this.postJson("/permissions/target/delete", this.deletePayload);
                        this.notifyChange();
                        this.deletePayload = null;
                        this.$refs.deleteDialog.close();
                        const loaded = await this.loadList();
                        this.listStatus = loaded ? "此對象的所有權限已刪除。" :
                            "權限已刪除；您可能已失去權限管理資格。";
                    } catch (error) {
                        this.deleteMessage = error.message;
                    } finally {
                        this.deleteApplying = false;
                    }
                },
                async openCopy() {
                    this.copySourceRequest++;
                    this.copyPreviewRequest++;
                    this.copySource = "";
                    this.copyTarget = "";
                    this.copySourceGrants = [];
                    this.copySelectedCodes = [];
                    this.copyPreviewRows = [];
                    this.copyPayload = null;
                    this.copyMessage = "";
                    await this.$nextTick();
                    this.$refs.copyDialog.showModal();
                    this.$refs.copySourceInput.focus();
                },
                copyClosed() {
                    this.copySourceRequest++;
                    this.copyPreviewRequest++;
                },
                invalidateCopyPreview() {
                    this.copyPreviewRequest++;
                    this.copyPreviewRows = [];
                    this.copyPayload = null;
                },
                copySourceChanged(value) {
                    this.copySource = value;
                    this.copySourceRequest++;
                    this.copySourceGrants = [];
                    this.copySelectedCodes = [];
                    this.invalidateCopyPreview();
                },
                async loadCopySource() {
                    const requestId = ++this.copySourceRequest;
                    this.copySourceGrants = [];
                    this.copySelectedCodes = [];
                    this.invalidateCopyPreview();
                    try {
                        const query = new URLSearchParams({
                            targetType: "0", target: this.copySource.trim()
                        });
                        const grants = await this.getJson("/permissions/grants?" + query,
                            "無法讀取來源設定。");
                        if (requestId !== this.copySourceRequest || !this.$refs.copyDialog.open) return;
                        this.copySourceGrants = grants;
                        this.copyMessage = grants.length ? "請選取要複製的報表。" :
                            "來源管理者尚無設定。";
                    } catch (error) {
                        if (requestId === this.copySourceRequest) this.copyMessage = error.message;
                    }
                },
                toggleCopyCode(code, checked) {
                    const codes = new Set(this.copySelectedCodes);
                    checked ? codes.add(code) : codes.delete(code);
                    this.copySelectedCodes = Array.from(codes);
                    this.invalidateCopyPreview();
                },
                async previewCopy() {
                    const requestId = ++this.copyPreviewRequest;
                    this.copyPayload = null;
                    this.copyPreviewRows = [];
                    const payload = {
                        sourceUserId: this.copySource.trim(),
                        targetUserId: this.copyTarget.trim(),
                        reportCodes: this.copySelectedCodes
                    };
                    try {
                        const response = await this.postJson("/permissions/copy/preview", payload);
                        const rows = await response.json();
                        if (requestId !== this.copyPreviewRequest || !this.$refs.copyDialog.open) return;
                        this.copyPreviewRows = rows;
                        payload.previewSourceMasks = Object.fromEntries(
                            rows.map(row => [row.reportCode, row.actionMask]));
                        payload.expectedTargetMasks = Object.fromEntries(
                            rows.map(row => [row.reportCode, row.oldMask]));
                        this.copyPayload = rows.every(row => row.operation === "無變更")
                            ? null : payload;
                        this.copyMessage = this.copyPayload
                            ? "請核對逐筆複製內容後確認。" : "所選權限沒有變更。";
                    } catch (error) {
                        if (requestId === this.copyPreviewRequest) this.copyMessage = error.message;
                    }
                },
                async applyCopy() {
                    if (!this.copyPayload || this.copyApplying) return;
                    this.copyApplying = true;
                    try {
                        await this.postJson("/permissions/copy/apply", this.copyPayload);
                        this.notifyChange();
                        this.copyPayload = null;
                        this.copyPreviewRows = [];
                        this.$refs.copyDialog.close();
                        await this.loadList();
                        this.listStatus = "管理者設定已複製。";
                    } catch (error) {
                        this.copyMessage = error.message;
                        this.copyPayload = null;
                    } finally {
                        this.copyApplying = false;
                    }
                },
                renderToolbar() {
                    return h("div", { class: "permission-toolbar", "aria-label": "權限列表控制列" }, [
                        h("label", { class: "permission-search-label", for: "permission-list-search" }, [
                            "搜尋權限",
                            h("input", {
                                id: "permission-list-search", type: "search",
                                placeholder: "報表名稱、代碼、人員或單位", autocomplete: "off",
                                value: this.listSearch,
                                onInput: event => {
                                    this.listSearch = event.target.value;
                                    this.searchList();
                                }
                            })
                        ]),
                        h("label", { for: "permission-list-category" }, [
                            "篩選模組",
                            h("select", {
                                id: "permission-list-category", value: this.listCategory,
                                onChange: event => {
                                    this.listCategory = event.target.value;
                                    this.listPage = 1;
                                    this.loadList();
                                }
                            }, [
                                h("option", { value: "" }, "全部模組"),
                                ...catalog.map(category =>
                                    h("option", { key: category.key, value: category.key }, category.name))
                            ])
                        ]),
                        button("清除篩選", () => this.clearList(), {
                            id: "permission-list-clear", class: "permission-quiet-button"
                        }),
                        button("＋ 新增權限", () => this.openAdd(), {
                            id: "permission-add", class: "permission-primary-button"
                        }),
                        button("⧉ 複製權限", () => this.openCopy(), {
                            id: "permission-copy-open", class: "permission-primary-button"
                        })
                    ]);
                },
                renderList() {
                    const pageButton = (text, page, id, label) => button(text,
                        () => this.goToPage(page), {
                            id, "aria-label": label,
                            disabled: page < 1 || page > this.listPages || page === this.listPage
                        });
                    return h("section", { class: "permission-list-card",
                        "aria-labelledby": "permission-list-title" }, [
                        h("div", { class: "permission-list-heading" }, [
                            h("div", [
                                h("h2", { id: "permission-list-title" }, "權限列表"),
                                h("p", "依授權對象檢視及維護報表授權。")
                            ]),
                            h("span", { id: "permission-list-total", "aria-live": "polite" },
                                `共 ${this.listTotal} 個授權對象`)
                        ]),
                        h("p", { id: "permission-list-status", role: "status", "aria-live": "polite" },
                            this.listStatus),
                        h("div", { class: "permission-table-scroll" }, [
                            h("table", { class: "permission-table" }, [
                                h("thead", [h("tr", [
                                    h("th", { scope: "col" }, "授權類型"),
                                    h("th", { scope: "col" }, "人員或單位"),
                                    h("th", { scope: "col" }, "已授權報表"),
                                    h("th", { scope: "col" }, "修改人員／時間"),
                                    h("th", { scope: "col" }, "操作")
                                ])]),
                                h("tbody", { id: "permission-list-body" },
                                    this.listItems.map(row => h("tr", {
                                        key: `${row.targetType}:${row.target}`
                                    }, [
                                        h("td", targetNames[row.targetType] || "未知"),
                                        h("td", [
                                            h("strong", row.targetName || row.target),
                                            h("small", row.target)
                                        ]),
                                        h("td", `${row.reportCount} 份報表`),
                                        h("td", [
                                            h("strong", row.modifiedBy || "—"),
                                            h("small", row.modifiedAt
                                                ? new Date(row.modifiedAt).toLocaleString("zh-TW")
                                                : "時間未記錄")
                                        ]),
                                        h("td", { class: "permission-row-actions" }, [
                                            button("✎", () => this.openEditor(row), {
                                                class: "permission-icon-button", title: "編輯權限",
                                                "aria-label": `編輯 ${row.targetName || row.target} 的報表權限`
                                            }),
                                            button("✕", () => this.openDelete(row), {
                                                class: "permission-icon-button permission-danger-button",
                                                title: "刪除所有權限",
                                                "aria-label": `刪除 ${row.targetName || row.target} 的所有報表權限`
                                            })
                                        ])
                                    ])))
                            ])
                        ]),
                        h("div", {
                            class: "pagination permission-list-footer",
                            hidden: this.listTotal === 0
                        }, [
                            h("span", { id: "permission-list-range" },
                                `第 ${this.listPage} / ${this.listPages} 頁`),
                            h("div", { class: "permission-page-buttons" }, [
                                pageButton("‹‹", 1, "permission-list-first", "第一頁"),
                                pageButton("‹", this.listPage - 1, "permission-list-prev", "上一頁"),
                                button(String(this.listPage), () => {}, {
                                    id: "permission-list-page", class: "active",
                                    "aria-current": "page", "aria-label": `第 ${this.listPage} 頁`
                                }),
                                pageButton("›", this.listPage + 1, "permission-list-next", "下一頁"),
                                pageButton("››", this.listPages, "permission-list-last", "最後一頁")
                            ])
                        ])
                    ]);
                },
                renderReportGroups() {
                    return catalog.map(category => {
                        const categoryCodes = category.groups.flatMap(group =>
                            group.reports.map(report => report.code));
                        return h("section", { key: category.key, class: "permission-module" }, [
                            h("h4", category.name),
                            h("div", { class: "permission-module-controls" }, [
                                button("全選", () => this.toggleCodes(categoryCodes, true),
                                    { class: "permission-quiet-button", disabled: this.editorLoading }),
                                button("全不選", () => this.toggleCodes(categoryCodes, false),
                                    { class: "permission-quiet-button", disabled: this.editorLoading })
                            ]),
                            h("div", { class: "permission-module-groups" },
                                category.groups.map(group => {
                                    const groupCodes = group.reports.map(report => report.code);
                                    return h("section", {
                                        key: group.name, class: "permission-report-group"
                                    }, [
                                        h("div", { class: "permission-report-group-heading" }, [
                                            h("h5", group.name),
                                            h("div", { class: "permission-module-controls" }, [
                                                button("全選", () => this.toggleCodes(groupCodes, true),
                                                    { class: "permission-quiet-button",
                                                        disabled: this.editorLoading }),
                                                button("全不選", () => this.toggleCodes(groupCodes, false),
                                                    { class: "permission-quiet-button",
                                                        disabled: this.editorLoading })
                                            ])
                                        ]),
                                        h("div", { class: "permission-module-reports" },
                                            group.reports.map(report =>
                                                h("label", { key: report.code }, [
                                                    checkbox(this.selectedCodes.includes(report.code),
                                                        checked => this.toggleCodes([report.code], checked),
                                                        { value: report.code,
                                                            disabled: this.editorLoading }),
                                                    ` ${report.code} ${report.name}`
                                                ])))
                                    ]);
                                }))
                        ]);
                    });
                },
                renderEditor() {
                    const actionCheck = (key, label) => h("label", [
                        checkbox(this.actions[key], checked => {
                            this.actions[key] = checked;
                            if (key !== "lineBot") this.generalActionsChanged = true;
                        }),
                        ` ${label}`
                    ]);
                    return h("dialog", {
                        ref: "editorDialog", id: "permission-editor-dialog",
                        class: "permission-dialog permission-editor-dialog",
                        "aria-labelledby": "permission-editor-title",
                        onClose: () => this.editorClosed()
                    }, [
                        h("form", { id: "permission-form", onSubmit: event => this.saveEditor(event) }, [
                            h("div", { class: "permission-editor-heading" }, [
                                h("div", [
                                    h("h2", { id: "permission-editor-title" }, this.editorTitle),
                                    h("p", "選擇授權對象、報表及可使用的操作。")
                                ]),
                                button("關閉", () => this.closeEditor(), {
                                    class: "permission-quiet-button", "aria-label": "關閉權限編輯"
                                })
                            ]),
                            h("div", { class: "permission-fields" }, [
                                h("label", [
                                    "授權類型",
                                    h("select", {
                                        value: String(this.targetType), disabled: this.editingTarget,
                                        onChange: event => this.targetTypeChanged(event.target.value)
                                    }, [
                                        h("option", { value: "0" }, "管理者帳號"),
                                        h("option", { value: "1" }, "使用者部門"),
                                        h("option", { value: "2" }, "個人帳號")
                                    ])
                                ]),
                                h("label", [
                                    "授權對象",
                                    h("span", { class: "permission-target-combobox" }, [
                                        h("input", {
                                            ref: "targetInput", maxlength: 100, required: true,
                                            autocomplete: "off", role: "combobox",
                                            "aria-autocomplete": "list",
                                            "aria-expanded": String(this.targetOptions.length > 0),
                                            "aria-controls": "permission-target-options",
                                            value: this.targetText, readOnly: this.editingTarget,
                                            onInput: event => this.targetTextChanged(event.target.value)
                                        }),
                                        h("span", {
                                            id: "permission-target-options",
                                            class: "permission-target-options", role: "listbox",
                                            "aria-label": "授權對象搜尋結果"
                                        }, this.targetOptions.map(option =>
                                            button(`${option.name} (${option.code})`,
                                                () => this.chooseTarget(option), {
                                                    key: option.code,
                                                    class: "permission-target-option",
                                                    role: "option",
                                                    onMousedown: event => event.preventDefault()
                                                })))
                                    ])
                                ])
                            ]),
                            h("p", { class: "permission-hint", role: "status", "aria-live": "polite" },
                                this.targetHint),
                            h("div", { class: "permission-module-heading" }, [
                                h("h3", "選擇報表"),
                                h("p", "依模組或小群勾選；已授權的報表會預先勾選，取消勾選將撤銷授權。")
                            ]),
                            h("div", { class: "permission-module-list" }, this.renderReportGroups()),
                            h("fieldset", { class: "permission-actions" }, [
                                h("legend", "允許的操作"),
                                actionCheck("query", "查詢"),
                                actionCheck("excel", "Excel 匯出"),
                                actionCheck("print", "列印"),
                                actionCheck("rebuild", "重建"),
                                actionCheck("lineBot", "C141 LineBot 發送"),
                                h("small", "C21、C23、C24 等有重建作業的報表適用；C22 櫃員現金表由來源資料即時計算。")
                            ]),
                            h("div", { class: "permission-editor-actions" }, [
                                button("僅讀取範例", () => this.setPreset(1),
                                    { class: "permission-quiet-button" }),
                                button("管理者範例", () => this.setPreset(7),
                                    { class: "permission-quiet-button" }),
                                h("button", { type: "submit", disabled: this.saving }, "儲存")
                            ]),
                            h("p", { role: "status", "aria-live": "polite" }, this.editorMessage)
                        ])
                    ]);
                },
                renderCopy() {
                    return h("dialog", {
                        ref: "copyDialog", id: "permission-copy-dialog",
                        class: "permission-dialog permission-copy-dialog",
                        "aria-labelledby": "permission-copy-title",
                        onClose: () => this.copyClosed()
                    }, [
                        h("div", { class: "permission-editor-heading" }, [
                            h("h2", { id: "permission-copy-title" }, "複製管理者設定"),
                            button("關閉", () => this.$refs.copyDialog.close(),
                                { class: "permission-quiet-button", "aria-label": "關閉複製權限" })
                        ]),
                        h("p", "從另一位管理者複製選取的報表授權；儲存前會列出新增與移除的操作。"),
                        h("div", { class: "permission-fields" }, [
                            h("label", ["來源管理者帳號", h("input", {
                                ref: "copySourceInput", maxlength: 10, autocomplete: "off",
                                value: this.copySource,
                                onInput: event => this.copySourceChanged(event.target.value)
                            })]),
                            h("label", ["目標管理者帳號", h("input", {
                                maxlength: 10, autocomplete: "off", value: this.copyTarget,
                                onInput: event => {
                                    this.copyTarget = event.target.value;
                                    this.invalidateCopyPreview();
                                }
                            })]),
                            button("讀取來源設定", () => this.loadCopySource())
                        ]),
                        h("div", { id: "permission-copy-options" },
                            this.copySourceGrants.map(grant =>
                                h("label", { key: grant.reportCode }, [
                                    checkbox(this.copySelectedCodes.includes(grant.reportCode),
                                        checked => this.toggleCopyCode(grant.reportCode, checked)),
                                    ` ${definitions.get(grant.reportCode)?.name || grant.reportCode} (${grant.reportCode}) · ${actionSummary(grant.actionMask)}`
                                ]))),
                        h("div", { class: "permission-fields" }, [
                            button("預覽複製", () => this.previewCopy()),
                            button("確認複製", () => this.applyCopy(),
                                { disabled: !this.copyPayload || this.copyApplying })
                        ]),
                        h("ul", this.copyPreviewRows.map(row =>
                            h("li", { key: row.reportCode },
                                `${row.reportCode}：${row.operation}。新增 ${actionSummary(row.addedMask)}；移除 ${actionSummary(row.removedMask)}。`))),
                        h("p", { role: "status", "aria-live": "polite" }, this.copyMessage)
                    ]);
                },
                renderDelete() {
                    return h("dialog", {
                        ref: "deleteDialog", id: "permission-delete-dialog",
                        class: "permission-dialog",
                        "aria-labelledby": "permission-delete-title",
                        onClose: () => this.deleteClosed()
                    }, [
                        h("h2", { id: "permission-delete-title" }, "刪除所有權限？"),
                        h("p", this.deleteRow
                            ? `${this.deleteRow.targetName || this.deleteRow.target}（${this.deleteRow.target}）`
                            : ""),
                        h("p", "確認後將刪除此對象的所有報表權限。"),
                        this.deleteRow?.targetType === 0
                            ? h("p", { class: "permission-revoke-warning" },
                                "若這是本人最後一筆管理者授權，刪除後將無法再進入權限管理。")
                            : null,
                        h("p", { role: "status", "aria-live": "polite" }, this.deleteMessage),
                        h("div", { class: "permission-dialog-actions" }, [
                            button("取消", () => this.$refs.deleteDialog.close(),
                                { class: "permission-quiet-button" }),
                            button("確認刪除", () => this.applyDelete(), {
                                class: "permission-delete-button",
                                disabled: !this.deletePayload || this.deleteApplying
                            })
                        ])
                    ]);
                }
            },
            render() {
                return h("div", { class: "permission-page" }, [
                    h("div", { class: "permission-header" }, [
                        h("div", [
                            h("p", { class: "permission-kicker" }, "門急診報表作業"),
                            h("h1", "設定權限"),
                            h("p", "選擇授權對象與報表，再調整可使用的操作。")
                        ])
                    ]),
                    this.renderToolbar(),
                    this.renderList(),
                    this.renderEditor(),
                    this.renderCopy(),
                    this.renderDelete()
                ]);
            }
        });
        root.__permissionApp = app;
        app.mount(root);
    };

    if (document.getElementById("permission-panel-root"))
        window.initializeReportPermissions();
})();
