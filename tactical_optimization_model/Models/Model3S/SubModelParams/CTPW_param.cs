using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class CTPW_param
    {
        public FACModel fac { get; set; }
        public WAREModel ware { get; set; }
        public TRANSModel trans { get; set; }
        public double ctpw { get; set; }
    }
}
