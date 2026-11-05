(() => {
    const eligibleInput = input => input instanceof HTMLInputElement
        && ["text", "search", "number", "date", "month", "time", "email", "tel", "url"].includes(input.type)
        && !input.closest(".c5-clearable-input")
        && (input.closest(".advanced-filter-region, .c22-filter-dialog")
            || input.closest(".query-panel")?.querySelector(".advanced-filter-toggle, .c22-filter-button"));

    const clearableInputsDirective = {
        mounted(element) {
            const buttons = new WeakMap();
            const synchronize = () => {
                for (const input of element.querySelectorAll(".query-panel input, .c22-filter-dialog input")) {
                    if (!eligibleInput(input)) continue;
                    let button = buttons.get(input);
                    if (!button || !button.isConnected) {
                        const container = input.closest(".report-autocomplete") || input.closest("label") || input.parentElement;
                        if (!container) continue;
                        container.classList.add("report-clearable-field");
                        if (input.type === "date" || input.type === "month" || input.type === "time" || input.type === "number") {
                            container.classList.add("report-clearable-native-control");
                        }
                        const label = input.labels?.[0];
                        const caption = label?.querySelector("span")?.textContent?.trim()
                            || label?.textContent?.trim().replace(/\s*\*\s*$/, "")
                            || "輸入內容";
                        button = document.createElement("button");
                        button.type = "button";
                        button.className = "report-input-clear";
                        button.setAttribute("aria-label", `清除${caption}`);
                        button.title = `清除${caption}`;
                        button.textContent = "×";
                        button.addEventListener("click", () => {
                            if (input.disabled || input.readOnly) return;
                            input.value = "";
                            input.dispatchEvent(new Event("input", { bubbles: true }));
                            input.dispatchEvent(new Event("change", { bubbles: true }));
                            button.hidden = true;
                            input.focus();
                        });
                        input.insertAdjacentElement("afterend", button);
                        buttons.set(input, button);
                    }
                    button.hidden = !input.value || input.disabled || input.readOnly;
                }
            };
            let frame = 0;
            const schedule = () => {
                if (frame) return;
                frame = requestAnimationFrame(() => {
                    frame = 0;
                    synchronize();
                });
            };
            const observer = new MutationObserver(schedule);
            observer.observe(element, { childList: true, subtree: true });
            element.addEventListener("input", schedule);
            element.addEventListener("change", schedule);
            element.addEventListener("click", schedule);
            element.addEventListener("focusin", schedule);
            synchronize();
            element._clearableInputsCleanup = () => {
                observer.disconnect();
                cancelAnimationFrame(frame);
                element.removeEventListener("input", schedule);
                element.removeEventListener("change", schedule);
                element.removeEventListener("click", schedule);
                element.removeEventListener("focusin", schedule);
            };
        },
        beforeUnmount(element) {
            element._clearableInputsCleanup?.();
            delete element._clearableInputsCleanup;
        }
    };

    window.ReportClearableInputsDirective = clearableInputsDirective;
})();
