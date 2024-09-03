using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class LaborPrice_param
    {
        public WEEKModel week { get; set; }
        public LOCModel loc { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double laborPrice { get; set; }
    }
}
