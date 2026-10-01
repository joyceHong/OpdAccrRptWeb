(() => {
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.ReportAutocomplete = {
        props: {
            modelValue: { type: String, default: "" },
            options: { type: Array, default: () => [] },
            open: { type: Boolean, default: false },
            idPrefix: { type: String, required: true },
            placeholder: { type: String, default: "" },
            loading: { type: Boolean, default: false }
        },
        emits: ["update:modelValue", "update:open", "input", "select"],
        data() { return { activeIndex: -1 }; },
        computed: {
            listId() { return `${this.idPrefix}-options`; },
            activeId() { return this.activeIndex >= 0 ? `${this.idPrefix}-option-${this.activeIndex}` : undefined; }
        },
        watch: {
            options() { this.activeIndex = -1; },
            open(value) { if (!value) this.activeIndex = -1; }
        },
        mounted() { document.addEventListener("pointerdown", this.closeFromOutside); },
        beforeUnmount() { document.removeEventListener("pointerdown", this.closeFromOutside); },
        methods: {
            onInput(event) {
                const value = event.target.value;
                this.$emit("update:modelValue", value);
                this.$emit("input", value);
                this.$emit("update:open", true);
            },
            selectOption(option) {
                this.$emit("select", option);
                this.$emit("update:open", false);
                this.activeIndex = -1;
            },
            onKeydown(event) {
                const count = this.options.length;
                if (event.key === "Escape") { this.$emit("update:open", false); this.activeIndex = -1; return; }
                if (!count) return;
                if (event.key === "ArrowDown") {
                    event.preventDefault(); this.$emit("update:open", true); this.activeIndex = (this.activeIndex + 1) % count;
                } else if (event.key === "ArrowUp") {
                    event.preventDefault(); this.$emit("update:open", true); this.activeIndex = (this.activeIndex - 1 + count) % count;
                } else if (event.key === "Enter" && this.open && this.activeIndex >= 0) {
                    event.preventDefault(); this.selectOption(this.options[this.activeIndex]);
                }
            },
            closeFromOutside(event) {
                if (!this.$el?.contains(event.target)) this.$emit("update:open", false);
            }
        },
        template: `
            <div class="report-autocomplete">
                <input :value="modelValue" autocomplete="off" :placeholder="placeholder"
                       role="combobox" aria-autocomplete="list" :aria-controls="listId"
                       :aria-expanded="open ? 'true' : 'false'" :aria-activedescendant="activeId"
                       v-on:focus="$emit('update:open', true)" v-on:input="onInput" v-on:keydown="onKeydown" />
                <div v-if="open && (options.length || loading)" :id="listId" class="report-autocomplete-panel" role="listbox">
                    <span v-if="loading" class="report-autocomplete-loading">載入中…</span>
                    <button v-for="(option,index) in options" :id="idPrefix + '-option-' + index" :key="option.value"
                            type="button" class="report-autocomplete-option" :class="{ active:index===activeIndex }"
                            role="option" :aria-selected="index===activeIndex ? 'true' : 'false'"
                            v-on:mouseenter="activeIndex=index" v-on:mousedown.prevent="selectOption(option)">
                        <strong>{{ option.code }}</strong><span>{{ option.label }}<template v-if="option.suffix">（{{ option.suffix }}）</template></span>
                    </button>
                </div>
            </div>`
    };
})();
