using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class PriceCrop_param
    {
        public WEEKSModel weeks { get; set; }
        public PRODModel prod { get; set; }
        public CUSTModel cust { get; set; }
        public SCENModel scen { get; set; }
        public double price { get; set; }
    }
}
