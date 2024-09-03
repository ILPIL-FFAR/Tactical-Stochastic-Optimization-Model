using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class T3_param
    {
        public FACModel fac { get; set; }
        public WAREModel ware { get; set; }
        public TRANSModel trans { get; set; }
        public double t3 { get; set; }
    }
}
