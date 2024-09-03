using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Tday_param
    {
        public FACModel fac { get; set; }
        public CUSTModel cust { get; set; }
        public TRANSModel trans { get; set; }
        public double tday { get; set; }
    }
}
