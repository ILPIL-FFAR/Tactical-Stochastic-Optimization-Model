using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class DemandPrice2_param
    {
        public WEEKSModel weeks { get; set; }
        public PRODModel prod { get; set; }
        public CUSTModel customer { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double demandPrice2 { get; set; }
    }
}
