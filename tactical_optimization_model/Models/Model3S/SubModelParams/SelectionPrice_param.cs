using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class SelectionPrice_param
    {
        public WORK3Model work3 { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double selectionPrice { get; set; }
    }
}
