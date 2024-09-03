using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class ReportResultModel
    {
        public UserRequestModel user { get; set; }
        public string model { get; set; }
        public String jsonResult { get; set; }
    }

    public class GetReportJsonModel
    {
        public UserRequestModel user { get; set; }
        public string reportId { get; set; }
    }
    public class YRPrediction
    {
        public string id { get; set; }
        public string crop { get; set; }
        public string market { get; set; }
        public double avgPriceLb { get; set; }
        public double stdevPriceLb { get; set; }
        public string week { get; set; }
    }
}
