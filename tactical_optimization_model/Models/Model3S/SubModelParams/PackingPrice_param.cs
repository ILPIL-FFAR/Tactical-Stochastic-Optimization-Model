using ModelStochastic6.Models.Inputs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class PackingPrice_param
    {
        public WEEKHModel weekH { get; set; }
        public PRODModel prod { get; set; }
        public QUALModel qual { get; set; }
        public FACModel fac { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double packingPrice { get; set; }
    }
}
