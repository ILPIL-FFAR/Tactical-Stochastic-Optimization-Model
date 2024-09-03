using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class TimeD_param
    {
        public DISTModel dc { get; set; }
        public CUSTModel cust { get; set; }
        public TRANSModel trans { get; set; }
        public double timeD { get; set; }
    }
}
