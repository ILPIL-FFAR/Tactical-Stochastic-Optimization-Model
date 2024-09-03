using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class HarvPrice_param
    {
        public WORKModel work { get; set; }
        public SCENModel scen { get; set; }
        public int fCut { get; set; }
        public double harvPrice { get; set; }
    }
}
