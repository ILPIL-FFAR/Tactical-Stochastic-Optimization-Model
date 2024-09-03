using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class LandAvailableResult
    {
        public string location { get; set; }
        public double land { get; set; }
        public string grower { get; set; }
    }
}
