using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class TimeWD_param
    {
        public WAREModel ware { get; set; }
        public DISTModel dc { get; set; }
        public TRANSModel trans { get; set; }
        public double timeWD { get; set; }
    }
}
