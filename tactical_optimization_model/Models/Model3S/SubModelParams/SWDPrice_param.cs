using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class SWDPrice_param
    {
        public WEEK1Model week1 { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double swdPrice { get; set; }
    }
}
