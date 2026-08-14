using static OpdAccrRptWeb.Help.ModelDescriptionsHelper;

namespace OpdAccrRptWeb.ViewModels
{
    public class ReportDataAndColumns<T>
    {
       public List<PropertyMetadata>? Columns { get; set; }

        public List<T>? Data { get; set; }
    }
}
