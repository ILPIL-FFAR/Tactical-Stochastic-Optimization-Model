using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class CTD_param
    {
        public DISTModel dc { get; set; }
        public CUSTModel cust { get; set; }
        public TRANSModel trans { get; set; }
        public double ctd { get; set; }
    }
}
