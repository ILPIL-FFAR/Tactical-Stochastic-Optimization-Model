using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Plantr_param
    {
        public WEEKPModel weekP { get; set; }
        public CROPModel crop { get; set; }
        public LOCModel loc { get; set; }
        public double plantR { get; set; }
    }
}
