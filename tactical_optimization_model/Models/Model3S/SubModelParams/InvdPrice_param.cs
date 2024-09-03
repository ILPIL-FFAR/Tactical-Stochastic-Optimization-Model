using ModelStochastic6.Models.Inputs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class InvdPrice_param
    {
        public WEEK1Model week1 { get; set; }
        public PRODModel prod { get; set; }
        public QUALModel qual { get; set; }
        public DISTModel dc { get; set; }
        public SCENModel scen { get; set; }
        public int nCut { get; set; }
        public double invdPrice { get; set; }
    }
}
