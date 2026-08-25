using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;
using static OpdAccrRptWeb.Help.ModelDescriptionsHelper;

namespace OpdAccrRptWeb.Repositories
{
    public interface IHealthCenterRepository
    {
        /// <summary>
        /// 健康管理中心明細資料
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchCondition"></param>
        /// <returns></returns>
        int GetHealthCenterDataCount(SearchReportCondition searchCondition);

        List<T> GetHealthCenterDataPage<T>(SearchReportCondition searchCondition);

        /// <summary>
        /// 健康管理中心明細資料的欄位資訊
        /// </summary>
        /// <returns></returns>
        List<ModelDescriptionsHelper.PropertyMetadata> GetHelthCenterDetailColumns();

        /// <summary>
        /// 健康管理中心金額統計的欄位資訊
        /// </summary>
        /// <returns></returns>
        List<ModelDescriptionsHelper.PropertyMetadata> GetHelthCenterCountColumns();

        /// <summary>
        /// 健康管理中心金額統計
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchCondition"></param>
        /// <returns></returns>
        List<T> GetHealthCenterCountData<T>(SearchReportCondition searchCondition);

        /// <summary>
        /// 健檢人次的欄位資訊
        /// </summary>
        /// <returns></returns>
        List<PropertyMetadata> GetHealthCheckupVisitsColumns();

        /// <summary>
        /// 健檢人次的資料
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchCondition"></param>
        /// <returns></returns>
        public List<T> GetHealthCheckupVisitsData<T>(SearchReportCondition searchCondition);

        /// <summary>
        /// C174 健康管理中心合約單位記帳表的欄位資訊
        /// </summary>
        /// <returns></returns>
        List<PropertyMetadata> GetHealthCenterContractBillingReportColumns();

        /// <summary>
        /// C174 健康管理中心合約單位記帳表總筆數
        /// </summary>
        /// <param name="searchCondition"></param>
        /// <returns></returns>
        int GetHealthCenterContractBillingReportCount(SearchReportCondition searchCondition);

        /// <summary>
        /// C174 健康管理中心合約單位記帳表分頁資料
        /// </summary>
        List<T> GetHealthCenterContractBillingReportPage<T>(SearchReportCondition searchCondition);

        List<HealthCenterContractBillingReport> GetHealthCenterContractBillingReportBatch(
            SearchReportCondition searchCondition,
            int offset,
            int batchSize);


    }
}
