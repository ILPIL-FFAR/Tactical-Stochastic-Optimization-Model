using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class CT_param
    {
        public FACModel MyProperty { get; set; }
        public CUSTModel cust { get; set; }
        public TRANSModel trans { get; set; }
        public double ct { get; set; }
    }
}
