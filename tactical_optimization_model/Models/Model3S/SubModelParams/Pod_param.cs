using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Pod_param
    {
        public WEEKPModel weekP { get; set; }
        public WEEKHModel weekH { get; set; }
        public CROPModel crop { get; set; }
        public PRODModel prod { get; set; }
        public double pod { get; set; }
    }
}
