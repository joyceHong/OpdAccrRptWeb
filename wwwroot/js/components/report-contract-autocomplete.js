(() => {
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.ReportContractAutocomplete = {
        components: { ReportAutocomplete: window.ReportComponents.ReportAutocomplete },
        props: {
            modelValue: { type: String, default: "" },
            options: { type: Array, default: () => [] },
            idPrefix: { type: String, required: true },
            placeholder: { type: String, default: "" },
            loading: { type: Boolean, default: false }
        },
        emits: ["update:modelValue", "validity-change"],
        data() {
            return { query: "", currentCode: "", selectedCode: "", unresolved: false, open: false };
        },
        computed: {
            filteredOptions() {
                const selected = this.options.find(item => item.code === this.selectedCode);
                const query = selected && this.query === this.displayChoice(selected)
                    ? "" : this.query.trim().toUpperCase();
                return this.options.filter(item => !query
                    || item.code.toUpperCase().includes(query)
                    || (item.name ?? "").toUpperCase().includes(query))
                    .map(item => ({ value: item.code, code: item.code, label: item.name ?? "" }));
            }
        },
        watch: {
            modelValue(value) {
                if ((value ?? "") !== this.currentCode) this.setCode(value);
            },
            options() {
                if (!this.unresolved) this.setCode(this.currentCode);
            }
        },
        mounted() { this.setCode(this.modelValue); },
        methods: {
            displayChoice(item) { return item.name ? `${item.code} ${item.name}` : item.code; },
            setCode(value) {
                const code = (value ?? "").trim();
                const choice = this.options.find(item => item.code === code);
                this.currentCode = code;
                this.selectedCode = choice ? code : "";
                this.query = choice ? this.displayChoice(choice) : code;
                this.unresolved = false;
            },
            onInput(value) {
                this.query = value;
                this.selectedCode = "";
                const code = value.trim();
                this.unresolved = /[\u3400-\u9fff]/u.test(code);
                this.currentCode = this.unresolved ? "" : code;
                this.$emit("update:modelValue", this.currentCode);
                this.$emit("validity-change", !this.unresolved);
            },
            selectChoice(item) {
                this.currentCode = item.code;
                this.selectedCode = item.code;
                this.query = this.displayChoice({ code: item.code, name: item.label });
                this.unresolved = false;
                this.open = false;
                this.$emit("update:modelValue", item.code);
                this.$emit("validity-change", true);
            },
            isValid() { return !this.unresolved; },
            focus() { this.$el?.querySelector("input")?.focus(); }
        },
        template: `
            <report-autocomplete :model-value="query" :options="filteredOptions" v-model:open="open"
                                 :id-prefix="idPrefix" :placeholder="placeholder" :loading="loading"
                                 v-on:update:model-value="onInput" v-on:select="selectChoice" />`
    };
})();
