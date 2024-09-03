using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class HarvestPrice_param
    {
        public WORKModel work { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double harvestPrice { get; set; }
    }
}
