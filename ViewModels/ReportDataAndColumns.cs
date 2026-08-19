using static OpdAccrRptWeb.Help.ModelDescriptionsHelper;

namespace OpdAccrRptWeb.ViewModels
{
    public class ReportDataAndColumns<T>
    {
       public List<PropertyMetadata>? Columns { get; set; }

        public List<T>? Data { get; set; }

        public int? TotalCount { get; set; }

        public int? PageNumber { get; set; }

        public int? PageSize { get; set; }

        public int? TotalPages { get; set; }
    }
}
