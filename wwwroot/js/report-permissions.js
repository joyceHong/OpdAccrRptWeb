window.initializeReportPermissions = () => {
    "use strict";

    const catalogElement = document.getElementById("permission-catalog");
    if (!catalogElement) return;
    const catalog = JSON.parse(catalogElement.textContent);
    const form = document.getElementById("permission-form");
    const editorDialog = document.getElementById("permission-editor-dialog");
    const copyDialog = document.getElementById("permission-copy-dialog");
    const deleteDialog = document.getElementById("permission-delete-dialog");
    const targetType = document.getElementById("permission-target-type");
    const target = document.getElementById("permission-target");
    const copySource = document.getElementById("permission-copy-source");
    const copyTarget = document.getElementById("permission-copy-target");
    const copyOptions = document.getElementById("permission-copy-options");
    const copyChanges = document.getElementById("permission-copy-changes");
    const copyApply = document.getElementById("permission-copy-apply");
    const copyMessage = document.getElementById("permission-copy-message");
    const deleteMessage = document.getElementById("permission-delete-message");
    const deleteConfirm = document.getElementById("permission-delete-confirm");
    const saveButton = document.getElementById("permission-save");
    const message = document.getElementById("permission-message");
    const targetOptions = document.getElementById("permission-target-options");
    const targetHint = document.getElementById("permission-target-hint");
    const moduleList = document.getElementById("permission-module-list");
    const listSearch = document.getElementById("permission-list-search");
    const listCategory = document.getElementById("permission-list-category");
    const listBody = document.getElementById("permission-list-body");
    const listStatus = document.getElementById("permission-list-status");
    const listTotal = document.getElementById("permission-list-total");
    const listRange = document.getElementById("permission-list-range");
    const listPage = document.getElementById("permission-list-page");
    const listFooter = document.querySelector(".permission-list-footer");
    const firstPage = document.getElementById("permission-list-first");
    const previousPage = document.getElementById("permission-list-prev");
    const nextPage = document.getElementById("permission-list-next");
    const finalPage = document.getElementById("permission-list-last");
    const checks = {
        query: document.getElementById("permission-query"),
        excel: document.getElementById("permission-excel"),
        print: document.getElementById("permission-print"),
        rebuild: document.getElementById("permission-rebuild")
    };
    let copyPayload = null;
    let copySourceRequest = 0;
    let copyPreviewRequest = 0;
    let deleteRequest = 0;
    let currentPage = 1;
    let totalPages = 1;
    let listRequest = 0;
    let selectedTargetCode = null;
    let targetSearchTimer;
    let targetSearchRequest = 0;
    let pendingDeleteRequest = null;
    let existingGrants = new Map();
    let editingTarget = false;
    let editorLoading = false;
    let actionsChanged = false;
    let editorRequest = 0;
    Object.values(checks).forEach(input => input.addEventListener("change", () => {
        actionsChanged = true;
    }));

    function actionSummary(mask) {
        return [[1, "查詢"], [2, "Excel"], [4, "列印"], [8, "重建"]]
            .filter(([bit]) => Boolean(mask & bit)).map(([, name]) => name).join("、") || "無操作";
    }

    function targetTypeName(value) {
        return { 0: "管理者", 1: "單位", 2: "個人" }[value] || "未知";
    }

    function renderList(page) {
        listBody.replaceChildren();
        listTotal.textContent = `共 ${page.totalCount} 個授權對象`;
        totalPages = Math.max(1, Math.ceil(page.totalCount / page.pageSize));
        listFooter.hidden = page.totalCount === 0;
        listRange.textContent = `第 ${page.page} / ${totalPages} 頁`;
        listPage.textContent = String(page.page);
        listPage.setAttribute("aria-label", `第 ${page.page} 頁`);
        firstPage.disabled = page.page <= 1;
        previousPage.disabled = page.page <= 1;
        nextPage.disabled = page.page >= totalPages;
        finalPage.disabled = page.page >= totalPages;
        if (!page.items.length) {
            listStatus.textContent = "找不到符合條件的授權，可清除篩選或新增權限。";
            return;
        }
        listStatus.textContent = "";
        for (const row of page.items) {
            const tr = document.createElement("tr");
            const type = document.createElement("td");
            type.textContent = targetTypeName(row.targetType);
            const person = document.createElement("td");
            const personName = document.createElement("strong");
            personName.textContent = row.targetName || row.target;
            const personCode = document.createElement("small");
            personCode.textContent = row.target;
            person.append(personName, personCode);
            const count = document.createElement("td");
            count.textContent = `${row.reportCount} 份報表`;
            const actor = document.createElement("td");
            const actorName = document.createElement("strong");
            actorName.textContent = row.modifiedBy || "—";
            const modifiedAt = document.createElement("small");
            modifiedAt.textContent = row.modifiedAt
                ? new Date(row.modifiedAt).toLocaleString("zh-TW") : "時間未記錄";
            actor.append(actorName, modifiedAt);
            const actions = document.createElement("td");
            actions.className = "permission-row-actions";
            const edit = document.createElement("button");
            edit.type = "button";
            edit.className = "permission-icon-button";
            edit.textContent = "✎";
            edit.title = "編輯權限";
            edit.setAttribute("aria-label", `編輯 ${row.targetName || row.target} 的報表權限`);
            edit.addEventListener("click", () => openEditor(row));
            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "permission-icon-button permission-danger-button";
            remove.textContent = "✕";
            remove.title = "刪除所有權限";
            remove.setAttribute("aria-label", `刪除 ${row.targetName || row.target} 的所有報表權限`);
            remove.addEventListener("click", () => openDelete(row));
            actions.append(edit, remove);
            tr.append(type, person, count, actor, actions);
            listBody.appendChild(tr);
        }
    }

    async function loadList() {
        const requestId = ++listRequest;
        listStatus.textContent = "載入中…";
        const query = new URLSearchParams({ page: String(currentPage), pageSize: "20" });
        if (listSearch.value.trim()) query.set("search", listSearch.value.trim());
        if (listCategory.value) query.set("categoryKey", listCategory.value);
        try {
            const response = await fetch("/permissions/target-list?" + query, { credentials: "same-origin" });
            if (!response.ok) throw new Error(await response.text() || "無法讀取權限列表。");
            const result = await response.json();
            if (requestId === listRequest && !result.items.length && result.totalCount > 0 && currentPage > 1) {
                currentPage = Math.ceil(result.totalCount / result.pageSize);
                return await loadList();
            }
            if (requestId === listRequest) renderList(result);
            return true;
        } catch (error) {
            if (requestId === listRequest) {
                listBody.replaceChildren();
                listStatus.textContent = error.message;
                listTotal.textContent = "";
                listRange.textContent = "";
                listFooter.hidden = true;
            }
            return false;
        }
    }

    let searchTimer;
    listSearch.addEventListener("input", () => {
        clearTimeout(searchTimer);
        currentPage = 1;
        searchTimer = setTimeout(loadList, 250);
    });
    listCategory.addEventListener("change", () => {
        currentPage = 1;
        loadList();
    });
    document.getElementById("permission-list-clear").addEventListener("click", () => {
        listSearch.value = "";
        listCategory.value = "";
        currentPage = 1;
        loadList();
    });
    firstPage.addEventListener("click", () => {
        if (currentPage > 1) { currentPage = 1; loadList(); }
    });
    previousPage.addEventListener("click", () => {
        if (currentPage > 1) { currentPage--; loadList(); }
    });
    nextPage.addEventListener("click", () => {
        if (currentPage < totalPages) { currentPage++; loadList(); }
    });
    finalPage.addEventListener("click", () => {
        if (currentPage < totalPages) { currentPage = totalPages; loadList(); }
    });
    document.getElementById("permission-add").addEventListener("click", () => {
        editingTarget = false;
        editorLoading = false;
        existingGrants = new Map();
        actionsChanged = false;
        selectedTargetCode = null;
        targetType.value = "0";
        targetType.disabled = false;
        target.readOnly = false;
        target.value = "";
        targetOptions.replaceChildren();
        target.setAttribute("aria-expanded", "false");
        message.textContent = "";
        targetHint.textContent = "輸入至少兩個字，並從清單中選取。";
        document.getElementById("permission-editor-title").textContent = "新增權限";
        moduleList.querySelectorAll('input[type="checkbox"]').forEach(input => {
            input.checked = false;
            input.disabled = false;
        });
        applyMask(7);
        editorDialog.showModal();
        target.focus();
    });
    document.getElementById("permission-editor-close").addEventListener("click", () => {
        editorDialog.close();
    });
    editorDialog.addEventListener("close", () => {
        editorRequest++;
        targetSearchRequest++;
        clearTimeout(targetSearchTimer);
        targetOptions.replaceChildren();
        target.setAttribute("aria-expanded", "false");
    });

    function chooseTarget(option) {
        selectedTargetCode = option.code;
        target.value = `${option.name} (${option.code})`;
        targetOptions.replaceChildren();
        target.setAttribute("aria-expanded", "false");
        targetHint.textContent = `已選取 ${option.name}（${option.code}）`;
        target.focus();
    }

    targetType.addEventListener("change", () => {
        targetSearchRequest++;
        clearTimeout(targetSearchTimer);
        selectedTargetCode = null;
        target.value = "";
        targetOptions.replaceChildren();
        targetHint.textContent = "輸入至少兩個字，並從清單中選取。";
    });
    target.addEventListener("input", () => {
        selectedTargetCode = null;
        clearTimeout(targetSearchTimer);
        const requestId = ++targetSearchRequest;
        targetOptions.replaceChildren();
        const search = target.value.trim();
        if (search.length < 2) {
            targetHint.textContent = "輸入至少兩個字，並從清單中選取。";
            target.setAttribute("aria-expanded", "false");
            return;
        }
        targetHint.textContent = "搜尋中…";
        targetSearchTimer = setTimeout(async () => {
            try {
                const query = new URLSearchParams({ targetType: targetType.value, search });
                const response = await fetch("/permissions/targets?" + query, { credentials: "same-origin" });
                if (!response.ok) throw new Error(await response.text() || "無法搜尋對象。");
                const options = await response.json();
                if (requestId !== targetSearchRequest) return;
                targetOptions.replaceChildren();
                for (const option of options) {
                    const button = document.createElement("button");
                    button.type = "button";
                    button.className = "permission-target-option";
                    button.setAttribute("role", "option");
                    button.textContent = `${option.name} (${option.code})`;
                    button.addEventListener("mousedown", event => event.preventDefault());
                    button.addEventListener("click", () => chooseTarget(option));
                    targetOptions.appendChild(button);
                }
                target.setAttribute("aria-expanded", String(options.length > 0));
                targetHint.textContent = options.length ? "請從結果中選取。" : "找不到符合條件的在職人員或單位。";
            } catch (error) {
                if (requestId === targetSearchRequest) targetHint.textContent = error.message;
            }
        }, 250);
    });

    for (const category of catalog) {
        const module = document.createElement("section");
        module.className = "permission-module";
        const heading = document.createElement("h4");
        heading.textContent = category.name;
        const controls = document.createElement("div");
        controls.className = "permission-module-controls";
        const selectAll = document.createElement("button");
        selectAll.type = "button";
        selectAll.className = "permission-quiet-button";
        selectAll.textContent = "全選";
        const selectNone = document.createElement("button");
        selectNone.type = "button";
        selectNone.className = "permission-quiet-button";
        selectNone.textContent = "全不選";
        controls.append(selectAll, selectNone);
        const moduleReports = document.createElement("div");
        moduleReports.className = "permission-module-groups";
        for (const group of category.groups) {
            const groupSection = document.createElement("section");
            groupSection.className = "permission-report-group";
            const groupHeading = document.createElement("div");
            groupHeading.className = "permission-report-group-heading";
            const groupTitle = document.createElement("h5");
            groupTitle.textContent = group.name;
            const groupActions = document.createElement("div");
            groupActions.className = "permission-module-controls";
            for (const [caption, checked] of [["全選", true], ["全不選", false]]) {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "permission-quiet-button";
                button.textContent = caption;
                button.addEventListener("click", () => {
                    groupSection.querySelectorAll('input[type="checkbox"]:not(:disabled)').forEach(input => {
                        input.checked = checked;
                    });
                });
                groupActions.appendChild(button);
            }
            groupHeading.append(groupTitle, groupActions);
            const reports = document.createElement("div");
            reports.className = "permission-module-reports";
            for (const item of group.reports) {
                const label = document.createElement("label");
                const checkbox = document.createElement("input");
                checkbox.type = "checkbox";
                checkbox.value = item.code;
                label.append(checkbox, document.createTextNode(` ${item.code} ${item.name}`));
                reports.appendChild(label);
            }
            groupSection.append(groupHeading, reports);
            moduleReports.appendChild(groupSection);
        }
        selectAll.addEventListener("click", () => {
            moduleReports.querySelectorAll("input:not(:disabled)")
                .forEach(input => { input.checked = true; });
        });
        selectNone.addEventListener("click", () => {
            moduleReports.querySelectorAll("input:not(:disabled)")
                .forEach(input => { input.checked = false; });
        });
        module.append(heading, controls, moduleReports);
        moduleList.appendChild(module);
    }

    document.getElementById("permission-preset-read").addEventListener("click", () => {
        applyMask(1);
        actionsChanged = true;
    });
    document.getElementById("permission-preset-manager").addEventListener("click", () => {
        applyMask(7);
        actionsChanged = true;
    });

    function applyMask(mask) {
        checks.query.checked = Boolean(mask & 1);
        checks.excel.checked = Boolean(mask & 2);
        checks.print.checked = Boolean(mask & 4);
        checks.rebuild.checked = Boolean(mask & 8);
    }

    function selectedActions() {
        return {
            query: checks.query.checked,
            excel: checks.excel.checked,
            print: checks.print.checked,
            rebuild: checks.rebuild.checked
        };
    }

    function antiforgeryToken() {
        return form.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    async function postJson(path, payload) {
        const response = await fetch(path, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": antiforgeryToken()
            },
            body: JSON.stringify(payload)
        });
        if (!response.ok) throw new Error(await response.text() || "操作失敗。");
        return response;
    }

    function notifyPermissionChange() {
        window.dispatchEvent(new Event("report-permissions-changed"));
    }

    async function openEditor(row) {
        const requestId = ++editorRequest;
        editingTarget = true;
        editorLoading = true;
        existingGrants = new Map();
        actionsChanged = false;
        selectedTargetCode = row.target;
        targetType.value = String(row.targetType);
        targetType.disabled = true;
        target.value = `${row.targetName || row.target} (${row.target})`;
        target.readOnly = true;
        targetOptions.replaceChildren();
        targetHint.textContent = "編輯既有對象時，授權類型及對象固定。";
        document.getElementById("permission-editor-title").textContent = "修改權限";
        moduleList.querySelectorAll('input[type="checkbox"]').forEach(input => {
            input.checked = false;
            input.disabled = true;
        });
        applyMask(7);
        message.textContent = "正在載入此對象的報表權限…";
        editorDialog.showModal();
        try {
            const query = new URLSearchParams({
                targetType: String(row.targetType), target: row.target
            });
            const response = await fetch("/permissions/grants?" + query, { credentials: "same-origin" });
            if (!response.ok) throw new Error(await response.text() || "無法讀取授權。");
            const grants = await response.json();
            if (!editorDialog.open || requestId !== editorRequest) return;
            const reportCodes = new Set(Array.from(
                moduleList.querySelectorAll('input[type="checkbox"]'), input => input.value));
            existingGrants = new Map(grants
                .filter(grant => reportCodes.has(grant.reportCode))
                .map(grant => [grant.reportCode, grant.actionMask]));
            moduleList.querySelectorAll('input[type="checkbox"]').forEach(input => {
                input.checked = existingGrants.has(input.value);
                input.disabled = false;
            });
            const first = grants.find(grant => grant.actionMask > 0);
            if (first) applyMask(first.actionMask);
            editorLoading = false;
            message.textContent = `${grants.length} 份報表已授權；操作勾選將套用至本次選取的報表。`;
        } catch (error) {
            if (requestId !== editorRequest) return;
            editorLoading = false;
            message.textContent = error.message;
            moduleList.querySelectorAll('input[type="checkbox"]').forEach(input => {
                input.disabled = true;
            });
        }
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (saveButton.disabled) return;
        if (editorLoading) {
            message.textContent = "請等候現有授權載入完成。";
            return;
        }
        if (!selectedTargetCode) {
            message.textContent = "請先從搜尋結果選取授權對象。";
            target.focus();
            return;
        }
        const codes = Array.from(moduleList.querySelectorAll('input[type="checkbox"]:checked'),
            input => input.value);
        if (!codes.length && !existingGrants.size) {
            message.textContent = "請選擇至少一個報表。";
            return;
        }
        const actions = selectedActions();
        const mask = (actions.query ? 1 : 0) | (actions.excel ? 2 : 0) |
            (actions.print ? 4 : 0) | (actions.rebuild ? 8 : 0);
        const payload = {
            targetType: Number(targetType.value), target: selectedTargetCode,
            grants: [
                ...codes.map(reportCode => ({ reportCode,
                    actionMask: editingTarget && !actionsChanged && existingGrants.has(reportCode)
                        ? existingGrants.get(reportCode) : mask,
                    expectedMask: null })),
                ...Array.from(existingGrants.keys())
                    .filter(reportCode => !codes.includes(reportCode))
                    .map(reportCode => ({ reportCode, actionMask: 0, expectedMask: null, revoke: true }))
            ]
        };
        saveButton.disabled = true;
        message.textContent = "儲存中…";
        try {
            const response = await postJson("/permissions/changes/preview", payload);
            const rows = await response.json();
            payload.grants.forEach((grant, index) => { grant.expectedMask = rows[index].oldMask; });
            const hasChanges = rows.some(row => row.operation !== "無變更");
            if (hasChanges) {
                await postJson("/permissions/changes/apply", payload);
                notifyPermissionChange();
            }
            editorDialog.close();
            const loaded = await loadList();
            listStatus.textContent = loaded
                ? hasChanges ? "設定已儲存。" : "設定沒有變更。"
                : "列表重新讀取失敗；您可能已失去權限管理資格。";
        } catch (error) {
            message.textContent = error.message;
        } finally {
            saveButton.disabled = false;
        }
    });

    async function openDelete(row) {
        const requestId = ++deleteRequest;
        pendingDeleteRequest = null;
        deleteConfirm.disabled = true;
        deleteMessage.textContent = "正在讀取權限…";
        document.getElementById("permission-delete-description").textContent =
            `${row.targetName || row.target}（${row.target}）`;
        document.getElementById("permission-delete-warning").hidden = row.targetType !== 0;
        deleteDialog.showModal();
        try {
            const query = new URLSearchParams({
                targetType: String(row.targetType), target: row.target
            });
            const response = await fetch("/permissions/grants?" + query, { credentials: "same-origin" });
            if (!response.ok) throw new Error(await response.text() || "無法讀取權限。");
            const grants = await response.json();
            if (!deleteDialog.open || requestId !== deleteRequest) return;
            if (!grants.length) {
                deleteMessage.textContent = "此對象已沒有權限，請重新整理列表。";
                return;
            }
            pendingDeleteRequest = {
                targetType: row.targetType, target: row.target,
                expectedMasks: Object.fromEntries(grants.map(grant =>
                    [grant.reportCode, grant.actionMask]))
            };
            deleteMessage.textContent = `將刪除 ${grants.length} 份報表的所有權限。`;
            deleteConfirm.disabled = false;
        } catch (error) {
            if (deleteDialog.open && requestId === deleteRequest)
                deleteMessage.textContent = error.message;
        }
    }

    document.getElementById("permission-delete-cancel").addEventListener("click", () =>
        deleteDialog.close());
    deleteDialog.addEventListener("close", () => {
        deleteRequest++;
        pendingDeleteRequest = null;
    });
    deleteConfirm.addEventListener("click", async () => {
        if (!pendingDeleteRequest) return;
        deleteConfirm.disabled = true;
        deleteMessage.textContent = "刪除中…";
        try {
            await postJson("/permissions/target/delete", pendingDeleteRequest);
            notifyPermissionChange();
            pendingDeleteRequest = null;
            deleteDialog.close();
            const loaded = await loadList();
            listStatus.textContent = loaded
                ? "此對象的所有權限已刪除。"
                : "權限已刪除；您可能已失去權限管理資格。";
        } catch (error) {
            deleteMessage.textContent = error.message;
        } finally {
            if (deleteDialog.open) deleteConfirm.disabled = false;
        }
    });

    document.getElementById("permission-copy-open").addEventListener("click", () => {
        copySourceRequest++;
        copyPreviewRequest++;
        copySource.value = "";
        copyTarget.value = "";
        copyOptions.replaceChildren();
        copyChanges.replaceChildren();
        copyPayload = null;
        copyApply.disabled = true;
        copyMessage.textContent = "";
        copyDialog.showModal();
        copySource.focus();
    });
    document.getElementById("permission-copy-close").addEventListener("click", () =>
        copyDialog.close());
    copyDialog.addEventListener("close", () => {
        copySourceRequest++;
        copyPreviewRequest++;
    });

    document.getElementById("permission-copy-load").addEventListener("click", async () => {
        const requestId = ++copySourceRequest;
        copyPayload = null;
        copyApply.disabled = true;
        copyOptions.replaceChildren();
        copyChanges.replaceChildren();
        const query = new URLSearchParams({ targetType: "0", target: copySource.value.trim() });
        try {
            const response = await fetch("/permissions/grants?" + query, { credentials: "same-origin" });
            if (!response.ok) throw new Error(await response.text() || "無法讀取來源設定。");
            const sourceGrants = await response.json();
            if (!copyDialog.open || requestId !== copySourceRequest) return;
            for (const grant of sourceGrants) {
                const label = document.createElement("label");
                const checkbox = document.createElement("input");
                checkbox.type = "checkbox";
                checkbox.value = grant.reportCode;
                const definition = catalog.flatMap(category => category.groups)
                    .flatMap(group => group.reports).find(report => report.code === grant.reportCode);
                label.append(checkbox, document.createTextNode(
                    ` ${definition?.name || grant.reportCode} (${grant.reportCode}) · ${actionSummary(grant.actionMask)}`));
                copyOptions.appendChild(label);
            }
            copyMessage.textContent = sourceGrants.length ? "請選取要複製的報表。" : "來源管理者尚無設定。";
        } catch (error) {
            if (copyDialog.open && requestId === copySourceRequest)
                copyMessage.textContent = error.message;
        }
    });

    function invalidateCopyPreview() {
        copyPreviewRequest++;
        copyPayload = null;
        copyApply.disabled = true;
        copyChanges.replaceChildren();
    }
    copySource.addEventListener("input", () => {
        copySourceRequest++;
        copyOptions.replaceChildren();
        invalidateCopyPreview();
    });
    copyTarget.addEventListener("input", invalidateCopyPreview);
    copyOptions.addEventListener("change", invalidateCopyPreview);

    document.getElementById("permission-copy-preview").addEventListener("click", async () => {
        const requestId = ++copyPreviewRequest;
        copyPayload = null;
        copyApply.disabled = true;
        copyChanges.replaceChildren();
        const payload = {
            sourceUserId: copySource.value.trim(),
            targetUserId: copyTarget.value.trim(),
            reportCodes: Array.from(copyOptions.querySelectorAll('input:checked'), input => input.value)
        };
        try {
            const response = await postJson("/permissions/copy/preview", payload);
            const rows = await response.json();
            if (!copyDialog.open || requestId !== copyPreviewRequest) return;
            for (const row of rows) {
                const item = document.createElement("li");
                item.textContent = `${row.reportCode}：${row.operation}。` +
                    `新增 ${actionSummary(row.addedMask)}；移除 ${actionSummary(row.removedMask)}。`;
                copyChanges.appendChild(item);
            }
            payload.previewSourceMasks = Object.fromEntries(rows.map(row => [row.reportCode, row.actionMask]));
            payload.expectedTargetMasks = Object.fromEntries(rows.map(row => [row.reportCode, row.oldMask]));
            copyPayload = payload;
            copyApply.disabled = rows.every(row => row.operation === "無變更");
            copyMessage.textContent = copyApply.disabled ? "所選權限沒有變更。" : "請核對逐筆複製內容後確認。";
        } catch (error) {
            if (copyDialog.open && requestId === copyPreviewRequest)
                copyMessage.textContent = error.message;
        }
    });

    copyApply.addEventListener("click", async () => {
        if (!copyPayload) return;
        copyApply.disabled = true;
        try {
            await postJson("/permissions/copy/apply", copyPayload);
            notifyPermissionChange();
            copyPayload = null;
            copyApply.disabled = true;
            copyChanges.replaceChildren();
            copyDialog.close();
            await loadList();
            listStatus.textContent = "管理者設定已複製。";
        } catch (error) {
            copyMessage.textContent = error.message;
            copyPayload = null;
            copyApply.disabled = true;
        }
    });

    loadList();
};

if (document.getElementById("permission-form")) window.initializeReportPermissions();
