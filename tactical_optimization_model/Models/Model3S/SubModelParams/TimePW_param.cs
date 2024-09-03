using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class TimePW_param
    {
        public FACModel fac { get; set; }
        public WAREModel ware { get; set; }
        public TRANSModel trans { get; set; }
        public double timePW { get; set; }
    }
}
