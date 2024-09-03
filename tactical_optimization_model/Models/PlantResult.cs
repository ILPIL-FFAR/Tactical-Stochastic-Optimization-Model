using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class PlantResult
    {
        public string crop { get; set; }
        public int week { get; set; }
        public string location { get; set; }
        public double acres { get; set; }
        public string grower { get; set; }

    }
}
