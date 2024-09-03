using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class HarvestResult
    {
        public string crop { get; set; }
        public int weekp { get; set; }
        public int weekh { get; set; }
        public string location { get; set; }
        public double pounds { get; set; }
        public string grower { get; set; }
    }
}
