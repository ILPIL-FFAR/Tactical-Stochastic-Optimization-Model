using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct DEMModel
    {
        public double WEEKS { get; set; }
        public string PROD { get; set; }
        public string CUST { get; set; }
        public double maxDem { get; set; }
        public double contractDem { get; set; }
        public double contractPrice { get; set; }
    }
}
