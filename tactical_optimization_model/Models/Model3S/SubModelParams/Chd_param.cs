using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class Chd_param
    {
        public PRODModel prod { get; set; }
        public DISTModel dc { get; set; }
        public double chd { get; set; }
    }
}
