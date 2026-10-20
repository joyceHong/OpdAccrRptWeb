window.initializeReportPermissions = () => {
    "use strict";

    const catalogElement = document.getElementById("permission-catalog");
    if (!catalogElement) return;
    const catalog = JSON.parse(catalogElement.textContent);
    const form = document.getElementById("permission-form");
    const targetType = document.getElementById("permission-target-type");
    const target = document.getElementById("permission-target");
    const report = document.getElementById("permission-report");
    const batchReports = document.getElementById("permission-batch-reports");
    const batchChanges = document.getElementById("permission-batch-changes");
    const batchApply = document.getElementById("permission-batch-apply");
    const copySource = document.getElementById("permission-copy-source");
    const copyTarget = document.getElementById("permission-copy-target");
    const copyOptions = document.getElementById("permission-copy-options");
    const copyChanges = document.getElementById("permission-copy-changes");
    const copyApply = document.getElementById("permission-copy-apply");
    const existing = document.getElementById("permission-existing");
    const message = document.getElementById("permission-message");
    const checks = {
        query: document.getElementById("permission-query"),
        excel: document.getElementById("permission-excel"),
        print: document.getElementById("permission-print"),
        rebuild: document.getElementById("permission-rebuild")
    };
    let grants = [];
    let previewPayload = null;
    let copyPayload = null;

    for (const category of catalog) {
        const group = document.createElement("optgroup");
        group.label = category.name;
        for (const item of category.groups.flatMap(entry => entry.reports)) {
            const option = document.createElement("option");
            option.value = item.code;
            option.textContent = item.code + " " + item.name;
            group.appendChild(option);
            const batchOption = document.createElement("option");
            batchOption.value = item.code;
            batchOption.textContent = item.code + " " + item.name;
            batchReports.appendChild(batchOption);
        }
        report.appendChild(group);
    }

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

    function showExisting() {
        existing.replaceChildren();
        for (const grant of grants) {
            const row = document.createElement("li");
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = grant.reportCode + " · 權限值 " + grant.actionMask;
            button.addEventListener("click", () => {
                report.value = grant.reportCode;
                applyMask(grant.actionMask);
            });
            row.appendChild(button);
            existing.appendChild(row);
        }
    }

    async function loadGrants() {
        if (!target.value.trim()) {
            message.textContent = "請先輸入授權對象。";
            return;
        }
        const query = new URLSearchParams({ targetType: targetType.value, target: target.value.trim() });
        const response = await fetch("/permissions/grants?" + query, { credentials: "same-origin" });
        if (!response.ok) throw new Error(await response.text() || "無法讀取設定。");
        grants = await response.json();
        showExisting();
        message.textContent = grants.length ? "已載入既有設定。" : "尚無設定。";
    }

    document.getElementById("permission-load").addEventListener("click", async () => {
        try {
            await loadGrants();
        } catch (error) {
            message.textContent = error.message;
        }
    });

    report.addEventListener("change", () => {
        const grant = grants.find(item => item.reportCode === report.value);
        applyMask(grant ? grant.actionMask : 7);
    });

    form.addEventListener("submit", async event => {
        event.preventDefault();
        const payload = {
            targetType: Number(targetType.value),
            target: target.value.trim(),
            reportCode: report.value,
            ...selectedActions()
        };
        const actionNames = Object.entries(checks)
            .filter(([, checkbox]) => checkbox.checked)
            .map(([name]) => ({ query: "查詢", excel: "Excel", print: "列印", rebuild: "重建" })[name]);
        const summary = [
            "授權對象：" + payload.target,
            "報表：" + report.selectedOptions[0]?.textContent,
            "操作：" + (actionNames.join("、") || "無")
        ].join("\n");
        if (!window.confirm(summary + "\n\n確認儲存？")) return;
        try {
            await postJson("/permissions/grants", payload);
            await loadGrants();
            message.textContent = "設定已儲存。";
        } catch (error) {
            message.textContent = error.message;
        }
    });

    document.getElementById("permission-batch-preview").addEventListener("click", async () => {
        previewPayload = null;
        batchApply.disabled = true;
        batchChanges.replaceChildren();
        const payload = {
            targetType: Number(targetType.value),
            target: target.value.trim(),
            reportCodes: Array.from(batchReports.selectedOptions, option => option.value),
            ...selectedActions()
        };
        try {
            const response = await postJson("/permissions/batch/preview", payload);
            const rows = await response.json();
            for (const row of rows) {
                const item = document.createElement("li");
                item.textContent = row.reportCode + "：" + row.operation + "，權限值 " + row.actionMask;
                batchChanges.appendChild(item);
            }
            previewPayload = payload;
            batchApply.disabled = false;
            message.textContent = "請核對逐筆異動後確認儲存。";
        } catch (error) {
            message.textContent = error.message;
        }
    });

    batchApply.addEventListener("click", async () => {
        if (!previewPayload) return;
        try {
            await postJson("/permissions/batch/apply", previewPayload);
            previewPayload = null;
            batchApply.disabled = true;
            batchChanges.replaceChildren();
            await loadGrants();
            message.textContent = "批次設定已儲存。";
        } catch (error) {
            message.textContent = error.message;
        }
    });

    document.getElementById("permission-copy-load").addEventListener("click", async () => {
        copyPayload = null;
        copyApply.disabled = true;
        copyOptions.replaceChildren();
        copyChanges.replaceChildren();
        const query = new URLSearchParams({ targetType: "0", target: copySource.value.trim() });
        try {
            const response = await fetch("/permissions/grants?" + query, { credentials: "same-origin" });
            if (!response.ok) throw new Error(await response.text() || "無法讀取來源設定。");
            const sourceGrants = await response.json();
            for (const grant of sourceGrants) {
                const label = document.createElement("label");
                const checkbox = document.createElement("input");
                checkbox.type = "checkbox";
                checkbox.value = grant.reportCode;
                label.append(checkbox, document.createTextNode(
                    " " + grant.reportCode + " · 權限值 " + grant.actionMask));
                copyOptions.appendChild(label);
            }
            message.textContent = sourceGrants.length ? "請選取要複製的報表。" : "來源管理者尚無設定。";
        } catch (error) {
            message.textContent = error.message;
        }
    });

    document.getElementById("permission-copy-preview").addEventListener("click", async () => {
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
            for (const row of rows) {
                const item = document.createElement("li");
                item.textContent = row.reportCode + "：" + row.operation + "，權限值 " + row.actionMask;
                copyChanges.appendChild(item);
            }
            copyPayload = payload;
            copyApply.disabled = false;
            message.textContent = "請核對逐筆複製內容後確認。";
        } catch (error) {
            message.textContent = error.message;
        }
    });

    copyApply.addEventListener("click", async () => {
        if (!copyPayload) return;
        try {
            await postJson("/permissions/copy/apply", copyPayload);
            copyPayload = null;
            copyApply.disabled = true;
            copyChanges.replaceChildren();
            message.textContent = "管理者設定已複製。";
        } catch (error) {
            message.textContent = error.message;
        }
    });
};

if (document.getElementById("permission-form")) window.initializeReportPermissions();
