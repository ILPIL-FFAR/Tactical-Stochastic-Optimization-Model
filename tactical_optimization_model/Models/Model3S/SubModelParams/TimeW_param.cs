using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class TimeW_param
    {
        public WAREModel ware { get; set; }
        public CUSTModel cust { get; set; }
        public TRANSModel trans { get; set; }
        public double timeW { get; set; }
    }
}
