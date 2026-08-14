# 門急診報表作業

ASP.NET Core 10 MVC + Razor + Vue 3 的報表 UI 骨架。目前查詢結果為前端示範資料，尚未連接資料庫或報表 API。

## 開發環境

- .NET 10 SDK / ASP.NET Core 10 Runtime
- Node.js 20 以上

## 啟動

```powershell
npm install
npm run copy:vendor
dotnet restore
dotnet run
```

Vue runtime 會從 `node_modules` 複製至 `wwwroot/vendor`，執行網站時不依賴 CDN。

## 架構與後續擴充

- `Controllers`：只處理 HTTP 與頁面流程。
- `Services`：組裝報表目錄及未來的業務規則。
- `ViewModels`：Razor 與 Vue 畫面需要的資料。
- `Views/Report`：Razor 掛載點與報表頁面。
- `wwwroot/js/report-app.js`：Vue 3 互動區。

串接真實報表時，新增 `Repositories`、`QueryModels` 與報表 API Controller。Dapper 僅能放在 Repository；資料庫密碼取得集中封裝為基礎設施服務，待取得 `FembDb.dll` 後由 DI 注入，不在設定檔保存密碼。OAuth2 亦待院內元件與設定提供後再接入。

第三方授權請參閱 `THIRD-PARTY-NOTICES.md`。
