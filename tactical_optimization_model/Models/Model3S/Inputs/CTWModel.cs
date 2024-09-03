using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct CTWModel
    {
        public string WARE { get; set; }
        public string CUST { get; set; }
        public string TRANS { get; set; }
        public double TimeW { get; set; }
        public double T2 { get; set; }
        public double CTW { get; set; }
    }
}
