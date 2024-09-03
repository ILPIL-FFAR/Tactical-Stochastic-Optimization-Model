using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class T5_param
    {
        public WAREModel ware { get; set; }
        public DISTModel dc { get; set; }
        public TRANSModel trans { get; set; }
        public double t5 { get; set; }
    }
}
