using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class T4_param
    {
        public FACModel fac { get; set; }
        public DISTModel dc { get; set; }
        public TRANSModel trans { get; set; }
        public double t4 { get; set; }
    }
}
