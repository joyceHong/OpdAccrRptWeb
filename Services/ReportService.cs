using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using System.Data;

namespace OpdAccrRptWeb.Services
{
    /// <summary>
    /// 所有報表服務的基礎類別，提供報表相關的功能和操作。
    /// </summary>
    public class ReportService : IReportService
    {
        private IHealthCenterRepository _healthCenterRepository;
        public ReportService(IHealthCenterRepository healthCenterRepository)
        {
            _healthCenterRepository = healthCenterRepository;
        }

        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition)
        {
            try
            {
                var columns = new List<ModelDescriptionsHelper.PropertyMetadata>();
                var data = new List<T>();

                if (searchCondition.StartDate != null)
                {
                    searchCondition.StartDate = DateTimeExtensions.ToRocDateString(DateTime.Parse(searchCondition.StartDate));
                }

                if (searchCondition.EndDate != null)
                {
                    searchCondition.EndDate = DateTimeExtensions.ToRocDateString(DateTime.Parse(searchCondition.EndDate));
                }

                switch (searchCondition.ReportCode)
                {
                    case "C171":
                        columns = _healthCenterRepository.GetHelthCenterDetailColumns();
                        data = _healthCenterRepository.GetHealthCenterData<T>(searchCondition);
                        break;
                    case "C172":
                        columns = _healthCenterRepository.GetHelthCenterCountColumns();
                        data = _healthCenterRepository.GetHealthCenterCountData<T>(searchCondition);
                        break;
                    default:
                        throw new ArgumentException($"Invalid report code: {searchCondition.ReportCode}");
                }

              

                 
                return new ReportDataAndColumns<T>
                {
                    Columns = columns,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"error: {ex.Message}", ex);
            }
        }

    }
}
