using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class SpotResult
    {
        public string crop { get; set; }
        public int week { get; set; }
        public string customer { get; set; }
        public string location { get; set; }
        public double boxes { get; set; }
    }
}
