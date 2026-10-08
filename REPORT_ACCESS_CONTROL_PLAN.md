# 報表權限與跨系統存取方案（設計草案）

> 範圍：本文件中的「表」指 C21、M1、Q2 等報表或查詢功能，不是 Oracle 底層資料表。本文是系統分析與建議，尚未實作。

## 1. 現況與要解決的問題

- 報表目錄目前由 `Services/ReportCatalogService.cs` 固定建立，前端從伺服器輸出的初始狀態顯示選單。
- `Program.cs` 已使用 Negotiate 登入，但 `Controllers/ReportController.cs` 目前只有 `Index` 明確標示 `[Authorize]`；共用查詢、預覽、Excel、匯出工作狀態和下載路由需逐一補齊驗證。部分專屬 Controller 有類別層 `[Authorize]`，仍沒有報表操作級授權。
- `Services/ReportExportJobStore.cs` 的工作資料只有條件、狀態與檔名，尚未記錄申請者。知道 `jobId` 的其他人可能嘗試讀取狀態或下載。
- C21/C23 的重建使用者身分目前來自設定值 `ConfiguredMockUser`，不能視為正式的人員授權依據。
- 「隱藏選單」只是畫面效果；真正的界線必須在每一個伺服器端點檢查。

## 2. 建議的權限模型

以 `reportCode × action` 為最小授權單位。固定操作如下：

| 操作代碼 | 意義 | 需要檢查的入口 |
| --- | --- | --- |
| `VIEW` | 看報表選單與頁面 | 目錄、報表頁面 |
| `QUERY` | 查詢與讀取資料 | 一般查詢 API、候選清單與資料 API |
| `PREVIEW` | 預覽及列印用內容 | HTML/PDF 預覽、列印頁 |
| `EXPORT` | 建立及下載 Excel | 同步匯出、排程工作、工作狀態、下載 |
| `REBUILD` | 手動重建計算結果 | `ForceRebuild=true` 的查詢或匯出流程 |

`REBUILD` 是附加權限：使用者若要「查詢並重建」，必須同時擁有 `QUERY` 與 `REBUILD`；若要「匯出並重建」，必須同時擁有 `EXPORT` 與 `REBUILD`。仍要保留原本的日期、模式與報表專屬限制。瀏覽器原生列印無法獨立管控，建議以伺服器產生的 `PREVIEW` 內容作為可控邊界。

授權主體支援 `USER`、`DEPARTMENT`、`ROLE`：個人適合例外開通，部門適合共同報表，角色適合跨部門職務。預設拒絕；任一有效 `ALLOW` 可授權，但匹配的 `DENY` 優先。停用報表、停用人員、過期授權、停用角色均不可取得權限。人員的在職狀態和部門以院內身分來源為準，不以本系統權限表自行宣告。

報表權限不等於資料列權限。例如有 C21 `QUERY` 不一定可看全院資料。如果需要「只看本人部門」或「只看特定院區」，須另定義可列舉的 `data_scope`，在 Repository 查詢條件中實際套用；不能靠前端帶部門代碼，也不能把任意 SQL 放在授權表。沒有安全資料範圍映射的報表，應拒絕受限範圍查詢。

## 3. 建議資料表

建議以 9 張表作為完整方案；若第一期只做站內權限，可先建前 5 張與稽核表，跨系統 3 張留到第二期。名稱可配合院內資料庫命名規範調整。

| 資料表 | 重要欄位 | 用途與約束 |
| --- | --- | --- |
| `REPORT_RESOURCE` | `report_code` PK、名稱、分類、`enabled`、敏感等級 | 系統可用報表主檔，與程式中的報表代碼定期對帳 |
| `REPORT_ACTION` | `report_code`、`action` 複合 PK | 列出各報表支援的操作；未登錄的操作不可授權 |
| `AUTH_ROLE` | `role_id` PK、`role_code` UNIQUE、名稱、`enabled` | 業務角色主檔 |
| `AUTH_USER_ROLE` | `subject_id`、`role_id`、生效/失效時間、指派者 | 人員與角色的有效期關係 |
| `AUTH_REPORT_GRANT` | `grant_id` PK、`principal_type`、`principal_id`、`report_code`、`action`、`effect`、`data_scope`、生效/失效時間、原因、異動者 | 個人/部門/角色的核心授權表；索引至少涵蓋主體、報表與操作 |
| `AUTH_API_CLIENT` | `client_id` PK、`active`、憑證參照或公鑰、允許的 audience | 受信任系統清冊；不存明文密鑰 |
| `AUTH_CLIENT_DELEGATION` | `client_id`、代理範圍、`report_code`、`action`、有效期、核准者 | 限制哪個系統可代表哪些人申請哪些報表 |
| `AUTH_REPORT_TICKET` | `ticket_hash` PK、`client_id`、`subject_id`、`report_code`、`action`、`audience`、核發/到期/撤銷時間 | 短效票據；只存雜湊，不存 token 明文 |
| `AUTH_ACCESS_AUDIT` | `event_id` PK、UTC 時間、操作人、目標使用者、client、報表、操作、結果、原因、correlation ID | 核發、存取、拒絕及權限異動稽核 |

人員與部門主檔建議直接使用院內 AD/SSO/HR 系統；若無即時查詢能力，需另外建立受控同步快取，並定義同步時效、資料失效與離職停權規則。是否要建本地人員快取表，取決於院內來源的介面，不宜現在直接把權限表當成人員主檔。

## 4. 權限判斷與選單流程

1. 使用者通過 Negotiate 登入，伺服器將登入名稱對應為穩定的 `subject_id`，驗證在職狀態並取得有效部門。
2. 伺服器讀取個人、部門、角色授權，依 `DENY > ALLOW > 無規則拒絕` 產生目前權限。
3. 選單只輸出有 `VIEW` 且已啟用的報表；空群組、空分類不顯示。按鈕依各自操作權限顯示。
4. 無論畫面是否顯示按鈕，後端對每個直接 URL/API 呼叫重新驗證 `reportCode` 與操作。未登入回 `401`；已登入但無權回 `403`；未知或停用報表回 `404`。
5. `jobId` 類型的資源須再核對申請者與目前 `EXPORT` 權限。舊資料沒有 owner 時，不開放下載。

建議使用 ASP.NET Core policy/resource-based authorization：`[Authorize]` 確保登入，另外以 `IAuthorizationService` 或集中授權服務處理執行時才知道的 `reportCode`、`action` 和 `jobId`。單獨一個 `[Authorize]` 只能證明已登入，不能證明可用指定報表。

## 5. 跨系統 API 與 token 流程

你提出的兩階段想法是對的：先授權再核發短效 token，使用時重新驗證。需要補上一個關鍵條件：**呼叫方不能只傳 `userID` 就取得他人的報表 token**。

建議契約：

1. 合作系統先以院內 OAuth/OIDC on-behalf-of、使用者簽章聲明，或經核准的後端憑證/代理範圍證明其身分與代理資格。
2. `POST /api/report-access/tickets` 接收 `{ "userId": "...", "reportCode": "C21", "action": "QUERY", "audience": "report-api" }`。伺服器驗證 client、可代理的使用者、使用者存在且在職、報表存在、操作可用、目前授權後核發預設 5 分鐘的 opaque token。
3. 回應 `{ "accessToken": "...", "tokenType": "Bearer", "expiresIn": 300, "reportUrl": "/api/reports/C21" }`，設定 `Cache-Control: no-store`。token 僅在此回應出現一次。
4. 合作系統呼叫報表時，將 token 放在 `Authorization: Bearer` 標頭。每次讀取重新檢查 token 雜湊、到期/撤銷、client、使用者、`reportCode`、`action`、audience，以及現在是否仍有權限。
5. 若要讓瀏覽器直接開報表頁，先透過受保護的 `POST` 換成短效 `Secure; HttpOnly; SameSite` cookie，再導向不含 token 的報表網址。瀏覽器地址列、query string、referer 與日誌中不放 token。

成功取得 token **不代表後續一直有權限**：離職、撤權、報表停用或 token 過期後，下一次讀取立即拒絕。若既有院內身分平台已能簽發符合需求的 OAuth access token，可優先整合，避免自行維護一套認證平台；本方案的 opaque ticket 是在需要即時撤銷且院內沒有可直接使用的委派流程時的替代方案。

## 6. 我認為原需求需要修正的地方

1. **`userID` 只能當查詢目標，不能當登入證明。** 必須驗證合作系統是否有資格代表這個人，否則任何受信任系統都可能填入院長或其他高權限帳號。
2. **「有角色」不等於「有此操作權限」。** 權限應落在 `reportCode + action`；部門、個人與角色的規則要有衝突優先序，建議 `DENY` 優先。
3. **選單隱藏不是授權。** 現有共用端點、專屬端點、匯出狀態與下載都要盤點。只在 MVC 頁面加 `[Authorize]` 不夠。
4. **報表權限和可看哪些資料列是兩個問題。** 若院內要限制部門資料，必須在每個報表的資料查詢端落實範圍。
5. **token 不應放在報表 URL。** 網址可能出現在瀏覽器紀錄、代理設備及日誌；改用標頭，瀏覽器則走 POST 換 cookie。
6. **背景匯出需綁定申請者。** 否則知道 `jobId` 的人可能取得他人的 Excel；撤權後排隊工作也不應繼續生成。
7. **重建不宜只靠設定旗標。** `RebuildEnabled` 可保留為功能開關，但操作本身還需獨立的 `REBUILD` 權限與真實使用者身分。

## 7. 分期建議與待確認事項

- 第一階段：先補齊站內登入與全部端點的授權盤點、權限表、選單/按鈕控制、匯出工作 owner；以預設拒絕上線。
- 第二階段：定義資料範圍，逐報表落實部門/院區資料隔離；若業務確認不需要列級限制，可省略此階段。
- 第三階段：確認院內 IdP 與合作系統資格後，上線短效 ticket API 與跨系統報表取用。

需要業務與資安單位決定：人員/部門的權威資料來源；部門異動生效時點；哪些報表及操作要授權；是否允許個別拒絕覆蓋部門授權；哪些合作系統有代理資格；token 實際用於資料 API、瀏覽器頁面，或兩者。

## 參考標準

- [Microsoft：ASP.NET Core policy-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)
- [Microsoft：ASP.NET Core resource-based authorization](https://learn.microsoft.com/aspnet/core/mvc/security/authorization/resource-based?view=aspnetcore-10.0)
- [RFC 9700：OAuth 2.0 Security Best Current Practice](https://www.rfc-editor.org/rfc/rfc9700)
- [OWASP API1:2023：Broken Object Level Authorization](https://api-security.owasp.org/editions/2023/en/0xa1-broken-object-level-authorization/)
