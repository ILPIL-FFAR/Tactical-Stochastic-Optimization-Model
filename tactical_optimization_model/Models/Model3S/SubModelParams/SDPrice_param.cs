using ModelStochastic6.Models.Inputs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class SDPrice_param
    {
        public WEEK1Model week1 { get; set; }
        public PRODModel prod { get; set; }
        public QUALModel qual { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double sdPrice { get; set; }
    }
}
