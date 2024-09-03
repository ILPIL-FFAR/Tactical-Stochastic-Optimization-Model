using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class SCResult
    {
        public string crop { get; set; }
        public int week { get; set; }
        public double quality { get; set; }
        public string processingFacility { get; set; }
        public string customer { get; set; }
        public string transportationType { get; set; }
        public string location { get; set; }
        public double boxes { get; set; }
        public string grower { get; set; }
    }
}
