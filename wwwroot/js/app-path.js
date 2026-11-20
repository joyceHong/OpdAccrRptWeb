(() => {
    "use strict";

    const rawBase = document.querySelector('meta[name="app-path-base"]')?.content || "";
    const pathBase = rawBase.replace(/\/+$/, "");

    window.sitePathBase = pathBase;
    window.siteUrl = path => {
        if (typeof path !== "string" || !path.startsWith("/") || path.startsWith("//")) {
            return path;
        }
        if (!pathBase || path === pathBase || path.startsWith(`${pathBase}/`) || path.startsWith(`${pathBase}?`)) {
            return path;
        }
        return `${pathBase}${path}`;
    };
})();
