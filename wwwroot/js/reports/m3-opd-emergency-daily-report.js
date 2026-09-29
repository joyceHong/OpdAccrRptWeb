(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    const yesterday = value => { const d = new Date(`${value}T00:00:00`); d.setDate(d.getDate() - 1); return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,"0")}-${String(d.getDate()).padStart(2,"0")}`; };
    const errorMessage = async response => { const type=response.headers.get("content-type")||""; if(type.includes("json")){const body=await response.json();return body.title||"M3 查詢失敗。";} return (await response.text())||"M3 查詢失敗。"; };
    window.ReportComponents=window.ReportComponents||{};
    window.ReportComponents.M3OpdEmergencyDailyReport={
        template:"#m3-opd-emergency-daily-template", props:["selectedReport","defaultStartDate"],
        data(){return{form:{reportDate:yesterday(this.defaultStartDate)},runId:null,rows:[],columns:[],nineKpis:{},totalCount:0,totalPages:0,currentPage:1,pageSize:10,isLoading:false,hasSearched:false,validationMessage:"",previewOpen:false,previewLoading:false,previewHtml:""};},
        computed:{kpiItems(){const k=this.nineKpis||{};return [["門診早",k.outpatientMorning],["門診午",k.outpatientAfternoon],["門診夜",k.outpatientNight],["急診白",k.emergencyDay],["急診小夜",k.emergencyEvening],["急診大夜",k.emergencyNight],["預約",k.appointment],["未到",k.noShow],["淨預約",k.netAppointment]].map(([label,value])=>({label,value:value??0}));}},
        methods:{
            conditionChanged(){this.clearResults();}, clearResults(){this.runId=null;this.rows=[];this.columns=[];this.nineKpis={};this.totalCount=0;this.totalPages=0;this.currentPage=1;this.hasSearched=false;this.closePreview();},
            resetForm(){this.form.reportDate=yesterday(this.defaultStartDate);this.validationMessage="";this.pageSize=10;this.clearResults();},
            payload(){return{reportDate:this.runId?null:this.form.reportDate,pageNumber:this.currentPage,pageSize:this.pageSize,runId:this.runId};},
            async search(){this.currentPage=1;this.runId=null;await this.load();},
            async load(){this.isLoading=true;this.validationMessage="";try{const response=await fetch("/medical-statistics/opd-emergency-daily/query",{method:"POST",headers:{"Content-Type":"application/json","RequestVerificationToken":token()},body:JSON.stringify(this.payload())});if(!response.ok)throw new Error(await errorMessage(response));const r=await response.json();this.runId=r.runId;this.rows=r.data;this.columns=r.columns;this.nineKpis=r.nineKpis;this.totalCount=r.totalCount;this.totalPages=r.totalPages;this.currentPage=r.pageNumber;this.hasSearched=true;}catch(error){this.clearResults();this.hasSearched=true;this.validationMessage=error instanceof Error?error.message:"M3 查詢失敗。";}finally{this.isLoading=false;}},
            async goToPage(page){if(page<1||page>this.totalPages||page===this.currentPage)return;this.currentPage=page;await this.load();}, async changePageSize(){this.currentPage=1;await this.load();},
            async openPreview(){if(!this.runId||this.previewLoading)return;this.previewLoading=true;this.validationMessage="";try{const response=await fetch(`/medical-statistics/opd-emergency-daily/preview?runId=${encodeURIComponent(this.runId)}`);if(!response.ok)throw new Error(await errorMessage(response));this.previewHtml=await response.text();this.previewOpen=true;}catch(error){this.closePreview();this.validationMessage=error instanceof Error?error.message:"M3 預覽失敗。";}finally{this.previewLoading=false;}},
            exportReport(format){if(this.runId)window.location.assign(`/medical-statistics/opd-emergency-daily/export?runId=${encodeURIComponent(this.runId)}&format=${format}`);}, closePreview(){this.previewOpen=false;this.previewHtml="";}, printPreview(){if(this.previewOpen)window.print();}
        }
    };
})();
