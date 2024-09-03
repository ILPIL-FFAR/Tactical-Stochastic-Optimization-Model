using ModelStochastic6.Models.Inputs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Inv1Price_param
    {
        public WEEKHModel weekH { get; set; }
        public PRODModel prod { get; set; }
        public QUALModel qual { get; set; }
        public WAREModel ware { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double inv1Price { get; set; }
    }
}
