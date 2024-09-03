using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Total_param
    {
        public WEEKPModel weekP { get; set; }
        public CROPModel crop { get; set; }
        public LOCModel loc { get; set; }
        public double total { get; set; }
    }
}
