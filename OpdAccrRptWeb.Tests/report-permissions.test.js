const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const source = fs.readFileSync("wwwroot/js/report-permissions.js", "utf8");
const catalog = [{
    key: "inpatient", name: "住院",
    groups: [{ name: "應收", reports: [
        { code: "C1", name: "報表一" },
        { code: "C141", name: "住院應收帳款排行" }
    ] }]
}];
const root = {};
let component;
const document = {
    getElementById(id) {
        if (id === "permission-panel-root") return root;
        if (id === "permission-catalog") return { textContent: JSON.stringify(catalog) };
        if (id === "permission-panel-token")
            return { querySelector() { return { value: "test-token" }; } };
        return null;
    }
};
const Vue = {
    h(tag, props, children) {
        if (arguments.length === 2 && (Array.isArray(props) ||
            typeof props === "string")) return { tag, props: {}, children: props };
        return { tag, props: props || {}, children };
    },
    createApp(options) {
        component = options;
        return { mount() {} };
    }
};
const fetch = async path => {
    assert.match(path, /^\/permissions\/grants\?/);
    return {
        ok: true,
        json: async () => [
            { reportCode: "C1", actionMask: 7 },
            { reportCode: "C141", actionMask: 23 }
        ]
    };
};
const window = { dispatchEvent() {} };
vm.runInNewContext(source, {
    Vue, document, window, fetch, URLSearchParams, Event, setTimeout, clearTimeout
});

const instance = {
    ...component.data(),
    $nextTick: async () => {},
    $refs: {
        editorDialog: {
            open: false,
            showModal() { this.open = true; },
            close() { this.open = false; }
        },
        copyDialog: {
            open: false,
            showModal() { this.open = true; },
            close() { this.open = false; }
        },
        copySourceInput: { focus() {} },
        deleteDialog: {
            open: false,
            showModal() { this.open = true; },
            close() { this.open = false; }
        },
        targetInput: { focus() {} }
    }
};
for (const [name, method] of Object.entries(component.methods))
    instance[name] = method.bind(instance);

(async () => {
    await instance.openEditor({
        targetType: 0, target: "F1482", targetName: "F1482"
    });
    assert.equal(instance.actions.lineBot, true);
    assert.equal(instance.initialLineBotChecked, true);
    assert.deepEqual(Array.from(instance.selectedCodes), ["C1", "C141"]);
    assert.equal(component.render.call(instance).tag, "div");

    const vnode = instance.renderEditor();
    const findLineBotInput = node => {
        if (!node || typeof node !== "object") return null;
        if (node.tag === "label" && Array.isArray(node.children) &&
            node.children.includes(" C141 LineBot 發送"))
            return node.children.find(child => child.tag === "input");
        for (const child of Array.isArray(node.children) ? node.children : [])
            if (findLineBotInput(child)) return findLineBotInput(child);
        return null;
    };
    assert.equal(findLineBotInput(vnode)?.props.checked, true);

    instance.actions.lineBot = false;
    assert.equal(instance.actionMaskFor("C1", 7), 7);
    assert.equal(instance.actionMaskFor("C141", 7), 7);
    assert.equal(instance.generalActionsChanged, false);

    const requests = [];
    instance.postJson = async (path, payload) => {
        requests.push({ path, payload: JSON.parse(JSON.stringify(payload)) });
        if (path.endsWith("/preview")) return { json: async () => [
            { reportCode: "C1", oldMask: 7, operation: "無變更" },
            { reportCode: "C141", oldMask: 23, operation: "變更" }
        ] };
        return {};
    };
    instance.loadList = async () => true;
    instance.notifyChange = () => {};
    await instance.saveEditor({ preventDefault() {} });
    assert.equal(requests.length, 2);
    assert.equal(requests[0].payload.grants[0].actionMask, 7);
    assert.equal(requests[0].payload.grants[1].actionMask, 7);
    assert.equal(requests[1].payload.grants[1].expectedMask, 23);
})().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
