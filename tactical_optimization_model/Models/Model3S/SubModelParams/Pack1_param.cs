using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Pack1_param
    {
        public WEEKHModel weekH { get; set; }
        public PRODModel prod { get; set; }
        public FACModel fac { get; set; }
        public SCENModel scen { get; set; }
        public double pack1 { get; set; }
    }
}
