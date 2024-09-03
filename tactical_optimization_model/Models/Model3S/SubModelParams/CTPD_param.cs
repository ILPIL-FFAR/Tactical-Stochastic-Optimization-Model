using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class CTPD_param
    {
        public FACModel fac { get; set; }
        public DISTModel dc { get; set; }
        public TRANSModel trans { get; set; }
        public double ctpd { get; set; }
    }
}
