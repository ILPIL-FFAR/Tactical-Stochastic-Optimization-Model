using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct CTPDModel
    {
        public string FAC { get; set; }
        public string DC { get; set; }
        public string TRANS { get; set; }
        public double TimePD { get; set; }
        public double T4 { get; set; }
        public double CTPD { get; set; }
    }
}
