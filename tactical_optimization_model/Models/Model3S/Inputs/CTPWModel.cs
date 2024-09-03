using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct CTPWModel
    {
        public string FAC { get; set; }
        public string WARE { get; set; }
        public string TRANS { get; set; }
        public double TimePW { get; set; }
        public double T3 { get; set; }
        public double CTPW { get; set; }
    }
}
