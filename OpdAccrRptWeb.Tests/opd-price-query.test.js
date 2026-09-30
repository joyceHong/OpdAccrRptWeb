const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");
global.window = { open(){} }; global.document = { querySelector(){ return { value:"csrf" }; } };
vm.runInThisContext(fs.readFileSync("wwwroot/js/reports/opd-price-query.js","utf8"));
const component=window.ReportComponents.OpdPriceQuery;
const catalog=fs.readFileSync("Services/ReportCatalogService.cs","utf8");
const app=fs.readFileSync("wwwroot/js/report-app.js","utf8");
const markup=fs.readFileSync("Views/Report/_OpdPriceQuery.cshtml","utf8");
const receipt=fs.readFileSync("Views/OpdPriceQuery/Receipt.cshtml","utf8");
assert.match(catalog,/Report\("Q1", "批價查詢"\).*Report\("Q2", "病歷查詢"\).*Report\("Q3", "掛號查詢"\)/s);
assert.match(app,/Q1:\s*window\.ReportComponents\.OpdPriceQuery/);assert.match(app,/report\.code === "Q1".*"\/data-query\/opd-price"/);
assert.match(app,/selectedReport\.name.*尚未建置/);assert.match(markup,/type="date"/);assert.match(markup,/partial name="_TableSkeleton"/);assert.match(markup,/role="status">資料查詢中/);assert.match(markup,/report-autocomplete/);assert.match(markup,/共 \{\{totalCount\}\} 筆/);assert.match(markup,/列印收據/);
assert.match(receipt,/window\.print/);assert.doesNotMatch(receipt,/Crystal|Receipt\.mdb/);

async function queryContract(){let calls=[];global.fetch=async(url,options)=>{calls.push([url,JSON.parse(options.body)]);if(url.endsWith("visits"))return{ok:true,json:async()=>({rows:[{visitToken:"v1"}],totalCount:1,totalPages:1,pageNumber:1})};return{ok:true,json:async()=>({patient:{},charges:[],receipts:[]})}};const state={...component.data(),async message(){return"error"}};Object.assign(state,{fetchVisits:component.methods.fetchVisits,selectVisit:component.methods.selectVisit});await component.methods.search.call(state);assert.equal(calls[0][0],"/data-query/opd-price/visits");assert.equal(calls[0][1].pageSize,10);assert.equal(calls[1][0],"/data-query/opd-price/detail");assert.equal(calls[1][1].visitToken,"v1");component.methods.invalidate.call(state);assert.equal(state.searched,false);}
queryContract().then(()=>console.log("opd price query tests passed"));
