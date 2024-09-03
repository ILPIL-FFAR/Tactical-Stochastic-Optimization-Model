using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class T1_param
    {
        public FACModel fac { get; set; }
        public CUSTModel cust { get; set; }
        public TRANSModel trans { get; set; }
        public double t1 { get; set; }
    }
}
