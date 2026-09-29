(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    const previousMonth = value => {
        const date = new Date(`${value.slice(0, 7)}-01T00:00:00`);
        date.setMonth(date.getMonth() - 1);
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;
    };
    const message = async response => {
        const type = response.headers.get("content-type") || "";
        if (type.includes("json")) { const body = await response.json(); return body.title || "M2 查詢失敗。"; }
        return (await response.text()) || "M2 查詢失敗。";
    };
    window.ReportComponents = window.ReportComponents || {};
    window.ReportComponents.M2DoctorMonthlyReport = {
        template: "#m2-doctor-monthly-template", props: ["selectedReport", "defaultStartDate"],
        data() { return { form: { reportMonth: previousMonth(this.defaultStartDate), actualVisit: false, visitScope: 0, timeSlot: 0 },
            runId: null, rows: [], columns: [], totalCount: 0, totalPages: 0, currentPage: 1, pageSize: 10,
            isLoading: false, hasSearched: false, validationMessage: "", previewOpen: false, previewHtml: "" }; },
        computed: { hasResults() { return this.rows.length > 0 && !!this.runId; } },
        methods: {
            conditionChanged() { this.clearResults(); },
            clearResults() { this.runId=null;this.rows=[];this.columns=[];this.totalCount=0;this.totalPages=0;this.currentPage=1;this.hasSearched=false;this.closePreview(); },
            resetForm() { this.form={reportMonth:previousMonth(this.defaultStartDate),actualVisit:false,visitScope:0,timeSlot:0};this.pageSize=10;this.validationMessage="";this.clearResults(); },
            payload() { return { reportMonth:this.runId?null:this.form.reportMonth,calculationBasis:this.form.actualVisit?1:0,visitScope:this.form.visitScope,timeSlot:this.form.timeSlot,pageNumber:this.currentPage,pageSize:this.pageSize,runId:this.runId }; },
            async search(){this.currentPage=1;this.runId=null;await this.load();},
            async load(){this.isLoading=true;this.validationMessage="";try{const response=await fetch("/medical-statistics/doctor-monthly/query",{method:"POST",headers:{"Content-Type":"application/json","RequestVerificationToken":token()},body:JSON.stringify(this.payload())});if(!response.ok)throw new Error(await message(response));const result=await response.json();this.runId=result.runId;this.rows=result.data;this.columns=result.columns;this.totalCount=result.totalCount;this.totalPages=result.totalPages;this.currentPage=result.pageNumber;this.hasSearched=true;}catch(error){this.runId=null;this.rows=[];this.totalCount=0;this.totalPages=0;this.hasSearched=true;this.validationMessage=error instanceof Error?error.message:"M2 查詢失敗。";}finally{this.isLoading=false;}},
            async goToPage(page){if(page<1||page>this.totalPages||page===this.currentPage)return;this.currentPage=page;await this.load();},
            async changePageSize(){this.currentPage=1;await this.load();},
            async openPreview(){if(!this.hasResults)return;try{const body=new URLSearchParams({runId:this.runId,__RequestVerificationToken:token()});const response=await fetch("/medical-statistics/doctor-monthly/preview",{method:"POST",headers:{"Content-Type":"application/x-www-form-urlencoded;charset=UTF-8","RequestVerificationToken":token()},body});if(!response.ok)throw new Error(await message(response));this.previewHtml=await response.text();this.previewOpen=true;}catch(error){this.validationMessage=error instanceof Error?error.message:"M2 預覽失敗。";}},
            exportReport(format){if(this.hasResults)window.location.assign(`/medical-statistics/doctor-monthly/export?runId=${encodeURIComponent(this.runId)}&format=${format}`);},
            closePreview(){this.previewOpen=false;this.previewHtml="";},printPreview(){if(this.previewOpen)window.print();}
        }
    };
})();
