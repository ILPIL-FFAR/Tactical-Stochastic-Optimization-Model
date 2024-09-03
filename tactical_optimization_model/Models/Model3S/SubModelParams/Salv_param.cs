using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Salv_param
    {
        public WEEKPModel weekP { get; set; }
        public WEEKHModel weekH { get; set; }
        public CROPModel crop { get; set; }
        public LOCModel loc { get; set; }
        public double salv { get; set; }
    }
}
