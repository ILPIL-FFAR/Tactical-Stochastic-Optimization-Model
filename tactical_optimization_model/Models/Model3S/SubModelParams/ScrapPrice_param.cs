using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class ScrapPrice_param
    {
        public WEEKHModel weekH { get; set; }
        public CROPModel crop { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double scrapPrice { get; set; }
    }
}
